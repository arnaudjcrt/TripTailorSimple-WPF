namespace TripTailorSimple.WPF.Models;

public class PropositionVoyage
{
    public string Cle { get; set; } = "";
    public string Pays { get; set; } = "";
    public string Ville { get; set; } = "";
    public string Region { get; set; } = "";

    public string Description { get; set; } = "";
    public string UrlImage { get; set; } = "";
    public string UrlDrapeau { get; set; } = "";

    public int PrixVol { get; set; }
    public int PrixHotel { get; set; }
    public int PrixActivites { get; set; }
    public int PrixTotal { get; set; }

    public double TemperatureMoyenne { get; set; }
    public double Score { get; set; }

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public string UrlVol { get; set; } = "";
    public string UrlHotel { get; set; } = "";
    public string UrlActivites { get; set; } = "";

    public List<string> Etiquettes { get; set; } = new();
    public List<string> Activites { get; set; } = new();
    public List<string> IdeesGratuites { get; set; } = new();

    public List<JourItineraire> Itineraire { get; set; } = new();
    public int NombreJours { get; set; }
    public string CompagnieVol { get; set; } = "";
    public string NomHotel { get; set; } = "";
    public string StyleVoyageAffiche { get; set; } = "";

    public string VillePays => $"{Ville}, {Pays}";
}