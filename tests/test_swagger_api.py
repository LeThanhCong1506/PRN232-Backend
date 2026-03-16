"""
Comprehensive Swagger API Tests for PRN232-Backend
API: https://prn232-backend-production.up.railway.app

Run:
    pip install -r requirements-test.txt
    py -m pytest tests/test_swagger_api.py -v
    py -m pytest tests/test_swagger_api.py -v --html=report.html --self-contained-html

Sections:
    TestHealth          - /api/health/*
    TestStore           - /api/store/*
    TestBrands          - /api/brands/*
    TestCategories      - /api/categories/*
    TestProducts        - /api/product/*
    TestProductImages   - /api/products/*/images
    TestWarranty        - /api/warranty/*
    TestRoles           - /api/roles/*
    TestAuth            - /api/users/register, /api/users/login
    TestUserProfile     - /api/users/me, /api/users/{id}
    TestCart            - /api/cart/*
    TestCheckout        - /api/checkout/*
    TestOrders          - /api/order/*
    TestChat            - /api/chat/*
    TestPayment         - /api/payment/*
    TestSecurity        - auth/authz enforcement
"""
import pytest
import requests


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def get(session, base_url, path, token=None, **kwargs):
    headers = kwargs.pop("headers", {})
    if token:
        headers["Authorization"] = f"Bearer {token}"
    return session.get(f"{base_url}{path}", headers=headers, timeout=30, **kwargs)


def post(session, base_url, path, token=None, **kwargs):
    headers = kwargs.pop("headers", {})
    if token:
        headers["Authorization"] = f"Bearer {token}"
    return session.post(f"{base_url}{path}", headers=headers, timeout=30, **kwargs)


def put(session, base_url, path, token=None, **kwargs):
    headers = kwargs.pop("headers", {})
    if token:
        headers["Authorization"] = f"Bearer {token}"
    return session.put(f"{base_url}{path}", headers=headers, timeout=30, **kwargs)


def delete(session, base_url, path, token=None, **kwargs):
    headers = kwargs.pop("headers", {})
    if token:
        headers["Authorization"] = f"Bearer {token}"
    return session.delete(f"{base_url}{path}", headers=headers, timeout=30, **kwargs)


# ---------------------------------------------------------------------------
# Health
# ---------------------------------------------------------------------------

class TestHealth:
    """GET /api/health, /api/health/ping, /api/health/version, /api/health/detail"""

    def test_health_simple(self, api_session, base_url):
        r = get(api_session, base_url, "/api/health")
        assert r.status_code == 200, r.text

    def test_health_ping_get(self, api_session, base_url):
        r = get(api_session, base_url, "/api/health/ping")
        assert r.status_code == 200, r.text

    def test_health_ping_head(self, api_session, base_url):
        r = api_session.head(f"{base_url}/api/health/ping", timeout=30)
        assert r.status_code == 200

    def test_health_version(self, api_session, base_url):
        r = get(api_session, base_url, "/api/health/version")
        assert r.status_code == 200, r.text

    def test_health_detail(self, api_session, base_url):
        r = get(api_session, base_url, "/api/health/detail")
        assert r.status_code == 200, r.text


# ---------------------------------------------------------------------------
# Store
# ---------------------------------------------------------------------------

class TestStore:
    """GET /api/store/location, /api/store/branches"""

    def test_store_location(self, api_session, base_url):
        r = get(api_session, base_url, "/api/store/location")
        assert r.status_code == 200, r.text

    def test_store_branches(self, api_session, base_url):
        r = get(api_session, base_url, "/api/store/branches")
        assert r.status_code == 200, r.text


# ---------------------------------------------------------------------------
# Brands
# ---------------------------------------------------------------------------

class TestBrands:
    """GET /api/brands, GET /api/brands/{id}"""

    def test_list_brands(self, api_session, base_url):
        r = get(api_session, base_url, "/api/brands")
        assert r.status_code == 200, r.text

    def test_list_brands_paginated(self, api_session, base_url):
        r = get(api_session, base_url, "/api/brands?pageNumber=1&pageSize=5")
        assert r.status_code == 200, r.text

    def test_get_brand_by_id(self, api_session, base_url, first_brand_id):
        if not first_brand_id:
            pytest.skip("No brand available")
        r = get(api_session, base_url, f"/api/brands/{first_brand_id}")
        assert r.status_code == 200, r.text

    def test_get_brand_not_found(self, api_session, base_url):
        r = get(api_session, base_url, "/api/brands/999999")
        assert r.status_code == 404, r.text

    def test_create_brand_unauthorized(self, api_session, base_url):
        r = post(api_session, base_url, "/api/brands", json={"name": "TestBrand"})
        assert r.status_code == 401, r.text


# ---------------------------------------------------------------------------
# Categories
# ---------------------------------------------------------------------------

class TestCategories:
    """GET /api/categories, GET /api/categories/{id}"""

    def test_list_categories(self, api_session, base_url):
        r = get(api_session, base_url, "/api/categories")
        assert r.status_code == 200, r.text

    def test_list_categories_paginated(self, api_session, base_url):
        r = get(api_session, base_url, "/api/categories?pageNumber=1&pageSize=5")
        assert r.status_code == 200, r.text

    def test_get_category_by_id(self, api_session, base_url, first_category_id):
        if not first_category_id:
            pytest.skip("No category available")
        r = get(api_session, base_url, f"/api/categories/{first_category_id}")
        assert r.status_code == 200, r.text

    def test_get_category_not_found(self, api_session, base_url):
        r = get(api_session, base_url, "/api/categories/999999")
        assert r.status_code == 404, r.text

    def test_create_category_unauthorized(self, api_session, base_url):
        r = post(api_session, base_url, "/api/categories", json={"name": "TestCat"})
        assert r.status_code == 401, r.text


# ---------------------------------------------------------------------------
# Products
# ---------------------------------------------------------------------------

class TestProducts:
    """GET /api/product, GET /api/product/{id}, /api/product/categories, /api/product/brands"""

    def test_list_products(self, api_session, base_url):
        r = get(api_session, base_url, "/api/product")
        assert r.status_code == 200, r.text

    def test_list_products_paginated(self, api_session, base_url):
        r = get(api_session, base_url, "/api/product?pageNumber=1&pageSize=5")
        assert r.status_code == 200, r.text

    def test_search_products(self, api_session, base_url):
        r = get(api_session, base_url, "/api/product?searchTerm=arduino")
        assert r.status_code == 200, r.text

    def test_filter_products_by_brand(self, api_session, base_url, first_brand_id):
        if not first_brand_id:
            pytest.skip("No brand available")
        r = get(api_session, base_url, f"/api/product?brandId={first_brand_id}")
        assert r.status_code == 200, r.text

    def test_filter_products_by_category(self, api_session, base_url, first_category_id):
        if not first_category_id:
            pytest.skip("No category available")
        r = get(api_session, base_url, f"/api/product?categoryId={first_category_id}")
        assert r.status_code == 200, r.text

    def test_filter_products_price_range(self, api_session, base_url):
        r = get(api_session, base_url, "/api/product?minPrice=0&maxPrice=1000000")
        assert r.status_code == 200, r.text

    def test_get_product_by_id(self, api_session, base_url, first_product_id):
        if not first_product_id:
            pytest.skip("No product available")
        r = get(api_session, base_url, f"/api/product/{first_product_id}")
        assert r.status_code == 200, r.text

    def test_get_product_not_found(self, api_session, base_url):
        r = get(api_session, base_url, "/api/product/999999")
        assert r.status_code == 404, r.text

    def test_product_categories(self, api_session, base_url):
        r = get(api_session, base_url, "/api/product/categories")
        assert r.status_code == 200, r.text

    def test_product_brands(self, api_session, base_url):
        r = get(api_session, base_url, "/api/product/brands")
        assert r.status_code == 200, r.text

    def test_get_kit_bundle(self, api_session, base_url, first_product_id):
        if not first_product_id:
            pytest.skip("No product available")
        r = get(api_session, base_url, f"/api/product/{first_product_id}/bundle")
        assert r.status_code in (200, 400, 404), r.text

    def test_get_kit_bundle_stock(self, api_session, base_url, first_product_id):
        if not first_product_id:
            pytest.skip("No product available")
        r = get(api_session, base_url, f"/api/product/{first_product_id}/bundle/available-stock")
        assert r.status_code in (200, 400, 404), r.text

    def test_create_product_unauthorized(self, api_session, base_url):
        r = post(api_session, base_url, "/api/product",
                 json={"name": "Test", "sku": "SKU001", "price": 100, "stockQuantity": 10})
        assert r.status_code == 401, r.text


# ---------------------------------------------------------------------------
# Product Images
# ---------------------------------------------------------------------------

class TestProductImages:
    """GET /api/products/{productId}/images"""

    def test_get_product_images(self, api_session, base_url, first_product_id):
        if not first_product_id:
            pytest.skip("No product available")
        r = get(api_session, base_url, f"/api/products/{first_product_id}/images")
        assert r.status_code == 200, r.text

    def test_add_product_image_unauthorized(self, api_session, base_url, first_product_id):
        if not first_product_id:
            pytest.skip("No product available")
        r = post(api_session, base_url, f"/api/products/{first_product_id}/images",
                 json={"imageUrl": "https://example.com/img.jpg"})
        assert r.status_code == 401, r.text

    def test_delete_product_image_unauthorized(self, api_session, base_url):
        r = delete(api_session, base_url, "/api/products/images/999999")
        assert r.status_code == 401, r.text


# ---------------------------------------------------------------------------
# Warranty (public)
# ---------------------------------------------------------------------------

class TestWarranty:
    """Public warranty endpoints"""

    def test_get_warranty_policies(self, api_session, base_url):
        r = get(api_session, base_url, "/api/warranty/policies")
        assert r.status_code == 200, r.text

    def test_get_all_warranties(self, api_session, base_url):
        r = get(api_session, base_url, "/api/warranty")
        assert r.status_code == 200, r.text

    def test_get_active_warranties(self, api_session, base_url):
        r = get(api_session, base_url, "/api/warranty/active")
        assert r.status_code == 200, r.text

    def test_get_expired_warranties(self, api_session, base_url):
        r = get(api_session, base_url, "/api/warranty/expired")
        assert r.status_code == 200, r.text

    def test_get_warranty_by_id_not_found(self, api_session, base_url):
        r = get(api_session, base_url, "/api/warranty/999999")
        assert r.status_code == 404, r.text

    def test_get_warranty_by_serial_not_found(self, api_session, base_url):
        r = get(api_session, base_url, "/api/warranty/serial/INVALID-SERIAL-000")
        assert r.status_code == 404, r.text

    def test_get_warranties_by_product(self, api_session, base_url, first_product_id):
        if not first_product_id:
            pytest.skip("No product available")
        r = get(api_session, base_url, f"/api/warranty/product/{first_product_id}")
        assert r.status_code in (200, 404), r.text

    def test_get_my_warranties_unauthenticated(self, api_session, base_url):
        r = get(api_session, base_url, "/api/warranties")
        assert r.status_code == 401, r.text

    def test_create_warranty_unauthorized(self, api_session, base_url):
        r = post(api_session, base_url, "/api/warranty",
                 json={"serialNumber": "TEST-001", "productId": 1})
        assert r.status_code == 401, r.text


# ---------------------------------------------------------------------------
# Roles
# ---------------------------------------------------------------------------

class TestRoles:
    """GET /api/roles"""

    def test_list_roles(self, api_session, base_url):
        r = get(api_session, base_url, "/api/roles")
        assert r.status_code == 200, r.text


# ---------------------------------------------------------------------------
# Auth
# ---------------------------------------------------------------------------

class TestAuth:
    """POST /api/users/register, POST /api/users/login"""

    def test_register_new_user(self, api_session, base_url, registered_user):
        if registered_user is None:
            pytest.fail("Registration returned non-201 (likely server error)")
        assert registered_user is not None

    def test_login_success(self, api_session, base_url, user_token):
        if user_token is None:
            pytest.fail("Login failed or registration failed (likely server error)")
        assert len(user_token) > 20  # JWT token should be reasonably long

    def test_login_wrong_password(self, api_session, base_url, registered_user):
        if registered_user is None:
            pytest.skip("Registration not available (server error)")
        creds = registered_user["credentials"]
        r = post(api_session, base_url, "/api/users/login",
                 json={"email": creds["email"], "password": "WrongPassword999!"})
        assert r.status_code == 401, r.text

    def test_login_wrong_email(self, api_session, base_url):
        r = post(api_session, base_url, "/api/users/login",
                 json={"email": "nonexistent_user_xyz@example.com", "password": "AnyPass123!"})
        assert r.status_code == 401, r.text

    def test_login_missing_fields(self, api_session, base_url):
        r = post(api_session, base_url, "/api/users/login", json={})
        assert r.status_code in (400, 401), r.text

    def test_register_duplicate_email(self, api_session, base_url, registered_user):
        if registered_user is None:
            pytest.skip("Registration not available (server error)")
        creds = registered_user["credentials"]
        r = post(api_session, base_url, "/api/users/register",
                 json={
                     "email": creds["email"],
                     "password": creds["password"],
                     "username": f"other_user_xyz",
                 })
        assert r.status_code in (400, 409), r.text

    def test_register_invalid_email(self, api_session, base_url):
        r = post(api_session, base_url, "/api/users/register",
                 json={"email": "not-an-email", "password": "Pass@123!", "username": "userxyz"})
        assert r.status_code == 400, r.text

    def test_register_missing_fields(self, api_session, base_url):
        r = post(api_session, base_url, "/api/users/register", json={})
        assert r.status_code == 400, r.text


# ---------------------------------------------------------------------------
# User Profile
# ---------------------------------------------------------------------------

class TestUserProfile:
    """GET/PUT /api/users/me, GET /api/users"""

    def test_get_my_profile(self, api_session, base_url, user_token):
        if user_token is None:
            pytest.skip("No user token available (server error during login)")
        r = get(api_session, base_url, "/api/users/me", token=user_token)
        assert r.status_code == 200, r.text

    def test_get_my_profile_unauthenticated(self, api_session, base_url):
        r = get(api_session, base_url, "/api/users/me")
        assert r.status_code == 401, r.text

    def test_update_my_profile(self, api_session, base_url, user_token):
        if user_token is None:
            pytest.skip("No user token available (server error during login)")
        profile = get(api_session, base_url, "/api/users/me", token=user_token).json()
        r = put(api_session, base_url, "/api/users/me", token=user_token,
                json={"fullName": "Test User Updated", "phone": "0912345678",
                      "email": profile.get("email", "")})
        assert r.status_code == 200, r.text

    def test_get_all_users_unauthorized(self, api_session, base_url, user_token):
        if user_token is None:
            pytest.skip("No user token available (server error during login)")
        r = get(api_session, base_url, "/api/users", token=user_token)
        assert r.status_code == 403, r.text

    def test_get_all_users_unauthenticated(self, api_session, base_url):
        r = get(api_session, base_url, "/api/users")
        assert r.status_code == 401, r.text


# ---------------------------------------------------------------------------
# Cart
# ---------------------------------------------------------------------------

class TestCart:
    """GET/POST/PUT/DELETE /api/cart"""

    def test_get_cart_unauthenticated(self, api_session, base_url):
        r = get(api_session, base_url, "/api/cart")
        assert r.status_code == 401, r.text

    def test_get_cart_authenticated(self, api_session, base_url, user_token):
        if user_token is None:
            pytest.skip("No user token (server error)")
        r = get(api_session, base_url, "/api/cart", token=user_token)
        assert r.status_code == 200, r.text

    def test_add_to_cart_unauthenticated(self, api_session, base_url, first_product_id):
        if not first_product_id:
            pytest.skip("No product available")
        r = post(api_session, base_url, "/api/cart/items",
                 json={"productId": first_product_id, "quantity": 1})
        assert r.status_code == 401, r.text

    def test_cart_workflow(self, api_session, base_url, user_token, first_product_id):
        """Add → Update → Delete cart item lifecycle."""
        if user_token is None:
            pytest.skip("No user token (server error)")
        if not first_product_id:
            pytest.skip("No product available")

        # Add item
        r = post(api_session, base_url, "/api/cart/items", token=user_token,
                 json={"productId": first_product_id, "quantity": 1})
        assert r.status_code in (201, 200, 400), r.text  # 400 if out of stock

        if r.status_code in (200, 201):
            data = r.json().get("data", {})
            cart_item_id = data.get("cartItemId")
            if cart_item_id:
                # Update quantity
                r2 = put(api_session, base_url, f"/api/cart/items/{cart_item_id}",
                         token=user_token, json={"quantity": 2})
                assert r2.status_code == 200, r2.text

                # Remove item
                r3 = delete(api_session, base_url, f"/api/cart/items/{cart_item_id}",
                            token=user_token)
                assert r3.status_code == 200, r3.text

    def test_clear_cart(self, api_session, base_url, user_token):
        if user_token is None:
            pytest.skip("No user token (server error)")
        r = delete(api_session, base_url, "/api/cart", token=user_token)
        assert r.status_code == 200, r.text

    def test_validate_invalid_coupon(self, api_session, base_url, user_token):
        if user_token is None:
            pytest.skip("No user token (server error)")
        r = post(api_session, base_url, "/api/cart/validate-coupon", token=user_token,
                 json={"couponCode": "INVALID_COUPON_XYZ_999"})
        assert r.status_code in (400, 404), r.text

    def test_validate_coupon_unauthenticated(self, api_session, base_url):
        r = post(api_session, base_url, "/api/cart/validate-coupon",
                 json={"couponCode": "TESTCODE"})
        assert r.status_code == 401, r.text

    def test_update_cart_item_not_found(self, api_session, base_url, user_token):
        if user_token is None:
            pytest.skip("No user token (server error)")
        r = put(api_session, base_url, "/api/cart/items/999999", token=user_token,
                json={"quantity": 1})
        assert r.status_code in (400, 404), r.text

    def test_delete_cart_item_not_found(self, api_session, base_url, user_token):
        if user_token is None:
            pytest.skip("No user token (server error)")
        r = delete(api_session, base_url, "/api/cart/items/999999", token=user_token)
        assert r.status_code in (400, 404), r.text


# ---------------------------------------------------------------------------
# Checkout
# ---------------------------------------------------------------------------

class TestCheckout:
    """GET /api/checkout/shipping-info, /api/checkout/payment-methods, POST /api/checkout/validate"""

    def test_shipping_info_unauthenticated(self, api_session, base_url):
        r = get(api_session, base_url, "/api/checkout/shipping-info")
        assert r.status_code == 401, r.text

    def test_shipping_info_authenticated(self, api_session, base_url, user_token):
        if user_token is None:
            pytest.skip("No user token (server error)")
        r = get(api_session, base_url, "/api/checkout/shipping-info", token=user_token)
        assert r.status_code == 200, r.text

    def test_payment_methods_unauthenticated(self, api_session, base_url):
        r = get(api_session, base_url, "/api/checkout/payment-methods")
        assert r.status_code == 401, r.text

    def test_payment_methods_authenticated(self, api_session, base_url, user_token):
        if user_token is None:
            pytest.skip("No user token (server error)")
        r = get(api_session, base_url, "/api/checkout/payment-methods", token=user_token)
        assert r.status_code == 200, r.text

    def test_validate_checkout_empty_cart(self, api_session, base_url, user_token):
        if user_token is None:
            pytest.skip("No user token (server error)")
        r = post(api_session, base_url, "/api/checkout/validate", token=user_token, json={})
        # Empty cart should return 400 or 200 with errors
        assert r.status_code in (200, 400), r.text

    def test_validate_checkout_unauthenticated(self, api_session, base_url):
        r = post(api_session, base_url, "/api/checkout/validate", json={})
        assert r.status_code == 401, r.text


# ---------------------------------------------------------------------------
# Orders
# ---------------------------------------------------------------------------

class TestOrders:
    """GET /api/order/my-orders, GET /api/order/{id}, PUT /api/order/{id}/cancel"""

    def test_my_orders_unauthenticated(self, api_session, base_url):
        r = get(api_session, base_url, "/api/order/my-orders")
        assert r.status_code == 401, r.text

    def test_my_orders_authenticated(self, api_session, base_url, user_token):
        if user_token is None:
            pytest.skip("No user token (server error)")
        r = get(api_session, base_url, "/api/order/my-orders", token=user_token)
        assert r.status_code == 200, r.text

    def test_my_orders_paginated(self, api_session, base_url, user_token):
        if user_token is None:
            pytest.skip("No user token (server error)")
        r = get(api_session, base_url, "/api/order/my-orders?pageNumber=1&pageSize=5",
                token=user_token)
        assert r.status_code == 200, r.text

    def test_get_order_not_found(self, api_session, base_url, user_token):
        if user_token is None:
            pytest.skip("No user token (server error)")
        r = get(api_session, base_url, "/api/order/999999", token=user_token)
        assert r.status_code in (400, 403, 404), r.text

    def test_get_all_orders_unauthorized(self, api_session, base_url, user_token):
        if user_token is None:
            pytest.skip("No user token (server error)")
        r = get(api_session, base_url, "/api/order", token=user_token)
        assert r.status_code == 403, r.text

    def test_cancel_order_not_found(self, api_session, base_url, user_token):
        if user_token is None:
            pytest.skip("No user token (server error)")
        r = put(api_session, base_url, "/api/order/999999/cancel", token=user_token,
                json={"reason": "Test cancel"})
        assert r.status_code in (400, 403, 404), r.text

    def test_checkout_empty_cart(self, api_session, base_url, user_token):
        """Creating an order from empty cart should fail."""
        if user_token is None:
            pytest.skip("No user token (server error)")
        r = post(api_session, base_url, "/api/order/checkout", token=user_token,
                 json={
                     "paymentMethod": "COD",
                     "customerName": "Test User",
                     "customerPhone": "0912345678",
                     "shippingAddress": "123 Test St, HCM"
                 })
        assert r.status_code in (400, 404), r.text  # cart is empty


# ---------------------------------------------------------------------------
# Chat
# ---------------------------------------------------------------------------

class TestChat:
    """GET /api/chat/history, /api/chat/unread-count, POST /api/chat/send"""

    def test_chat_history_unauthenticated(self, api_session, base_url):
        r = get(api_session, base_url, "/api/chat/history")
        assert r.status_code == 401, r.text

    def test_chat_history_authenticated(self, api_session, base_url, user_token):
        if user_token is None:
            pytest.skip("No user token (server error)")
        r = get(api_session, base_url, "/api/chat/history", token=user_token)
        assert r.status_code == 200, r.text

    def test_chat_history_paginated(self, api_session, base_url, user_token):
        if user_token is None:
            pytest.skip("No user token (server error)")
        r = get(api_session, base_url, "/api/chat/history?pageNumber=1&pageSize=20",
                token=user_token)
        assert r.status_code == 200, r.text

    def test_unread_count_unauthenticated(self, api_session, base_url):
        r = get(api_session, base_url, "/api/chat/unread-count")
        assert r.status_code == 401, r.text

    def test_unread_count_authenticated(self, api_session, base_url, user_token):
        if user_token is None:
            pytest.skip("No user token (server error)")
        r = get(api_session, base_url, "/api/chat/unread-count", token=user_token)
        assert r.status_code == 200, r.text

    def test_mark_read_unauthenticated(self, api_session, base_url):
        r = post(api_session, base_url, "/api/chat/mark-read")
        assert r.status_code == 401, r.text

    def test_mark_read_authenticated(self, api_session, base_url, user_token):
        if user_token is None:
            pytest.skip("No user token (server error)")
        r = post(api_session, base_url, "/api/chat/mark-read", token=user_token)
        assert r.status_code == 200, r.text

    def test_chat_conversations_unauthorized(self, api_session, base_url, user_token):
        if user_token is None:
            pytest.skip("No user token (server error)")
        r = get(api_session, base_url, "/api/chat/conversations", token=user_token)
        assert r.status_code == 403, r.text

    def test_send_message_unauthenticated(self, api_session, base_url):
        r = post(api_session, base_url, "/api/chat/send",
                 json={"content": "Hello"})
        assert r.status_code == 401, r.text

    def test_send_message_authenticated(self, api_session, base_url, user_token):
        if user_token is None:
            pytest.skip("No user token (server error)")
        r = post(api_session, base_url, "/api/chat/send", token=user_token,
                 json={"content": "Hello, I need help"})
        assert r.status_code in (200, 201), r.text


# ---------------------------------------------------------------------------
# Payment (public endpoints only)
# ---------------------------------------------------------------------------

class TestPayment:
    """Public payment endpoints"""

    def test_poll_payment_status_not_found(self, api_session, base_url):
        r = get(api_session, base_url, "/api/payment/999999/poll-status")
        assert r.status_code in (200, 400, 404), r.text

    def test_get_payment_status_unauthenticated(self, api_session, base_url):
        r = get(api_session, base_url, "/api/payment/999999/status")
        assert r.status_code == 401, r.text

    def test_payment_status_authenticated(self, api_session, base_url, user_token):
        if user_token is None:
            pytest.skip("No user token (server error)")
        r = get(api_session, base_url, "/api/payment/999999/status", token=user_token)
        assert r.status_code in (400, 403, 404), r.text

    def test_sepay_webhook_empty_body(self, api_session, base_url):
        r = post(api_session, base_url, "/api/payment/sepay-webhook", json={})
        assert r.status_code in (200, 400, 401, 422), r.text


# ---------------------------------------------------------------------------
# Security: auth/authz enforcement
# ---------------------------------------------------------------------------

class TestSecurity:
    """Verify protected endpoints reject unauthenticated and unauthorised requests."""

    PROTECTED_401 = [
        ("GET",  "/api/cart"),
        ("GET",  "/api/users/me"),
        ("GET",  "/api/order/my-orders"),
        ("GET",  "/api/checkout/shipping-info"),
        ("GET",  "/api/checkout/payment-methods"),
        ("GET",  "/api/warranties"),
        ("GET",  "/api/chat/history"),
        ("GET",  "/api/chat/unread-count"),
    ]

    ADMIN_ONLY_403 = [
        ("GET",  "/api/admin/orders"),
        ("GET",  "/api/admin/dashboard"),
        ("GET",  "/api/users"),
        ("GET",  "/api/chat/conversations"),
        ("GET",  "/api/admin/products"),
        ("GET",  "/api/admin/warranty-claims"),
    ]

    @pytest.mark.parametrize("method,path", PROTECTED_401)
    def test_unauthenticated_returns_401(self, api_session, base_url, method, path):
        r = api_session.request(method, f"{base_url}{path}", timeout=30)
        assert r.status_code == 401, f"{method} {path} → expected 401, got {r.status_code}"

    @pytest.mark.parametrize("method,path", ADMIN_ONLY_403)
    def test_regular_user_returns_403_on_admin_endpoints(
        self, api_session, base_url, user_token, method, path
    ):
        if user_token is None:
            pytest.skip("No user token (server error)")
        headers = {"Authorization": f"Bearer {user_token}"}
        r = api_session.request(method, f"{base_url}{path}", headers=headers, timeout=30)
        assert r.status_code == 403, f"{method} {path} → expected 403, got {r.status_code}"

    def test_invalid_jwt_returns_401(self, api_session, base_url):
        r = get(api_session, base_url, "/api/users/me",
                headers={"Authorization": "Bearer invalid.jwt.token.here"})
        assert r.status_code == 401, r.text

    def test_malformed_bearer_header(self, api_session, base_url):
        r = get(api_session, base_url, "/api/users/me",
                headers={"Authorization": "NotBearer sometoken"})
        assert r.status_code == 401, r.text
