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
        /// <summary>Base delta for Upregulation of Vrn1 >20oC</summary>
        public double BaseDVrn1 { get; set; }
        /// <summary>Maximum delta for Upregulation of Vrn1 at 0oC</summary>
        public double MaxDVrn1 { get; set; }
        /// <summary>potential Vrn2 expression soon after emergence under > 16h</summary>
        public double MaxIpVrn2 { get; set; }
        /// <summary>delta potential Vrn2 expression  under > 16h</summary>
        public double MaxDpVrn2 { get; set; }
        /// <summary>Base delta for Upregulation of Vrn3 at Pp below 8h </summary>
        public double BaseDVrn3 { get; set; }
        /// <summary>Maximum delta for Upregulation of Vrn3 at Pp above 16h </summary>
        public double MaxDVrn3 { get; set; }
        /// <summary>Maximum delta for upregulation of Vrn1 due to long Pp</summary>
        public double MaxDVrnX { get; set; }
        /// <summary> Base phyllochron</summary>
        public double IntFLNvsTSHS { get; set; }
        /// <summary>The maximum methalation of cold Vrn 1</summary>
        public double MaxMethColdVern1 { get; set; }
    }

}
