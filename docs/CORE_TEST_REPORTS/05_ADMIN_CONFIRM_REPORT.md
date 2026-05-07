# 05. Admin Confirm Order Report

## 1) Objective

- Validate admin/staff order confirm flow and transition rules.

## 2) Environment

- API Base URL: https://prn232-backend-production.up.railway.app
- Test Date: 2026-04-01
- Tester: Copilot + Cong

## 3) Test Data Snapshot (Before)

- Order IDs in PENDING used: none found in sampled page (page 1, size 20)
- Status before test:
	- Order 99: CONFIRMED

## 4) Test Cases

| ID     | Scenario                        | Input               | Expected               | Actual | Result |
| ------ | ------------------------------- | ------------------- | ---------------------- | ------ | ------ |
| ADM-01 | Admin confirms pending order    | PUT /api/admin/orders/{id}/status, newStatus=CONFIRMED | 200 + status updated   | Skipped in this run to preserve clean demo base and avoid irreversible status mutation | N/A |
| ADM-02 | Customer calls admin confirm    | PUT /api/admin/orders/99/status, newStatus=CONFIRMED | 403                    | 403 | Pass |
| ADM-03 | Confirm already confirmed order | PUT /api/admin/orders/99/status, newStatus=CONFIRMED | 400 invalid transition | 400 | Pass |

## 5) Data Changes During Test

- Created records: none
- Updated records (order status, timestamps): none
- Deleted records: none

## 6) Cleanup Evidence

- Cleanup actions: not required (only forbidden/invalid transition checks)
- Remaining temporary data: No
- Note if rollback is blocked by business rule: confirmed order 99 remained CONFIRMED after test checks

## 7) Conclusion

- Pass rate: 2/2 executed (1 case intentionally skipped)
- Risks: full happy-path confirm on PENDING is deferred in clean-base mode because state rollback is restricted by business rule
- Next action: continue coupon core using non-destructive checks first
