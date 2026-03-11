using Microsoft.EntityFrameworkCore;
using SimilarityChecker.Api.Data.Entities;
using DbContext = Microsoft.EntityFrameworkCore.DbContext;

namespace SimilarityChecker.Api.Data;

public sealed class SimilarityCheckerDbContext : DbContext
{
    public SimilarityCheckerDbContext(DbContextOptions<SimilarityCheckerDbContext> options)
        : base(options) { }

    public DbSet<AppUserEntity> AppUsers => Set<AppUserEntity>();
    public DbSet<DocumentEntity> Documents => Set<DocumentEntity>();
    public DbSet<InternalMatchEntity> InternalMatches => Set<InternalMatchEntity>();
    public DbSet<OnlineSourceEntity> OnlineSources => Set<OnlineSourceEntity>();
    public DbSet<SearchQueryEntity> SearchQueries => Set<SearchQueryEntity>();
    public DbSet<SearchResultEntity> SearchResults => Set<SearchResultEntity>();
    public DbSet<MatchEntity> Matches => Set<MatchEntity>();
    public DbSet<ReportEntity> Reports => Set<ReportEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUserEntity>()
            .HasIndex(x => x.Email)
            .IsUnique();

        modelBuilder.Entity<OnlineSourceEntity>()
            .HasIndex(x => x.Url)
            .IsUnique();

        modelBuilder.Entity<DocumentEntity>()
            .HasIndex(x => x.Sha256);

        modelBuilder.Entity<AppUserEntity>()
            .HasMany(x => x.Documents)
            .WithOne(x => x.User)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AppUserEntity>()
            .HasMany(x => x.Reports)
            .WithOne(x => x.User)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SearchQueryEntity>()
            .HasMany(x => x.Results)
            .WithOne(x => x.SearchQuery)
            .HasForeignKey(x => x.SearchQueryId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DocumentEntity>()
            .HasMany(x => x.SearchQueries)
            .WithOne(x => x.Document)
            .HasForeignKey(x => x.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DocumentEntity>()
            .HasMany(x => x.Matches)
            .WithOne(x => x.Document)
            .HasForeignKey(x => x.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OnlineSourceEntity>()
            .HasMany(x => x.Matches)
            .WithOne(x => x.OnlineSource)
            .HasForeignKey(x => x.OnlineSourceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DocumentEntity>()
            .HasMany(x => x.Reports)
            .WithOne(x => x.Document)
            .HasForeignKey(x => x.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<InternalMatchEntity>()
            .HasOne(x => x.Document)
            .WithMany()
            .HasForeignKey(x => x.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<InternalMatchEntity>()
            .HasOne(x => x.ComparedDocument)
            .WithMany()
            .HasForeignKey(x => x.ComparedDocumentId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}