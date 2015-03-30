using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics.LinearAlgebra;

namespace SWIMFrame
{
     // Calculates flux tables given soil properties and path lengths.
    public class Fluxes
    {
        public static FluxTable ft = new FluxTable();
        SoilProps sp;
        static int mx = 100; // max no. of phi values
        static int i, j, ni, ns, nt, nu, nit, nfu, nphif, ip, nfs, ii, ie;
        static int[] iphif = new int[mx];
        static int[] ifs = new int[mx];
        static double qsmall = 1.0e-5;
        static double rerr = 1.0e-2;
        static double cfac = 1.2;
        static double dh, q1, x, he, Ks, q;
        static double[] ah = new double[mx];
        static double[] aK = new double[mx];
        static double[] aphi = new double[mx];
        static double[] hpK = new double[mx];
        static double[] aS = new double[mx];
        static double[] phif = new double[mx];
        static double[] re = new double[mx];
        static double[] phii = new double[mx];
        static double[] phii5 = new double[mx];
        static double[,] aKco = new double[3, mx];
        static double[,] aphico = new double[3, mx];
        static double[,] aq = new double[mx, mx];
        static double[,] qf = new double[mx, mx];
        static double[,] qi1 = new double[mx, mx];
        static double[,] qi2 = new double[mx, mx];
        static double[,] qi3 = new double[mx, mx];
        static double[,] qi5 = new double[mx, mx];
        static FluxEnd pe = new FluxEnd();

        public Fluxes()
        {
            ft.fend = new FluxEnd[2];
        }

        public static void FluxTable(double dz, SoilProps sp)
        {
            // Generates a flux table for use by other programs.
            // Assumes soil props available in sp of module soil.
            // dz - path length.


            //diags - timer start here

            nu = sp.nc;
            ah = sp.hc; aK = sp.Kc; aphi = sp.phic; aS = sp.Sc;
            aKco = sp.Kco; aphico = sp.phico;
            he = sp.he; Ks = sp.ks;

            // Get K values for Simpson's integration rule in subroutine odef.
            for (i = 0; i < nu - 2; i++)
            {
                x = 0.5 * (aphi[i + 1] - aphi[i]);
                hpK[i] = aK[i] + x * (aKco[0, i] + x * (aKco[1, i] + x * aKco[2, i]));
            }

            // Get fluxes aq(1,:) for values aphi[i] at bottom (wet), aphi(1) at top (dry).
            // These are used to select suitable phi values for flux table.
            nit = 0;
            aq[1, 1] = aK[1]; // q=K here because dphi/dz=0
            dh = 2.0; // for getting phi in saturated region
            q1 = (aphi[1] - aphi[2]) / dz; // q1 is initial estimate
            aq[1, 2] = ssflux(1, 2, dz, q1, 0.1 * rerr); // get accurate flux
            for (j = 2; j < nu + 20; j++) // 20*dh should be far enough for small curvature in (phi,q)
                if (j > nu) // part satn - set h, K and phi
                {
                    ah[j] = ah[j - 1] + dh * (j - nu);
                    aK[j] = Ks;
                    aphi[j] = aphi[j - 1] + Ks * dh * (j - nu);
                }

            // get approx q from linear extrapolation
            q1 = aq[1, j - 1] + (aphi[j] - aphi[j - 1]) * (aq[1, j - 1] - aq[1, j - 2]) / (aphi[j - 1] - aphi[j - 2]);
            aq[1, j] = ssflux(1, j, dz, q1, 0.1 * rerr); // get accurate q
            nt = j;
            ns = nt - nu;
            if (j > nu)
                if (-(aphi[j] - aphi[j - 1]) / (aq[1, j] - aq[1, j - 1]) < (1 + rerr) * dz)
                    Environment.Exit(0);

            // Get phi values phif for flux table using curvature of q vs phi.
            // rerr and cfac determine spacings of phif.
            Vector<double> aphiV = Vector<double>.Build.DenseOfArray(aphi);
            Matrix<double> aqM = Matrix<double>.Build.DenseOfArray(aq);
            i = nonlin(nu, aphiV.SubVector(0, nu).ToArray(), aqM.Column(0).SubVector(0, nu).ToArray(), rerr);
            re = curv(nu, aphiV.SubVector(0, nu).ToArray(), aqM.Column(0).SubVector(0, nu).ToArray());// for unsat phi
            indices(nu - 2, re.Take(nu - 3).Reverse().ToArray(), 1 + nu - i, cfac, out nphif, ref iphif);
            int[] iphifReverse = iphif.Take(nphif).Reverse().ToArray();
            for (int idx = 0; idx < nphif; idx++)
                iphif[idx] = 1 + nu - iphifReverse[idx]; // locations of phif in aphi
            aphiV = Vector<double>.Build.DenseOfArray(aphi); //may not need to do this; haevn't check in aphi has changed since last use.
            aqM = Matrix<double>.Build.DenseOfArray(aq); //as above
            re = curv(1 + ns, aphiV.SubVector(nu, nt - nu).ToArray(), aqM.Column(1).SubVector(nu, nt - nu).ToArray()); // for sat phi
            indices(ns - 1, re, ns, cfac, out nfs, ref ifs);

            for (int idx = nphif; i < nphif + nfs - 2; idx++)
                iphif[idx] = nu - 1 + ifs[idx];
            nfu = nphif; // no. of unsat phif
            nphif = nphif + nfs - 1;
            for (int idx = 0; i < nphif; idx++)
            {
                phif[idx] = aphi[iphif[idx]];
                qf[0, idx] = aq[0, iphif[idx]];
            }
            // Get rest of fluxes
            // First for lower end wetter
            for (j = 1; j < nphif; j++)
                for (i = 1; i < j; i++)
                {
                    q1 = qf[i - 1, j];
                    if (ah[iphif[j]] - dz < ah[iphif[i]])
                        q1 = 0.0; // improve?
                    qf[i, j] = ssflux(iphif[i], iphif[j], dz, q1, 0.1 * rerr);
                }
            // Then for upper end wetter
            for (i = 1; i < nphif; i++)
                for (j = i - 1; j > 1; j--)
                {
                    q1 = qf[i, j + 1];
                    if (j + 1 == i)
                        q1 = q1 + (aphi[iphif[i]] - aphi[iphif[j]]) / dz;
                    qf[i, j] = ssflux(iphif[i], iphif[j], dz, q1, 0.1 * rerr);
                }
            // Use of flux table involves only linear interpolation, so gain accuracy
            // by providing fluxes in between using quadratic interpolation.
            ni = nphif - 1;
            for (int idx = 0; idx < ni; idx++)
                phii[idx] = 0.5 * (phif[idx] + phif[idx + 1]);

            Matrix<double> qi1M = Matrix<double>.Build.DenseOfArray(qi1);
            Matrix<double> qfM = Matrix<double>.Build.DenseOfArray(qf);
            double[] qi1Return;
            double[] qi2Return;
            double[] qi3Return;

            for (i = 0; i < nphif; i++)
            {
                qi1Return = quadinterp(phif, qfM.Row(i).ToArray(), nphif, phii);
                for (int idx = 0; idx < qi1Return.Length; idx++)
                    qi1[i, idx] = qi1Return[idx];
            }

            for (j = 0; j < nphif; j++)
            {
                qi2Return = quadinterp(phif, qfM.Column(j).ToArray(), nphif, phii);
                for (int idx = 0; idx < qi2Return.Length; idx++)
                    qi2[idx, i] = qi2Return[idx];
            }

            for (j = 0; j < ni; j++)
            {
                qi1M = Matrix<double>.Build.DenseOfArray(qi1);
                qi3Return = quadinterp(phif, qi1M.Column(j).ToArray(), nphif, phii);
                for (int idx = 0; idx < qi3Return.Length; idx++)
                    qi3[idx, i] = qi3Return[idx];
            }

            // Put all the fluxes together.
            i = nphif + ni;
            for (int iidx = 0; iidx < i; i += 2)
                for (int npidx = 0; npidx < nphif; npidx++)
                    for (int niidx = 0; niidx < ni; niidx++)
                    {
                        qi5[iidx, iidx] = qf[npidx, npidx];
                        qi5[iidx, iidx + 1] = qi1[npidx, niidx];
                        qi5[iidx + 1, iidx] = qi2[niidx, npidx];
                        qi5[iidx + 1, iidx + 1] = qi3[niidx, niidx];
                    }

            // Get accurate qi5(j,j)=Kofphi(phii(ip))
            ip = 0;
            for (j = 1; j < i; j += 2)
            {
                ip = ip + 1;
                ii = iphif[ip + 1] - 1;
                for (int idx = 0; idx < aphi.Length; idx++) // Search down to locate phii position for cubic.
                {
                    if (aphi[ii] <= phii[ip])
                        break;
                    ii = ii - 1;
                }
                x = phii[ip] - aphi[ii];
                qi5[j, j] = aK[ii] + x * (aKco[1, ii] + x * (aKco[2, ii] + x * aKco[3, ii]));
            }

            for (int idx = 0; idx < i; i++)
            {
                phii5[idx * 2] = phif[idx];
                phii5[idx * 2 + 1] = phii[idx];
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

        private static double[] odef(int n1, int n2, double[] aK, double[] hpK)
        {
            double[] u = new double[2];
            int m = 4;
            int np;
            double[] da = new double[n2 - n1 + 1];
            double[] db = new double[n2 - n1];
            // Get z and dz/dq for flux q and phi from aphi(n1) to aphi(n2).
            // q is global to subroutine fluxtbl.
            np = n2 - n1 + 1;
            Vector<double> akV = Vector<double>.Build.DenseOfArray(aK);
            Vector<double> hpKV = Vector<double>.Build.DenseOfArray(hpK);
            double[] daTemp = Utility.Math.Subtract_Value(akV.SubVector(n1, n2-n1+1).ToArray(), q);
            double[] dbTemp = Utility.Math.Subtract_Value(akV.SubVector(n1, n2 - n1).ToArray(), q);
            for (int idx = 0; idx < da.Length; idx++)
            {
                da[idx] = 1.0 / daTemp[idx];
                if(idx < db.Length)
                    db[idx] = 1.0 / dbTemp[idx];
            }

            // apply Simpson's rule
            Vector<double> aphiV = Vector<double>.Build.DenseOfArray(aphi);
            Vector<double> daV = Vector<double>.Build.DenseOfArray(da);

            // sum((aphi(n1+1:n2)-aphi(n1:n2-1))*(da(1:np-1)+4*db+da(2:np))/6) for both of these stupid lines... find something better.
            Vector<double> t1 = aphiV.SubVector(n1, n2 - n1 + 1);
            Vector<double> t2 = aphiV.SubVector(n1 - 1, n2 - n1+1);
            Vector<double> t3 = daV.SubVector(0, np);
            Vector<double> t4 = daV.SubVector(1, np - 1);

            //u[0] = Utility.Math.Sum(Utility.Math.Divide_Value(Utility.Math.Multiply(aphiV.SubVector(n1, n2 - n1 + 1).Subtract(aphiV.SubVector(n1 - 1, n2 - n1)).ToArray(), Utility.Math.Add(Utility.Math.Multiply(daV.SubVector(0, np).Add(4).ToArray(), db), daV.SubVector(1, np + 1).ToArray())), 6)); // this is madness!
            u[0] = Utility.Math.Sum(Utility.Math.Divide_Value(Utility.Math.Multiply(aphiV.SubVector(n1, n2 - n1 + 1).Subtract(aphiV.SubVector(n1 - 1, n2 - n1)).ToArray(), Utility.Math.Add(Utility.Math.Multiply(daV.SubVector(0, np).Add(4).ToArray(), db), daV.SubVector(1, np + 1).ToArray())), 6)); // this is madness!
            da = Utility.Math.Multiply(da, da);
            db = Utility.Math.Multiply(db, db);
            u[1] = Utility.Math.Sum(Utility.Math.Divide_Value(Utility.Math.Multiply(aphiV.SubVector(n1, n2 - n1 + 1).Subtract(aphiV.SubVector(n1 - 1, n2 - n1)).ToArray(), Utility.Math.Add(Utility.Math.Multiply(daV.SubVector(0, np).Add(4).ToArray(), db), daV.SubVector(1, np + 1).ToArray())), 6)); // this is madness!
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
            //write (*,*) i,j
            if (i == j) // free drainage
            {
                return aK[i];
                //write (*,*) "aK[i] ",aK[i],i
            }
            ha = ah[i]; hb = ah[j]; Ka = aK[i]; Kb = aK[j];
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
                    n1 = ia;
                n2 = ib;
            }
            u0 = new double[] { 0.0, 0.0 }; // u(1) is z, u(2) is dz/dq (partial deriv)
            //write (*,*) q1,q,q2
            for (it = 1; it < maxit; it++)// bounded Newton iterations to get q that gives correct dz
            {
                u = u0; //point?
                u = odef(n1, n2, aK, hpK);
                //write (*,*) it,q,u(1),u(2)
                if (i > n || j > n) // add sat solns
                {
                    Ks = Math.Max(Ka, Kb);
                    u[0] = u[0] + Ks * dh / (Ks - q);
                    u[1] = u[1] + Ks * dh / Math.Pow(Ks - q, 2);
                }

                dq = (v1 - u[0]) / u[1]; // delta z / dz/dq
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
                Console.WriteLine("ssflux: too many its", ia, ib);
            nit = nit + it;
            return q;
        }

        // get curvature at interior points of (x,y)
        private static double[] curv(int n, double[] x, double[] y)
        {
            double[] c = new double[n - 2];
            double[] s = new double[n - 2];
            double[] yl = new double[n - 2];
            Vector<double> xV = Vector<double>.Build.DenseOfArray(x);
            Vector<double> yV = Vector<double>.Build.DenseOfArray(y);
            s = Utility.Math.Divide(Utility.Math.Subtract(yV.SubVector(2, n - 1).ToArray(), yV.SubVector(0, n - 3).ToArray()), Utility.Math.Subtract(xV.SubVector(2, n - 1).ToArray(), xV.SubVector(0, n - 3).ToArray()));
            yl = Utility.Math.Add(yV.SubVector(0, n - 3).ToArray(), 
                                  Utility.Math.Multiply(Utility.Math.Subtract(xV.SubVector(1, n - 2).ToArray(),
                                                                                xV.SubVector(0, n - 3).ToArray()),
                                                              s));
            return Utility.Math.Subtract_Value(Utility.Math.Divide(yV.SubVector(1, n - 2).ToArray(), yl), 1);
        }

        // get last point where (x,y) deviates from linearity by < re
        private static int nonlin(int n, double[] x, double[] y, double re)
        {
            int nonlin, i;
            double s, are;
            double[] yl = new double[n - 2];
            nonlin = n;
            for (i = 2; i < n; i++)
            {
                s = (y[i] - y[0]) / (x[i] - x[0]);
                Vector<double> xV = Vector<double>.Build.DenseOfArray(x);
                Vector<double> ylV = Vector<double>.Build.DenseOfArray(yl);
                Vector<double> yV = Vector<double>.Build.DenseOfArray(y);
                Vector<double> xSub = xV.SubVector(1, i - 2);
                Vector<double> ylSub = ylV.SubVector(0, i - 3);
                Vector<double> ySub = yV.SubVector(1, i - 2);
                for (int idx = 0; idx < ylSub.Count; idx++)
                {
                    ylSub[idx] = y[0] + s * (xSub[i] - x[0]);
                }
                double[] div = Utility.Math.Subtract_Value(Utility.Math.Divide(ySub.ToArray(), ylSub.ToArray()), 1);
                for (int idx = 0; idx < div.Length; idx++)
                    div[idx] = Math.Abs(div[idx]);
                are = Utility.Math.Max(div);
                if (are > re)
                {
                    return i - 1;
                }
            }
            return 0;
        }

        // get indices of elements selected using curvature
        private static void indices(int n, double[] c, int iend, double fac, out int nsel, ref int[] isel)
        {
            int i, j;
            int[] di = new int[n];
            double[] ac = new double[n];

            for (int idx = 0; idx < n; idx++)
            {
                ac[idx] = Math.Abs(c[idx]);
                di[idx] = (int)Math.Round(fac * Utility.Math.Max(ac) / ac[idx], MidpointRounding.ToEven); // min spacings
            }
            isel[0] = 1; i = 1; j = 1;
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
        private static double[] quadco(double[] x, double[] y)
        {
            double[] co = new double[3];
            double s, x1, y2, x12, c1, c2;
            s = 1.0 / (x[2] - x[0]);
            x1 = s * (x[1] - x[0]);
            y2 = y[2] - y[0];
            x12 = x1 * x1;
            c1 = (y[1] - y[0] - x12 * y2) / (x1 - x12);
            c2 = y2 - c1;
            co[0] = y[1];
            co[1] = s * c1;
            co[2] = s * s * c2;
            return co;
        }

        // Return v(1:n-1) corresponding to u(1:n-1) using quadratic interpolation.
        private static double[] quadinterp(double[] x, double[] y, int n, double[] u)
        {
            double[] v = new double[3];
            int i, j, k;
            double z;
            double[] co = new double[3];
            Vector<double> xV = Vector<double>.Build.DenseOfArray(x);
            Vector<double> yV = Vector<double>.Build.DenseOfArray(y);
            for (k = 0; k < n; k += 2)
            {
                i = k;
                if (k + 2 > n)
                    i = n - 2;
                co = quadco(xV.SubVector(i, 3).ToArray(), yV.SubVector(i, 3).ToArray());
                for (j = k; j < i; j++)
                {
                    z = u[j] - x[i];
                    v[j] = co[1] + z * (co[2] + z * co[3]);
                }
            }
            return v;
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
