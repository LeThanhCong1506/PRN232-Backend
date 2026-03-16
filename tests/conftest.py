"""
Pytest configuration and shared fixtures for PRN232-Backend API tests.

Pass existing credentials via environment variables to skip auto-registration:
    set TEST_USER_EMAIL=your@email.com
    set TEST_USER_PASSWORD=YourPass123
    set TEST_ADMIN_EMAIL=admin@email.com
    set TEST_ADMIN_PASSWORD=AdminPass123
    pytest tests/test_swagger_api.py -v

Or on Linux/Mac:
    TEST_USER_EMAIL=your@email.com TEST_USER_PASSWORD=Pass123 pytest tests/ -v

API response formats (confirmed via live testing):
    - Login/Register: { "userId", "username", "email", "role", "accessToken" }  (no "data" wrapper)
    - Products:       { "success", "message", "data": { "items": [...], "pagination": {...} } }
    - Brands:         { "items": [...], "pagination": {...} }  (no "data" wrapper)
    - Categories:     { "items": [...], "pagination": {...} }  (no "data" wrapper)
"""
import os
import uuid
import random
import string
import pytest
import requests

BASE_URL = "https://prn232-backend-production.up.railway.app"

# Read from environment variables (optional — override auto-register)
ENV_USER_EMAIL    = os.environ.get("TEST_USER_EMAIL")
ENV_USER_PASSWORD = os.environ.get("TEST_USER_PASSWORD")
ENV_ADMIN_EMAIL   = os.environ.get("TEST_ADMIN_EMAIL")
ENV_ADMIN_PASSWORD= os.environ.get("TEST_ADMIN_PASSWORD")

# Valid Vietnamese mobile prefixes (Viettel, Mobifone, Vinaphone, Vietnamobile, Gmobile)
_VN_PHONE_PREFIXES = [
    "032", "033", "034", "035", "036", "037", "038", "039",  # Viettel
    "096", "097", "098",                                       # Viettel
    "070", "076", "077", "078", "079", "089", "090", "093",   # Mobifone
    "081", "082", "083", "084", "085", "086",                  # Vinaphone
    "056", "058",                                              # Vietnamobile
    "059",                                                     # Gmobile
]


def random_suffix(length: int = 8) -> str:
    return "".join(random.choices(string.ascii_lowercase + string.digits, k=length))


def _unique_phone() -> str:
    """Generate a unique Vietnamese phone number using UUID-based suffix."""
    prefix = random.choice(_VN_PHONE_PREFIXES)
    # Use UUID to ensure global uniqueness across test runs
    tail = str(uuid.uuid4().int)[:7]
    return prefix + tail


@pytest.fixture(scope="session")
def base_url() -> str:
    return BASE_URL


@pytest.fixture(scope="session")
def api_session() -> requests.Session:
    session = requests.Session()
    session.timeout = 30
    yield session
    session.close()


@pytest.fixture(scope="session")
def test_user_credentials():
    suffix = random_suffix()
    return {
        "email": f"testuser_{suffix}@example.com",
        "password": f"Test{suffix}Pass1!",
        "username": f"testuser_{suffix}",
        "fullName": f"Test User {suffix}",
        "phone": _unique_phone(),
    }


@pytest.fixture(scope="session")
def registered_user(api_session, base_url, test_user_credentials):
    """
    Return user credentials.
    - If TEST_USER_EMAIL env var is set → use that existing account (skip register).
    - Otherwise → auto-register a new test account.
    Returns None if both options fail.
    """
    if ENV_USER_EMAIL and ENV_USER_PASSWORD:
        # Use existing account from env
        return {"credentials": {"email": ENV_USER_EMAIL, "password": ENV_USER_PASSWORD,
                                "username": ENV_USER_EMAIL.split("@")[0]},
                "data": {}}

    # Auto-register
    resp = api_session.post(
        f"{base_url}/api/users/register",
        json=test_user_credentials,
        timeout=30,
    )
    if resp.status_code not in (200, 201):
        return None
    data = resp.json().get("data", resp.json())
    return {"credentials": test_user_credentials, "data": data}


@pytest.fixture(scope="session")
def user_token(api_session, base_url, registered_user):
    """Login and return JWT token. Returns None if login fails."""
    if registered_user is None:
        return None
    creds = registered_user["credentials"]
    resp = api_session.post(
        f"{base_url}/api/users/login",
        json={"email": creds["email"], "password": creds["password"]},
        timeout=30,
    )
    if resp.status_code != 200:
        return None
    # API returns token directly (no "data" wrapper): { "accessToken": "..." }
    data = resp.json().get("data", resp.json())
    return (data.get("accessToken") or data.get("token")
            or data.get("AccessToken") or data.get("Token"))


def _extract_items(resp_json: dict) -> list:
    """
    Handle both wrapped and unwrapped list responses:
      - Wrapped:   { "data": { "items": [...] } }
      - Unwrapped: { "items": [...] }
    """
    inner = resp_json.get("data", resp_json)
    if isinstance(inner, dict):
        items = inner.get("items", [])
        if isinstance(items, list):
            return items
    return []


@pytest.fixture(scope="session")
def first_product_id(api_session, base_url):
    """Fetch the first available product ID."""
    resp = api_session.get(f"{base_url}/api/product?pageSize=1", timeout=30)
    if resp.status_code != 200:
        return None
    items = _extract_items(resp.json())
    if items:
        return items[0].get("productId") or items[0].get("id")
    return None


@pytest.fixture(scope="session")
def first_brand_id(api_session, base_url):
    """Fetch the first available brand ID.
    Brands endpoint returns { "items": [...] } without a "data" wrapper.
    """
    resp = api_session.get(f"{base_url}/api/brands?pageSize=1", timeout=30)
    if resp.status_code != 200:
        return None
    items = _extract_items(resp.json())
    if items:
        return items[0].get("brandId") or items[0].get("id")
    return None


@pytest.fixture(scope="session")
def first_category_id(api_session, base_url):
    """Fetch the first available category ID.
    Categories endpoint returns { "items": [...] } without a "data" wrapper.
    """
    resp = api_session.get(f"{base_url}/api/categories?pageSize=1", timeout=30)
    if resp.status_code != 200:
        return None
    items = _extract_items(resp.json())
    if items:
        return items[0].get("categoryId") or items[0].get("id")
    return None
