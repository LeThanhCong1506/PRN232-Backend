# 📖 PAYMENT FLOW - Hướng dẫn dành cho Frontend

> **Phiên bản:** v1.0  
> **Ngày cập nhật:** 2026-02-23  
> **Base URL:** `http://localhost:5000` (hoặc domain production)

---

## 📋 Mục lục

1. [Tổng quan hệ thống](#1-tổng-quan-hệ-thống)
2. [Các trạng thái (Enums)](#2-các-trạng-thái-enums)
3. [Luồng thanh toán COD](#3-luồng-thanh-toán-cod)
4. [Luồng thanh toán SePay (Online Banking)](#4-luồng-thanh-toán-sepay-online-banking)
5. [Chi tiết API Endpoints](#5-chi-tiết-api-endpoints)
6. [Sơ đồ luồng (Flow Diagram)](#6-sơ-đồ-luồng-flow-diagram)
7. [Xử lý Error & Edge Cases](#7-xử-lý-error--edge-cases)
8. [Code mẫu Frontend (React)](#8-code-mẫu-frontend-react)
9. [Câu hỏi thường gặp (FAQ)](#9-câu-hỏi-thường-gặp-faq)

---

## 1. Tổng quan hệ thống

Hệ thống có **2 phương thức thanh toán**:

| Phương thức | Mô tả | Cần xử lý gì ở FE? |
|-------------|--------|---------------------|
| **COD** | Thanh toán khi nhận hàng | Chỉ cần gọi Checkout → hiển thị kết quả |
| **SEPAY** | Thanh toán online qua SePay Gateway | Checkout → Hiển thị QR Code → Polling trạng thái **HOẶC** Redirect SePay Checkout |

### Kiến trúc tổng thể

```
┌──────────┐     ┌──────────────┐     ┌──────────┐     ┌──────────┐
│ Frontend │────▶│  Backend API │────▶│ Database │     │  SePay   │
│  (React) │◀────│   (.NET 8)   │◀────│(PostgreSQL)│◀────│ Gateway  │
└──────────┘     └──────────────┘     └──────────┘     └──────────┘
                        │                                     │
                        │◀──── Webhook / Success Callback ────│
```

---

## 2. Các trạng thái (Enums)

### 2.1. Payment Method
| Giá trị | Mô tả |
|---------|-------|
| `COD` | Thanh toán khi nhận hàng |
| `SEPAY` | Thanh toán online qua SePay |

### 2.2. Payment Status
| Giá trị | Mô tả | Khi nào xảy ra? |
|---------|-------|-----------------|
| `PENDING` | Chờ thanh toán | Ngay khi tạo order |
| `COMPLETED` | Đã thanh toán thành công | SePay webhook xác nhận / COD giao hàng thành công / Admin verify thủ công |
| `FAILED` | Thanh toán thất bại | Khi order bị hủy |
| `EXPIRED` | Hết hạn thanh toán | Quá 30 phút không thanh toán (chỉ SEPAY) |

### 2.3. Order Status
| Giá trị | Mô tả | Chuyển từ trạng thái nào? |
|---------|-------|--------------------------|
| `PENDING` | Chờ xử lý | *(trạng thái khởi tạo)* |
| `CONFIRMED` | Đã xác nhận | `PENDING` → `CONFIRMED` |
| `SHIPPED` | Đang giao hàng | `CONFIRMED` → `SHIPPED` |
| `DELIVERED` | Đã giao thành công | `SHIPPED` → `DELIVERED` |
| `CANCELLED` | Đã hủy | `PENDING` → `CANCELLED` |

### 2.4. Mối quan hệ Order Status ⟷ Payment Status

```
Tạo Order:
  ├─ Order: PENDING
  └─ Payment: PENDING

Thanh toán thành công (SEPAY):
  ├─ Payment: PENDING → COMPLETED
  └─ Order: PENDING → CONFIRMED  (tự động)

Giao hàng thành công (COD):
  ├─ Order: SHIPPED → DELIVERED
  └─ Payment: PENDING → COMPLETED  (tự động)

Hủy đơn:
  ├─ Order: PENDING → CANCELLED
  └─ Payment: PENDING → FAILED

Hết hạn (SEPAY, 30 phút):
  ├─ Payment: PENDING → EXPIRED
  └─ Order: PENDING → CANCELLED  (tự động + hoàn stock + coupon)
```

---

## 3. Luồng thanh toán COD

### Flow đơn giản

```
User chọn COD ──▶ Gọi POST /api/Order/checkout ──▶ Nhận kết quả ──▶ Hiển thị "Đặt hàng thành công"
```

### Bước chạy:

| Bước | Hành động FE | API | Mô tả |
|------|-------------|-----|-------|
| 1 | User bấm "Đặt hàng" | `POST /api/Order/checkout` | Gửi `paymentMethod: "COD"` |
| 2 | Nhận response | *(không cần gọi thêm)* | Hiển thị trang thành công |

### Response mẫu khi checkout COD:

```json
{
  "success": true,
  "message": "Order placed successfully.",
  "data": {
    "orderId": 42,
    "orderNumber": "ORD20260223001",
    "status": "PENDING",
    "totalAmount": 505000,
    "paymentMethod": "COD",
    "paymentStatus": "PENDING",
    "paymentReference": null,
    "qrCodeUrl": null,
    "checkoutUrl": null,
    "paymentExpiredAt": null,
    "message": "Order successful. Payment upon delivery."
  }
}
```

> ⚠️ **Lưu ý:** Với COD, `checkoutUrl`, `qrCodeUrl`, `paymentReference`, `paymentExpiredAt` sẽ là `null`.

---

## 4. Luồng thanh toán SePay (Online Banking)

### ⭐ Có 2 cách triển khai phía FE:

---

### Cách 1: Hiển thị QR Code trực tiếp + Polling (Khuyến nghị ✅)

```
User chọn SEPAY ──▶ POST /api/Order/checkout ──▶ Nhận QR Code URL
                                                         │
                                           ┌─────────────▼──────────────┐
                                           │ FE hiển thị QR Code        │
                                           │ + Countdown 30 phút       │
                                           │ + Polling GET /status mỗi  │
                                           │   3-5 giây                 │
                                           └─────────────┬──────────────┘
                                                         │
                              ┌───────────────────────────┼───────────────────────┐
                              │                           │                       │
                    PaymentStatus                PaymentStatus              Hết 30 phút
                    = "COMPLETED"                vẫn "PENDING"             (hoặc EXPIRED)
                              │                           │                       │
                    Redirect ──▶                 Tiếp tục polling         Hiển thị hết hạn
                    /payment/success                                      ──▶ Đặt lại đơn
```

**Các bước chi tiết:**

| Bước | Hành động FE | API | Chi tiết |
|------|-------------|-----|---------|
| 1 | User bấm "Đặt hàng" | `POST /api/Order/checkout` | Gửi `paymentMethod: "SEPAY"` |
| 2 | Nhận response chứa `qrCodeUrl` | — | Hiển thị QR Code bằng `<img>` tag |
| 3 | Bắt đầu polling | `GET /api/Payment/{orderId}/status` | Gọi mỗi **3-5 giây** |
| 4a | Nếu `isPaid = true` | — | 🎉 Chuyển trang "Thanh toán thành công" |
| 4b | Nếu `remainingSeconds = 0` hoặc `paymentStatus = "EXPIRED"` | — | ⏰ Hiển thị "Hết hạn" |
| 4c | Nếu vẫn `PENDING` | — | Tiếp tục polling... |

---

### Cách 2: Redirect đến SePay Checkout Page

```
User chọn SEPAY ──▶ POST /api/Order/checkout ──▶ Nhận checkoutUrl
                                                         │
                                              FE redirect/window.open
                                              đến checkoutUrl + query params
                                                         │
                                              ┌──────────▼──────────┐
                                              │ SePay Checkout Page │
                                              │ (user thanh toán)   │
                                              └──────────┬──────────┘
                                                         │
                              ┌───────────────────────────┼──────────────────────────┐
                              │                           │                          │
                         Thành công                    Lỗi                        Hủy
                              │                           │                          │
                   Redirect về FE                 Redirect về FE             Redirect về FE
                   successUrl?status=success      errorUrl                   cancelUrl
                   &orderId=42                    
                   &orderNumber=ORD...            
```

**Các bước chi tiết:**

| Bước | Hành động FE | API / URL | Chi tiết |
|------|-------------|-----------|---------|
| 1 | User bấm "Đặt hàng" | `POST /api/Order/checkout` | Gửi `paymentMethod: "SEPAY"` |
| 2 | Nhận response chứa `checkoutUrl` | — | Ví dụ: `/api/Payment/42/checkout` |
| 3 | Redirect đến checkout URL | `GET /api/Payment/{orderId}/checkout?successUrl=...&errorUrl=...&cancelUrl=...` | FE mở URL này bằng `window.location.href` hoặc `window.open()` |
| 4 | SePay redirect về FE | — | Query params: `?status=success&orderId=42&orderNumber=ORD20260223001` |

**Query params khi gọi checkout URL:**

| Param | Bắt buộc? | Mô tả | Ví dụ |
|-------|-----------|-------|-------|
| `successUrl` | Không (default: `http://localhost:3000/payment/success`) | URL FE redirect khi thanh toán thành công | `http://localhost:3000/payment/success` |
| `errorUrl` | Không (default: `http://localhost:3000/payment/error`) | URL FE redirect khi thanh toán lỗi | `http://localhost:3000/payment/error` |
| `cancelUrl` | Không (default: `http://localhost:3000/payment/cancel`) | URL FE redirect khi user hủy thanh toán | `http://localhost:3000/payment/cancel` |

**Ví dụ URL đầy đủ:**
```
http://localhost:5000/api/Payment/42/checkout?successUrl=http://localhost:3000/payment/success&errorUrl=http://localhost:3000/payment/error&cancelUrl=http://localhost:3000/payment/cancel
```

**Query params FE nhận khi SePay redirect về `successUrl`:**
```
http://localhost:3000/payment/success?status=success&orderId=42&orderNumber=ORD20260223001
```

---

## 5. Chi tiết API Endpoints

### 5.1. `POST /api/Order/checkout` — Tạo đơn hàng (Checkout)

> **Auth:** Bearer Token (bắt buộc)

**Request Body:**
```json
{
  "paymentMethod": "SEPAY",          // "COD" hoặc "SEPAY" (bắt buộc)
  "couponCode": "SALE10",            // Mã giảm giá (tùy chọn)
  "customerName": "Nguyễn Văn A",    // Tên khách hàng (bắt buộc, 2-100 ký tự)
  "customerEmail": "a@email.com",    // Email (bắt buộc, đúng format)
  "customerPhone": "0912345678",     // SĐT (bắt buộc, 10-15 số)
  "province": "Hồ Chí Minh",        // Tỉnh/Thành phố (bắt buộc)
  "district": "Quận 1",             // Quận/Huyện (bắt buộc)
  "ward": "Phường Bến Nghé",        // Phường/Xã (bắt buộc)
  "streetAddress": "123 Nguyễn Huệ", // Địa chỉ (bắt buộc, tối đa 200 ký tự)
  "notes": "Giao giờ hành chính"     // Ghi chú (tùy chọn, tối đa 500 ký tự)
}
```

**Response thành công (200 OK) — SEPAY:**
```json
{
  "success": true,
  "message": "Order placed successfully.",
  "data": {
    "orderId": 42,
    "orderNumber": "ORD20260223001",
    "status": "PENDING",
    "totalAmount": 505000,
    "paymentMethod": "SEPAY",
    "paymentStatus": "PENDING",
    "paymentReference": "SEVQR20260223001",
    "qrCodeUrl": "https://qr.sepay.vn/img?bank=MBBank&acc=0123456789&template=compact&amount=505000&des=SEVQR20260223001",
    "checkoutUrl": "/api/Payment/42/checkout",
    "paymentExpiredAt": "2026-02-23T10:43:20+07:00",
    "message": "Order successful. Redirect to checkoutUrl to complete payment."
  }
}
```

**Response thành công (200 OK) — COD:**
```json
{
  "success": true,
  "message": "Order placed successfully.",
  "data": {
    "orderId": 43,
    "orderNumber": "ORD20260223002",
    "status": "PENDING",
    "totalAmount": 505000,
    "paymentMethod": "COD",
    "paymentStatus": "PENDING",
    "paymentReference": null,
    "qrCodeUrl": null,
    "checkoutUrl": null,
    "paymentExpiredAt": null,
    "message": "Order successful. Payment upon delivery."
  }
}
```

**Response lỗi (400):**
```json
{
  "success": false,
  "message": "Shopping cart is empty.",
  "data": null
}
```

**Các message lỗi có thể trả về:**
- `"Invalid payment method. Only COD or SEPAY will be accepted."`
- `"Shopping cart is empty."`
- `"Product 'Tên SP' only has X left in stock, you requested Y."`
- `"The coupon code does not exist."`
- `"The coupon code has expired or has not started."`
- `"The coupon code has expired."` (hết lượt sử dụng)
- `"Minimum order value Xđ to use the coupon"`

---

### 5.2. `GET /api/Payment/{orderId}/status` — Polling trạng thái thanh toán

> **Auth:** Bearer Token (bắt buộc)  
> **Mục đích:** FE gọi API này liên tục (mỗi 3-5 giây) để kiểm tra user đã thanh toán chưa.

**Request:**
```
GET /api/Payment/42/status
Authorization: Bearer <token>
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": null,
  "data": {
    "orderId": 42,
    "orderNumber": "ORD20260223001",
    "paymentMethod": "SEPAY",
    "paymentStatus": "PENDING",
    "amount": 505000,
    "receivedAmount": null,
    "qrCodeUrl": "https://qr.sepay.vn/img?bank=MBBank&acc=...",
    "paymentReference": "SEVQR20260223001",
    "expiredAt": "2026-02-23T10:43:20+07:00",
    "isPaid": false,
    "remainingSeconds": 1423
  }
}
```

**Các trường quan trọng FE cần dùng:**

| Trường | Kiểu | Mô tả | Cách dùng |
|--------|------|-------|-----------|
| `isPaid` | `boolean` | Đã thanh toán chưa? | Nếu `true` → chuyển trang thành công |
| `paymentStatus` | `string` | Trạng thái thanh toán | `PENDING`, `COMPLETED`, `FAILED`, `EXPIRED` |
| `remainingSeconds` | `int` | Số giây còn lại | Hiển thị countdown timer |
| `qrCodeUrl` | `string?` | URL ảnh QR Code | Dùng cho `<img src="...">` |
| `paymentReference` | `string?` | Nội dung chuyển khoản | Hiển thị cho user copy |

**Logic polling phía FE:**
```
if (isPaid === true || paymentStatus === "COMPLETED") → ✅ Thanh toán thành công, STOP polling
if (paymentStatus === "EXPIRED" || remainingSeconds <= 0) → ⏰ Hết hạn, STOP polling
if (paymentStatus === "FAILED") → ❌ Thất bại, STOP polling
else → 🔄 Tiếp tục polling sau 3-5 giây
```

---

### 5.3. `GET /api/Payment/{orderId}/checkout` — Redirect đến SePay Checkout

> **Auth:** Không cần (AllowAnonymous)  
> **Mục đích:** FE mở URL này → Backend auto-redirect đến trang thanh toán SePay

**Request (browser redirect):**
```
GET /api/Payment/42/checkout?successUrl=http://localhost:3000/payment/success&errorUrl=http://localhost:3000/payment/error&cancelUrl=http://localhost:3000/payment/cancel
```

**Kết quả:** Browser sẽ tự động redirect đến trang thanh toán SePay.

**Lỗi có thể trả về:**
- `404`: Đơn hàng không tồn tại
- `400`: Không phải phương thức SEPAY / Đã thanh toán / Hết hạn

---

### 5.4. `GET /api/Order/{id}` — Xem chi tiết đơn hàng

> **Auth:** Bearer Token (bắt buộc)

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "orderId": 42,
    "orderNumber": "ORD20260223001",
    "status": "PENDING",
    "customerName": "Nguyễn Văn A",
    "customerEmail": "a@email.com",
    "customerPhone": "0912345678",
    "province": "Hồ Chí Minh",
    "district": "Quận 1",
    "ward": "Phường Bến Nghé",
    "streetAddress": "123 Nguyễn Huệ",
    "shippingAddress": "123 Nguyễn Huệ, Phường Bến Nghé, Quận 1, Hồ Chí Minh",
    "subtotalAmount": 500000,
    "shippingFee": 5000,
    "discountAmount": 0,
    "totalAmount": 505000,
    "notes": "Giao giờ hành chính",
    "createdAt": "2026-02-23T10:13:20+07:00",
    "updatedAt": "2026-02-23T10:13:20+07:00",
    "confirmedAt": null,
    "shippedAt": null,
    "deliveredAt": null,
    "cancelledAt": null,
    "cancelReason": null,
    "trackingNumber": null,
    "carrier": null,
    "expectedDeliveryDate": null,
    "items": [
      {
        "orderItemId": 1,
        "productId": 10,
        "productName": "STEM Robot Kit",
        "productSku": "STEM-RBT-001",
        "productImageUrl": "https://example.com/robot.jpg",
        "quantity": 2,
        "unitPrice": 250000,
        "discountAmount": null,
        "subtotal": 500000
      }
    ],
    "payment": {
      "paymentId": 30,
      "paymentMethod": "SEPAY",
      "status": "PENDING",
      "amount": 505000,
      "receivedAmount": null,
      "paymentReference": "SEVQR20260223001",
      "transactionId": null,
      "qrCodeUrl": "https://qr.sepay.vn/img?...",
      "paymentDate": null,
      "expiredAt": "2026-02-23T10:43:20+07:00"
    },
    "coupon": null
  }
}
```

---

### 5.5. `PUT /api/Order/{id}/cancel` — Hủy đơn hàng

> **Auth:** Bearer Token (bắt buộc)  
> **Điều kiện:** Chỉ hủy được khi Order đang ở trạng thái `PENDING`

**Request Body:**
```json
{
  "cancelReason": "Tôi muốn thay đổi sản phẩm"  // Bắt buộc
}
```

**Response thành công (200 OK):**
```json
{
  "success": true,
  "message": "Order cancelled successfully."
}
```

**Side effects tự động khi hủy:**
- ✅ Order Status → `CANCELLED`
- ✅ Payment Status → `FAILED`
- ✅ Stock sản phẩm được hoàn lại
- ✅ Lượt sử dụng coupon được hoàn lại

---

### 5.6. `GET /api/Order/my-orders` — Lấy danh sách đơn hàng của user

> **Auth:** Bearer Token (bắt buộc)

**Query Parameters:**

| Param | Kiểu | Mô tả | Default |
|-------|------|-------|---------|
| `status` | `string?` | Lọc theo trạng thái order | *(tất cả)* |
| `pageNumber` | `int` | Trang hiện tại | `1` |
| `pageSize` | `int` | Số item/trang | `10` |

**Ví dụ:**
```
GET /api/Order/my-orders?status=PENDING&pageNumber=1&pageSize=10
```

---

## 6. Sơ đồ luồng (Flow Diagram)

### 6.1. Luồng tổng thể — COD

```
┌─────────┐                          ┌──────────┐               ┌──────────┐
│   User  │                          │ Frontend │               │ Backend  │
└────┬────┘                          └────┬─────┘               └────┬─────┘
     │                                    │                          │
     │   1. Chọn COD + Bấm "Đặt hàng"   │                          │
     │──────────────────────────────────▶│                          │
     │                                    │   2. POST /checkout      │
     │                                    │────────────────────────▶│
     │                                    │                          │ Tạo Order (PENDING)
     │                                    │                          │ Tạo Payment (PENDING, COD)
     │                                    │                          │ Trừ stock, dùng coupon
     │                                    │                          │ Xóa giỏ hàng
     │                                    │   3. Response (success)  │
     │                                    │◀────────────────────────│
     │   4. Hiển thị "Đặt hàng thành công" │                          │
     │◀───────────────────────────────────│                          │
     │                                    │                          │
     │          === ADMIN XỬ LÝ ===       │                          │
     │                                    │                          │ Admin CONFIRMED
     │                                    │                          │ Admin SHIPPED (+ tracking)
     │                                    │                          │ Admin DELIVERED
     │                                    │                          │ → Payment COD → COMPLETED
     │                                    │                          │
```

### 6.2. Luồng tổng thể — SePay (QR Code + Polling)

```
┌─────────┐          ┌──────────┐          ┌──────────┐          ┌──────────┐
│   User  │          │ Frontend │          │ Backend  │          │  SePay   │
└────┬────┘          └────┬─────┘          └────┬─────┘          └────┬─────┘
     │                    │                     │                     │
     │ 1. Chọn SEPAY      │                     │                     │
     │   + Bấm "Đặt hàng" │                     │                     │
     │──────────────────▶│                     │                     │
     │                    │ 2. POST /checkout   │                     │
     │                    │───────────────────▶│                     │
     │                    │                     │ Tạo Order (PENDING) │
     │                    │                     │ Tạo Payment (PENDING, SEPAY)
     │                    │                     │ + PaymentReference: SEVQR...
     │                    │                     │ + QR Code URL
     │                    │                     │ + ExpiredAt (30 phút)
     │                    │                     │ Trừ stock, dùng coupon
     │                    │                     │ Xóa giỏ hàng
     │                    │ 3. Response         │                     │
     │                    │   (qrCodeUrl,       │                     │
     │                    │    paymentReference, │                     │
     │                    │    paymentExpiredAt) │                     │
     │                    │◀───────────────────│                     │
     │                    │                     │                     │
     │ 4. Hiển thị:       │                     │                     │
     │   - QR Code        │                     │                     │
     │   - Nội dung CK    │                     │                     │
     │   - Countdown      │                     │                     │
     │◀──────────────────│                     │                     │
     │                    │                     │                     │
     │ 5. User mở app     │                     │                     │
     │   ngân hàng +      │                     │                     │
     │   quét QR / CK     │                     │                     │
     │────────────────────────────────────────────────────────────▶│
     │                    │                     │                     │
     │                    │ 6. Polling           │                     │
     │                    │ GET /status (3-5s)  │                     │
     │                    │───────────────────▶│                     │
     │                    │ isPaid: false        │                     │
     │                    │◀───────────────────│                     │
     │                    │                     │                     │
     │                    │                     │  7. SePay Webhook   │
     │                    │                     │  (user đã CK xong) │
     │                    │                     │◀────────────────────│
     │                    │                     │  → Payment COMPLETED│
     │                    │                     │  → Order CONFIRMED  │
     │                    │                     │                     │
     │                    │ 8. Polling           │                     │
     │                    │ GET /status          │                     │
     │                    │───────────────────▶│                     │
     │                    │ isPaid: true ✅      │                     │
     │                    │◀───────────────────│                     │
     │                    │                     │                     │
     │ 9. "Thanh toán     │                     │                     │
     │    thành công!" 🎉 │                     │                     │
     │◀──────────────────│                     │                     │
```

### 6.3. Luồng Hết hạn (Auto-expire)

```
┌──────────┐         ┌──────────────────┐
│ Backend  │         │ Background       │
│          │         │ Service (60s)    │
└────┬─────┘         └───────┬──────────┘
     │                       │
     │                       │ Kiểm tra mỗi 60 giây
     │                       │ Tìm payment SEPAY + PENDING
     │                       │ + ExpiredAt < now
     │                       │
     │                       │ Nếu tìm thấy:
     │◀──────────────────────│
     │ Payment → EXPIRED     │
     │ Order → CANCELLED     │
     │ Stock → Hoàn lại      │
     │ Coupon → Hoàn lại     │
     │                       │
```

---

## 7. Xử lý Error & Edge Cases

### 7.1. Cấu trúc response lỗi chung

```json
{
  "success": false,
  "message": "Thông báo lỗi chi tiết",
  "data": null
}
```

### 7.2. HTTP Status Codes

| Code | Khi nào? | Xử lý FE |
|------|---------|-----------|
| `200` | Thành công | Parse `data` |
| `400` | Lỗi validation / Business logic | Hiển thị `message` cho user |
| `401` | Token hết hạn / không có | Redirect về trang Login |
| `403` | Không có quyền | Hiển thị "Bạn không có quyền..." |
| `404` | Không tìm thấy resource | Hiển thị "Không tìm thấy đơn hàng" |

### 7.3. Edge Cases cần xử lý

| Trường hợp | Giải pháp FE |
|------------|-------------|
| User F5 trang checkout đang polling | Lưu `orderId` vào sessionStorage, tiếp tục polling |
| User đóng tab rồi quay lại | Show order detail page, kiểm tra `paymentStatus` |
| Token hết hạn đang polling | Làm mới token hoặc redirect login |
| Mạng mất khi đang polling | Thử lại với exponential backoff, tối đa 3 lần |
| User chuyển khoản thiếu tiền | BE xử lý: payment vẫn PENDING (webhook nhận nhưng không confirm) |
| User chuyển khoản 2 lần | BE xử lý: idempotent - lần 2 sẽ bị bỏ qua |
| SePay checkout redirect thành công nhưng webhook chưa tới | Success callback sẽ tự cập nhật status |

---

## 8. Code mẫu Frontend (React)

### 8.1. Hàm Checkout

```javascript
// services/orderService.js

const API_BASE = 'http://localhost:5000/api';

export async function checkout(token, orderData) {
  const response = await fetch(`${API_BASE}/Order/checkout`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify(orderData)
  });
  
  return await response.json();
}
```

### 8.2. Hàm Polling Payment Status

```javascript
// services/paymentService.js

export async function getPaymentStatus(token, orderId) {
  const response = await fetch(`${API_BASE}/Payment/${orderId}/status`, {
    headers: {
      'Authorization': `Bearer ${token}`
    }
  });
  
  return await response.json();
}
```

### 8.3. Component xử lý thanh toán SePay (QR Code + Polling)

```jsx
// pages/SepayPaymentPage.jsx

import { useEffect, useState, useRef } from 'react';
import { getPaymentStatus } from '../services/paymentService';
import { useNavigate } from 'react-router-dom';

function SepayPaymentPage({ orderId, token, checkoutData }) {
  const [paymentStatus, setPaymentStatus] = useState(null);
  const [remainingSeconds, setRemainingSeconds] = useState(1800); // 30 phút
  const pollingRef = useRef(null);
  const navigate = useNavigate();

  // === POLLING LOGIC ===
  useEffect(() => {
    const poll = async () => {
      try {
        const result = await getPaymentStatus(token, orderId);
        
        if (result.success) {
          const data = result.data;
          setPaymentStatus(data);
          setRemainingSeconds(data.remainingSeconds);

          // ✅ Thanh toán thành công → dừng polling
          if (data.isPaid || data.paymentStatus === 'COMPLETED') {
            clearInterval(pollingRef.current);
            navigate('/payment/success', { 
              state: { orderId, orderNumber: data.orderNumber } 
            });
            return;
          }

          // ⏰ Hết hạn → dừng polling
          if (data.paymentStatus === 'EXPIRED' || data.remainingSeconds <= 0) {
            clearInterval(pollingRef.current);
            navigate('/payment/expired', { state: { orderId } });
            return;
          }

          // ❌ Thất bại → dừng polling  
          if (data.paymentStatus === 'FAILED') {
            clearInterval(pollingRef.current);
            navigate('/payment/failed', { state: { orderId } });
            return;
          }
        }
      } catch (error) {
        console.error('Polling error:', error);
      }
    };

    // Bắt đầu polling mỗi 4 giây
    pollingRef.current = setInterval(poll, 4000);
    poll(); // Gọi ngay lần đầu

    return () => clearInterval(pollingRef.current); // Cleanup
  }, [orderId, token, navigate]);

  // === COUNTDOWN TIMER ===
  useEffect(() => {
    const timer = setInterval(() => {
      setRemainingSeconds(prev => {
        if (prev <= 0) {
          clearInterval(timer);
          return 0;
        }
        return prev - 1;
      });
    }, 1000);

    return () => clearInterval(timer);
  }, []);

  const formatTime = (seconds) => {
    const m = Math.floor(seconds / 60).toString().padStart(2, '0');
    const s = (seconds % 60).toString().padStart(2, '0');
    return `${m}:${s}`;
  };

  return (
    <div className="payment-container">
      <h2>Thanh toán đơn hàng</h2>
      
      <div className="payment-info">
        <p><strong>Mã đơn:</strong> {checkoutData.orderNumber}</p>
        <p><strong>Số tiền:</strong> {checkoutData.totalAmount?.toLocaleString()}đ</p>
        <p className="countdown">
          ⏱️ Thời gian còn lại: <strong>{formatTime(remainingSeconds)}</strong>
        </p>
      </div>

      {/* QR Code */}
      {checkoutData.qrCodeUrl && (
        <div className="qr-section">
          <h3>Quét mã QR để thanh toán</h3>
          <img 
            src={checkoutData.qrCodeUrl} 
            alt="QR Code thanh toán" 
            width={300} 
            height={300}
          />
        </div>
      )}

      {/* Nội dung chuyển khoản */}
      {checkoutData.paymentReference && (
        <div className="transfer-info">
          <h3>Hoặc chuyển khoản thủ công</h3>
          <p>Nội dung chuyển khoản: 
            <strong> {checkoutData.paymentReference}</strong>
            <button onClick={() => {
              navigator.clipboard.writeText(checkoutData.paymentReference);
              alert('Đã copy!');
            }}>📋 Copy</button>
          </p>
        </div>
      )}

      <p className="status-text">
        🔄 Đang chờ thanh toán... Trang sẽ tự động cập nhật khi bạn thanh toán thành công.
      </p>
    </div>
  );
}

export default SepayPaymentPage;
```

### 8.4. Component xử lý Checkout (chọn phương thức)

```jsx
// pages/CheckoutPage.jsx

import { useState } from 'react';
import { checkout } from '../services/orderService';
import { useNavigate } from 'react-router-dom';

function CheckoutPage({ token }) {
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();

  const handleCheckout = async (formData) => {
    setLoading(true);
    
    try {
      const result = await checkout(token, formData);
      
      if (!result.success) {
        alert(result.message);
        return;
      }

      const data = result.data;

      if (data.paymentMethod === 'COD') {
        // ===== COD: Chuyển thẳng trang thành công =====
        navigate('/order/success', { state: { order: data } });
      } 
      else if (data.paymentMethod === 'SEPAY') {
        // ===== SEPAY: Chuyển trang QR Code + Polling =====
        navigate('/payment/sepay', {
          state: {
            orderId: data.orderId,
            checkoutData: data
          }
        });

        // ===== HOẶC: Redirect đến SePay Checkout Page =====
        // const checkoutUrl = `http://localhost:5000${data.checkoutUrl}`
        //   + `?successUrl=${encodeURIComponent('http://localhost:3000/payment/success')}`
        //   + `&errorUrl=${encodeURIComponent('http://localhost:3000/payment/error')}`
        //   + `&cancelUrl=${encodeURIComponent('http://localhost:3000/payment/cancel')}`;
        // window.location.href = checkoutUrl;
      }
    } catch (error) {
      alert('Có lỗi xảy ra: ' + error.message);
    } finally {
      setLoading(false);
    }
  };

  // ... render form
}
```

### 8.5. Xử lý Success Redirect (Cách 2 - SePay Checkout)

```jsx
// pages/PaymentSuccessPage.jsx

import { useEffect, useState } from 'react';
import { useSearchParams } from 'react-router-dom';

function PaymentSuccessPage() {
  const [searchParams] = useSearchParams();
  
  const status = searchParams.get('status');       // "success"
  const orderId = searchParams.get('orderId');     // "42"
  const orderNumber = searchParams.get('orderNumber'); // "ORD20260223001"
  const note = searchParams.get('note');           // "already_completed" (nếu có)

  if (status === 'success') {
    return (
      <div>
        <h1>🎉 Thanh toán thành công!</h1>
        <p>Mã đơn hàng: <strong>{orderNumber}</strong></p>
        <a href={`/orders/${orderId}`}>Xem chi tiết đơn hàng</a>
      </div>
    );
  }

  if (status === 'error') {
    const message = searchParams.get('message');
    return (
      <div>
        <h1>❌ Thanh toán thất bại</h1>
        <p>{message}</p>
        <a href="/cart">Quay lại giỏ hàng</a>
      </div>
    );
  }

  return <div>Đang xử lý...</div>;
}
```

---

## 9. Câu hỏi thường gặp (FAQ)

### Q1: Nên dùng Cách 1 (QR + Polling) hay Cách 2 (SePay Redirect)?

**Khuyến nghị Cách 1** (QR Code + Polling) vì:
- User không rời khỏi trang web
- Trải nghiệm mượt mà hơn
- Kiểm soát hoàn toàn UI/UX

Dùng **Cách 2** (SePay Redirect) khi:
- Muốn SePay hỗ trợ nhiều phương thức thanh toán (QR, thẻ, ví điện tử...)
- Không muốn tự build UI thanh toán

### Q2: Polling bao lâu thì nên dừng?

- Polling trong **30 phút** (bằng `paymentExpiredAt`)
- Hoặc dựa vào `remainingSeconds` từ API — khi `remainingSeconds <= 0` thì dừng
- Backend sẽ tự xử lý expire sau 30 phút

### Q3: Nếu webhook bị lỗi, user đã chuyển khoản nhưng status vẫn PENDING?

- **Cách 1:** FE hướng dẫn user liên hệ hỗ trợ
- **Cách 2 (SePay Redirect):** Success callback sẽ tự cập nhật status
- **Admin** có thể gọi `PUT /api/Payment/{orderId}/verify` để xác nhận thủ công

### Q4: PaymentReference dùng để làm gì?

- Đây là **nội dung chuyển khoản** mà user phải ghi khi chuyển tiền
- Ví dụ: `SEVQR20260223001`
- Backend dùng nó để khớp giao dịch từ SePay webhook
- **FE nên hiển thị rõ ràng** và cho user **copy** nội dung này

### Q5: QrCodeUrl là gì?

- Đây là URL ảnh QR Code từ SePay
- FE dùng trực tiếp: `<img src="{qrCodeUrl}" />`
- QR Code đã chứa sẵn: số tài khoản, số tiền, nội dung CK
- User chỉ cần quét → tự điền hết thông tin

### Q6: checkoutUrl trả về dạng relative path?

- Đúng, ví dụ: `/api/Payment/42/checkout`
- FE cần nối thêm base URL: `http://localhost:5000/api/Payment/42/checkout?successUrl=...`
- URL này là **GET request**, mở bằng `window.location.href` hoặc `window.open()`

### Q7: Khi nào Cart bị xóa?

- Cart bị xóa **ngay khi checkout thành công** (cả COD lẫn SEPAY)
- Nếu Payment SEPAY bị expire → Order hủy, stock hoàn lại, **nhưng Cart đã bị xóa rồi**
- User cần thêm lại sản phẩm vào giỏ hàng nếu muốn đặt lại

---

## 📌 Tóm tắt nhanh — Các API FE cần call

| # | Mục đích | Method | Endpoint | Auth? |
|---|---------|--------|----------|-------|
| 1 | Tạo đơn hàng | `POST` | `/api/Order/checkout` | ✅ Bearer Token |
| 2 | Polling trạng thái | `GET` | `/api/Payment/{orderId}/status` | ✅ Bearer Token |
| 3 | Redirect SePay (Cách 2) | `GET` | `/api/Payment/{orderId}/checkout?successUrl=...&errorUrl=...&cancelUrl=...` | ❌ Không cần |
| 4 | Xem chi tiết đơn hàng | `GET` | `/api/Order/{id}` | ✅ Bearer Token |
| 5 | Danh sách đơn hàng | `GET` | `/api/Order/my-orders?status=...&pageNumber=...&pageSize=...` | ✅ Bearer Token |
| 6 | Hủy đơn hàng | `PUT` | `/api/Order/{id}/cancel` | ✅ Bearer Token |

---

> 📝 **Lưu ý cuối:** Tất cả API đều trả response dạng:
> ```json
> {
>   "success": true/false,
>   "message": "...",
>   "data": { ... }
> }
> ```
> FE luôn kiểm tra `success` trước khi xử lý `data`.
