using System;
using Models.Core;
using Models.Interfaces;
using Models.PMF;
using APSIM.Shared.Utilities;

namespace Models
{
    [Serializable]
    public class Script : Model
    {
        [Link] IClock Clock;
        [Link] ISummary Summary;

        [EventSubscribe("DoManagement")]
        private void DoDailyCalculations(object sender, EventArgs e)
        {
            // Called once per day
        }
    }
}
