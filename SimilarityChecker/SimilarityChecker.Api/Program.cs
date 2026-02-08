using Microsoft.EntityFrameworkCore;
using SimilarityChecker.Api.Data;
using SimilarityChecker.Api.Services.Plagiarism;
using SimilarityChecker.Api.Services.TextExtraction;
using SimilarityChecker.UI.Services;
using SimilarityChecker.UI.Services.TextExtraction;

var builder = WebApplication.CreateBuilder(args);

// ===== Controllers (MVC) =====
builder.Services.AddControllers();

// ===== Swagger =====
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<SimilarityCheckerDbContext>(options =>
{
    var cs = builder.Configuration.GetConnectionString("SimilarityCheckerDb");
    options.UseSqlServer(cs);
});

// ===== DI: Text extraction =====
builder.Services.AddSingleton<TextExtractionService>();
builder.Services.AddSingleton<ITextExtractor, PdfTextExtractor>();
builder.Services.AddSingleton<ITextExtractor, DocxTextExtractor>();
builder.Services.AddSingleton<ITextExtractor, TxtTextExtractor>();

// ===== DI: Plagiarism =====
builder.Services.AddSingleton<IPlagiarismService, PlagiarismService>();

var app = builder.Build();

// ===== HTTP pipeline =====
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Dacă vei folosi auth în API mai târziu, aici vor veni:
// app.UseAuthentication();
app.UseAuthorization();

// IMPORTANT: map controllers
app.MapControllers();

app.Run();
