# Invoice Runtime Flow - Test Report

## 1) Scope
- Feature: Runtime invoice preview from order data.
- Endpoint expected: `POST /api/invoices/orders/{orderId}/preview`
- Constraint: No database schema change, no persistent invoice write.

## 2) Test Environment
- Target backend (deploy): `https://prn232-backend-production.up.railway.app`
- Test date: 2026-04-01
- Test account: Admin (`lethanhcong.work@gmail.com`)
- Test order id (real data): `99`

## 3) Test Inputs
- Payload (PERSONAL invoice):
```json
{
  "invoiceType": "PERSONAL",
  "taxCode": "8790693081",
  "personalName": "Le Quoc Khanh",
  "billingAddress": "Quan Tan Phu, TP Ho Chi Minh"
}
```

## 4) Execution Result (Real Deploy Data)
1. Login API (`POST /api/users/login`): reachable.
2. Invoice API call on deploy (`POST /api/invoices/orders/99/preview`): `404 Not Found`.

## 5) Conclusion
- Business implementation exists in source code (controller + service), but deploy environment does not expose the new invoice endpoint yet.
- End-to-end validation on real deploy data is currently blocked by deployment gap, not by request payload or test account.

## 6) Safety / Data Cleanliness
- No write operation succeeded for invoice flow on deploy.
- No new data was created in deploy database from this test.
- Demo data remains clean for invoice flow testing.

## 7) Retest Plan After Deploy
1. Deploy current branch containing invoice runtime endpoint.
2. Re-run test with the same admin account and real order id.
3. Verify response contract fields:
   - `invoiceNumber`, `invoiceType`, `taxCode`, `billingName`, `billingAddress`
   - order snapshot and line items
4. Verify authorization cases:
   - Admin/Staff can preview any order.
   - Customer can preview only own order.
5. Record pass/fail and attach response sample.
