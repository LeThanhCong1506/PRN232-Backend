-- ============================================
-- TẠO DATABASE VÀ SEED DATA (GỘP FINAL V6 HOÀN CHỈNH)
-- E-COMMERCE DATABASE FOR STEM PRODUCTS
-- ============================================
-- 
-- HƯỚNG DẪN CHẠY:
-- 1. Tạo database: CREATE DATABASE ecommerce_db WITH ENCODING 'UTF8';
-- 2. Kết nối: \c ecommerce_db
-- 3. Chạy file này: \i database_init_and_seed_final.sql
-- ============================================

-- Xóa database nếu đã tồn tại (CẨN THẬN!)
DROP DATABASE IF EXISTS ecommerce_db;

-- Tạo database mới
CREATE DATABASE ecommerce_db WITH ENCODING 'UTF8';

-- Kết nối vào database
\c ecommerce_db

-- ============================================
-- 1. TẠO ENUMS (Đã cập nhật đầy đủ V6)
-- ============================================

CREATE TYPE instance_status_enum AS ENUM ('IN_STOCK', 'SOLD', 'WARRANTY', 'DEFECTIVE', 'RETURNED');
CREATE TYPE difficulty_level_enum AS ENUM ('beginner', 'intermediate', 'advanced');
CREATE TYPE discount_type_enum AS ENUM ('FIXED_AMOUNT', 'PERCENTAGE');
CREATE TYPE order_status_enum AS ENUM ('PENDING', 'CONFIRMED', 'SHIPPED', 'DELIVERED', 'CANCELLED');
CREATE TYPE payment_method_enum AS ENUM ('COD', 'SEPAY');
CREATE TYPE payment_status_enum AS ENUM ('PENDING', 'COMPLETED', 'FAILED', 'EXPIRED');
CREATE TYPE claim_status_enum AS ENUM ('SUBMITTED', 'APPROVED', 'REJECTED', 'RESOLVED');

-- ============================================
-- 2. TẠO TABLES (Cấu trúc Final V6 & Milestone 3)
-- ============================================

-- QUẢN LÝ NGƯỜI DÙNG
CREATE TABLE ROLE (
    role_id SERIAL PRIMARY KEY,
    role_name VARCHAR(50) NOT NULL UNIQUE
);

CREATE TABLE "USER" (
    user_id SERIAL PRIMARY KEY,
    role_id INTEGER NOT NULL,
    username VARCHAR(50) NOT NULL UNIQUE,
    email VARCHAR(100) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    full_name VARCHAR(100),
    phone VARCHAR(20),
    address TEXT,
    province VARCHAR(50),
    district VARCHAR(50),
    ward VARCHAR(50),
    street_address VARCHAR(200),
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_user_role FOREIGN KEY (role_id) REFERENCES ROLE(role_id) ON DELETE RESTRICT
);

-- SẢN PHẨM
CREATE TABLE BRAND (
    brand_id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    logo_url VARCHAR(255)
);

CREATE TABLE CATEGORY (
    category_id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL
);

CREATE TABLE WARRANTY_POLICY (
    policy_id SERIAL PRIMARY KEY,
    policy_name VARCHAR(100) NOT NULL UNIQUE,
    duration_months INTEGER NOT NULL,
    description TEXT,
    terms_and_conditions TEXT,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE PRODUCT (
    product_id SERIAL PRIMARY KEY,
    brand_id INTEGER NOT NULL,
    warranty_policy_id INTEGER,
    product_type VARCHAR(50) NOT NULL,
    sku VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    price NUMERIC(12, 2) NOT NULL,
    stock_quantity INTEGER DEFAULT 0,
    has_serial_tracking BOOLEAN DEFAULT FALSE,
    is_active BOOLEAN DEFAULT TRUE,
    is_deleted BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_product_brand FOREIGN KEY (brand_id) REFERENCES BRAND(brand_id) ON DELETE RESTRICT,
    CONSTRAINT fk_product_warranty FOREIGN KEY (warranty_policy_id) REFERENCES WARRANTY_POLICY(policy_id) ON DELETE SET NULL
);

CREATE TABLE PRODUCT_CATEGORY (
    product_id INTEGER NOT NULL,
    category_id INTEGER NOT NULL,
    PRIMARY KEY (product_id, category_id),
    CONSTRAINT fk_pc_product FOREIGN KEY (product_id) REFERENCES PRODUCT(product_id) ON DELETE CASCADE,
    CONSTRAINT fk_pc_category FOREIGN KEY (category_id) REFERENCES CATEGORY(category_id) ON DELETE CASCADE
);

CREATE TABLE PRODUCT_IMAGE (
    image_id SERIAL PRIMARY KEY,
    product_id INTEGER NOT NULL,
    image_url VARCHAR(255) NOT NULL,
    is_primary BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_image_product FOREIGN KEY (product_id) REFERENCES PRODUCT(product_id) ON DELETE CASCADE
);

CREATE TABLE PRODUCT_BUNDLE (
    bundle_id SERIAL PRIMARY KEY,
    parent_product_id INTEGER NOT NULL,
    child_product_id INTEGER NOT NULL,
    quantity INTEGER DEFAULT 1,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_bundle_parent FOREIGN KEY (parent_product_id) REFERENCES PRODUCT(product_id) ON DELETE CASCADE,
    CONSTRAINT fk_bundle_child FOREIGN KEY (child_product_id) REFERENCES PRODUCT(product_id) ON DELETE CASCADE,
    CONSTRAINT chk_bundle_different CHECK (parent_product_id != child_product_id)
);

-- BÀI HƯỚNG DẪN
CREATE TABLE TUTORIAL (
    tutorial_id SERIAL PRIMARY KEY,
    created_by INTEGER NOT NULL,
    title VARCHAR(255) NOT NULL,
    description TEXT,
    difficulty_level difficulty_level_enum DEFAULT 'beginner',
    estimated_duration INTEGER,
    instructions TEXT,
    video_url VARCHAR(255),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_tutorial_user FOREIGN KEY (created_by) REFERENCES "USER"(user_id) ON DELETE CASCADE
);

COMMENT ON COLUMN TUTORIAL.estimated_duration IS 'Duration in minutes';

CREATE TABLE TUTORIAL_COMPONENT (
    id SERIAL PRIMARY KEY,
    tutorial_id INTEGER NOT NULL,
    product_id INTEGER NOT NULL,
    quantity INTEGER DEFAULT 1,
    usage_note TEXT,
    CONSTRAINT fk_tc_tutorial FOREIGN KEY (tutorial_id) REFERENCES TUTORIAL(tutorial_id) ON DELETE CASCADE,
    CONSTRAINT fk_tc_product FOREIGN KEY (product_id) REFERENCES PRODUCT(product_id) ON DELETE CASCADE
);

-- GIỎ HÀNG
CREATE TABLE CART (
    cart_id SERIAL PRIMARY KEY,
    user_id INTEGER NOT NULL UNIQUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_cart_user FOREIGN KEY (user_id) REFERENCES "USER"(user_id) ON DELETE CASCADE
);

CREATE TABLE CART_ITEM (
    cart_item_id SERIAL PRIMARY KEY,
    cart_id INTEGER NOT NULL,
    product_id INTEGER NOT NULL,
    quantity INTEGER DEFAULT 1,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_ci_cart FOREIGN KEY (cart_id) REFERENCES CART(cart_id) ON DELETE CASCADE,
    CONSTRAINT fk_ci_product FOREIGN KEY (product_id) REFERENCES PRODUCT(product_id) ON DELETE CASCADE,
    CONSTRAINT unique_cart_product UNIQUE (cart_id, product_id)
);

-- COUPON
CREATE TABLE COUPON (
    coupon_id SERIAL PRIMARY KEY,
    code VARCHAR(50) NOT NULL UNIQUE,
    discount_type discount_type_enum NOT NULL DEFAULT 'FIXED_AMOUNT',
    discount_value NUMERIC(10, 2) NOT NULL,
    min_order_value NUMERIC(12, 2) DEFAULT 0,
    start_date TIMESTAMP NOT NULL,
    end_date TIMESTAMP NOT NULL,
    usage_limit INTEGER,
    used_count INTEGER DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- ĐƠN HÀNG
CREATE TABLE ORDER_HEADER (
    order_id SERIAL PRIMARY KEY,
    user_id INTEGER NOT NULL,
    coupon_id INTEGER,
    order_number VARCHAR(50) NOT NULL UNIQUE,
    shipping_fee NUMERIC(10, 2) DEFAULT 0,
    subtotal_amount NUMERIC(12, 2) NOT NULL,
    discount_amount NUMERIC(10, 2) DEFAULT 0,
    total_amount NUMERIC(12, 2) NOT NULL,
    status order_status_enum DEFAULT 'PENDING',
    shipping_address TEXT NOT NULL,
    customer_name VARCHAR(100),
    customer_email VARCHAR(100),
    customer_phone VARCHAR(20),
    province VARCHAR(50),
    district VARCHAR(50),
    ward VARCHAR(50),
    street_address VARCHAR(200),
    notes TEXT,
    confirmed_at TIMESTAMP,
    shipped_at TIMESTAMP,
    delivered_at TIMESTAMP,
    cancelled_at TIMESTAMP,
    cancel_reason TEXT,
    confirmed_by INTEGER,
    shipped_by INTEGER,
    cancelled_by INTEGER,
    tracking_number VARCHAR(100),
    carrier VARCHAR(100),
    expected_delivery_date DATE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_order_user FOREIGN KEY (user_id) REFERENCES "USER"(user_id) ON DELETE RESTRICT,
    CONSTRAINT fk_order_coupon FOREIGN KEY (coupon_id) REFERENCES COUPON(coupon_id) ON DELETE SET NULL,
    CONSTRAINT fk_order_confirmed_by FOREIGN KEY (confirmed_by) REFERENCES "USER"(user_id) ON DELETE SET NULL,
    CONSTRAINT fk_order_shipped_by FOREIGN KEY (shipped_by) REFERENCES "USER"(user_id) ON DELETE SET NULL,
    CONSTRAINT fk_order_cancelled_by FOREIGN KEY (cancelled_by) REFERENCES "USER"(user_id) ON DELETE SET NULL
);

CREATE TABLE ORDER_ITEM (
    order_item_id SERIAL PRIMARY KEY,
    order_id INTEGER NOT NULL,
    product_id INTEGER NOT NULL,
    quantity INTEGER NOT NULL,
    unit_price NUMERIC(12, 2) NOT NULL,
    subtotal NUMERIC(12, 2) NOT NULL,
    product_name VARCHAR(255),
    product_sku VARCHAR(50),
    product_image_url VARCHAR(255),
    discount_amount NUMERIC(10, 2) DEFAULT 0,
    notes TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_oi_order FOREIGN KEY (order_id) REFERENCES ORDER_HEADER(order_id) ON DELETE CASCADE,
    CONSTRAINT fk_oi_product FOREIGN KEY (product_id) REFERENCES PRODUCT(product_id) ON DELETE RESTRICT
);

-- INSTANCE CHO SẢN PHẨM CÓ SERIAL
CREATE TABLE PRODUCT_INSTANCE (
    serial_number VARCHAR(100) PRIMARY KEY,
    product_id INTEGER NOT NULL,
    order_item_id INTEGER,
    manufacturing_date DATE,
    status instance_status_enum DEFAULT 'IN_STOCK',
    notes TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_instance_product FOREIGN KEY (product_id) REFERENCES PRODUCT(product_id) ON DELETE RESTRICT,
    CONSTRAINT fk_instance_order_item FOREIGN KEY (order_item_id) REFERENCES ORDER_ITEM(order_item_id) ON DELETE SET NULL
);

-- ĐÁNH GIÁ SẢN PHẨM
CREATE TABLE REVIEW (
    review_id SERIAL PRIMARY KEY,
    product_id INTEGER NOT NULL,
    user_id INTEGER NOT NULL,
    rating INTEGER NOT NULL CHECK (rating BETWEEN 1 AND 5),
    comment TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_review_product FOREIGN KEY (product_id) REFERENCES PRODUCT(product_id) ON DELETE CASCADE,
    CONSTRAINT fk_review_user FOREIGN KEY (user_id) REFERENCES "USER"(user_id) ON DELETE CASCADE
);

-- THANH TOÁN
CREATE TABLE PAYMENT (
    payment_id SERIAL PRIMARY KEY,
    order_id INTEGER NOT NULL UNIQUE,
    payment_method payment_method_enum NOT NULL,
    amount NUMERIC(12, 2) NOT NULL,
    status payment_status_enum DEFAULT 'PENDING',
    payment_date TIMESTAMP,
    transaction_id VARCHAR(100),
    bank_code VARCHAR(50),
    gateway_response TEXT,
    expired_at TIMESTAMP,
    payment_reference VARCHAR(100),
    received_amount NUMERIC(12, 2),
    qr_code_url VARCHAR(500),
    retry_count INTEGER DEFAULT 0,
    notes TEXT,
    verified_by INTEGER,
    verified_at TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_payment_order FOREIGN KEY (order_id) REFERENCES ORDER_HEADER(order_id) ON DELETE RESTRICT,
    CONSTRAINT fk_payment_verified_by FOREIGN KEY (verified_by) REFERENCES "USER"(user_id) ON DELETE SET NULL
);

-- BẢO HÀNH VÀ YÊU CẦU BẢO HÀNH
CREATE TABLE WARRANTY (
    warranty_id SERIAL PRIMARY KEY,
    serial_number VARCHAR(100) NOT NULL,
    warranty_policy_id INTEGER NOT NULL,
    start_date DATE NOT NULL,
    end_date DATE NOT NULL,
    is_active BOOLEAN DEFAULT TRUE,
    activation_date TIMESTAMP,
    notes TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_warranty_instance FOREIGN KEY (serial_number) REFERENCES PRODUCT_INSTANCE(serial_number) ON DELETE RESTRICT,
    CONSTRAINT fk_warranty_policy FOREIGN KEY (warranty_policy_id) REFERENCES WARRANTY_POLICY(policy_id) ON DELETE RESTRICT
);

CREATE TABLE WARRANTY_CLAIM (
    claim_id SERIAL PRIMARY KEY,
    warranty_id INTEGER NOT NULL,
    user_id INTEGER NOT NULL,
    claim_date DATE NOT NULL,
    issue_description TEXT NOT NULL,
    status claim_status_enum DEFAULT 'SUBMITTED',
    resolution TEXT,
    resolved_date DATE,
    contact_phone VARCHAR(20),
    resolution_note TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_claim_warranty FOREIGN KEY (warranty_id) REFERENCES WARRANTY(warranty_id) ON DELETE RESTRICT,
    CONSTRAINT fk_claim_user FOREIGN KEY (user_id) REFERENCES "USER"(user_id) ON DELETE RESTRICT
);

-- TÍCH HỢP SEPAY
CREATE TABLE SEPAY_CONFIG (
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

CREATE TABLE SEPAY_TRANSACTION (
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
    CONSTRAINT fk_sepay_order FOREIGN KEY (order_id) REFERENCES ORDER_HEADER(order_id) ON DELETE SET NULL
);

-- COMMENTS
COMMENT ON TABLE SEPAY_CONFIG IS 'Cấu hình tài khoản ngân hàng cho SePay';
COMMENT ON TABLE SEPAY_TRANSACTION IS 'Log tất cả giao dịch nhận được từ SePay webhook';
COMMENT ON COLUMN PAYMENT.payment_reference IS 'Mã tham chiếu thanh toán - nội dung chuyển khoản';
COMMENT ON COLUMN ORDER_HEADER.customer_name IS 'Snapshot tên khách hàng tại thời điểm đặt hàng';
COMMENT ON COLUMN WARRANTY_CLAIM.contact_phone IS 'SĐT liên hệ khách hàng khi gửi yêu cầu bảo hành';
COMMENT ON COLUMN WARRANTY_CLAIM.resolution_note IS 'Ghi chú xử lý từ admin/staff khi resolve claim';

-- ============================================
-- 3. TẠO INDEXES
-- ============================================
CREATE INDEX idx_user_email ON "USER"(email);
CREATE INDEX idx_user_role ON "USER"(role_id);
CREATE INDEX idx_user_phone ON "USER"(phone);
CREATE INDEX idx_user_fullname ON "USER"(full_name);

CREATE INDEX idx_product_sku ON PRODUCT(sku);
CREATE INDEX idx_product_brand ON PRODUCT(brand_id);
CREATE INDEX idx_product_type ON PRODUCT(product_type);

CREATE INDEX idx_order_user ON ORDER_HEADER(user_id);
CREATE INDEX idx_order_status ON ORDER_HEADER(status);
CREATE INDEX idx_order_number ON ORDER_HEADER(order_number);
CREATE INDEX idx_order_customer_email ON ORDER_HEADER(customer_email);
CREATE INDEX idx_order_customer_phone ON ORDER_HEADER(customer_phone);
CREATE INDEX idx_order_updated ON ORDER_HEADER(updated_at);
CREATE INDEX idx_order_tracking ON ORDER_HEADER(tracking_number);

CREATE INDEX idx_payment_status ON PAYMENT(status);
CREATE INDEX idx_payment_transaction ON PAYMENT(transaction_id);
CREATE INDEX idx_payment_reference ON PAYMENT(payment_reference);

CREATE INDEX idx_warranty_serial ON WARRANTY(serial_number);
CREATE INDEX idx_warranty_active ON WARRANTY(is_active);
CREATE INDEX idx_warranty_claim_status ON WARRANTY_CLAIM(status);
CREATE INDEX idx_warranty_claim_user ON WARRANTY_CLAIM(user_id);
CREATE INDEX idx_warranty_claim_warranty ON WARRANTY_CLAIM(warranty_id);

CREATE INDEX idx_review_product ON REVIEW(product_id);
CREATE INDEX idx_review_user ON REVIEW(user_id);

CREATE INDEX idx_coupon_discount_type ON COUPON(discount_type);
CREATE INDEX idx_sepay_config_active ON SEPAY_CONFIG(is_active);
CREATE INDEX idx_sepay_trans_order ON SEPAY_TRANSACTION(order_id);
CREATE INDEX idx_sepay_trans_code ON SEPAY_TRANSACTION(code);
CREATE INDEX idx_sepay_trans_processed ON SEPAY_TRANSACTION(is_processed);

-- ============================================
-- 4. TẠO FUNCTIONS & TRIGGERS
-- ============================================

CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

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

CREATE OR REPLACE FUNCTION generate_order_number()
RETURNS VARCHAR(50) AS $$
DECLARE
    today_str VARCHAR(8);
    seq_num INTEGER;
    order_num VARCHAR(50);
BEGIN
    today_str := TO_CHAR(CURRENT_DATE, 'YYYYMMDD');
    SELECT COUNT(*) + 1 INTO seq_num FROM ORDER_HEADER WHERE order_number LIKE 'ORD' || today_str || '%';
    order_num := 'ORD' || today_str || LPAD(seq_num::TEXT, 3, '0');
    RETURN order_num;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION generate_payment_reference(p_order_number VARCHAR)
RETURNS VARCHAR(100) AS $$
BEGIN
    RETURN 'STEM' || SUBSTRING(p_order_number FROM 4);
END;
$$ LANGUAGE plpgsql;

-- Apply Triggers
CREATE TRIGGER trigger_order_status_update BEFORE UPDATE ON ORDER_HEADER FOR EACH ROW EXECUTE FUNCTION update_order_status_timestamp();
CREATE TRIGGER trigger_payment_status_update BEFORE UPDATE ON PAYMENT FOR EACH ROW EXECUTE FUNCTION update_payment_status_timestamp();
CREATE TRIGGER update_sepay_config_updated_at BEFORE UPDATE ON SEPAY_CONFIG FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- ============================================
-- 5. TẠO VIEWS
-- ============================================

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
    p.retry_count, p.verified_by, p.verified_at, p.notes AS payment_notes
FROM ORDER_HEADER oh
LEFT JOIN PAYMENT p ON oh.order_id = p.order_id;

CREATE OR REPLACE VIEW v_order_items_detail AS
SELECT 
    oh.order_id, oh.order_number, oh.customer_name, oh.status AS order_status,
    oi.order_item_id, oi.product_id, oi.product_name, oi.product_sku, oi.product_image_url,
    oi.quantity, oi.unit_price, oi.discount_amount AS item_discount,
    oi.subtotal AS item_subtotal, oi.notes AS item_notes
FROM ORDER_HEADER oh
JOIN ORDER_ITEM oi ON oh.order_id = oi.order_id;

CREATE OR REPLACE VIEW v_sepay_payment_report AS
SELECT 
    p.payment_id, oh.order_number, oh.customer_name, oh.customer_phone,
    p.payment_reference, p.amount AS expected_amount, p.received_amount, 
    p.status AS payment_status, p.qr_code_url, p.expired_at, p.payment_date, 
    p.transaction_id, p.retry_count, st.sepay_id, st.transfer_amount, 
    st.content AS transfer_content, st.transaction_date AS transfer_date, st.is_processed
FROM PAYMENT p
JOIN ORDER_HEADER oh ON p.order_id = oh.order_id
LEFT JOIN SEPAY_TRANSACTION st ON p.payment_reference = st.code
WHERE p.payment_method = 'SEPAY';

CREATE OR REPLACE VIEW v_sepay_pending AS
SELECT 
    st.*, oh.order_number, oh.total_amount AS expected_amount
FROM SEPAY_TRANSACTION st
LEFT JOIN ORDER_HEADER oh ON st.order_id = oh.order_id
WHERE st.is_processed = FALSE;

-- ============================================
-- 6. SEED DATA (FULL DỮ LIỆU GỐC)
-- ============================================

INSERT INTO ROLE (role_id, role_name) VALUES 
(1, 'Admin'),
(2, 'Customer'),
(3, 'Staff');

INSERT INTO WARRANTY_POLICY (policy_id, policy_name, duration_months, description, terms_and_conditions, is_active) VALUES
(1, 'Standard Warranty', 12, 'Bao hanh tieu chuan 1 nam', 'Bao hanh loi do nha san xuat. Khong bao hanh loi do nguoi dung gay ra, roi vo, ngam nuoc.', TRUE),
(2, 'Extended Warranty', 24, 'Bao hanh mo rong 2 nam', 'Bao hanh toan dien 2 nam, bao gom ca loi phan cung va ho tro ky thuat.', TRUE),
(3, 'Premium Warranty', 36, 'Bao hanh cao cap 3 nam', 'Bao hanh VIP 3 nam, doi moi trong 30 ngay dau, ho tro ky thuat 24/7.', TRUE);

INSERT INTO BRAND (brand_id, name, logo_url) VALUES
(1, 'Arduino', '/images/brands/arduino.png'),
(2, 'Raspberry Pi', '/images/brands/rpi.png'),
(3, 'ESP32/Espressif', '/images/brands/esp32.png'),
(4, 'Adafruit', '/images/brands/adafruit.png'),
(5, 'SparkFun', '/images/brands/sparkfun.png'),
(6, 'Seeed Studio', '/images/brands/seeed.png'),
(7, 'STMicroelectronics', '/images/brands/stm.png'),
(8, 'Texas Instruments', '/images/brands/ti.png');

INSERT INTO CATEGORY (category_id, name) VALUES
(1, 'Microcontrollers'), (2, 'Sensors'), (3, 'Actuators'), (4, 'Power Supply'),
(5, 'Communication Modules'), (6, 'Development Kits'), (7, 'Tools & Accessories'),
(8, 'Displays'), (9, 'Storage'), (10, 'Cables & Connectors');

INSERT INTO "USER" (user_id, role_id, username, full_name, email, password_hash, phone, address, is_active, created_at) VALUES
(1, 1, 'admin', 'admin', 'admin@stemstore.vn', '$2a$10$abcdefghijklmnopqrstuvwxyz1234567890', '0901234567', '123 Nguyen Hue, Q.1, TP.HCM', TRUE, '2025-01-01 08:00:00'),
(2, 3, 'staff_nguyen', 'staff_nguyen', 'staff.nguyen@stemstore.vn', '$2a$10$abcdefghijklmnopqrstuvwxyz1234567891', '0902345678', '456 Le Loi, Q.1, TP.HCM', TRUE, '2025-01-02 09:00:00'),
(3, 3, 'staff_tran', 'staff_tran', 'staff.tran@stemstore.vn', '$2a$10$abcdefghijklmnopqrstuvwxyz1234567892', '0903456789', '789 Hai Ba Trung, Q.3, TP.HCM', TRUE, '2025-01-03 09:00:00'),
(4, 2, 'nguyenvana', 'nguyenvana', 'nguyenvana@gmail.com', '$2a$10$abcdefghijklmnopqrstuvwxyz1234567893', '0904567890', '12 Tran Hung Dao, Q.5, TP.HCM', TRUE, '2025-01-05 10:30:00'),
(5, 2, 'tranthib', 'tranthib', 'tranthib@gmail.com', '$2a$10$abcdefghijklmnopqrstuvwxyz1234567894', '0905678901', '34 Ly Thuong Kiet, Q.10, TP.HCM', TRUE, '2025-01-06 11:00:00'),
(6, 2, 'levanc', 'levanc', 'levanc@gmail.com', '$2a$10$abcdefghijklmnopqrstuvwxyz1234567895', '0906789012', '56 Vo Van Tan, Q.3, TP.HCM', TRUE, '2025-01-07 14:20:00'),
(7, 2, 'phamthid', 'phamthid', 'phamthid@gmail.com', '$2a$10$abcdefghijklmnopqrstuvwxyz1234567896', '0907890123', '78 Cach Mang Thang 8, Q.Tan Binh, TP.HCM', TRUE, '2025-01-08 15:45:00'),
(8, 2, 'hoangvane', 'hoangvane', 'hoangvane@gmail.com', '$2a$10$abcdefghijklmnopqrstuvwxyz1234567897', '0908901234', '90 Nguyen Thi Minh Khai, Q.1, TP.HCM', TRUE, '2025-01-10 09:15:00'),
(9, 2, 'vuthif', 'vuthif', 'vuthif@gmail.com', '$2a$10$abcdefghijklmnopqrstuvwxyz1234567898', '0909012345', '123 Dien Bien Phu, Q.Binh Thanh, TP.HCM', TRUE, '2025-01-11 10:30:00'),
(10, 2, 'dovanh', 'dovanh', 'dovanh@gmail.com', '$2a$10$abcdefghijklmnopqrstuvwxyz1234567899', '0910123456', '45 Phan Xich Long, Q.Phu Nhuan, TP.HCM', TRUE, '2025-01-12 11:00:00'),
(11, 2, 'buithi', 'buithi', 'buithi@gmail.com', '$2a$10$abcdefghijklmnopqrstuvwxyz1234567800', '0911234567', '67 Hoang Van Thu, Q.Tan Binh, TP.HCM', TRUE, '2025-01-13 13:20:00'),
(12, 2, 'ngothij', 'ngothij', 'ngothij@gmail.com', '$2a$10$abcdefghijklmnopqrstuvwxyz1234567801', '0912345678', '89 Truong Chinh, Q.12, TP.HCM', TRUE, '2025-01-14 14:45:00'),
(13, 2, 'lythik', 'lythik', 'lythik@gmail.com', '$2a$10$abcdefghijklmnopqrstuvwxyz1234567802', '0913456789', '101 Lac Long Quan, Q.11, TP.HCM', TRUE, '2025-01-15 16:00:00'),
(14, 2, 'dangvanl', 'dangvanl', 'dangvanl@gmail.com', '$2a$10$abcdefghijklmnopqrstuvwxyz1234567803', '0914567890', '234 Au Co, Q.Tan Phu, TP.HCM', TRUE, '2025-01-16 08:30:00'),
(15, 2, 'vovanm', 'vovanm', 'vovanm@gmail.com', '$2a$10$abcdefghijklmnopqrstuvwxyz1234567804', '0915678901', '345 Phan Van Tri, Q.Go Vap, TP.HCM', TRUE, '2025-01-18 10:00:00');

INSERT INTO PRODUCT (product_id, brand_id, warranty_policy_id, product_type, sku, name, description, price, stock_quantity, has_serial_tracking, created_at) VALUES
(1, 1, 1, 'MODULE', 'ARD-UNO-R3', 'Arduino Uno R3', 'Bo mach Arduino Uno R3 chinh hang...', 350000, 50, FALSE, '2025-01-01 10:00:00'),
(2, 1, 1, 'MODULE', 'ARD-MEGA-2560', 'Arduino Mega 2560', 'Bo mach Arduino Mega 2560...', 520000, 30, FALSE, '2025-01-01 10:05:00'),
(3, 1, 1, 'MODULE', 'ARD-NANO', 'Arduino Nano V3', 'Bo mach Arduino Nano...', 180000, 80, FALSE, '2025-01-01 10:10:00'),
(4, 1, 2, 'KIT', 'ARD-STARTER-KIT', 'Arduino Starter Kit', 'Bo kit hoc Arduino day du...', 1250000, 25, FALSE, '2025-01-01 10:15:00'),
(5, 2, 2, 'MODULE', 'RPI-4B-4GB', 'Raspberry Pi 4 Model B 4GB', 'Raspberry Pi 4B voi 4GB RAM...', 1450000, 40, TRUE, '2025-01-01 10:20:00'),
(6, 2, 2, 'MODULE', 'RPI-4B-8GB', 'Raspberry Pi 4 Model B 8GB', 'Raspberry Pi 4B phien ban 8GB RAM...', 1850000, 20, TRUE, '2025-01-01 10:25:00'),
(7, 2, 1, 'MODULE', 'RPI-PICO', 'Raspberry Pi Pico', 'Vi dieu khien Raspberry Pi Pico...', 85000, 100, FALSE, '2025-01-01 10:30:00'),
(8, 2, 2, 'KIT', 'RPI-4-COMPLETE-KIT', 'Raspberry Pi 4 Complete Kit', 'Bo kit day du: Raspberry Pi 4B...', 2100000, 15, TRUE, '2025-01-01 10:35:00'),
(9, 3, 1, 'MODULE', 'ESP32-DEVKIT', 'ESP32 DevKit V1', 'Module ESP32 voi WiFi + Bluetooth...', 120000, 150, FALSE, '2025-01-01 10:40:00'),
(10, 3, 1, 'MODULE', 'ESP32-CAM', 'ESP32-CAM', 'Module ESP32 tich hop camera...', 150000, 60, FALSE, '2025-01-01 10:45:00'),
(11, 3, 1, 'MODULE', 'ESP8266-NODEMCU', 'ESP8266 NodeMCU', 'Module WiFi ESP8266...', 75000, 200, FALSE, '2025-01-01 10:50:00'),
(12, 4, 1, 'COMPONENT', 'DHT22-TEMP-HUM', 'DHT22 Temperature Humidity Sensor', 'Cam bien nhiet do va do am DHT22...', 95000, 120, FALSE, '2025-01-01 11:00:00'),
(13, 4, 1, 'COMPONENT', 'HC-SR04-ULTRASONIC', 'HC-SR04 Ultrasonic Sensor', 'Cam bien sieu am do khoang cach...', 45000, 180, FALSE, '2025-01-01 11:05:00'),
(14, 5, 1, 'COMPONENT', 'PIR-HC-SR501', 'PIR Motion Sensor HC-SR501', 'Cam bien chuyen dong hong ngoai PIR...', 35000, 150, FALSE, '2025-01-01 11:10:00'),
(15, 4, 1, 'COMPONENT', 'MQ-2-GAS-SENSOR', 'MQ-2 Gas Smoke Sensor', 'Cam bien khi gas va khoi MQ-2...', 55000, 90, FALSE, '2025-01-01 11:15:00'),
(16, 5, 1, 'COMPONENT', 'BMP280-PRESSURE', 'BMP280 Pressure Sensor', 'Cam bien ap suat khi quyen BMP280...', 65000, 75, FALSE, '2025-01-01 11:20:00'),
(17, 6, 1, 'COMPONENT', 'SG90-SERVO', 'SG90 Micro Servo Motor 9g', 'Servo motor mini SG90...', 38000, 200, FALSE, '2025-01-01 11:25:00'),
(18, 6, 1, 'COMPONENT', 'MG996R-SERVO', 'MG996R High Torque Servo', 'Servo motor MG996R...', 115000, 80, FALSE, '2025-01-01 11:30:00'),
(19, 5, 1, 'COMPONENT', '28BYJ-48-STEPPER', '28BYJ-48 Stepper Motor + Driver', 'Dong co buoc 28BYJ-48...', 55000, 100, FALSE, '2025-01-01 11:35:00'),
(20, 6, 1, 'COMPONENT', 'L298N-MOTOR-DRIVER', 'L298N Motor Driver Module', 'Module dieu khien 2 dong co DC...', 68000, 90, FALSE, '2025-01-01 11:40:00'),
(21, 4, 1, 'COMPONENT', 'LCD-1602-I2C', 'LCD 1602 Display with I2C', 'Man hinh LCD 16x2...', 85000, 110, FALSE, '2025-01-01 11:45:00'),
(22, 4, 1, 'COMPONENT', 'OLED-096-I2C', 'OLED 0.96 inch I2C Display', 'Man hinh OLED 0.96 inch...', 95000, 130, FALSE, '2025-01-01 11:50:00'),
(23, 6, 1, 'COMPONENT', 'TFT-24-TOUCH', 'TFT 2.4 inch Touch Screen', 'Man hinh cam ung TFT 2.4 inch...', 185000, 60, FALSE, '2025-01-01 11:55:00'),
(24, 7, 1, 'COMPONENT', 'MB102-POWER', 'MB102 Breadboard Power Supply', 'Module nguon cho breadboard...', 28000, 180, FALSE, '2025-01-01 12:00:00'),
(25, 8, 1, 'COMPONENT', 'AMS1117-REGULATOR', 'AMS1117 3.3V Voltage Regulator', 'Module on ap AMS1117...', 18000, 250, FALSE, '2025-01-01 12:05:00'),
(26, 7, 1, 'COMPONENT', 'TP4056-CHARGER', 'TP4056 Li-ion Battery Charger', 'Module sac pin lithium...', 25000, 200, FALSE, '2025-01-01 12:10:00'),
(27, 5, 1, 'COMPONENT', 'NRF24L01-WIRELESS', 'NRF24L01+ Wireless Module', 'Module truyen thong khong day...', 48000, 85, FALSE, '2025-01-01 12:15:00'),
(28, 5, 1, 'COMPONENT', 'HC-05-BLUETOOTH', 'HC-05 Bluetooth Module', 'Module Bluetooth HC-05...', 85000, 95, FALSE, '2025-01-01 12:20:00'),
(29, 4, 2, 'KIT', 'SENSOR-KIT-37IN1', 'Sensor Kit 37 in 1', 'Bo 37 cam bien da dang...', 680000, 35, FALSE, '2025-01-01 12:25:00'),
(30, 5, 2, 'KIT', 'ROBOTICS-STARTER-KIT', 'Robotics Starter Kit', 'Bo kit lam robot...', 950000, 20, FALSE, '2025-01-01 12:30:00');

INSERT INTO PRODUCT_CATEGORY (product_id, category_id) VALUES
(1, 1), (1, 6), (2, 1), (2, 6), (3, 1), (3, 6), (4, 6), (4, 7),
(5, 1), (5, 6), (6, 1), (6, 6), (7, 1), (8, 6), (8, 7), (9, 1), (9, 5), (9, 6),
(10, 1), (10, 5), (10, 2), (11, 1), (11, 5), (12, 2), (13, 2), (14, 2), (15, 2),
(16, 2), (17, 3), (18, 3), (19, 3), (20, 3), (20, 7), (21, 8), (22, 8), (23, 8),
(24, 4), (24, 7), (25, 4), (26, 4), (27, 5), (28, 5), (29, 2), (29, 6), (29, 7),
(30, 3), (30, 6), (30, 7);

INSERT INTO PRODUCT_IMAGE (image_id, product_id, image_url, created_at) VALUES
(1, 1, '/images/products/arduino-uno-1.jpg', '2025-01-01 10:00:00'), (2, 1, '/images/products/arduino-uno-2.jpg', '2025-01-01 10:00:00'),
(3, 2, '/images/products/arduino-mega-1.jpg', '2025-01-01 10:05:00'), (4, 2, '/images/products/arduino-mega-2.jpg', '2025-01-01 10:05:00'),
(5, 3, '/images/products/arduino-nano-1.jpg', '2025-01-01 10:10:00'), (6, 3, '/images/products/arduino-nano-2.jpg', '2025-01-01 10:10:00'),
(7, 4, '/images/products/arduino-starter-kit-1.jpg', '2025-01-01 10:15:00'), (8, 4, '/images/products/arduino-starter-kit-2.jpg', '2025-01-01 10:15:00'), (9, 4, '/images/products/arduino-starter-kit-3.jpg', '2025-01-01 10:15:00'),
(10, 5, '/images/products/rpi4-4gb-1.jpg', '2025-01-01 10:20:00'), (11, 5, '/images/products/rpi4-4gb-2.jpg', '2025-01-01 10:20:00'),
(12, 6, '/images/products/rpi4-8gb-1.jpg', '2025-01-01 10:25:00'), (13, 6, '/images/products/rpi4-8gb-2.jpg', '2025-01-01 10:25:00'),
(14, 7, '/images/products/rpi-pico-1.jpg', '2025-01-01 10:30:00'), (15, 7, '/images/products/rpi-pico-2.jpg', '2025-01-01 10:30:00'),
(16, 8, '/images/products/rpi-complete-kit-1.jpg', '2025-01-01 10:35:00'), (17, 8, '/images/products/rpi-complete-kit-2.jpg', '2025-01-01 10:35:00'), (18, 8, '/images/products/rpi-complete-kit-3.jpg', '2025-01-01 10:35:00'),
(19, 9, '/images/products/esp32-devkit-1.jpg', '2025-01-01 10:40:00'), (20, 9, '/images/products/esp32-devkit-2.jpg', '2025-01-01 10:40:00'),
(21, 10, '/images/products/esp32-cam-1.jpg', '2025-01-01 10:45:00'), (22, 10, '/images/products/esp32-cam-2.jpg', '2025-01-01 10:45:00'),
(23, 11, '/images/products/esp8266-1.jpg', '2025-01-01 10:50:00'), (24, 11, '/images/products/esp8266-2.jpg', '2025-01-01 10:50:00'),
(25, 12, '/images/products/dht22-1.jpg', '2025-01-01 11:00:00'), (26, 12, '/images/products/dht22-2.jpg', '2025-01-01 11:00:00'),
(27, 13, '/images/products/hc-sr04-1.jpg', '2025-01-01 11:05:00'), (28, 13, '/images/products/hc-sr04-2.jpg', '2025-01-01 11:05:00'),
(29, 14, '/images/products/pir-1.jpg', '2025-01-01 11:10:00'), (30, 14, '/images/products/pir-2.jpg', '2025-01-01 11:10:00'),
(31, 15, '/images/products/mq2-1.jpg', '2025-01-01 11:15:00'), (32, 15, '/images/products/mq2-2.jpg', '2025-01-01 11:15:00'),
(33, 16, '/images/products/bmp280-1.jpg', '2025-01-01 11:20:00'), (34, 16, '/images/products/bmp280-2.jpg', '2025-01-01 11:20:00'),
(35, 17, '/images/products/sg90-1.jpg', '2025-01-01 11:25:00'), (36, 17, '/images/products/sg90-2.jpg', '2025-01-01 11:25:00'),
(37, 18, '/images/products/mg996r-1.jpg', '2025-01-01 11:30:00'), (38, 18, '/images/products/mg996r-2.jpg', '2025-01-01 11:30:00'),
(39, 19, '/images/products/stepper-1.jpg', '2025-01-01 11:35:00'), (40, 19, '/images/products/stepper-2.jpg', '2025-01-01 11:35:00'),
(41, 20, '/images/products/l298n-1.jpg', '2025-01-01 11:40:00'), (42, 20, '/images/products/l298n-2.jpg', '2025-01-01 11:40:00'),
(43, 21, '/images/products/lcd1602-1.jpg', '2025-01-01 11:45:00'), (44, 21, '/images/products/lcd1602-2.jpg', '2025-01-01 11:45:00'),
(45, 22, '/images/products/oled-1.jpg', '2025-01-01 11:50:00'), (46, 22, '/images/products/oled-2.jpg', '2025-01-01 11:50:00'),
(47, 23, '/images/products/tft-1.jpg', '2025-01-01 11:55:00'), (48, 23, '/images/products/tft-2.jpg', '2025-01-01 11:55:00'),
(49, 24, '/images/products/mb102-1.jpg', '2025-01-01 12:00:00'), (50, 24, '/images/products/mb102-2.jpg', '2025-01-01 12:00:00'),
(51, 25, '/images/products/ams1117-1.jpg', '2025-01-01 12:05:00'), (52, 25, '/images/products/ams1117-2.jpg', '2025-01-01 12:05:00'),
(53, 26, '/images/products/tp4056-1.jpg', '2025-01-01 12:10:00'), (54, 26, '/images/products/tp4056-2.jpg', '2025-01-01 12:10:00'),
(55, 27, '/images/products/nrf24l01-1.jpg', '2025-01-01 12:15:00'), (56, 27, '/images/products/nrf24l01-2.jpg', '2025-01-01 12:15:00'),
(57, 28, '/images/products/hc05-1.jpg', '2025-01-01 12:20:00'), (58, 28, '/images/products/hc05-2.jpg', '2025-01-01 12:20:00'),
(59, 29, '/images/products/sensor-kit-1.jpg', '2025-01-01 12:25:00'), (60, 29, '/images/products/sensor-kit-2.jpg', '2025-01-01 12:25:00'), (61, 29, '/images/products/sensor-kit-3.jpg', '2025-01-01 12:25:00'),
(62, 30, '/images/products/robotics-kit-1.jpg', '2025-01-01 12:30:00'), (63, 30, '/images/products/robotics-kit-2.jpg', '2025-01-01 12:30:00'), (64, 30, '/images/products/robotics-kit-3.jpg', '2025-01-01 12:30:00');

INSERT INTO PRODUCT_BUNDLE (bundle_id, parent_product_id, child_product_id, quantity, created_at) VALUES
(1, 4, 1, 1, '2025-01-01 10:15:00'), (2, 4, 12, 1, '2025-01-01 10:15:00'), (3, 4, 13, 1, '2025-01-01 10:15:00'),
(4, 4, 17, 2, '2025-01-01 10:15:00'), (5, 4, 21, 1, '2025-01-01 10:15:00'),
(6, 8, 6, 1, '2025-01-01 10:35:00'), (7, 8, 24, 1, '2025-01-01 10:35:00'),
(8, 29, 12, 1, '2025-01-01 12:25:00'), (9, 29, 13, 1, '2025-01-01 12:25:00'), (10, 29, 14, 1, '2025-01-01 12:25:00'),
(11, 29, 15, 1, '2025-01-01 12:25:00'), (12, 29, 16, 1, '2025-01-01 12:25:00'),
(13, 30, 3, 1, '2025-01-01 12:30:00'), (14, 30, 13, 2, '2025-01-01 12:30:00'), (15, 30, 19, 2, '2025-01-01 12:30:00'),
(16, 30, 20, 1, '2025-01-01 12:30:00'), (17, 30, 26, 1, '2025-01-01 12:30:00');

INSERT INTO PRODUCT_INSTANCE (serial_number, product_id, order_item_id, manufacturing_date, status, notes, created_at) VALUES
('RPI4-4GB-2024120001', 5, NULL, '2024-12-01', 'IN_STOCK', 'Chua ban', '2024-12-15 10:00:00'),
('RPI4-4GB-2024120002', 5, NULL, '2024-12-01', 'IN_STOCK', 'Chua ban', '2024-12-15 10:00:00'),
('RPI4-8GB-2024120001', 6, NULL, '2024-12-01', 'IN_STOCK', 'Chua ban', '2024-12-15 10:00:00'),
('RPI4-8GB-2024120002', 6, NULL, '2024-12-01', 'IN_STOCK', 'Chua ban', '2024-12-15 10:00:00'),
('RPI-KIT-2024120001', 8, NULL, '2024-12-05', 'IN_STOCK', 'Bo kit day du', '2024-12-20 10:00:00'),
('RPI-KIT-2024120004', 8, NULL, '2024-12-05', 'IN_STOCK', 'Chua ban', '2024-12-20 10:00:00');

INSERT INTO REVIEW (review_id, product_id, user_id, rating, comment, created_at) VALUES
(1, 1, 4, 5, 'Arduino Uno rat tot cho nguoi moi bat dau...', '2025-01-10 14:30:00'), (2, 1, 5, 5, 'Chat luong tuyet voi...', '2025-01-12 16:45:00'),
(3, 1, 6, 4, 'San pham tot, gia hop ly...', '2025-01-15 10:20:00'), (4, 5, 7, 5, 'Raspberry Pi 4B 4GB rat manh...', '2025-01-11 11:00:00'),
(5, 5, 8, 5, 'Performance tuyet voi...', '2025-01-13 15:30:00'), (6, 6, 9, 5, '8GB RAM qua du cho moi tac vu...', '2025-01-14 09:15:00'),
(7, 9, 10, 5, 'ESP32 DevKit gia re ma chat luong tot...', '2025-01-12 13:20:00'), (8, 9, 11, 4, 'Module tot, lap trinh de dang...', '2025-01-16 14:00:00'),
(9, 10, 12, 5, 'ESP32-CAM tuyet voi cho cac du an camera IoT...', '2025-01-15 16:30:00'), (10, 12, 13, 5, 'Cam bien DHT22 do nhiet do...', '2025-01-13 10:45:00'),
(11, 12, 14, 4, 'Sensor on, do chinh xac cao...', '2025-01-17 11:20:00'), (12, 13, 15, 5, 'HC-SR04 do khoang cach rat tot...', '2025-01-14 15:00:00'),
(13, 17, 4, 5, 'Servo SG90 nho gon, hoat dong em...', '2025-01-16 09:30:00'), (14, 21, 5, 4, 'LCD 1602 voi I2C rat tien...', '2025-01-18 10:15:00'),
(15, 22, 6, 5, 'Man hinh OLED hien thi sac net...', '2025-01-17 13:40:00'), (16, 4, 7, 5, 'Arduino Starter Kit day du...', '2025-01-18 14:20:00'),
(17, 29, 8, 5, 'Bo 37 sensors da dang...', '2025-01-19 11:00:00'), (18, 30, 9, 4, 'Robotics Kit kha day du...', '2025-01-20 15:30:00'),
(19, 28, 10, 5, 'HC-05 Bluetooth ket noi on dinh...', '2025-01-19 16:45:00'), (20, 20, 11, 5, 'L298N Driver manh me...', '2025-01-21 10:00:00');

INSERT INTO TUTORIAL (tutorial_id, created_by, title, description, difficulty_level, estimated_duration, instructions, video_url, created_at) VALUES
(1, 1, 'Bat dau voi Arduino Uno - LED Blink', 'Huong dan co ban nhat...', 'beginner', 30, 'Buoc 1...', 'https://youtube.com/...', '2025-01-05 10:00:00'),
(2, 1, 'Do nhiet do va do am voi DHT22', 'Huong dan su dung cam bien...', 'beginner', 45, 'Buoc 1...', 'https://youtube.com/...', '2025-01-06 11:00:00'),
(3, 2, 'Robot tranh vat can voi Ultrasonic', 'Xay dung robot di chuyen...', 'intermediate', 120, 'Buoc 1...', 'https://youtube.com/...', '2025-01-08 14:00:00'),
(4, 2, 'Hien thi du lieu len man hinh OLED', 'Huong dan su dung man hinh...', 'intermediate', 60, 'Buoc 1...', 'https://youtube.com/...', '2025-01-10 15:30:00'),
(5, 1, 'Home Automation voi ESP32 va Blynk', 'Xay dung he thong nha thong minh...', 'intermediate', 90, 'Buoc 1...', 'https://youtube.com/...', '2025-01-12 16:00:00'),
(6, 2, 'Raspberry Pi - Setup va cai dat Home Assistant', 'Huong dan cai dat...', 'advanced', 180, 'Buoc 1...', 'https://youtube.com/...', '2025-01-14 10:00:00'),
(7, 1, 'Dieu khien Servo Motor theo goc', 'Hoc cach dieu khien servo...', 'beginner', 40, 'Buoc 1...', 'https://youtube.com/...', '2025-01-16 11:30:00'),
(8, 2, 'IoT - Giam sat moi truong real-time', 'Xay dung he thong giam sat...', 'advanced', 150, 'Buoc 1...', 'https://youtube.com/...', '2025-01-18 13:00:00');

INSERT INTO TUTORIAL_COMPONENT (id, tutorial_id, product_id, quantity, usage_note) VALUES
(1, 1, 1, 1, 'Bo mach Arduino Uno R3'), (2, 2, 1, 1, 'Bo mach Arduino Uno R3'), (3, 2, 12, 1, 'Cam bien nhiet do va do am DHT22'),
(4, 3, 3, 1, 'Arduino Nano de gan tren robot'), (5, 3, 13, 1, 'Cam bien sieu am HC-SR04'), (6, 3, 20, 1, 'Module dieu khien dong co L298N'), (7, 3, 26, 1, 'Module sac pin TP4056'),
(8, 4, 1, 1, 'Arduino Uno R3'), (9, 4, 22, 1, 'Man hinh OLED 0.96 inch'), (10, 5, 9, 1, 'ESP32 DevKit V1 co WiFi'), (11, 5, 12, 1, 'Cam bien DHT22'),
(12, 6, 5, 1, 'Raspberry Pi 4B 4GB hoac 8GB'), (13, 6, 24, 1, 'Module nguon 5V'), (14, 7, 1, 1, 'Arduino Uno R3'), (15, 7, 17, 2, '2 servo motor SG90'),
(16, 8, 9, 1, 'ESP32 DevKit co WiFi'), (17, 8, 12, 1, 'Cam bien DHT22'), (18, 8, 16, 1, 'Cam bien ap suat BMP280'), (19, 8, 22, 1, 'Man hinh OLED de hien thi local');

INSERT INTO CART (cart_id, user_id, created_at) VALUES
(1, 4, '2025-01-20 10:00:00'), (2, 5, '2025-01-21 11:30:00'), (3, 6, '2025-01-22 14:00:00'),
(4, 12, '2025-01-23 09:15:00'), (5, 13, '2025-01-24 16:20:00'), (6, 14, '2025-01-25 10:45:00');

INSERT INTO CART_ITEM (cart_item_id, cart_id, product_id, quantity, created_at) VALUES
(1, 1, 2, 1, '2025-01-20 10:05:00'), (2, 1, 22, 1, '2025-01-20 10:10:00'), (3, 1, 12, 2, '2025-01-20 10:15:00'),
(4, 2, 9, 2, '2025-01-21 11:35:00'), (5, 2, 28, 1, '2025-01-21 11:40:00'), (6, 3, 30, 1, '2025-01-22 14:05:00'),
(7, 3, 26, 2, '2025-01-22 14:10:00'), (8, 4, 1, 1, '2025-01-23 09:20:00'), (9, 4, 29, 1, '2025-01-23 09:25:00'),
(10, 4, 21, 1, '2025-01-23 09:30:00'), (11, 5, 7, 3, '2025-01-24 16:25:00'), (12, 5, 17, 5, '2025-01-24 16:30:00'),
(13, 6, 5, 1, '2025-01-25 10:50:00'), (14, 6, 23, 1, '2025-01-25 10:55:00');

INSERT INTO COUPON (coupon_id, code, discount_type, discount_value, min_order_value, start_date, end_date, usage_limit, used_count, created_at) VALUES
(1, 'WELCOME2025', 'PERCENTAGE', 10.00, 500000, '2025-01-01 00:00:00', '2025-03-31 23:59:59', 100, 5, '2025-01-01 08:00:00'),
(2, 'NEWYEAR50K', 'FIXED_AMOUNT', 50000, 1000000, '2025-01-01 00:00:00', '2025-01-31 23:59:59', 50, 3, '2025-01-01 08:00:00'),
(3, 'STUDENT15', 'PERCENTAGE', 15.00, 300000, '2025-01-01 00:00:00', '2025-12-31 23:59:59', NULL, 8, '2025-01-01 08:00:00'),
(4, 'FREESHIP', 'FIXED_AMOUNT', 30000, 0, '2025-01-15 00:00:00', '2025-02-15 23:59:59', 200, 12, '2025-01-15 08:00:00'),
(5, 'RASPBERRY20', 'PERCENTAGE', 20.00, 1500000, '2025-01-10 00:00:00', '2025-02-28 23:59:59', 30, 2, '2025-01-10 08:00:00'),
(6, 'EXPIRED', 'PERCENTAGE', 25.00, 500000, '2024-12-01 00:00:00', '2024-12-31 23:59:59', 50, 50, '2024-12-01 08:00:00'),
(7, 'MEGA100K', 'FIXED_AMOUNT', 100000, 2000000, '2025-01-20 00:00:00', '2025-03-31 23:59:59', 20, 1, '2025-01-20 08:00:00');

-- Sửa shiping_fee -> shipping_fee
INSERT INTO ORDER_HEADER (order_id, user_id, coupon_id, order_number, shipping_fee, subtotal_amount, discount_amount, total_amount, status, shipping_address, created_at) VALUES
(1, 4, 1, 'ORD-20250105-001', 30000, 2200000, 220000, 2010000, 'DELIVERED', '12 Tran Hung Dao, Q.5, TP.HCM', '2025-01-05 14:30:00'),
(2, 5, NULL, 'ORD-20250106-002', 30000, 520000, 0, 550000, 'DELIVERED', '34 Ly Thuong Kiet, Q.10, TP.HCM', '2025-01-06 10:15:00'),
(3, 6, 2, 'ORD-20250107-003', 30000, 1600000, 50000, 1580000, 'SHIPPED', '56 Vo Van Tan, Q.3, TP.HCM', '2025-01-07 16:20:00'),
(4, 7, NULL, 'ORD-20250108-004', 30000, 2100000, 0, 2130000, 'CONFIRMED', '78 Cach Mang Thang 8, Q.Tan Binh, TP.HCM', '2025-01-08 11:00:00'),
(5, 8, 3, 'ORD-20250109-005', 30000, 1535000, 230250, 1334750, 'DELIVERED', '90 Nguyen Thi Minh Khai, Q.1, TP.HCM', '2025-01-09 09:45:00'),
(6, 9, 4, 'ORD-20250110-006', 0, 2100000, 30000, 2070000, 'DELIVERED', '123 Dien Bien Phu, Q.Binh Thanh, TP.HCM', '2025-01-10 15:30:00'),
(7, 10, NULL, 'ORD-20250111-007', 30000, 680000, 0, 710000, 'SHIPPED', '45 Phan Xich Long, Q.Phu Nhuan, TP.HCM', '2025-01-11 13:20:00'),
(8, 11, 1, 'ORD-20250112-008', 30000, 930000, 93000, 867000, 'CONFIRMED', '67 Hoang Van Thu, Q.Tan Binh, TP.HCM', '2025-01-12 10:00:00'),
(9, 12, NULL, 'ORD-20250113-009', 30000, 1450000, 0, 1480000, 'PENDING', '89 Truong Chinh, Q.12, TP.HCM', '2025-01-13 14:45:00'),
(10, 13, NULL, 'ORD-20250114-010', 30000, 380000, 0, 410000, 'DELIVERED', '101 Lac Long Quan, Q.11, TP.HCM', '2025-01-14 16:10:00'),
(11, 14, 3, 'ORD-20250115-011', 30000, 950000, 142500, 837500, 'CANCELLED', '234 Au Co, Q.Tan Phu, TP.HCM', '2025-01-15 11:30:00'),
(12, 15, NULL, 'ORD-20250116-012', 30000, 1850000, 0, 1880000, 'DELIVERED', '345 Phan Van Tri, Q.Go Vap, TP.HCM', '2025-01-16 09:20:00'),
(13, 4, 5, 'ORD-20250117-013', 30000, 3700000, 740000, 2990000, 'CONFIRMED', '12 Tran Hung Dao, Q.5, TP.HCM', '2025-01-17 15:00:00'),
(14, 5, NULL, 'ORD-20250118-014', 30000, 180000, 0, 210000, 'PENDING', '34 Ly Thuong Kiet, Q.10, TP.HCM', '2025-01-18 10:30:00'),
(15, 6, NULL, 'ORD-20250119-015', 30000, 635000, 0, 665000, 'DELIVERED', '56 Vo Van Tan, Q.3, TP.HCM', '2025-01-19 13:45:00');

INSERT INTO ORDER_ITEM (order_item_id, order_id, product_id, quantity, unit_price, subtotal) VALUES
(1, 1, 6, 1, 1850000, 1850000), (2, 1, 29, 1, 680000, 680000), (3, 2, 2, 1, 520000, 520000),
(4, 3, 5, 1, 1450000, 1450000), (5, 3, 22, 1, 95000, 95000), (6, 3, 17, 2, 38000, 76000),
(7, 4, 8, 1, 2100000, 2100000), (8, 5, 5, 1, 1450000, 1450000), (9, 5, 12, 1, 95000, 95000),
(10, 5, 21, 1, 85000, 85000), (11, 6, 8, 1, 2100000, 2100000), (12, 7, 29, 1, 680000, 680000),
(13, 8, 30, 1, 950000, 950000), (14, 9, 5, 1, 1450000, 1450000), (15, 10, 9, 2, 120000, 240000),
(16, 10, 12, 1, 95000, 95000), (17, 10, 14, 1, 35000, 35000), (18, 11, 30, 1, 950000, 950000),
(19, 12, 6, 1, 1850000, 1850000), (20, 13, 6, 2, 1850000, 3700000), (21, 14, 3, 1, 180000, 180000),
(22, 15, 1, 1, 350000, 350000), (23, 15, 12, 1, 95000, 95000), (24, 15, 13, 1, 45000, 45000),
(25, 15, 21, 1, 85000, 85000), (26, 15, 17, 2, 38000, 76000);

-- Update ORDER_ITEM mapping into PRODUCT_INSTANCE cho các sp đã bán
INSERT INTO PRODUCT_INSTANCE (serial_number, product_id, order_item_id, manufacturing_date, status, notes, created_at)
SELECT 'RPI4-4GB-2024120003', 5, order_item_id, '2024-12-01', 'SOLD', 'Da ban cho khach hang', '2024-12-15 10:00:00' FROM ORDER_ITEM WHERE order_id = 3 AND product_id = 5 LIMIT 1;
INSERT INTO PRODUCT_INSTANCE (serial_number, product_id, order_item_id, manufacturing_date, status, notes, created_at)
SELECT 'RPI4-4GB-2024120004', 5, order_item_id, '2024-12-01', 'SOLD', 'Da ban cho khach hang', '2024-12-15 10:00:00' FROM ORDER_ITEM WHERE order_id = 5 AND product_id = 5 LIMIT 1;
INSERT INTO PRODUCT_INSTANCE (serial_number, product_id, order_item_id, manufacturing_date, status, notes, created_at)
SELECT 'RPI4-8GB-2024120003', 6, order_item_id, '2024-12-01', 'SOLD', 'Da ban cho khach hang', '2024-12-15 10:00:00' FROM ORDER_ITEM WHERE order_id = 1 AND product_id = 6 LIMIT 1;
INSERT INTO PRODUCT_INSTANCE (serial_number, product_id, order_item_id, manufacturing_date, status, notes, created_at)
SELECT 'RPI4-8GB-2024120004', 6, order_item_id, '2024-12-01', 'SOLD', 'Da ban cho khach hang', '2024-12-15 10:00:00' FROM ORDER_ITEM WHERE order_id = 12 AND product_id = 6 LIMIT 1;
INSERT INTO PRODUCT_INSTANCE (serial_number, product_id, order_item_id, manufacturing_date, status, notes, created_at)
SELECT 'RPI-KIT-2024120002', 8, order_item_id, '2024-12-05', 'SOLD', 'Da ban', '2024-12-20 10:00:00' FROM ORDER_ITEM WHERE order_id = 4 AND product_id = 8 LIMIT 1;
INSERT INTO PRODUCT_INSTANCE (serial_number, product_id, order_item_id, manufacturing_date, status, notes, created_at)
SELECT 'RPI-KIT-2024120003', 8, order_item_id, '2024-12-05', 'SOLD', 'Da ban', '2024-12-20 10:00:00' FROM ORDER_ITEM WHERE order_id = 6 AND product_id = 8 LIMIT 1;

-- Thay thế BANK_TRANSFER, MOMO, ZALO_PAY bằng SEPAY (V6)
INSERT INTO PAYMENT (payment_id, order_id, payment_method, amount, status, payment_date, payment_reference, created_at) VALUES
(1, 1, 'SEPAY', 2010000, 'COMPLETED', '2025-01-05 15:00:00', 'STEM20250105001', '2025-01-05 14:30:00'),
(2, 2, 'COD', 550000, 'COMPLETED', '2025-01-08 10:00:00', NULL, '2025-01-06 10:15:00'),
(3, 3, 'SEPAY', 1580000, 'COMPLETED', '2025-01-07 16:30:00', 'STEM20250107003', '2025-01-07 16:20:00'),
(4, 4, 'SEPAY', 2130000, 'PENDING', NULL, 'STEM20250108004', '2025-01-08 11:00:00'),
(5, 5, 'SEPAY', 1334750, 'COMPLETED', '2025-01-09 10:00:00', 'STEM20250109005', '2025-01-09 09:45:00'),
(6, 6, 'SEPAY', 2070000, 'COMPLETED', '2025-01-10 15:45:00', 'STEM20250110006', '2025-01-10 15:30:00'),
(7, 7, 'COD', 710000, 'PENDING', NULL, NULL, '2025-01-11 13:20:00'),
(8, 8, 'SEPAY', 867000, 'PENDING', NULL, 'STEM20250112008', '2025-01-12 10:00:00'),
(9, 9, 'COD', 1480000, 'PENDING', NULL, NULL, '2025-01-13 14:45:00'),
(10, 10, 'SEPAY', 410000, 'COMPLETED', '2025-01-14 16:20:00', 'STEM20250114010', '2025-01-14 16:10:00'),
(11, 11, 'SEPAY', 837500, 'FAILED', NULL, 'STEM20250115011', '2025-01-15 11:30:00'),
(12, 12, 'SEPAY', 1880000, 'COMPLETED', '2025-01-16 09:30:00', 'STEM20250116012', '2025-01-16 09:20:00'),
(13, 13, 'SEPAY', 2990000, 'PENDING', NULL, 'STEM20250117013', '2025-01-17 15:00:00'),
(14, 14, 'COD', 210000, 'PENDING', NULL, NULL, '2025-01-18 10:30:00'),
(15, 15, 'SEPAY', 665000, 'COMPLETED', '2025-01-19 14:00:00', 'STEM20250119015', '2025-01-19 13:45:00');

INSERT INTO WARRANTY (serial_number, warranty_policy_id, start_date, end_date, is_active, activation_date, notes, created_at) VALUES
('RPI4-8GB-2024120003', 2, '2025-01-05', '2027-01-05', TRUE, '2025-01-05 15:30:00', 'Kich hoat bao hanh khi giao hang', '2025-01-05 15:30:00'),
('RPI4-8GB-2024120004', 1, '2025-01-16', '2026-01-16', TRUE, '2025-01-16 10:00:00', 'Bao hanh tieu chuan 12 thang', '2025-01-16 10:00:00'),
('RPI4-4GB-2024120003', 2, '2025-01-07', '2027-01-07', TRUE, '2025-01-07 17:00:00', 'Bao hanh mo rong 24 thang', '2025-01-07 17:00:00'),
('RPI4-4GB-2024120004', 2, '2025-01-09', '2027-01-09', TRUE, '2025-01-09 10:30:00', 'Extended warranty', '2025-01-09 10:30:00'),
('RPI-KIT-2024120002', 2, '2025-01-08', '2027-01-08', TRUE, '2025-01-08 11:30:00', 'Bao hanh toan bo kit', '2025-01-08 11:30:00'),
('RPI-KIT-2024120003', 2, '2025-01-10', '2027-01-10', TRUE, '2025-01-10 16:00:00', 'Kit warranty activated', '2025-01-10 16:00:00');

-- Sử dụng đúng Enum claim_status_enum (không nhầm với resolution)
INSERT INTO WARRANTY_CLAIM (warranty_id, user_id, claim_date, issue_description, status, resolution, resolved_date, created_at)
SELECT w.warranty_id, 6, '2025-01-20', 'Board khong khoi dong duoc...', 'APPROVED', 'Da kiem tra va xac nhan loi bo mach...', '2025-01-22', '2025-01-20 10:30:00' FROM WARRANTY w WHERE w.serial_number = 'RPI4-4GB-2024120003';
INSERT INTO WARRANTY_CLAIM (warranty_id, user_id, claim_date, issue_description, status, resolution, resolved_date, created_at)
SELECT w.warranty_id, 4, '2025-01-18', 'Cong HDMI khong xuat duoc hinh...', 'SUBMITTED', NULL, NULL, '2025-01-18 14:15:00' FROM WARRANTY w WHERE w.serial_number = 'RPI4-8GB-2024120003';
INSERT INTO WARRANTY_CLAIM (warranty_id, user_id, claim_date, issue_description, status, resolution, resolved_date, created_at)
SELECT w.warranty_id, 7, '2025-01-21', 'Module WiFi khong hoat dong...', 'APPROVED', 'Da test lai va xac nhan loi WiFi chip...', '2025-01-23', '2025-01-21 11:00:00' FROM WARRANTY w WHERE w.serial_number = 'RPI-KIT-2024120002';
INSERT INTO WARRANTY_CLAIM (warranty_id, user_id, claim_date, issue_description, status, resolution, resolved_date, created_at)
SELECT w.warranty_id, 15, '2025-01-19', 'Cong USB 3.0 khong nhan dien thiet bi...', 'REJECTED', 'Sau khi kiem tra, xac dinh loi do thiet bi...', '2025-01-20', '2025-01-19 15:30:00' FROM WARRANTY w WHERE w.serial_number = 'RPI4-8GB-2024120004';
INSERT INTO WARRANTY_CLAIM (warranty_id, user_id, claim_date, issue_description, status, resolution, resolved_date, created_at)
SELECT w.warranty_id, 8, '2025-01-25', 'Board bi nong bat thuong...', 'SUBMITTED', NULL, NULL, '2025-01-25 09:45:00' FROM WARRANTY w WHERE w.serial_number = 'RPI4-4GB-2024120004';

INSERT INTO SEPAY_CONFIG (bank_name, bank_code, account_number, account_name, api_key, is_active) VALUES 
('MB Bank', 'MB', '0123456789', 'CONG TY STEM SHOP', 'YOUR_SEPAY_API_KEY_HERE', TRUE);

-- ============================================
-- 7. RESET SEQUENCES
-- ============================================

SELECT setval('role_role_id_seq', (SELECT MAX(role_id) FROM ROLE));
SELECT setval('warranty_policy_policy_id_seq', (SELECT MAX(policy_id) FROM WARRANTY_POLICY));
SELECT setval('brand_brand_id_seq', (SELECT MAX(brand_id) FROM BRAND));
SELECT setval('category_category_id_seq', (SELECT MAX(category_id) FROM CATEGORY));
SELECT setval('"USER_user_id_seq"', (SELECT MAX(user_id) FROM "USER"));
SELECT setval('product_product_id_seq', (SELECT MAX(product_id) FROM PRODUCT));
SELECT setval('product_image_image_id_seq', (SELECT MAX(image_id) FROM PRODUCT_IMAGE));
SELECT setval('product_bundle_bundle_id_seq', (SELECT MAX(bundle_id) FROM PRODUCT_BUNDLE));
SELECT setval('review_review_id_seq', (SELECT MAX(review_id) FROM REVIEW));
SELECT setval('tutorial_tutorial_id_seq', (SELECT MAX(tutorial_id) FROM TUTORIAL));
SELECT setval('tutorial_component_id_seq', (SELECT MAX(id) FROM TUTORIAL_COMPONENT));
SELECT setval('cart_cart_id_seq', (SELECT MAX(cart_id) FROM CART));
SELECT setval('cart_item_cart_item_id_seq', (SELECT MAX(cart_item_id) FROM CART_ITEM));
SELECT setval('coupon_coupon_id_seq', (SELECT MAX(coupon_id) FROM COUPON));
SELECT setval('order_header_order_id_seq', (SELECT MAX(order_id) FROM ORDER_HEADER));
SELECT setval('order_item_order_item_id_seq', (SELECT MAX(order_item_id) FROM ORDER_ITEM));
SELECT setval('payment_payment_id_seq', (SELECT MAX(payment_id) FROM PAYMENT));
SELECT setval('warranty_warranty_id_seq', (SELECT MAX(warranty_id) FROM WARRANTY));
SELECT setval('warranty_claim_claim_id_seq', (SELECT MAX(claim_id) FROM WARRANTY_CLAIM));
SELECT setval('sepay_config_config_id_seq', (SELECT MAX(config_id) FROM SEPAY_CONFIG));

SELECT 'DATABASE INITIALIZATION & FULL SEED COMPLETED SUCCESSFULLY!' AS status;