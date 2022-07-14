using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using APSIM.Shared.Utilities;
using Models.Core;

namespace Models.Soils
{
    /// <summary>
    /// Simple Hydraulic Properties Model
    /// </summary>
    [Serializable]
    //[ViewName("UserInterface.Views.PropertyView")]
    //[PresenterName("UserInterface.Presenters.PropertyPresenter")]
    //[ValidParent(ParentType=typeof(Swim3))]
    public class HyProps //: Model
    {

        /// <summary> The amount of rainfall intercepted by crop and residue canopies </summary>
        public double[,] DELk;
        /// <summary> The amount of rainfall intercepted by crop and residue canopies </summary>
        public double[,] Mk;
        /// <summary> The amount of rainfall intercepted by crop and residue canopies </summary>
        public double[,] M0;
        /// <summary> The amount of rainfall intercepted by crop and residue canopies </summary>
        public double[,] M1;
        /// <summary> The amount of rainfall intercepted by crop and residue canopies </summary>
        public double[,] Y0;
        /// <summary> The amount of rainfall intercepted by crop and residue canopies </summary>
        public double[,] Y1;
        /// <summary> The amount of rainfall intercepted by crop and residue canopies </summary>
        public double[] MicroP;
        /// <summary> The amount of rainfall intercepted by crop and residue canopies </summary>
        public double[] MicroKs;
        /// <summary> The amount of rainfall intercepted by crop and residue canopies </summary>
        public double[] Kdula;
        /// <summary> The amount of rainfall intercepted by crop and residue canopies </summary>
        public double[] MacroP;
        /// <summary> The amount of rainfall intercepted by crop and residue canopies </summary>
        public double[] psid;
        const double psi_ll15 = -15000.0;
        const double psiad = -1e6;
        const double psi0 = -0.6e7;

        internal void ResizePropfileArrays(int newSize)
        {
            DELk = new double[newSize, 4];
            Mk = new double[newSize, 4];
            M0 = new double[newSize, 5];
            M1 = new double[newSize, 5];
            Y0 = new double[newSize, 5];
            Y1 = new double[newSize, 5];
            Array.Resize(ref MicroP, newSize);
            Array.Resize(ref MicroKs, newSize);
            Array.Resize(ref Kdula, newSize);
            Array.Resize(ref MacroP, newSize);
            Array.Resize(ref psid, newSize);
        }

        /// <summary> The amount of rainfall intercepted by crop and residue canopies </summary>
        public void SetupThetaCurve(double PSIDul, int n, double[] LL15, double[] DUL, double[] SAT)
        {
            for (int layer = 0; layer <= n; layer++)
            {
                psid[layer] = PSIDul;  //- (p%x(p%n) - p%x(layer))

                DELk[layer, 0] = (DUL[layer] - SAT[layer]) / (Math.Log10(-psid[layer]));
                DELk[layer, 1] = (LL15[layer] - DUL[layer]) / (Math.Log10(-psi_ll15) - Math.Log10(-psid[layer]));
                DELk[layer, 2] = -LL15[layer] / (Math.Log10(-psi0) - Math.Log10(-psi_ll15));
                DELk[layer, 3] = -LL15[layer] / (Math.Log10(-psi0) - Math.Log10(-psi_ll15));

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
                Y0[layer, 0] = SAT[layer];
                Y1[layer, 0] = SAT[layer];

                M0[layer, 1] = Mk[layer, 0] * (Math.Log10(-psid[layer]) - 0.0);
                M1[layer, 1] = Mk[layer, 1] * (Math.Log10(-psid[layer]) - 0.0);
                Y0[layer, 1] = SAT[layer];
                Y1[layer, 1] = DUL[layer];

                M0[layer, 2] = Mk[layer, 1] * (Math.Log10(-psi_ll15) - Math.Log10(-psid[layer]));
                M1[layer, 2] = Mk[layer, 2] * (Math.Log10(-psi_ll15) - Math.Log10(-psid[layer]));
                Y0[layer, 2] = DUL[layer];
                Y1[layer, 2] = LL15[layer];

                M0[layer, 3] = Mk[layer, 2] * (Math.Log10(-psi0) - Math.Log10(-psi_ll15));
                M1[layer, 3] = Mk[layer, 3] * (Math.Log10(-psi0) - Math.Log10(-psi_ll15));
                Y0[layer, 3] = LL15[layer];
                Y1[layer, 3] = 0.0;

                M0[layer, 4] = 0.0;
                M1[layer, 4] = 0.0;
                Y0[layer, 4] = 0.0;
                Y1[layer, 4] = 0.0;
            }
        }
        /// <summary> The amount of rainfall intercepted by crop and residue canopies </summary>
        public void SetupKCurve(int n, double[] LL15, double[] DUL, double[] SAT, double[] KS, double KDul, double PSIDul)
        {
            for (int layer = 0; layer <= n; layer++)
            {
                double b = -Math.Log(PSIDul / psi_ll15) / Math.Log(DUL[layer] / LL15[layer]);
                MicroP[layer] = b * 2.0 + 3.0;
                Kdula[layer] = Math.Min(0.99 * KDul, KS[layer]);
                MicroKs[layer] = Kdula[layer] / Math.Pow(DUL[layer] / SAT[layer], MicroP[layer]);

                double Sdul = DUL[layer] / SAT[layer];
                MacroP[layer] = Math.Log10(Kdula[layer] / 99.0 / (KS[layer] - MicroKs[layer])) / Math.Log10(Sdul);
            }
        }
        /// <summary> The amount of rainfall intercepted by crop and residue canopies </summary>
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

            return (2 * tCube - 3 * tSqr + 1) * Y0[layer, i] + (tCube - 2 * tSqr + t) * M0[layer, i]
                    + (-2 * tCube + 3 * tSqr) * Y1[layer, i] + (tCube - tSqr) * M1[layer, i];
        }

        /// <summary> The amount of rainfall intercepted by crop and residue canopies </summary>
        public double SimpleK(int layer, double psiValue, double[] SAT, double[] KS)
        {
            //  Purpose
            //      Calculate Conductivity for a given node for a specified suction.

            double S = SimpleS(layer, psiValue, SAT);
            double simpleK;

            if (S <= 0.0)
                simpleK = 1e-100;
            else
            {
                double microK = MicroKs[layer] * Math.Pow(S, MicroP[layer]);

                if (MicroKs[layer] >= KS[layer])
                    simpleK = microK;
                else
                {
                    double macroK = (KS[layer] - MicroKs[layer]) * Math.Pow(S, MacroP[layer]);
                    simpleK = microK + macroK;
                }
            }
            return simpleK / 24.0 / 10.0;
        }

        private double SimpleS(int layer, double psiValue, double[] SAT)
        {
            //  Purpose
            //      Calculate S for a given node for a specified suction.
            return SimpleTheta(layer, psiValue) / SAT[layer];
        }

        /// <summary> The amount of rainfall intercepted by crop and residue canopies </summary>
        public double Suction(int node, double theta, double[] _psi, double PSIDul, double[] LL15, double[] DUL, double[] SAT)
        {
            //  Purpose
            //   Calculate the suction for a given water content for a given node.
            const int maxIterations = 1000;
            const double tolerance = 1e-9;
            const double dpF = 0.01;
            double psiValue;

            if (theta == SAT[node])
            {
                if (_psi[node] > 0)
                    return _psi[node];
                else
                    return 0;
            }
            else
            {
                if (MathUtilities.FloatsAreEqual(_psi[node], 0.0))
                    if (theta > DUL[node])
                        psiValue = PSIDul; // Initial estimate
                    else if (theta < LL15[node])
                        psiValue = psi_ll15;
                    else
                    {
                        double pFll15 = Math.Log10(-psi_ll15);
                        double pFdul = Math.Log10(-PSIDul);
                        double frac = (theta - LL15[node]) / (DUL[node] - LL15[node]);
                        double pFinit = pFll15 + frac * (pFdul - pFll15);
                        psiValue = -Math.Pow(10, pFinit);
                    }
                else
                    psiValue = _psi[node]; // Initial estimate

                for (int iter = 0; iter < maxIterations; iter++)
                {
                    double est = SimpleTheta(node, psiValue);
                    double pF = 0.000001;
                    if (psiValue < 0)
                        pF = Math.Log10(-psiValue);
                    double pF2 = pF + dpF;
                    double psiValue2 = -Math.Pow(10, pF2);
                    double est2 = SimpleTheta(node, psiValue2);

                    double m = (est2 - est) / dpF;

                    if (Math.Abs(est - theta) < tolerance)
                        break;
                    double pFnew = pF - (est - theta) / m;
                    if (pFnew > (Math.Log10(-psi0)))
                        pF += dpF;  // This is not really adequate - just saying...
                    else
                        pF = pFnew;
                    psiValue = -Math.Pow(10, pF);
                }
                return psiValue;
            }
        }
    }

}
