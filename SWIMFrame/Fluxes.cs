using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics.LinearAlgebra;
using APSIM.Shared.Utilities;

using System.IO; //debug

namespace SWIMFrame
{
     // Calculates flux tables given soil properties and path lengths.
    public class Fluxes
    {
        public static FluxTable ft = new FluxTable();
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
        static SoilProps sp;
        static FluxEnd pe = new FluxEnd();

        static StringBuilder diags = new StringBuilder();

        /// <summary>
        /// Public accessor to set a SoilProps object.
        /// Only required for unit testing.
        /// </summary>
        /// <param name="soilProps"></param>
        public static void SetupSsflux(SoilProps setsp, int setnu, double[] sethpK)
        {
            sp = setsp;
            nu = setnu;
            hpK = sethpK;
        }

        public static void FluxTable(double dz, SoilProps props)
        {
            // Generates a flux table for use by other programs.
            // Assumes soil props available in sp of module soil.
            // dz - path length.


            //diags - timer start here
            sp = props;
            ft.fend = new FluxEnd[2];
            nu = sp.nc;
            he = sp.he; Ks = sp.ks;

            // Get K values for Simpson's integration rule in subroutine odef.
            for (i = 1; i <= nu - 1; i++)
            {
                x = 0.5 * (sp.phic[i + 1] - sp.phic[i]);
                hpK[i] = sp.Kc[i] + x * (sp.Kco[0, i] + x * (sp.Kco[1, i] + x * sp.Kco[2, i]));
            }

            // Get fluxes aq(1,:) for values aphi[i] at bottom (wet), aphi(1) at top (dry).
            // These are used to select suitable phi values for flux table.
            // Note that due to the complexity of array indexing in the FORTRAN,
            // we're keeping aq 1 indexed.
            nit = 0;
            aq[1, 1] = sp.Kc[1]; // q=K here because dphi/dz=0
            dh = 2.0; // for getting phi in saturated region
            q1 = (sp.phic[1] - sp.phic[2]) / dz; // q1 is initial estimate
            aq[1, 2] = ssflux(1, 2, dz, q1, 0.1 * rerr); // get accurate flux
            for (j = 3; j <= nu + 20; j++) // 20*dh should be far enough for small curvature in (phi,q)
            {
                if (j > nu) // part satn - set h, K and phi
                {
                    sp.hc[j] = sp.hc[j - 1] + dh * (j - nu);
                    sp.Kc[j] = Ks;
                    sp.phic[j] = sp.phic[j - 1] + Ks * dh * (j - nu);
                }

                // get approx q from linear extrapolation
                q1 = aq[1, j - 1] + (sp.phic[j] - sp.phic[j - 1]) * (aq[1, j - 1] - aq[1, j - 2]) / (sp.phic[j - 1] - sp.phic[j - 2]);
                aq[1, j] = ssflux(1, j, dz, q1, 0.1 * rerr); // get accurate q
                nt = j;
                ns = nt - nu;
                if (j > nu)
                    if (-(sp.phic[j] - sp.phic[j - 1]) / (aq[1, j] - aq[1, j - 1]) < (1 + rerr) * dz)
                        break;
            }

            // Get phi values phif for flux table using curvature of q vs phi.
            // rerr and cfac determine spacings of phif.
            Matrix<double> aqM = Matrix<double>.Build.DenseOfArray(aq);
            i = nonlin(nu, sp.phic.Slice(1, nu), aqM.Row(1).ToArray().Slice(1, nu), rerr);
             re = curv(nu, sp.phic.Slice(1, nu), aqM.Row(1).ToArray().Slice(1, nu));// for unsat phi
            indices(nu - 2, re.Slice(1,nu-2).Reverse().ToArray(), 1 + nu - i, cfac, out nphif, out iphif);
            int[] iphifReverse = iphif.Take(nphif).Reverse().ToArray();
            for (int idx = 0; idx < nphif; idx++)
                iphif[idx] = 1 + nu - iphifReverse[idx]; // locations of phif in aphi
            aqM = Matrix<double>.Build.DenseOfArray(aq); //as above
            re = curv(1 + ns, sp.phic.Slice(nu, nt), aqM.Row(1).ToArray().Slice(nu, nt)); // for sat phi
            indices(ns - 1, re, ns, cfac, out nfs, out ifs);

            for (int idx = nphif + 1; idx <= nphif + nfs - 1; idx++)
                iphif[idx] = nu - 1 + ifs[idx]; //TODO debug this mess
            nfu = nphif; // no. of unsat phif
            nphif = nphif + nfs - 1;
            for (int idx = 1; idx <= nphif; idx++)
            {
                phif[idx] = sp.phic[iphif[idx]];
                qf[1, idx] = aq[1, iphif[idx]];
            }
            // Get rest of fluxes
            // First for lower end wetter
            for (j = 2; j <= nphif; j++)
                for (i = 2; i <= j; i++)
                {
                    q1 = qf[i, j + 1];
                    if (sp.hc[iphif[j]] - dz < sp.hc[iphif[i]])
                        q1 = 0.0; // improve?
                    qf[i, j] = ssflux(iphif[i], iphif[j], dz, q1, 0.1 * rerr);
                }
            // Then for upper end wetter
            for (i = 2; i <= nphif; i++)
                for (j = i - 1; j > 1; j--)
                {
                    q1 = qf[i, j + 1];
                    if (j + 1 == i)
                        q1 = q1 + (sp.phic[iphif[i]] - sp.phic[iphif[j]]) / dz;
                    qf[i, j] = ssflux(iphif[i], iphif[j], dz, q1, 0.1 * rerr);
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
                qi1Return = quadinterp(phif, qfM.Row(i).ToArray(), nphif, phii);
                for (int idx = 1; idx < qi1Return.Length; idx++)
                    qi1[i, idx] = qi1Return[idx];
            }

            for (j = 1; j <= nphif; j++)
            {
                qi2Return = quadinterp(phif, qfM.Column(j).ToArray(), nphif, phii);
                for (int idx = 1; idx < qi2Return.Length; idx++)
                    qi2[idx, i] = qi2Return[idx];
            }

            for (j = 1; j <= ni; j++)
            {
                qi1M = Matrix<double>.Build.DenseOfArray(qi1);
                qi3Return = quadinterp(phif, qi1M.Column(j).ToArray(), nphif, phii);
                for (int idx = 1; idx < qi3Return.Length; idx++)
                    qi3[idx, i] = qi3Return[idx];
            }

            // Put all the fluxes together.
            i = nphif + ni;
            for (int iidx = 1; iidx <= i; iidx += 2)
                for (int npidx = 1; npidx < nphif; npidx++)
                    for (int niidx = 1; niidx < ni; niidx++)
                    {
                        qi5[iidx, iidx] = qf[npidx, npidx];
                        qi5[iidx, iidx + 1] = qi1[npidx, niidx];
                        qi5[iidx + 1, iidx] = qi2[niidx, npidx];
                        qi5[iidx + 1, iidx + 1] = qi3[niidx, niidx];
                    }

            // Get accurate qi5(j,j)=Kofphi(phii(ip))
            ip = 0;
            for (j = 2; j <= i; j += 2)
            {
                ip = ip + 1;
                ii = iphif[ip + 1] - 1;
                if (ii >= sp.Kco.GetLength(1))
                    ii = sp.Kco.GetLength(1) - 1;
                do // Search down to locate phii position for cubic.
                {
                    if (sp.phic[ii] <= phii[ip])
                        break;
                    ii = ii - 1;
                } while (true);
                x = phii[ip] - sp.phic[ii];
                qi5[j, j] = sp.Kc[ii] + x * (sp.Kco[0, ii] + x * (sp.Kco[1, ii] + x * sp.Kco[2, ii]));
            }

            double[] phii51 = phif.Slice(1, nphif);
            double[] phii52 = phii.Slice(1, ni);
            for (int i = 1;i <= nphif;i++)
            {
                phii5[i * 2] = phii51[i];
            }

            for (int i = 1; i <= ni; i++)
            {
                phii5[1 + i * 2] = phii52[i];
            }

            // diags - end timer here


            // Assemble flux table
            j = 2 * nfu - 1;
            for (ie = 0; ie < 2; ie++)
            {
                pe = ft.fend[ie];
                pe.phif = new double[phif.Length];
                pe.sid = sp.sid;
                pe.nfu = j;
                pe.nft = i;
                pe.dz = dz;
                pe.phif = phii5; //(1:i) assume it's the whole array
            }
            ft.ftable = qi5; // (1:i,1:i) as above
        }

        /// <summary>
        /// Test harness for setting private variable 'q'
        /// </summary>
        /// <param name="q">The q value</param>
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
       //     if (double.IsNaN(u[0]) || double.IsNaN(u[1]))
       //         throw new Exception();
            return u;
        }

        public static void WriteDiags()
        {
            File.WriteAllText("C:\\temp\\NETout.txt", diags.ToString());
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
                return sp.Kc[i-1];
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
                Console.WriteLine("ssflux: qin ", qin, " out of range ", q1, q2);
                Console.WriteLine("at ia, ib = ", ia, ib);
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
            u0 = new double[] {0.0, 0.0, 0.0 }; // u(1) is z, u(2) is dz/dq (partial deriv)
            //write (*,*) q1,q,q2
            for (it = 1; it < maxit; it++)// bounded Newton iterations to get q that gives correct dz
            {
                u = u0; //point?
                u = odef(n1, n2, sp.Kc, hpK);
                //write (*,*) it,q,u(1),u(2)
                if (i > n || j > n) // add sat solns
                {
                    Ks = Math.Max(Ka, Kb);
                    u[1] += Ks * dh / (Ks - q);
                    u[2] += Ks * dh / Math.Pow(Ks - q, 2);
                }

                /*/ this is where the deviation occurs.
                 * The numbers here are correct to 7 dp (single precision from FORTRAN)
                 * The problem is that we have a small number divided by a much larger number
                 * which introduces error. This may or may not be a problem.
                /*/
                dq = (v1 - u[1]) / u[2]; // delta z / dz/dq
                qp = q; // save q before updating
                if (dq > 0.0)
                {
                    q1 = q;
                    q = q + dq;
                }
                if (q >= q2)
                    q = 0.5 * (q1 + q2);
                else
                {
                    q2 = q;
                    q = q + dq;
                }
                if (q <= q1)
                {
                    q = 0.5 * (q1 + q2);
                }

                // convergence test - q can be at or near zero
                if (Math.Abs(q - qp) < rerr * Math.Max(Math.Abs(q), Ka) && Math.Abs(u[0] - v1) < rerr * dz || Math.Abs(q1 - q2) < 0.01 * qsmall)
                    break;
            }
            if (it > maxit)
                Console.WriteLine("ssflux: too many iterations", ia, ib);
            nit = nit + it;
            // Possible diversion here. Numbers are out by about 0.2%. Appears to be a multiplicative issue from other functions due
            // to floating point differences between FORTRAN and C#.
            return q;
        }

        // get curvature at interior points of (x,y)
        private static double[] curv(int n, double[] x, double[] y)
        {
            double[] c = new double[n - 1];
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
            double[] div = MathUtilities.Divide(ySlice, yl);
            double[] re = MathUtilities.Subtract_Value(div, 1);
            return MathUtilities.Subtract_Value(MathUtilities.Divide(ySlice, yl), 1);
        }

        // get last point where (x,y) deviates from linearity by < re
        private static int nonlin(int n, double[] x, double[] y, double re)
        {
            int nonlin, i;
            double s, are;
            double[] yl = new double[n - 1];
            nonlin = n;
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
                {
                    return i - 1;
                }
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
        /// <param name="isel">isel</param>
        /// <returns></returns>
        public static KeyValuePair<int, int[]> TestIndices(int n, double[] c, int iend, double fac)
        {
            int nsel;
            int[] isel = new int[n + 2];
            indices(n, c, iend, fac, out nsel, out isel);
            return new KeyValuePair<int, int[]>(nsel, isel);
        }

        // get indices of elements selected using curvature
        private static void indices(int n, double[] c, int iend, double fac, out int nsel, out int[] isel)
        {
            int i, j;
            int[] di = new int[n+1];
            isel = new int[100];
            double[] ac = new double[n+1];

            for (int idx = 1; idx < c.Length; idx++)
            {
                ac[idx] = Math.Abs(c[idx]);
            }
            for (int idx = 1; idx < c.Length; idx++)
            {
                di[idx] = (int)Math.Round(fac * MathUtilities.Max(ac) / ac[idx], MidpointRounding.ToEven); // min spacings
            }
            isel[1] = 1; i = 1; j = 1;
            while (true) //will want to change this
            {
                if (i >= iend)
                    break;
                i++;
                if (i > n)
                    break;
                if (di[i - 1] > 2 && di[i] > 1)
                    i = i + 2; // don't want points to be any further apart
                else if (di[i - 1] > 1)
                    i = i + 1;

                j++;
                isel[j] = i;
            }
            if (isel[j] < n + 2)
            {
                j++;
                isel[j] = n + 2;
            }
            nsel = j;
        }
        // Return quadratic interpolation coeffs co.
        public static double[] quadco(double[] x, double[] y)
        {
            double[] co = new double[4];
            double s, x1, y2, x12, c1, c2;
            s = 1.0 / (x[3] - x[1]);
            x1 = s * (x[2] - x[1]);
            y2 = y[3] - y[1];
            x12 = x1 * x1;
            c1 = (y[2] - y[1] - x12 * y2) / (x1 - x12);
            c2 = y2 - c1;
            co[1] = y[1];
            co[2] = s * c1;
            co[3] = s * s * c2;
            return co;
        }

        // Return v(1:n-1) corresponding to u(1:n-1) using quadratic interpolation.
        private static double[] quadinterp(double[] x, double[] y, int n, double[] u)
        {
            double[] v = new double[100];
            int i, j, k;
            double z;
            double[] co = new double[4];
            for (k = 1; k <= n; k += 2)
            {
                i = k;
                if (k + 2 > n)
                    i = n - 2;
                co = quadco(x.Slice(i, i+2), y.Slice(i, i+2));
                for (j = k; j < i+1; j++)
                {
                    z = u[j] - x[i];
                    v[j] = co[1] + z * (co[2] + z * co[3]);
                }
            }
            return v;
        }

        public void TestFluxs()
        {
            double[] aK =  new double[] { 8.740528E-10,3.148991E-09,1.116638E-08,3.906024E-08,1.350389E-07,4.621461E-07,1.567779E-06,5.278070E-06,1.765091E-05,5.868045E-05,1.940329E-04,6.381824E-04,2.086113E-03,6.757548E-03,2.152482E-02,6.618264E-02,1.887549E-01,4.655217E-01,9.153457E-01,1.393520E+00,1.733586E+00,1.916091E+00,2.000000E+00,0.000000E+00,0.000000E+00,
                                          0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,
                                          0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,
                                          0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00};
            double[] hpK = new double[] { 1.942348E-09,6.760092E-09,2.390674E-08,8.260039E-08,2.837631E-07,9.641152E-07,3.252644E-06,1.089420E-05,3.627295E-05,1.201039E-04,3.956002E-04,1.295509E-03,4.209049E-03,1.348672E-02,4.200805E-02,1.232292E-01,3.212703E-01,6.904247E-01,1.165940E+00,1.578200E+00,1.834724E+00,1.963039E+00,0.000000E+00,0.000000E+00,0.000000E+00,
                                          0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,
                                          0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,
                                          0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00,0.000000E+00 };
            double[] odefOut = odef(1, 2, aK, hpK);
        }
    }


    //  sid - soil ident
    //  nfu, nft - no. of fluxes unsat and total
    //  dz - path length
    //  phif(1:nft) - phi values
    public struct FluxEnd
    {
        public int sid, nfu, nft;
        public double[] phif;
        public double dz;
    }

    //  fend(2) - flux end data
    //  qf(1:fend(1)%nft,1:fend(2)%nft) - flux table
    public struct FluxTable
    {
        public FluxEnd[] fend;
        public double[,] ftable;
    }
}
