using Microsoft.EntityFrameworkCore;
using LegalRag.Api.Models;

namespace LegalRag.Api.Data;

// The "archivist": knows how to reach the database and which tables it has.
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // One DbSet per table. Each property of Document becomes a column.
    public DbSet<Document> Documents { get; set; }
    public DbSet<Law> Laws { get; set; }

    // Rules from docs/phase-0.md, translated to EF Core.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // laws.name: UNIQUE
        modelBuilder.Entity<Law>()
            .HasIndex(l => l.Name)
            .IsUnique();

        // documents.law_id → laws.id, ON DELETE RESTRICT
        modelBuilder.Entity<Document>()
            .HasOne<Law>()
            .WithMany()
            .HasForeignKey(d => d.LawId)
            .OnDelete(DeleteBehavior.Restrict);

        // UNIQUE (law_id, effective_date): the same version cannot be loaded twice
        modelBuilder.Entity<Document>()
            .HasIndex(d => new { d.LawId, d.EffectiveDate })
            .IsUnique();
    }
}