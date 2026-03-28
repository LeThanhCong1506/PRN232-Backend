using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MV.ApplicationLayer.Interfaces;
using MV.ApplicationLayer.Services;
using MV.DomainLayer.Helpers;
using MV.DomainLayer.Interfaces;
using MV.InfrastructureLayer.DBContext;
using MV.InfrastructureLayer.Interfaces;
using MV.InfrastructureLayer.Repositories;
using MV.InfrastructureLayer.Services;
using MV.PresentationLayer.Hubs;
using MV.PresentationLayer.Services;
using System.Text;

namespace MV.PresentationLayer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Fix PostgreSQL DateTime UTC issue
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

            var builder = WebApplication.CreateBuilder(args);

            // Configure CORS for Frontend
            // Đọc thêm origins từ config (dùng cho production frontend URL)
            var extraOrigins = builder.Configuration
                .GetSection("Cors:AllowedOrigins")
                .Get<string[]>() ?? Array.Empty<string>();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    var origins = new[]
                    {
                        "http://localhost:5173",
                        "http://localhost:3000",
                        "http://127.0.0.1:5173",
                        "http://localhost:5174",
                        "http://localhost:5175",
                        "http://127.0.0.1:3000",
                        "http://localhost:5255",  // Swagger Docker
                        "http://127.0.0.1:5255",
                        // Production FE domain
                        "https://prn232.store",
                        "https://www.prn232.store"
                    }.Concat(extraOrigins).Distinct().ToArray();

                    policy.WithOrigins(origins)
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();
                });
            });

            // Configure Swagger
            builder.Services.AddSwaggerGen(c =>
            {
                c.EnableAnnotations();
                c.MapType<IFormFile>(() => new Microsoft.OpenApi.Models.OpenApiSchema { Type = "string", Format = "binary" });
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "MV API",
                    Version = "v1"
                });

                // Configure Swagger to use string enums
                c.UseAllOfToExtendReferenceSchemas();
                c.UseInlineDefinitionsForEnums();

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Nhập: Bearer {your JWT token}"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            // Configure JWT Authentication
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = builder.Configuration["Jwt:Issuer"],
                        ValidAudience = builder.Configuration["Jwt:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
                        )
                    };

                    // SignalR: cho phép gửi JWT token qua query string (vì WebSocket không hỗ trợ header)
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"];
                            var path = context.HttpContext.Request.Path;
                            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                            {
                                context.Token = accessToken;
                            }
                            return Task.CompletedTask;
                        }
                    };
                });

            builder.Services.AddAuthorization();

            builder.Services.AddScoped<IJwtService, JwtService>();

            // Add services to the container.
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    // CRITICAL: SePay webhook gửi JSON có thể khác case (TransferAmount vs transferAmount)
                    // System.Text.Json mặc định case-sensitive → model binding fail → 400 trước khi vào controller
                    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
                })
                .ConfigureApiBehaviorOptions(options =>
                {
                    options.InvalidModelStateResponseFactory = context =>
                    {
                        var errors = context.ModelState
                            .Where(e => e.Value?.Errors.Count > 0)
                            .Select(e => $"{e.Key}: {string.Join(", ", e.Value!.Errors.Select(x => x.ErrorMessage))}")
                            .ToList();
                        Console.WriteLine($"[MODEL VALIDATION FAILED] {context.HttpContext.Request.Path}");
                        foreach (var err in errors)
                            Console.WriteLine($"  -> {err}");
                        return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(context.ModelState);
                    };
                });

            builder.Services.AddScoped<IRoleRepository, RoleRepository>();
            builder.Services.AddScoped<IProductRepository, ProductRepository>();
            builder.Services.AddScoped<ICartRepository, CartRepository>();
            builder.Services.AddScoped<ICouponRepository, CouponRepository>();
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
            builder.Services.AddScoped<IBrandRepository, BrandRepository>();
            builder.Services.AddScoped<IProductImageRepository, ProductImageRepository>();
            builder.Services.AddScoped<IProductBundleRepository, ProductBundleRepository>();
            builder.Services.AddScoped<IWarrantyRepository, WarrantyRepository>();
            builder.Services.AddScoped<IWarrantyClaimRepository, WarrantyClaimRepository>();
            builder.Services.AddScoped<IOrderRepository, OrderRepository>();
            builder.Services.AddScoped<ISepayRepository, SepayRepository>();
            builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
            builder.Services.AddScoped<INotificationRepository, NotificationRepository>();

            builder.Services.AddScoped<IRoleService, RoleService>();
            builder.Services.AddScoped<IOrderService, OrderService>();
            builder.Services.AddScoped<IPaymentService, PaymentService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IProductService, ProductService>();
            builder.Services.AddScoped<ICartService, CartService>();
            builder.Services.AddScoped<IBrandService, BrandService>();
            builder.Services.AddScoped<ICategoryService, CategoryService>();
            builder.Services.AddScoped<IProductImageService, ProductImageService>();
            builder.Services.AddScoped<IProductBundleService, ProductBundleService>();
            builder.Services.AddScoped<IWarrantyService, WarrantyService>();
            builder.Services.AddScoped<IWarrantyClaimService, WarrantyClaimService>();
            builder.Services.AddScoped<ICheckoutService, CheckoutService>();
            builder.Services.AddScoped<IAdminProductService, AdminProductService>();
            builder.Services.AddScoped<IAdminOrderService, AdminOrderService>();
            builder.Services.AddScoped<IReviewService, ReviewService>();
            
            // OAuth
            builder.Services.AddHttpClient();
            builder.Services.AddScoped<IExternalAuthService, ExternalAuthService>();

            // Background Services
            builder.Services.AddHostedService<PaymentExpiryBackgroundService>();

            // Cloudinary
            builder.Services.AddSingleton<ICloudinaryService, CloudinaryService>();

            builder.Services.AddSingleton<IChatbotService, ChatbotService>();

            // SignalR + Realtime Notification
            builder.Services.AddSignalR();
            builder.Services.AddSingleton<INotificationService, SignalRNotificationService>();

            // Register DbContext with connection string from appsettings
            // Connection pool settings tự động theo environment:
            // - Production (Render free tier): pool nhỏ vì max_connections giới hạn
            // - Development (local PostgreSQL): pool lớn hơn vì max_connections=100
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            if (!connectionString!.Contains("Maximum Pool Size", StringComparison.OrdinalIgnoreCase))
            {
                var isProduction = builder.Environment.IsProduction();
                var poolSize = isProduction ? 10 : 30;
                connectionString += $";Maximum Pool Size={poolSize};Minimum Pool Size=0;Connection Idle Lifetime=60;Timeout=15;Command Timeout=30";
            }

            // MEMORY FIX: AddDbContextPool tái sử dụng DbContext instances thay vì tạo mới mỗi request
            // Tránh OOM do EF Core tích lũy internal service providers không được giải phóng
            builder.Services.AddDbContextPool<StemDbContext>(options =>
                options.UseNpgsql(connectionString,
                    npgsqlOptions =>
                    {
                        npgsqlOptions.MigrationsAssembly("MV.InfrastructureLayer");

                        // Enable automatic retry on transient failures
                        // maxRetryCount: 5 attempts, maxRetryDelay: wait max 30s between retries
                        npgsqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorCodesToAdd: null);

                        // SingleQuery (default) — dùng 1 connection per query thay vì N connections cho N Include
                        // Chỉ dùng .AsSplitQuery() explicit khi thực sự cần (large result sets)
                        npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);

                        npgsqlOptions.MapEnum<MV.DomainLayer.Enums.ProductTypeEnum>(
                            "product_type_enum",
                            schemaName: null,
                            nameTranslator: new Npgsql.NameTranslation.NpgsqlNullNameTranslator());
                    })
                // PERFORMANCE FIX: Set default NoTracking cho read-only queries
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking),
                poolSize: 32);

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            //builder.Services.AddSwaggerGen();

            // Health checks - để monitoring và keep-alive
            builder.Services.AddHealthChecks()
                .AddNpgSql(
                    connectionString: connectionString,
                    name: "postgresql",
                    timeout: TimeSpan.FromSeconds(10),
                    tags: new[] { "db", "sql", "postgresql" });

            var app = builder.Build();

            // Kiểm tra kết nối database khi startup (không tạo/xóa schema)
            // Schema được quản lý bằng database_init_and_seed.sql
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<StemDbContext>();
                if (db.Database.CanConnect())
                {
                    try
                    {
                        db.Database.ExecuteSqlRaw("ALTER TYPE claim_status_enum ADD VALUE IF NOT EXISTS 'UNRESOLVED';");
                        Console.WriteLine("[DB INIT] Added UNRESOLVED to claim_status_enum successfully.");

                        // Bổ sung tạo cột cho OAuth
                        db.Database.ExecuteSqlRaw(@"
                            ALTER TABLE ""USER"" ADD COLUMN IF NOT EXISTS external_provider VARCHAR(20);
                            ALTER TABLE ""USER"" ADD COLUMN IF NOT EXISTS external_id VARCHAR(255);
                            ALTER TABLE ""USER"" ALTER COLUMN password_hash DROP NOT NULL;
                        ");
                        Console.WriteLine("[DB INIT] Added OAuth columns to USER table successfully.");

                        // Bổ sung tạo bảng Notification nếu chưa có
                        db.Database.ExecuteSqlRaw(@"
                            CREATE TABLE IF NOT EXISTS notification (
                                notification_id SERIAL PRIMARY KEY,
                                user_id INT NOT NULL,
                                title VARCHAR(255) NOT NULL,
                                message TEXT NOT NULL,
                                type VARCHAR(50) NOT NULL,
                                is_read BOOLEAN DEFAULT false,
                                link_url VARCHAR(500),
                                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                            );
                        ");
                        Console.WriteLine("[DB INIT] Checked/Created notification table successfully.");

                        // Thêm FK constraint nếu chưa có (align DB với EF model)
                        // Lưu ý: tên bảng user trong DB là "USER" (uppercase)
                        db.Database.ExecuteSqlRaw(@"
                            DO $$
                            BEGIN
                                IF NOT EXISTS (
                                    SELECT 1 FROM information_schema.table_constraints
                                    WHERE constraint_name = 'fk_notification_user'
                                    AND table_name = 'notification'
                                ) THEN
                                    ALTER TABLE notification
                                    ADD CONSTRAINT fk_notification_user
                                    FOREIGN KEY (user_id) REFERENCES ""USER""(user_id)
                                    ON DELETE RESTRICT;
                                END IF;
                            END $$;
                        ");
                        Console.WriteLine("[DB INIT] FK constraint fk_notification_user ensured.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[DB INIT] Note: Could not alter schema: {ex.Message}");
                    }
                }
            }

            // Configure the HTTP request pipeline.
            // Enable Swagger in Development or when EnableSwagger is true (for Docker)
            app.UseSwagger();
            app.UseSwaggerUI();

            // Chỉ dùng HTTPS redirect khi không phải môi trường production trên Render
            // (Render tự xử lý SSL termination, container chạy HTTP)
            if (!app.Environment.IsProduction())
            {
                app.UseHttpsRedirection();
            }

            // Serve static files from wwwroot (product images, etc.)
            var wwwrootPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
            if (!Directory.Exists(wwwrootPath))
                Directory.CreateDirectory(wwwrootPath);
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(wwwrootPath)
            });

            // Enable CORS - must be before Authentication/Authorization
            app.UseCors("AllowFrontend");

            app.UseAuthentication();
            
            app.UseAuthorization();

            app.MapControllers();

            // Health check endpoints
            // /health - simple health check (200 OK if healthy)
            // /health/detail - detailed health check with DB status
            app.MapHealthChecks("/health");
            app.MapHealthChecks("/health/detail", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
            {
                ResponseWriter = async (context, report) =>
                {
                    context.Response.ContentType = "application/json";
                    var result = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        status = report.Status.ToString(),
                        timestamp = DateTimeHelper.VietnamNow(),
                        checks = report.Entries.Select(e => new
                        {
                            name = e.Key,
                            status = e.Value.Status.ToString(),
                            duration = e.Value.Duration,
                            description = e.Value.Description,
                            exception = e.Value.Exception?.Message
                        })
                    });
                    await context.Response.WriteAsync(result);
                }
            });

            // SignalR Hub endpoints
            app.MapHub<NotificationHub>("/hubs/notification");
            app.MapHub<ChatHub>("/hubs/chat");

            app.Run();
        }
    }
}
