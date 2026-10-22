namespace LegalRag.Api.Models;

// One version of a law. Design decisions: docs/phase-0.md
public class Document
{
    public int Id { get; set; }

    // Versions are grouped by id, not by name, so a typo in a title
    // cannot create a different law.
    public int LawId { get; set; }

    // Link to the original, so any citation can be verified.
    public required string SourceUrl { get; set; }
    // The current version is the latest one on or before today.
    public DateOnly EffectiveDate { get; set; }
}