namespace Presentation.Models;

public class VerficationServiceResult
{
    public bool Succeeded { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
}
