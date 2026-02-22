using System;
using System.Collections.Generic;
using MV.DomainLayer.Entities;
using Microsoft.EntityFrameworkCore;

namespace MV.InfrastructureLayer.DBContext;

public partial class StemDbContext : DbContext
{
    public StemDbContext(DbContextOptions<StemDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Brand> Brands { get; set; }

    public virtual DbSet<Cart> Carts { get; set; }

    public virtual DbSet<CartItem> CartItems { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Coupon> Coupons { get; set; }

    public virtual DbSet<OrderHeader> OrderHeaders { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductBundle> ProductBundles { get; set; }

    public virtual DbSet<ProductImage> ProductImages { get; set; }

    public virtual DbSet<ProductInstance> ProductInstances { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<SepayConfig> SepayConfigs { get; set; }

    public virtual DbSet<SepayTransaction> SepayTransactions { get; set; }

    public virtual DbSet<Tutorial> Tutorials { get; set; }

    public virtual DbSet<TutorialComponent> TutorialComponents { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<VOrderItemsDetail> VOrderItemsDetails { get; set; }

    public virtual DbSet<VOrderWithPayment> VOrderWithPayments { get; set; }

    public virtual DbSet<VSepayPaymentReport> VSepayPaymentReports { get; set; }

    public virtual DbSet<VSepayPending> VSepayPendings { get; set; }

    public virtual DbSet<Warranty> Warranties { get; set; }

    public virtual DbSet<WarrantyClaim> WarrantyClaims { get; set; }

    public virtual DbSet<WarrantyPolicy> WarrantyPolicies { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasPostgresEnum("claim_status_enum", new[] { "SUBMITTED", "APPROVED", "REJECTED", "RESOLVED" })
            .HasPostgresEnum("difficulty_level_enum", new[] { "beginner", "intermediate", "advanced" })
            .HasPostgresEnum("discount_type_enum", new[] { "FIXED_AMOUNT", "PERCENTAGE" })
            .HasPostgresEnum("instance_status_enum", new[] { "IN_STOCK", "SOLD", "WARRANTY", "DEFECTIVE", "RETURNED" })
            .HasPostgresEnum("order_status_enum", new[] { "PENDING", "CONFIRMED", "SHIPPED", "DELIVERED", "CANCELLED" })
            .HasPostgresEnum("payment_method_enum", new[] { "COD", "SEPAY" })
            .HasPostgresEnum("payment_status_enum", new[] { "PENDING", "COMPLETED", "FAILED", "EXPIRED" });

        modelBuilder.Entity<Brand>(entity =>
        {
            entity.HasKey(e => e.BrandId).HasName("brand_pkey");

            entity.ToTable("brand");

            entity.Property(e => e.BrandId).HasColumnName("brand_id");
            entity.Property(e => e.LogoUrl)
                .HasMaxLength(255)
                .HasColumnName("logo_url");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(e => e.CartId).HasName("cart_pkey");

            entity.ToTable("cart");

            entity.HasIndex(e => e.UserId, "cart_user_id_key").IsUnique();

            entity.Property(e => e.CartId).HasColumnName("cart_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithOne(p => p.Cart)
                .HasForeignKey<Cart>(d => d.UserId)
                .HasConstraintName("fk_cart_user");
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasKey(e => e.CartItemId).HasName("cart_item_pkey");

            entity.ToTable("cart_item");

            entity.HasIndex(e => new { e.CartId, e.ProductId }, "unique_cart_product").IsUnique();

            entity.Property(e => e.CartItemId).HasColumnName("cart_item_id");
            entity.Property(e => e.CartId).HasColumnName("cart_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Quantity)
                .HasDefaultValue(1)
                .HasColumnName("quantity");

            entity.HasOne(d => d.Cart).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.CartId)
                .HasConstraintName("fk_ci_cart");

            entity.HasOne(d => d.Product).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("fk_ci_product");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("category_pkey");

            entity.ToTable("category");

            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Coupon>(entity =>
        {
            entity.HasKey(e => e.CouponId).HasName("coupon_pkey");

            entity.ToTable("coupon");

            entity.HasIndex(e => e.Code, "coupon_code_key").IsUnique();

            entity.Property(e => e.CouponId).HasColumnName("coupon_id");
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.DiscountValue)
                .HasPrecision(10, 2)
                .HasColumnName("discount_value");
            entity.Property(e => e.EndDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("end_date");
            entity.Property(e => e.MinOrderValue)
                .HasPrecision(12, 2)
                .HasDefaultValueSql("0")
                .HasColumnName("min_order_value");
            entity.Property(e => e.StartDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("start_date");
            entity.Property(e => e.UsageLimit).HasColumnName("usage_limit");
            entity.Property(e => e.UsedCount)
                .HasDefaultValue(0)
                .HasColumnName("used_count");
        });

        modelBuilder.Entity<OrderHeader>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("order_header_pkey");

            entity.ToTable("order_header");

            entity.HasIndex(e => e.CancelledAt, "idx_order_cancelled_at");

            entity.HasIndex(e => e.Carrier, "idx_order_carrier");

            entity.HasIndex(e => e.ConfirmedAt, "idx_order_confirmed_at");

            entity.HasIndex(e => e.CustomerEmail, "idx_order_customer_email");

            entity.HasIndex(e => e.CustomerPhone, "idx_order_customer_phone");

            entity.HasIndex(e => e.DeliveredAt, "idx_order_delivered_at");

            entity.HasIndex(e => e.OrderNumber, "idx_order_number");

            entity.HasIndex(e => e.ShippedAt, "idx_order_shipped_at");

            entity.HasIndex(e => e.TrackingNumber, "idx_order_tracking");

            entity.HasIndex(e => e.UpdatedAt, "idx_order_updated");

            entity.HasIndex(e => e.UserId, "idx_order_user");

            entity.HasIndex(e => e.OrderNumber, "order_header_order_number_key").IsUnique();

            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.CancelReason)
                .HasComment("Lý do hủy đơn")
                .HasColumnName("cancel_reason");
            entity.Property(e => e.CancelledAt)
                .HasComment("Thời điểm hủy đơn")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("cancelled_at");
            entity.Property(e => e.CancelledBy)
                .HasComment("Người hủy đơn")
                .HasColumnName("cancelled_by");
            entity.Property(e => e.Carrier)
                .HasMaxLength(100)
                .HasComment("Đơn vị vận chuyển")
                .HasColumnName("carrier");
            entity.Property(e => e.ConfirmedAt)
                .HasComment("Thời điểm xác nhận đơn")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("confirmed_at");
            entity.Property(e => e.ConfirmedBy)
                .HasComment("Staff/Admin xác nhận đơn")
                .HasColumnName("confirmed_by");
            entity.Property(e => e.CouponId).HasColumnName("coupon_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.CustomerEmail)
                .HasMaxLength(100)
                .HasColumnName("customer_email");
            entity.Property(e => e.CustomerName)
                .HasMaxLength(100)
                .HasComment("Snapshot tên khách hàng tại thời điểm đặt hàng")
                .HasColumnName("customer_name");
            entity.Property(e => e.CustomerPhone)
                .HasMaxLength(20)
                .HasColumnName("customer_phone");
            entity.Property(e => e.DeliveredAt)
                .HasComment("Thời điểm giao thành công")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("delivered_at");
            entity.Property(e => e.DiscountAmount)
                .HasPrecision(10, 2)
                .HasDefaultValueSql("0")
                .HasColumnName("discount_amount");
            entity.Property(e => e.District)
                .HasMaxLength(50)
                .HasColumnName("district");
            entity.Property(e => e.ExpectedDeliveryDate)
                .HasComment("Ngày dự kiến giao")
                .HasColumnName("expected_delivery_date");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.OrderNumber)
                .HasMaxLength(50)
                .HasColumnName("order_number");
            entity.Property(e => e.Province)
                .HasMaxLength(50)
                .HasColumnName("province");
            entity.Property(e => e.ShippedAt)
                .HasComment("Thời điểm giao vận chuyển")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("shipped_at");
            entity.Property(e => e.ShippedBy)
                .HasComment("Staff/Admin xử lý giao hàng")
                .HasColumnName("shipped_by");
            entity.Property(e => e.ShippingAddress).HasColumnName("shipping_address");
            entity.Property(e => e.ShippingFee)
                .HasPrecision(10, 2)
                .HasDefaultValueSql("0")
                .HasColumnName("shipping_fee");
            entity.Property(e => e.StreetAddress)
                .HasMaxLength(200)
                .HasColumnName("street_address");
            entity.Property(e => e.SubtotalAmount)
                .HasPrecision(12, 2)
                .HasColumnName("subtotal_amount");
            entity.Property(e => e.TotalAmount)
                .HasPrecision(12, 2)
                .HasColumnName("total_amount");
            entity.Property(e => e.TrackingNumber)
                .HasMaxLength(100)
                .HasComment("Mã vận đơn")
                .HasColumnName("tracking_number");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Ward)
                .HasMaxLength(50)
                .HasColumnName("ward");

            entity.HasOne(d => d.CancelledByNavigation).WithMany(p => p.OrderHeaderCancelledByNavigations)
                .HasForeignKey(d => d.CancelledBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_order_cancelled_by");

            entity.HasOne(d => d.ConfirmedByNavigation).WithMany(p => p.OrderHeaderConfirmedByNavigations)
                .HasForeignKey(d => d.ConfirmedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_order_confirmed_by");

            entity.HasOne(d => d.Coupon).WithMany(p => p.OrderHeaders)
                .HasForeignKey(d => d.CouponId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_order_coupon");

            entity.HasOne(d => d.ShippedByNavigation).WithMany(p => p.OrderHeaderShippedByNavigations)
                .HasForeignKey(d => d.ShippedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_order_shipped_by");

            entity.HasOne(d => d.User).WithMany(p => p.OrderHeaderUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_order_user");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.OrderItemId).HasName("order_item_pkey");

            entity.ToTable("order_item");

            entity.Property(e => e.OrderItemId).HasColumnName("order_item_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.DiscountAmount)
                .HasPrecision(10, 2)
                .HasDefaultValueSql("0")
                .HasComment("Giảm giá cho item")
                .HasColumnName("discount_amount");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.ProductImageUrl)
                .HasMaxLength(255)
                .HasComment("Snapshot ảnh sản phẩm")
                .HasColumnName("product_image_url");
            entity.Property(e => e.ProductName)
                .HasMaxLength(255)
                .HasColumnName("product_name");
            entity.Property(e => e.ProductSku)
                .HasMaxLength(50)
                .HasColumnName("product_sku");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.Subtotal)
                .HasPrecision(12, 2)
                .HasColumnName("subtotal");
            entity.Property(e => e.UnitPrice)
                .HasPrecision(12, 2)
                .HasColumnName("unit_price");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("fk_oi_order");

            entity.HasOne(d => d.Product).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_oi_product");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("payment_pkey");

            entity.ToTable("payment");

            entity.HasIndex(e => e.ExpiredAt, "idx_payment_expired");

            entity.HasIndex(e => e.ReceivedAmount, "idx_payment_received_amount");

            entity.HasIndex(e => e.PaymentReference, "idx_payment_reference");

            entity.HasIndex(e => e.TransactionId, "idx_payment_transaction");

            entity.HasIndex(e => e.VerifiedBy, "idx_payment_verified_by");

            entity.HasIndex(e => e.OrderId, "payment_order_id_key").IsUnique();

            entity.Property(e => e.PaymentId).HasColumnName("payment_id");
            entity.Property(e => e.Amount)
                .HasPrecision(12, 2)
                .HasColumnName("amount");
            entity.Property(e => e.BankCode)
                .HasMaxLength(50)
                .HasColumnName("bank_code");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiredAt)
                .HasComment("Thời gian hết hạn thanh toán online")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("expired_at");
            entity.Property(e => e.GatewayResponse).HasColumnName("gateway_response");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.PaymentDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("payment_date");
            entity.Property(e => e.PaymentReference)
                .HasMaxLength(100)
                .HasComment("Mã tham chiếu thanh toán - nội dung chuyển khoản")
                .HasColumnName("payment_reference");
            entity.Property(e => e.QrCodeUrl)
                .HasMaxLength(500)
                .HasComment("URL QR code thanh toán")
                .HasColumnName("qr_code_url");
            entity.Property(e => e.ReceivedAmount)
                .HasPrecision(12, 2)
                .HasComment("Số tiền thực nhận")
                .HasColumnName("received_amount");
            entity.Property(e => e.RetryCount)
                .HasDefaultValue(0)
                .HasComment("Số lần thanh toán thất bại")
                .HasColumnName("retry_count");
            entity.Property(e => e.TransactionId)
                .HasMaxLength(100)
                .HasColumnName("transaction_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");
            entity.Property(e => e.VerifiedAt)
                .HasComment("Thời điểm xác nhận thanh toán")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("verified_at");
            entity.Property(e => e.VerifiedBy)
                .HasComment("Staff xác nhận thanh toán")
                .HasColumnName("verified_by");

            entity.HasOne(d => d.Order).WithOne(p => p.Payment)
                .HasForeignKey<Payment>(d => d.OrderId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_payment_order");

            entity.HasOne(d => d.VerifiedByNavigation).WithMany(p => p.Payments)
                .HasForeignKey(d => d.VerifiedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_payment_verified_by");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("product_pkey");

            entity.ToTable("product");

            entity.HasIndex(e => e.BrandId, "idx_product_brand");

            entity.HasIndex(e => e.Sku, "idx_product_sku");

            entity.HasIndex(e => e.ProductType, "idx_product_type");

            entity.HasIndex(e => e.Sku, "product_sku_key").IsUnique();

            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.BrandId).HasColumnName("brand_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.HasSerialTracking)
                .HasDefaultValue(false)
                .HasColumnName("has_serial_tracking");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.Price)
                .HasPrecision(12, 2)
                .HasColumnName("price");
            entity.Property(e => e.ProductType)
                .HasMaxLength(50)
                .HasColumnName("product_type");
            entity.Property(e => e.Sku)
                .HasMaxLength(50)
                .HasColumnName("sku");
            entity.Property(e => e.StockQuantity)
                .HasDefaultValue(0)
                .HasColumnName("stock_quantity");
            entity.Property(e => e.WarrantyPolicyId).HasColumnName("warranty_policy_id");

            entity.HasOne(d => d.Brand).WithMany(p => p.Products)
                .HasForeignKey(d => d.BrandId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_product_brand");

            entity.HasOne(d => d.WarrantyPolicy).WithMany(p => p.Products)
                .HasForeignKey(d => d.WarrantyPolicyId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_product_warranty");

            entity.HasMany(d => d.Categories).WithMany(p => p.Products)
                .UsingEntity<Dictionary<string, object>>(
                    "ProductCategory",
                    r => r.HasOne<Category>().WithMany()
                        .HasForeignKey("CategoryId")
                        .HasConstraintName("fk_pc_category"),
                    l => l.HasOne<Product>().WithMany()
                        .HasForeignKey("ProductId")
                        .HasConstraintName("fk_pc_product"),
                    j =>
                    {
                        j.HasKey("ProductId", "CategoryId").HasName("product_category_pkey");
                        j.ToTable("product_category");
                        j.IndexerProperty<int>("ProductId").HasColumnName("product_id");
                        j.IndexerProperty<int>("CategoryId").HasColumnName("category_id");
                    });
        });

        modelBuilder.Entity<ProductBundle>(entity =>
        {
            entity.HasKey(e => e.BundleId).HasName("product_bundle_pkey");

            entity.ToTable("product_bundle");

            entity.Property(e => e.BundleId).HasColumnName("bundle_id");
            entity.Property(e => e.ChildProductId).HasColumnName("child_product_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.ParentProductId).HasColumnName("parent_product_id");
            entity.Property(e => e.Quantity)
                .HasDefaultValue(1)
                .HasColumnName("quantity");

            entity.HasOne(d => d.ChildProduct).WithMany(p => p.ProductBundleChildProducts)
                .HasForeignKey(d => d.ChildProductId)
                .HasConstraintName("fk_bundle_child");

            entity.HasOne(d => d.ParentProduct).WithMany(p => p.ProductBundleParentProducts)
                .HasForeignKey(d => d.ParentProductId)
                .HasConstraintName("fk_bundle_parent");
        });

        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.HasKey(e => e.ImageId).HasName("product_image_pkey");

            entity.ToTable("product_image");

            entity.Property(e => e.ImageId).HasColumnName("image_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(255)
                .HasColumnName("image_url");
            entity.Property(e => e.IsPrimary)
                .HasDefaultValue(false)
                .HasColumnName("is_primary");
            entity.Property(e => e.ProductId).HasColumnName("product_id");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductImages)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("fk_image_product");
        });

        modelBuilder.Entity<ProductInstance>(entity =>
        {
            entity.HasKey(e => e.SerialNumber).HasName("product_instance_pkey");

            entity.ToTable("product_instance");

            entity.Property(e => e.SerialNumber)
                .HasMaxLength(100)
                .HasColumnName("serial_number");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.ManufacturingDate).HasColumnName("manufacturing_date");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.OrderItemId).HasColumnName("order_item_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");

            entity.HasOne(d => d.OrderItem).WithMany(p => p.ProductInstances)
                .HasForeignKey(d => d.OrderItemId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_instance_order_item");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductInstances)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_instance_product");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.ReviewId).HasName("review_pkey");

            entity.ToTable("review");

            entity.HasIndex(e => e.ProductId, "idx_review_product");

            entity.HasIndex(e => e.UserId, "idx_review_user");

            entity.Property(e => e.ReviewId).HasColumnName("review_id");
            entity.Property(e => e.Comment).HasColumnName("comment");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Rating).HasColumnName("rating");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Product).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("fk_review_product");

            entity.HasOne(d => d.User).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_review_user");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("role_pkey");

            entity.ToTable("role");

            entity.HasIndex(e => e.RoleName, "role_role_name_key").IsUnique();

            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.RoleName)
                .HasMaxLength(50)
                .HasColumnName("role_name");
        });

        modelBuilder.Entity<SepayConfig>(entity =>
        {
            entity.HasKey(e => e.ConfigId).HasName("sepay_config_pkey");

            entity.ToTable("sepay_config", tb => tb.HasComment("Cấu hình tài khoản ngân hàng cho SePay"));

            entity.HasIndex(e => e.IsActive, "idx_sepay_config_active");

            entity.Property(e => e.ConfigId).HasColumnName("config_id");
            entity.Property(e => e.AccountName)
                .HasMaxLength(100)
                .HasColumnName("account_name");
            entity.Property(e => e.AccountNumber)
                .HasMaxLength(50)
                .HasColumnName("account_number");
            entity.Property(e => e.ApiKey)
                .HasMaxLength(255)
                .HasColumnName("api_key");
            entity.Property(e => e.BankCode)
                .HasMaxLength(20)
                .HasColumnName("bank_code");
            entity.Property(e => e.BankName)
                .HasMaxLength(100)
                .HasColumnName("bank_name");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<SepayTransaction>(entity =>
        {
            entity.HasKey(e => e.TransactionId).HasName("sepay_transaction_pkey");

            entity.ToTable("sepay_transaction", tb => tb.HasComment("Log tất cả giao dịch nhận được từ SePay webhook"));

            entity.HasIndex(e => e.Code, "idx_sepay_trans_code");

            entity.HasIndex(e => e.TransactionDate, "idx_sepay_trans_date");

            entity.HasIndex(e => e.OrderId, "idx_sepay_trans_order");

            entity.HasIndex(e => e.IsProcessed, "idx_sepay_trans_processed");

            entity.Property(e => e.TransactionId).HasColumnName("transaction_id");
            entity.Property(e => e.AccountNumber)
                .HasMaxLength(50)
                .HasColumnName("account_number");
            entity.Property(e => e.Accumulated)
                .HasPrecision(12, 2)
                .HasColumnName("accumulated");
            entity.Property(e => e.Code)
                .HasMaxLength(100)
                .HasColumnName("code");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Gateway)
                .HasMaxLength(50)
                .HasColumnName("gateway");
            entity.Property(e => e.IsProcessed)
                .HasDefaultValue(false)
                .HasColumnName("is_processed");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.ProcessedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("processed_at");
            entity.Property(e => e.RawData).HasColumnName("raw_data");
            entity.Property(e => e.ReferenceNumber)
                .HasMaxLength(100)
                .HasColumnName("reference_number");
            entity.Property(e => e.SepayId)
                .HasMaxLength(100)
                .HasColumnName("sepay_id");
            entity.Property(e => e.TransactionDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("transaction_date");
            entity.Property(e => e.TransferAmount)
                .HasPrecision(12, 2)
                .HasColumnName("transfer_amount");
            entity.Property(e => e.TransferType)
                .HasMaxLength(20)
                .HasColumnName("transfer_type");

            entity.HasOne(d => d.Order).WithMany(p => p.SepayTransactions)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_sepay_order");
        });

        modelBuilder.Entity<Tutorial>(entity =>
        {
            entity.HasKey(e => e.TutorialId).HasName("tutorial_pkey");

            entity.ToTable("tutorial");

            entity.Property(e => e.TutorialId).HasColumnName("tutorial_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.EstimatedDuration)
                .HasComment("Duration in minutes")
                .HasColumnName("estimated_duration");
            entity.Property(e => e.Instructions).HasColumnName("instructions");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
            entity.Property(e => e.VideoUrl)
                .HasMaxLength(255)
                .HasColumnName("video_url");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Tutorials)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("fk_tutorial_user");
        });

        modelBuilder.Entity<TutorialComponent>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("tutorial_component_pkey");

            entity.ToTable("tutorial_component");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Quantity)
                .HasDefaultValue(1)
                .HasColumnName("quantity");
            entity.Property(e => e.TutorialId).HasColumnName("tutorial_id");
            entity.Property(e => e.UsageNote).HasColumnName("usage_note");

            entity.HasOne(d => d.Product).WithMany(p => p.TutorialComponents)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("fk_tc_product");

            entity.HasOne(d => d.Tutorial).WithMany(p => p.TutorialComponents)
                .HasForeignKey(d => d.TutorialId)
                .HasConstraintName("fk_tc_tutorial");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("USER_pkey");

            entity.ToTable("USER");

            entity.HasIndex(e => e.Email, "USER_email_key").IsUnique();

            entity.HasIndex(e => e.Username, "USER_username_key").IsUnique();

            entity.HasIndex(e => e.Email, "idx_user_email");

            entity.HasIndex(e => e.FullName, "idx_user_fullname");

            entity.HasIndex(e => e.Phone, "idx_user_phone");

            entity.HasIndex(e => e.RoleId, "idx_user_role");

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Address).HasColumnName("address");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.District)
                .HasMaxLength(50)
                .HasColumnName("district");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.FullName)
                .HasMaxLength(100)
                .HasColumnName("full_name");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.Province)
                .HasMaxLength(50)
                .HasColumnName("province");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.StreetAddress)
                .HasMaxLength(200)
                .HasColumnName("street_address");
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .HasColumnName("username");
            entity.Property(e => e.Ward)
                .HasMaxLength(50)
                .HasColumnName("ward");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_user_role");
        });

        modelBuilder.Entity<VOrderItemsDetail>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_order_items_detail");

            entity.Property(e => e.CustomerName)
                .HasMaxLength(100)
                .HasColumnName("customer_name");
            entity.Property(e => e.ItemDiscount)
                .HasPrecision(10, 2)
                .HasColumnName("item_discount");
            entity.Property(e => e.ItemNotes).HasColumnName("item_notes");
            entity.Property(e => e.ItemSubtotal)
                .HasPrecision(12, 2)
                .HasColumnName("item_subtotal");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.OrderItemId).HasColumnName("order_item_id");
            entity.Property(e => e.OrderNumber)
                .HasMaxLength(50)
                .HasColumnName("order_number");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.ProductImageUrl)
                .HasMaxLength(255)
                .HasColumnName("product_image_url");
            entity.Property(e => e.ProductName)
                .HasMaxLength(255)
                .HasColumnName("product_name");
            entity.Property(e => e.ProductSku)
                .HasMaxLength(50)
                .HasColumnName("product_sku");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.UnitPrice)
                .HasPrecision(12, 2)
                .HasColumnName("unit_price");
        });

        modelBuilder.Entity<VOrderWithPayment>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_order_with_payment");

            entity.Property(e => e.BankCode)
                .HasMaxLength(50)
                .HasColumnName("bank_code");
            entity.Property(e => e.CancelReason).HasColumnName("cancel_reason");
            entity.Property(e => e.CancelledAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("cancelled_at");
            entity.Property(e => e.Carrier)
                .HasMaxLength(100)
                .HasColumnName("carrier");
            entity.Property(e => e.ConfirmedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("confirmed_at");
            entity.Property(e => e.CustomerEmail)
                .HasMaxLength(100)
                .HasColumnName("customer_email");
            entity.Property(e => e.CustomerName)
                .HasMaxLength(100)
                .HasColumnName("customer_name");
            entity.Property(e => e.CustomerPhone)
                .HasMaxLength(20)
                .HasColumnName("customer_phone");
            entity.Property(e => e.DeliveredAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("delivered_at");
            entity.Property(e => e.DiscountAmount)
                .HasPrecision(10, 2)
                .HasColumnName("discount_amount");
            entity.Property(e => e.District)
                .HasMaxLength(50)
                .HasColumnName("district");
            entity.Property(e => e.ExpectedDeliveryDate).HasColumnName("expected_delivery_date");
            entity.Property(e => e.ExpiredAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("expired_at");
            entity.Property(e => e.OrderDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("order_date");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.OrderNotes).HasColumnName("order_notes");
            entity.Property(e => e.OrderNumber)
                .HasMaxLength(50)
                .HasColumnName("order_number");
            entity.Property(e => e.OrderUpdatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("order_updated_at");
            entity.Property(e => e.PaymentAmount)
                .HasPrecision(12, 2)
                .HasColumnName("payment_amount");
            entity.Property(e => e.PaymentDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("payment_date");
            entity.Property(e => e.PaymentId).HasColumnName("payment_id");
            entity.Property(e => e.PaymentNotes).HasColumnName("payment_notes");
            entity.Property(e => e.PaymentReference)
                .HasMaxLength(100)
                .HasColumnName("payment_reference");
            entity.Property(e => e.Province)
                .HasMaxLength(50)
                .HasColumnName("province");
            entity.Property(e => e.QrCodeUrl)
                .HasMaxLength(500)
                .HasColumnName("qr_code_url");
            entity.Property(e => e.ReceivedAmount)
                .HasPrecision(12, 2)
                .HasColumnName("received_amount");
            entity.Property(e => e.RetryCount).HasColumnName("retry_count");
            entity.Property(e => e.ShippedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("shipped_at");
            entity.Property(e => e.ShippingAddress).HasColumnName("shipping_address");
            entity.Property(e => e.ShippingFee)
                .HasPrecision(10, 2)
                .HasColumnName("shipping_fee");
            entity.Property(e => e.StreetAddress)
                .HasMaxLength(200)
                .HasColumnName("street_address");
            entity.Property(e => e.SubtotalAmount)
                .HasPrecision(12, 2)
                .HasColumnName("subtotal_amount");
            entity.Property(e => e.TotalAmount)
                .HasPrecision(12, 2)
                .HasColumnName("total_amount");
            entity.Property(e => e.TrackingNumber)
                .HasMaxLength(100)
                .HasColumnName("tracking_number");
            entity.Property(e => e.TransactionId)
                .HasMaxLength(100)
                .HasColumnName("transaction_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.VerifiedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("verified_at");
            entity.Property(e => e.VerifiedBy).HasColumnName("verified_by");
            entity.Property(e => e.Ward)
                .HasMaxLength(50)
                .HasColumnName("ward");
        });

        modelBuilder.Entity<VSepayPaymentReport>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_sepay_payment_report");

            entity.Property(e => e.CustomerName)
                .HasMaxLength(100)
                .HasColumnName("customer_name");
            entity.Property(e => e.CustomerPhone)
                .HasMaxLength(20)
                .HasColumnName("customer_phone");
            entity.Property(e => e.ExpectedAmount)
                .HasPrecision(12, 2)
                .HasColumnName("expected_amount");
            entity.Property(e => e.ExpiredAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("expired_at");
            entity.Property(e => e.IsProcessed).HasColumnName("is_processed");
            entity.Property(e => e.OrderNumber)
                .HasMaxLength(50)
                .HasColumnName("order_number");
            entity.Property(e => e.PaymentDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("payment_date");
            entity.Property(e => e.PaymentId).HasColumnName("payment_id");
            entity.Property(e => e.PaymentReference)
                .HasMaxLength(100)
                .HasColumnName("payment_reference");
            entity.Property(e => e.QrCodeUrl)
                .HasMaxLength(500)
                .HasColumnName("qr_code_url");
            entity.Property(e => e.ReceivedAmount)
                .HasPrecision(12, 2)
                .HasColumnName("received_amount");
            entity.Property(e => e.RetryCount).HasColumnName("retry_count");
            entity.Property(e => e.SepayId)
                .HasMaxLength(100)
                .HasColumnName("sepay_id");
            entity.Property(e => e.TransactionId)
                .HasMaxLength(100)
                .HasColumnName("transaction_id");
            entity.Property(e => e.TransferAmount)
                .HasPrecision(12, 2)
                .HasColumnName("transfer_amount");
            entity.Property(e => e.TransferContent).HasColumnName("transfer_content");
            entity.Property(e => e.TransferDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("transfer_date");
        });

        modelBuilder.Entity<VSepayPending>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_sepay_pending");

            entity.Property(e => e.AccountNumber)
                .HasMaxLength(50)
                .HasColumnName("account_number");
            entity.Property(e => e.Accumulated)
                .HasPrecision(12, 2)
                .HasColumnName("accumulated");
            entity.Property(e => e.Code)
                .HasMaxLength(100)
                .HasColumnName("code");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.ExpectedAmount)
                .HasPrecision(12, 2)
                .HasColumnName("expected_amount");
            entity.Property(e => e.Gateway)
                .HasMaxLength(50)
                .HasColumnName("gateway");
            entity.Property(e => e.IsProcessed).HasColumnName("is_processed");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.OrderNumber)
                .HasMaxLength(50)
                .HasColumnName("order_number");
            entity.Property(e => e.ProcessedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("processed_at");
            entity.Property(e => e.RawData).HasColumnName("raw_data");
            entity.Property(e => e.ReferenceNumber)
                .HasMaxLength(100)
                .HasColumnName("reference_number");
            entity.Property(e => e.SepayId)
                .HasMaxLength(100)
                .HasColumnName("sepay_id");
            entity.Property(e => e.TransactionDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("transaction_date");
            entity.Property(e => e.TransactionId).HasColumnName("transaction_id");
            entity.Property(e => e.TransferAmount)
                .HasPrecision(12, 2)
                .HasColumnName("transfer_amount");
            entity.Property(e => e.TransferType)
                .HasMaxLength(20)
                .HasColumnName("transfer_type");
        });

        modelBuilder.Entity<Warranty>(entity =>
        {
            entity.HasKey(e => e.WarrantyId).HasName("warranty_pkey");

            entity.ToTable("warranty");

            entity.HasIndex(e => e.IsActive, "idx_warranty_active");

            entity.HasIndex(e => e.SerialNumber, "idx_warranty_serial");

            entity.Property(e => e.WarrantyId).HasColumnName("warranty_id");
            entity.Property(e => e.ActivationDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("activation_date");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.SerialNumber)
                .HasMaxLength(100)
                .HasColumnName("serial_number");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.WarrantyPolicyId).HasColumnName("warranty_policy_id");

            entity.HasOne(d => d.SerialNumberNavigation).WithMany(p => p.Warranties)
                .HasForeignKey(d => d.SerialNumber)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_warranty_instance");

            entity.HasOne(d => d.WarrantyPolicy).WithMany(p => p.Warranties)
                .HasForeignKey(d => d.WarrantyPolicyId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_warranty_policy");
        });

        modelBuilder.Entity<WarrantyClaim>(entity =>
        {
            entity.HasKey(e => e.ClaimId).HasName("warranty_claim_pkey");

            entity.ToTable("warranty_claim");

            entity.HasIndex(e => e.UserId, "idx_warranty_claim_user");

            entity.HasIndex(e => e.WarrantyId, "idx_warranty_claim_warranty");

            entity.Property(e => e.ClaimId).HasColumnName("claim_id");
            entity.Property(e => e.ClaimDate).HasColumnName("claim_date");
            entity.Property(e => e.ContactPhone)
                .HasMaxLength(20)
                .HasComment("SĐT liên hệ khách hàng khi gửi yêu cầu bảo hành")
                .HasColumnName("contact_phone");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.IssueDescription).HasColumnName("issue_description");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.Resolution).HasColumnName("resolution");
            entity.Property(e => e.ResolutionNote)
                .HasComment("Ghi chú xử lý từ admin/staff khi resolve claim")
                .HasColumnName("resolution_note");
            entity.Property(e => e.ResolvedDate).HasColumnName("resolved_date");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.WarrantyId).HasColumnName("warranty_id");

            entity.HasOne(d => d.User).WithMany(p => p.WarrantyClaims)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_claim_user");

            entity.HasOne(d => d.Warranty).WithMany(p => p.WarrantyClaims)
                .HasForeignKey(d => d.WarrantyId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_claim_warranty");
        });

        modelBuilder.Entity<WarrantyPolicy>(entity =>
        {
            entity.HasKey(e => e.PolicyId).HasName("warranty_policy_pkey");

            entity.ToTable("warranty_policy");

            entity.HasIndex(e => e.PolicyName, "warranty_policy_policy_name_key").IsUnique();

            entity.Property(e => e.PolicyId).HasColumnName("policy_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.DurationMonths).HasColumnName("duration_months");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.PolicyName)
                .HasMaxLength(100)
                .HasColumnName("policy_name");
            entity.Property(e => e.TermsAndConditions).HasColumnName("terms_and_conditions");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
