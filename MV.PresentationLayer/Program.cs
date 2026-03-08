using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MV.ApplicationLayer.Interfaces;
using MV.ApplicationLayer.Services;
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
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy.WithOrigins(
                            "http://localhost:5173",  
                            "http://localhost:3000",  
                            "http://127.0.0.1:5173",
                            "http://localhost:5174",
                            "http://localhost:5175",
                            "http://127.0.0.1:3000",
                            "http://localhost:5255",  // Swagger Docker
                            "http://127.0.0.1:5255"
                        )
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();
                });
            });

            // Configure Swagger
            builder.Services.AddSwaggerGen(c =>
            {
                c.EnableAnnotations();
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
            builder.Services.AddScoped<IProductRepository, ProductRepository>();
            builder.Services.AddScoped<IProductImageRepository, ProductImageRepository>();
            builder.Services.AddScoped<IProductBundleRepository, ProductBundleRepository>();
            builder.Services.AddScoped<IWarrantyRepository, WarrantyRepository>();
            builder.Services.AddScoped<IWarrantyClaimRepository, WarrantyClaimRepository>();
            builder.Services.AddScoped<IOrderRepository, OrderRepository>();
            builder.Services.AddScoped<ISepayRepository, SepayRepository>();
            builder.Services.AddScoped<IReviewRepository, ReviewRepository>();

            builder.Services.AddScoped<IRoleService, RoleService>();
            builder.Services.AddScoped<IOrderService, OrderService>();
            builder.Services.AddScoped<IPaymentService, PaymentService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IProductService, ProductService>();
            builder.Services.AddScoped<ICartService, CartService>();
            builder.Services.AddScoped<IBrandService, BrandService>();
            builder.Services.AddScoped<ICategoryService, CategoryService>();
            builder.Services.AddScoped<IProductService, ProductService>();
            builder.Services.AddScoped<IProductImageService, ProductImageService>();
            builder.Services.AddScoped<IProductBundleService, ProductBundleService>();
            builder.Services.AddScoped<IWarrantyService, WarrantyService>();
            builder.Services.AddScoped<IWarrantyClaimService, WarrantyClaimService>();
            builder.Services.AddScoped<ICheckoutService, CheckoutService>();
            builder.Services.AddScoped<IAdminProductService, AdminProductService>();
            builder.Services.AddScoped<IAdminOrderService, AdminOrderService>();
            builder.Services.AddScoped<IReviewService, ReviewService>();

            // Cloudinary
            builder.Services.AddSingleton<ICloudinaryService, CloudinaryService>();

            // SignalR + Realtime Notification
            builder.Services.AddSignalR();
            builder.Services.AddSingleton<INotificationService, SignalRNotificationService>();

            // Background service: auto-expire overdue SEPAY payments every 60 seconds
            builder.Services.AddHostedService<PaymentExpiryBackgroundService>();

            // Background service: polling SePay API (bật cho local dev)
            builder.Services.AddHostedService<SepayPollingBackgroundService>();

            // Prevent background service exceptions from crashing the host
            builder.Services.Configure<HostOptions>(options =>
            {
                options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
            });

            // Register DbContext with connection string from appsettings
            // ConfigureWarnings is used to suppress ManyServiceProvidersCreatedWarning which causes 500 errors
            builder.Services.AddDbContext<StemDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
                    npgsqlOptions =>
                    {
                        npgsqlOptions.MigrationsAssembly("MV.InfrastructureLayer");
                        npgsqlOptions.MapEnum<MV.DomainLayer.Enums.ProductTypeEnum>(
                            "product_type_enum",
                            schemaName: null,
                            nameTranslator: new Npgsql.NameTranslation.NpgsqlNullNameTranslator());
                    })
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ManyServiceProvidersCreatedWarning)));

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            //builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            // Enable Swagger in Development or when EnableSwagger is true (for Docker)
            var enableSwagger = app.Configuration.GetValue<bool>("EnableSwagger", false);
            if (app.Environment.IsDevelopment() || enableSwagger)
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

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

            // SignalR Hub endpoints
            app.MapHub<NotificationHub>("/hubs/notification");
            app.MapHub<ChatHub>("/hubs/chat");

            app.Run();
        }
    }
}
