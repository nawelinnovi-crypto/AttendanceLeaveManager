using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using backend.Data;
using backend.Data.Interfaces;
using backend.Repositories;
using backend.Repositories.Interfaces;
using backend.Services;
using backend.Services.Interfaces;
using backend.Settings;

var builder = WebApplication.CreateBuilder(args);

// Database configuration matching wadheeftiidentity
builder.Services.Configure<AuthenticateDatabaseSettings>(builder.Configuration.GetSection("AuthenticateDatabaseSettings"));
builder.Services.AddSingleton<IAuthenticateDatabaseSettings>(sp =>
    sp.GetRequiredService<IOptions<AuthenticateDatabaseSettings>>().Value);
builder.Services.AddSingleton<AuthenticateDatabaseSettings>(sp =>
    sp.GetRequiredService<IOptions<AuthenticateDatabaseSettings>>().Value);

builder.Services.AddSingleton<IBaseContext, BaseContext>();
builder.Services.AddSingleton(typeof(IMongoRepository<>), typeof(MongoRepository<>));
builder.Services.AddSingleton<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddSingleton<ICompanyRepository, CompanyRepository>();
builder.Services.AddSingleton<IAuthService, AuthService>();

// HttpContext & Multi-Tenant Service
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<ITenantService, TenantService>();

// JWT Authentication configuration
var jwtSecret = builder.Configuration["JwtSettings:Secret"] ?? "AttendanceLeaveManagerSuperSecretKey2026_AttendanceLeaveDb_Key";
var jwtIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "AttendanceLeaveBackend";
var jwtAudience = builder.Configuration["JwtSettings:Audience"] ?? "AttendanceLeaveFrontend";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// Register Data Store Service as Singleton
builder.Services.AddSingleton<IDataStoreService, DataStoreService>();

// Add Controllers
builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Attendance & Leave Manager API", Version = "v1" });
    
    var securityScheme = new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
    };
    c.AddSecurityDefinition("Bearer", securityScheme);

    c.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer"),
            new List<string>()
        }
    });
});

// CORS configuration for Angular frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
              {
                  if (string.IsNullOrEmpty(origin)) return false;
                  try
                  {
                      var uri = new Uri(origin);
                      return uri.Host == "localhost" ||
                             uri.Host == "127.0.0.1" ||
                             uri.Host.EndsWith(".pages.dev", StringComparison.OrdinalIgnoreCase);
                  }
                  catch
                  {
                      return false;
                  }
              })
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure HTTP pipeline
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.DisplayRequestDuration();
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Attendance & Leave Manager API v1");
});

app.UseCors("AllowAngular");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
