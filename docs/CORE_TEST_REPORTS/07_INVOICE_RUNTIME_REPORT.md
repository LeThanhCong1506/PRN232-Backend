# 07. Invoice Runtime Report

## 1) Objective

- Validate runtime invoice preview from existing order data (no DB schema change).

## 2) Environment

- API Base URL: https://prn232-backend-production.up.railway.app
- Test Date: 2026-04-01
- Tester: Copilot + Cong

## 3) Test Data Snapshot (Before)

- Order IDs used: 99 (owned test order), 98 (foreign order for customer)
- Tax data used:
  - Tax code: 8790693081
  - PERSONAL name: Le Quoc Khanh
  - COMPANY name: Le Quoc Khanh Co
  - Representative: Le Quoc Khanh
- No new product created: Yes

## 4) Test Cases

| ID     | Scenario                          | Input                                  | Expected              | Actual | Result |
| ------ | --------------------------------- | -------------------------------------- | --------------------- | ------ | ------ |
| INV-01 | PERSONAL invoice                  | personalName + taxCode                 | 200 + invoice payload | 200 (success=True) on order 99 | Pass |
| INV-02 | COMPANY invoice                   | companyName + representative + taxCode | 200 + invoice payload | 200 (success=True) on order 99 | Pass |
| INV-03 | Unauthorized access order invoice | foreign orderId                        | 403                   | 403 on order 98 with customer token | Pass |

## 5) Data Changes During Test

- Created records: none
- Updated records: none
- Deleted records: none

## 6) Cleanup Evidence

- Cleanup actions: not required (runtime preview only, no persistence)
- Remaining temporary data: No

## 7) Conclusion

- Pass rate: 3/3
- Risks: none critical in runtime preview scope
- Next action: continue serial tracking and warranty/return core with the same clean-base policy
