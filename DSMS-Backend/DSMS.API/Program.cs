using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using DSMS.API.Data;
using DSMS.API.Services;
using DSMS.API.Middleware;
using Serilog;

// ── Structured File Logging (Serilog) ───────────────────────────────────────
// Logs roll daily → Logs/dsms-YYYY-MM-DD.log, kept for 30 days, max 10 MB/file
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path:                   Path.Combine("Logs", "dsms-.log"),
        rollingInterval:        RollingInterval.Day,
        retainedFileCountLimit: 30,
        fileSizeLimitBytes:     10 * 1024 * 1024,
        rollOnFileSizeLimit:    true,
        outputTemplate:         "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
    )
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Replace default logging with Serilog
builder.Host.UseSerilog();

builder.Services.AddDbContext<DsmsDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"]!;

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
});

builder.Services.AddAuthorization();
builder.Services.AddControllers();

// Allow up to 10 MB multipart uploads for document images
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 10 * 1024 * 1024;
});

// DSMS Services
builder.Services.AddSingleton<ISystemSettingsService, SystemSettingsService>();
builder.Services.AddScoped<IReceiptService, ReceiptService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IWhatsAppService, WhatsAppService>();
builder.Services.AddScoped<IChatbotService, ChatbotService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "DSMS API",
        Version = "v1",
        Description = "Driving School Management System - Arampath Driving School"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization. Enter: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
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

var app = builder.Build();

// ── Startup safety checks ────────────────────────────────────────────────────
var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();

// 1. Ensure uploads folder exists — wrapped so a permission error never kills the app
try
{
    var uploadsDir = Path.Combine(app.Environment.ContentRootPath, "Uploads", "student-docs");
    Directory.CreateDirectory(uploadsDir);
    startupLogger.LogInformation("Uploads directory ready: {Dir}", uploadsDir);
}
catch (Exception ex)
{
    startupLogger.LogWarning("Could not create uploads directory: {Msg}. Document uploads may fail.", ex.Message);
}

// 2. Verify DB connection at startup so failures surface immediately in the log
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<DsmsDbContext>();
    if (await db.Database.CanConnectAsync())
        startupLogger.LogInformation("Database connection OK.");
    else
        startupLogger.LogError("Database connection FAILED. Check SQL Server and connection string.");
}
catch (Exception ex)
{
    startupLogger.LogError(ex, "Database startup check threw an exception. API will run but DB calls will fail.");
}

// 3. Seed default data (roles, branch, admin user) on first run
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<DsmsDbContext>();

    // Seed roles
    if (!await db.Roles.AnyAsync())
    {
        db.Roles.AddRange(
            new DSMS.API.Models.Role { RoleName = "Company Admin", Description = "Company-level administrator", Active = true, CreatedBy = "system", CreatedDateTime = DateTime.Now },
            new DSMS.API.Models.Role { RoleName = "Branch Admin",  Description = "Branch-level administrator",  Active = true, CreatedBy = "system", CreatedDateTime = DateTime.Now },
            new DSMS.API.Models.Role { RoleName = "Staff",         Description = "General staff",               Active = true, CreatedBy = "system", CreatedDateTime = DateTime.Now },
            new DSMS.API.Models.Role { RoleName = "OfficeStaff",   Description = "Office staff",                Active = true, CreatedBy = "system", CreatedDateTime = DateTime.Now },
            new DSMS.API.Models.Role { RoleName = "Instructor",    Description = "Driving instructor",          Active = true, CreatedBy = "system", CreatedDateTime = DateTime.Now },
            new DSMS.API.Models.Role { RoleName = "Admin",         Description = "System administrator",        Active = true, CreatedBy = "system", CreatedDateTime = DateTime.Now }
        );
        await db.SaveChangesAsync();
        startupLogger.LogInformation("Default roles seeded.");
    }

    // Seed default branch
    if (!await db.Branches.AnyAsync())
    {
        db.Branches.Add(new DSMS.API.Models.Branch
        {
            Name    = "Main Branch",
            Code    = "MAIN",
            Address = "Colombo, Sri Lanka",
            Phone   = "0112345678"
        });
        await db.SaveChangesAsync();
        startupLogger.LogInformation("Default branch seeded.");
    }

    // Seed admin user (plain-text password — use /api/auth/setup-admin to BCrypt-hash it)
    if (!await db.UserSecurities.AnyAsync(u => u.UserName == "admin"))
    {
        var adminRole = await db.Roles.FirstOrDefaultAsync(r => r.RoleName == "Company Admin");
        if (adminRole != null)
        {
            db.UserSecurities.Add(new DSMS.API.Models.UserSecurity
            {
                UserName        = "admin",
                Password        = "Admin@1234",   // plain text; call /api/auth/setup-admin to hash
                UserFullName    = "System Admin",
                RoleId          = adminRole.Id,
                BranchId        = null,
                Active          = true,
                FirstTimeLogin  = true,
                CreatedBy       = "system",
                CreatedDateTime = DateTime.Now
            });
            await db.SaveChangesAsync();
            startupLogger.LogInformation("Admin user seeded (username: admin, password: Admin@1234).");
        }
    }
}
catch (Exception ex)
{
    startupLogger.LogError(ex, "Startup data seeding failed — API will still run.");
}

// ── Global Exception Handler — must be FIRST in the pipeline ────────────────
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("AllowAngular");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();