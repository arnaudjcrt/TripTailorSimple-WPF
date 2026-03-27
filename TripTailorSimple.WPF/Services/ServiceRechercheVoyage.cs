using TripTailorSimple.WPF.Models;

namespace TripTailorSimple.WPF.Services;

public class ServiceRechercheVoyage
{
    private readonly ServiceDestinations _serviceDestinations;
    private readonly ServiceMeteo _serviceMeteo;
    private readonly ServiceWikipedia _serviceWikipedia;
    private readonly ServicePays _servicePays;
    private readonly ServiceSuggestionsVoyage _serviceSuggestionsVoyage;

    public ServiceRechercheVoyage(
        ServiceDestinations serviceDestinations,
        ServiceMeteo serviceMeteo,
        ServiceWikipedia serviceWikipedia,
        ServicePays servicePays,
        ServiceSuggestionsVoyage serviceSuggestionsVoyage)
    {
        _serviceDestinations = serviceDestinations;
        _serviceMeteo = serviceMeteo;
        _serviceWikipedia = serviceWikipedia;
        _servicePays = servicePays;
        _serviceSuggestionsVoyage = serviceSuggestionsVoyage;
    }

    public async Task<List<PropositionVoyage>> RechercherVoyagesAsync(CritereRecherche criteres)
    {
        var destinations = await _serviceDestinations.ChargerDestinationsAsync();
        return await RechercherAsync(destinations, criteres);
    }

    public async Task<List<PropositionVoyage>> RechercherAsync(
        List<DestinationBrute> destinations,
        CritereRecherche criteres)
    {
        var candidates = destinations.AsEnumerable();

        if (criteres.Regions.Count > 0)
        {
            candidates = candidates.Where(d =>
                criteres.Regions.Contains(d.Region, StringComparer.OrdinalIgnoreCase));
        }

        candidates = candidates.Where(d => FiltreClimat(d.Climat, criteres.Climat));

        var liste = new List<PropositionVoyage>();

        foreach (var destination in candidates)
        {
            int prixVol = AjusterPrixVol(destination.PrixVolBase, criteres.StyleVoyage);
            int prixHotel = AjusterPrixHotel(destination.PrixHotelParNuitBase, criteres.NombreJours, criteres.StyleVoyage);
            int prixActivites = AjusterPrixActivites(destination.PrixActivitesBase, criteres.StyleVoyage);
            int prixTotal = prixVol + prixHotel + prixActivites;

            if (prixTotal > criteres.Budget)
                continue;

            double temperature = destination.TemperatureMoyenne;

            if (destination.Latitude != 0 || destination.Longitude != 0)
            {
                var temperatureApi = await _serviceMeteo.RecupererTemperatureMoyenneAsync(
                    destination.Latitude,
                    destination.Longitude);

                if (!double.IsNaN(temperatureApi))
                    temperature = temperatureApi;
            }

            var proposition = new PropositionVoyage
            {
                Cle = destination.Cle,
                Ville = destination.Ville,
                Pays = destination.Pays,
                Region = destination.Region,

                Latitude = destination.Latitude,
                Longitude = destination.Longitude,

                TemperatureMoyenne = temperature,

                PrixVol = prixVol,
                PrixHotel = prixHotel,
                PrixActivites = prixActivites,
                PrixTotal = prixTotal,

                UrlImage = ConstruireImagePlaceholder(destination.Ville),
                Etiquettes = destination.Etiquettes.ToList(),
                Activites = _serviceSuggestionsVoyage.GenererActivites(destination),
                IdeesGratuites = _serviceSuggestionsVoyage.GenererIdeesGratuites(destination),

                UrlVol = ConstruireUrlVol(destination),
                UrlHotel = ConstruireUrlHotel(destination),
                UrlActivites = ConstruireUrlActivites(destination)
            };

            proposition.Score = CalculerScore(destination, criteres, prixTotal);
            liste.Add(proposition);
        }

        liste = liste
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.PrixTotal)
            .Take(12)
            .ToList();

        foreach (var item in liste)
        {
            var resume = await _serviceWikipedia.RecupererResumeAsync(item.Ville);

            if (!string.IsNullOrWhiteSpace(resume.Description))
                item.Description = resume.Description;

            if (!string.IsNullOrWhiteSpace(resume.ImageUrl))
                item.UrlImage = resume.ImageUrl;

            item.UrlDrapeau = await _servicePays.RecupererDrapeauAsync(item.Pays);
        }

        return liste;
    }

    private bool FiltreClimat(string climatDestination, string climatRecherche)
    {
        return climatRecherche switch
        {
            "Chaud" => climatDestination.Equals("Hot", StringComparison.OrdinalIgnoreCase)
                       || climatDestination.Equals("Warm", StringComparison.OrdinalIgnoreCase),

            "Froid" => climatDestination.Equals("Cold", StringComparison.OrdinalIgnoreCase),

            _ => climatDestination.Equals("Mild", StringComparison.OrdinalIgnoreCase)
                 || climatDestination.Equals("Temperate", StringComparison.OrdinalIgnoreCase)
                 || climatDestination.Equals("Tempéré", StringComparison.OrdinalIgnoreCase)
        };
    }

    private int AjusterPrixVol(int basePrice, string style)
    {
        return style switch
        {
            "Économique" => (int)(basePrice * 0.9),
            "Luxe" => (int)(basePrice * 1.4),
            _ => basePrice
        };
    }

    private int AjusterPrixHotel(int basePerNight, int jours, string style)
    {
        int multiplicateur = style switch
        {
            "Économique" => 1,
            "Luxe" => 3,
            _ => 2
        };

        return basePerNight * jours * multiplicateur;
    }

    private int AjusterPrixActivites(int basePrice, string style)
    {
        return style switch
        {
            "Économique" => (int)(basePrice * 0.7),
            "Luxe" => (int)(basePrice * 1.5),
            _ => basePrice
        };
    }

    private double CalculerScore(DestinationBrute destination, CritereRecherche criteres, int prixTotal)
    {
        double score = 1000 - prixTotal;

        if (criteres.Regions.Contains(destination.Region))
            score += 200;

        if (FiltreClimat(destination.Climat, criteres.Climat))
            score += 250;

        score += destination.Etiquettes.Count * 20;

        return score;
    }

    private string ConstruireImagePlaceholder(string ville)
    {
        return $"https://picsum.photos/seed/{Uri.EscapeDataString(ville)}/900/600";
    }

    private string ConstruireUrlVol(DestinationBrute d)
    {
        return $"https://www.google.com/travel/flights?q=vols%20pour%20{Uri.EscapeDataString(d.Ville)}";
    }

    private string ConstruireUrlHotel(DestinationBrute d)
    {
        return $"https://www.booking.com/searchresults.fr.html?ss={Uri.EscapeDataString($"{d.Ville} {d.Pays}")}";
    }

    private string ConstruireUrlActivites(DestinationBrute d)
    {
        return $"https://www.getyourguide.com/s/?q={Uri.EscapeDataString($"{d.Ville} {d.Pays}")}";
    }
}