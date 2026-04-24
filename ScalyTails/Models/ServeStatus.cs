using System.Text.Json.Serialization;

namespace ScalyTails.Models;

public class ServeStatus
{
    [JsonPropertyName("TCP")]
    public Dictionary<string, TcpPortHandler>? TCP { get; set; }

    [JsonPropertyName("Web")]
    public Dictionary<string, WebServerConfig>? Web { get; set; }

    [JsonPropertyName("AllowFunnel")]
    public Dictionary<string, bool>? AllowFunnel { get; set; }

    public bool IsEmpty =>
        (TCP is null || TCP.Count == 0) &&
        (Web is null || Web.Count == 0);

    public IReadOnlyList<ServeEntry> ToEntries()
    {
        var entries = new List<ServeEntry>();

        if (Web != null)
        {
            foreach (var (hostPort, webConfig) in Web)
            {
                var port = hostPort.Contains(':')
                    ? hostPort[(hostPort.LastIndexOf(':') + 1)..]
                    : "443";
                var isFunnel = AllowFunnel?.ContainsKey(hostPort) == true;

                if (webConfig.Handlers != null)
                {
                    foreach (var (path, handler) in webConfig.Handlers)
                    {
                        entries.Add(new ServeEntry
                        {
                            Protocol = isFunnel ? "HTTPS Funnel" : "HTTPS",
                            Port = port,
                            Path = path,
                            Target = handler.Proxy ?? handler.Path ?? handler.Text ?? "",
                            FunnelEnabled = isFunnel,
                            HostPort = hostPort,
                        });
                    }
                }
            }
        }

        if (TCP != null)
        {
            foreach (var (port, handler) in TCP)
            {
                entries.Add(new ServeEntry
                {
                    Protocol = "TCP",
                    Port = port,
                    Path = "",
                    Target = handler.TCPForward ?? "",
                    FunnelEnabled = false,
                    HostPort = $":{port}",
                });
            }
        }

        return entries;
    }
}

public class TcpPortHandler
{
    [JsonPropertyName("HTTPS")]
    public bool HTTPS { get; set; }

    [JsonPropertyName("TCPForward")]
    public string? TCPForward { get; set; }
}

public class WebServerConfig
{
    [JsonPropertyName("Handlers")]
    public Dictionary<string, HttpHandler>? Handlers { get; set; }
}

public class HttpHandler
{
    [JsonPropertyName("Proxy")]
    public string? Proxy { get; set; }

    [JsonPropertyName("Path")]
    public string? Path { get; set; }

    [JsonPropertyName("Text")]
    public string? Text { get; set; }
}

public class ServeEntry
{
    public string Protocol { get; set; } = "";
    public string Port { get; set; } = "";
    public string Path { get; set; } = "";
    public string Target { get; set; } = "";
    public bool FunnelEnabled { get; set; }
    public string HostPort { get; set; } = "";
}
