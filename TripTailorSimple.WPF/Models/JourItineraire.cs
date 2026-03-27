using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TripTailorSimple.WPF.Models;

public class JourItineraire
{
    public string Titre { get; set; } = "";
    public string DateLabel { get; set; } = "";
    public List<string> Etapes { get; set; } = new();
}