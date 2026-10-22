namespace TalitaKumiWebApp.Models;

public class StudioSettings
{
    public string StudioName { get; set; } = "Talita Kumi - Estudio de Belleza";
    public string WhatsAppNumber { get; set; } = "51986073098";
    public string WhatsAppFormatted { get; set; } = "+51 986 073 098";

    private string? _instagramHandle;
    public string InstagramHandle
    {
        get => _instagramHandle ?? "ttalita_kumi";
        set => _instagramHandle = value?.Trim();
    }

    private string? _instagramUrl;
    public string InstagramUrl
    {
        get => !string.IsNullOrWhiteSpace(_instagramUrl) ? _instagramUrl : $"https://instagram.com/{InstagramHandle}";
        set => _instagramUrl = value;
    }

    private string? _tikTokHandle;
    public string TikTokHandle
    {
        get => _tikTokHandle ?? "ttalita_kumi";
        set => _tikTokHandle = value?.Trim().TrimStart('@');
    }

    private string? _tikTokUrl;
    public string TikTokUrl
    {
        get => !string.IsNullOrWhiteSpace(_tikTokUrl) ? _tikTokUrl : $"https://www.tiktok.com/@{TikTokHandle}";
        set => _tikTokUrl = value;
    }

    public string WhatsAppUrl => $"https://wa.me/{WhatsAppNumber}";
}
