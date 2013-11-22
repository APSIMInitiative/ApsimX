using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;

namespace Models.PMF.Organs
{
    public class BelowGroundOrgan : GenericOrgan, BelowGround, Reproductive
    {
        [Link]
        Clock Clock = null;

        [Link]
        Summary Summary = null;
        
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

            Summary.WriteMessage(FullPath, "");
            Summary.WriteMessage(FullPath, Title);
            Summary.WriteMessage(FullPath, Indent + new string('-', Title.Length));
            Summary.WriteMessage(FullPath, Indent + Name + " Yield DWt: " + YieldDW.ToString("f2") + " (g/m^2)");
            Summary.WriteMessage(FullPath, "");


            Live.Clear();
            Dead.Clear();

        }
    }
}
