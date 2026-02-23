using System;
using System.Collections.Generic;

namespace MV.DomainLayer.Entities;

public partial class VOrderWithPayment
{
    public int? OrderId { get; set; }

    public string? OrderNumber { get; set; }

    public int? UserId { get; set; }

    public string? CustomerName { get; set; }

    public string? CustomerEmail { get; set; }

    public string? CustomerPhone { get; set; }

    public string? Province { get; set; }

    public string? District { get; set; }

    public string? Ward { get; set; }

    public string? StreetAddress { get; set; }

    public string? ShippingAddress { get; set; }

    public decimal? SubtotalAmount { get; set; }

    public decimal? ShippingFee { get; set; }

    public decimal? DiscountAmount { get; set; }

    public decimal? TotalAmount { get; set; }

    public string? OrderNotes { get; set; }

    public string? TrackingNumber { get; set; }

    public string? Carrier { get; set; }

    public DateOnly? ExpectedDeliveryDate { get; set; }

    public string? CancelReason { get; set; }

    public DateTime? OrderDate { get; set; }

    public DateTime? ConfirmedAt { get; set; }

    public DateTime? ShippedAt { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public DateTime? OrderUpdatedAt { get; set; }

    public int? PaymentId { get; set; }

    public decimal? PaymentAmount { get; set; }

    public decimal? ReceivedAmount { get; set; }

    public string? PaymentReference { get; set; }

    public string? TransactionId { get; set; }

    public string? BankCode { get; set; }

    public string? QrCodeUrl { get; set; }

    public DateTime? PaymentDate { get; set; }

    public DateTime? ExpiredAt { get; set; }

    public int? RetryCount { get; set; }

    public int? VerifiedBy { get; set; }

    public DateTime? VerifiedAt { get; set; }

    public string? PaymentNotes { get; set; }
}
