namespace LegalRag.Api.Models;

// The form the client fills in to create a document.
// No Id: the server assigns it, so two clients can never pick the same one.
public class CreateDocumentRequest
{
    public required int LawId { get; set; }
    public required string SourceUrl { get; set; }
    public required DateOnly EffectiveDate { get; set; }
}
