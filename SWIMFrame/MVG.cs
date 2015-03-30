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
