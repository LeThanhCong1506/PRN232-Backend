using System.ComponentModel.DataAnnotations;

namespace MV.DomainLayer.DTOs.Invoice.Request;

public class GenerateInvoiceRequest
{
    [Required(ErrorMessage = "Invoice type is required")]
    [RegularExpression("^(PERSONAL|COMPANY)$", ErrorMessage = "Invoice type must be PERSONAL or COMPANY")]
    public string InvoiceType { get; set; } = null!;

    [Required(ErrorMessage = "Tax code is required")]
    [StringLength(20, MinimumLength = 10, ErrorMessage = "Tax code must be between 10 and 20 characters")]
    public string TaxCode { get; set; } = null!;

    [StringLength(200, ErrorMessage = "Personal name cannot exceed 200 characters")]
    public string? PersonalName { get; set; }

    [StringLength(200, ErrorMessage = "Company name cannot exceed 200 characters")]
    public string? CompanyName { get; set; }

    [StringLength(200, ErrorMessage = "Representative name cannot exceed 200 characters")]
    public string? RepresentativeName { get; set; }

    [Required(ErrorMessage = "Billing address is required")]
    [StringLength(500, ErrorMessage = "Billing address cannot exceed 500 characters")]
    public string BillingAddress { get; set; } = null!;
}