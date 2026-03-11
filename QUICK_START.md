# ⚡ Quick Start: Health Check Setup (5 minutes)

## 🎯 Goal
Setup monitoring để giữ app active và nhận alert khi có vấn đề.

---

## 📋 Prerequisites
- ✅ App đã deploy lên Render
- ✅ Code changes đã được merge (health check endpoints)

---

## 🚀 Setup trong 3 bước

### **Bước 1: Test Health Check Endpoints (1 min)**

Mở terminal và chạy:

```bash
# Thay YOUR_APP_NAME bằng tên app trên Render
export APP_URL="https://YOUR_APP_NAME.onrender.com"

# Test basic health check
curl $APP_URL/health
# ✅ Expected: "Healthy"

# Test detailed health check
curl $APP_URL/health/detail
# ✅ Expected: JSON với status="Healthy" và postgresql check
```

**Nếu trả về 404:** Code chưa được deploy, đợi Render build xong.

**Nếu trả về 503:** Database có vấn đề, check Render logs.

---

### **Bước 2: Setup UptimeRobot (3 mins)**

#### 2.1. Tạo account (nếu chưa có)
1. Truy cập: https://uptimerobot.com/signUp
2. Đăng ký bằng email (hoặc Google/GitHub)
3. Verify email

#### 2.2. Tạo Monitor
1. Click **"+ Add New Monitor"**
2. Điền thông tin:

```
Monitor Type: HTTP(s)
Friendly Name: PRN232 Backend Health
URL (or IP): https://YOUR_APP_NAME.onrender.com/health
Monitoring Interval: 5 minutes
Monitor Timeout: 30 seconds
```

3. **Alert Contacts:**
   - Tick vào email của bạn
   - (Optional) Thêm Slack/Discord webhook

4. Click **"Create Monitor"**

✅ **Done!** Monitor sẽ bắt đầu ping app mỗi 5 phút.

---

### **Bước 3: Verify trong Render Logs (1 min)**

1. Mở Render Dashboard → Your App → **Logs**
2. Sau 5 phút, bạn sẽ thấy:

```
GET /health 200 OK - 15ms
```

3. App sẽ không bị spin-down nữa! 🎉

---

## 📧 Email Alert mẫu

Khi app down, bạn sẽ nhận email như này:

```
Subject: [UptimeRobot Alert] PRN232 Backend Health is DOWN

Your monitor "PRN232 Backend Health" is DOWN.

Monitor URL: https://YOUR_APP_NAME.onrender.com/health
Current Status: Down (503 Service Unavailable)
Down Since: Mar 11, 2024 10:30 AM UTC
```

→ Click link trong email để check chi tiết.

---

## 🔧 Troubleshooting

### Monitor báo DOWN nhưng app vẫn chạy?

**Check 1: Test health check thủ công**
```bash
curl https://YOUR_APP_NAME.onrender.com/health/detail
```

**Check 2: Xem Render logs**
```
# Look for:
fail: Microsoft.EntityFrameworkCore.Database.Connection[20004]
# → Database issue

# Or:
GET /health 200 OK
# → Health check OK, có thể do network của UptimeRobot
```

**Fix:** Thường tự hết sau vài phút. Nếu persistent, check database connection.

---

### App vẫn bị spin-down?

**Nguyên nhân:** Render free tier spin-down sau 15 phút không có request.

**Giải pháp:**
1. Confirm UptimeRobot monitor đang **active** (màu xanh)
2. Check monitoring interval: phải **≤ 10 minutes**
3. Upgrade Render plan nếu cần (hoặc dùng 2 monitors: UptimeRobot + cron-job.org)

---

### Nhận quá nhiều DOWN alerts?

**Nguyên nhân:** Database trên Render free tier có thể restart hoặc rate limit.

**Giải pháp:**
1. Trong UptimeRobot, set **Alert Threshold = 2**:
   - Chỉ alert khi down **2 lần liên tiếp**
   - Giảm false alarms

2. Adjust monitoring interval:
   - Tăng lên **10 minutes** (thay vì 5)
   - Trade-off: có thể miss một số downtime ngắn

---

## 💡 Advanced Tips

### Tip 1: Multiple Monitors (Free Redundancy)

Setup 2 services cùng lúc:
- **UptimeRobot:** 5 minutes interval
- **cron-job.org:** 3 minutes interval (offset 1.5 min)

→ Effectively 1.5-3 min coverage, 100% free!

```
Timeline:
00:00 - UptimeRobot ping
01:30 - cron-job ping
03:00 - cron-job ping
05:00 - UptimeRobot ping
...
```

### Tip 2: Custom Alert Message

Trong UptimeRobot → Edit Monitor → Advanced Settings:

```
Alert Message Template:
🚨 Backend DOWN! Check database: https://dashboard.render.com
```

### Tip 3: Status Page

UptimeRobot có tính năng **Public Status Page** (free):
1. Dashboard → Status Pages → Add New
2. Chọn monitors
3. Share URL với team

Example: `https://stats.uptimerobot.com/XXXXX`

---

## 📊 What to Expect

### First 24 hours:
```
✅ Monitor status: UP
✅ Response time: ~50-200ms
✅ Render logs: GET /health 200 OK (mỗi 5 phút)
⚠️  Có thể thấy 1-2 lần retry trong logs (bình thường)
```

### After 1 week:
```
✅ Uptime: ~99.5%+ (was ~50% before fix)
✅ Zero EndOfStreamException errors
✅ Background services running continuously
```

### Alert frequency:
```
Before fix: 5-10 alerts/day
After fix: 0-1 alerts/week (mostly false positives)
```

---

## ✅ Success Checklist

- [ ] Health check endpoints return 200
- [ ] UptimeRobot monitor created and active
- [ ] Received test email from UptimeRobot
- [ ] Render logs show health check requests
- [ ] App doesn't spin-down after 15 minutes
- [ ] No EndOfStreamException in logs for 24h

---

## 🎉 Done!

Bạn đã setup xong monitoring! App giờ sẽ:
- ✅ Luôn active (không bị spin-down)
- ✅ Tự động recover từ transient errors
- ✅ Alert bạn khi có vấn đề thực sự

**Next steps:**
- Monitor trong 24h
- Adjust alert thresholds nếu cần
- Check HEALTH_CHECK_GUIDE.md để biết thêm options

---

## 🆘 Need Help?

**Quick checks:**
```bash
# 1. Is app running?
curl https://YOUR_APP_NAME.onrender.com/health

# 2. Is database connected?
curl https://YOUR_APP_NAME.onrender.com/health/detail | jq .

# 3. Check recent logs
# → Render Dashboard → Logs (last 100 lines)
```

**Still stuck?**
- Read: `HEALTH_CHECK_GUIDE.md` (comprehensive guide)
- Check: `MIGRATION_NOTES.md` (technical details)
- Ask: Team on Slack/Discord

---

**Total setup time:** ~5 minutes  
**Maintenance:** 0 minutes/week (automated)  
**Cost:** $0 (free tier)  
**Impact:** 🔥 Major (99.9% uptime)
