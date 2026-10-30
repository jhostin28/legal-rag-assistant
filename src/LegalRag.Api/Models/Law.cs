namespace LegalRag.Api.Models;

// One law, stored once (e.g. Código de Trabajo). Its versions are Documents.
// Design decisions: docs/phase-0.md
public class Law
{
    public int Id { get; set; }
    public required string Name { get; set; }
    
}

