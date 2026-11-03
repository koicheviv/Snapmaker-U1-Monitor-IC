
using System;
using System.Net.Http;
using System.Text.Json;

namespace SnapmakerU1Monitor;

public class PrinterService
{
    private readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromSeconds(5)
    };

    public string BaseUrl { get; set; } = string.Empty;

    public async Task<JsonDocument> GetPrinterStatusAsync(
        CancellationToken token = default)
    {
        if (!Uri.TryCreate(
                BaseUrl,
                UriKind.Absolute,
                out Uri? baseUri) ||
            (baseUri.Scheme != Uri.UriSchemeHttp &&
             baseUri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException(
                "Printer address is not configured.");
        }

        string url =
            $"{BaseUrl.TrimEnd('/')}/printer/objects/query" +
            "?print_stats&display_status&extruder&heater_bed&webhooks";

        using var response = await _http.GetAsync(url, token);

        response.EnsureSuccessStatusCode();

        string json =
            await response.Content.ReadAsStringAsync(token);

        return JsonDocument.Parse(json);
    }
}
