using Microsoft.AspNetCore.Mvc;   // controller tools: ControllerBase, Ok(), [HttpGet]
using LegalRag.Api.Models;        // brings in Document (Models/Document.cs)

namespace LegalRag.Api.Controllers;

[ApiController]
[Route("[controller]")]            // → /documents
public class DocumentsController : ControllerBase
{
    // Temporary data, until PostgreSQL is connected later in this phase.
    private static readonly List<Document> Documents =
    [
        new Document { Id = 1, LawId = 1, SourceUrl = "https://example.com/codigo-2020.pdf", EffectiveDate = new DateOnly(2020, 1, 1) },
        new Document { Id = 2, LawId = 1, SourceUrl = "https://example.com/codigo-2026.pdf", EffectiveDate = new DateOnly(2026, 3, 1) },
    ];

    // GET /documents
    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok(Documents);
    }

    // GET /documents/5
    [HttpGet("{id}")]
    public IActionResult GetById(int id)
    {
        var document = Documents.Find(d => d.Id == id);

        if (document == null)
            return NotFound();   // 404: that document does not exist

        return Ok(document);     // 200: here it is
    }
}