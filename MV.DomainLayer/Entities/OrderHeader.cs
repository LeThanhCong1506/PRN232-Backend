using System;
using System.Collections.Generic;

namespace MV.DomainLayer.Entities;

public partial class OrderHeader
{
    public int OrderId { get; set; }

    public int UserId { get; set; }

    public int? CouponId { get; set; }

    public string OrderNumber { get; set; } = null!;

    public decimal? ShippingFee { get; set; }

    public decimal SubtotalAmount { get; set; }

    public decimal? DiscountAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public string ShippingAddress { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// Snapshot tên khách hàng tại thời điểm đặt hàng
    /// </summary>
    public string? CustomerName { get; set; }

    public string? CustomerEmail { get; set; }

    public string? CustomerPhone { get; set; }

    public string? Province { get; set; }

    public string? District { get; set; }

    public string? Ward { get; set; }

    public string? StreetAddress { get; set; }

    public string? Notes { get; set; }

    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Thời điểm xác nhận đơn
    /// </summary>
    public DateTime? ConfirmedAt { get; set; }

    /// <summary>
    /// Thời điểm giao vận chuyển
    /// </summary>
    public DateTime? ShippedAt { get; set; }

    /// <summary>
    /// Thời điểm giao thành công
    /// </summary>
    public DateTime? DeliveredAt { get; set; }

    /// <summary>
    /// Thời điểm hủy đơn
    /// </summary>
    public DateTime? CancelledAt { get; set; }

    /// <summary>
    /// Lý do hủy đơn
    /// </summary>
    public string? CancelReason { get; set; }

    /// <summary>
    /// Staff/Admin xác nhận đơn
    /// </summary>
    public int? ConfirmedBy { get; set; }

    /// <summary>
    /// Staff/Admin xử lý giao hàng
    /// </summary>
    public int? ShippedBy { get; set; }

    /// <summary>
    /// Người hủy đơn
    /// </summary>
    public int? CancelledBy { get; set; }

    /// <summary>
    /// Mã vận đơn
    /// </summary>
    public string? TrackingNumber { get; set; }

    /// <summary>
    /// Đơn vị vận chuyển
    /// </summary>
    public string? Carrier { get; set; }

    /// <summary>
    /// Ngày dự kiến giao
    /// </summary>
    public DateOnly? ExpectedDeliveryDate { get; set; }

    public virtual User? CancelledByNavigation { get; set; }

    public virtual User? ConfirmedByNavigation { get; set; }

    public virtual Coupon? Coupon { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual Payment? Payment { get; set; }

    public virtual ICollection<SepayTransaction> SepayTransactions { get; set; } = new List<SepayTransaction>();

    public virtual User? ShippedByNavigation { get; set; }

    public virtual User User { get; set; } = null!;
}
