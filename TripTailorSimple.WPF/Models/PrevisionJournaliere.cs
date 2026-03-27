using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TripTailorSimple.WPF.Modeles;

public class PrevisionJournaliere
{
    public string Jour { get; set; } = "";
    public double Temperature { get; set; }
    public string ResumeMeteo { get; set; } = "";
}