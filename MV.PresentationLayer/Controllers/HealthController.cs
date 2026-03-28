using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MV.DomainLayer.Helpers;
using MV.InfrastructureLayer.DBContext;
using Swashbuckle.AspNetCore.Annotations;
using System.Diagnostics;

namespace MV.PresentationLayer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly StemDbContext _context;
    private readonly ILogger<HealthController> _logger;

    public HealthController(StemDbContext context, ILogger<HealthController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Simple health check endpoint - trả về 200 nếu app đang chạy.
    /// Dùng cho monitoring services (UptimeRobot, Pingdom, etc.)
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Health Check - Simple status check")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable, Type = typeof(object))]
    public IActionResult GetHealth()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTimeHelper.VietnamNow(),
            service = "PRN232 Backend API"
        });
    }

    /// <summary>
    /// Detailed health check - kiểm tra kết nối database và trả về chi tiết.
    /// Dùng cho debugging và monitoring chi tiết.
    /// </summary>
    [HttpGet("detail")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Health Check Detail - Detailed status with DB check")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable, Type = typeof(object))]
    public async Task<IActionResult> GetHealthDetail()
    {
        var stopwatch = Stopwatch.StartNew();
        var checks = new List<object>();
        var overallHealthy = true;

        // Check 1: Database Connection
        dynamic dbCheck = await CheckDatabaseAsync();
        checks.Add(dbCheck);
        if (dbCheck.status != "Healthy")
        {
            overallHealthy = false;
        }

        stopwatch.Stop();

        var response = new
        {
            status = overallHealthy ? "Healthy" : "Unhealthy",
            timestamp = DateTimeHelper.VietnamNow(),
            totalDuration = $"{stopwatch.ElapsedMilliseconds}ms",
            service = "PRN232 Backend API",
            checks = checks
        };

        if (overallHealthy)
        {
            return Ok(response);
        }
        else
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, response);
        }
    }

    /// <summary>
    /// Ping endpoint - minimal response để giữ app alive.
    /// Chỉ trả về 200 OK, không check database.
    /// </summary>
    [HttpGet("ping")]
    [HttpHead("ping")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Ping - Minimal keep-alive endpoint")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))]
    public IActionResult Ping()
    {
        return Ok(new
        {
            status = "pong",
            timestamp = DateTimeHelper.VietnamNow()
        });
    }

    /// <summary>
    /// Version endpoint - trả về thông tin version của API.
    /// </summary>
    [HttpGet("version")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Version - API version information")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))]
    public IActionResult GetVersion()
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        var buildDate = GetBuildDate(assembly);

        return Ok(new
        {
            version = version?.ToString() ?? "1.0.0",
            buildDate = buildDate,
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
            framework = ".NET 8",
            timestamp = DateTimeHelper.VietnamNow()
        });
    }

    private async Task<object> CheckDatabaseAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            // Try to execute a simple query
            var canConnect = await _context.Database.CanConnectAsync();
            stopwatch.Stop();

            if (canConnect)
            {
                // Get additional DB info
                var connectionString = _context.Database.GetConnectionString();
                var dbName = ExtractDatabaseName(connectionString);

                return new
                {
                    name = "postgresql",
                    status = "Healthy",
                    duration = $"{stopwatch.ElapsedMilliseconds}ms",
                    database = dbName,
                    description = "Database connection successful"
                };
            }
            else
            {
                return new
                {
                    name = "postgresql",
                    status = "Unhealthy",
                    duration = $"{stopwatch.ElapsedMilliseconds}ms",
                    description = "Cannot connect to database"
                };
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Database health check failed");

            return new
            {
                name = "postgresql",
                status = "Unhealthy",
                duration = $"{stopwatch.ElapsedMilliseconds}ms",
                description = "Database connection failed",
                exception = ex.Message
            };
        }
    }

    private string ExtractDatabaseName(string? connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            return "unknown";

        try
        {
            var parts = connectionString.Split(';');
            var dbPart = parts.FirstOrDefault(p => p.Trim().StartsWith("Database=", StringComparison.OrdinalIgnoreCase));
            if (dbPart != null)
            {
                return dbPart.Split('=')[1].Trim();
            }
        }
        catch { }

        return "unknown";
    }

    private DateTime GetBuildDate(System.Reflection.Assembly assembly)
    {
        try
        {
            var attributes = assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false);
            if (attributes.Length > 0 && attributes[0] is System.Reflection.AssemblyInformationalVersionAttribute attribute)
            {
                if (DateTime.TryParse(attribute.InformationalVersion, out var date))
                {
                    return date;
                }
            }
        }
        catch { }

        // Fallback: use file creation time
        try
        {
            return System.IO.File.GetCreationTime(assembly.Location);
        }
        catch
        {
            return DateTimeHelper.VietnamNow();
        }
    }
}
