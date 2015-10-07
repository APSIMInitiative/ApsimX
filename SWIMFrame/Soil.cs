using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics.LinearAlgebra;

namespace SWIMFrame
{
/*  ! Generates soil property tables from functions in module hyfuns.
    ! If K functions not provided, calculates K from the Mualem model
    ! K=Ks*Integral(dS'/h,S'=0,S)/Integral(dS'/h,S'=0,1) using numerical
    ! integration, with dS=dS/dh*dh/dlnh*dlnh=dS/dh*h*dlnh, where lnh=ln(-h).
    ! See type soilprops.
    ! sid                - soil ident. no.
    ! ths, Ks, he        - soil params.
    ! hd                 - driest h (usually -1e7).
    ! p                  - pore interaction param giving S**p factor for K.
    ! Kgiven             - .true. if functions giving K provided, else .false.
    ! Sofh(h)            - function giving S, used if K functions given.
    ! Sdofh(h,S,dSdh)    - subroutine giving S and dSdh, used if K calculated.
    ! Kofh(h),KofhS(h,S) - functions giving K.
    ! Work in cm and hours.
 */
    public static class Soil
    {
        /* ! soilprops - derived type definition for properties.
         ! gensptbl  - subroutine to generate the property values.
         ! sp        - variable of type soilprops containing the properties.
        */
        static int nliapprox = 70; // approx no. of log intervals
        static double qsmall = 1.0e-5; // smaller fluxes negligible
        static double vhmax = -10000; // for vapour - rel humidity > 0.99 at vhmax
        static int nhpd = 5; // no. of h per decade from hd to vhmax
        static double dSdh, hdry, hwet, dlhr, dlh, lhd, phidry, phie, x;
        // The above parameters can be varied, but the defaults should usually be ok.

        //subroutine to generate the property values.
        public static SoilProps gensptbl(double dzmin, SoilParam sPar, bool Kgiven)
        {
            int nlimax = 220;
            int i, j, nli1, nc, nld;
            double[] h= new double[nlimax + 3];
            double[] lhr= new double[nlimax + 3];
            double[] K = new double[nlimax + 3];
            double[] phi = new double[nlimax + 3];
            double[] S = new double[nlimax + 3];
            double[] cco = new double[4];

            //diags - timing starts here

            SoilProps sp = new SoilProps();
            sp.sid = sPar.sid; sp.ths = sPar.ths; sp.ks = sPar.ks;
            sp.he = sPar.he; sp.phie = phie; sp.S = S; sp.n = sPar.layers;

            // Find start of significant fluxes.
            hdry = sPar.hd; // normally -1e7 cm
            phidry = 0.0;
            hwet = Math.Min(sPar.he, -1.0); // h/hwet used for log spacing

            if (Kgiven)
            {
                dlh = Math.Log(10.0) / 3.0; // three points per decade
                x = Math.Exp(-dlh); // x*x*x=0.1
                h[0] = hdry;
                K[0] = MVG.Kofh(h[0]);
                phi[0] = 0.0;
                for (i = 1; i < nlimax; i++) // should exit well before nlimax
                {
                    h[i] = x * h[i - 1];
                    K[i] = MVG.Kofh(h[i]);
                    // Get approx. phi by integration using dln(-h).
                    phi[i] = phi[i - 1] - 0.5 * (K[i] * h[i] - K[i - 1] * h[i - 1]) * dlh;
                    if (phi[i] > qsmall * dzmin)
                        break; // max flux is approx (phi-0)/dzmin
                }
                if (i > nlimax)
                {
                    Console.WriteLine("gensptbl: start of significant fluxes not found");
                    Environment.Exit(1);
                }
                hdry = h[i - 1];
                phidry = phi[i - 1];
            }
            else
            {
                // Calculate K and find start of significant fluxes.
                Props(ref sp, hdry, phidry, lhr, h, Kgiven);
                for (i = 2; i < sp.n; i++)
                    if (phi[i] > qsmall * dzmin)
                        break;
                if (i > sp.n)
                {
                    Console.WriteLine("gensptbl: start of significant fluxes not found");
                    Environment.Exit(1);
                }
                i = i - 1;
                hdry = h[i];
                phidry = phi[i];
            }

            // hdry and phidry are values where significant fluxes start.
            // Get props.
            sp.Kc = new double[sp.n];
            Props(ref sp, hdry, phidry, lhr,h,Kgiven);
            // Get ln(-h) and S values from dryness to approx -10000 cm.
            // These needed for vapour flux (rel humidity > 0.99 at -10000 cm).
            // To have complete S(h) coverage, bridge any gap between -10000 and h[1].
            x = Math.Log(-Math.Max(vhmax, h[0]));
            lhd = Math.Log(-sPar.hd);
            dlh = Math.Log(10.0) / nhpd; // nhpd points per decade
            nli1 = (int)Math.Round((lhd - x) / dlh, 0);
            nld = nli1 + 1;
            nc = 1 + sp.n / 3; // n-1 has been made divisible by 3
            // fill out the rest of the structure.
             sp.nld = nld; sp.nc = nc;
             sp.h = h; 
             sp.lnh = new double[nld+1];
             sp.Sd = new double[nld+1];
             sp.Kco = new double[3, nc];
             sp.phico = new double[3, nc];
             sp.Sco = new double[3, nc];

            // Store Sd and lnh in sp.
            sp.lnh[1] = lhd;
            for (j = 2; j <= nli1; j++)
                sp.lnh[j] = lhd - dlh * j;

            if (Kgiven)
            {
                sp.Sd[1] = MVG.Sofh(sPar.hd);
                for (j = 2; j <= nld; j++)
                {
                    x = sp.lnh[j];
                    sp.Sd[j] = MVG.Sofh(-Math.Exp(x));
                }
            }
            else
            {
                MVG.Sdofh(sPar.hd, out x, out dSdh);
                sp.Sd[1] = x;
                for (j = 2; j <= nld; j++)
                {
                    x = sp.lnh[j];
                    MVG.Sdofh(-Math.Exp(x), out x, out dSdh);
                    sp.Sd[j] = x;
                }
            }

            // Get polynomial coefficients.
            j = 0;
            sp.Sc = new double[sp.n+1];
            sp.hc = new double[sp.n+1];
            sp.phic = new double[sp.n+1];
            Matrix<double> KcoM = Matrix<double>.Build.DenseOfArray(sp.Kco);
            Matrix<double> phicoM = Matrix<double>.Build.DenseOfArray(sp.phico);
            Matrix<double> ScoM = Matrix<double>.Build.DenseOfArray(sp.Sco);
            for (i = 1; i <= sp.n; i += 3)
            {
                j = j + 1;
                sp.Sc[j] = S[i];
                sp.hc[j] = h[i];
                sp.Kc[j] = sp.K[i];
                sp.phic[j] = sp.phi[i];
                if (i == sp.n)
                    break;

                cco = Cuco(sp.phi.Slice(i,i+3), sp.K.Slice(i, i+3));
                double[] temp = cco.Slice(2, 4).Skip(1).ToArray();
                KcoM.SetColumn(j, cco.Slice(2, 4).Skip(1).ToArray());
                sp.Kco = KcoM.ToArray();

                cco = Cuco(sp.S.Slice(i, i + 3), sp.phi.Slice(i, i + 3));
                phicoM.SetColumn(j, cco.Slice(2, 4).Skip(1).ToArray());
                sp.phico = phicoM.ToArray();

                cco = Cuco(sp.phi.Slice(i, i + 3), sp.S.Slice(i, i + 3));
                ScoM.SetColumn(j, cco.Slice(2, 4).Skip(1).ToArray());
                sp.Sco = ScoM.ToArray();
            }
            // diags - end timing
            return sp;
        }

        private static void Props(ref SoilProps sp, double hdry, double phidry, double[] lhr, double[] h, bool Kgiven)
        {
            int i, j, nli;
            double[] g = new double[201];
            double[] dSdhg = new double[201];

            j = 2 * (nliapprox / 6); // an even number
            nli = 3 * j; //nli divisible by 2 (for integrations) and 3 (for cubic coeffs)
            if (sp.he > hwet)
                nli = 3 * (j + 1) - 1; // to allow for extra points
            dlhr = -Math.Log(hdry / hwet) / nli; // even spacing in log(-h)

            double[] slice = lhr.Slice(1, nli + 1);
            for (int idx = nli; idx > 0; idx--)    //
                slice[idx] = -idx * dlhr;              // will need to check this, fortran syntax is unknown: lhr(1:nli+1)=(/(-i*dlhr,i=nli,0,-1)/)
            Array.Reverse(slice);
            Array.Copy(slice, 0, lhr, 0, slice.Length);
            for (int idx = 1; idx <= nli + 1; idx++)
                h[idx] = hwet * Math.Exp(lhr[idx]);

            if (sp.he > hwet)  // add extra points
            {
                sp.n = nli + 3;
                h[sp.n - 1] = 0.5 * (sp.he + hwet);
                h[sp.n] = sp.he;
            }
            else
                sp.n = nli + 1;

            sp.K = new double[sp.n+1];
            sp.Kc = new double[sp.n+1];
            sp.phi = new double[sp.n+1];

            if (Kgiven)
            {
                for (i = 1; i <= sp.n; i++)
                {

                    sp.S[i] = MVG.Sofh(h[i]);
                    sp.K[i] = MVG.KofhS(h[i], sp.S[i]);
                }
               sp.S[sp.n] = MVG.Sofh(h[sp.n]);
            }
            else // calculate relative K by integration using dln(-h)
            {

                for (i = 1; i <= sp.n; i++)
                    MVG.Sdofh(h[i], out sp.S[i], out dSdhg[i]);

                g[1] = 0;

                for (i = 2; i <= nli; i += 2)  // integrate using Simpson's rule
                    g[i + 1] = g[i - 1] + dlhr * (dSdhg[i - 1] + 4.0 * dSdhg[i] + dSdhg[i + 1]) / 3.0;

                g[2] = 0.5 * (g[0] + g[2]);

                for (i = 3; i <= nli - 1; i += 2)
                    g[i + 1] = g[i - 1] + dlhr * (dSdhg[i - 1] + 4.0 * dSdhg[i] + dSdhg[i + 1]) / 3.0;

                if (sp.he > hwet)
                {
                    g[sp.n] = g[sp.n - 2] + (h[sp.n] - h[sp.n - 1]) * (dSdhg[sp.n - 2] / h[sp.n - 2] + 4.0 * dSdhg[sp.n - 1] / h[sp.n - 1]) / 3.0;
                    g[sp.n - 1] = g[sp.n]; // not accurate, but K[sp.n-1] will be discarded
                }

                for (i = 1; i <= sp.n; i++)
                    sp.K[i] = sp.ks * Math.Pow(sp.S[i], MVG.GetP()) * Math.Pow(g[i] / g[sp.n], 2);
            }

            // Calculate phi by integration using dln(-h).
            sp.phi[1] = phidry;

            for (i = 2; i <= nli; i += 2) // integrate using Simpson's rule
                sp.phi[i + 1] = sp.phi[i - 1] + dlhr * (sp.K[i - 1] * h[i - 1] + 4.0 * sp.K[i] * h[i] + sp.K[i + 1] * h[i + 1]) / 3.0;

            sp.phi[2] = 0.5 * (sp.phi[1] + sp.phi[3]);

            for (i = 3; i <= nli - 1; i += 2)
                sp.phi[i + 1] = sp.phi[i - 1] + dlhr * (sp.K[i - 1] * h[i - 1] + 4.0 * sp.K[i] * h[i] + sp.K[i + 1] * h[i + 1]) / 3.0;

            if (sp.he > hwet)  // drop unwanted point
            {
                sp.phi[sp.n - 1] = sp.phi[sp.n - 2] + (h[sp.n] - h[sp.n - 1]) * (sp.K[sp.n - 2] + 4.0 * sp.K[sp.n - 1] + sp.K[sp.n]) / 3.0;
                h[sp.n - 1] = h[sp.n];
                sp.S[sp.n - 1] = sp.S[sp.n];
                sp.K[sp.n - 1] = sp.K[sp.n];
                sp.n = sp.n - 1;
            }
            phie = sp.phi[sp.n];
            sp.phie = phie;
        }

        private static double[] Cuco(double[] x, double[] y)
        {
            // Get coeffs of cubic through (x,y)
            double s, x1, x2, y3, x12, x13, x22, x23, a1, a2, a3, b1, b2, b3, c1, c2, c3;
            double[] co = new double[5];

            s = 1.0 / (x[4] - x[1]);
            x1 = s * (x[2] - x[1]);
            x2 = s * (x[3] - x[1]);
            y3 = y[4] - y[1];
            x12 = x1 * x1;
            x13 = x1 * x12;
            x22 = x2 * x2;
            x23 = x2 * x22;
            a1 = x1 - x13;
            a2 = x12 - x13;
            a3 = y[2] - y[1] - x13 * y3;
            b1 = x2 - x23;
            b2 = x22 - x23;
            b3 = y[3] - y[1] - x23 * y3;
            c1 = (a3 * b2 - a2 * b3) / (a1 * b2 - a2 * b1);
            c2 = (a3 - a1 * c1) / a2;
            c3 = y3 - c1 - c2;
            co[1] = y[1];
            co[2] = s * c1;
            co[3] = s * s * c2;
            co[4] = s * s * s * c3;
            return co;
        }
    }

    public class SoilParam
    {
        public int layers;
        public int sid; // arbitrary soil ident
        public double hd = -1e7;
        public double ths;
        public double ks;
        public double he;
        public double hg;
        public double mn;
        public double en;
        public double em;
        public double p;
        public SoilProps sp = new SoilProps();

        public SoilParam(int layers, int sid, double ths, double ks, double he, double hg, double mn, double p)
        {
            this.layers = layers;
            this.sid = sid;
            this.ths = ths;
            this.ks = ks;
            this.he = he;
            this.hg = hg;
            this.mn = mn;
            this.p = p;

            en = 2.0 + mn;
            em = mn / en;
        }
    }

    [Serializable]
    public struct SoilProps
    {
        /*! Sd and lnh - 1:nld
          ! S, h, K, phi - 1:n
          ! Sc, hc, Kc, phic - 1:nc
          ! Kco, phico - 1:3,1:nc-1
          ! S(1:n:3) <=> Sc(1:nc) etc.
          ! Kco are cubic coeffs for K of phi, phico for phi of S, Sco for S of phi
          ! e.g. K=Kc[i]+x*(Kco(1,i)+x*(Kco(2,i)+x*Kco(3,i))) where x=phi-phic[i]
          ! S[n]=1, h[n]=he, K[n]=Ks, phi[n]=phie
          ! phi is matric flux potential (Kirchhoff transform), used for flux tables
        */
        public int sid, nld, n, nc;

        public double ths, ks, he, phie;
        public double[] Sd, lnh, S, h, K, phi, Sc, hc, Kc, phic;
        public double[,] Kco, phico, Sco;
    }
}
