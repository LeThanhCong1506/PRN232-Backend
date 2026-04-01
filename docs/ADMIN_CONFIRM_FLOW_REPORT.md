# Admin Confirm Flow Report

## 1) Muc tieu

Danh gia business logic cua luong Admin Confirm don hang (`PENDING -> CONFIRMED`) va ghi lai ket qua kiem thu de doi team doi chieu.

## 2) Endpoint va pham vi

- Endpoint: `PUT /api/admin/orders/{id}/status`
- Controller: `MV.PresentationLayer/Controllers/AdminOrderController.cs`
- Service chinh: `MV.ApplicationLayer/Services/AdminOrderService.cs`
- Pham vi report nay: luong Admin Confirm (khong bao gom luong In Stock theo yeu cau).

## 3) Tom tat luong xu ly hien tai

1. Xac thuc va phan quyen

- Endpoint yeu cau role `Admin`.
- Neu khong lay duoc `adminUserId` tu claim thi tra `Unauthorized`.

2. Validate transition trang thai

- Service dung bang `AllowedTransitions`.
- `PENDING` duoc phep chuyen sang `CONFIRMED` hoac `CANCELLED`.
- Transition khong hop le se bi tu choi.

3. Xu ly side effects khi `CONFIRMED`

- Kiem tra payment status khong duoc la `FAILED`.
- Set `ConfirmedAt` va `ConfirmedBy`.
- Neu don co serial tracking thi cap nhat `ProductInstance` sang `SOLD` theo `OrderItemIds`.

4. Tinh nhat quan du lieu

- Toan bo xu ly status nam trong transaction + execution strategy.
- Neu loi trong qua trinh xu ly thi rollback.

5. Sau khi commit

- Gui thong bao realtime cho user (ngoai transaction, co log warning neu fail notify).

## 4) Danh sach quy tac business duoc ghi nhan

- Chi `Admin` moi duoc doi status don trong endpoint admin.
- Chi cho phep transition theo state machine.
- Khong cho confirm neu payment `FAILED`.
- Confirm thanh cong phai co dau vet nguoi xac nhan (`ConfirmedBy`) va thoi gian (`ConfirmedAt`).
- Xu ly side effect serial instance khi confirm.

## 5) Ket qua danh gia

### Ket luan tong quan

Business logic cua luong Admin Confirm **on ve nghiep vu** (auth, transition, side effects, transaction).

### Van de da gap trong qua trinh test

- Co gap loi do **lech schema DB test** so voi model code hien tai (khong phai loi business rule cua luong confirm).
- Khi dong bo lai schema test cho phu hop model, luong confirm chay thanh cong.

## 6) Ket qua test da ghi nhan

1. Khong token -> endpoint admin: bi chan (mong doi).
2. Token user khong phai admin -> bi chan (mong doi).
3. Confirm don `PENDING`:

- Truoc khi dong bo schema test: that bai do loi schema.
- Sau khi dong bo schema test: thanh cong, trang thai cap nhat dung.

4. Confirm lap lai tren don da `CONFIRMED`: bi tu choi do transition khong hop le (mong doi).

## 7) Rui ro con lai

- Moi truong test de phat sinh false negative neu schema DB khong dong bo voi code.
- Can co migration/chuan hoa schema test truoc khi chay regression de tranh nham lan voi loi nghiep vu.

## 8) Khuyen nghi tiep theo

1. Chot 1 script migration cho `ecommerce_test_db` de khoi tao test environment on dinh.
2. Tu dong hoa bo smoke test cho endpoint admin status update (401/403/200/invalid transition).
3. Chuyen sang test luong tiep theo theo ke hoach:

- Xuat hoa don ca nhan/cong ty kem MST.
- Admin manage ma giam gia.
- Quan ly serial number tren toan bo nghiep vu san pham.

## 9) Ket luan de bao cao

- Neu xet rieng business logic cua code luong Admin Confirm: **Dat**.
- Neu xet readiness de test/CI: **Can khoa schema test de tranh loi moi truong**.
