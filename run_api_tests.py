#!/usr/bin/env python3
"""
Standalone API test runner for PRN232-Backend Swagger endpoints.

Usage:
    python run_api_tests.py
    python run_api_tests.py --user-email user@example.com --user-password Pass@123
    python run_api_tests.py --admin-email admin@example.com --admin-password Admin@123
    python run_api_tests.py --user-email u@e.com --user-password P@123 --admin-email a@e.com --admin-password A@123
    python run_api_tests.py --base-url https://prn232-backend-production.up.railway.app

Requirements:
    pip install requests
"""
import argparse
import json
import random
import string
import sys
from datetime import datetime, timedelta
from typing import Any, Dict, Optional

import requests

BASE_URL = "https://prn232-backend-production.up.railway.app"

GREEN  = "\033[92m"
RED    = "\033[91m"
YELLOW = "\033[93m"
CYAN   = "\033[96m"
BOLD   = "\033[1m"
DIM    = "\033[2m"
RESET  = "\033[0m"


def _suffix(n: int = 8) -> str:
    return "".join(random.choices(string.ascii_lowercase + string.digits, k=n))


class ApiTester:
    def __init__(self, base_url: str):
        self.base_url = base_url.rstrip("/")
        self.session = requests.Session()
        self.session.timeout = 30
        self.results: list[dict] = []

        # State populated during test run
        self.user_token:     Optional[str] = None
        self.admin_token:    Optional[str] = None
        self.test_user_id:   Optional[Any] = None
        self.product_id:     Optional[Any] = None
        self.brand_id:       Optional[Any] = None
        self.category_id:    Optional[Any] = None

    # ------------------------------------------------------------------
    # Low-level helpers
    # ------------------------------------------------------------------

    def _headers(self, token: Optional[str]) -> dict:
        return {"Authorization": f"Bearer {token}"} if token else {}

    def _req(self, method: str, path: str, token: Optional[str] = None, **kw) -> requests.Response:
        headers = {**self._headers(token), **kw.pop("headers", {})}
        return self.session.request(method, f"{self.base_url}{path}",
                                    headers=headers, timeout=30, **kw)

    def _record(self, method: str, path: str, got: int, want: int, note: str = "") -> bool:
        ok = got == want
        is_server_err = (got == 500)
        self.results.append({"method": method, "path": path,
                              "got": got, "want": want, "ok": ok,
                              "server_err": is_server_err, "note": note})
        if ok:
            icon = f"{GREEN}✓{RESET}"
        elif is_server_err:
            icon = f"{YELLOW}⚡{RESET}"  # server-side error
        else:
            icon = f"{RED}✗{RESET}"
        note_str = f"  {DIM}({note}){RESET}" if note else ""
        suffix = f"  {YELLOW}[SERVER ERROR]{RESET}" if (not ok and is_server_err) else ""
        print(f"  {icon} {method:<7}{path}{note_str}  →  {got}{suffix}")
        return ok

    def t(self, method: str, path: str, want: int,
          token: Optional[str] = None, note: str = "", **kw) -> Optional[requests.Response]:
        """Make a request, record the result, return the response (or None on error)."""
        try:
            r = self._req(method, path, token=token, **kw)
            self._record(method, path, r.status_code, want, note)
            return r
        except Exception as exc:
            self._record(method, path, 0, want, f"ERROR: {exc}")
            return None

    def _heading(self, title: str, icon: str = "▸"):
        print(f"\n{CYAN}{BOLD}{icon}  {title}{RESET}")

    # ------------------------------------------------------------------
    # Data bootstrapping helpers
    # ------------------------------------------------------------------

    def _bootstrap_ids(self):
        """Fetch the first product/brand/category IDs so tests can reference them."""
        for path, id_keys, attr in [
            ("/api/product?pageSize=1",    ["productId", "id"],    "product_id"),
            ("/api/brands?pageSize=1",     ["brandId",   "id"],    "brand_id"),
            ("/api/categories?pageSize=1", ["categoryId","id"],    "category_id"),
        ]:
            try:
                r = self._req("GET", path)
                if r.status_code == 200:
                    data = r.json().get("data", {})
                    items = (data.get("items", data) if isinstance(data, dict) else data)
                    if isinstance(items, list) and items:
                        item = items[0]
                        for k in id_keys:
                            if k in item:
                                setattr(self, attr, item[k])
                                break
            except Exception:
                pass

    # ------------------------------------------------------------------
    # Test groups
    # ------------------------------------------------------------------

    def test_health(self):
        self._heading("Health  /api/health/*", "💚")
        self.t("GET",  "/api/health",         200, note="simple")
        self.t("GET",  "/api/health/ping",    200, note="ping")
        self.t("GET",  "/api/health/version", 200, note="version")
        self.t("GET",  "/api/health/detail",  200, note="detail + DB")

    def test_store(self):
        self._heading("Store  /api/store/*", "🏪")
        self.t("GET", "/api/store/location", 200, note="GPS location")
        self.t("GET", "/api/store/branches", 200, note="all branches")

    def test_brands(self):
        self._heading("Brands  /api/brands/*", "🏷️")
        self.t("GET", "/api/brands",                  200, note="list")
        self.t("GET", "/api/brands?pageNumber=1&pageSize=5", 200, note="paginated")
        if self.brand_id:
            self.t("GET", f"/api/brands/{self.brand_id}", 200, note="by ID")
        self.t("GET", "/api/brands/999999",            404, note="not found")
        self.t("POST","/api/brands", 401, json={"name":"X"}, note="no auth → 401")

    def test_categories(self):
        self._heading("Categories  /api/categories/*", "📂")
        self.t("GET", "/api/categories",               200, note="list")
        self.t("GET", "/api/categories?pageNumber=1&pageSize=5", 200, note="paginated")
        if self.category_id:
            self.t("GET", f"/api/categories/{self.category_id}", 200, note="by ID")
        self.t("GET", "/api/categories/999999",         404, note="not found")
        self.t("POST","/api/categories", 401, json={"name":"X"}, note="no auth → 401")

    def test_products(self):
        self._heading("Products  /api/product/*", "📦")
        self.t("GET", "/api/product",                                 200, note="list")
        self.t("GET", "/api/product?pageNumber=1&pageSize=5",         200, note="paginated")
        self.t("GET", "/api/product?searchTerm=arduino",              200, note="search")
        self.t("GET", "/api/product?minPrice=0&maxPrice=5000000",     200, note="price filter")
        self.t("GET", "/api/product/categories",                      200, note="categories+count")
        self.t("GET", "/api/product/brands",                          200, note="brands+count")
        if self.product_id:
            self.t("GET", f"/api/product/{self.product_id}",          200, note="by ID")
            self.t("GET", f"/api/products/{self.product_id}/images",  200, note="images")
            self.t("GET", f"/api/product/{self.product_id}/reviews",  200, note="reviews")
            self.t("GET", f"/api/product/{self.product_id}/bundle",   200, note="bundle")
        if self.brand_id:
            self.t("GET", f"/api/product?brandId={self.brand_id}",    200, note="filter by brand")
        if self.category_id:
            self.t("GET", f"/api/product?categoryId={self.category_id}", 200, note="filter by cat")
        self.t("GET", "/api/product/999999",                          404, note="not found")
        self.t("POST","/api/product", 401, json={"name":"X"},         note="no auth → 401")

    def test_warranty_public(self):
        self._heading("Warranty  /api/warranty/* (public)", "🛡️")
        self.t("GET", "/api/warranty/policies",                200, note="policies")
        self.t("GET", "/api/warranty",                         200, note="all")
        self.t("GET", "/api/warranty/active",                  200, note="active")
        self.t("GET", "/api/warranty/expired",                 200, note="expired")
        self.t("GET", "/api/warranty/999999",                  404, note="not found")
        self.t("GET", "/api/warranty/serial/INVALID-XYZ-000", 404, note="serial not found")
        self.t("GET", "/api/warranties",                       401, note="my warranties → 401")

    def test_roles(self):
        self._heading("Roles  /api/roles", "👥")
        self.t("GET", "/api/roles", 200, note="list roles")

    def test_auth(self):
        self._heading("Auth  /api/users/register & /login", "🔐")
        sfx = _suffix()
        email    = f"testapi_{sfx}@example.com"
        password = f"Test@{sfx}Pass1!"
        username = f"testapi_{sfx}"

        # Register
        r = self.t("POST", "/api/users/register", 201,
                   json={"email": email, "password": password, "username": username},
                   note="register")

        # Login
        r = self.t("POST", "/api/users/login", 200,
                   json={"email": email, "password": password},
                   note="login")
        if r and r.status_code == 200:
            try:
                d = r.json().get("data", r.json())
                self.user_token   = d.get("token") or d.get("accessToken")
                self.test_user_id = (d.get("user") or {}).get("id")
                if self.user_token:
                    print(f"    {GREEN}→ user token obtained{RESET}")
            except Exception:
                pass

        # Negative cases
        self.t("POST", "/api/users/login", 401,
               json={"email": email, "password": "WrongPass999!"},
               note="wrong password → 401")
        self.t("POST", "/api/users/login", 401,
               json={"email": "nobody@example.com", "password": "anything"},
               note="unknown email → 401")
        self.t("POST", "/api/users/register", 400,
               json={"email": email, "password": password, "username": "dup"},
               note="duplicate email → 400")
        self.t("POST", "/api/users/register", 400,
               json={"email": "bad-email", "password": password, "username": "x"},
               note="invalid email → 400")
        self.t("POST", "/api/users/register", 400,
               json={}, note="empty body → 400")

    def test_user_authenticated(self):
        if not self.user_token:
            print(f"\n{YELLOW}⚠  Skipping authenticated user tests (no token){RESET}")
            return

        self._heading("User Profile  /api/users/*", "👤")
        self.t("GET", "/api/users/me",    200, token=self.user_token, note="my profile")
        self.t("GET", "/api/users/me",    401, note="no token → 401")
        self.t("PUT", "/api/users/me",    200, token=self.user_token,
               json={"fullName": "API Test User", "phone": "0909000000"},
               note="update profile")
        if self.test_user_id:
            self.t("GET", f"/api/users/{self.test_user_id}", 200,
                   token=self.user_token, note="get by ID")

        self._heading("Cart  /api/cart/*", "🛒")
        self.t("GET", "/api/cart", 200, token=self.user_token, note="get cart")
        self.t("GET", "/api/cart", 401,                         note="no auth → 401")

        if self.product_id:
            r = self.t("POST", "/api/cart/items", 201, token=self.user_token,
                       json={"productId": self.product_id, "quantity": 1},
                       note="add item")
            if r and r.status_code in (200, 201):
                try:
                    cid = r.json().get("data", {}).get("cartItemId")
                    if cid:
                        self.t("PUT", f"/api/cart/items/{cid}", 200, token=self.user_token,
                               json={"quantity": 2}, note="update qty")
                        self.t("DELETE", f"/api/cart/items/{cid}", 200, token=self.user_token,
                               note="remove item")
                except Exception:
                    pass

        self.t("POST", "/api/cart/validate-coupon", 400, token=self.user_token,
               json={"couponCode": "BAD_COUPON_XYZ"}, note="invalid coupon → 400")
        self.t("DELETE", "/api/cart", 200, token=self.user_token, note="clear cart")

        self._heading("Checkout  /api/checkout/*", "💳")
        self.t("GET",  "/api/checkout/shipping-info",   200, token=self.user_token)
        self.t("GET",  "/api/checkout/payment-methods", 200, token=self.user_token)
        self.t("POST", "/api/checkout/validate",        400, token=self.user_token,
               json={}, note="empty cart → 400")

        self._heading("Orders  /api/order/*", "📋")
        self.t("GET", "/api/order/my-orders",                         200, token=self.user_token)
        self.t("GET", "/api/order/my-orders?pageNumber=1&pageSize=5", 200, token=self.user_token,
               note="paginated")
        self.t("GET", "/api/order/999999",      404, token=self.user_token, note="not found")
        self.t("GET", "/api/order",             403, token=self.user_token, note="admin only → 403")
        self.t("POST","/api/order/checkout",    400, token=self.user_token,
               json={"paymentMethod":"COD","customerName":"Test","customerPhone":"0900000000",
                     "shippingAddress":"123 Test St"},
               note="empty cart → 400")

        self._heading("Chat  /api/chat/*", "💬")
        self.t("GET",  "/api/chat/history",                         200, token=self.user_token)
        self.t("GET",  "/api/chat/history?pageNumber=1&pageSize=20",200, token=self.user_token)
        self.t("GET",  "/api/chat/unread-count",                    200, token=self.user_token)
        self.t("POST", "/api/chat/mark-read",                       200, token=self.user_token,
               note="mark admin msgs read")
        self.t("POST", "/api/chat/send",                            200, token=self.user_token,
               json={"content": "Hello, need help"}, note="send message")
        self.t("GET",  "/api/chat/conversations", 403, token=self.user_token,
               note="admin only → 403")

        self._heading("Warranty (Authenticated)  /api/warranties", "🛡️")
        self.t("GET", "/api/warranties", 200, token=self.user_token, note="my warranties")

    def test_admin(self):
        if not self.admin_token:
            print(f"\n{YELLOW}⚠  No admin token — skipping admin endpoint tests{RESET}")
            return

        self._heading("Admin Orders  /api/admin/orders/*", "👑")
        self.t("GET", "/api/admin/orders",              200, token=self.admin_token)
        self.t("GET", "/api/admin/orders?pageSize=5",   200, token=self.admin_token)
        self.t("GET", "/api/admin/orders/999999",       404, token=self.admin_token, note="not found")

        self._heading("Admin Dashboard  /api/admin/dashboard/*", "📊")
        self.t("GET", "/api/admin/dashboard", 200, token=self.admin_token)
        from_dt = (datetime.now() - timedelta(days=30)).strftime("%Y-%m-%d")
        to_dt   = datetime.now().strftime("%Y-%m-%d")
        self.t("GET", f"/api/admin/dashboard/revenue-chart?from={from_dt}&to={to_dt}",
               200, token=self.admin_token)

        self._heading("Admin Products  /api/admin/products/*", "📦")
        self.t("GET", "/api/admin/products",            200, token=self.admin_token)
        self.t("GET", "/api/admin/products?pageSize=5", 200, token=self.admin_token)

        self._heading("Admin Warranty Claims  /api/admin/warranty-claims", "🛡️")
        self.t("GET", "/api/admin/warranty-claims",     200, token=self.admin_token)

        self._heading("Admin Chat  /api/chat/conversations", "💬")
        self.t("GET", "/api/chat/conversations",        200, token=self.admin_token)

        self._heading("Users (admin)  /api/users", "👥")
        self.t("GET", "/api/users",                     200, token=self.admin_token)
        self.t("GET", "/api/users/me",                  200, token=self.admin_token)

    def test_security(self):
        self._heading("Security — auth/authz enforcement", "🔒")

        protected = [
            ("GET",    "/api/cart"),
            ("GET",    "/api/users/me"),
            ("GET",    "/api/order/my-orders"),
            ("GET",    "/api/checkout/shipping-info"),
            ("GET",    "/api/checkout/payment-methods"),
            ("GET",    "/api/warranties"),
            ("GET",    "/api/chat/history"),
            ("GET",    "/api/chat/unread-count"),
        ]
        for method, path in protected:
            r = self._req(method, path)
            self._record(method, path, r.status_code, 401, "no token → 401")

        if self.user_token:
            admin_only = [
                ("GET", "/api/admin/orders"),
                ("GET", "/api/admin/dashboard"),
                ("GET", "/api/users"),
                ("GET", "/api/chat/conversations"),
                ("GET", "/api/admin/products"),
                ("GET", "/api/admin/warranty-claims"),
            ]
            for method, path in admin_only:
                r = self._req(method, path, token=self.user_token)
                self._record(method, path, r.status_code, 403, "user → admin endpoint → 403")

        # Invalid JWT
        r = self._req("GET", "/api/users/me",
                      headers={"Authorization": "Bearer invalid.jwt.token.here"})
        self._record("GET", "/api/users/me", r.status_code, 401, "invalid JWT → 401")

        r = self._req("GET", "/api/users/me",
                      headers={"Authorization": "Basic dXNlcjpwYXNz"})
        self._record("GET", "/api/users/me", r.status_code, 401, "Basic scheme → 401")

    def test_payment_public(self):
        self._heading("Payment (public endpoints)", "💰")
        self.t("GET",  "/api/payment/999999/poll-status", 404,    note="not found")
        self.t("GET",  "/api/payment/999999/status",      401,    note="no auth → 401")
        self.t("POST", "/api/payment/sepay-webhook",      200,
               json={"id": 0, "gateway": "MBBank", "transactionDate": "2024-01-01 00:00:00",
                     "accountNumber": "0001234", "subAccount": None, "code": None,
                     "content": "test", "transferType": "in", "transferAmount": 100000,
                     "accumulated": 0, "referenceCode": "TESTREF", "description": "test"},
               note="webhook (test payload)")

    # ------------------------------------------------------------------
    # Run all + summary
    # ------------------------------------------------------------------

    def _login(self, email: str, password: str) -> Optional[str]:
        """Login and return token, or None if failed."""
        try:
            r = self._req("POST", "/api/users/login",
                          json={"email": email, "password": password})
            if r.status_code == 200:
                d = r.json().get("data", r.json())
                token = d.get("token") or d.get("accessToken")
                user  = d.get("user") or {}
                if not self.test_user_id:
                    self.test_user_id = user.get("id") or user.get("userId")
                return token
        except Exception:
            pass
        return None

    def run(self, user_email: Optional[str] = None, user_password: Optional[str] = None,
            admin_email: Optional[str] = None, admin_password: Optional[str] = None):
        print(f"\n{BOLD}{CYAN}{'='*62}{RESET}")
        print(f"{BOLD}{CYAN}  PRN232-Backend  ·  Swagger API Test Suite{RESET}")
        print(f"{BOLD}{CYAN}  {self.base_url}{RESET}")
        print(f"{BOLD}{CYAN}  {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}{RESET}")
        print(f"{BOLD}{CYAN}{'='*62}{RESET}")

        self._bootstrap_ids()
        print(f"\n  {DIM}product_id={self.product_id}  "
              f"brand_id={self.brand_id}  "
              f"category_id={self.category_id}{RESET}")

        # Pre-supply user token from provided credentials (skip auto-register)
        if user_email and user_password:
            token = self._login(user_email, user_password)
            if token:
                self.user_token = token
                print(f"\n  {GREEN}User token obtained  ({user_email}){RESET}")
            else:
                print(f"\n  {RED}User login failed  ({user_email}){RESET}")

        # Pre-supply admin token
        if admin_email and admin_password:
            token = self._login(admin_email, admin_password)
            if token:
                self.admin_token = token
                print(f"  {GREEN}Admin token obtained  ({admin_email}){RESET}")
            else:
                print(f"  {RED}Admin login failed  ({admin_email}){RESET}")

        self.test_health()
        self.test_store()
        self.test_brands()
        self.test_categories()
        self.test_products()
        self.test_warranty_public()
        self.test_roles()
        self.test_auth()
        self.test_user_authenticated()
        self.test_admin()
        self.test_security()
        self.test_payment_public()

        # Summary
        total  = len(self.results)
        passed = sum(1 for r in self.results if r["ok"])
        failed = total - passed

        print(f"\n{BOLD}{'='*62}")
        print(f"  RESULTS   total={total}  "
              f"{GREEN}passed={passed}{RESET}{BOLD}  "
              f"{RED if failed else GREEN}failed={failed}{RESET}{BOLD}")
        print(f"  Pass rate: {passed/total*100:.1f}%" if total else "")
        print(f"{'='*62}{RESET}")

        server_errs = [r for r in self.results if not r["ok"] and r.get("server_err")]
        logic_fails = [r for r in self.results if not r["ok"] and not r.get("server_err")]

        if server_errs:
            print(f"\n{YELLOW}⚡ Server Errors (HTTP 500) — likely DB/service issue on server:{RESET}")
            for r in server_errs:
                print(f"  ⚡ {r['method']:<7}{r['path']}")
                print(f"      expected {r['want']}, got 500")

        if logic_fails:
            print(f"\n{RED}✗ Logic Failures (wrong status code):{RESET}")
            for r in logic_fails:
                print(f"  ✗ {r['method']:<7}{r['path']}")
                print(f"      expected {r['want']}, got {r['got']}"
                      + (f"  ({r['note']})" if r["note"] else ""))

        if server_errs:
            print(f"\n{YELLOW}NOTE: {len(server_errs)} endpoint(s) returned HTTP 500.{RESET}")
            print(f"{YELLOW}This typically indicates a server-side issue (e.g. DB connection){RESET}")
            print(f"{YELLOW}rather than a bug in the test logic. Check Railway logs.{RESET}")

        return failed == 0


# ---------------------------------------------------------------------------
# Entry point
# ---------------------------------------------------------------------------

if __name__ == "__main__":
    parser = argparse.ArgumentParser(
        description="PRN232-Backend Swagger API test runner"
    )
    parser.add_argument("--base-url",        default=BASE_URL)
    parser.add_argument("--user-email",      default=None, help="Existing user email")
    parser.add_argument("--user-password",   default=None, help="Existing user password")
    parser.add_argument("--admin-email",     default=None, help="Admin email")
    parser.add_argument("--admin-password",  default=None, help="Admin password")
    args = parser.parse_args()

    tester = ApiTester(args.base_url)
    ok = tester.run(
        user_email=args.user_email,
        user_password=args.user_password,
        admin_email=args.admin_email,
        admin_password=args.admin_password,
    )
    sys.exit(0 if ok else 1)
