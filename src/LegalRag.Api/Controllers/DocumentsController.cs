using Microsoft.AspNetCore.Mvc;   // controller tools: ControllerBase, Ok(), [HttpGet]
using LegalRag.Api.Data;          // brings in AppDbContext (Data/AppDbContext.cs)
using LegalRag.Api.Models;        // brings in Document (Models/Document.cs)

namespace LegalRag.Api.Controllers;

[ApiController]
[Route("[controller]")]            // → /documents
public class DocumentsController : ControllerBase
{
    private readonly AppDbContext _db;

    // The database session is handed in when the controller is created
    // (dependency injection, registered in Program.cs).
    public DocumentsController(AppDbContext db)
    {
        _db = db;
    }

    // GET /documents
    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok(_db.Documents.ToList());
    }

    // GET /documents/5
    [HttpGet("{id}")]
    public IActionResult GetById(int id)
    {
        var document = _db.Documents.Find(id);   // looks it up by primary key

        if (document == null)
            return NotFound();   // 404: that document does not exist

        return Ok(document);     // 200: here it is
    }

    // POST /documents
    // A missing required field never reaches this method:
    // [ApiController] rejects it with 400 first.
    [HttpPost]
    public IActionResult Create(CreateDocumentRequest request)
    {
        // No id here: PostgreSQL assigns it when the row is saved.
        var document = new Document
        {
            LawId = request.LawId,
            SourceUrl = request.SourceUrl,
            EffectiveDate = request.EffectiveDate,
        };

        _db.Documents.Add(document);
        _db.SaveChanges();   // runs the INSERT; afterwards document.Id holds the new id

        // 201 Created, with a Location header pointing to GET /documents/{id}.
        return CreatedAtAction(nameof(GetById), new { id = document.Id }, document);
    }
}