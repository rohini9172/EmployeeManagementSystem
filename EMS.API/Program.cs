using System.Text;
using EMS.API.Data;
using EMS.API.Middleware;
using EMS.API.Repositories;
using EMS.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// ── 1. Database ────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── 2. Redis Connection ────────────────────────────────────────────────────
// ConnectionMultiplexer is registered as a SINGLETON — one TCP connection shared
// across all requests. Creating a new connection per request is expensive and wrong.
// StackExchange.Redis is designed to be used this way.
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var connStr = config["Redis:ConnectionString"] ?? "localhost:6379";
    var logger = sp.GetRequiredService<ILogger<Program>>();

    try
    {
        var options = ConfigurationOptions.Parse(connStr);
        // abortConnect=false means the app starts even if Redis is unavailable.
        // Without this, the app would crash on startup if Redis is down.
        options.AbortOnConnectFail = false;
        options.ConnectTimeout = 3000;  // 3 second connection timeout
        options.SyncTimeout = 3000;
        return ConnectionMultiplexer.Connect(options);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to connect to Redis at {ConnStr}", connStr);
        throw;
    }
});

// RedisService is Singleton too — it holds a reference to the singleton multiplexer.
// All Redis operations go through this service.
builder.Services.AddSingleton<IRedisService, RedisService>();

// ── 3. Dependency Injection — Repositories and Services ───────────────────
// AddScoped: one instance per HTTP request (correct for DB-backed services)
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IActivityService, ActivityService>();

// ── 3. JWT Authentication ──────────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is not configured.");

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
            // The signing key must match what was used to create the token
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };

        // Return JSON error responses instead of empty 401/403 bodies
        // Without this, the browser gets an empty response which is hard to debug
        options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse(); // Suppress default redirect behavior
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("{\"message\":\"Unauthorized. Please log in.\"}" );
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = 403;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("{\"message\":\"Forbidden. You do not have permission.\"}" );
            }
        };
    });

builder.Services.AddAuthorization();

// ── 4. Controllers ─────────────────────────────────────────────────────────
builder.Services.AddControllers();

// ── 5. Swagger / OpenAPI ───────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "EMS API",
        Version = "v1",
        Description = "Employee Management System API with Redis caching"
    });

    // Add JWT Bearer support to Swagger UI so you can test protected endpoints
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token. Example: eyJhbGci..."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ── 6. CORS — allow React dev server ──────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactApp", policy =>
        policy.WithOrigins("http://localhost:5173", "http://localhost:5013") // Vite + API ports
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

// ── 7. Global Exception Middleware (must be first in pipeline) ─────────────
app.UseMiddleware<ExceptionMiddleware>();

// ── 8. Swagger UI ──────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "EMS API v1");
        c.RoutePrefix = string.Empty; // Swagger UI at root: http://localhost:5000
    });
}

app.UseHttpsRedirection();
app.UseCors("ReactApp");

// Authentication must come before Authorization -- Executes Authorization and Authentication Checks during incoming request
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ── 9. Run DB migrations and seed data on startup ─────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    await DbSeeder.SeedAsync(db, config);
}

app.Run();
