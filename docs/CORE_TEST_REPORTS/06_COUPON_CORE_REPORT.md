# 06. Coupon Core Report

## 1) Objective

- Validate coupon apply and core validation rules.

## 2) Environment

- API Base URL: https://prn232-backend-production.up.railway.app
- Test Date: 2026-04-01
- Tester: Copilot + Cong

## 3) Test Data Snapshot (Before)

- Existing coupon codes used: STUDENT15, WELCOME2025, EXPIRED, MEGA100K, INVALID123
- Coupon usage counts before: not modified in this run (read-only validation calls)
- Customer cart before test: count=0, signature=empty

## 4) Test Cases

| ID     | Scenario              | Input                 | Expected         | Actual | Result |
| ------ | --------------------- | --------------------- | ---------------- | ------ | ------ |
| CPN-01 | Apply valid coupon    | code + eligible order | discount applied | No active valid coupon found on deploy sample set | N/A |
| CPN-02 | Apply expired coupon  | code                  | rejected         | 400 with expired message for STUDENT15/WELCOME2025/EXPIRED/MEGA100K | Pass |
| CPN-03 | Apply below min order | code + amount         | rejected         | Blocked by same reason as CPN-01 (all sampled coupons expired before min-order validation) | N/A |

## 5) Data Changes During Test

- Created records: none
- Updated records (coupon used_count): none
- Deleted records: none

## 6) Cleanup Evidence

- Cleanup actions: not required (validate-coupon calls only)
- Remaining temporary data: No
- Verification: cart signature before and after test remained empty; restored=True

## 7) Conclusion

- Pass rate: 1/1 executed, 2 cases N/A
- Risks: coupon baseline on deploy is fully expired, so valid/min-order scenarios need either active coupon seed or controlled staging data
- Next action: proceed with invoice runtime flow (already executable on deploy)
