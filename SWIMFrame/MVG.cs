using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SWIMFrame
{
    /*
     * Uses van Genuchten S(h) modified to be 1 at he and zero at hd:
     * S(h)=(f(h)-f(hd))/(f(he)-f(hd)), where f(h)=(1+(h/hg**n)**(-m).
     * K can either be of the Brooks-Corey form, K=S**eta, or it can be
     * calculated from the Mualem model using numerical integration.
     * Refer to these as the modified van Genuchten Brooks-Corey (MVGBC)
     * model and the modified van Genuchten Mualem (MVGM) model.
     * Work in cm and hours.
     */
    public static class MVG
    {
        static int sid; // soil ident
        static double ths, Ks, he, hd, p, hg, m, n, eta, fhe, fhd;

        // Set hydraulic params.
        // sid1      - soil ident (arbitrary identifier).
        // ths1 etc. - MVGBC or MVGM params.
        public static void Params(int sid1, double ths1, double Ks1, double he1, double hd1, double p1, double hg1, double m1, double n1)
        {
            sid = sid1; ths = ths1; Ks = Ks1; he = he1; hd = hd1; p = p1; hg = hg1; m = m1; n = n1;
            eta = 2.0 / (m * n) + 2.0 + p;
            fhe = Math.Pow(1.0 + Math.Pow(he / hg, n), -m);
            fhd = Math.Pow(1.0 + Math.Pow(hd / hg, n), -m);
        }

        /// <summary>
        /// Helper function for unit testing. Only tests values that are modified by Params
        /// </summary>
        /// <param name="etaTest">Expected eta</param>
        /// <param name="fheTest">Expected fhe</param>
        /// <param name="fhdTest">Expected fhd</param>
        /// <returns></returns>
        public static bool TestParams(int sidTest, double etaTest, double fheTest, double fhdTest)
        {
            if (sidTest==103)
            {
                ths = 0.4;
                Ks = 2.0;
                he = -2.0;
                hd = -10000000.0;
                p = 1.0;
                hg = -10.0;
                m = 0.14285714285714285;
                n = 2.3333333333333335;
            }
            else
            {
                ths = 0.6;
                Ks = 0.2;
                he = -2.0;
                hd = -10000000.0;
                p = 1.0;
                hg = -40.0;
                m = 5.26315789473684181E-002;
                n = 2.1111111111111112;
            }

            Params(sidTest, ths, Ks, he, hd, p, hg, m, n);

            if (etaTest == eta && fheTest == fhe && fhdTest == fhd)
                return true;
            else
                return false;
        }

        public static double Sofh(double h)
        {
            double f;
            if (h < he)
            {
                f = Math.Pow(1.0 + Math.Pow(h / hg, n), -m);
                return (f - fhd) / (fhe - fhd);
            }
            else
                return 1.0;
        }

        public static double GetP()
        {
            return p;
        }

        // Used instead of Sofh when K to be calculated.
        public static void Sdofh(double h, out double S, out double dSdh)
        {
            double dfdh, f, v, vn;
            if (h < he)
            {
                v = h / hg;
                vn = Math.Pow(v, n);
                f = Math.Pow(1.0 + vn, -m);
                dfdh = m * n * vn * f / (-hg * v * (1.0 + vn));
                S = (f - fhd) / (fhe - fhd);
                dSdh = dfdh / (fhe - fhd);
            }
            else
            {
                S = 1.0;
                dSdh = 0.0;
            }
        }

        public static double KofhS(double h, double S)
        {
            return Math.Pow(Ks * S, eta);
        }

        public static double Kofh(double h)
        {
            return Math.Pow(Ks * Sofh(h), eta);
        }
    }
}
