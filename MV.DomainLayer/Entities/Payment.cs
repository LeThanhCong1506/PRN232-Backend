using System;
using System.Collections.Generic;

namespace MV.DomainLayer.Entities;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int OrderId { get; set; }

    public decimal Amount { get; set; }

    public DateTime? PaymentDate { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? TransactionId { get; set; }

    public string? BankCode { get; set; }

    public string? GatewayResponse { get; set; }

    /// <summary>
    /// Thời gian hết hạn thanh toán online
    /// </summary>
    public DateTime? ExpiredAt { get; set; }

    /// <summary>
    /// Mã tham chiếu thanh toán - nội dung chuyển khoản
    /// </summary>
    public string? PaymentReference { get; set; }

    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Số tiền thực nhận
    /// </summary>
    public decimal? ReceivedAmount { get; set; }

    /// <summary>
    /// URL QR code thanh toán
    /// </summary>
    public string? QrCodeUrl { get; set; }

    /// <summary>
    /// Số lần thanh toán thất bại
    /// </summary>
    public int? RetryCount { get; set; }

    public string? Notes { get; set; }

    /// <summary>
    /// Staff xác nhận thanh toán
    /// </summary>
    public int? VerifiedBy { get; set; }

    /// <summary>
    /// Thời điểm xác nhận thanh toán
    /// </summary>
    public DateTime? VerifiedAt { get; set; }

    public virtual OrderHeader Order { get; set; } = null!;

    public virtual User? VerifiedByNavigation { get; set; }
}
