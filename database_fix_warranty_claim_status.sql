-- ============================================
-- FIX WARRANTY_CLAIM: Đồng bộ cột status
-- ============================================
-- Script này IDEMPOTENT: có thể chạy nhiều lần
--
-- Vấn đề: Code cũ dùng cột `resolution` để lưu status (SUBMITTED, APPROVED...),
-- trong khi DB schema có cột `status` riêng (claim_status_enum).
-- Code mới đã sửa để dùng đúng cột `status`.
--
-- Script này đảm bảo data cũ (tạo bởi app) được migrate sang đúng cột.
-- ============================================

-- 1. Với các claim tạo bởi app (resolution chứa status value, status = default 'SUBMITTED'):
--    Copy giá trị từ resolution sang status, rồi clear resolution
UPDATE WARRANTY_CLAIM
SET status = resolution::claim_status_enum,
    resolution = NULL
WHERE resolution IN ('SUBMITTED', 'APPROVED', 'REJECTED', 'RESOLVED')
  AND (status = 'SUBMITTED' OR status IS NULL);

-- 2. Verify kết quả
SELECT claim_id, status, resolution, resolved_date
FROM WARRANTY_CLAIM
ORDER BY claim_id;

SELECT '========================================' AS separator;
SELECT 'FIX WARRANTY_CLAIM STATUS HOÀN TẤT!' AS result;
SELECT '========================================' AS separator;
