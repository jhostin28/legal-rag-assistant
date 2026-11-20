using Microsoft.AspNetCore.Mvc;
using LegalRag.Api.Data;
using LegalRag.Api.Models;

namespace LegalRag.Api.Controllers;

[ApiController]
[Route("[controller]")]            // → /laws
public class LawsController : ControllerBase
{
    private readonly AppDbContext _db;

    public LawsController(AppDbContext db)
    {
        _db = db;
    }

    // GET /laws
    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok(_db.Laws.ToList());
    }

    // POST /laws
    [HttpPost]
    public IActionResult Create(CreateLawRequest request)
    {
        // Each law is stored once: a repeated name is a conflict (409), not a server failure (500).
        if (_db.Laws.Any(l => l.Name == request.Name))                                  
            return Conflict($"Law {request.Name} already exist.");                             

        var law = new Law { Name = request.Name };

        _db.Laws.Add(law);                                    
        _db.SaveChanges();

        return Created($"/laws/{law.Id}", law);
    }
}