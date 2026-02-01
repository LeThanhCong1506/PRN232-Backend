using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MV.ApplicationLayer.Interfaces;
using MV.ApplicationLayer.Services;
using MV.DomainLayer.Interfaces;
using MV.InfrastructureLayer.DBContext;
using MV.InfrastructureLayer.Interfaces;
using MV.InfrastructureLayer.Repositories;
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
                            "http://127.0.0.1:3000"
                        )
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();
                });
            });

            // Configure JWT Authentication
            var jwtSettings = builder.Configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");


            builder.Services.AddSwaggerGen(c =>
            builder.Services.AddAuthentication(options =>
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
            }));


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
});

            builder.Services.AddAuthorization();

            builder.Services.AddScoped<IJwtService, JwtService>();

            // Add services to the container.
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
                });

            builder.Services.AddScoped<IRoleRepository, RoleRepository>();
            builder.Services.AddScoped<IRoleService, RoleService>();
            builder.Services.AddScoped<IProductRepository, ProductRepository>();
            builder.Services.AddScoped<ICartRepository, CartRepository>();
            builder.Services.AddScoped<ICouponRepository, CouponRepository>();

            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IProductService, ProductService>();
            builder.Services.AddScoped<ICartService, CartService>();

            builder.Services.AddScoped<IBrandRepository, BrandRepository>();
            builder.Services.AddScoped<IBrandService, BrandService>();

            builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
            builder.Services.AddScoped<ICategoryService, CategoryService>();

            builder.Services.AddScoped<IProductRepository, ProductRepository>();
            builder.Services.AddScoped<IProductService, ProductService>();

            builder.Services.AddScoped<IProductImageRepository, ProductImageRepository>();
            builder.Services.AddScoped<IProductImageService, ProductImageService>();

            builder.Services.AddScoped<IProductBundleRepository, ProductBundleRepository>();
            builder.Services.AddScoped<IProductBundleService, ProductBundleService>();

            builder.Services.AddScoped<IWarrantyRepository, WarrantyRepository>();
            builder.Services.AddScoped<IWarrantyService, WarrantyService>();

            // Register Npgsql enum mappings BEFORE DbContext
            Npgsql.NpgsqlConnection.GlobalTypeMapper.MapEnum<MV.DomainLayer.Enums.ProductTypeEnum>(
                "product_type_enum",
                new Npgsql.NameTranslation.NpgsqlNullNameTranslator());

            // Register DbContext with connection string from appsettings
            builder.Services.AddDbContext<StemDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
                    npgsqlOptions =>
                    {
                        npgsqlOptions.MigrationsAssembly("MV.InfrastructureLayer");
                        npgsqlOptions.MapEnum<MV.DomainLayer.Enums.ProductTypeEnum>(
                            "product_type_enum",
                            schemaName: null,
                            nameTranslator: new Npgsql.NameTranslation.NpgsqlNullNameTranslator());
                    }));

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

            // Enable CORS - must be before Authentication/Authorization
            app.UseCors("AllowFrontend");

            app.UseAuthentication();
            
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
