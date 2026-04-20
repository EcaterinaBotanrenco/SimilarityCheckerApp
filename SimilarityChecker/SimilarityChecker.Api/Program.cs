using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SimilarityChecker.Api.Data;
using SimilarityChecker.Api.Services;
using SimilarityChecker.Api.Services.Auth;
using SimilarityChecker.Api.Services.InternalScan;
using SimilarityChecker.Api.Services.Plagiarism;
using SimilarityChecker.Api.Services.TextExtraction;
using SimilarityChecker.Shared.Dto;
using SimilarityChecker.UI.Services.TextExtraction;
using SimilarityChecker.Api.Services.Email;
using System.Text;
using SimilarityChecker.Api.Services.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<SimilarityCheckerDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("SimilarityCheckerDb")));

// Înregistrăm Seeder-ul
builder.Services.AddScoped<Seeder>();

// Configurare JWT
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"]!;
var jwtIssuer = jwtSection["Issuer"]!;
var jwtAudience = jwtSection["Audience"]!;

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero // Zero tolerance pentru expirarea token-ului
        };
    });

// Configurare autorizare pentru Admin
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<JwtTokenService>();

// Servicii pentru extragerea textului din documente
builder.Services.AddSingleton<TextExtractionService>();
builder.Services.AddSingleton<ITextExtractor, PdfTextExtractor>();
builder.Services.AddSingleton<ITextExtractor, DocxTextExtractor>();
builder.Services.AddSingleton<ITextExtractor, TxtTextExtractor>();

// Servicii pentru scanarea internă și plagiat
builder.Services.AddScoped<IInternalScanService, InternalScanService>();
builder.Services.AddSingleton<IPlagiarismService, PlagiarismService>();

// Configurare email
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<IDocumentStorageService, DocumentStorageService>();

var app = builder.Build();

// Apelăm Seeder-ul pentru a atribui roluri
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<Seeder>();
    await seeder.AssignAdminRoleToUsers();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Middleware pentru autentificare și autorizare
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();