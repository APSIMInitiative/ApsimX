using Models.Functions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Models.PMF.Struct
{
    /// <summary>
    /// Input parameters passed to the Culm constructor.
    /// This is required because the culms are created ad-hoc, partway
    /// through a simulation, so we can't use [Link]s to resolve
    /// dependencies.
    /// </summary>
    [Serializable]
    public class CulmParams
    {
        /// <summary>
        /// Function which corrects for other growing leaves.
        /// </summary>
        public IFunction LeafNoCorrection { get; set; }

        /// <summary>
        /// Function which returns Eqn 14 calc x0 - position of largest leaf.
        /// </summary>
        public IFunction AX0 { get; set; }

        /// <summary>
        /// Function which returns seed number?
        /// </summary>
        public IFunction NoSeed { get; set; }

        /// <summary>
        /// Function which return culm init rate.
        /// </summary>
        public IFunction InitRate { get; set; }

        /// <summary>
        /// Initial appearance rate.
        /// </summary>
        public IFunction AppearanceRate1 { get; set; }

        /// <summary>
        /// Mid appearance rate.
        /// </summary>
        public IFunction AppearanceRate2 { get; set; }

        /// <summary>
        /// Final appearance rate.
        /// </summary>
        public IFunction AppearanceRate3 { get; set; }

        /// <summary>
        /// idek
        /// </summary>
        public IFunction NoRateChange { get; set; }

        /// <summary>
        /// idek
        /// </summary>
        public IFunction NoRateChange2 { get; set; }

        /// <summary>
        /// Leaf number at emergence.
        /// </summary>
        public IFunction LeafNoAtEmergence { get; set; }

        /// <summary>
        /// Min leaf number.
        /// </summary>
        public IFunction MinLeafNo { get; set; }

        /// <summary>
        /// Max leaf number.
        /// </summary>
        public IFunction MaxLeafNo { get; set; }

        /// <summary>
        /// Accumulated TT from emergence to floral init.
        /// </summary>
        public IFunction TTEmergToFI { get; set; }

        /// <summary>
        /// Largest leaf area slope.
        /// </summary>
        public IFunction AMaxS { get; set; }

        /// <summary>
        /// Largest leaf area intercept.
        /// </summary>
        public IFunction AMaxI { get; set; }

        /// <summary>
        /// Plant sowing density.
        /// </summary>
        public double Density { get; set; }

        /// <summary>
        /// Daily thermaltime value.
        /// </summary>
        public IFunction DltTT { get; set; }

        /// <summary>
        /// bellCurveParams[0]
        /// </summary>
        public IFunction A0 { get; set; }

        /// <summary>
        /// bellCurveParams[1]
        /// </summary>
        public IFunction A1 { get; set; }

        /// <summary>
        /// bellCurveParams[2]
        /// </summary>
        public IFunction B0 { get; set; }

        /// <summary>
        /// bellCurveParams[3]
        /// </summary>
        public IFunction B1 { get; set; }

        /// <summary>
        /// largestLeafParams[0]
        /// </summary>
        public IFunction AMaxA { get; set; }

        /// <summary>
        /// largestLeafParams[1]
        /// </summary>
        public IFunction AMaxB { get; set; }

        /// <summary>
        /// largestLeafParams[2]
        /// </summary>
        public IFunction AMaxC { get; set; }

        /// <summary>
        /// Function which fetches TT target from endjuv to floaral init.
        /// </summary>
        public Action<double> GetTTFI { get; set; }
    }
}
