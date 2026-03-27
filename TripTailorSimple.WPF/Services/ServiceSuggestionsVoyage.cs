using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TripTailorSimple.WPF.Models;


namespace TripTailorSimple.WPF.Services;

public class ServiceSuggestionsVoyage
{
    public List<string> GenererActivites(DestinationBrute d)
    {
        return new List<string>
        {
            $"Visite guidée premium de {d.Ville}",
            $"Découverte gastronomique à {d.Ville}",
            $"Excursion culturelle dans la région {d.Region}"
        };
    }

    public List<string> GenererIdeesGratuites(DestinationBrute d)
    {
        return new List<string>
        {
            $"Balade libre dans les quartiers emblématiques de {d.Ville}",
            $"Point de vue et promenade au coucher du soleil",
            $"Marché local et découverte de l’ambiance de {d.Pays}"
        };
    }

    public List<JourItineraire> GenererItineraire(DestinationBrute d, int nombreJours)
    {
        var jours = new List<JourItineraire>();

        for (int i = 1; i <= Math.Min(nombreJours, 5); i++)
        {
            jours.Add(new JourItineraire
            {
                Titre = $"Jour {i}",
                DateLabel = DateTime.Today.AddDays(i - 1).ToString("dd/MM/yyyy"),
                Etapes = new List<string>
                {
                    $"Matin : découverte de {d.Ville}",
                    $"Après-midi : activité locale en {d.Region}",
                    $"Soir : dîner typique à {d.Pays}"
                }
            });
        }

        return jours;
    }
}