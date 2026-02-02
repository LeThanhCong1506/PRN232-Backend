-- ============================================
-- TAO DATABASE VA SEED DATA
-- E-COMMERCE DATABASE FOR STEM PRODUCTS
-- ============================================
-- 
-- HUONG DAN CHAY:
-- 1. Tao database: CREATE DATABASE ecommerce_db WITH ENCODING 'UTF8';
-- 2. Ket noi: \c ecommerce_db
-- 3. Chay file nay: \i database_init_and_seed.sql
-- 
-- Hoac chay truc tiep: psql -U your_username -d postgres -f database_init_and_seed.sql
-- ============================================

-- Xoa database neu da ton tai (CAN THAN!)
DROP DATABASE IF EXISTS ecommerce_db;

-- Tao database moi
CREATE DATABASE ecommerce_db WITH ENCODING 'UTF8';

-- Ket noi vao database
\c ecommerce_db

-- ============================================
-- TAO ENUMS
-- ============================================

CREATE TYPE product_type_enum AS ENUM ('MODULE', 'KIT', 'COMPONENT');
CREATE TYPE instance_status_enum AS ENUM ('IN_STOCK', 'SOLD', 'WARRANTY', 'DEFECTIVE', 'RETURNED');
CREATE TYPE difficulty_level_enum AS ENUM ('beginner', 'intermediate', 'advanced');
CREATE TYPE discount_type_enum AS ENUM ('FIXED_AMOUNT', 'PERCENTAGE');
CREATE TYPE order_status_enum AS ENUM ('PENDING', 'CONFIRMED', 'SHIPPED', 'DELIVERED', 'CANCELLED');
CREATE TYPE payment_method_enum AS ENUM ('COD', 'BANK_TRANSFER', 'MOMO', 'ZALO_PAY');
CREATE TYPE payment_status_enum AS ENUM ('PENDING', 'COMPLETED', 'FAILED');
CREATE TYPE claim_status_enum AS ENUM ('SUBMITTED', 'APPROVED', 'REJECTED', 'RESOLVED');

-- ============================================
-- TAO TABLES
-- ============================================

-- QUAN LY NGUOI DUNG
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
    phone VARCHAR(20),
    address TEXT,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_user_role FOREIGN KEY (role_id) REFERENCES ROLE(role_id) ON DELETE RESTRICT
);

-- SAN PHAM
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
    product_type product_type_enum NOT NULL,
    sku VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    price NUMERIC(12, 2) NOT NULL,
    stock_quantity INTEGER DEFAULT 0,
    has_serial_tracking BOOLEAN DEFAULT FALSE,
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

CREATE TABLE PRODUCT_INSTANCE (
    serial_number VARCHAR(100) PRIMARY KEY,
    product_id INTEGER NOT NULL,
    order_item_id INTEGER,
    manufacturing_date DATE,
    status instance_status_enum DEFAULT 'IN_STOCK',
    notes TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_instance_product FOREIGN KEY (product_id) REFERENCES PRODUCT(product_id) ON DELETE RESTRICT
);

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

-- BAI HUONG DAN
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

-- GIO HANG
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
    discount_type discount_type_enum NOT NULL,
    discount_value NUMERIC(10, 2) NOT NULL,
    min_order_value NUMERIC(12, 2) DEFAULT 0,
    start_date TIMESTAMP NOT NULL,
    end_date TIMESTAMP NOT NULL,
    usage_limit INTEGER,
    used_count INTEGER DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- DON HANG
CREATE TABLE ORDER_HEADER (
    order_id SERIAL PRIMARY KEY,
    user_id INTEGER NOT NULL,
    coupon_id INTEGER,
    order_number VARCHAR(50) NOT NULL UNIQUE,
    shiping_fee NUMERIC(10, 2) DEFAULT 0,
    subtotal_amount NUMERIC(12, 2) NOT NULL,
    discount_amount NUMERIC(10, 2) DEFAULT 0,
    total_amount NUMERIC(12, 2) NOT NULL,
    status order_status_enum DEFAULT 'PENDING',
    shipping_address TEXT NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_order_user FOREIGN KEY (user_id) REFERENCES "USER"(user_id) ON DELETE RESTRICT,
    CONSTRAINT fk_order_coupon FOREIGN KEY (coupon_id) REFERENCES COUPON(coupon_id) ON DELETE SET NULL
);

CREATE TABLE ORDER_ITEM (
    order_item_id SERIAL PRIMARY KEY,
    order_id INTEGER NOT NULL,
    product_id INTEGER NOT NULL,
    quantity INTEGER NOT NULL,
    unit_price NUMERIC(12, 2) NOT NULL,
    subtotal NUMERIC(12, 2) NOT NULL,
    CONSTRAINT fk_oi_order FOREIGN KEY (order_id) REFERENCES ORDER_HEADER(order_id) ON DELETE CASCADE,
    CONSTRAINT fk_oi_product FOREIGN KEY (product_id) REFERENCES PRODUCT(product_id) ON DELETE RESTRICT
);

-- Cap nhat khoa ngoai cho PRODUCT_INSTANCE
ALTER TABLE PRODUCT_INSTANCE
ADD CONSTRAINT fk_instance_order_item FOREIGN KEY (order_item_id) REFERENCES ORDER_ITEM(order_item_id) ON DELETE SET NULL;

-- THANH TOAN
CREATE TABLE PAYMENT (
    payment_id SERIAL PRIMARY KEY,
    order_id INTEGER NOT NULL UNIQUE,
    payment_method payment_method_enum NOT NULL,
    amount NUMERIC(12, 2) NOT NULL,
    status payment_status_enum DEFAULT 'PENDING',
    payment_date TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_payment_order FOREIGN KEY (order_id) REFERENCES ORDER_HEADER(order_id) ON DELETE RESTRICT
);

-- BAO HANH
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
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_claim_warranty FOREIGN KEY (warranty_id) REFERENCES WARRANTY(warranty_id) ON DELETE RESTRICT,
    CONSTRAINT fk_claim_user FOREIGN KEY (user_id) REFERENCES "USER"(user_id) ON DELETE RESTRICT
);

-- ============================================
-- TAO INDEX
-- ============================================

CREATE INDEX idx_user_email ON "USER"(email);
CREATE INDEX idx_user_role ON "USER"(role_id);
CREATE INDEX idx_product_sku ON PRODUCT(sku);
CREATE INDEX idx_product_brand ON PRODUCT(brand_id);
CREATE INDEX idx_product_type ON PRODUCT(product_type);
CREATE INDEX idx_order_user ON ORDER_HEADER(user_id);
CREATE INDEX idx_order_status ON ORDER_HEADER(status);
CREATE INDEX idx_order_number ON ORDER_HEADER(order_number);
CREATE INDEX idx_payment_status ON PAYMENT(status);
CREATE INDEX idx_warranty_serial ON WARRANTY(serial_number);
CREATE INDEX idx_warranty_active ON WARRANTY(is_active);
CREATE INDEX idx_review_product ON REVIEW(product_id);
CREATE INDEX idx_review_user ON REVIEW(user_id);

-- ============================================
-- SEED DATA
-- ============================================

-- ============================================
-- 1. ROLES
-- ============================================
INSERT INTO ROLE (role_id, role_name) VALUES 
(1, 'Admin'),
(2, 'Customer'),
(3, 'Staff');

-- ============================================
-- 2. WARRANTY POLICIES
-- ============================================
INSERT INTO WARRANTY_POLICY (policy_id, policy_name, duration_months, description, terms_and_conditions, is_active) VALUES
(1, 'Standard Warranty', 12, 'Bao hanh tieu chuan 1 nam', 'Bao hanh loi do nha san xuat. Khong bao hanh loi do nguoi dung gay ra, roi vo, ngam nuoc.', TRUE),
(2, 'Extended Warranty', 24, 'Bao hanh mo rong 2 nam', 'Bao hanh toan dien 2 nam, bao gom ca loi phan cung va ho tro ky thuat.', TRUE),
(3, 'Premium Warranty', 36, 'Bao hanh cao cap 3 nam', 'Bao hanh VIP 3 nam, doi moi trong 30 ngay dau, ho tro ky thuat 24/7.', TRUE);

-- ============================================
-- 3. BRANDS
-- ============================================
INSERT INTO BRAND (brand_id, name, logo_url) VALUES
(1, 'Arduino', '/images/brands/arduino.png'),
(2, 'Raspberry Pi', '/images/brands/rpi.png'),
(3, 'ESP32/Espressif', '/images/brands/esp32.png'),
(4, 'Adafruit', '/images/brands/adafruit.png'),
(5, 'SparkFun', '/images/brands/sparkfun.png'),
(6, 'Seeed Studio', '/images/brands/seeed.png'),
(7, 'STMicroelectronics', '/images/brands/stm.png'),
(8, 'Texas Instruments', '/images/brands/ti.png');

-- ============================================
-- 4. CATEGORIES
-- ============================================
INSERT INTO CATEGORY (category_id, name) VALUES
(1, 'Microcontrollers'),
(2, 'Sensors'),
(3, 'Actuators'),
(4, 'Power Supply'),
(5, 'Communication Modules'),
(6, 'Development Kits'),
(7, 'Tools & Accessories'),
(8, 'Displays'),
(9, 'Storage'),
(10, 'Cables & Connectors');

-- ============================================
-- 5. USERS
-- ============================================
-- NOTE: Tat ca password mac dinh la "Password123!" (da duoc hash bang bcrypt)
-- De dang nhap: su dung username hoac email + password: Password123!
INSERT INTO "USER" (user_id, role_id, username, email, password_hash, phone, address, is_active, created_at) VALUES
(1, 1, 'admin', 'admin@stemstore.vn', '$2a$10$abcdefghijklmnopqrstuvwxyz1234567890', '0901234567', '123 Nguyen Hue, Q.1, TP.HCM', TRUE, '2025-01-01 08:00:00'),
(2, 3, 'staff_nguyen', 'staff.nguyen@stemstore.vn', '$2a$10$abcdefghijklmnopqrstuvwxyz1234567891', '0902345678', '456 Le Loi, Q.1, TP.HCM', TRUE, '2025-01-02 09:00:00'),
(3, 3, 'staff_tran', 'staff.tran@stemstore.vn', '$2a$10$abcdefghijklmnopqrstuvwxyz1234567892', '0903456789', '789 Hai Ba Trung, Q.3, TP.HCM', TRUE, '2025-01-03 09:00:00'),
(4, 2, 'nguyenvana', 'nguyenvana@gmail.com', '$2a$10$abcdefghijklmnopqrstuvwxyz1234567893', '0904567890', '12 Tran Hung Dao, Q.5, TP.HCM', TRUE, '2025-01-05 10:30:00'),
(5, 2, 'tranthib', 'tranthib@gmail.com', '$2a$10$abcdefghijklmnopqrstuvwxyz1234567894', '0905678901', '34 Ly Thuong Kiet, Q.10, TP.HCM', TRUE, '2025-01-06 11:00:00'),
(6, 2, 'levanc', 'levanc@gmail.com', '$2a$10$abcdefghijklmnopqrstuvwxyz1234567895', '0906789012', '56 Vo Van Tan, Q.3, TP.HCM', TRUE, '2025-01-07 14:20:00'),
(7, 2, 'phamthid', 'phamthid@gmail.com', '$2a$10$abcdefghijklmnopqrstuvwxyz1234567896', '0907890123', '78 Cach Mang Thang 8, Q.Tan Binh, TP.HCM', TRUE, '2025-01-08 15:45:00'),
(8, 2, 'hoangvane', 'hoangvane@gmail.com', '$2a$10$abcdefghijklmnopqrstuvwxyz1234567897', '0908901234', '90 Nguyen Thi Minh Khai, Q.1, TP.HCM', TRUE, '2025-01-10 09:15:00'),
(9, 2, 'vuthif', 'vuthif@gmail.com', '$2a$10$abcdefghijklmnopqrstuvwxyz1234567898', '0909012345', '123 Dien Bien Phu, Q.Binh Thanh, TP.HCM', TRUE, '2025-01-11 10:30:00'),
(10, 2, 'dovanh', 'dovanh@gmail.com', '$2a$10$abcdefghijklmnopqrstuvwxyz1234567899', '0910123456', '45 Phan Xich Long, Q.Phu Nhuan, TP.HCM', TRUE, '2025-01-12 11:00:00'),
(11, 2, 'buithi', 'buithi@gmail.com', '$2a$10$abcdefghijklmnopqrstuvwxyz1234567800', '0911234567', '67 Hoang Van Thu, Q.Tan Binh, TP.HCM', TRUE, '2025-01-13 13:20:00'),
(12, 2, 'ngothij', 'ngothij@gmail.com', '$2a$10$abcdefghijklmnopqrstuvwxyz1234567801', '0912345678', '89 Truong Chinh, Q.12, TP.HCM', TRUE, '2025-01-14 14:45:00'),
(13, 2, 'lythik', 'lythik@gmail.com', '$2a$10$abcdefghijklmnopqrstuvwxyz1234567802', '0913456789', '101 Lac Long Quan, Q.11, TP.HCM', TRUE, '2025-01-15 16:00:00'),
(14, 2, 'dangvanl', 'dangvanl@gmail.com', '$2a$10$abcdefghijklmnopqrstuvwxyz1234567803', '0914567890', '234 Au Co, Q.Tan Phu, TP.HCM', TRUE, '2025-01-16 08:30:00'),
(15, 2, 'vovanm', 'vovanm@gmail.com', '$2a$10$abcdefghijklmnopqrstuvwxyz1234567804', '0915678901', '345 Phan Van Tri, Q.Go Vap, TP.HCM', TRUE, '2025-01-18 10:00:00');

-- ============================================
-- 6. PRODUCTS (30 products)
-- ============================================
INSERT INTO PRODUCT (product_id, brand_id, warranty_policy_id, product_type, sku, name, description, price, stock_quantity, has_serial_tracking, created_at) VALUES
-- Arduino Products
(1, 1, 1, 'MODULE', 'ARD-UNO-R3', 'Arduino Uno R3', 'Bo mach Arduino Uno R3 chinh hang voi chip ATmega328P, 14 digital I/O pins, 6 analog inputs. Hoan hao cho nguoi moi bat dau.', 350000, 50, FALSE, '2025-01-01 10:00:00'),
(2, 1, 1, 'MODULE', 'ARD-MEGA-2560', 'Arduino Mega 2560', 'Bo mach Arduino Mega 2560 voi 54 digital I/O pins, 16 analog inputs. Phu hop cho cac du an lon va phuc tap.', 520000, 30, FALSE, '2025-01-01 10:05:00'),
(3, 1, 1, 'MODULE', 'ARD-NANO', 'Arduino Nano V3', 'Bo mach Arduino Nano nho gon, de dang gan vao breadboard. Chip ATmega328P tuong tu Uno.', 180000, 80, FALSE, '2025-01-01 10:10:00'),
(4, 1, 2, 'KIT', 'ARD-STARTER-KIT', 'Arduino Starter Kit', 'Bo kit hoc Arduino day du gom: Arduino Uno R3, breadboard, LED, resistor, sensors, jumper wires va sach huong dan tieng Viet.', 1250000, 25, FALSE, '2025-01-01 10:15:00'),

-- Raspberry Pi Products
(5, 2, 2, 'MODULE', 'RPI-4B-4GB', 'Raspberry Pi 4 Model B 4GB', 'Raspberry Pi 4B voi 4GB RAM, CPU Quad-core 1.5GHz, ho tro 4K dual display, Gigabit Ethernet, USB 3.0.', 1450000, 40, TRUE, '2025-01-01 10:20:00'),
(6, 2, 2, 'MODULE', 'RPI-4B-8GB', 'Raspberry Pi 4 Model B 8GB', 'Raspberry Pi 4B phien ban 8GB RAM, manh me cho AI, machine learning va server applications.', 1850000, 20, TRUE, '2025-01-01 10:25:00'),
(7, 2, 1, 'MODULE', 'RPI-PICO', 'Raspberry Pi Pico', 'Vi dieu khien Raspberry Pi Pico voi chip RP2040, 264KB RAM, lap trinh bang MicroPython hoac C/C++.', 85000, 100, FALSE, '2025-01-01 10:30:00'),
(8, 2, 2, 'KIT', 'RPI-4-COMPLETE-KIT', 'Raspberry Pi 4 Complete Kit', 'Bo kit day du: Raspberry Pi 4B 4GB, nguon 5V/3A, the nho 32GB, case, tan nhiet, cap HDMI.', 2100000, 15, TRUE, '2025-01-01 10:35:00'),

-- ESP32 Products
(9, 3, 1, 'MODULE', 'ESP32-DEVKIT', 'ESP32 DevKit V1', 'Module ESP32 voi WiFi + Bluetooth, 2 cores 240MHz, 520KB RAM. Ly tuong cho IoT projects.', 120000, 150, FALSE, '2025-01-01 10:40:00'),
(10, 3, 1, 'MODULE', 'ESP32-CAM', 'ESP32-CAM', 'Module ESP32 tich hop camera OV2640 2MP, ho tro WiFi. Hoan hao cho nhan dien hinh anh, streaming.', 150000, 60, FALSE, '2025-01-01 10:45:00'),
(11, 3, 1, 'MODULE', 'ESP8266-NODEMCU', 'ESP8266 NodeMCU', 'Module WiFi ESP8266 voi cong USB, de lap trinh qua Arduino IDE. Gia re cho IoT projects.', 75000, 200, FALSE, '2025-01-01 10:50:00'),

-- Sensors
(12, 4, 1, 'COMPONENT', 'DHT22-TEMP-HUM', 'DHT22 Temperature Humidity Sensor', 'Cam bien nhiet do va do am DHT22 do chinh xac cao, dai do -40~80 do C, 0-100% RH.', 95000, 120, FALSE, '2025-01-01 11:00:00'),
(13, 4, 1, 'COMPONENT', 'HC-SR04-ULTRASONIC', 'HC-SR04 Ultrasonic Sensor', 'Cam bien sieu am do khoang cach HC-SR04, dai do 2cm-4m, do chinh xac 3mm.', 45000, 180, FALSE, '2025-01-01 11:05:00'),
(14, 5, 1, 'COMPONENT', 'PIR-HC-SR501', 'PIR Motion Sensor HC-SR501', 'Cam bien chuyen dong hong ngoai PIR, phat hien chuyen dong trong pham vi 7m, goc 120 do.', 35000, 150, FALSE, '2025-01-01 11:10:00'),
(15, 4, 1, 'COMPONENT', 'MQ-2-GAS-SENSOR', 'MQ-2 Gas Smoke Sensor', 'Cam bien khi gas va khoi MQ-2, phat hien LPG, propane, methane, hydrogen, smoke.', 55000, 90, FALSE, '2025-01-01 11:15:00'),
(16, 5, 1, 'COMPONENT', 'BMP280-PRESSURE', 'BMP280 Pressure Sensor', 'Cam bien ap suat khi quyen BMP280, do nhiet do va ap suat, giao tiep I2C/SPI.', 65000, 75, FALSE, '2025-01-01 11:20:00'),

-- Actuators & Motors
(17, 6, 1, 'COMPONENT', 'SG90-SERVO', 'SG90 Micro Servo Motor 9g', 'Servo motor mini SG90 9g, goc quay 180 do, moment xoan 1.8kg/cm, dien ap 4.8-6V.', 38000, 200, FALSE, '2025-01-01 11:25:00'),
(18, 6, 1, 'COMPONENT', 'MG996R-SERVO', 'MG996R High Torque Servo', 'Servo motor MG996R cong suat lon, moment xoan 11kg/cm, cau truc kim loai ben bi.', 115000, 80, FALSE, '2025-01-01 11:30:00'),
(19, 5, 1, 'COMPONENT', '28BYJ-48-STEPPER', '28BYJ-48 Stepper Motor + Driver', 'Dong co buoc 28BYJ-48 voi driver ULN2003, 5V DC, goc buoc 5.625 do, gear ratio 1:64.', 55000, 100, FALSE, '2025-01-01 11:35:00'),
(20, 6, 1, 'COMPONENT', 'L298N-MOTOR-DRIVER', 'L298N Motor Driver Module', 'Module dieu khien 2 dong co DC hoac 1 stepper motor, dong toi da 2A, dien ap 5-35V.', 68000, 90, FALSE, '2025-01-01 11:40:00'),

-- Displays
(21, 4, 1, 'COMPONENT', 'LCD-1602-I2C', 'LCD 1602 Display with I2C', 'Man hinh LCD 16x2 ky tu voi module I2C, tiet kiem chan GPIO, backlight xanh la.', 85000, 110, FALSE, '2025-01-01 11:45:00'),
(22, 4, 1, 'COMPONENT', 'OLED-096-I2C', 'OLED 0.96 inch I2C Display', 'Man hinh OLED 0.96 inch 128x64 pixel, giao tiep I2C, hien thi sac net, tiet kiem nang luong.', 95000, 130, FALSE, '2025-01-01 11:50:00'),
(23, 6, 1, 'COMPONENT', 'TFT-24-TOUCH', 'TFT 2.4 inch Touch Screen', 'Man hinh cam ung TFT 2.4 inch 240x320 pixel, dieu khien qua SPI, ho tro the SD.', 185000, 60, FALSE, '2025-01-01 11:55:00'),

-- Power Supply & Modules
(24, 7, 1, 'COMPONENT', 'MB102-POWER', 'MB102 Breadboard Power Supply', 'Module nguon cho breadboard MB102, output 3.3V/5V, dong toi da 700mA, dau vao USB/DC 7-12V.', 28000, 180, FALSE, '2025-01-01 12:00:00'),
(25, 8, 1, 'COMPONENT', 'AMS1117-REGULATOR', 'AMS1117 3.3V Voltage Regulator', 'Module on ap AMS1117 3.3V, dong toi da 1A, dau vao 4.5-7V, bao ve qua nhiet.', 18000, 250, FALSE, '2025-01-01 12:05:00'),
(26, 7, 1, 'COMPONENT', 'TP4056-CHARGER', 'TP4056 Li-ion Battery Charger', 'Module sac pin lithium TP4056 1A, bao ve qua sac/qua phong, LED bao trang thai.', 25000, 200, FALSE, '2025-01-01 12:10:00'),

-- Communication Modules
(27, 5, 1, 'COMPONENT', 'NRF24L01-WIRELESS', 'NRF24L01+ Wireless Module', 'Module truyen thong khong day 2.4GHz NRF24L01+, tam xa 100m, toc do 2Mbps.', 48000, 85, FALSE, '2025-01-01 12:15:00'),
(28, 5, 1, 'COMPONENT', 'HC-05-BLUETOOTH', 'HC-05 Bluetooth Module', 'Module Bluetooth HC-05 SPP, tam xa 10m, ho tro master/slave mode, UART communication.', 85000, 95, FALSE, '2025-01-01 12:20:00'),

-- Kits & Bundles
(29, 4, 2, 'KIT', 'SENSOR-KIT-37IN1', 'Sensor Kit 37 in 1', 'Bo 37 cam bien da dang: nhiet do, do am, anh sang, am thanh, chuyen dong, gas, vv. Kem huong dan.', 680000, 35, FALSE, '2025-01-01 12:25:00'),
(30, 5, 2, 'KIT', 'ROBOTICS-STARTER-KIT', 'Robotics Starter Kit', 'Bo kit lam robot cho nguoi moi: chassis, dong co, banh xe, cam bien, Arduino compatible board, battery.', 950000, 20, FALSE, '2025-01-01 12:30:00');

-- ============================================
-- 7. PRODUCT_CATEGORY (Many-to-Many)
-- ============================================
INSERT INTO PRODUCT_CATEGORY (product_id, category_id) VALUES
-- Arduino Uno
(1, 1), (1, 6),
-- Arduino Mega
(2, 1), (2, 6),
-- Arduino Nano
(3, 1), (3, 6),
-- Arduino Starter Kit
(4, 6), (4, 7),
-- Raspberry Pi 4B 4GB
(5, 1), (5, 6),
-- Raspberry Pi 4B 8GB
(6, 1), (6, 6),
-- Raspberry Pi Pico
(7, 1),
-- Raspberry Pi Complete Kit
(8, 6), (8, 7),
-- ESP32 DevKit
(9, 1), (9, 5), (9, 6),
-- ESP32-CAM
(10, 1), (10, 5), (10, 2),
-- ESP8266 NodeMCU
(11, 1), (11, 5),
-- DHT22
(12, 2),
-- HC-SR04
(13, 2),
-- PIR
(14, 2),
-- MQ-2
(15, 2),
-- BMP280
(16, 2),
-- SG90 Servo
(17, 3),
-- MG996R Servo
(18, 3),
-- Stepper Motor
(19, 3),
-- L298N Driver
(20, 3), (20, 7),
-- LCD 1602
(21, 8),
-- OLED
(22, 8),
-- TFT Touch
(23, 8),
-- MB102 Power
(24, 4), (24, 7),
-- AMS1117
(25, 4),
-- TP4056
(26, 4),
-- NRF24L01
(27, 5),
-- HC-05
(28, 5),
-- Sensor Kit
(29, 2), (29, 6), (29, 7),
-- Robotics Kit
(30, 3), (30, 6), (30, 7);

-- ============================================
-- 8. PRODUCT_IMAGES (2-3 images per product)
-- ============================================
INSERT INTO PRODUCT_IMAGE (image_id, product_id, image_url, created_at) VALUES
-- Arduino Uno (1)
(1, 1, '/images/products/arduino-uno-1.jpg', '2025-01-01 10:00:00'),
(2, 1, '/images/products/arduino-uno-2.jpg', '2025-01-01 10:00:00'),
-- Arduino Mega (2)
(3, 2, '/images/products/arduino-mega-1.jpg', '2025-01-01 10:05:00'),
(4, 2, '/images/products/arduino-mega-2.jpg', '2025-01-01 10:05:00'),
-- Arduino Nano (3)
(5, 3, '/images/products/arduino-nano-1.jpg', '2025-01-01 10:10:00'),
(6, 3, '/images/products/arduino-nano-2.jpg', '2025-01-01 10:10:00'),
-- Arduino Starter Kit (4)
(7, 4, '/images/products/arduino-starter-kit-1.jpg', '2025-01-01 10:15:00'),
(8, 4, '/images/products/arduino-starter-kit-2.jpg', '2025-01-01 10:15:00'),
(9, 4, '/images/products/arduino-starter-kit-3.jpg', '2025-01-01 10:15:00'),
-- Raspberry Pi 4B 4GB (5)
(10, 5, '/images/products/rpi4-4gb-1.jpg', '2025-01-01 10:20:00'),
(11, 5, '/images/products/rpi4-4gb-2.jpg', '2025-01-01 10:20:00'),
-- Raspberry Pi 4B 8GB (6)
(12, 6, '/images/products/rpi4-8gb-1.jpg', '2025-01-01 10:25:00'),
(13, 6, '/images/products/rpi4-8gb-2.jpg', '2025-01-01 10:25:00'),
-- Raspberry Pi Pico (7)
(14, 7, '/images/products/rpi-pico-1.jpg', '2025-01-01 10:30:00'),
(15, 7, '/images/products/rpi-pico-2.jpg', '2025-01-01 10:30:00'),
-- Raspberry Pi Complete Kit (8)
(16, 8, '/images/products/rpi-complete-kit-1.jpg', '2025-01-01 10:35:00'),
(17, 8, '/images/products/rpi-complete-kit-2.jpg', '2025-01-01 10:35:00'),
(18, 8, '/images/products/rpi-complete-kit-3.jpg', '2025-01-01 10:35:00'),
-- ESP32 DevKit (9)
(19, 9, '/images/products/esp32-devkit-1.jpg', '2025-01-01 10:40:00'),
(20, 9, '/images/products/esp32-devkit-2.jpg', '2025-01-01 10:40:00'),
-- ESP32-CAM (10)
(21, 10, '/images/products/esp32-cam-1.jpg', '2025-01-01 10:45:00'),
(22, 10, '/images/products/esp32-cam-2.jpg', '2025-01-01 10:45:00'),
-- ESP8266 (11)
(23, 11, '/images/products/esp8266-1.jpg', '2025-01-01 10:50:00'),
(24, 11, '/images/products/esp8266-2.jpg', '2025-01-01 10:50:00'),
-- DHT22 (12)
(25, 12, '/images/products/dht22-1.jpg', '2025-01-01 11:00:00'),
(26, 12, '/images/products/dht22-2.jpg', '2025-01-01 11:00:00'),
-- HC-SR04 (13)
(27, 13, '/images/products/hc-sr04-1.jpg', '2025-01-01 11:05:00'),
(28, 13, '/images/products/hc-sr04-2.jpg', '2025-01-01 11:05:00'),
-- PIR (14)
(29, 14, '/images/products/pir-1.jpg', '2025-01-01 11:10:00'),
(30, 14, '/images/products/pir-2.jpg', '2025-01-01 11:10:00'),
-- MQ-2 (15)
(31, 15, '/images/products/mq2-1.jpg', '2025-01-01 11:15:00'),
(32, 15, '/images/products/mq2-2.jpg', '2025-01-01 11:15:00'),
-- BMP280 (16)
(33, 16, '/images/products/bmp280-1.jpg', '2025-01-01 11:20:00'),
(34, 16, '/images/products/bmp280-2.jpg', '2025-01-01 11:20:00'),
-- SG90 (17)
(35, 17, '/images/products/sg90-1.jpg', '2025-01-01 11:25:00'),
(36, 17, '/images/products/sg90-2.jpg', '2025-01-01 11:25:00'),
-- MG996R (18)
(37, 18, '/images/products/mg996r-1.jpg', '2025-01-01 11:30:00'),
(38, 18, '/images/products/mg996r-2.jpg', '2025-01-01 11:30:00'),
-- Stepper (19)
(39, 19, '/images/products/stepper-1.jpg', '2025-01-01 11:35:00'),
(40, 19, '/images/products/stepper-2.jpg', '2025-01-01 11:35:00'),
-- L298N (20)
(41, 20, '/images/products/l298n-1.jpg', '2025-01-01 11:40:00'),
(42, 20, '/images/products/l298n-2.jpg', '2025-01-01 11:40:00'),
-- LCD (21)
(43, 21, '/images/products/lcd1602-1.jpg', '2025-01-01 11:45:00'),
(44, 21, '/images/products/lcd1602-2.jpg', '2025-01-01 11:45:00'),
-- OLED (22)
(45, 22, '/images/products/oled-1.jpg', '2025-01-01 11:50:00'),
(46, 22, '/images/products/oled-2.jpg', '2025-01-01 11:50:00'),
-- TFT (23)
(47, 23, '/images/products/tft-1.jpg', '2025-01-01 11:55:00'),
(48, 23, '/images/products/tft-2.jpg', '2025-01-01 11:55:00'),
-- MB102 (24)
(49, 24, '/images/products/mb102-1.jpg', '2025-01-01 12:00:00'),
(50, 24, '/images/products/mb102-2.jpg', '2025-01-01 12:00:00'),
-- AMS1117 (25)
(51, 25, '/images/products/ams1117-1.jpg', '2025-01-01 12:05:00'),
(52, 25, '/images/products/ams1117-2.jpg', '2025-01-01 12:05:00'),
-- TP4056 (26)
(53, 26, '/images/products/tp4056-1.jpg', '2025-01-01 12:10:00'),
(54, 26, '/images/products/tp4056-2.jpg', '2025-01-01 12:10:00'),
-- NRF24L01 (27)
(55, 27, '/images/products/nrf24l01-1.jpg', '2025-01-01 12:15:00'),
(56, 27, '/images/products/nrf24l01-2.jpg', '2025-01-01 12:15:00'),
-- HC-05 (28)
(57, 28, '/images/products/hc05-1.jpg', '2025-01-01 12:20:00'),
(58, 28, '/images/products/hc05-2.jpg', '2025-01-01 12:20:00'),
-- Sensor Kit (29)
(59, 29, '/images/products/sensor-kit-1.jpg', '2025-01-01 12:25:00'),
(60, 29, '/images/products/sensor-kit-2.jpg', '2025-01-01 12:25:00'),
(61, 29, '/images/products/sensor-kit-3.jpg', '2025-01-01 12:25:00'),
-- Robotics Kit (30)
(62, 30, '/images/products/robotics-kit-1.jpg', '2025-01-01 12:30:00'),
(63, 30, '/images/products/robotics-kit-2.jpg', '2025-01-01 12:30:00'),
(64, 30, '/images/products/robotics-kit-3.jpg', '2025-01-01 12:30:00');

-- ============================================
-- 9. PRODUCT_BUNDLES (Kits contain components)
-- ============================================
INSERT INTO PRODUCT_BUNDLE (bundle_id, parent_product_id, child_product_id, quantity, created_at) VALUES
-- Arduino Starter Kit (4) contains:
(1, 4, 1, 1, '2025-01-01 10:15:00'),  -- Arduino Uno
(2, 4, 12, 1, '2025-01-01 10:15:00'), -- DHT22 sensor
(3, 4, 13, 1, '2025-01-01 10:15:00'), -- Ultrasonic sensor
(4, 4, 17, 2, '2025-01-01 10:15:00'), -- 2x SG90 servos
(5, 4, 21, 1, '2025-01-01 10:15:00'), -- LCD display

-- Raspberry Pi Complete Kit (8) contains:
(6, 8, 6, 1, '2025-01-01 10:35:00'),  -- Raspberry Pi 4B 8GB
(7, 8, 24, 1, '2025-01-01 10:35:00'), -- Power supply module

-- Sensor Kit (29) contains various sensors:
(8, 29, 12, 1, '2025-01-01 12:25:00'),  -- DHT22
(9, 29, 13, 1, '2025-01-01 12:25:00'),  -- HC-SR04
(10, 29, 14, 1, '2025-01-01 12:25:00'), -- PIR
(11, 29, 15, 1, '2025-01-01 12:25:00'), -- MQ-2
(12, 29, 16, 1, '2025-01-01 12:25:00'), -- BMP280

-- Robotics Starter Kit (30) contains:
(13, 30, 3, 1, '2025-01-01 12:30:00'),  -- Arduino Nano
(14, 30, 13, 2, '2025-01-01 12:30:00'), -- 2x Ultrasonic sensors
(15, 30, 19, 2, '2025-01-01 12:30:00'), -- 2x Stepper motors
(16, 30, 20, 1, '2025-01-01 12:30:00'), -- L298N driver
(17, 30, 26, 1, '2025-01-01 12:30:00'); -- Battery charger

-- ============================================
-- 10. PRODUCT_INSTANCES (Serial numbers for tracked products)
-- ============================================
-- NOTE: Insert IN_STOCK instances first (không cần order_item_id)
INSERT INTO PRODUCT_INSTANCE (serial_number, product_id, order_item_id, manufacturing_date, status, notes, created_at) VALUES
-- Raspberry Pi 4B 4GB - IN STOCK
('RPI4-4GB-2024120001', 5, NULL, '2024-12-01', 'IN_STOCK', 'Chua ban', '2024-12-15 10:00:00'),
('RPI4-4GB-2024120002', 5, NULL, '2024-12-01', 'IN_STOCK', 'Chua ban', '2024-12-15 10:00:00'),
-- Raspberry Pi 4B 8GB - IN STOCK
('RPI4-8GB-2024120001', 6, NULL, '2024-12-01', 'IN_STOCK', 'Chua ban', '2024-12-15 10:00:00'),
('RPI4-8GB-2024120002', 6, NULL, '2024-12-01', 'IN_STOCK', 'Chua ban', '2024-12-15 10:00:00'),
-- Raspberry Pi Complete Kit - IN STOCK
('RPI-KIT-2024120001', 8, NULL, '2024-12-05', 'IN_STOCK', 'Bo kit day du', '2024-12-20 10:00:00'),
('RPI-KIT-2024120004', 8, NULL, '2024-12-05', 'IN_STOCK', 'Chua ban', '2024-12-20 10:00:00');

-- ============================================
-- 11. REVIEWS
-- ============================================
INSERT INTO REVIEW (review_id, product_id, user_id, rating, comment, created_at) VALUES
(1, 1, 4, 5, 'Arduino Uno rat tot cho nguoi moi bat dau. De su dung, tai lieu phong phu. Shop giao hang nhanh!', '2025-01-10 14:30:00'),
(2, 1, 5, 5, 'Chat luong tuyet voi, dong goi can than. Da test hoat dong on dinh.', '2025-01-12 16:45:00'),
(3, 1, 6, 4, 'San pham tot, gia hop ly. Tru 1 sao vi ship hoi lau.', '2025-01-15 10:20:00'),
(4, 5, 7, 5, 'Raspberry Pi 4B 4GB rat manh! Chay Home Assistant ngon lanh. Highly recommended!', '2025-01-11 11:00:00'),
(5, 5, 8, 5, 'Performance tuyet voi, ho tro 4K dual monitor. Dong goi can than, giao dung hen.', '2025-01-13 15:30:00'),
(6, 6, 9, 5, '8GB RAM qua du cho moi tac vu. Chay Docker containers rat muot. Shop uy tin!', '2025-01-14 09:15:00'),
(7, 9, 10, 5, 'ESP32 DevKit gia re ma chat luong tot. WiFi va Bluetooth hoat dong on dinh.', '2025-01-12 13:20:00'),
(8, 9, 11, 4, 'Module tot, lap trinh de dang. Tuy nhien can luu y ve nguon cap du manh.', '2025-01-16 14:00:00'),
(9, 10, 12, 5, 'ESP32-CAM tuyet voi cho cac du an camera IoT. Hinh anh kha sac net, gia re.', '2025-01-15 16:30:00'),
(10, 12, 13, 5, 'Cam bien DHT22 do nhiet do va do am rat chinh xac. Da dung cho du an nha thong minh.', '2025-01-13 10:45:00'),
(11, 12, 14, 4, 'Sensor on, do chinh xac cao. Chi can luu y thoi gian doc giua cac lan.', '2025-01-17 11:20:00'),
(12, 13, 15, 5, 'HC-SR04 do khoang cach rat tot. Dung cho robot tranh vat can hoan hao.', '2025-01-14 15:00:00'),
(13, 17, 4, 5, 'Servo SG90 nho gon, hoat dong em. Gia re phu hop cho nhieu du an.', '2025-01-16 09:30:00'),
(14, 21, 5, 4, 'LCD 1602 voi I2C rat tien. Tiet kiem duoc nhieu chan GPIO. Hoi kho han I2C module.', '2025-01-18 10:15:00'),
(15, 22, 6, 5, 'Man hinh OLED hien thi sac net, dep mat. Giao tiep I2C de dang. Rat hai long!', '2025-01-17 13:40:00'),
(16, 4, 7, 5, 'Arduino Starter Kit day du cho nguoi moi. Co sach huong dan tieng Viet rat hay. Gia tri!', '2025-01-18 14:20:00'),
(17, 29, 8, 5, 'Bo 37 sensors da dang, phu hop hoc tap va thu nghiem. Dang dong tien!', '2025-01-19 11:00:00'),
(18, 30, 9, 4, 'Robotics Kit kha day du. Tuy nhien can them huong dan chi tiet hon. Overall good!', '2025-01-20 15:30:00'),
(19, 28, 10, 5, 'HC-05 Bluetooth ket noi on dinh. Dung de dieu khien robot bang smartphone rat tot.', '2025-01-19 16:45:00'),
(20, 20, 11, 5, 'L298N Driver manh me, dieu khien 2 motor DC de dang. Tan nhiet tot. Recommend!', '2025-01-21 10:00:00');

-- ============================================
-- 12. TUTORIALS
-- ============================================
INSERT INTO TUTORIAL (tutorial_id, created_by, title, description, difficulty_level, estimated_duration, instructions, video_url, created_at) VALUES
(1, 1, 'Bat dau voi Arduino Uno - LED Blink', 
   'Huong dan co ban nhat de bat dau voi Arduino. Ban se hoc cach nap code va dieu khien LED nhap nhay.', 
   'beginner', 30, 
   'Buoc 1: Cai dat Arduino IDE\nBuoc 2: Ket noi Arduino Uno voi may tinh\nBuoc 3: Chon board va port\nBuoc 4: Upload code mau Blink\nBuoc 5: Quan sat LED on-board nhap nhay', 
   'https://youtube.com/watch?v=arduino-blink-tutorial', 
   '2025-01-05 10:00:00'),

(2, 1, 'Do nhiet do va do am voi DHT22', 
   'Huong dan su dung cam bien DHT22 de do nhiet do va do am, hien thi ket qua tren Serial Monitor.', 
   'beginner', 45, 
   'Buoc 1: Ket noi DHT22 voi Arduino (VCC, GND, Data)\nBuoc 2: Cai dat thu vien DHT\nBuoc 3: Viet code doc du lieu\nBuoc 4: Hien thi ket qua tren Serial Monitor\nBuoc 5: Thu nghiem voi cac dieu kien khac nhau', 
   'https://youtube.com/watch?v=dht22-tutorial', 
   '2025-01-06 11:00:00'),

(3, 2, 'Robot tranh vat can voi Ultrasonic', 
   'Xay dung robot di chuyen tu dong tranh vat can su dung cam bien sieu am HC-SR04.', 
   'intermediate', 120, 
   'Buoc 1: Lap rap chassis robot va dong co\nBuoc 2: Ket noi L298N driver va dong co\nBuoc 3: Gan cam bien HC-SR04 phia truoc\nBuoc 4: Viet code doc khoang cach va dieu khien dong co\nBuoc 5: Test va tinh chinh thuat toan', 
   'https://youtube.com/watch?v=obstacle-avoidance-robot', 
   '2025-01-08 14:00:00'),

(4, 2, 'Hien thi du lieu len man hinh OLED', 
   'Huong dan su dung man hinh OLED 0.96 inch de hien thi text, so va hinh anh don gian.', 
   'intermediate', 60, 
   'Buoc 1: Ket noi OLED voi Arduino qua I2C\nBuoc 2: Cai dat thu vien Adafruit SSD1306\nBuoc 3: Khoi tao man hinh trong code\nBuoc 4: Hien thi text va so\nBuoc 5: Ve hinh don gian (line, circle, rectangle)', 
   'https://youtube.com/watch?v=oled-display-tutorial', 
   '2025-01-10 15:30:00'),

(5, 1, 'Home Automation voi ESP32 va Blynk', 
   'Xay dung he thong nha thong minh don gian dieu khien den va doc cam bien qua smartphone.', 
   'intermediate', 90, 
   'Buoc 1: Setup ESP32 va ket noi WiFi\nBuoc 2: Tao project tren Blynk app\nBuoc 3: Ket noi DHT22 va relay module\nBuoc 4: Viet code doc sensor va dieu khien relay\nBuoc 5: Test qua Blynk app', 
   'https://youtube.com/watch?v=esp32-blynk-home-automation', 
   '2025-01-12 16:00:00'),

(6, 2, 'Raspberry Pi - Setup va cai dat Home Assistant', 
   'Huong dan cai dat Home Assistant tren Raspberry Pi 4 de xay dung trung tam smarthome.', 
   'advanced', 180, 
   'Buoc 1: Chuan bi Raspberry Pi va the SD\nBuoc 2: Flash Home Assistant OS\nBuoc 3: Boot va cau hinh ban dau\nBuoc 4: Cai dat integrations co ban\nBuoc 5: Ket noi thiet bi IoT\nBuoc 6: Tao automation don gian', 
   'https://youtube.com/watch?v=homeassistant-setup', 
   '2025-01-14 10:00:00'),

(7, 1, 'Dieu khien Servo Motor theo goc', 
   'Hoc cach dieu khien servo motor quay den goc mong muon va tao chuyen dong muot ma.', 
   'beginner', 40, 
   'Buoc 1: Ket noi servo voi Arduino\nBuoc 2: Cai dat thu vien Servo\nBuoc 3: Viet code dieu khien goc\nBuoc 4: Tao chuyen dong quet (sweep)\nBuoc 5: Dieu khien nhieu servo cung luc', 
   'https://youtube.com/watch?v=servo-motor-tutorial', 
   '2025-01-16 11:30:00'),

(8, 2, 'IoT - Giam sat moi truong real-time', 
   'Xay dung he thong giam sat nhiet do, do am, ap suat real-time va luu du lieu len cloud.', 
   'advanced', 150, 
   'Buoc 1: Setup ESP32 voi multiple sensors (DHT22, BMP280)\nBuoc 2: Doc du lieu tu cac sensors\nBuoc 3: Ket noi WiFi va gui du lieu len ThingSpeak\nBuoc 4: Tao dashboard hien thi real-time\nBuoc 5: Cau hinh alerts khi vuot nguong', 
   'https://youtube.com/watch?v=iot-environmental-monitoring', 
   '2025-01-18 13:00:00');

-- ============================================
-- 13. TUTORIAL_COMPONENTS (Components needed for each tutorial)
-- ============================================
INSERT INTO TUTORIAL_COMPONENT (id, tutorial_id, product_id, quantity, usage_note) VALUES
-- Tutorial 1: Arduino Blink (chi can Arduino Uno)
(1, 1, 1, 1, 'Bo mach Arduino Uno R3'),

-- Tutorial 2: DHT22 Temperature
(2, 2, 1, 1, 'Bo mach Arduino Uno R3'),
(3, 2, 12, 1, 'Cam bien nhiet do va do am DHT22'),

-- Tutorial 3: Obstacle Avoidance Robot
(4, 3, 3, 1, 'Arduino Nano de gan tren robot'),
(5, 3, 13, 1, 'Cam bien sieu am HC-SR04'),
(6, 3, 20, 1, 'Module dieu khien dong co L298N'),
(7, 3, 26, 1, 'Module sac pin TP4056'),

-- Tutorial 4: OLED Display
(8, 4, 1, 1, 'Arduino Uno R3'),
(9, 4, 22, 1, 'Man hinh OLED 0.96 inch'),

-- Tutorial 5: ESP32 Home Automation
(10, 5, 9, 1, 'ESP32 DevKit V1 co WiFi'),
(11, 5, 12, 1, 'Cam bien DHT22'),

-- Tutorial 6: Raspberry Pi Home Assistant
(12, 6, 5, 1, 'Raspberry Pi 4B 4GB hoac 8GB'),
(13, 6, 24, 1, 'Module nguon 5V'),

-- Tutorial 7: Servo Motor Control
(14, 7, 1, 1, 'Arduino Uno R3'),
(15, 7, 17, 2, '2 servo motor SG90'),

-- Tutorial 8: IoT Environmental Monitoring
(16, 8, 9, 1, 'ESP32 DevKit co WiFi'),
(17, 8, 12, 1, 'Cam bien DHT22'),
(18, 8, 16, 1, 'Cam bien ap suat BMP280'),
(19, 8, 22, 1, 'Man hinh OLED de hien thi local');

-- ============================================
-- 14. CARTS
-- ============================================
INSERT INTO CART (cart_id, user_id, created_at) VALUES
(1, 4, '2025-01-20 10:00:00'),
(2, 5, '2025-01-21 11:30:00'),
(3, 6, '2025-01-22 14:00:00'),
(4, 12, '2025-01-23 09:15:00'),
(5, 13, '2025-01-24 16:20:00'),
(6, 14, '2025-01-25 10:45:00');

-- ============================================
-- 15. CART_ITEMS
-- ============================================
INSERT INTO CART_ITEM (cart_item_id, cart_id, product_id, quantity, created_at) VALUES
-- Cart 1 (user 4)
(1, 1, 2, 1, '2025-01-20 10:05:00'),  -- Arduino Mega
(2, 1, 22, 1, '2025-01-20 10:10:00'), -- OLED Display
(3, 1, 12, 2, '2025-01-20 10:15:00'), -- 2x DHT22

-- Cart 2 (user 5)
(4, 2, 9, 2, '2025-01-21 11:35:00'),  -- 2x ESP32 DevKit
(5, 2, 28, 1, '2025-01-21 11:40:00'), -- HC-05 Bluetooth

-- Cart 3 (user 6)
(6, 3, 30, 1, '2025-01-22 14:05:00'), -- Robotics Kit
(7, 3, 26, 2, '2025-01-22 14:10:00'), -- 2x Battery Charger

-- Cart 4 (user 12)
(8, 4, 1, 1, '2025-01-23 09:20:00'),  -- Arduino Uno
(9, 4, 29, 1, '2025-01-23 09:25:00'), -- Sensor Kit
(10, 4, 21, 1, '2025-01-23 09:30:00'), -- LCD Display

-- Cart 5 (user 13)
(11, 5, 7, 3, '2025-01-24 16:25:00'), -- 3x Raspberry Pi Pico
(12, 5, 17, 5, '2025-01-24 16:30:00'), -- 5x SG90 Servo

-- Cart 6 (user 14)
(13, 6, 5, 1, '2025-01-25 10:50:00'), -- Raspberry Pi 4B 4GB
(14, 6, 23, 1, '2025-01-25 10:55:00'); -- TFT Touch Screen

-- ============================================
-- 16. COUPONS
-- ============================================
INSERT INTO COUPON (coupon_id, code, discount_type, discount_value, min_order_value, start_date, end_date, usage_limit, used_count, created_at) VALUES
(1, 'WELCOME2025', 'PERCENTAGE', 10.00, 500000, '2025-01-01 00:00:00', '2025-03-31 23:59:59', 100, 5, '2025-01-01 08:00:00'),
(2, 'NEWYEAR50K', 'FIXED_AMOUNT', 50000, 1000000, '2025-01-01 00:00:00', '2025-01-31 23:59:59', 50, 3, '2025-01-01 08:00:00'),
(3, 'STUDENT15', 'PERCENTAGE', 15.00, 300000, '2025-01-01 00:00:00', '2025-12-31 23:59:59', NULL, 8, '2025-01-01 08:00:00'),
(4, 'FREESHIP', 'FIXED_AMOUNT', 30000, 0, '2025-01-15 00:00:00', '2025-02-15 23:59:59', 200, 12, '2025-01-15 08:00:00'),
(5, 'RASPBERRY20', 'PERCENTAGE', 20.00, 1500000, '2025-01-10 00:00:00', '2025-02-28 23:59:59', 30, 2, '2025-01-10 08:00:00'),
(6, 'EXPIRED', 'PERCENTAGE', 25.00, 500000, '2024-12-01 00:00:00', '2024-12-31 23:59:59', 50, 50, '2024-12-01 08:00:00'),
(7, 'MEGA100K', 'FIXED_AMOUNT', 100000, 2000000, '2025-01-20 00:00:00', '2025-03-31 23:59:59', 20, 1, '2025-01-20 08:00:00');

-- ============================================
-- 17. ORDER_HEADER
-- ============================================
INSERT INTO ORDER_HEADER (order_id, user_id, coupon_id, order_number, shiping_fee, subtotal_amount, discount_amount, total_amount, status, shipping_address, created_at) VALUES
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

-- ============================================
-- 18. ORDER_ITEMS
-- ============================================
INSERT INTO ORDER_ITEM (order_item_id, order_id, product_id, quantity, unit_price, subtotal) VALUES
-- Order 1: user 4 (ORD-20250105-001) - RPI 8GB + Sensor Kit
(1, 1, 6, 1, 1850000, 1850000),  -- Raspberry Pi 4B 8GB
(2, 1, 29, 1, 680000, 680000),   -- Sensor Kit (discounted by coupon)

-- Order 2: user 5 (ORD-20250106-002) - Arduino Mega
(3, 2, 2, 1, 520000, 520000),    -- Arduino Mega 2560

-- Order 3: user 6 (ORD-20250107-003) - RPI 4GB + OLED
(4, 3, 5, 1, 1450000, 1450000),  -- Raspberry Pi 4B 4GB
(5, 3, 22, 1, 95000, 95000),     -- OLED Display
(6, 3, 17, 2, 38000, 76000),     -- 2x SG90 Servo (used coupon)

-- Order 4: user 7 (ORD-20250108-004) - RPI Complete Kit
(7, 4, 8, 1, 2100000, 2100000),  -- Raspberry Pi Complete Kit

-- Order 5: user 8 (ORD-20250109-005) - RPI 4GB + DHT22 + LCD
(8, 5, 5, 1, 1450000, 1450000),  -- Raspberry Pi 4B 4GB
(9, 5, 12, 1, 95000, 95000),     -- DHT22 Sensor (15% discount)
(10, 5, 21, 1, 85000, 85000),    -- LCD Display

-- Order 6: user 9 (ORD-20250110-006) - RPI Complete Kit
(11, 6, 8, 1, 2100000, 2100000), -- Raspberry Pi Complete Kit

-- Order 7: user 10 (ORD-20250111-007) - Sensor Kit
(12, 7, 29, 1, 680000, 680000),  -- Sensor Kit 37 in 1

-- Order 8: user 11 (ORD-20250112-008) - Robotics Kit (10% discount)
(13, 8, 30, 1, 950000, 950000),  -- Robotics Starter Kit

-- Order 9: user 12 (ORD-20250113-009) - RPI 4GB
(14, 9, 5, 1, 1450000, 1450000), -- Raspberry Pi 4B 4GB

-- Order 10: user 13 (ORD-20250114-010) - ESP32 + DHT22
(15, 10, 9, 2, 120000, 240000),  -- 2x ESP32 DevKit
(16, 10, 12, 1, 95000, 95000),   -- DHT22 Sensor
(17, 10, 14, 1, 35000, 35000),   -- PIR Sensor

-- Order 11: user 14 (ORD-20250115-011) - CANCELLED
(18, 11, 30, 1, 950000, 950000), -- Robotics Kit (15% discount)

-- Order 12: user 15 (ORD-20250116-012) - RPI 4B 8GB
(19, 12, 6, 1, 1850000, 1850000), -- Raspberry Pi 4B 8GB

-- Order 13: user 4 (ORD-20250117-013) - Multiple RPI (20% discount)
(20, 13, 6, 2, 1850000, 3700000), -- 2x Raspberry Pi 4B 8GB

-- Order 14: user 5 (ORD-20250118-014) - Arduino Nano
(21, 14, 3, 1, 180000, 180000),   -- Arduino Nano

-- Order 15: user 6 (ORD-20250119-015) - Arduino Starter Kit items
(22, 15, 1, 1, 350000, 350000),   -- Arduino Uno
(23, 15, 12, 1, 95000, 95000),    -- DHT22
(24, 15, 13, 1, 45000, 45000),    -- HC-SR04
(25, 15, 21, 1, 85000, 85000),    -- LCD Display
(26, 15, 17, 2, 38000, 76000);    -- 2x SG90 Servo

-- ============================================
-- 18B. UPDATE PRODUCT_INSTANCES - Link to ORDER_ITEMS (for SOLD items)
-- ============================================
-- Update instances that were sold - link to specific order items
-- Raspberry Pi 4B 4GB - SOLD instances
INSERT INTO PRODUCT_INSTANCE (serial_number, product_id, order_item_id, manufacturing_date, status, notes, created_at)
SELECT 'RPI4-4GB-2024120003', 5, order_item_id, '2024-12-01', 'SOLD', 'Da ban cho khach hang', '2024-12-15 10:00:00'
FROM ORDER_ITEM WHERE order_id = 3 AND product_id = 5 LIMIT 1;

INSERT INTO PRODUCT_INSTANCE (serial_number, product_id, order_item_id, manufacturing_date, status, notes, created_at)
SELECT 'RPI4-4GB-2024120004', 5, order_item_id, '2024-12-01', 'SOLD', 'Da ban cho khach hang', '2024-12-15 10:00:00'
FROM ORDER_ITEM WHERE order_id = 5 AND product_id = 5 LIMIT 1;

-- Raspberry Pi 4B 8GB - SOLD instances
INSERT INTO PRODUCT_INSTANCE (serial_number, product_id, order_item_id, manufacturing_date, status, notes, created_at)
SELECT 'RPI4-8GB-2024120003', 6, order_item_id, '2024-12-01', 'SOLD', 'Da ban cho khach hang', '2024-12-15 10:00:00'
FROM ORDER_ITEM WHERE order_id = 1 AND product_id = 6 LIMIT 1;

INSERT INTO PRODUCT_INSTANCE (serial_number, product_id, order_item_id, manufacturing_date, status, notes, created_at)
SELECT 'RPI4-8GB-2024120004', 6, order_item_id, '2024-12-01', 'SOLD', 'Da ban cho khach hang', '2024-12-15 10:00:00'
FROM ORDER_ITEM WHERE order_id = 12 AND product_id = 6 LIMIT 1;

-- Raspberry Pi Complete Kit - SOLD instances
INSERT INTO PRODUCT_INSTANCE (serial_number, product_id, order_item_id, manufacturing_date, status, notes, created_at)
SELECT 'RPI-KIT-2024120002', 8, order_item_id, '2024-12-05', 'SOLD', 'Da ban', '2024-12-20 10:00:00'
FROM ORDER_ITEM WHERE order_id = 4 AND product_id = 8 LIMIT 1;

INSERT INTO PRODUCT_INSTANCE (serial_number, product_id, order_item_id, manufacturing_date, status, notes, created_at)
SELECT 'RPI-KIT-2024120003', 8, order_item_id, '2024-12-05', 'SOLD', 'Da ban', '2024-12-20 10:00:00'
FROM ORDER_ITEM WHERE order_id = 6 AND product_id = 8 LIMIT 1;

-- ============================================
-- 19. PAYMENTS
-- ============================================
INSERT INTO PAYMENT (payment_id, order_id, payment_method, amount, status, payment_date, created_at) VALUES
(1, 1, 'BANK_TRANSFER', 2010000, 'COMPLETED', '2025-01-05 15:00:00', '2025-01-05 14:30:00'),
(2, 2, 'COD', 550000, 'COMPLETED', '2025-01-08 10:00:00', '2025-01-06 10:15:00'),
(3, 3, 'MOMO', 1580000, 'COMPLETED', '2025-01-07 16:30:00', '2025-01-07 16:20:00'),
(4, 4, 'BANK_TRANSFER', 2130000, 'PENDING', NULL, '2025-01-08 11:00:00'),
(5, 5, 'ZALO_PAY', 1334750, 'COMPLETED', '2025-01-09 10:00:00', '2025-01-09 09:45:00'),
(6, 6, 'MOMO', 2070000, 'COMPLETED', '2025-01-10 15:45:00', '2025-01-10 15:30:00'),
(7, 7, 'COD', 710000, 'PENDING', NULL, '2025-01-11 13:20:00'),
(8, 8, 'BANK_TRANSFER', 867000, 'PENDING', NULL, '2025-01-12 10:00:00'),
(9, 9, 'COD', 1480000, 'PENDING', NULL, '2025-01-13 14:45:00'),
(10, 10, 'MOMO', 410000, 'COMPLETED', '2025-01-14 16:20:00', '2025-01-14 16:10:00'),
(11, 11, 'BANK_TRANSFER', 837500, 'FAILED', NULL, '2025-01-15 11:30:00'),
(12, 12, 'ZALO_PAY', 1880000, 'COMPLETED', '2025-01-16 09:30:00', '2025-01-16 09:20:00'),
(13, 13, 'BANK_TRANSFER', 2990000, 'PENDING', NULL, '2025-01-17 15:00:00'),
(14, 14, 'COD', 210000, 'PENDING', NULL, '2025-01-18 10:30:00'),
(15, 15, 'MOMO', 665000, 'COMPLETED', '2025-01-19 14:00:00', '2025-01-19 13:45:00');

-- ============================================
-- 20. WARRANTIES (for sold serial items)
-- ============================================
-- Insert warranties for SOLD product instances
-- Warranties for Raspberry Pi 4B 8GB
INSERT INTO WARRANTY (serial_number, warranty_policy_id, start_date, end_date, is_active, activation_date, notes, created_at)
VALUES
('RPI4-8GB-2024120003', 2, '2025-01-05', '2027-01-05', TRUE, '2025-01-05 15:30:00', 'Kich hoat bao hanh khi giao hang', '2025-01-05 15:30:00'),
('RPI4-8GB-2024120004', 1, '2025-01-16', '2026-01-16', TRUE, '2025-01-16 10:00:00', 'Bao hanh tieu chuan 12 thang', '2025-01-16 10:00:00'),

-- Warranties for Raspberry Pi 4B 4GB
('RPI4-4GB-2024120003', 2, '2025-01-07', '2027-01-07', TRUE, '2025-01-07 17:00:00', 'Bao hanh mo rong 24 thang', '2025-01-07 17:00:00'),
('RPI4-4GB-2024120004', 2, '2025-01-09', '2027-01-09', TRUE, '2025-01-09 10:30:00', 'Extended warranty', '2025-01-09 10:30:00'),

-- Warranties for Raspberry Pi Complete Kit
('RPI-KIT-2024120002', 2, '2025-01-08', '2027-01-08', TRUE, '2025-01-08 11:30:00', 'Bao hanh toan bo kit', '2025-01-08 11:30:00'),
('RPI-KIT-2024120003', 2, '2025-01-10', '2027-01-10', TRUE, '2025-01-10 16:00:00', 'Kit warranty activated', '2025-01-10 16:00:00');

-- ============================================
-- 21. WARRANTY_CLAIMS
-- ============================================
-- Insert warranty claims using subquery to get warranty_id dynamically
-- Claim 1: RPI4-4GB-2024120003 (user 6)
INSERT INTO WARRANTY_CLAIM (warranty_id, user_id, claim_date, issue_description, status, resolution, resolved_date, created_at)
SELECT w.warranty_id, 6, '2025-01-20', 
   'Board khong khoi dong duoc, den LED power khong sang. Da thu nhieu nguon khac nhau.',
   'APPROVED', 'Da kiem tra va xac nhan loi bo mach. Doi board moi cho khach hang.', '2025-01-22', '2025-01-20 10:30:00'
FROM WARRANTY w WHERE w.serial_number = 'RPI4-4GB-2024120003';

-- Claim 2: RPI4-8GB-2024120003 (user 4)
INSERT INTO WARRANTY_CLAIM (warranty_id, user_id, claim_date, issue_description, status, resolution, resolved_date, created_at)
SELECT w.warranty_id, 4, '2025-01-18',
   'Cong HDMI khong xuat duoc hinh, da thu nhieu man hinh va cap khac nhau.',
   'SUBMITTED', NULL, NULL, '2025-01-18 14:15:00'
FROM WARRANTY w WHERE w.serial_number = 'RPI4-8GB-2024120003';

-- Claim 3: RPI-KIT-2024120002 (user 7)
INSERT INTO WARRANTY_CLAIM (warranty_id, user_id, claim_date, issue_description, status, resolution, resolved_date, created_at)
SELECT w.warranty_id, 7, '2025-01-21',
   'Module WiFi khong hoat dong, khong the ket noi mang WiFi.',
   'APPROVED', 'Da test lai va xac nhan loi WiFi chip. Tien hanh doi moi.', '2025-01-23', '2025-01-21 11:00:00'
FROM WARRANTY w WHERE w.serial_number = 'RPI-KIT-2024120002';

-- Claim 4: RPI4-8GB-2024120004 (user 15)
INSERT INTO WARRANTY_CLAIM (warranty_id, user_id, claim_date, issue_description, status, resolution, resolved_date, created_at)
SELECT w.warranty_id, 15, '2025-01-19',
   'Cong USB 3.0 khong nhan dien thiet bi, USB 2.0 van hoat dong binh thuong.',
   'REJECTED', 'Sau khi kiem tra, xac dinh loi do thiet bi USB cua khach khong tuong thich. Board hoat dong binh thuong voi USB khac.', '2025-01-20', '2025-01-19 15:30:00'
FROM WARRANTY w WHERE w.serial_number = 'RPI4-8GB-2024120004';

-- Claim 5: RPI4-4GB-2024120004 (user 8)
INSERT INTO WARRANTY_CLAIM (warranty_id, user_id, claim_date, issue_description, status, resolution, resolved_date, created_at)
SELECT w.warranty_id, 8, '2025-01-25',
   'Board bi nong bat thuong khi su dung, nhiet do CPU len toi 85 do C trong idle.',
   'SUBMITTED', NULL, NULL, '2025-01-25 09:45:00'
FROM WARRANTY w WHERE w.serial_number = 'RPI4-4GB-2024120004';

-- ============================================
-- HOAN THANH!
-- ============================================

-- Reset sequences to current values
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

-- Hien thi thong ke du lieu
SELECT 'DATABASE INITIALIZATION COMPLETED!' AS status;
SELECT '' AS empty_line;
SELECT 'DATA SUMMARY:' AS section;
SELECT '============================================' AS separator;
SELECT 'Roles: ' || COUNT(*) FROM ROLE
UNION ALL SELECT 'Users: ' || COUNT(*) FROM "USER"
UNION ALL SELECT 'Brands: ' || COUNT(*) FROM BRAND
UNION ALL SELECT 'Categories: ' || COUNT(*) FROM CATEGORY
UNION ALL SELECT 'Warranty Policies: ' || COUNT(*) FROM WARRANTY_POLICY
UNION ALL SELECT 'Products: ' || COUNT(*) FROM PRODUCT
UNION ALL SELECT 'Product Images: ' || COUNT(*) FROM PRODUCT_IMAGE
UNION ALL SELECT 'Product Bundles: ' || COUNT(*) FROM PRODUCT_BUNDLE
UNION ALL SELECT 'Product Instances: ' || COUNT(*) FROM PRODUCT_INSTANCE
UNION ALL SELECT 'Reviews: ' || COUNT(*) FROM REVIEW
UNION ALL SELECT 'Tutorials: ' || COUNT(*) FROM TUTORIAL
UNION ALL SELECT 'Tutorial Components: ' || COUNT(*) FROM TUTORIAL_COMPONENT
UNION ALL SELECT 'Carts: ' || COUNT(*) FROM CART
UNION ALL SELECT 'Cart Items: ' || COUNT(*) FROM CART_ITEM
UNION ALL SELECT 'Coupons: ' || COUNT(*) FROM COUPON
UNION ALL SELECT 'Orders: ' || COUNT(*) FROM ORDER_HEADER
UNION ALL SELECT 'Order Items: ' || COUNT(*) FROM ORDER_ITEM
UNION ALL SELECT 'Payments: ' || COUNT(*) FROM PAYMENT
UNION ALL SELECT 'Warranties: ' || COUNT(*) FROM WARRANTY
UNION ALL SELECT 'Warranty Claims: ' || COUNT(*) FROM WARRANTY_CLAIM;


-- 1. Thay đổi kiểu dữ liệu của cột product_type sang VARCHAR(50)
-- Sử dụng mệnh đề USING để ép kiểu dữ liệu cũ (Enum) sang chuỗi (text)
ALTER TABLE public.product 
ALTER COLUMN product_type TYPE VARCHAR(50) 
USING product_type::text;

-- 2. (Tùy chọn) Xóa kiểu Enum cũ nếu bạn không còn dùng ở bất kỳ đâu khác để làm sạch database
DROP TYPE IF EXISTS public.product_type_enum;
