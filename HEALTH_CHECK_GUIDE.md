# 🏥 Health Check & Database Connection Resilience Guide

## ✅ Các cải tiến đã thực hiện

### 1. **Connection String Optimization** (Program.cs)
```csharp
// Các tham số mới được thêm vào connection string:
- Keepalive=30          // Gửi keepalive packet mỗi 30s để tránh stale connections
- Timeout=30            // Timeout cho connection attempts
- Connection Idle Lifetime=60  // Recycle connections sau 60s idle
- Maximum Pool Size=20  // Giới hạn pool size
- Minimum Pool Size=1   // Đảm bảo luôn có ít nhất 1 connection sẵn sàng
```

### 2. **EnableRetryOnFailure** (Program.cs)
```csharp
npgsqlOptions.EnableRetryOnFailure(
    maxRetryCount: 5,                      // Retry tối đa 5 lần
    maxRetryDelay: TimeSpan.FromSeconds(30), // Chờ tối đa 30s giữa các retry
    errorCodesToAdd: null                   // Retry tất cả transient errors
);
```

**Transient errors được handle:**
- Connection timeout
- Network interruption
- Server temporarily unavailable
- EndOfStreamException (stale connections)

### 3. **Raw SQL Retry Logic** (SepayRepository.cs)
```csharp
// GetExpiredPendingSepayOrderIdsAsync() giờ có:
- 3 lần retry với exponential backoff (2s, 4s, 8s)
- Detect transient errors (EndOfStreamException, connection errors)
- Keepalive parameters trong connection string
```

### 4. **Background Service Retry** (SepayPollingBackgroundService.cs)
```csharp
// Helper method RetryDbOperationAsync:
- Retry DB operations 3 lần
- Exponential backoff: 2^attempt seconds
- Detect transient exceptions
- Chi tiết logging cho monitoring
```

---

## 🔌 Health Check Endpoints

Ứng dụng cung cấp **2 implementation** cho health checks:

### **A. Built-in ASP.NET Core Health Checks** (Recommended cho production)
- Endpoint: `/health` và `/health/detail`
- Sử dụng middleware `app.MapHealthChecks()`
- Performance tốt, chuẩn Microsoft

### **B. Custom HealthController** (Flexible, dễ mở rộng)
- Endpoint: `/api/health`, `/api/health/detail`, `/api/health/ping`, `/api/health/version`
- Controller riêng, dễ customize
- Có thêm endpoints bổ sung

**💡 Khuyến nghị:** Dùng **cả 2** cho các mục đích khác nhau:
- `/health` (built-in) → Cho UptimeRobot keep-alive
- `/api/health/detail` (controller) → Cho debugging/monitoring chi tiết

---

### **Endpoint 1: `/health` (Built-in)**
**Công dụng:** Simple health check cho monitoring services (UptimeRobot, Pingdom, etc.)

**Response khi healthy:**
```
HTTP 200 OK
Healthy
```

**Response khi unhealthy:**
```
HTTP 503 Service Unavailable
Unhealthy
```

**Cách sử dụng với UptimeRobot/Pingdom:**
1. Tạo HTTP Monitor
2. URL: `https://your-app.onrender.com/health`
3. Interval: **5 minutes** (khuyến nghị để giữ app không bị spin-down)
4. Expected status code: `200`

---

### **Endpoint 2: `/health/detail` (Built-in)**
**Công dụng:** Detailed health check với thông tin về database connection

**Response (JSON format):**
```json
{
  "status": "Healthy",
  "timestamp": "2024-03-11T10:00:00Z",
  "checks": [
    {
      "name": "postgresql",
      "status": "Healthy",
      "duration": "00:00:00.1234567",
      "description": null,
      "exception": null
    }
  ]
}
```

**Khi database down:**
```json
{
  "status": "Unhealthy",
  "timestamp": "2024-03-11T10:00:00Z",
  "checks": [
    {
      "name": "postgresql",
      "status": "Unhealthy",
      "duration": "00:00:10.0000000",
      "description": null,
      "exception": "Npgsql.NpgsqlException: connection timeout"
    }
  ]
}
```

---

### **Endpoint 3: `/api/health` (HealthController)**
**Công dụng:** Custom health check với response JSON đẹp hơn

**Response:**
```json
{
  "status": "healthy",
  "timestamp": "2024-03-11T10:00:00Z",
  "service": "PRN232 Backend API"
}
```

---

### **Endpoint 4: `/api/health/detail` (HealthController)**
**Công dụng:** Detailed check với thông tin database + duration

**Response khi healthy:**
```json
{
  "status": "Healthy",
  "timestamp": "2024-03-11T10:00:00Z",
  "totalDuration": "45ms",
  "service": "PRN232 Backend API",
  "checks": [
    {
      "name": "postgresql",
      "status": "Healthy",
      "duration": "42ms",
      "database": "prn232_db_k24y",
      "description": "Database connection successful"
    }
  ]
}
```

**Response khi unhealthy:**
```json
{
  "status": "Unhealthy",
  "timestamp": "2024-03-11T10:00:00Z",
  "totalDuration": "10023ms",
  "service": "PRN232 Backend API",
  "checks": [
    {
      "name": "postgresql",
      "status": "Unhealthy",
      "duration": "10020ms",
      "description": "Database connection failed",
      "exception": "Npgsql.NpgsqlException: connection timeout"
    }
  ]
}
```

---

### **Endpoint 5: `/api/health/ping` (HealthController)**
**Công dụng:** Minimal keep-alive endpoint, không check database

**Response:**
```json
{
  "status": "pong",
  "timestamp": "2024-03-11T10:00:00Z"
}
```

**💡 Use case:** Dùng cho cron jobs cần response nhanh (< 50ms), không quan tâm database status.

---

### **Endpoint 6: `/api/health/version` (HealthController)**
**Công dụng:** Thông tin version và environment

**Response:**
```json
{
  "version": "1.0.0",
  "buildDate": "2024-03-11T10:00:00Z",
  "environment": "Production",
  "framework": ".NET 8",
  "timestamp": "2024-03-11T10:00:00Z"
}
```

---

## 🚀 Setup với Monitoring Services

### **Option 1: UptimeRobot (Free)**
1. Đăng ký tại https://uptimerobot.com
2. Tạo Monitor mới:
   - **Monitor Type:** HTTP(s)
   - **Friendly Name:** PRN232 Backend
   - **URL:** `https://your-app.onrender.com/health`
   - **Monitoring Interval:** 5 minutes
3. Alert Contacts: Email của bạn

**Lợi ích:**
- Giữ app không bị spin-down trên Render free tier
- Nhận email alert khi app down
- 50 monitors miễn phí

---

### **Option 2: Pingdom (Free Trial)**
1. Đăng ký tại https://www.pingdom.com
2. Tạo Uptime check:
   - **Name:** PRN232 Backend Health
   - **URL:** `https://your-app.onrender.com/health`
   - **Check interval:** 5 minutes
3. Set alert thresholds

---

### **Option 3: Better Stack (Formerly Better Uptime)**
1. Đăng ký tại https://betterstack.com
2. Tạo Monitor:
   - **URL:** `https://your-app.onrender.com/health`
   - **Interval:** 3 minutes
   - **Expected status:** 200

**Lợi ích:** 
- UI đẹp hơn
- Free plan có nhiều features
- Incident management tốt

---

### **Option 4: Cron-job.org (Free, không cần đăng ký)**
1. Truy cập https://cron-job.org
2. Tạo cron job:
   - **URL:** `https://your-app.onrender.com/health`
   - **Schedule:** Every 5 minutes
   - **Method:** GET

---

## 📊 Monitoring & Logging

### **Check logs để verify retry mechanism:**

**Trong Render logs, bạn sẽ thấy:**

✅ **Khi connection healthy:**
```
SepayPollingBackgroundService started - polling mỗi 15s
SePay Polling: Found 5 transactions, 2 pending orders, lastProcessedId=12345
```

⚠️ **Khi connection có vấn đề (đang retry):**
```
DB operation 'GetPendingSepayOrdersAsync' failed (attempt 1/3) - retrying in 2s
Npgsql.NpgsqlException: Exception while reading from stream
```

✅ **Khi retry thành công:**
```
SePay Polling: Found 5 transactions, 2 pending orders, lastProcessedId=12345
```

❌ **Khi retry fail hết:**
```
Failed to get pending orders after retries - skipping this poll cycle
Error in SepayPollingBackgroundService - backing off for 30s
```

---

## 🎯 Tại sao giải pháp này hiệu quả?

| Vấn đề cũ | Giải pháp mới | Kết quả |
|-----------|---------------|---------|
| Stale connections sau 4-5h idle | `Keepalive=30` + `Connection Idle Lifetime=60` | Connection pool tự động refresh |
| EndOfStreamException khi reconnect | `EnableRetryOnFailure` + custom retry | Tự động retry, không crash app |
| Raw SQL không có retry | `RetryDbOperationAsync` helper | Background services stable hơn |
| App bị spin-down trên Render | Health check + ping service | App luôn active |

---

## 🧪 Testing

### **Test 1: Health Check hoạt động**
```bash
curl https://your-app.onrender.com/health
# Expected: HTTP 200, body "Healthy"

curl https://your-app.onrender.com/health/detail
# Expected: JSON với status="Healthy"
```

### **Test 2: Simulate connection failure**
Tắt database tạm thời và check logs:
```
- Sẽ thấy retry attempts
- Background services sẽ backoff 30s thay vì crash
- Health check endpoint trả về 503
```

### **Test 3: Long-running stability**
- Deploy lên Render
- Setup UptimeRobot ping mỗi 5 phút
- Để chạy 12-24h
- Check logs → không còn EndOfStreamException

---

## 📝 Notes

1. **Render free tier limitations:**
   - Database có giới hạn connections (thường ~20-100)
   - App spin-down sau 15 phút không có request
   - Health check ping giúp giữ app active

2. **Connection pool settings:**
   - `Maximum Pool Size=20`: An toàn cho Render free tier
   - Tăng lên 50-100 nếu dùng paid tier

3. **Retry delays:**
   - Background services: 30s backoff khi lỗi DB
   - DB operations: exponential backoff (2s, 4s, 8s)
   - Tránh "hammering" database khi có vấn đề

4. **Health check interval:**
   - 5 phút: Khuyến nghị cho production
   - 3 phút: Nếu cần keep-alive tốt hơn
   - 1 phút: Quá thường xuyên, có thể tốn resources

---

## 🔗 References

- [Npgsql Connection String Parameters](https://www.npgsql.org/doc/connection-string-parameters.html)
- [EF Core Connection Resiliency](https://learn.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency)
- [ASP.NET Core Health Checks](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks)
- [Render PostgreSQL Best Practices](https://render.com/docs/databases)
