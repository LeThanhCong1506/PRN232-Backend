-- ============================================
-- Migration: Sprint 4.1 - Admin Product Management
-- Chạy trên PostgreSQL database
-- ============================================

-- 1. Table product: thêm is_active, is_deleted
ALTER TABLE product ADD COLUMN IF NOT EXISTS is_active BOOLEAN DEFAULT true;
ALTER TABLE product ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN DEFAULT false;

-- 2. Table product_image: thêm is_primary
ALTER TABLE product_image ADD COLUMN IF NOT EXISTS is_primary BOOLEAN DEFAULT false;

-- 3. Table product_instance: kiểm tra column status (cần cho Sprint 4.2)
-- Column này dùng enum instance_status_enum đã có sẵn trong DB
-- Nếu chưa có column status thì chạy dòng dưới:
-- ALTER TABLE product_instance ADD COLUMN IF NOT EXISTS status instance_status_enum DEFAULT 'IN_STOCK';
