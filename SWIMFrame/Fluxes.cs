using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics.LinearAlgebra;
using APSIM.Shared.Utilities;

namespace SWIMFrame
{
    // Calculates flux tables given soil properties and path lengths.
    // Static; there should not be more than one of these per simulation.
    public static class Fluxes
    {
        /// <summary>Store a list of flux tables and their associated soil names and layers.</summary>
        public static Dictionary<string, FluxTable> FluxTables {get;set;}

        public static FluxTable ft;
        static int mx = 100; // max no. of phi values
        static int i, j, ni, ns, nt, nu, nit, nfu, nphif, ip, nfs, ii, ie;
        static int[] iphif = new int[mx+1];
        static int[] ifs = new int[mx+1];
        static double qsmall = 1.0e-5;
        static double rerr = 1.0e-2;
        static double cfac = 1.2;
        static double dh, q1, x, he, Ks, q;
        static double[] hpK = new double[mx + 1];
        static double[] phif = new double[mx + 1];
        static double[] re = new double[mx + 1];
        static double[] phii = new double[mx + 1];
        static double[] phii5 = new double[mx + 1];
        static double[,] aq = new double[mx + 1, mx + 1];
        static double[,] qf = new double[mx + 1, mx + 1];
        static double[,] qi1 = new double[mx + 1, mx + 1];
        static double[,] qi2 = new double[mx + 1, mx + 1];
        static double[,] qi3 = new double[mx + 1, mx + 1];
        static double[,] qi5 = new double[mx + 1, mx + 1];
        static double[,] aKco = new double[mx + 1, 3 + 1];
        static SoilProps sp;

        static StringBuilder diags = new StringBuilder();

        public static void FluxTable(double dz, SoilProps props)
        {
            // Generates a flux table for use by other programs.
            // Assumes soil props available in sp of module soil.
            // dz - path length.

            sp = props;
            ft.fend = new FluxEnd[2];
            nu = sp.nc;
            he = sp.he; Ks = sp.ks;
            for (i = 1; i <= nu - 1; i++)
                for (j = 1; j < sp.Kco.GetLength(0); j++)
                    aKco[i, j] = sp.Kco[j, i];

            // Get K values for Simpson's integration rule in subroutine odef.
            for (i = 1; i <= nu - 1; i++)
            {
                x = 0.5 * (sp.phic[i + 1] - sp.phic[i]);
                hpK[i] = sp.Kc[i] + x * (aKco[i, 1] + x * (aKco[i, 2] + x * aKco[i, 3]));
            }

            // Get fluxes aq(1,:) for values aphi[i] at bottom (wet), aphi(1) at top (dry).
            // These are used to select suitable phi values for flux table.
            // Note that due to the complexity of array indexing in the FORTRAN,
            // we're keeping aq 1 indexed.
            nit = 0;
            aq[1, 1] = sp.Kc[1]; // q=K here because dphi/dz=0
            dh = 2.0; // for getting phi in saturated region
            q1 = (sp.phic[1] - sp.phic[2]) / dz; // q1 is initial estimate
            aq[2, 1] = ssflux(1, 2, dz, q1, 0.1 * rerr); // get accurate flux
            for (j = 3; j <= nu + 20; j++) // 20*dh should be far enough for small curvature in (phi,q)
            {
                if (j > nu) // part satn - set h, K and phi
                {
                    sp.hc[j] = sp.hc[j - 1] + dh * (j - nu);
                    sp.Kc[j] = Ks;
                    sp.phic[j] = sp.phic[j - 1] + Ks * dh * (j - nu);
                }

                // get approx q from linear extrapolation
                q1 = aq[j - 1, 1] + (sp.phic[j] - sp.phic[j - 1]) * (aq[j - 1, 1] - aq[j - 2, 1]) / (sp.phic[j - 1] - sp.phic[j - 2]);
                aq[j, 1] = ssflux(1, j, dz, q1, 0.1 * rerr); // get accurate q
                nt = j;
                ns = nt - nu;
                if (j > nu)
                    if (-(sp.phic[j] - sp.phic[j - 1]) / (aq[j, 1] - aq[j - 1, 1]) < (1 + rerr) * dz)
                        break;
            }

            // Get phi values phif for flux table using curvature of q vs phi.
            // rerr and cfac determine spacings of phif.
            Matrix<double> aqM = Matrix<double>.Build.DenseOfArray(aq);
            i = nonlin(nu, sp.phic.Slice(1, nu), aqM.Column(1).ToArray().Slice(1, nu), rerr);
            re = curv(nu, sp.phic.Slice(1, nu), aqM.Column(1).ToArray().Slice(1, nu));// for unsat phi
            double[] rei = new double[nu - 2 + 1];
            Array.Copy(re.Slice(1, nu - 2).Reverse().ToArray(), 0, rei, 1, re.Slice(1, nu - 2).Reverse().ToArray().Length - 1); //need to 1-index slice JF
            Indices(nu - 2, rei, 1 + nu - i, cfac, out nphif, out iphif);
            int[] iphifReverse = iphif.Skip(1).Take(nphif).Reverse().ToArray();
            int[] iphifReversei = new int[iphifReverse.Length + 1]; 
            Array.Copy(iphifReverse, 0, iphifReversei, 1, iphifReverse.Length); // again, need to 1-index JF
            for (int idx = 1; idx < nphif; idx++)
                iphif[idx] = 1 + nu - iphifReversei[idx]; // locations of phif in aphi
            aqM = Matrix<double>.Build.DenseOfArray(aq); //as above
            re = curv(1 + ns, sp.phic.Slice(nu, nt), aqM.Column(1).ToArray().Slice(nu, nt)); // for sat phi
            Indices(ns - 1, re, ns, cfac, out nfs, out ifs);

            int[] ifsTemp = ifs.Slice(2, nfs);
            for (int idx = nphif + 1; idx <= nphif + nfs - 1; idx++)
                iphif[idx] = nu - 1 + ifsTemp[idx - nphif];
            nfu = nphif; // no. of unsat phif
            nphif = nphif + nfs - 1;
            for (int idx = 1; idx <= nphif; idx++)
            {
                phif[idx] = sp.phic[iphif[idx]];
                qf[idx, 1] = aq[iphif[idx], 1];
            }

            // Get rest of fluxes
            // First for lower end wetter
            for (j = 2; j <= nphif; j++)
                for (i = 2; i <= j; i++)
                {
                    q1 = qf[j, i - 1];
                    if (sp.hc[iphif[j]] - dz < sp.hc[iphif[i]])
                        q1 = 0.0; // improve?
                    qf[j, i] = ssflux(iphif[i], iphif[j], dz, q1, 0.1 * rerr);
                }
            // Then for upper end wetter
            for (i = 2; i <= nphif; i++)
                for (j = i - 1; j >= 1; j--)
                {
                    q1 = qf[j + 1, i];
                    if (j + 1 == i)
                        q1 = q1 + (sp.phic[iphif[i]] - sp.phic[iphif[j]]) / dz;
                    qf[j, i] = ssflux(iphif[i], iphif[j], dz, q1, 0.1 * rerr);
                }
            // Use of flux table involves only linear interpolation, so gain accuracy
            // by providing fluxes in between using quadratic interpolation.
            ni = nphif - 1;
            for (int idx = 1; idx <= ni; idx++)
                phii[idx] = 0.5 * (phif[idx] + phif[idx + 1]);

            Matrix<double> qi1M = Matrix<double>.Build.DenseOfArray(qi1);
            Matrix<double> qfM = Matrix<double>.Build.DenseOfArray(qf);
            double[] qi1Return;
            double[] qi2Return;
            double[] qi3Return;

            for (i = 1; i <= nphif; i++)
            {
                qi1Return = Quadinterp(phif, qfM.Column(i).ToArray(), nphif, phii);
                for (int idx = 1; idx < qi1Return.Length; idx++)
                    qi1[idx, i] = qi1Return[idx];
            }

            for (j = 1; j <= nphif; j++)
            {
                qi2Return = Quadinterp(phif, qfM.Row(j).ToArray(), nphif, phii);
                for (int idx = 1; idx < qi2Return.Length; idx++)
                    qi2[j, idx] = qi2Return[idx];
            }

            for (j = 1; j <= ni; j++)
            {
                qi1M = Matrix<double>.Build.DenseOfArray(qi1);
                qi3Return = Quadinterp(phif, qi1M.Row(j).ToArray(), nphif, phii);
                for (int idx = 1; idx < qi3Return.Length; idx++)
                    qi3[j, idx] = qi3Return[idx];
            }

            // Put all the fluxes together.
            i = nphif + ni;
            for (int row = 1; row <= i; row += 2)
                for (int col = 1; col <= i; col += 2)
                {
                    qi5[col, row] = qf[col / 2 + 1, row / 2 + 1];
                    qi5[col+1, row] = qi1[col / 2 + 1, row / 2 + 1];
                    qi5[col, row + 1] = qi2[col / 2 + 1, row / 2 + 1];
                    qi5[col + 1, row + 1] = qi3[col / 2 + 1, row / 2 + 1];
                }

            // Get accurate qi5(j,j)=Kofphi(phii(ip))
            ip = 0;
            for (j = 2; j <= i; j += 2)
            {
                ip = ip + 1;
                ii = iphif[ip + 1] - 1;
               // if (ii >= sp.Kco.GetLength(1))
               //     ii = sp.Kco.GetLength(1) - 1;
                while (true) // Search down to locate phii position for cubic.
                {
                    if (sp.phic[ii] <= phii[ip])
                        break;
                    ii = ii - 1;
                } 
                x = phii[ip] - sp.phic[ii];
                qi5[j, j] = sp.Kc[ii] + x * (aKco[ii, 1] + x * (aKco[ii, 2] + x * aKco[ii, 3]));
            }

            double[] phii51 = phif.Slice(1, nphif);
            double[] phii52 = phii.Slice(1, ni);
            for (int a = 1; a <= nphif;a++)
            {
                phii5[a * 2 - 1] = phii51[a];
            }

            for (int a = 1; a <= ni; a++)
            {
                phii5[a * 2] = phii52[a];
            }

            // Assemble flux table
            j = 2 * nfu - 1;
            for (ie = 0; ie < 2; ie++)
            {
                ft.fend[ie].phif = new double[phif.Length];
                ft.fend[ie].sid = sp.sid;
                ft.fend[ie].nfu = j;
                ft.fend[ie].nft = i;
                ft.fend[ie].dz = dz;
                ft.fend[ie].phif = phii5; //(1:i) assume it's the whole array
            }
            ft.ftable = qi5; // (1:i,1:i) as above
        }

        /// <summary>
        /// Test harness for setting private variable 'q'
        /// </summary>
        /// <param name="setQ">The q value</param>
        /// <param name="aphi">The sp.phic values</param>
        public static void SetupOdef(double setQ, double[] aphi)
        {
            q = setQ;
            sp.phic = aphi;
        }

        private static double[] odef(int n1, int n2, double[] aK, double[] hpK)
        {
            double[] u = new double[3];
            int np;
            double[] da = new double[n2 - n1 + 2];
            double[] db = new double[n2 - n1 + 1];
            // Get z and dz/dq for flux q and phi from aphi(n1) to aphi(n2).
            // q is global to subroutine fluxtbl.
            np = n2 - n1 + 1;
            double[] daTemp = MathUtilities.Subtract_Value(aK.Slice(n1, n2), q);
            double[] dbTemp = MathUtilities.Subtract_Value(hpK.Slice(n1, n2-1), q);
            diags.AppendLine("n1: " + n1 + Environment.NewLine + "n2: " + n2);

            for (int idx = 1; idx < da.Length; idx++)
            {
                da[idx] = 1.0 / daTemp[idx];
            }

            for (int idx=1;idx<db.Length;idx++)
            {
                db[idx] = 1.0 / dbTemp[idx];
            }

            diags.Append("da: ");
            for (int i = 1; i < da.Length; i++)
                diags.Append(" " + da[i]);
            diags.AppendLine();

            // apply Simpson's rule
            // u[] = sum((aphi(n1+1:n2)-aphi(n1:n2-1))*(da(1:np-1)+4*db+da(2:np))/6). Love the C# implementation. Note aphi is now sp.phic
            u[1] = MathUtilities.Sum(MathUtilities.Divide_Value(MathUtilities.Multiply
                  (MathUtilities.Subtract(sp.phic.Slice(n1 + 1, n2), sp.phic.Slice(n1, n2 - 1)), 
                  MathUtilities.Add(MathUtilities.Add(MathUtilities.Multiply_Value(db, 4), da.Slice(1, np - 1)), da.Slice(2, np))), 6)); // this is madness!
            da = MathUtilities.Multiply(da, da);
            db = MathUtilities.Multiply(db, db);
            u[2] = MathUtilities.Sum(MathUtilities.Divide_Value(MathUtilities.Multiply
                  (MathUtilities.Subtract(sp.phic.Slice(n1 + 1, n2), sp.phic.Slice(n1, n2 - 1)),
                  MathUtilities.Add(MathUtilities.Add(MathUtilities.Multiply_Value(db, 4), da.Slice(1, np - 1)), da.Slice(2, np))), 6));
            return u;
        }

        private static double ssflux(int ia, int ib, double dz, double qin, double rerr)
        {
            // Get steady-state flux
            // ia,ib,iz,dz - table entry (ia,ib,iz) and path length dz
            int maxit = 50;
            int i, it, j, n, n1, n2;
            double dh = 0;
            double dq, ha, hb, Ka, Kb, Ks, q1, q2, qp, v1;
            double[] u = new double[2];
            double[] u0 = new double[2];

            i = ia; j = ib; n = nu;
            if (i == j) // free drainage
            {
                return sp.Kc[i];
            }
            ha = sp.hc[i]; hb = sp.hc[j]; Ka = sp.Kc[i]; Kb = sp.Kc[j];
            if (i >= n && j >= n) // saturated flow
                return Ka * ((ha - hb) / dz + 1.0);

            // get bounds q1 and q2
            // q is global in module
            if (i > j)
            {
                q1 = Ka;
                q2 = 1.0e20;
                q = 1.1 * Ka;
            }
            else
                if (ha > hb - dz)
                {
                    q1 = 0.0;
                    q2 = Ka;
                    q = 0.1 * Ka;
                }
                else
                {
                    q1 = -1.0e20;
                    q2 = 0.0;
                    q = -0.1 * Ka;
                }

            if (qin < q1 || qin > q2)
            {
                //Console.WriteLine("ssflux: qin {0} out of range {1} {2}", qin, q1, q2);
                //Console.WriteLine("at ia, ib = {0} {1}", ia, ib);
            }
            else
                q = qin;

            // integrate from dry to wet - up to satn
            if (i > j)
            {
                v1 = -dz;
                if (i > n)
                {
                    Ks = Ka;
                    dh = ha - he;
                    n1 = ib; n2 = n;
                }
                else
                {
                    n1 = ib; n2 = ia;
                }
            }
            else
            {
                v1 = dz;
                if (j > n)
                {
                    dh = hb - he;
                    n1 = ia;
                    n2 = n;
                }
                else
                {
                    n1 = ia;
                    n2 = ib;
                }
            }
            MathUtilities.Zero(u0); // u(1) is z, u(2) is dz/dq (partial deriv)
            for (it = 1; it < maxit; it++)// bounded Newton iterations to get q that gives correct dz
            {
                u = odef(n1, n2, sp.Kc, hpK);
                if (i > n || j > n) // add sat solns
                {
                    Ks = Math.Max(Ka, Kb);
                    u[1] += Ks * dh / (Ks - q);
                    u[2] += Ks * dh / Math.Pow(Ks - q, 2);
                }

                dq = (v1 - u[1]) / u[2]; // delta z / dz/dq
                qp = q; // save q before updating
                if (dq > 0.0)
                {
                    q1 = q;
                    q = q + dq;

                    if (q >= q2)
                        q = 0.5 * (q1 + q2);
                }
                else
                {
                    q2 = q;
                    q = q + dq;

                    if (q <= q1)
                    {
                        q = 0.5 * (q1 + q2);
                    }
                }

                // convergence test - q can be at or near zero
                double qqp = q - qp;
                if (Math.Abs(qqp) < rerr * Math.Max(Math.Abs(q), Ka) && Math.Abs(u[1] - v1) < rerr * dz || Math.Abs(q1 - q2) < 0.01 * qsmall)
                    break;
            }
            if (it > maxit)
                Console.WriteLine("ssflux: too many iterations {0} {1}", ia, ib);
            nit = nit + it;
            return q;
        }

        // get curvature at interior points of (x,y)
        private static double[] curv(int n, double[] x, double[] y)
        {
            double[] s = new double[n - 1];
            double[] yl = new double[n - 1];

            double[] ySub = MathUtilities.Subtract(y.Slice(3, n), y.Slice(1, n - 2));
            double[] xSub = MathUtilities.Subtract(x.Slice(3, n), x.Slice(1, n - 2));

            s = MathUtilities.Divide(ySub, xSub);
            yl = MathUtilities.Add(y.Slice(1, n-2), 
                                  MathUtilities.Multiply(MathUtilities.Subtract(x.Slice(2, n-1),
                                                                                x.Slice(1, n-2)),
                                                              s));
            double[] ySlice = y.Slice(2, n - 1);
            return MathUtilities.Subtract_Value(MathUtilities.Divide(ySlice, yl), 1);
        }

        // get last point where (x,y) deviates from linearity by < re
        private static int nonlin(int n, double[] x, double[] y, double re)
        {
            int i;
            double s, are;
            double[] yl = new double[n - 1];
            for (i = 3; i <= n; i++)
            {
                s = (y[i] - y[1]) / (x[i] - x[1]);
                double[] xSub = x.Slice(2, i - 1);
                double[] ylSub = yl.Slice(1, i - 2);
                double[] ySub = y.Slice(2, i - 1);
                for (int idx = 1; idx < ylSub.Length; idx++)
                {
                    ylSub[idx] = y[1] + s * (xSub[idx] - x[1]);
                }
                double[] div = MathUtilities.Subtract_Value(MathUtilities.Divide(ySub, ylSub), 1);
                div = div.Skip(1).ToArray();
                for (int idx = 1; idx <= div.Length - 1; idx++)
                    div[idx] = Math.Abs(div[idx]);
                are = MathUtilities.Max(div);
                if (are > re)
                    return i - 1;
            }
            return 0;
        }

        /// <summary>
        /// Test harness for indices; can't use extension method due to presence of ref and out vars.
        /// </summary>
        /// <param name="n">n</param>
        /// <param name="c">c</param>
        /// <param name="iend">iend</param>
        /// <param name="fac">fac</param>
        /// <returns></returns>
        public static KeyValuePair<int, int[]> TestIndices(int n, double[] c, int iend, double fac)
        {
            int nsel;
            int[] isel = new int[n + 2];
            Indices(n, c, iend, fac, out nsel, out isel);
            return new KeyValuePair<int, int[]>(nsel, isel);
        }

        // get indices of elements selected using curvature
        private static void Indices(int n, double[] c, int iend, double fac, out int nsel, out int[] isel)
        {
            int a = 1, b = 1;
            int[] di = new int[n+1];
            isel = new int[100];
            double[] ac = new double[n+1];

            for (int idx = 1; idx < c.Length; idx++)
                ac[idx] = Math.Abs(c[idx]);
            for (int idx = 1; idx < c.Length; idx++)
                di[idx] = (int)Math.Round(fac * MathUtilities.Max(ac) / ac[idx], MidpointRounding.ToEven); // min spacings

            isel[1] = 1; 
            while (true) 
            {
                if (a >= iend)
                    break;
                a++;
                if (a > n)
                    break;
                if (di[a - 1] > 2 && di[a] > 1)
                    a = a + 2; // don't want points to be any further apart
                else if (di[a - 1] > 1)
                    a = a + 1;

                b++;
                isel[b] = a;
            }
            if (isel[b] < n + 2)
            {
                b++;
                isel[b] = n + 2;
            }
            nsel = b;
        }
        // Return quadratic interpolation coeffs co.
        public static double[] Quadco(double[] x, double[] y)
        {
            double[] co = new double[4];
            double s = 1.0 / (x[3] - x[1]);
            double x1 = s * (x[2] - x[1]);
            double y2 = y[3] - y[1];
            double x12 = x1 * x1;
            double c1 = (y[2] - y[1] - x12 * y2) / (x1 - x12);
            double c2 = y2 - c1;
            co[1] = y[1];
            co[2] = s * c1;
            co[3] = s * s * c2;
            return co;
        }

        // Return v(1:n-1) corresponding to u(1:n-1) using quadratic interpolation.
        public static double[] Quadinterp(double[] x, double[] y, int n, double[] u)
        {
            double[] v = new double[100 + 1];
            int i, j, k;
            double z;
            double[] co = new double[4];
            for (k = 1; k <= n; k += 2)
            {
                i = k;
                if (k + 2 > n)
                    i = n - 2;
                co = Quadco(x.Slice(i, i+2), y.Slice(i, i+2));
                for (j = k; j <= i+1; j++)
                {
                    z = u[j] - x[i];
                    v[j] = co[1] + z * (co[2] + z * co[3]);
                }
            }
            return v;
        }

        public static FluxTable ReadFluxTable(string key)
        {
            return FluxTables[key];
        }

        /// <summary>
        /// Public accessor to set a SoilProps object.
        /// Only required for unit testing.
        /// </summary>
        /// <param name="setsp"></param>
        /// <param name="setnu"></param>
        /// <param name="sethpK"></param>
        public static void SetupSsflux(SoilProps setsp, int setnu, double[] sethpK)
        {
            sp = setsp;
            nu = setnu;
            hpK = sethpK;
        }
    }
    
    //  sid - soil ident
    //  nfu, nft - no. of fluxes unsat and total
    //  dz - path length
    //  phif(1:nft) - phi values
    [Serializable]
    public struct FluxEnd
    {
        public int sid, nfu, nft;
        public double[] phif;
        public double dz;
    }

    //  fend(2) - flux end data
    //  qf(1:fend(1)%nft,1:fend(2)%nft) - flux table
    [Serializable]
    public struct FluxTable
    {
        public FluxEnd[] fend;
        public double[,] ftable;
    }
}
