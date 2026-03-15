"""
Pytest configuration and shared fixtures for PRN232-Backend API tests.
"""
import random
import string
import pytest
import requests

BASE_URL = "https://prn232-backend-production.up.railway.app"


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
    return {
        "email": f"testuser_{suffix}@example.com",
        "password": f"Test@{suffix}Pass1!",
        "username": f"testuser_{suffix}",
    }


@pytest.fixture(scope="session")
def registered_user(api_session, base_url, test_user_credentials):
    """Register a test user. Returns None if registration fails (e.g. server error)."""
    resp = api_session.post(
        f"{base_url}/api/users/register",
        json=test_user_credentials,
        timeout=30,
    )
    if resp.status_code != 201:
        return None
    data = resp.json().get("data", resp.json())
    return {"credentials": test_user_credentials, "data": data}


@pytest.fixture(scope="session")
def user_token(api_session, base_url, registered_user):
    """Login test user and return JWT token. Returns None if login fails."""
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
