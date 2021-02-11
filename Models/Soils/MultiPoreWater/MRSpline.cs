using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Models.Soils
{
    /// <summary>
    /// Fits a 5 point hermite spline to moisture release data and returns theta for any specified psi.  Gets its parameters from the soil Water node and a couple of parameters it owns
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.ProfileView")]
    [PresenterName("UserInterface.Presenters.ProfilePresenter")]
    [ValidParent(ParentType = typeof(Soil))]
    public class MRSpline : Model
    {
        #region External links

        /// <summary>Access the WEIRDO model.</summary>
        [Link]
        WEIRDO Weirdo = null;

        /// <summary>Access the soil physical properties.</summary>
        [Link]
        private IPhysical soilPhysical = null;

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
        /// Water potential at drained upper limit
        /// </summary>
        private double psidul = -1000;
        /// <summary>
        /// Water potential at lower limit
        /// </summary>
        const double psi_ll15 = -150000.0;
        /// <summary>
        /// Water potential at oven dry
        /// </summary>
        const double psi0 = -0.6e8;
        /// <summary>
        /// Water potential at saturation
        /// </summary>
        const double psis = -10;
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

            if (psiValue >= psis)
            {
                i = 0;
                t = 0.0;
            }
            else if (psiValue > Weirdo.PsiBub[layer])
            {
                i = 1;
                t = (Math.Log10(-psiValue) - Math.Log10(-psis)) / (Math.Log10(-Weirdo.PsiBub[layer]) - Math.Log10(-psis));
            }
            else if (psiValue > psidul)
            {
                i = 2;
                t = (Math.Log10(-psiValue) - Math.Log10(-Weirdo.PsiBub[layer])) / (Math.Log10(-psidul) - Math.Log10(-Weirdo.PsiBub[layer]));
            }
            else if (psiValue > psi_ll15)
            {
                i = 3;
                t = (Math.Log10(-psiValue) - Math.Log10(-psidul)) / (Math.Log10(-psi_ll15) - Math.Log10(-psidul));
            }
            else if (psiValue > psi0)
            {
                i = 4;
                t = (Math.Log10(-psiValue) - Math.Log10(-psi_ll15)) / (Math.Log10(-psi0) - Math.Log10(-psi_ll15));
            }
            else
            {
                i = 5;
                t = 0.0;
            }

            double tSqr = t * t;
            double tCube = tSqr * t;
            double theta = (2 * tCube - 3 * tSqr + 1) * Y0[layer, i] + (tCube - 2 * tSqr + t) * M0[layer, i]
                    + (-2 * tCube + 3 * tSqr) * Y1[layer, i] + (tCube - tSqr) * M1[layer, i];
            return Math.Min(theta, soilPhysical.SAT[layer]); //When Sat and DUL are very close, spline can produce number greater that sat
        }
                
        /// <summary>
        /// Called when soil models that require hydraulic properties information initiate their properties
        /// </summary>
        public void SetHydraulicProperties()
        {
            int nLayers = soilPhysical.SAT.Length;
            DELk = new double[nLayers, 5];
            Mk = new double[nLayers, 5];
            M0 = new double[nLayers, 6];
            M1 = new double[nLayers, 6];
            Y0 = new double[nLayers, 6];
            Y1 = new double[nLayers, 6];
            
            SetupThetaCurve();
        }
        
        /// <summary>
        /// Sets up the theta curve
        /// </summary>
        private void SetupThetaCurve()
        {
            for (int layer = 0; layer < soilPhysical.SAT.Length; layer++)
            {
                if (Weirdo.PsiBub[layer] > 0)
                    throw new Exception(this + "PsiBub is positive in layer " + layer + ".  It must be a negative number" );

                DELk[layer, 0] = (soilPhysical.SAT[layer] - (soilPhysical.SAT[layer]+1e-20)) / (Math.Log10(-Weirdo.PsiBub[layer])); //Tiny amount added to Sat so in situations where DUL = SAT this function returns a non zero value
                DELk[layer, 1] = (soilPhysical.DUL[layer] - soilPhysical.SAT[layer]) / (Math.Log10(-psidul) - Math.Log10(-Weirdo.PsiBub[layer]));
                DELk[layer, 2] = (soilPhysical.LL15[layer] - soilPhysical.DUL[layer]) / (Math.Log10(-psi_ll15) - Math.Log10(-psidul));
                DELk[layer, 3] = -soilPhysical.LL15[layer] / (Math.Log10(-psi0) - Math.Log10(-psi_ll15));
                DELk[layer, 4] = -soilPhysical.LL15[layer] / (Math.Log10(-psi0) - Math.Log10(-psi_ll15));

                Mk[layer, 0] = 0.0;
                Mk[layer, 1] = (DELk[layer, 0] + DELk[layer, 1]) / 2.0;
                Mk[layer, 2] = (DELk[layer, 1] + DELk[layer, 2]) / 2.0;
                Mk[layer, 3] = (DELk[layer, 2] + DELk[layer, 3]) / 2.0;
                Mk[layer, 4] = DELk[layer, 4];

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
                Y0[layer, 0] = soilPhysical.SAT[layer];
                Y1[layer, 0] = soilPhysical.SAT[layer];

                M0[layer, 1] = Mk[layer, 0] * (Math.Log10(-Weirdo.PsiBub[layer]) - Math.Log10(-psis));
                M1[layer, 1] = Mk[layer, 1] * (Math.Log10(-Weirdo.PsiBub[layer]) - Math.Log10(-psis));
                Y0[layer, 1] = soilPhysical.SAT[layer];
                Y1[layer, 1] = soilPhysical.SAT[layer];

                M0[layer, 2] = Mk[layer, 1] * (Math.Log10(-psidul) - Math.Log10(-Weirdo.PsiBub[layer]));
                M1[layer, 2] = Mk[layer, 2] * (Math.Log10(-psidul) - Math.Log10(-Weirdo.PsiBub[layer]));
                Y0[layer, 2] = soilPhysical.SAT[layer];
                Y1[layer, 2] = soilPhysical.DUL[layer];

                M0[layer, 3] = Mk[layer, 2] * (Math.Log10(-psi_ll15) - Math.Log10(-psidul));
                M1[layer, 3] = Mk[layer, 3] * (Math.Log10(-psi_ll15) - Math.Log10(-psidul));
                Y0[layer, 3] = soilPhysical.DUL[layer];
                Y1[layer, 3] = soilPhysical.LL15[layer];

                M0[layer, 4] = Mk[layer, 3] * (Math.Log10(-psi0) - Math.Log10(-psi_ll15));
                M1[layer, 4] = Mk[layer, 4] * (Math.Log10(-psi0) - Math.Log10(-psi_ll15));
                Y0[layer, 4] = soilPhysical.LL15[layer];
                Y1[layer, 4] = 0.0;

                M0[layer, 5] = 0.0;
                M1[layer, 5] = 0.0;
                Y0[layer, 5] = 0.0;
                Y1[layer, 5] = 0.0;
            }
        }
        #endregion

    }
}
