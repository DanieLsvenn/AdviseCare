using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Email;
using AdviseCare.Application.Interfaces.Excel;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Security;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Application.Interfaces.Storage;
using AdviseCare.Application.Services;
using AdviseCare.API.BackgroundServices;
using AdviseCare.Domain.Models;
using AdviseCare.Infrastructure.Configuration;
using AdviseCare.Infrastructure.Email;
using AdviseCare.Infrastructure.Excel;
using AdviseCare.Infrastructure.Repository;
using AdviseCare.Infrastructure.Security;
using AdviseCare.Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// --- Database (SQL Server) ---
// Enums are stored as NVARCHAR + CHECK in AdviseCareDB and stay as string in code, so no enum mapping is needed here.
var connectionString = builder.Configuration.GetConnectionString("AdviseCareDB") ?? throw new InvalidOperationException("Connection string 'AdviseCareDB' was not found.");
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
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<IActivityLogService, ActivityLogService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<INotificationPreferenceService, NotificationPreferenceService>();
builder.Services.AddScoped<IFacultyService, FacultyService>();
builder.Services.AddScoped<IProgrammeService, ProgrammeService>();
builder.Services.AddScoped<IAcademicTermService, AcademicTermService>();
builder.Services.AddScoped<ISubjectService, SubjectService>();
builder.Services.AddScoped<IExternalServiceService, ExternalServiceService>();
builder.Services.AddScoped<IExternalReferralService, ExternalReferralService>();
builder.Services.AddScoped<IEscalationService, EscalationService>();
builder.Services.AddScoped<IInterventionService, InterventionService>();
builder.Services.AddScoped<ISupportGrantService, SupportGrantService>();
builder.Services.AddScoped<IStudentIndicatorImportService, StudentIndicatorImportService>();

// Member B
builder.Services.AddScoped<IIndicatorService, IndicatorService>();
builder.Services.AddScoped<IConsentPurposeService, ConsentPurposeService>();
builder.Services.AddScoped<IAccessPolicyService, AccessPolicyService>();
builder.Services.AddScoped<IAccessGuardService, AccessGuardService>();
builder.Services.AddScoped<IConsentService, ConsentService>();
builder.Services.AddScoped<IConsentRequestService, ConsentRequestService>();
builder.Services.AddScoped<IAccessLogService, AccessLogService>();
builder.Services.AddScoped<IErasureRequestService, ErasureRequestService>();
builder.Services.AddScoped<IEmergencyAccessRequestService, EmergencyAccessRequestService>();

// --- Repositories ---
// Registered once for every entity (open generic), so no per-entity line is needed.
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IErasureWorkflowRepository, ErasureWorkflowRepository>();

// --- Security ---
builder.Services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

// --- Email (Gmail SMTP), storage (Supabase) and Excel import/export ---
builder.Services.Configure<GmailSettings>(builder.Configuration.GetSection("Gmail"));
builder.Services.Configure<SupabaseSettings>(builder.Configuration.GetSection("Supabase"));
builder.Services.Configure<VaultTransitSettings>(builder.Configuration.GetSection("VaultTransit"));
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddHttpClient<IStorageService, SupabaseStorageService>();
builder.Services.AddHttpClient<IKeyManagementService, VaultTransitKeyManagementService>()
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AllowAutoRedirect = false });
builder.Services.AddScoped<IExcelReader, ExcelReader>();
builder.Services.AddScoped<ICsvReader, CsvReader>();
builder.Services.AddScoped<IExcelWriter, ExcelWriter>();

// -- HttpContextAccessor ---
builder.Services.AddHttpContextAccessor();

// --- Loggers ---
builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.AddConsole();
    loggingBuilder.AddDebug();
});

// --- Background services ---
builder.Services.AddHostedService<ExternalReferralOverdueService>();

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
            UnauthorizedException => StatusCodes.Status401Unauthorized,
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

// --- Account status + session gate ---
// Re-checks on every authenticated request, so a Ban/Deactivate, a Logout or a password change takes effect
// immediately instead of waiting for the JWT to expire on its own, and ends sessions older than 1 month.
// Skipped for [AllowAnonymous] endpoints (e.g. login), so a stale token left in the browser can never block signing in again.
app.Use(async (context, next) =>
{
    var isAnonymousEndpoint = context.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() != null;
    if (context.User.Identity?.IsAuthenticated == true && !isAnonymousEndpoint)
    {
        var accountIdClaim = context.User.FindFirstValue(ClaimTypes.Sid);
        var sessionIdClaim = context.User.FindFirstValue("sessionId");
        if (Guid.TryParse(accountIdClaim, out var accountId) && Guid.TryParse(sessionIdClaim, out var sessionId))
        {
            var accountRepository = context.RequestServices.GetRequiredService<IGenericRepository<Account>>();
            var sessionRepository = context.RequestServices.GetRequiredService<IGenericRepository<Session>>();
            var account = await accountRepository.GetByIdAsync(accountId);
            var session = await sessionRepository.GetByIdAsync(sessionId);

            if (account == null || account.Status != "active")
            {
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new
                {
                    message = account == null
                        ? "Account not found."
                        : $"Account is not active. Current status: {account.Status}."
                });
                return;
            }

            // A session ends on its own 1 month after sign-in
            if (session != null && session.IsActive && session.LoginAt.AddDays(SessionService.LifetimeDays) <= DateTime.UtcNow)
            {
                session.IsActive = false;
                session.LogoutAt = session.LoginAt.AddDays(SessionService.LifetimeDays);
                await sessionRepository.UpdateAsync(session);
            }

            if (session == null || session.AccountId != accountId || !session.IsActive)
            {
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { message = "Session has ended. Please log in again." });
                return;
            }
        }
        else
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { message = "Invalid token." });
            return;
        }
    }

    await next();
});

app.UseAuthorization();
app.MapControllers();
app.Run();
