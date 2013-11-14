using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;

namespace Models.PMF.Organs
{
    class BelowGroundOrgan : GenericOrgan, BelowGround, Reproductive
    {
        [Link]
        Clock Clock = null;

        
        public event NullTypeDelegate Harvesting;
        [EventSubscribe("Harvest")]
        private void OnHarvest()
        {
            Harvesting.Invoke();

            DateTime Today = new DateTime(Clock.Today.Year, 1, 1);
            Today = Today.AddDays(Clock.Today.Day - 1);
            string Indent = "     ";
            string Title = Indent + Today.ToShortDateString() + "  - Harvesting " + Name + " from " + Plant.Name;
            double YieldDW = (Live.Wt + Dead.Wt);

            Console.WriteLine("");
            Console.WriteLine(Title);
            Console.WriteLine(Indent + new string('-', Title.Length));
            Console.WriteLine(Indent + Name + " Yield DWt: " + YieldDW.ToString("f2") + " (g/m^2)");
            Console.WriteLine("");


            Live.Clear();
            Dead.Clear();

        }
    }
}
