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
"""
import os
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


def random_suffix(length: int = 8) -> str:
    return "".join(random.choices(string.ascii_lowercase + string.digits, k=length))


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
    phone_digits = "".join(random.choices(string.digits, k=8))
    return {
        "email": f"testuser_{suffix}@example.com",
        "password": f"Test{suffix}Pass1",
        "username": f"testuser_{suffix}",
        "fullName": f"Test User {suffix}",
        "phone": f"09{phone_digits}",
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
    data = resp.json().get("data", resp.json())
    return data.get("token") or data.get("accessToken")


@pytest.fixture(scope="session")
def first_product_id(api_session, base_url):
    """Fetch the first available product ID."""
    resp = api_session.get(f"{base_url}/api/product?pageSize=1", timeout=30)
    if resp.status_code != 200:
        return None
    data = resp.json().get("data", {})
    items = data.get("items", data) if isinstance(data, dict) else data
    if isinstance(items, list) and items:
        item = items[0]
        return item.get("productId") or item.get("id")
    return None


@pytest.fixture(scope="session")
def first_brand_id(api_session, base_url):
    """Fetch the first available brand ID."""
    resp = api_session.get(f"{base_url}/api/brands?pageSize=1", timeout=30)
    if resp.status_code != 200:
        return None
    data = resp.json().get("data", {})
    items = data.get("items", data) if isinstance(data, dict) else data
    if isinstance(items, list) and items:
        item = items[0]
        return item.get("brandId") or item.get("id")
    return None


@pytest.fixture(scope="session")
def first_category_id(api_session, base_url):
    """Fetch the first available category ID."""
    resp = api_session.get(f"{base_url}/api/categories?pageSize=1", timeout=30)
    if resp.status_code != 200:
        return None
    data = resp.json().get("data", {})
    items = data.get("items", data) if isinstance(data, dict) else data
    if isinstance(items, list) and items:
        item = items[0]
        return item.get("categoryId") or item.get("id")
    return None
