# 01. Auth and Role Smoke Report

## 1) Objective
- Validate login and role-based access for core endpoints.

## 2) Environment
- API Base URL: https://prn232-backend-production.up.railway.app
- Test Date: 2026-04-01
- Tester: Copilot + Cong

## 3) Test Data Snapshot (Before)
- Existing accounts used:
	- Admin: lethanhcong.work@gmail.com
	- Customer: congltse183504@fpt.edu.vn
- No new product created: Yes
- Baseline counts (if relevant): read-only auth tests, no record creation expected

## 4) Test Cases
| ID | Scenario | Input | Expected | Actual | Result |
|---|---|---|---|---|---|
| AUTH-01 | Admin login | email/password | 200 + token | 200, role=Admin, token not empty | Pass |
| AUTH-02 | Customer login | email/password | 200 + token | 200, role=Customer, token not empty | Pass |
| AUTH-03 | Invalid token | GET /api/order/my-orders | 401 | 401 | Pass |
| AUTH-04 | Customer calls admin endpoint | GET /api/admin/orders | 403 | 403 | Pass |

## 5) Data Changes During Test
- Created records: none
- Updated records: none
- Deleted records: none

## 6) Cleanup Evidence
- Cleanup actions: not required (read-only calls)
- Remaining temporary data: No
- Verification query/result: all scenarios used login and GET endpoints only

## 7) Conclusion
- Pass rate: 4/4
- Risks: none critical in auth role smoke scope
- Next action: proceed to cart/checkout test with strict cleanup logging
