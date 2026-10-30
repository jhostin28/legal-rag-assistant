using Microsoft.EntityFrameworkCore;
using LegalRag.Api.Models;

namespace LegalRag.Api.Data;

// The "archivist": knows how to reach the database and which tables it has.
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // One DbSet per table. Each property of Document becomes a column.
    public DbSet<Document> Documents { get; set; }
}