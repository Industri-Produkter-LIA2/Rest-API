namespace IPShop.Api.Models.Constants;

public static class OrderStatuses
{
    public const string Behandlas = "Behandlas";
    public const string Levereras = "Levereras";
    public const string Levererad = "Levererad";
    public const string Fakturerad = "Fakturerad";

    public static readonly string[] All =
    [
        Behandlas,
        Levereras,
        Levererad,
        Fakturerad
    ];

    public static bool IsValid(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return false;
        }

        return All.Contains(status.Trim(), StringComparer.OrdinalIgnoreCase);
    }
}
