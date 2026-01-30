   # 🐳 Docker Setup - Hướng dẫn cho FE Team

## Yêu cầu

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (Windows/Mac)

## Khởi chạy

```bash
# 1. Clone repo (nếu chưa có)
git clone <repo-url>
cd PRN232-Backend

# 2. Chạy Docker
docker-compose up --build -d
```

> ⏱️ Lần đầu chạy sẽ mất 2-3 phút để build và khởi tạo database.

## Truy cập

| Service | URL | Mô tả |
|---------|-----|-------|
| **API + Swagger** | http://localhost:5255/swagger | Test API endpoints |
| **pgAdmin** | http://localhost:5050 | Quản lý database (web) |

### Đăng nhập pgAdmin

- **Email**: `admin@admin.com`
- **Password**: `admin`

### Kết nối Database trong pgAdmin

1. Click **Add New Server**
2. Tab **General**: Name = `StemStore`
3. Tab **Connection**:
   - Host: `postgres-db`
   - Port: `5432`
   - Database: `ecommerce_db`
   - Username: `postgres`
   - Password: `12345`

## Các lệnh hữu ích

```bash
# Dừng tất cả containers
docker-compose down

# Xem logs
docker-compose logs -f api

# Restart API (sau khi BE update code)
docker-compose up --build -d api

# Reset database (xóa data và seed lại)
docker-compose down -v
docker-compose up --build -d
```

## Tài khoản test

| Role | Username | Password |
|------|----------|----------|
| Admin | `admin` | `Password123!` |
| Customer | `nguyenvana` | `Password123!` |
| Staff | `staff_nguyen` | `Password123!` |

## Cập nhật khi BE thay đổi

### 🔄 Khi có code mới (API changes)

```bash
# Pull code mới từ git
git pull

# Rebuild và restart API container
docker-compose up --build -d api
```

### 🗃️ Khi có thay đổi database

**Cách 1: Reset toàn bộ database (xóa data cũ, seed lại)**
```bash
docker-compose down -v
docker-compose up --build -d
```

**Cách 2: Chạy SQL mới (giữ data cũ)**
```bash
# Chạy file SQL
docker exec -i stemstore-db psql -U postgres -d ecommerce_db < path/to/new_script.sql

# Hoặc chạy lệnh SQL trực tiếp
docker exec -it stemstore-db psql -U postgres -d ecommerce_db -c "YOUR SQL HERE"
```

**Cách 3: Dùng pgAdmin**
1. Truy cập http://localhost:5050
2. Kết nối database (xem hướng dẫn ở trên)
3. Click chuột phải vào database → **Query Tool**
4. Paste và chạy SQL

> ⚠️ **Lưu ý**: File `database_init_and_seed.sql` chỉ chạy tự động **lần đầu tiên**. Nếu database đã có data, cần dùng một trong các cách trên.

---

## Troubleshooting

**Q: Port đã được sử dụng?**
```bash
# Dừng container cũ
docker-compose down
# Hoặc đổi port trong docker-compose.yml
```

**Q: Database không có data?**
```bash
# Reset và seed lại
docker-compose down -v
docker-compose up --build -d
```
