# 03. Cart and Checkout Core Report

## 1) Objective
- Validate add/update/remove cart and checkout flow.

## 2) Environment
- API Base URL: https://prn232-backend-production.up.railway.app
- Test Date: 2026-04-01
- Tester: Copilot + Cong

## 3) Test Data Snapshot (Before)
- Existing user used: congltse183504@fpt.edu.vn
- Existing product IDs used: 30 (existing product, no new product created)
- Initial cart state: empty cart (signature: empty)
- No new product created: Yes

## 4) Test Cases
| ID | Scenario | Input | Expected | Actual | Result |
|---|---|---|---|---|---|
| CART-01 | Add to cart | productId=30, qty=1 | 201/200 success | success=True | Pass |
| CART-02 | Update cart item qty | cartItemId=106, qty=2 | 200 success | success=True | Pass |
| CART-03 | Remove cart item | cartItemId=106 | 200 success | success=True | Pass |
| CHECK-01 | Checkout safety run (no create) | COD payload with empty cart | 400, no new order | 400 and top order unchanged (99->99) | Pass (clean-base mode) |

## 5) Data Changes During Test
- Created records (cart/order/payment): temporary cart item created during CART-01
- Updated records: temporary cart item quantity updated in CART-02
- Deleted records: temporary cart item removed in CART-03

## 6) Cleanup Evidence
- Cleanup actions: removed temporary cart item after update test
- Remaining temporary data: No
- Verification notes:
	- Cart signature before: empty
	- Cart signature after: empty
	- Restored check: True
	- Order top before checkout attempt: 99
	- Order top after checkout attempt: 99
	- Order unchanged check: True

## 7) Conclusion
- Pass rate: 4/4 in clean-base scope
- Risks: checkout success path was intentionally not executed to avoid persistent order creation in demo-clean environment
- Next action: proceed to payment core on existing orders only (no new product, no irreversible data creation)
