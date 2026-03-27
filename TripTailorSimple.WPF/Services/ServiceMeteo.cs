using System.Net.Http;
using System.Text.Json;
using TripTailorSimple.WPF.Modeles;
using TripTailorSimple.WPF.Models;

namespace TripTailorSimple.WPF.Services;

public sealed class ServiceMeteo
{
    private readonly HttpClient _httpClient;

    public ServiceMeteo(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<double> RecupererTemperatureMoyenneAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url =
                $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&daily=temperature_2m_max,temperature_2m_min&timezone=auto&forecast_days=7";

            using var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            var daily = document.RootElement.GetProperty("daily");
            var maxTemps = daily.GetProperty("temperature_2m_max").EnumerateArray().Select(x => x.GetDouble()).ToList();
            var minTemps = daily.GetProperty("temperature_2m_min").EnumerateArray().Select(x => x.GetDouble()).ToList();

            if (maxTemps.Count == 0 || minTemps.Count == 0)
                return double.NaN;

            var moyenne = maxTemps.Zip(minTemps, (max, min) => (max + min) / 2.0).Average();
            return Math.Round(moyenne, 1);
        }
        catch
        {
            return double.NaN;
        }
    }

    public async Task<List<PrevisionJournaliere>> RecupererPrevisionsJournalieresAsync(
        double latitude,
        double longitude,
        int nombreJours,
        CancellationToken cancellationToken = default)
    {
        var resultat = new List<PrevisionJournaliere>();

        try
        {
            var jours = Math.Clamp(nombreJours, 1, 10);

            var url =
                $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&daily=temperature_2m_max,temperature_2m_min,weather_code&timezone=auto&forecast_days={jours}";

            using var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            var daily = document.RootElement.GetProperty("daily");

            var dates = daily.GetProperty("time").EnumerateArray().Select(x => x.GetString() ?? "").ToList();
            var maxTemps = daily.GetProperty("temperature_2m_max").EnumerateArray().Select(x => x.GetDouble()).ToList();
            var minTemps = daily.GetProperty("temperature_2m_min").EnumerateArray().Select(x => x.GetDouble()).ToList();
            var codes = daily.GetProperty("weather_code").EnumerateArray().Select(x => x.GetInt32()).ToList();

            for (int i = 0; i < dates.Count && i < maxTemps.Count && i < minTemps.Count && i < codes.Count; i++)
            {
                resultat.Add(new PrevisionJournaliere
                {
                    Jour = dates[i],
                    Temperature = Math.Round((maxTemps[i] + minTemps[i]) / 2.0, 1),
                    ResumeMeteo = MapperCodeMeteo(codes[i])
                });
            }
        }
        catch
        {
        }

        return resultat;
    }

    private static string MapperCodeMeteo(int code)
    {
        return code switch
        {
            0 => "Ensoleillé",
            1 or 2 or 3 => "Partiellement nuageux",
            45 or 48 => "Brouillard",
            51 or 53 or 55 or 61 or 63 or 65 => "Pluie",
            71 or 73 or 75 => "Neige",
            80 or 81 or 82 => "Averses",
            95 or 96 or 99 => "Orage",
            _ => "Variable"
        };
    }
}