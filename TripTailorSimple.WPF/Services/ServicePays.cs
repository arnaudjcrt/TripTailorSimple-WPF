using System.Net.Http;
using System.Text.Json;

namespace TripTailorSimple.WPF.Services;

public sealed class ServicePays
{
    private readonly HttpClient _httpClient;

    public ServicePays(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> RecupererDrapeauAsync(string pays, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pays))
            return string.Empty;

        try
        {
            var url = $"https://restcountries.com/v3.1/name/{Uri.EscapeDataString(pays)}";
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var donnees = await JsonSerializer.DeserializeAsync<List<ReponsePays>>(stream, options, cancellationToken);
            return donnees?.FirstOrDefault()?.Flags?.Png ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private sealed class ReponsePays
    {
        public DrapeauxPays? Flags { get; set; }
    }

    private sealed class DrapeauxPays
    {
        public string? Png { get; set; }
    }
}