-- ============================================================
-- STEM STORE — PRODUCT SEED DATA (20 sản phẩm thực tế)
-- Phiên bản: 2025 | Thị trường Việt Nam
-- ============================================================
-- HƯỚNG DẪN CHẠY:
--   1. Đã có schema (đã chạy pgAdmin dump)
--   2. Đã insert ROLE (1=Admin, 2=Customer, 3=Staff)
--   3. Chạy file này: \i product_seed.sql
-- ============================================================

-- ============================================================
-- BƯỚC 1: WARRANTY POLICY (3 chính sách bảo hành)
-- ============================================================
INSERT INTO public.warranty_policy (policy_id, policy_name, duration_months, description, terms_and_conditions, is_active) VALUES
(1, 'Không bảo hành',    0,
 'Áp dụng cho linh kiện cơ bản, phụ kiện rời. Không có chính sách đổi trả sau khi đã kiểm tra.',
 'Sản phẩm không được bảo hành. Vui lòng kiểm tra kỹ trước khi thanh toán. Không áp dụng đổi trả sau khi giao hàng thành công.',
 TRUE),
(2, 'Bảo hành 12 tháng', 12,
 'Bảo hành tiêu chuẩn 12 tháng kể từ ngày mua hàng, áp dụng cho lỗi do nhà sản xuất.',
 'Bảo hành lỗi do nhà sản xuất: chập mạch, hỏng linh kiện, lỗi firmware xuất xưởng. Không bảo hành: rơi vỡ, ngâm nước, chập điện do đấu dây sai, tháo dỡ không đúng cách. Yêu cầu giữ nguyên tem bảo hành.',
 TRUE),
(3, 'Bảo hành 24 tháng', 24,
 'Bảo hành mở rộng toàn diện 24 tháng dành cho sản phẩm cao cấp.',
 'Bảo hành toàn diện 24 tháng bao gồm lỗi phần cứng và hỗ trợ kỹ thuật. Đổi mới hoàn toàn trong 30 ngày đầu nếu có lỗi nhà sản xuất. Hỗ trợ kỹ thuật qua hotline và email trong suốt thời gian bảo hành.',
 TRUE)
ON CONFLICT (policy_id) DO NOTHING;

-- ============================================================
-- BƯỚC 2: BRANDS (8 thương hiệu)
-- ============================================================
INSERT INTO public.brand (brand_id, name, logo_url) VALUES
(1, 'Arduino',            'https://upload.wikimedia.org/wikipedia/commons/thumb/8/87/Arduino_Logo.svg/200px-Arduino_Logo.svg.png'),
(2, 'Raspberry Pi',       'https://upload.wikimedia.org/wikipedia/en/thumb/c/cb/Raspberry_Pi_Logo.svg/150px-Raspberry_Pi_Logo.svg.png'),
(3, 'Espressif Systems',  'https://www.espressif.com/sites/all/themes/espressif/logo-black.png'),
(4, 'ELEGOO',             NULL),
(5, 'Adafruit Industries',NULL),
(6, 'DFRobot',            NULL),
(7, 'Waveshare',          NULL),
(8, 'Bosch Sensortec',    NULL)
ON CONFLICT (brand_id) DO NOTHING;

-- ============================================================
-- BƯỚC 3: CATEGORIES (8 danh mục)
-- ============================================================
INSERT INTO public.category (category_id, name, image_url) VALUES
(1, 'Vi điều khiển',              '/images/categories/microcontroller.jpg'),
(2, 'Cảm biến',                   '/images/categories/sensor.jpg'),
(3, 'Module truyền thông',        '/images/categories/communication.jpg'),
(4, 'Màn hình & Hiển thị',       '/images/categories/display.jpg'),
(5, 'Động cơ & Servo',            '/images/categories/motor.jpg'),
(6, 'Driver điều khiển',         '/images/categories/driver.jpg'),
(7, 'Kit học tập',                '/images/categories/kit.jpg'),
(8, 'Bo mạch phát triển',         '/images/categories/devboard.jpg')
ON CONFLICT (category_id) DO NOTHING;

-- ============================================================
-- BƯỚC 4: PRODUCTS (20 sản phẩm STEM thực tế)
-- ============================================================
INSERT INTO public.product (
    product_id, brand_id, warranty_policy_id,
    product_type, sku, name, description,
    price, stock_quantity, has_serial_tracking,
    is_active, is_deleted, created_at
) VALUES

-- ===== VI ĐIỀU KHIỂN - MICROCONTROLLERS (1–3) =====
(1, 1, 2, 'MODULE', 'ARD-UNO-R3-001',
 'Arduino Uno R3 (ATmega328P)',
 'Arduino Uno R3 là bo mạch vi điều khiển nổi tiếng nhất thế giới, sử dụng chip ATmega328P 8-bit 16MHz. Trang bị Flash 32KB, SRAM 2KB, EEPROM 1KB, 14 chân digital I/O (6 chân PWM), 6 chân analog input 10-bit, cổng USB Type-B, jack nguồn DC 2.1mm (7–12V). Hoạt động ổn định ở 5V. Tương thích đầy đủ với hàng nghìn shield mở rộng và thư viện Arduino IDE. Lý tưởng cho người mới học lập trình nhúng, sinh viên kỹ thuật điện tử, và các dự án DIY cơ bản đến nâng cao.',
 165000.00, 85, FALSE, TRUE, FALSE, '2025-01-01 08:00:00'),

(2, 1, 2, 'MODULE', 'ARD-NANO-EVERY-001',
 'Arduino Nano Every (ATMega4809)',
 'Arduino Nano Every là phiên bản nâng cấp chính thức của Arduino Nano truyền thống, trang bị chip ATMega4809 với Flash 48KB (gấp 1.5 lần Nano cổ điển), SRAM 6KB, EEPROM 256B. Kích thước siêu nhỏ 45×18mm, cắm trực tiếp vào breadboard không cần dây. Có 14 chân digital I/O, 8 chân analog input. Nhận nguồn qua USB Micro-B hoặc chân VIN. Phù hợp cho các dự án cần form factor nhỏ gọn: thiết bị wearable, IoT sensor node, robot mini, badge điện tử.',
 195000.00, 62, FALSE, TRUE, FALSE, '2025-01-01 08:00:00'),

(3, 1, 2, 'MODULE', 'ARD-MEGA-2560-001',
 'Arduino Mega 2560 R3 (ATmega2560)',
 'Arduino Mega 2560 R3 là bo mạch mạnh nhất trong hệ sinh thái Arduino, dùng chip ATmega2560 với Flash 256KB, SRAM 8KB, EEPROM 4KB. Nổi bật với 54 chân digital I/O (15 chân PWM), 16 chân analog input 10-bit, 4 UART phần cứng, SPI, I2C. Hoàn toàn tương thích shield Arduino cỡ Uno. Phù hợp cho dự án phức tạp: máy in 3D (firmware Marlin), máy CNC, robot đa bậc tự do, hệ thống tự động hóa nhà xưởng, trạm thời tiết đa cảm biến.',
 285000.00, 40, FALSE, TRUE, FALSE, '2025-01-01 08:00:00'),

-- ===== BO MẠCH PHÁT TRIỂN - DEVELOPMENT BOARDS (4–8) =====
(4, 2, 3, 'MODULE', 'RPI-4B-4GB-001',
 'Raspberry Pi 4 Model B – RAM 4GB',
 'Raspberry Pi 4 Model B 4GB là máy tính nhúng mạnh mẽ với CPU ARM Cortex-A72 64-bit quad-core 1.8GHz và GPU VideoCore VI hỗ trợ OpenGL ES 3.1 / Vulkan 1.0. RAM 4GB LPDDR4-3200, hai cổng micro-HDMI xuất 4K@60fps song song, hai cổng USB 3.0, hai USB 2.0, Gigabit Ethernet thực, WiFi dual-band 2.4GHz/5GHz 802.11ac, Bluetooth 5.0. Khe microSD, 40-pin GPIO tương thích Pi 3. Chạy Raspberry Pi OS, Ubuntu 22.04, Kali Linux, HomeAssistant OS. Phù hợp cho desktop computing, NAS, media center 4K, AI edge computing, máy chủ Docker.',
 1850000.00, 15, TRUE, TRUE, FALSE, '2025-01-01 08:00:00'),

(5, 2, 3, 'MODULE', 'RPI-4B-8GB-001',
 'Raspberry Pi 4 Model B – RAM 8GB',
 'Raspberry Pi 4 Model B 8GB là phiên bản cao cấp nhất dòng Pi 4, trang bị 8GB LPDDR4-3200 RAM – gấp đôi bản 4GB – phù hợp cho tác vụ nặng. Cùng nền tảng phần cứng: CPU ARM Cortex-A72 quad-core 1.8GHz, GPU VideoCore VI, dual 4K HDMI, USB 3.0, Gigabit Ethernet, WiFi 5GHz, Bluetooth 5.0. Hỗ trợ chạy 64-bit OS đầy đủ. Lý tưởng cho: biên dịch code nặng, chạy Docker/Kubernetes, machine learning với TensorFlow Lite, lab server ảo hóa, desktop thay thế PC cơ bản.',
 2350000.00, 8, TRUE, TRUE, FALSE, '2025-01-01 08:00:00'),

(6, 3, 2, 'MODULE', 'ESP32-DEVKIT-V1-001',
 'ESP32 DevKit V1 – WiFi & Bluetooth (38 chân)',
 'ESP32 DevKit V1 sử dụng module ESP-WROOM-32 của Espressif, tích hợp WiFi 802.11 b/g/n 2.4GHz và Bluetooth 4.2 (BLE + Classic) trong một SoC. CPU Xtensa dual-core LX6 240MHz, SRAM 520KB, Flash SPI 4MB. 38 chân GPIO đa năng: 18 kênh ADC 12-bit, 2 kênh DAC 8-bit, 10 cảm ứng điện dung, I2C, SPI, UART, CAN, PWM trên mọi chân. Lập trình bằng Arduino IDE, ESP-IDF, MicroPython. Giá thành rất thấp cho tính năng WiFi+BT. Phù hợp mọi ứng dụng IoT, Smart Home, wearable, công nghiệp.',
 85000.00, 120, FALSE, TRUE, FALSE, '2025-01-01 08:00:00'),

(7, 3, 2, 'MODULE', 'ESP32-CAM-OV2640-001',
 'ESP32-CAM + Camera OV2640 2MP (WiFi)',
 'ESP32-CAM tích hợp module camera OV2640 2 Megapixel trực tiếp trên board ESP32, có thể stream video MJPEG qua WiFi không cần server bên ngoài. Hỗ trợ nhận diện khuôn mặt và theo dõi đối tượng bằng AI framework tích hợp. Flash LED 1W để chụp đêm. Khe microSD lên đến 4GB lưu trữ local. Kích thước nhỏ gọn 40×27mm. Lập trình qua cổng UART (cần USB-TTL converter). Phù hợp: camera an ninh WiFi DIY, smart doorbell, time-lapse, wildlife camera, baby monitor, object detection.',
 135000.00, 55, FALSE, TRUE, FALSE, '2025-01-01 08:00:00'),

(8, 3, 2, 'MODULE', 'ESP8266-NODEMCU-V3-001',
 'ESP8266 NodeMCU V3 – WiFi IoT Board (Lua)',
 'ESP8266 NodeMCU V3 là bo mạch IoT phổ biến nhất cho người mới bắt đầu với chip WiFi ESP8266EX tích hợp, giá cực kỳ phải chăng. CPU Tensilica L106 80/160MHz, Flash 4MB, SRAM 80KB. Có 11 chân GPIO, 1 ADC 10-bit (max 3.3V input), đầy đủ TCP/IP stack, HTTP, MQTT, WebSocket. Lập trình bằng Arduino IDE, MicroPython hoặc Lua. Cấp nguồn và flash qua USB Micro-B tiện lợi. Phù hợp cho mọi dự án IoT entry-level: đèn thông minh, nhiệt kế WiFi, điều khiển relay từ xa, data logger gửi lên ThingSpeak/Blynk.',
 65000.00, 98, FALSE, TRUE, FALSE, '2025-01-01 08:00:00'),

-- ===== CẢM BIẾN - SENSORS (9–14) =====
(9, 6, 1, 'COMPONENT', 'SEN-DHT22-AM2302-001',
 'Cảm biến nhiệt độ & độ ẩm DHT22 (AM2302)',
 'DHT22 (tên gọi khác AM2302) là cảm biến nhiệt độ và độ ẩm thế hệ mới, chính xác hơn DHT11. Dải đo nhiệt độ -40°C đến +80°C với sai số ±0.5°C; độ ẩm 0–100%RH với sai số ±2–5%RH. Giao tiếp 1-wire đơn giản qua 1 chân data, tần số lấy mẫu tối đa 0.5Hz (1 lần/2 giây). Điện áp 3.3–5.5V. Có thư viện hỗ trợ đầy đủ trên Arduino, ESP32, ESP8266, Raspberry Pi. Phù hợp cho: trạm thời tiết mini, nhà kính tự động hóa, hệ thống HVAC thông minh, data logger môi trường, phòng máy chủ.',
 38000.00, 150, FALSE, TRUE, FALSE, '2025-01-01 08:00:00'),

(10, 6, 1, 'COMPONENT', 'SEN-HCSR04-ULTRA-001',
 'Cảm biến siêu âm đo khoảng cách HC-SR04',
 'HC-SR04 đo khoảng cách không tiếp xúc bằng sóng siêu âm 40kHz. Dải đo từ 2cm đến 400cm, độ phân giải 0.3cm, sai số ±3mm. Điện áp 5V DC, tiêu thụ 15mA. Điều khiển qua 2 chân: TRIG (kích phát xung 10µs) và ECHO (nhận xung phản hồi). Góc phát hiện hẹp <15° tránh nhiễu. Phù hợp cho: robot tránh vật cản, đo mực nước bồn chứa, cảm biến lùi xe ô tô DIY, hệ thống báo động xâm nhập, thước đo khoảng cách điện tử, cổng tự động.',
 22000.00, 200, FALSE, TRUE, FALSE, '2025-01-01 08:00:00'),

(11, 5, 1, 'COMPONENT', 'SEN-MPU6050-GY521-001',
 'Module IMU 6 trục GY-521 MPU-6050',
 'GY-521 tích hợp MPU-6050 – IMU 6 trục từ InvenSense gồm gia tốc kế 3 trục (±2/4/8/16g) và con quay hồi chuyển 3 trục (±250/500/1000/2000°/s), độ phân giải 16-bit mỗi trục. Giao tiếp I2C tốc độ cao (địa chỉ 0x68 hoặc 0x69), điện áp 3.3–5V. Tích hợp DMP (Digital Motion Processor) tính toán quaternion và góc Euler nội bộ, giảm tải cho vi điều khiển. Phù hợp: quadcopter, xe cân bằng động hai bánh, cánh tay robot, game controller cử chỉ tay, theo dõi chuyển động thể thao.',
 42000.00, 130, FALSE, TRUE, FALSE, '2025-01-01 08:00:00'),

(12, 6, 1, 'COMPONENT', 'SEN-PIR-HCSR501-001',
 'Cảm biến chuyển động hồng ngoại PIR HC-SR501',
 'HC-SR501 phát hiện chuyển động của người và động vật qua sự thay đổi bức xạ hồng ngoại (PIR – Passive Infrared). Góc phát hiện 120°, tầm xa tối đa 7 mét (điều chỉnh được từ 1–7m qua biến trở SEN). Điện áp 4.5–20V, output TTL HIGH (3.3V) khi phát hiện. Biến trở TIME điều chỉnh thời gian giữ output 5 giây đến 5 phút. Chế độ H (Repeatable trigger) hoặc L (Single trigger). Phù hợp: đèn cảm ứng tự bật/tắt, camera an ninh kích hoạt tự động, báo động xâm nhập, tiết kiệm điện phòng họp.',
 28000.00, 180, FALSE, TRUE, FALSE, '2025-01-01 08:00:00'),

(13, 6, 1, 'COMPONENT', 'SEN-MQ2-GAS-001',
 'Cảm biến khí gas và khói MQ-2',
 'MQ-2 là cảm biến chất bán dẫn phát hiện các loại khí dễ cháy và khói: LPG, propane, methane (CH4), hydrogen (H2), CO, alcohol, và khói thuốc lá. Dải phát hiện 300–10000ppm, độ nhạy cao, thời gian phản hồi <10 giây. Yêu cầu preheat 24 giờ lần đầu để đạt độ ổn định. Điện áp 5V. Output kép: analog (nồng độ liên tục 0–5V) và digital (TTL, ngưỡng cảnh báo điều chỉnh qua biến trở). Phù hợp: báo rò rỉ gas bếp, báo cháy, monitoring chất lượng không khí, phòng thí nghiệm hóa học.',
 32000.00, 115, FALSE, TRUE, FALSE, '2025-01-01 08:00:00'),

(14, 8, 1, 'COMPONENT', 'SEN-BMP280-BARO-001',
 'Cảm biến áp suất khí quyển & nhiệt độ BMP280',
 'BMP280 của Bosch Sensortec là cảm biến áp suất tuyệt đối độ chính xác cao, đo áp suất 300–1100hPa (tương đương độ cao -500m đến 9000m so với mực nước biển) với sai số ±1hPa, và nhiệt độ -40°C đến +85°C với sai số ±1°C. Có thể tính độ cao tương đối với độ phân giải 10cm. Giao tiếp I2C (0x76/0x77) hoặc SPI 4 dây. Tiêu thụ điện siêu thấp 2.7µA ở chế độ 1Hz. Phù hợp: trạm thời tiết, đồng hồ thể thao leo núi, drone altitude hold, UAV, thiết bị hàng không.',
 38000.00, 95, FALSE, TRUE, FALSE, '2025-01-01 08:00:00'),

-- ===== MODULE TRUYỀN THÔNG - COMMUNICATION (15–16) =====
(15, 6, 1, 'MODULE', 'COM-HC05-BT-001',
 'Module Bluetooth HC-05 Master/Slave UART',
 'HC-05 là module Bluetooth Classic SPP (Serial Port Profile) v2.0+EDR hỗ trợ cả chế độ Master và Slave (không như HC-06 chỉ có Slave). Giao tiếp UART serial với MCU, baudrate mặc định 9600bps (cấu hình qua lệnh AT ở chế độ AT: EN = HIGH). Khoảng cách hoạt động ~10m trong nhà thông thường. Điện áp module 3.3V (logic 3.3V, cần voltage divider 5V→3.3V cho chân RX). Mặc định tên "HC-05", mã PIN "1234". Dễ dàng kết nối với smartphone Android. Phù hợp: điều khiển robot qua app, truyền dữ liệu cảm biến không dây, remote control, bàn phím BT DIY.',
 68000.00, 75, FALSE, TRUE, FALSE, '2025-01-01 08:00:00'),

(16, 6, 1, 'MODULE', 'COM-NRF24L01-PLUS-001',
 'Module RF NRF24L01+ 2.4GHz PA+LNA (Tầm xa 1km)',
 'NRF24L01+ với bộ khuếch đại công suất (PA) và bộ khuếch đại tạp âm thấp (LNA) nâng tầm phủ sóng lên đến 100m trong nhà và 1000m ngoài trời thông thoáng, vượt trội so với bản NRF24L01 cơ bản. Hoạt động tại băng tần ISM 2.4GHz, 125 kênh độc lập, tốc độ dữ liệu 250kbps/1Mbps/2Mbps. Giao tiếp SPI tốc độ cao, điện áp 3.3V (GPIO tương thích 5V). Hỗ trợ auto-ACK, auto-retransmit tự động, đa điểm (1 master – 6 slave). Tiêu thụ điện rất thấp (11.3mA TX, 12.3mA RX, 26µA standby). Phù hợp: mạng cảm biến nông nghiệp, điều khiển drone từ xa, thu thập dữ liệu phân tán, trò chơi multiplayer.',
 45000.00, 140, FALSE, TRUE, FALSE, '2025-01-01 08:00:00'),

-- ===== MÀN HÌNH - DISPLAYS (17–18) =====
(17, 7, 1, 'MODULE', 'DIS-LCD1602-I2C-001',
 'Màn hình LCD 1602 16x2 kèm module I2C PCF8574',
 'LCD 1602 màu xanh dương kèm sẵn module I2C PCF8574 hàn liền, giảm số chân kết nối từ 16 xuống còn 4 chân (VCC 5V, GND, SDA, SCL). Hiển thị 16 ký tự trên 2 hàng, đèn nền LED xanh dương, contrast điều chỉnh bằng biến trở 3296. Địa chỉ I2C mặc định 0x27 (thay đổi được qua jumper A0/A1/A2 để tránh xung đột khi dùng nhiều LCD). Hỗ trợ ký tự tùy chỉnh (CGRAM 8 ký tự). Thư viện LiquidCrystal_I2C phổ biến. Phù hợp: đồng hồ nhiệt độ/độ ẩm, máy đo điện áp, đồng hồ RTC, menu giao diện thiết bị nhúng.',
 48000.00, 85, FALSE, TRUE, FALSE, '2025-01-01 08:00:00'),

(18, 7, 1, 'MODULE', 'DIS-OLED096-I2C-001',
 'Màn hình OLED 0.96 inch I2C SSD1306 128x64px',
 'Màn hình OLED 0.96 inch SSD1306 màu trắng, độ phân giải 128×64 pixels. Công nghệ OLED tự phát sáng cho contrast cực cao (đen tuyệt đối – không có backlight), góc nhìn rộng 160°, hoạt động tốt cả trong ánh sáng mạnh ngoài trời. Giao tiếp I2C chỉ cần 2 dây (SDA + SCL), hỗ trợ địa chỉ 0x3C hoặc 0x3D, điện áp 3.3–5V. Tiêu thụ điện rất thấp ~10mA khi sáng, phù hợp thiết bị chạy pin. Thư viện Adafruit SSD1306 + GFX hỗ trợ text đa font, đồ thị, hình ảnh bitmap. Phù hợp: smartwatch DIY, wearable, oscilloscope nhỏ, game console, đồng hồ desktop.',
 52000.00, 90, FALSE, TRUE, FALSE, '2025-01-01 08:00:00'),

-- ===== ĐỘNG CƠ & DRIVER (19–20) =====
(19, 4, 1, 'COMPONENT', 'ACT-SG90-SERVO-001',
 'Servo Motor SG90 9g Mini Tower Pro (0°–180°)',
 'Tower Pro SG90 là servo micro được sử dụng rộng rãi nhất trong robotics giáo dục và mô hình RC. Trọng lượng siêu nhẹ chỉ 9g, moment xoắn 1.8 kg·cm ở 4.8V và 2.2 kg·cm ở 6V. Góc quay 0–180°, tốc độ 0.1s/60° ở 4.8V. Điều khiển bằng tín hiệu PWM tiêu chuẩn 50Hz (pulse width 1ms–2ms tương ứng 0°–180°). Đi kèm 3 cần servo (tròn, chữ X, cánh quạt) và 4 vít M2 bắt servo. Vỏ nhựa ABS bền, dây tín hiệu dài 25cm với đầu nối 3-pin. Phù hợp: robot cánh tay 4–6 DOF, lái hướng xe RC, gimbal 2-3 trục, cơ cấu kẹp/lật, robot STEM cho học sinh.',
 32000.00, 160, FALSE, TRUE, FALSE, '2025-01-01 08:00:00'),

(20, 4, 1, 'MODULE', 'DRV-L298N-DUAL-001',
 'Mạch điều khiển động cơ L298N Dual H-Bridge',
 'Module L298N là driver H-bridge kép phổ biến nhất trong robotics, điều khiển độc lập 2 động cơ DC (hoặc 1 động cơ bước bipolar). Điện áp motor 5–46V DC, dòng liên tục tối đa 2A/kênh (peak 3A), công suất tổng 25W. Điện áp logic 5V. Tích hợp 8 diode flyback bảo vệ cuộn dây và tản nhiệt nhôm phòng quá nhiệt. Điều khiển qua: IN1/IN2 (chiều quay motor A), IN3/IN4 (chiều quay motor B), ENA/ENB nhận tín hiệu PWM điều tốc. Có output 5V cấp nguồn cho MCU (khi không cần jumper). Phù hợp: robot 2/4 bánh, băng tải, xe điều khiển từ xa, cửa tự động, máy CNC trục đơn.',
 55000.00, 70, FALSE, TRUE, FALSE, '2025-01-01 08:00:00')

ON CONFLICT (product_id) DO NOTHING;

-- ============================================================
-- BƯỚC 5: PRODUCT_CATEGORY (mapping sản phẩm ↔ danh mục)
-- ============================================================
INSERT INTO public.product_category (product_id, category_id) VALUES
-- Vi điều khiển (Category 1)
(1, 1),   -- Arduino Uno R3
(2, 1),   -- Arduino Nano Every
(3, 1),   -- Arduino Mega 2560
-- Bo mạch phát triển (Category 8)
(4, 8),   -- Raspberry Pi 4B 4GB
(5, 8),   -- Raspberry Pi 4B 8GB
(6, 8),   -- ESP32 DevKit V1
(6, 1),   -- ESP32 cũng là vi điều khiển
(7, 8),   -- ESP32-CAM
(8, 8),   -- ESP8266 NodeMCU
(8, 1),   -- ESP8266 cũng là vi điều khiển
-- Cảm biến (Category 2)
(9,  2),  -- DHT22
(10, 2),  -- HC-SR04
(11, 2),  -- MPU-6050
(12, 2),  -- PIR HC-SR501
(13, 2),  -- MQ-2
(14, 2),  -- BMP280
-- Module truyền thông (Category 3)
(15, 3),  -- HC-05 Bluetooth
(16, 3),  -- NRF24L01+
-- Màn hình (Category 4)
(17, 4),  -- LCD 1602 I2C
(18, 4),  -- OLED 0.96"
-- Động cơ (Category 5)
(19, 5),  -- Servo SG90
-- Driver (Category 6)
(20, 6)   -- L298N Motor Driver
ON CONFLICT (product_id, category_id) DO NOTHING;

-- ============================================================
-- BƯỚC 6: PRODUCT_IMAGE (2 ảnh mỗi sản phẩm)
-- ============================================================
INSERT INTO public.product_image (image_id, product_id, image_url, is_primary, created_at) VALUES
-- Arduino Uno R3
(1,  1, 'https://store.arduino.cc/cdn/shop/files/A000066_03.front_643x483.jpg',       TRUE,  NOW()),
(2,  1, 'https://store.arduino.cc/cdn/shop/files/A000066_03.back_643x483.jpg',        FALSE, NOW()),
-- Arduino Nano Every
(3,  2, 'https://store.arduino.cc/cdn/shop/files/ABX00028_00.front_643x483.jpg',      TRUE,  NOW()),
(4,  2, 'https://store.arduino.cc/cdn/shop/files/ABX00028_00.back_643x483.jpg',       FALSE, NOW()),
-- Arduino Mega 2560
(5,  3, 'https://store.arduino.cc/cdn/shop/files/A000067_03.front_643x483.jpg',       TRUE,  NOW()),
(6,  3, 'https://store.arduino.cc/cdn/shop/files/A000067_03.back_643x483.jpg',        FALSE, NOW()),
-- Raspberry Pi 4B 4GB
(7,  4, 'https://www.raspberrypi.com/app/uploads/2022/02/HERO-RPI4-1.png',            TRUE,  NOW()),
(8,  4, 'https://www.raspberrypi.com/app/uploads/2022/02/HERO-RPI4-2.png',            FALSE, NOW()),
-- Raspberry Pi 4B 8GB
(9,  5, 'https://www.raspberrypi.com/app/uploads/2022/02/HERO-RPI4-1.png',            TRUE,  NOW()),
(10, 5, 'https://www.raspberrypi.com/app/uploads/2022/02/RPI4-BACK-1.png',            FALSE, NOW()),
-- ESP32 DevKit V1
(11, 6, 'https://www.espressif.com/sites/default/files/products/images/esp32-devkitc-v4-front_0.jpg', TRUE,  NOW()),
(12, 6, 'https://www.espressif.com/sites/default/files/products/images/esp32-devkitc-v4-back_0.jpg',  FALSE, NOW()),
-- ESP32-CAM
(13, 7, '/images/products/esp32-cam-front.jpg',  TRUE,  NOW()),
(14, 7, '/images/products/esp32-cam-side.jpg',   FALSE, NOW()),
-- ESP8266 NodeMCU V3
(15, 8, '/images/products/nodemcu-v3-front.jpg', TRUE,  NOW()),
(16, 8, '/images/products/nodemcu-v3-back.jpg',  FALSE, NOW()),
-- DHT22
(17, 9,  '/images/products/dht22-front.jpg',     TRUE,  NOW()),
(18, 9,  '/images/products/dht22-side.jpg',      FALSE, NOW()),
-- HC-SR04
(19, 10, '/images/products/hcsr04-front.jpg',    TRUE,  NOW()),
(20, 10, '/images/products/hcsr04-angle.jpg',    FALSE, NOW()),
-- MPU-6050 GY-521
(21, 11, '/images/products/mpu6050-front.jpg',   TRUE,  NOW()),
(22, 11, '/images/products/mpu6050-back.jpg',    FALSE, NOW()),
-- PIR HC-SR501
(23, 12, '/images/products/pir-hcsr501-front.jpg', TRUE,  NOW()),
(24, 12, '/images/products/pir-hcsr501-back.jpg',  FALSE, NOW()),
-- MQ-2
(25, 13, '/images/products/mq2-front.jpg',       TRUE,  NOW()),
(26, 13, '/images/products/mq2-angle.jpg',       FALSE, NOW()),
-- BMP280
(27, 14, '/images/products/bmp280-front.jpg',    TRUE,  NOW()),
(28, 14, '/images/products/bmp280-back.jpg',     FALSE, NOW()),
-- HC-05
(29, 15, '/images/products/hc05-front.jpg',      TRUE,  NOW()),
(30, 15, '/images/products/hc05-back.jpg',       FALSE, NOW()),
-- NRF24L01+
(31, 16, '/images/products/nrf24l01-plus-front.jpg', TRUE,  NOW()),
(32, 16, '/images/products/nrf24l01-plus-angle.jpg', FALSE, NOW()),
-- LCD 1602 I2C
(33, 17, '/images/products/lcd1602-i2c-front.jpg',  TRUE,  NOW()),
(34, 17, '/images/products/lcd1602-i2c-side.jpg',   FALSE, NOW()),
-- OLED 0.96"
(35, 18, '/images/products/oled-096-front.jpg',  TRUE,  NOW()),
(36, 18, '/images/products/oled-096-back.jpg',   FALSE, NOW()),
-- SG90 Servo
(37, 19, '/images/products/sg90-front.jpg',      TRUE,  NOW()),
(38, 19, '/images/products/sg90-parts.jpg',      FALSE, NOW()),
-- L298N Motor Driver
(39, 20, '/images/products/l298n-front.jpg',     TRUE,  NOW()),
(40, 20, '/images/products/l298n-top.jpg',       FALSE, NOW())

ON CONFLICT (image_id) DO NOTHING;

-- ============================================================
-- BƯỚC 7: RESET SEQUENCES (đồng bộ auto-increment)
-- ============================================================
SELECT setval('public.warranty_policy_policy_id_seq', (SELECT MAX(policy_id)  FROM public.warranty_policy));
SELECT setval('public.brand_brand_id_seq',            (SELECT MAX(brand_id)   FROM public.brand));
SELECT setval('public.category_category_id_seq',      (SELECT MAX(category_id) FROM public.category));
SELECT setval('public.product_product_id_seq',        (SELECT MAX(product_id) FROM public.product));
SELECT setval('public.product_image_image_id_seq',    (SELECT MAX(image_id)   FROM public.product_image));

-- ============================================================
-- KIỂM TRA KẾT QUẢ
-- ============================================================
SELECT
    p.product_id,
    p.sku,
    p.name,
    b.name                          AS brand,
    wp.policy_name                  AS warranty,
    p.price,
    p.stock_quantity                AS stock,
    COUNT(pi.image_id)              AS images,
    STRING_AGG(c.name, ', ')        AS categories
FROM public.product p
JOIN public.brand              b  ON p.brand_id           = b.brand_id
LEFT JOIN public.warranty_policy wp ON p.warranty_policy_id = wp.policy_id
LEFT JOIN public.product_image  pi ON p.product_id         = pi.product_id
LEFT JOIN public.product_category pc ON p.product_id       = pc.product_id
LEFT JOIN public.category       c  ON pc.category_id       = c.category_id
GROUP BY p.product_id, p.sku, p.name, b.name, wp.policy_name, p.price, p.stock_quantity
ORDER BY p.product_id;
