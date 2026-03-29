using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net.Http;
using System.Text.Json;
using TripTailorSimple.WPF.Models;

namespace TripTailorSimple.WPF.Services;

public sealed class ServiceRechercheLibre
{
    private readonly HttpClient _httpClient;
    private readonly ServiceMeteo _serviceMeteo;
    private readonly ServiceWikipedia _serviceWikipedia;
    private readonly ServicePays _servicePays;
    private readonly ServiceSuggestionsVoyage _serviceSuggestionsVoyage;

    public ServiceRechercheLibre(
        HttpClient httpClient,
        ServiceMeteo serviceMeteo,
        ServiceWikipedia serviceWikipedia,
        ServicePays servicePays,
        ServiceSuggestionsVoyage serviceSuggestionsVoyage)
    {
        _httpClient = httpClient;
        _serviceMeteo = serviceMeteo;
        _serviceWikipedia = serviceWikipedia;
        _servicePays = servicePays;
        _serviceSuggestionsVoyage = serviceSuggestionsVoyage;
    }

    public async Task<PropositionVoyage?> RechercherAsync(string texte, CritereRecherche criteres)
    {
        if (string.IsNullOrWhiteSpace(texte))
            return null;

        var url = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(texte)}&count=1&language=fr&format=json";

        using var response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
            return null;

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);

        if (!doc.RootElement.TryGetProperty("results", out var results) || results.GetArrayLength() == 0)
            return null;

        var r = results[0];

        string ville = r.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? texte : texte;
        string pays = r.TryGetProperty("country", out var countryEl) ? countryEl.GetString() ?? "" : "";
        string region = r.TryGetProperty("admin1", out var adminEl) ? adminEl.GetString() ?? pays : pays;
        double latitude = r.TryGetProperty("latitude", out var latEl) ? latEl.GetDouble() : 0;
        double longitude = r.TryGetProperty("longitude", out var lonEl) ? lonEl.GetDouble() : 0;

        var destination = new DestinationBrute
        {
            Cle = $"{ville}-{pays}".ToLowerInvariant().Replace(" ", "-"),
            Ville = ville,
            Pays = pays,
            Region = string.IsNullOrWhiteSpace(region) ? pays : region,
            Climat = criteres.Climat,
            Latitude = latitude,
            Longitude = longitude,
            PrixVolBase = BasePrixVol(criteres.StyleVoyage),
            PrixHotelParNuitBase = BasePrixHotel(criteres.StyleVoyage),
            PrixActivitesBase = BasePrixActivites(criteres.StyleVoyage),
            TemperatureMoyenne = 20,
            Etiquettes = new List<string> { "Découverte", "Voyage", "Sur mesure" }
        };

        double temperature = await _serviceMeteo.RecupererTemperatureMoyenneAsync(latitude, longitude);
        if (double.IsNaN(temperature))
            temperature = 20;

        int prixVol = AjusterPrixVol(destination.PrixVolBase, criteres.StyleVoyage);
        int prixHotel = destination.PrixHotelParNuitBase * criteres.NombreJours;
        int prixActivites = AjusterPrixActivites(destination.PrixActivitesBase, criteres.StyleVoyage);
        int prixTotal = prixVol + prixHotel + prixActivites;

        var resume = await _serviceWikipedia.RecupererResumeAsync(ville);
        var drapeau = await _servicePays.RecupererDrapeauAsync(pays);

        return new PropositionVoyage
        {
            Cle = destination.Cle,
            Ville = ville,
            Pays = pays,
            Region = destination.Region,
            Description = string.IsNullOrWhiteSpace(resume.Description)
                ? $"Destination trouvée via la recherche libre : {ville}, {pays}."
                : resume.Description,
            UrlImage = string.IsNullOrWhiteSpace(resume.ImageUrl)
                ? $"https://placehold.co/1200x700?text={Uri.EscapeDataString(ville)}"
                : resume.ImageUrl,
            UrlDrapeau = drapeau,
            PrixVol = prixVol,
            PrixHotel = prixHotel,
            PrixActivites = prixActivites,
            PrixTotal = prixTotal,
            TemperatureMoyenne = temperature,
            Latitude = latitude,
            Longitude = longitude,
            UrlVol = $"https://www.google.com/travel/flights?q={Uri.EscapeDataString(ville)}",
            UrlHotel = $"https://www.google.com/travel/hotels/{Uri.EscapeDataString(ville)}",
            UrlActivites = $"https://www.google.com/search?q={Uri.EscapeDataString($"activités {ville}")}",
            Etiquettes = new List<string> { "Recherche libre", criteres.StyleVoyage, criteres.Climat },
            Activites = _serviceSuggestionsVoyage.GenererActivites(destination),
            IdeesGratuites = _serviceSuggestionsVoyage.GenererIdeesGratuites(destination),
            Itineraire = _serviceSuggestionsVoyage.GenererItineraire(destination, criteres.NombreJours),
            NombreJours = criteres.NombreJours,
            CompagnieVol = SuggereCompagnie(pays),
            NomHotel = SuggereHotel(ville, criteres.StyleVoyage),
            StyleVoyageAffiche = criteres.StyleVoyage,
            Score = 100
        };
    }

    private static int BasePrixVol(string style) => style switch
    {
        "Luxe" => 550,
        "Économique" => 180,
        _ => 320
    };

    private static int BasePrixHotel(string style) => style switch
    {
        "Luxe" => 180,
        "Économique" => 55,
        _ => 95
    };

    private static int BasePrixActivites(string style) => style switch
    {
        "Luxe" => 220,
        "Économique" => 70,
        _ => 130
    };

    private static int AjusterPrixVol(int basePrice, string style) => style switch
    {
        "Luxe" => (int)(basePrice * 1.2),
        "Économique" => (int)(basePrice * 0.9),
        _ => basePrice
    };

    private static int AjusterPrixActivites(int basePrice, string style) => style switch
    {
        "Luxe" => (int)(basePrice * 1.4),
        "Économique" => (int)(basePrice * 0.8),
        _ => basePrice
    };

    private static string SuggereCompagnie(string pays)
    {
        if (pays.Contains("Japon", StringComparison.OrdinalIgnoreCase)) return "ANA";
        if (pays.Contains("France", StringComparison.OrdinalIgnoreCase)) return "Air France";
        if (pays.Contains("Brésil", StringComparison.OrdinalIgnoreCase)) return "LATAM";
        return "Compagnie partenaire";
    }

    private static string SuggereHotel(string ville, string style)
    {
        return style switch
        {
            "Luxe" => $"Grand Hotel {ville} 5★",
            "Économique" => $"City Budget Hotel {ville} 3★",
            _ => $"Central Comfort Hotel {ville} 4★"
        };
    }
}