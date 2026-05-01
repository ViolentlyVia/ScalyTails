namespace ScalyTails.Web.Models;

public class AppSettings
{
    public string ApiKey { get; set; } = "";
    public string Tailnet { get; set; } = "-";
    public bool AdvancedMode { get; set; } = false;
}
