using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace SimilarityChecker.Api.Data;

public sealed class SimilarityCheckerDbContextFactory
    : IDesignTimeDbContextFactory<SimilarityCheckerDbContext>
{
    public SimilarityCheckerDbContext CreateDbContext(string[] args)
    {
        var current = Directory.GetCurrentDirectory();

        // 1) încearcă în directorul curent
        var basePath = current;

        // 2) dacă nu există appsettings.json aici, încearcă în subfolderul SimilarityChecker.Api
        if (!File.Exists(Path.Combine(basePath, "appsettings.json")))
        {
            var apiSubfolder = Path.Combine(current, "SimilarityChecker.Api");
            if (File.Exists(Path.Combine(apiSubfolder, "appsettings.json")))
                basePath = apiSubfolder;
        }

        // dacă tot nu găsim, aruncăm o eroare clară
        var appsettingsPath = Path.Combine(basePath, "appsettings.json");
        if (!File.Exists(appsettingsPath))
        {
            throw new InvalidOperationException(
                $"appsettings.json not found. Checked: '{current}' and '{Path.Combine(current, "SimilarityChecker.Api")}'"
            );
        }

        var config = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var cs = config.GetConnectionString("SimilarityCheckerDb");
        if (string.IsNullOrWhiteSpace(cs))
            throw new InvalidOperationException("ConnectionStrings:SimilarityCheckerDb is missing in appsettings.json");

        var options = new DbContextOptionsBuilder<SimilarityCheckerDbContext>()
            .UseSqlServer(cs)
            .Options;

        return new SimilarityCheckerDbContext(options);
    }
}
