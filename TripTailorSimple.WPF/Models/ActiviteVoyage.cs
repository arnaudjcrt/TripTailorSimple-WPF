using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TripTailorSimple.WPF.Models;

public class ActiviteVoyage
{
    public string Titre { get; set; } = "";
    public string Categorie { get; set; } = "";
    public string Resume { get; set; } = "";
    public decimal Prix { get; set; }
    public bool EstGratuite { get; set; }
}
