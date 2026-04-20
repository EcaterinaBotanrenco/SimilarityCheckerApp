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
using System.Net.Http;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<SimilarityCheckerDbContext>(options =>
{
    var cs = builder.Configuration.GetConnectionString("SimilarityCheckerDb");
    options.UseSqlServer(cs);
});

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
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});
builder.Services.AddAuthorization();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddSingleton<TextExtractionService>();
builder.Services.AddSingleton<ITextExtractor, PdfTextExtractor>();
builder.Services.AddSingleton<ITextExtractor, DocxTextExtractor>();
builder.Services.AddSingleton<ITextExtractor, TxtTextExtractor>();
builder.Services.AddScoped<IInternalScanService, InternalScanService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();

builder.Services.AddSingleton<IPlagiarismService, PlagiarismService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
