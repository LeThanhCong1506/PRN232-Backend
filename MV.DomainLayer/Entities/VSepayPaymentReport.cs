using System;
using System.Collections.Generic;

namespace MV.DomainLayer.Entities;

public partial class VSepayPaymentReport
{
    public int? PaymentId { get; set; }

    public string? OrderNumber { get; set; }

    public string? CustomerName { get; set; }

    public string? CustomerPhone { get; set; }

    public string? PaymentReference { get; set; }

    public decimal? ExpectedAmount { get; set; }

    public decimal? ReceivedAmount { get; set; }

    public string? QrCodeUrl { get; set; }

    public DateTime? ExpiredAt { get; set; }

    public DateTime? PaymentDate { get; set; }

    public string? TransactionId { get; set; }

    public int? RetryCount { get; set; }

    public string? SepayId { get; set; }

    public decimal? TransferAmount { get; set; }

    public string? TransferContent { get; set; }

    public DateTime? TransferDate { get; set; }

    public bool? IsProcessed { get; set; }
}
