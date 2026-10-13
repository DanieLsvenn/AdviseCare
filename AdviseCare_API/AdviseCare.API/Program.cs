using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Security;
using AdviseCare.Domain.Models;
using AdviseCare.Infrastructure.Repository;
using AdviseCare.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// --- Database (SQL Server) ---
// Enums are stored as NVARCHAR + CHECK in AdviseCareDB, so no native enum mapping is needed here.
// Map enum properties with .HasConversion<string>() in the scaffolded AdviseCareContext.
var connectionString = builder.Configuration.GetConnectionString("DBDefault") ?? throw new InvalidOperationException("Connection string 'DBDefault' was not found.");
builder.Services.AddDbContext<AdviseCareContext>(options => options.UseSqlServer(connectionString));

// --- CORS ---
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("CorsPolicy", policyBuilder =>
    {
        policyBuilder.AllowAnyHeader().AllowAnyMethod().WithOrigins(allowedOrigins);
    });
});

// --- Application services ---
// builder.Services.AddScoped<IAccountService, AccountService>();

// --- Repositories ---
// Registered once for every entity (open generic), so no per-entity line is needed.
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

// --- Security ---
builder.Services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

// -- HttpContextAccessor ---
builder.Services.AddHttpContextAccessor();

// --- Loggers ---
builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.AddConsole();
    loggingBuilder.AddDebug();
});

// --- Background services ---
// builder.Services.AddHostedService<...>();

// --- Controllers + Swagger ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.EnableAnnotations();
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token.\n\nExample:\neyJhbGciOiJIUzI1Ni..."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
    });
});

// --- JWT ---
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

var app = builder.Build();

// --- Global exception handling ---
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = exception switch
        {
            NotFoundException => StatusCodes.Status404NotFound,
            ValidationException => StatusCodes.Status400BadRequest,
            ConflictException => StatusCodes.Status409Conflict,
            ForbiddenException => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError
        };
        await context.Response.WriteAsJsonAsync(new { message = exception is AppException ? exception!.Message : "An unexpected error occurred." });
    });
});

//if (app.Environment.IsDevelopment())
//{
    app.UseSwagger();
    app.UseSwaggerUI();
//}

app.UseCors("CorsPolicy");
app.UseHttpsRedirection();
app.UseAuthentication();

// --- Account status gate (re-checks status on every request so a Ban/Deactivate
// takes effect immediately, instead of waiting for the JWT to expire on its own) ---
// TODO: enable after ACCOUNTS is scaffolded (needs Account + its status enum,
// and "using System.Security.Claims;" at the top).
//app.Use(async (context, next) =>
//{
//    if (context.User.Identity?.IsAuthenticated == true)
//    {
//        var accountIdClaim = context.User.FindFirstValue(ClaimTypes.Sid);
//        if (Guid.TryParse(accountIdClaim, out var accountId))
//        {
//            var accountRepository = context.RequestServices.GetRequiredService<IGenericRepository<Account>>();
//            var account = await accountRepository.GetByIdAsync(accountId);
//
//            if (account == null || account.Status != AccountStatus.Active)
//            {
//                context.Response.ContentType = "application/json";
//                context.Response.StatusCode = StatusCodes.Status403Forbidden;
//                await context.Response.WriteAsJsonAsync(new
//                {
//                    message = account == null
//                        ? "Account not found."
//                        : $"Account is not active. Current status: {account.Status}."
//                });
//                return;
//            }
//        }
//    }
//
//    await next();
//});

app.UseAuthorization();
app.MapControllers();
app.Run();
