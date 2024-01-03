using System;
using Models.Core;

namespace Models.PMF.Phen
{

    /// <summary>
    /// Vernalisation rate parameter set for specific cultivar
    /// </summary>
    [Serializable]
    public class CultivarRateParams : Model
    {
        /// <summary>Base delta for Upregulation of Vrn during vegetative phase at temp >20oC</summary>
        public double BaseDVrnVeg { get; set; }
        /// <summary>Maximum delta for Upregulation of Vrn1 at 0oC</summary>
        public double MaxDVrnVeg { get; set; }
        /// <summary>Base delta for Upregulation of Vrn during vegetative phase at temp >20oC</summary>
        public double BaseDVrnER { get; set; }
        /// <summary>The relative increase in delta Vrn cuased by Vrn3 expression under long photoperiod during early reproductive phase</summary>
        public double MaxDVrnER { get; set; }
        /// <summary> The relative increase in delta Vrn caused by Vrn3 expression under long Pp during vegetative phase. </summary>
        public double PpVrn3FactVeg { get; set; }
        /// <summary>The relative increase in delta Vrn caused by Vrn3 expression under long Pp during early reproductive phase </summary>
        public double PpVrn3FactER { get; set; }
        /// <summary>Maximum Vrn2 expression under long days </summary>
        public double MaxVrn2 { get; set; }
        /// <summary>Amount of Cold that must be encountered before persistant Vrn1 upregulation occurs</summary>
        public double MethalationThreshold { get; set; }
        /// <summary> The relative increase in delta Vrn1 caused by cold upregulation of Vrn1</summary>
        public double ColdVrn1Fact { get; set; }
    }

}
