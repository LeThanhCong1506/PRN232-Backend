# 04. Payment Core Report

## 1) Objective

- Validate COD and online payment success/fail/expired core scenarios.

## 2) Environment

- API Base URL: https://prn232-backend-production.up.railway.app
- Test Date: 2026-04-01
- Tester: Copilot + Cong

## 3) Test Data Snapshot (Before)

- Order IDs used: 99, 95, 98, 97
- Payment records baseline:
	- Order 99: COD / PENDING
	- Order 95: SEPAY / COMPLETED
	- Order 98: COD / EXPIRED
	- Order 97: COD / FAILED

## 4) Test Cases

| ID     | Scenario                    | Input                 | Expected          | Actual | Result |
| ------ | --------------------------- | --------------------- | ----------------- | ------ | ------ |
| PAY-01 | COD order payment status    | GET admin order detail (orderId=99) | status consistent | paymentMethod=COD, paymentStatus=PENDING | Pass |
| PAY-02 | Online payment success      | GET admin order detail (orderId=95) | completed         | paymentMethod=SEPAY, paymentStatus=COMPLETED | Pass |
| PAY-03 | Online payment fail/expired | GET admin order detail (orderId=98,97) + role guard check | failed/expired    | 98=EXPIRED, 97=FAILED, customer PUT payment-status returns 403 | Pass |

## 5) Data Changes During Test

- Created records: none
- Updated records: none
- Deleted records: none

## 6) Cleanup Evidence

- Cleanup actions: not required (read-only payment verification + forbidden write attempt)
- Remaining temporary data: No

## 7) Conclusion

- Pass rate: 3/3
- Risks: no callback simulation was executed on deploy to keep base clean
- Next action: continue with admin confirm guard tests in non-destructive mode
