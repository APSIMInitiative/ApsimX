using System;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;
using Newtonsoft.Json;

namespace Models.Soils
{
    /// <summary>
    /// Returns theta and ksat values for specified psi and theta values respectively.  Gets its parameters from the soil Water node and a couple of parameters it owns
    /// </summary>
    [Serializable]
    [ViewName("ApsimNG.Resources.Glade.ProfileView.glade")]
    [PresenterName("UserInterface.Presenters.ProfilePresenter")]
    [ValidParent(ParentType = typeof(Soil))]
    public class HydraulicProperties : Model
    {
        #region External links
        [Link]
        private Physical Water = null;
        #endregion

        #region Internal States
        ///// <summary>The de lk</summary>
        /// <summary>
        /// The de lk
        /// </summary>
        private double[,] DELk = null;
        ///// <summary>The mk</summary>
        /// <summary>
        /// The mk
        /// </summary>
        private double[,] Mk = null;
        /// <summary>
        /// The m0
        /// </summary>
        private double[,] M0 = null;
        /// <summary>
        /// The m1
        /// </summary>
        private double[,] M1 = null;
        /// <summary>
        /// The y0
        /// </summary>
        private double[,] Y0 = null;
        /// <summary>
        /// The y1
        /// </summary>
        private double[,] Y1 = null;
        /// <summary>
        /// The micro p
        /// </summary>
        private double[] MicroP = null;
        /// <summary>
        /// The micro ks
        /// </summary>
        private double[] MicroKs = null;
        ///// <summary>The kdula</summary>
        /// <summary>
        /// The kdula
        /// </summary>
        private double[] Kdula = null;
        /// <summary>
        /// The macro p
        /// </summary>
        private double[] MacroP = null;

        /// <summary>
        /// The psid
        /// </summary>
        private double[] psid = null;
        /// <summary>
        /// The psi_ll15
        /// </summary>
        const double psi_ll15 = -15000.0;
        /// <summary>
        /// The psiad
        /// </summary>
        const double psiad = -1e6;
        /// <summary>
        /// The psi0
        /// </summary>
        const double psi0 = -0.6e7;


        #endregion

        #region Parameters
        /// <summary>
        /// psidul
        /// </summary>
        [Description("The suction when the soil is at DUL")]
        [Units("cm")]
        [Bounds(Lower = -1e3, Upper = 0.0)]
        public double psidul { get; set; }

        /// <summary>Gets or sets the thickness.</summary>
        public double[] Thickness { get; set; }

        /// <summary>Depth strings. Wrapper around Thickness.</summary>
        [Display]
        [Units("mm")]
        [JsonIgnore]
        public string[] Depth
        {
            get => SoilUtilities.ToDepthStrings(Thickness);
            set => Thickness = SoilUtilities.ToThickness(value);
        }

        /// <summary>
        /// kdul
        /// </summary>
        /// <value>
        /// The kdul.
        /// </value>
        [Display]
        [Units("mm/d")]
        public double[] kdul { get; set; }
        #endregion

        #region public methods
        /// <summary>
        /// Simples the theta.
        /// </summary>
        /// <param name="layer">The layer.</param>
        /// <param name="psiValue">The psi value.</param>
        /// <returns></returns>
        public double SimpleTheta(int layer, double psiValue)
        {
            //  Purpose
            //     Calculate Theta for a given node for a specified suction.
            int i;
            double t;

            if (psiValue >= -1.0)
            {
                i = 0;
                t = 0.0;
            }
            else if (psiValue > psid[layer])
            {
                i = 1;
                t = (Math.Log10(-psiValue) - 0.0) / (Math.Log10(-psid[layer]) - 0.0);
            }
            else if (psiValue > psi_ll15)
            {
                i = 2;
                t = (Math.Log10(-psiValue) - Math.Log10(-psid[layer])) / (Math.Log10(-psi_ll15) - Math.Log10(-psid[layer]));
            }
            else if (psiValue > psi0)
            {
                i = 3;
                t = (Math.Log10(-psiValue) - Math.Log10(-psi_ll15)) / (Math.Log10(-psi0) - Math.Log10(-psi_ll15));
            }
            else
            {
                i = 4;
                t = 0.0;
            }

            double tSqr = t * t;
            double tCube = tSqr * t;
            double theta = (2 * tCube - 3 * tSqr + 1) * Y0[layer, i] + (tCube - 2 * tSqr + t) * M0[layer, i]
                    + (-2 * tCube + 3 * tSqr) * Y1[layer, i] + (tCube - tSqr) * M1[layer, i];
            return Math.Min(theta, Water.SAT[layer]); //When Sat and DUL are very close, spline can produce number greater that sat
        }

        /// <summary>
        /// Calcultates and returns hydraulic conductivity in cm/h
        /// </summary>
        /// <param name="layer">The layer.</param>
        /// <param name="psiValue">The psi value.</param>
        /// <returns>Hydraulic Conductivity</returns>
        public double SimpleK(int layer, double psiValue)
        {
            //  Purpose
            //      Calculate Conductivity for a given node for a specified suction.

            double S = SimpleS(layer, psiValue);
            double simpleK;

            if (S <= 0.0)
                simpleK = 1e-100;
            else
            {
                double microK = MicroKs[layer] * Math.Pow(S, MicroP[layer]);

                if (MicroKs[layer] >= Water.KS[layer])
                    simpleK = microK;
                else
                {
                    double macroK = (Water.KS[layer] - MicroKs[layer]) * Math.Pow(S, MacroP[layer]);
                    simpleK = microK + macroK;
                }
            }
            return simpleK / 24.0 / 10.0;
        }

        /// <summary>
        /// Called when soil models that require hydraulic properties information initiate their properties
        /// </summary>
        public void SetHydraulicProperties()
        {
            DELk = new double[Water.Thickness.Length, 4];
            Mk = new double[Water.Thickness.Length, 4];
            M0 = new double[Water.Thickness.Length, 5];
            M1 = new double[Water.Thickness.Length, 5];
            Y0 = new double[Water.Thickness.Length, 5];
            Y1 = new double[Water.Thickness.Length, 5];
            MicroP = new double[Water.Thickness.Length];
            MicroKs = new double[Water.Thickness.Length];
            kdul = new double[Water.Thickness.Length];
            Kdula = new double[Water.Thickness.Length];
            MacroP = new double[Water.Thickness.Length];
            psid = new double[Water.Thickness.Length];

            SetupThetaCurve();
            SetupKCurve();
        }
        #endregion

        #region Internal methods
        /// <summary>
        /// Simples the s.
        /// </summary>
        /// <param name="layer">The layer.</param>
        /// <param name="psiValue">The psi value.</param>
        /// <returns></returns>
        private double SimpleS(int layer, double psiValue)
        {
            //  Purpose
            //      Calculate S for a given node for a specified suction.
            return SimpleTheta(layer, psiValue) / Water.SAT[layer];
        }

        /// <summary>
        /// Sets up the theta curve
        /// </summary>
        private void SetupThetaCurve()
        {
            for (int layer = 0; layer < Water.Thickness.Length; layer++)
            {
                psid[layer] = psidul;  //- (p%x(p%n) - p%x(layer))

                DELk[layer, 0] = (Water.DUL[layer] - (Water.SAT[layer] + 0.000000000001)) / (Math.Log10(-psid[layer])); //Tiny amount added to Sat so in situations where DUL = SAT this function returns a non zero value
                DELk[layer, 1] = (Water.LL15[layer] - Water.DUL[layer]) / (Math.Log10(-psi_ll15) - Math.Log10(-psid[layer]));
                DELk[layer, 2] = -Water.LL15[layer] / (Math.Log10(-psi0) - Math.Log10(-psi_ll15));
                DELk[layer, 3] = -Water.LL15[layer] / (Math.Log10(-psi0) - Math.Log10(-psi_ll15));

                Mk[layer, 0] = 0.0;
                Mk[layer, 1] = (DELk[layer, 0] + DELk[layer, 1]) / 2.0;
                Mk[layer, 2] = (DELk[layer, 1] + DELk[layer, 2]) / 2.0;
                Mk[layer, 3] = DELk[layer, 3];

                // First bit might not be monotonic so check and adjust
                double alpha = Mk[layer, 0] / DELk[layer, 0];
                double beta = Mk[layer, 1] / DELk[layer, 0];
                double phi = alpha - (Math.Pow(2.0 * alpha + beta - 3.0, 2.0) / (3.0 * (alpha + beta - 2.0)));
                if (phi <= 0)
                {
                    double tau = 3.0 / Math.Sqrt(alpha * alpha + beta * beta);
                    Mk[layer, 0] = tau * alpha * DELk[layer, 0];
                    Mk[layer, 1] = tau * beta * DELk[layer, 0];
                }

                M0[layer, 0] = 0.0;
                M1[layer, 0] = 0.0;
                Y0[layer, 0] = Water.SAT[layer];
                Y1[layer, 0] = Water.SAT[layer];

                M0[layer, 1] = Mk[layer, 0] * (Math.Log10(-psid[layer]) - 0.0);
                M1[layer, 1] = Mk[layer, 1] * (Math.Log10(-psid[layer]) - 0.0);
                Y0[layer, 1] = Water.SAT[layer];
                Y1[layer, 1] = Water.DUL[layer];

                M0[layer, 2] = Mk[layer, 1] * (Math.Log10(-psi_ll15) - Math.Log10(-psid[layer]));
                M1[layer, 2] = Mk[layer, 2] * (Math.Log10(-psi_ll15) - Math.Log10(-psid[layer]));
                Y0[layer, 2] = Water.DUL[layer];
                Y1[layer, 2] = Water.LL15[layer];

                M0[layer, 3] = Mk[layer, 2] * (Math.Log10(-psi0) - Math.Log10(-psi_ll15));
                M1[layer, 3] = Mk[layer, 3] * (Math.Log10(-psi0) - Math.Log10(-psi_ll15));
                Y0[layer, 3] = Water.LL15[layer];
                Y1[layer, 3] = 0.0;

                M0[layer, 4] = 0.0;
                M1[layer, 4] = 0.0;
                Y0[layer, 4] = 0.0;
                Y1[layer, 4] = 0.0;
            }
        }

        /// <summary>
        /// Sets up the K curve
        /// </summary>
        private void SetupKCurve()
        {
            for (int layer = 0; layer < Water.Thickness.Length; layer++)
            {
                double b = -Math.Log(psidul / psi_ll15) / Math.Log(Water.DUL[layer] / Water.LL15[layer]);
                MicroP[layer] = b * 2.0 + 3.0;
                Kdula[layer] = Math.Min(0.99 * kdul[layer], Water.KS[layer]);
                MicroKs[layer] = Kdula[layer] / Math.Pow(Water.DUL[layer] / Water.SAT[layer], MicroP[layer]);

                double Sdul = Water.DUL[layer] / Water.SAT[layer];
                MacroP[layer] = Math.Log10(Kdula[layer] / 99.0 / (Water.KS[layer] - MicroKs[layer])) / Math.Log10(Sdul);
            }
        }
        #endregion
    }
}
