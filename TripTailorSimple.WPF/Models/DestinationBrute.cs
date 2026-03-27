using System.Text.Json.Serialization;

namespace TripTailorSimple.WPF.Models;

public class DestinationBrute
{
    [JsonPropertyName("key")]
    public string Cle { get; set; } = "";

    [JsonPropertyName("country")]
    public string Pays { get; set; } = "";

    [JsonPropertyName("city")]
    public string Ville { get; set; } = "";

    [JsonPropertyName("region")]
    public string Region { get; set; } = "";

    [JsonPropertyName("climate")]
    public string Climat { get; set; } = "";

    [JsonPropertyName("baseFlightPrice")]
    public int PrixVolBase { get; set; }

    [JsonPropertyName("baseHotelPricePerNight")]
    public int PrixHotelParNuitBase { get; set; }

    [JsonPropertyName("baseActivitiesPrice")]
    public int PrixActivitesBase { get; set; }

    [JsonPropertyName("averageTemperature")]
    public double TemperatureMoyenne { get; set; }

    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    [JsonPropertyName("tags")]
    public List<string> Etiquettes { get; set; } = new();
}