using System.Collections.Generic;

namespace MV.DomainLayer.DTOs.ResponseModels
{
    public class CartResponseDto
    {
        public int CartId { get; set; }
        public List<CartItemDto> Items { get; set; } = new List<CartItemDto>();
        public CartSummaryDto Summary { get; set; }
    }

    public class CartItemDto
    {
        public int CartItemId { get; set; }
        public int Quantity { get; set; }
        public CartProductDto Product { get; set; }
        public decimal ItemTotal { get; set; } // Quantity * Price
    }

    public class CartProductDto
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public string Sku { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string PrimaryImage { get; set; }
        public bool InStock { get; set; }
    }

    public class CartSummaryDto
    {
        public int TotalItems { get; set; }
        public decimal Subtotal { get; set; }
        public decimal ShippingFee { get; set; } = 30000; // Fixed fee for now
        public decimal Discount { get; set; } = 0;
        public decimal Total { get; set; } // Subtotal + Shipping - Discount
    }
}