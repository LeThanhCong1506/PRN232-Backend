-- ============================================================
-- Migration: Add chat_message table for real-time chat feature
-- Date: 2026-02-23
-- Description: Tạo bảng chat_message cho tính năng Chat (SignalR)
--              Customer <-> Admin/Staff real-time messaging
-- ============================================================

-- 1. Tạo bảng chat_message
CREATE TABLE IF NOT EXISTS chat_message (
    message_id SERIAL PRIMARY KEY,
    sender_id INTEGER NOT NULL,
    receiver_id INTEGER,
    content TEXT NOT NULL,
    is_from_admin BOOLEAN NOT NULL DEFAULT FALSE,
    sent_at TIMESTAMP NOT NULL DEFAULT NOW(),
    is_read BOOLEAN NOT NULL DEFAULT FALSE,

    CONSTRAINT fk_chat_message_sender
        FOREIGN KEY (sender_id) REFERENCES "user" (user_id) ON DELETE RESTRICT,
    CONSTRAINT fk_chat_message_receiver
        FOREIGN KEY (receiver_id) REFERENCES "user" (user_id) ON DELETE RESTRICT
);

-- 2. Indexes cho truy vấn nhanh
CREATE INDEX IF NOT EXISTS idx_chat_message_sender_id ON chat_message (sender_id);
CREATE INDEX IF NOT EXISTS idx_chat_message_receiver_id ON chat_message (receiver_id);
CREATE INDEX IF NOT EXISTS idx_chat_message_sent_at ON chat_message (sent_at DESC);
CREATE INDEX IF NOT EXISTS idx_chat_message_is_read ON chat_message (is_read) WHERE is_read = FALSE;

-- 3. Comment mô tả
COMMENT ON TABLE chat_message IS 'Real-time chat messages between customers and admin/staff';
COMMENT ON COLUMN chat_message.sender_id IS 'User ID của người gửi';
COMMENT ON COLUMN chat_message.receiver_id IS 'User ID của người nhận (NULL = gửi cho Admin/Store)';
COMMENT ON COLUMN chat_message.is_from_admin IS 'TRUE nếu tin nhắn từ Admin/Staff';
COMMENT ON COLUMN chat_message.is_read IS 'Trạng thái đã đọc';
