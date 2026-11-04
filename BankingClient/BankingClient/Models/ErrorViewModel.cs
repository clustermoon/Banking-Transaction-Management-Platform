namespace BankingClient.Models;

/// <summary>
/// Provides data for the error view, including the request ID for troubleshooting.
/// </summary>
public class ErrorViewModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
