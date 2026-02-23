using System;
using System.Collections.Generic;

namespace MV.DomainLayer.Entities;

/// <summary>
/// Cấu hình tài khoản ngân hàng cho SePay
/// </summary>
public partial class SepayConfig
{
    public int ConfigId { get; set; }

    public string BankName { get; set; } = null!;

    public string BankCode { get; set; } = null!;

    public string AccountNumber { get; set; } = null!;

    public string AccountName { get; set; } = null!;

    public string? ApiKey { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
