using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TripTailorSimple.WPF.Models;

public class CritereRecherche
{
    public string Climat { get; set; } = "Tempéré";
    public List<string> Regions { get; set; } = new();
    public int Budget { get; set; } = 2000;
    public int NombreJours { get; set; } = 7;
    public string StyleVoyage { get; set; } = "Confort";
}