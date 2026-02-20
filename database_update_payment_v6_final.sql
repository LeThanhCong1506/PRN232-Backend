-- ============================================
-- SCRIPT CẬP NHẬT DATABASE HOÀN CHỈNH
-- CHO LUỒNG CHECKOUT / PAYMENT / SEPAY
-- ============================================
-- Version: 6.0 FINAL | February 2026
-- ============================================
--
-- HƯỚNG DẪN SỬ DỤNG:
-- 1. Chạy file database_init_and_seed.sql trước (tạo DB + seed data)
-- 2. Sau đó chạy file này để cập nhật schema cho Payment/Checkout
--
-- Script này IDEMPOTENT: có thể chạy nhiều lần mà không bị lỗi
-- ============================================


-- ============================================
-- PHẦN 1: SỬA LỖI CHÍNH TẢ
-- ============================================

-- Fix: shiping_fee → shipping_fee (lỗi typo trong SQL gốc)
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'order_header' AND column_name = 'shiping_fee'
    ) THEN
        ALTER TABLE ORDER_HEADER RENAME COLUMN shiping_fee TO shipping_fee;
        RAISE NOTICE 'Renamed shiping_fee → shipping_fee';
    ELSE
        RAISE NOTICE 'Column shipping_fee already exists, skip rename';
    END IF;
END $$;


-- ============================================
-- PHẦN 2: CẬP NHẬT ENUMS
-- ============================================

-- 2a. Cập nhật payment_method_enum: bỏ BANK_TRANSFER/MOMO/ZALO_PAY, thêm SEPAY
DO $$
DECLARE
    has_old_values BOOLEAN;
BEGIN
    -- Kiểm tra xem enum cũ còn giá trị BANK_TRANSFER không
    SELECT EXISTS (
        SELECT 1 FROM pg_enum
        WHERE enumlabel = 'BANK_TRANSFER'
        AND enumtypid = (SELECT oid FROM pg_type WHERE typname = 'payment_method_enum')
    ) INTO has_old_values;

    IF has_old_values THEN
        -- Drop views phụ thuộc
        DROP VIEW IF EXISTS v_sepay_payment_report;
        DROP VIEW IF EXISTS v_order_with_payment;
        DROP VIEW IF EXISTS v_order_items_detail;
        DROP VIEW IF EXISTS v_sepay_pending;

        -- Chuyển column sang text tạm
        ALTER TABLE PAYMENT ALTER COLUMN payment_method TYPE TEXT USING payment_method::TEXT;

        -- Cập nhật data cũ
        UPDATE PAYMENT SET payment_method = 'SEPAY' WHERE payment_method IN ('BANK_TRANSFER', 'MOMO', 'ZALO_PAY');

        -- Drop enum cũ, tạo enum mới
        DROP TYPE IF EXISTS payment_method_enum;
        CREATE TYPE payment_method_enum AS ENUM ('COD', 'SEPAY');

        -- Chuyển column về enum mới
        ALTER TABLE PAYMENT ALTER COLUMN payment_method TYPE payment_method_enum USING payment_method::payment_method_enum;

        RAISE NOTICE 'Updated payment_method_enum to (COD, SEPAY)';
    ELSE
        RAISE NOTICE 'payment_method_enum already up to date, skip';
    END IF;
END $$;

-- 2b. Thêm 'EXPIRED' vào payment_status_enum
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_enum
        WHERE enumlabel = 'EXPIRED'
        AND enumtypid = (SELECT oid FROM pg_type WHERE typname = 'payment_status_enum')
    ) THEN
        ALTER TYPE payment_status_enum ADD VALUE 'EXPIRED';
        RAISE NOTICE 'Added EXPIRED to payment_status_enum';
    ELSE
        RAISE NOTICE 'EXPIRED already exists in payment_status_enum, skip';
    END IF;
END $$;


-- ============================================
-- PHẦN 3: CẬP NHẬT BẢNG USER
-- ============================================

ALTER TABLE "USER" ADD COLUMN IF NOT EXISTS full_name VARCHAR(100);
ALTER TABLE "USER" ADD COLUMN IF NOT EXISTS province VARCHAR(50);
ALTER TABLE "USER" ADD COLUMN IF NOT EXISTS district VARCHAR(50);
ALTER TABLE "USER" ADD COLUMN IF NOT EXISTS ward VARCHAR(50);
ALTER TABLE "USER" ADD COLUMN IF NOT EXISTS street_address VARCHAR(200);

-- Cập nhật full_name từ username cho data cũ
UPDATE "USER" SET full_name = username WHERE full_name IS NULL;


-- ============================================
-- PHẦN 4: CẬP NHẬT BẢNG ORDER_HEADER
-- ============================================

-- 4a. Customer snapshot fields
ALTER TABLE ORDER_HEADER ADD COLUMN IF NOT EXISTS customer_name VARCHAR(100);
ALTER TABLE ORDER_HEADER ADD COLUMN IF NOT EXISTS customer_email VARCHAR(100);
ALTER TABLE ORDER_HEADER ADD COLUMN IF NOT EXISTS customer_phone VARCHAR(20);

-- 4b. Địa chỉ chi tiết snapshot
ALTER TABLE ORDER_HEADER ADD COLUMN IF NOT EXISTS province VARCHAR(50);
ALTER TABLE ORDER_HEADER ADD COLUMN IF NOT EXISTS district VARCHAR(50);
ALTER TABLE ORDER_HEADER ADD COLUMN IF NOT EXISTS ward VARCHAR(50);
ALTER TABLE ORDER_HEADER ADD COLUMN IF NOT EXISTS street_address VARCHAR(200);

-- 4c. Notes & timestamps
ALTER TABLE ORDER_HEADER ADD COLUMN IF NOT EXISTS notes TEXT;
ALTER TABLE ORDER_HEADER ADD COLUMN IF NOT EXISTS updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP;

-- 4d. Order lifecycle tracking
ALTER TABLE ORDER_HEADER ADD COLUMN IF NOT EXISTS confirmed_at TIMESTAMP;
ALTER TABLE ORDER_HEADER ADD COLUMN IF NOT EXISTS shipped_at TIMESTAMP;
ALTER TABLE ORDER_HEADER ADD COLUMN IF NOT EXISTS delivered_at TIMESTAMP;
ALTER TABLE ORDER_HEADER ADD COLUMN IF NOT EXISTS cancelled_at TIMESTAMP;
ALTER TABLE ORDER_HEADER ADD COLUMN IF NOT EXISTS cancel_reason TEXT;

-- 4e. Admin/Staff tracking
ALTER TABLE ORDER_HEADER ADD COLUMN IF NOT EXISTS confirmed_by INTEGER;
ALTER TABLE ORDER_HEADER ADD COLUMN IF NOT EXISTS shipped_by INTEGER;
ALTER TABLE ORDER_HEADER ADD COLUMN IF NOT EXISTS cancelled_by INTEGER;

-- 4f. Shipping tracking
ALTER TABLE ORDER_HEADER ADD COLUMN IF NOT EXISTS tracking_number VARCHAR(100);
ALTER TABLE ORDER_HEADER ADD COLUMN IF NOT EXISTS carrier VARCHAR(100);
ALTER TABLE ORDER_HEADER ADD COLUMN IF NOT EXISTS expected_delivery_date DATE;

-- 4g. Foreign keys cho admin tracking
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_order_confirmed_by') THEN
        ALTER TABLE ORDER_HEADER ADD CONSTRAINT fk_order_confirmed_by
        FOREIGN KEY (confirmed_by) REFERENCES "USER"(user_id) ON DELETE SET NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_order_shipped_by') THEN
        ALTER TABLE ORDER_HEADER ADD CONSTRAINT fk_order_shipped_by
        FOREIGN KEY (shipped_by) REFERENCES "USER"(user_id) ON DELETE SET NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_order_cancelled_by') THEN
        ALTER TABLE ORDER_HEADER ADD CONSTRAINT fk_order_cancelled_by
        FOREIGN KEY (cancelled_by) REFERENCES "USER"(user_id) ON DELETE SET NULL;
    END IF;
END $$;


-- ============================================
-- PHẦN 5: CẬP NHẬT BẢNG ORDER_ITEM
-- ============================================

-- Product snapshot fields
ALTER TABLE ORDER_ITEM ADD COLUMN IF NOT EXISTS product_name VARCHAR(255);
ALTER TABLE ORDER_ITEM ADD COLUMN IF NOT EXISTS product_sku VARCHAR(50);
ALTER TABLE ORDER_ITEM ADD COLUMN IF NOT EXISTS product_image_url VARCHAR(255);
ALTER TABLE ORDER_ITEM ADD COLUMN IF NOT EXISTS discount_amount NUMERIC(10, 2) DEFAULT 0;
ALTER TABLE ORDER_ITEM ADD COLUMN IF NOT EXISTS notes TEXT;
ALTER TABLE ORDER_ITEM ADD COLUMN IF NOT EXISTS created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP;


-- ============================================
-- PHẦN 6: CẬP NHẬT BẢNG PAYMENT
-- ============================================

-- 6a. Transaction tracking
ALTER TABLE PAYMENT ADD COLUMN IF NOT EXISTS transaction_id VARCHAR(100);
ALTER TABLE PAYMENT ADD COLUMN IF NOT EXISTS bank_code VARCHAR(50);
ALTER TABLE PAYMENT ADD COLUMN IF NOT EXISTS bank_account VARCHAR(50);
ALTER TABLE PAYMENT ADD COLUMN IF NOT EXISTS gateway_response TEXT;

-- 6b. SePay specific fields
ALTER TABLE PAYMENT ADD COLUMN IF NOT EXISTS expired_at TIMESTAMP;
ALTER TABLE PAYMENT ADD COLUMN IF NOT EXISTS payment_reference VARCHAR(100);
ALTER TABLE PAYMENT ADD COLUMN IF NOT EXISTS updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP;

-- 6c. QR Code & extra tracking
ALTER TABLE PAYMENT ADD COLUMN IF NOT EXISTS received_amount NUMERIC(12, 2);
ALTER TABLE PAYMENT ADD COLUMN IF NOT EXISTS qr_code_url VARCHAR(500);
ALTER TABLE PAYMENT ADD COLUMN IF NOT EXISTS qr_code_data TEXT;
ALTER TABLE PAYMENT ADD COLUMN IF NOT EXISTS retry_count INTEGER DEFAULT 0;
ALTER TABLE PAYMENT ADD COLUMN IF NOT EXISTS notes TEXT;

-- 6d. Manual verification by admin
ALTER TABLE PAYMENT ADD COLUMN IF NOT EXISTS verified_by INTEGER;
ALTER TABLE PAYMENT ADD COLUMN IF NOT EXISTS verified_at TIMESTAMP;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_payment_verified_by') THEN
        ALTER TABLE PAYMENT ADD CONSTRAINT fk_payment_verified_by
        FOREIGN KEY (verified_by) REFERENCES "USER"(user_id) ON DELETE SET NULL;
    END IF;
END $$;


-- ============================================
-- PHẦN 7: CẬP NHẬT BẢNG COUPON
-- ============================================

-- Thêm discount_type nếu chưa có (bảng gốc đã có column này trong CREATE TABLE,
-- nhưng nếu DB đã tồn tại từ version cũ thì cần thêm)
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'coupon' AND column_name = 'discount_type'
    ) THEN
        ALTER TABLE coupon ADD COLUMN discount_type discount_type_enum NOT NULL DEFAULT 'FIXED_AMOUNT';
        ALTER TABLE coupon ALTER COLUMN discount_type DROP DEFAULT;
        RAISE NOTICE 'Added discount_type to coupon';
    ELSE
        RAISE NOTICE 'coupon.discount_type already exists, skip';
    END IF;
END $$;


-- ============================================
-- PHẦN 8: TẠO BẢNG SEPAY_CONFIG
-- ============================================

CREATE TABLE IF NOT EXISTS SEPAY_CONFIG (
    config_id SERIAL PRIMARY KEY,
    bank_name VARCHAR(100) NOT NULL,
    bank_code VARCHAR(20) NOT NULL,
    account_number VARCHAR(50) NOT NULL,
    account_name VARCHAR(100) NOT NULL,
    api_key VARCHAR(255),
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_sepay_config_active ON SEPAY_CONFIG(is_active);


-- ============================================
-- PHẦN 9: TẠO BẢNG SEPAY_TRANSACTION
-- ============================================

CREATE TABLE IF NOT EXISTS SEPAY_TRANSACTION (
    transaction_id SERIAL PRIMARY KEY,
    order_id INTEGER,
    sepay_id VARCHAR(100),
    gateway VARCHAR(50),
    transaction_date TIMESTAMP,
    account_number VARCHAR(50),
    transfer_type VARCHAR(20),
    transfer_amount NUMERIC(12, 2),
    accumulated NUMERIC(12, 2),
    code VARCHAR(100),
    content TEXT,
    reference_number VARCHAR(100),
    description TEXT,
    is_processed BOOLEAN DEFAULT FALSE,
    processed_at TIMESTAMP,
    raw_data TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_sepay_order FOREIGN KEY (order_id)
        REFERENCES ORDER_HEADER(order_id) ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS idx_sepay_trans_order ON SEPAY_TRANSACTION(order_id);
CREATE INDEX IF NOT EXISTS idx_sepay_trans_code ON SEPAY_TRANSACTION(code);
CREATE INDEX IF NOT EXISTS idx_sepay_trans_processed ON SEPAY_TRANSACTION(is_processed);
CREATE INDEX IF NOT EXISTS idx_sepay_trans_date ON SEPAY_TRANSACTION(transaction_date);


-- ============================================
-- PHẦN 10: TẠO INDEXES
-- ============================================

-- ORDER_HEADER indexes
CREATE INDEX IF NOT EXISTS idx_order_customer_email ON ORDER_HEADER(customer_email);
CREATE INDEX IF NOT EXISTS idx_order_customer_phone ON ORDER_HEADER(customer_phone);
CREATE INDEX IF NOT EXISTS idx_order_updated ON ORDER_HEADER(updated_at);
CREATE INDEX IF NOT EXISTS idx_order_confirmed_at ON ORDER_HEADER(confirmed_at);
CREATE INDEX IF NOT EXISTS idx_order_shipped_at ON ORDER_HEADER(shipped_at);
CREATE INDEX IF NOT EXISTS idx_order_delivered_at ON ORDER_HEADER(delivered_at);
CREATE INDEX IF NOT EXISTS idx_order_cancelled_at ON ORDER_HEADER(cancelled_at);
CREATE INDEX IF NOT EXISTS idx_order_tracking ON ORDER_HEADER(tracking_number);
CREATE INDEX IF NOT EXISTS idx_order_carrier ON ORDER_HEADER(carrier);

-- PAYMENT indexes
CREATE INDEX IF NOT EXISTS idx_payment_transaction ON PAYMENT(transaction_id);
CREATE INDEX IF NOT EXISTS idx_payment_reference ON PAYMENT(payment_reference);
CREATE INDEX IF NOT EXISTS idx_payment_expired ON PAYMENT(expired_at);
CREATE INDEX IF NOT EXISTS idx_payment_received_amount ON PAYMENT(received_amount);
CREATE INDEX IF NOT EXISTS idx_payment_verified_by ON PAYMENT(verified_by);

-- COUPON indexes
CREATE INDEX IF NOT EXISTS idx_coupon_discount_type ON COUPON(discount_type);

-- USER indexes
CREATE INDEX IF NOT EXISTS idx_user_phone ON "USER"(phone);
CREATE INDEX IF NOT EXISTS idx_user_fullname ON "USER"(full_name);


-- ============================================
-- PHẦN 11: TRIGGERS
-- ============================================

-- 11a. Trigger: auto-update order lifecycle timestamps
CREATE OR REPLACE FUNCTION update_order_status_timestamp()
RETURNS TRIGGER AS $$
BEGIN
    IF NEW.status = 'CONFIRMED' AND (OLD.status IS NULL OR OLD.status != 'CONFIRMED') THEN
        NEW.confirmed_at = CURRENT_TIMESTAMP;
    END IF;
    IF NEW.status = 'SHIPPED' AND (OLD.status IS NULL OR OLD.status != 'SHIPPED') THEN
        NEW.shipped_at = CURRENT_TIMESTAMP;
    END IF;
    IF NEW.status = 'DELIVERED' AND (OLD.status IS NULL OR OLD.status != 'DELIVERED') THEN
        NEW.delivered_at = CURRENT_TIMESTAMP;
    END IF;
    IF NEW.status = 'CANCELLED' AND (OLD.status IS NULL OR OLD.status != 'CANCELLED') THEN
        NEW.cancelled_at = CURRENT_TIMESTAMP;
    END IF;
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS update_order_header_updated_at ON ORDER_HEADER;
DROP TRIGGER IF EXISTS trigger_order_status_update ON ORDER_HEADER;
CREATE TRIGGER trigger_order_status_update
    BEFORE UPDATE ON ORDER_HEADER
    FOR EACH ROW EXECUTE FUNCTION update_order_status_timestamp();

-- 11b. Trigger: auto-update payment timestamps
CREATE OR REPLACE FUNCTION update_payment_status_timestamp()
RETURNS TRIGGER AS $$
BEGIN
    IF NEW.status = 'COMPLETED' AND (OLD.status IS NULL OR OLD.status != 'COMPLETED') THEN
        IF NEW.payment_date IS NULL THEN
            NEW.payment_date = CURRENT_TIMESTAMP;
        END IF;
    END IF;
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS update_payment_updated_at ON PAYMENT;
DROP TRIGGER IF EXISTS trigger_payment_status_update ON PAYMENT;
CREATE TRIGGER trigger_payment_status_update
    BEFORE UPDATE ON PAYMENT
    FOR EACH ROW EXECUTE FUNCTION update_payment_status_timestamp();

-- 11c. Trigger: auto-update sepay_config
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS update_sepay_config_updated_at ON SEPAY_CONFIG;
CREATE TRIGGER update_sepay_config_updated_at
    BEFORE UPDATE ON SEPAY_CONFIG
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();


-- ============================================
-- PHẦN 12: HELPER FUNCTIONS
-- ============================================

-- Generate order number: ORDyyyyMMdd001
CREATE OR REPLACE FUNCTION generate_order_number()
RETURNS VARCHAR(50) AS $$
DECLARE
    today_str VARCHAR(8);
    seq_num INTEGER;
    order_num VARCHAR(50);
BEGIN
    today_str := TO_CHAR(CURRENT_DATE, 'YYYYMMDD');

    SELECT COUNT(*) + 1 INTO seq_num
    FROM ORDER_HEADER
    WHERE order_number LIKE 'ORD' || today_str || '%';

    order_num := 'ORD' || today_str || LPAD(seq_num::TEXT, 3, '0');

    RETURN order_num;
END;
$$ LANGUAGE plpgsql;

-- Generate payment reference: STEM + date + seq (e.g., STEM20260207001)
CREATE OR REPLACE FUNCTION generate_payment_reference(p_order_number VARCHAR)
RETURNS VARCHAR(100) AS $$
BEGIN
    RETURN 'STEM' || SUBSTRING(p_order_number FROM 4);
END;
$$ LANGUAGE plpgsql;


-- ============================================
-- PHẦN 13: TẠO / CẬP NHẬT VIEWS
-- ============================================

-- Drop tất cả views cũ trước khi tạo lại
DROP VIEW IF EXISTS v_sepay_payment_report;
DROP VIEW IF EXISTS v_order_with_payment;
DROP VIEW IF EXISTS v_order_items_detail;
DROP VIEW IF EXISTS v_sepay_pending;

-- 13a. View: Đơn hàng + thông tin thanh toán
CREATE OR REPLACE VIEW v_order_with_payment AS
SELECT
    oh.order_id, oh.order_number, oh.user_id,
    oh.customer_name, oh.customer_email, oh.customer_phone,
    oh.province, oh.district, oh.ward, oh.street_address,
    oh.shipping_address, oh.status AS order_status,
    oh.subtotal_amount, oh.shipping_fee, oh.discount_amount, oh.total_amount,
    oh.notes AS order_notes, oh.tracking_number, oh.carrier,
    oh.expected_delivery_date, oh.cancel_reason,
    oh.created_at AS order_date, oh.confirmed_at, oh.shipped_at,
    oh.delivered_at, oh.cancelled_at, oh.updated_at AS order_updated_at,
    p.payment_id, p.payment_method, p.status AS payment_status,
    p.amount AS payment_amount, p.received_amount,
    p.payment_reference, p.transaction_id, p.bank_code,
    p.qr_code_url, p.payment_date, p.expired_at,
    p.retry_count, p.verified_by, p.verified_at,
    p.notes AS payment_notes
FROM ORDER_HEADER oh
LEFT JOIN PAYMENT p ON oh.order_id = p.order_id;

-- 13b. View: Chi tiết items trong đơn hàng
CREATE OR REPLACE VIEW v_order_items_detail AS
SELECT
    oh.order_id, oh.order_number, oh.customer_name,
    oh.status AS order_status,
    oi.order_item_id, oi.product_id, oi.product_name,
    oi.product_sku, oi.product_image_url,
    oi.quantity, oi.unit_price,
    oi.discount_amount AS item_discount,
    oi.subtotal AS item_subtotal, oi.notes AS item_notes
FROM ORDER_HEADER oh
JOIN ORDER_ITEM oi ON oh.order_id = oi.order_id;

-- 13c. View: Báo cáo thanh toán SePay
CREATE OR REPLACE VIEW v_sepay_payment_report AS
SELECT
    p.payment_id, oh.order_number,
    oh.customer_name, oh.customer_phone,
    p.payment_reference, p.amount AS expected_amount,
    p.received_amount, p.status AS payment_status,
    p.qr_code_url, p.expired_at, p.payment_date,
    p.transaction_id, p.retry_count,
    st.sepay_id, st.transfer_amount,
    st.content AS transfer_content,
    st.transaction_date AS transfer_date, st.is_processed
FROM PAYMENT p
JOIN ORDER_HEADER oh ON p.order_id = oh.order_id
LEFT JOIN SEPAY_TRANSACTION st ON p.payment_reference = st.code
WHERE p.payment_method = 'SEPAY';

-- 13d. View: Giao dịch SePay chưa xử lý
CREATE OR REPLACE VIEW v_sepay_pending AS
SELECT
    st.*,
    oh.order_number,
    oh.total_amount AS expected_amount
FROM SEPAY_TRANSACTION st
LEFT JOIN ORDER_HEADER oh ON st.order_id = oh.order_id
WHERE st.is_processed = FALSE;


-- ============================================
-- PHẦN 14: SEED DATA CHO SEPAY_CONFIG
-- ============================================

INSERT INTO SEPAY_CONFIG (bank_name, bank_code, account_number, account_name, api_key, is_active)
SELECT 'MB Bank', 'MB', '0123456789', 'CONG TY STEM SHOP', 'YOUR_SEPAY_API_KEY_HERE', TRUE
WHERE NOT EXISTS (SELECT 1 FROM SEPAY_CONFIG LIMIT 1);


-- ============================================
-- PHẦN 15: COMMENTS
-- ============================================

COMMENT ON TABLE SEPAY_CONFIG IS 'Cấu hình tài khoản ngân hàng cho SePay';
COMMENT ON TABLE SEPAY_TRANSACTION IS 'Log tất cả giao dịch nhận được từ SePay webhook';
COMMENT ON COLUMN PAYMENT.payment_reference IS 'Mã tham chiếu thanh toán - nội dung chuyển khoản';
COMMENT ON COLUMN PAYMENT.expired_at IS 'Thời gian hết hạn thanh toán online';
COMMENT ON COLUMN PAYMENT.received_amount IS 'Số tiền thực nhận';
COMMENT ON COLUMN PAYMENT.qr_code_url IS 'URL QR code thanh toán';
COMMENT ON COLUMN PAYMENT.retry_count IS 'Số lần thanh toán thất bại';
COMMENT ON COLUMN PAYMENT.verified_by IS 'Staff xác nhận thanh toán';
COMMENT ON COLUMN PAYMENT.verified_at IS 'Thời điểm xác nhận thanh toán';
COMMENT ON COLUMN ORDER_HEADER.customer_name IS 'Snapshot tên khách hàng tại thời điểm đặt hàng';
COMMENT ON COLUMN ORDER_HEADER.confirmed_at IS 'Thời điểm xác nhận đơn';
COMMENT ON COLUMN ORDER_HEADER.shipped_at IS 'Thời điểm giao vận chuyển';
COMMENT ON COLUMN ORDER_HEADER.delivered_at IS 'Thời điểm giao thành công';
COMMENT ON COLUMN ORDER_HEADER.cancelled_at IS 'Thời điểm hủy đơn';
COMMENT ON COLUMN ORDER_HEADER.cancel_reason IS 'Lý do hủy đơn';
COMMENT ON COLUMN ORDER_HEADER.confirmed_by IS 'Staff/Admin xác nhận đơn';
COMMENT ON COLUMN ORDER_HEADER.shipped_by IS 'Staff/Admin xử lý giao hàng';
COMMENT ON COLUMN ORDER_HEADER.cancelled_by IS 'Người hủy đơn';
COMMENT ON COLUMN ORDER_HEADER.tracking_number IS 'Mã vận đơn';
COMMENT ON COLUMN ORDER_HEADER.carrier IS 'Đơn vị vận chuyển';
COMMENT ON COLUMN ORDER_HEADER.expected_delivery_date IS 'Ngày dự kiến giao';
COMMENT ON COLUMN ORDER_ITEM.product_image_url IS 'Snapshot ảnh sản phẩm';
COMMENT ON COLUMN ORDER_ITEM.discount_amount IS 'Giảm giá cho item';
COMMENT ON COLUMN COUPON.discount_type IS 'FIXED_AMOUNT hoặc PERCENTAGE';


-- ============================================
-- PHẦN 16: KIỂM TRA KẾT QUẢ
-- ============================================

SELECT '========================================' AS separator;
SELECT 'KIỂM TRA KẾT QUẢ CẬP NHẬT' AS title;
SELECT '========================================' AS separator;

-- Kiểm tra enums
SELECT 'payment_method_enum values:' AS check_name;
SELECT enum_range(NULL::payment_method_enum) AS payment_methods;

SELECT 'payment_status_enum values:' AS check_name;
SELECT enum_range(NULL::payment_status_enum) AS payment_statuses;

-- Kiểm tra bảng ORDER_HEADER
SELECT 'ORDER_HEADER columns:' AS check_name;
SELECT column_name, data_type, is_nullable
FROM information_schema.columns
WHERE table_name = 'order_header' ORDER BY ordinal_position;

-- Kiểm tra bảng PAYMENT
SELECT 'PAYMENT columns:' AS check_name;
SELECT column_name, data_type, is_nullable
FROM information_schema.columns
WHERE table_name = 'payment' ORDER BY ordinal_position;

-- Kiểm tra bảng ORDER_ITEM
SELECT 'ORDER_ITEM columns:' AS check_name;
SELECT column_name, data_type, is_nullable
FROM information_schema.columns
WHERE table_name = 'order_item' ORDER BY ordinal_position;

-- Kiểm tra bảng SEPAY
SELECT 'SEPAY tables:' AS check_name;
SELECT table_name
FROM information_schema.tables
WHERE table_schema = 'public'
AND table_name IN ('sepay_config', 'sepay_transaction')
ORDER BY table_name;

-- Kiểm tra views
SELECT 'Views:' AS check_name;
SELECT table_name
FROM information_schema.views
WHERE table_schema = 'public'
AND table_name LIKE 'v_%'
ORDER BY table_name;

SELECT '========================================' AS separator;
SELECT 'HOÀN TẤT CẬP NHẬT THÀNH CÔNG!' AS status;
SELECT '========================================' AS separator;
