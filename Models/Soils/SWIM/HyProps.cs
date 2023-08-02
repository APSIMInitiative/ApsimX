using System;
using APSIM.Shared.Utilities;

namespace Models.Soils
{
    /// <summary>
    /// Simple Hydraulic Properties Model
    /// </summary>
    [Serializable]
    public class HyProps
    {

        private double[,] delk;
        private double[,] mk;
        private double[,] m0;
        private double[,] m1;
        private double[,] y0;
        private double[,] y1;
        private double[] microP;
        private double[] microKs;
        private double[] kdula;
        private double[] macroP;
        private double[] psid;
        const double psi_ll15 = -15000.0;
        const double psiad = -1e6;
        const double psi0 = -0.6e7;

        internal void ResizePropfileArrays(int newSize)
        {
            delk = new double[newSize, 4];
            mk = new double[newSize, 4];
            m0 = new double[newSize, 5];
            m1 = new double[newSize, 5];
            y0 = new double[newSize, 5];
            y1 = new double[newSize, 5];
            Array.Resize(ref microP, newSize);
            Array.Resize(ref microKs, newSize);
            Array.Resize(ref kdula, newSize);
            Array.Resize(ref macroP, newSize);
            Array.Resize(ref psid, newSize);
        }

        /// <summary> The amount of rainfall intercepted by crop and residue canopies </summary>
        public void SetupThetaCurve(double psiDul, int n, double[] ll15, double[] dul, double[] sat)
        {
            for (int layer = 0; layer <= n; layer++)
            {
                psid[layer] = psiDul;  //- (p%x(p%n) - p%x(layer))

                delk[layer, 0] = (dul[layer] - sat[layer]) / (Math.Log10(-psid[layer]));
                delk[layer, 1] = (ll15[layer] - dul[layer]) / (Math.Log10(-psi_ll15) - Math.Log10(-psid[layer]));
                delk[layer, 2] = -ll15[layer] / (Math.Log10(-psi0) - Math.Log10(-psi_ll15));
                delk[layer, 3] = -ll15[layer] / (Math.Log10(-psi0) - Math.Log10(-psi_ll15));

                mk[layer, 0] = 0.0;
                mk[layer, 1] = (delk[layer, 0] + delk[layer, 1]) / 2.0;
                mk[layer, 2] = (delk[layer, 1] + delk[layer, 2]) / 2.0;
                mk[layer, 3] = delk[layer, 3];

                // First bit might not be monotonic so check and adjust
                double alpha = mk[layer, 0] / delk[layer, 0];
                double beta = mk[layer, 1] / delk[layer, 0];
                double phi = alpha - (Math.Pow(2.0 * alpha + beta - 3.0, 2.0) / (3.0 * (alpha + beta - 2.0)));
                if (phi <= 0)
                {
                    double tau = 3.0 / Math.Sqrt(alpha * alpha + beta * beta);
                    mk[layer, 0] = tau * alpha * delk[layer, 0];
                    mk[layer, 1] = tau * beta * delk[layer, 0];
                }

                m0[layer, 0] = 0.0;
                m1[layer, 0] = 0.0;
                y0[layer, 0] = sat[layer];
                y1[layer, 0] = sat[layer];

                m0[layer, 1] = mk[layer, 0] * (Math.Log10(-psid[layer]) - 0.0);
                m1[layer, 1] = mk[layer, 1] * (Math.Log10(-psid[layer]) - 0.0);
                y0[layer, 1] = sat[layer];
                y1[layer, 1] = dul[layer];

                m0[layer, 2] = mk[layer, 1] * (Math.Log10(-psi_ll15) - Math.Log10(-psid[layer]));
                m1[layer, 2] = mk[layer, 2] * (Math.Log10(-psi_ll15) - Math.Log10(-psid[layer]));
                y0[layer, 2] = dul[layer];
                y1[layer, 2] = ll15[layer];

                m0[layer, 3] = mk[layer, 2] * (Math.Log10(-psi0) - Math.Log10(-psi_ll15));
                m1[layer, 3] = mk[layer, 3] * (Math.Log10(-psi0) - Math.Log10(-psi_ll15));
                y0[layer, 3] = ll15[layer];
                y1[layer, 3] = 0.0;

                m0[layer, 4] = 0.0;
                m1[layer, 4] = 0.0;
                y0[layer, 4] = 0.0;
                y1[layer, 4] = 0.0;
            }
        }
        /// <summary> The amount of rainfall intercepted by crop and residue canopies </summary>
        public void SetupKCurve(int n, double[] ll15, double[] dul, double[] sat, double[] ks, double kdul, double psiDul)
        {
            for (int layer = 0; layer <= n; layer++)
            {
                double b = -Math.Log(psiDul / psi_ll15) / Math.Log(dul[layer] / ll15[layer]);
                microP[layer] = b * 2.0 + 3.0;
                kdula[layer] = Math.Min(0.99 * kdul, ks[layer]);
                microKs[layer] = kdula[layer] / Math.Pow(dul[layer] / sat[layer], microP[layer]);

                double sdul = dul[layer] / sat[layer];
                macroP[layer] = Math.Log10(kdula[layer] / 99.0 / (ks[layer] - microKs[layer])) / Math.Log10(sdul);
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

            return (2 * tCube - 3 * tSqr + 1) * y0[layer, i] + (tCube - 2 * tSqr + t) * m0[layer, i]
                    + (-2 * tCube + 3 * tSqr) * y1[layer, i] + (tCube - tSqr) * m1[layer, i];
        }

        /// <summary> The amount of rainfall intercepted by crop and residue canopies </summary>
        public double SimpleK(int layer, double psiValue, double[] sat, double[] ks)
        {
            //  Purpose
            //      Calculate Conductivity for a given node for a specified suction.

            double s = SimpleS(layer, psiValue, sat);
            double simpleK;

            if (s <= 0.0)
                simpleK = 1e-100;
            else
            {
                double microK = microKs[layer] * Math.Pow(s, microP[layer]);

                if (microKs[layer] >= ks[layer])
                    simpleK = microK;
                else
                {
                    double macroK = (ks[layer] - microKs[layer]) * Math.Pow(s, macroP[layer]);
                    simpleK = microK + macroK;
                }
            }
            return simpleK / 24.0 / 10.0;
        }

        private double SimpleS(int layer, double psiValue, double[] sat)
        {
            //  Purpose
            //      Calculate S for a given node for a specified suction.
            return SimpleTheta(layer, psiValue) / sat[layer];
        }

        /// <summary> The amount of rainfall intercepted by crop and residue canopies </summary>
        public double Suction(int node, double theta, double[] _psi, double psiDul, double[] ll15, double[] dul, double[] sat)
        {
            //  Purpose
            //   Calculate the suction for a given water content for a given node.
            const int maxIterations = 1000;
            const double tolerance = 1e-9;
            const double dpF = 0.01;
            double psiValue;

            if (theta == sat[node])
            {
                if (_psi[node] > 0)
                    return _psi[node];
                else
                    return 0;
            }
            else
            {
                if (MathUtilities.FloatsAreEqual(_psi[node], 0.0))
                    if (theta > dul[node])
                        psiValue = psiDul; // Initial estimate
                    else if (theta < ll15[node])
                        psiValue = psi_ll15;
                    else
                    {
                        double pFll15 = Math.Log10(-psi_ll15);
                        double pFdul = Math.Log10(-psiDul);
                        double frac = (theta - ll15[node]) / (dul[node] - ll15[node]);
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
