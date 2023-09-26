using System;
using APSIM.Shared.Utilities;
using Models.Core;

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
        private double[] microporePowerTerm;
        private double[] microP;
        private double[] microporeKs;
        private double[] kdula;
        private double[] macroporePowerTerm;
        private double[] psid;
        const double psi_ll15 = -15000.0;  // matric potential at 15 Bar
        const double psiad = -1e6;         // matric potentiral at air dry
        const double psi0 = -0.6e7;        // matric potential at oven dry
        const double psiSat = -1.0;        // matric potential at saturation


        ///<summary>Pore Interaction Index for shape of the K(theta) curve for soil hydraulic conductivity</summary>
        public double[] PoreInteractionIndex { 
            get 
            { 
                return microP; 
            } 
            set 
            { 
                microP = value; 

            } 
        }

        internal void ResizePropfileArrays(int newSize)
        {
            delk = new double[newSize, 4];
            mk = new double[newSize, 4];
            m0 = new double[newSize, 5];
            m1 = new double[newSize, 5];
            y0 = new double[newSize, 5];
            y1 = new double[newSize, 5];
            
            Array.Resize(ref microporePowerTerm, newSize);
            Array.Resize(ref microP, newSize);
            Array.Fill(microP, 1.0);  // give array default value of 1
            Array.Resize(ref microporeKs, newSize);
            Array.Resize(ref kdula, newSize);
            Array.Resize(ref macroporePowerTerm, newSize);
            Array.Resize(ref psid, newSize);
        }

        /// <summary> The amount of rainfall intercepted by crop and residue canopies </summary>
        public void SetupThetaCurve(double psiDul, int n, double[] ll15, double[] dul, double[] sat)
        {
            for (int layer = 0; layer <= n; layer++)
            {
                psid[layer] = psiDul;  //- (p%x(p%n) - p%x(layer))

                delk[layer, 0] = (dul[layer] - sat[layer]) / (Math.Log10(-psid[layer]) - Math.Log10(-psiSat));
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

                m0[layer, 1] = mk[layer, 0] * (Math.Log10(-psid[layer]) - Math.Log10(-psiSat));
                m1[layer, 1] = mk[layer, 1] * (Math.Log10(-psid[layer]) - Math.Log10(-psiSat));
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
                microporePowerTerm[layer] = b * 2.0 + 2.0 + microP[layer];
                kdula[layer] = Math.Min(0.99 * kdul, ks[layer]);
                microporeKs[layer] = kdula[layer] / Math.Pow(dul[layer] / sat[layer], microporePowerTerm[layer]);

                double sdul = dul[layer] / sat[layer];
                macroporePowerTerm[layer] = Math.Log10(kdula[layer] / 99.0 / (ks[layer] - microporeKs[layer])) / Math.Log10(sdul);
            }
        }
        /// <summary> The amount of rainfall intercepted by crop and residue canopies </summary>
        public double SimpleTheta(int layer, double psiValue)
        {
            //  Purpose
            //     Calculate Theta for a given node for a specified suction.
            int i;
            double t;

            if (psiValue >= psiSat)
            {
                return y0[layer, 0];
            }
            else if (psiValue > psid[layer])
            {
                i = 1;
                t = (Math.Log10(-psiValue) - Math.Log10(-psiSat)) / (Math.Log10(-psid[layer]) - Math.Log10(-psiSat));
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
                return (1-t) * y0[layer, 3];
            }
            else
            {
                return 0.0;
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
                double microK = microporeKs[layer] * Math.Pow(s, microporePowerTerm[layer]);

                if (microporeKs[layer] >= ks[layer])
                    simpleK = microK;
                else
                {
                    double macroK = (ks[layer] - microporeKs[layer]) * Math.Pow(s, macroporePowerTerm[layer]);
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

            if (theta == sat[node])
            {
                if (_psi[node] > 0)
                    return _psi[node];
                else
                    return 0;
            }
            else
            {
                double est1 = 0;
                double est2 = 0;
                double bestest = 0;
                double besterr = 0;

                if (theta > dul[node])
                {
                    est1 = Math.Log10(-psiSat); est2 = Math.Log10(-psiDul);
                }
                else if (theta > ll15[node])
                {
                    est1 = Math.Log10(-psiDul); est2 = Math.Log10(-psi_ll15);
                }
                else
                {
                    est1 = Math.Log10(-psi_ll15); est2 = Math.Log10(-psi0);
                }

                // Use secant method to solve for suction
                for (int iter = 0; iter < maxIterations; iter++)
                {
                    double Y1 = SimpleTheta(node, -Math.Pow(10, est1))-theta;
                    double Y2 = SimpleTheta(node, -Math.Pow(10, est2))-theta;

                    double est3 = est2 - Y2 * (est2 - est1)/(Y2 - Y1);
                    double Y3 = SimpleTheta(node, -Math.Pow(10, est3)) - theta;


                    if (Math.Abs(Y3) < tolerance)
                        return -Math.Pow(10, est3);

                    if (Math.Abs(Y3) < besterr)
                    {
                        besterr = Math.Abs(Y3);
                        bestest = est3;
                    }

                    est1 = est2;
                    est2 = est3;


                }
                return -Math.Pow(10, bestest);
                //throw (new Exception("Soil hydraulic properties model failed to find value of suction for given theta"));

            }
        }
    }

}
