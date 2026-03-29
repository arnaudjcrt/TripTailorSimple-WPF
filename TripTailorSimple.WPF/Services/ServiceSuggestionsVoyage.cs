using TripTailorSimple.WPF.Models;

namespace TripTailorSimple.WPF.Services;

public class ServiceSuggestionsVoyage
{
    public List<string> GenererActivites(DestinationBrute d)
    {
        return new List<string>
        {
            $"Tour gastronomique à {d.Ville}",
            $"Cours de cuisine locale à {d.Ville}",
            $"Visite culturelle dans la région {d.Region}",
            $"Excursion guidée autour de {d.Ville}",
            $"Découverte premium des incontournables de {d.Ville}",
            $"Soirée typique à {d.Pays}"
        };
    }

    public List<string> GenererIdeesGratuites(DestinationBrute d)
    {
        return new List<string>
        {
            $"Balade libre dans les quartiers emblématiques de {d.Ville}",
            $"Point de vue et promenade au coucher du soleil",
            $"Marché local et découverte de l’ambiance de {d.Pays}",
            $"Découverte à pied du centre historique de {d.Ville}",
            $"Promenade dans les rues animées de {d.Ville}",
            $"Architecture en plein air et photos souvenirs"
        };
    }

    public List<JourItineraire> GenererItineraire(DestinationBrute d, int nombreJours)
    {
        var jours = new List<JourItineraire>();

        var activites = GenererActivites(d);
        var gratuites = GenererIdeesGratuites(d);

        int totalJours = Math.Max(2, nombreJours);

        for (int i = 1; i <= totalJours; i++)
        {
            if (i == 1)
            {
                jours.Add(new JourItineraire
                {
                    Titre = $"Jour {i}",
                    DateLabel = DateTime.Today.AddDays(i - 1).ToString("dd/MM/yyyy"),
                    Etapes = new List<string>
                    {
                        $"✈ Arrivée à {d.Ville}",
                        "🏨 Installation à l'hôtel",
                        $"🚶 Première découverte libre de {d.Ville}"
                    }
                });
                continue;
            }

            if (i == totalJours)
            {
                jours.Add(new JourItineraire
                {
                    Titre = $"Jour {i}",
                    DateLabel = DateTime.Today.AddDays(i - 1).ToString("dd/MM/yyyy"),
                    Etapes = new List<string>
                    {
                        "🧳 Dernières découvertes et temps libre",
                        $"📷 Dernière promenade dans {d.Ville}",
                        $"✈ Retour depuis {d.Ville}"
                    }
                });
                continue;
            }

            string activite = activites[(i - 2) % activites.Count];
            string gratuite = gratuites[(i - 2) % gratuites.Count];

            jours.Add(new JourItineraire
            {
                Titre = $"Jour {i}",
                DateLabel = DateTime.Today.AddDays(i - 1).ToString("dd/MM/yyyy"),
                Etapes = new List<string>
                {
                    $"🎯 {activite}",
                    $"🆓 {gratuite}",
                    $"🌆 Soirée libre à {d.Ville}"
                }
            });
        }

        return jours;
    }
}