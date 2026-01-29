# PRN232-Backend

STEM Products E-Commerce Backend API - .NET 8 + PostgreSQL

## 🚀 Quick Start

### 1️⃣ Yêu cầu

- **.NET 8 SDK**
- **PostgreSQL 17+**
- **Visual Studio 2022** hoặc **VS Code**

### 2️⃣ Cài đặt Database

#### Cách 1: Tự động (khuyên dùng)

```bash
# Chạy file SQL để tạo database và seed data
psql -U postgres -d postgres -f database_init_and_seed.sql
```

#### Cách 2: Thủ công

```sql
-- 1. Tạo database
CREATE DATABASE "STEM-DB" WITH ENCODING 'UTF8';

-- 2. Kết nối vào database
\c "STEM-DB"

-- 3. Chạy file init
\i database_init_and_seed.sql
```

### 3️⃣ Cấu hình Connection String

Mở `MV.PresentationLayer/appsettings.json` và cập nhật:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=STEM-DB;Username=postgres;Password=YOUR_PASSWORD"
  }
}
```

### 4️⃣ Chạy API

```bash
cd MV.PresentationLayer
dotnet restore
dotnet run
```

**Swagger UI:** `https://localhost:7295/swagger`

---

## 📊 Seed Data

File `database_init_and_seed.sql` bao gồm:

- ✅ **3 Roles:** Admin, Customer, Staff
- ✅ **15 Users** (password mặc định: `Password123!`)
- ✅ **8 Brands:** Arduino, Raspberry Pi, ESP32, Adafruit, SparkFun, Seeed, STM, TI
- ✅ **10 Categories:** Microcontrollers, Sensors, Actuators, Power Supply, Communication...
- ✅ **30 Products** STEM với đầy đủ thông tin tiếng Việt
- ✅ **64 Product Images** (2-3 ảnh/sản phẩm)
- ✅ **20 Reviews** với rating và comment tiếng Việt
- ✅ **8 Tutorials** hướng dẫn Arduino/ESP32/Raspberry Pi
- ✅ **15 Orders** với đầy đủ payment và shipping
- ✅ **7 Coupons** (discount codes)
- ✅ **6 Warranties** và **5 Warranty Claims**

### 🔑 Test Accounts

| Username       | Email                     | Password       | Role     |
| -------------- | ------------------------- | -------------- | -------- |
| `admin`        | admin@stemstore.vn        | `Password123!` | Admin    |
| `staff_nguyen` | staff.nguyen@stemstore.vn | `Password123!` | Staff    |
| `nguyenvana`   | nguyenvana@gmail.com      | `Password123!` | Customer |

---

## 🏗️ Cấu trúc Project

```
PRN232-Backend/
├── MV.DomainLayer/          # Entities (Product, User, Order...)
├── MV.ApplicationLayer/     # Business Logic & Services
├── MV.InfrastructureLayer/  # DbContext, Repositories
├── MV.PresentationLayer/    # API Controllers
└── database_init_and_seed.sql
```

---

## 🔧 API Endpoints

### Authentication

- `POST /api/auth/register` - Đăng ký user mới
- `POST /api/auth/login` - Login và nhận JWT token

### Products

- `GET /api/products` - Lấy danh sách sản phẩm
- `GET /api/products/{id}` - Chi tiết sản phẩm
- `POST /api/products` - Tạo sản phẩm mới (Admin)

### Orders

- `GET /api/orders` - Lấy danh sách đơn hàng
- `POST /api/orders` - Tạo đơn hàng mới

_(Xem Swagger UI để biết full API documentation)_

---

## 📝 Notes

- **Database encoding:** UTF-8
- **Password hashing:** BCrypt
- **JWT Authentication:** Bearer token
- **Port:** HTTP 5255, HTTPS 7295
