using System.IO;
using System.Text.Json;
using TripTailorSimple.WPF.Models;

namespace TripTailorSimple.WPF.Services;

public class ServiceDestinations
{
    public async Task<List<DestinationBrute>> ChargerDestinationsAsync()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;

        var chemins = new[]
        {
            Path.Combine(baseDir, "Data", "destinations.json"),
            Path.Combine(baseDir, "Donnees", "destinations.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "Data", "destinations.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "Donnees", "destinations.json")
        };

        string? fichier = chemins.FirstOrDefault(File.Exists);

        if (fichier != null)
        {
            try
            {
                string json = await File.ReadAllTextAsync(fichier);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var data = JsonSerializer.Deserialize<List<DestinationBrute>>(json, options);

                if (data != null && data.Count > 0)
                {
                    var nettoyees = data
                        .Where(d =>
                            !string.IsNullOrWhiteSpace(d.Ville) &&
                            !string.IsNullOrWhiteSpace(d.Pays) &&
                            !string.IsNullOrWhiteSpace(d.Region) &&
                            !string.IsNullOrWhiteSpace(d.Climat))
                        .ToList();

                    if (nettoyees.Count > 0)
                        return nettoyees;
                }
            }
            catch
            {
            }
        }

        return GetDestinationsDeSecours();
    }

    private static List<DestinationBrute> GetDestinationsDeSecours()
    {
        return new List<DestinationBrute>
        {
            new DestinationBrute
            {
                Cle = "kyoto-japon",
                Ville = "Kyoto",
                Pays = "Japon",
                Region = "Asie",
                Climat = "Mild",
                PrixVolBase = 650,
                PrixHotelParNuitBase = 70,
                PrixActivitesBase = 180,
                TemperatureMoyenne = 22,
                Latitude = 35.0116,
                Longitude = 135.7681,
                Etiquettes = new List<string> { "Culture", "Zen", "Food" }
            },
            new DestinationBrute
            {
                Cle = "marrakech-maroc",
                Ville = "Marrakech",
                Pays = "Maroc",
                Region = "Afrique",
                Climat = "Hot",
                PrixVolBase = 180,
                PrixHotelParNuitBase = 45,
                PrixActivitesBase = 120,
                TemperatureMoyenne = 30,
                Latitude = 31.6295,
                Longitude = -7.9811,
                Etiquettes = new List<string> { "Soleil", "Souks", "Dépaysement" }
            },
            new DestinationBrute
            {
                Cle = "paris-france",
                Ville = "Paris",
                Pays = "France",
                Region = "Europe",
                Climat = "Mild",
                PrixVolBase = 90,
                PrixHotelParNuitBase = 90,
                PrixActivitesBase = 140,
                TemperatureMoyenne = 18,
                Latitude = 48.8566,
                Longitude = 2.3522,
                Etiquettes = new List<string> { "Musées", "Ville", "Romantique" }
            },
            new DestinationBrute
            {
                Cle = "reykjavik-islande",
                Ville = "Reykjavik",
                Pays = "Islande",
                Region = "Europe",
                Climat = "Cold",
                PrixVolBase = 320,
                PrixHotelParNuitBase = 110,
                PrixActivitesBase = 220,
                TemperatureMoyenne = 7,
                Latitude = 64.1466,
                Longitude = -21.9426,
                Etiquettes = new List<string> { "Nature", "Nordique", "Aurores" }
            },
            new DestinationBrute
            {
                Cle = "bali-indonesie",
                Ville = "Ubud",
                Pays = "Indonésie",
                Region = "Asie",
                Climat = "Hot",
                PrixVolBase = 720,
                PrixHotelParNuitBase = 40,
                PrixActivitesBase = 160,
                TemperatureMoyenne = 31,
                Latitude = -8.5069,
                Longitude = 115.2625,
                Etiquettes = new List<string> { "Nature", "Yoga", "Tropical" }
            },
            new DestinationBrute
            {
                Cle = "newyork-usa",
                Ville = "New York",
                Pays = "États-Unis",
                Region = "Amériques",
                Climat = "Mild",
                PrixVolBase = 500,
                PrixHotelParNuitBase = 130,
                PrixActivitesBase = 260,
                TemperatureMoyenne = 20,
                Latitude = 40.7128,
                Longitude = -74.0060,
                Etiquettes = new List<string> { "City Trip", "Shopping", "Skyline" }
            },
            new DestinationBrute
            {
                Cle = "sydney-australie",
                Ville = "Sydney",
                Pays = "Australie",
                Region = "Océanie",
                Climat = "Hot",
                PrixVolBase = 950,
                PrixHotelParNuitBase = 120,
                PrixActivitesBase = 230,
                TemperatureMoyenne = 26,
                Latitude = -33.8688,
                Longitude = 151.2093,
                Etiquettes = new List<string> { "Océan", "Ville", "Plages" }
            }
        };
    }
}