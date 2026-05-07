# 02. Catalog and Product Detail Report

## 1) Objective

- Validate product listing/filter/pagination and product detail.

## 2) Environment

- API Base URL: https://prn232-backend-production.up.railway.app
- Test Date: 2026-04-01
- Tester: Copilot + Cong

## 3) Test Data Snapshot (Before)

- Product IDs used (existing only): 30
- No new product created: Yes

## 4) Test Cases

| ID     | Scenario                        | Input                                                        | Expected          | Actual                         | Result |
| ------ | ------------------------------- | ------------------------------------------------------------ | ----------------- | ------------------------------ | ------ |
| CAT-01 | Product list default            | GET /api/product?pageNumber=1&pageSize=10                    | 200 + paged list  | 200, items=10                  | Pass   |
| CAT-02 | Filter by search/brand/category | GET /api/product?searchTerm=arduino&pageNumber=1&pageSize=10 | correct filtering | 200, filtered items=4          | Pass   |
| CAT-03 | Product detail                  | GET /api/product/30                                          | 200 + full detail | 200, name=Robotics Starter Kit | Pass   |

## 5) Data Changes During Test

- Created records: none
- Updated records: none
- Deleted records: none

## 6) Cleanup Evidence

- Cleanup actions: not required (read-only catalog endpoints)
- Remaining temporary data: No

## 7) Conclusion

- Pass rate: 3/3
- Risks: response field totalItems is empty in current payload, monitor for FE pagination dependency
- Next action: proceed to cart/checkout with full before/after data snapshot and cleanup proof
