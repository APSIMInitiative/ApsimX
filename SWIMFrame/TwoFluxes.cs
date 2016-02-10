using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics.LinearAlgebra;
using APSIM.Shared.Utilities;

namespace SWIMFrame
{
    public static class TwoFluxes
    {
        static int mx = 100;
        static int maxit = 20;
        static int i, j, k, m, ne, ni, id, ip, nco1, nco2, nit, ie, ii, jj;
        static int[] nft = new int[2 + 1];
        static int[] nfu = new int[2 + 1];
        static int[] n = new int[2 + 1];
        static int[] nfi = new int[2 + 1];
        static double rerr = 1e-3;
        static double phi1max, dhe, v, vlast, dx, e, f, df, q1, phialast, v1, v2, f1, f2;
        static double[] he = new double[2 + 1];
        static double[] phie = new double[2 + 1];
        static double[] Ks = new double[2 + 1];
        static double[] co = new double[4 + 1];
        static double[] xval = new double[mx + 1];
        static double[] phico1 = new double[mx + 1];
        static double[] phico2 = new double[mx + 1];
        static double[] y2 = new double[3 * mx + 1];
        static double[] hi = new double[3 * mx + 1];
        static double[,] phif = new double[2, mx + 1];
        static double[,] phifi = new double[2, mx + 1];
        static double[,] phii5 = new double[2, mx + 1];
        static double[,] coq = new double[3 + 1, 3 * mx + 1];
        static double[,] co1 = new double[4 + 1, mx + 1];
        static double[,] qp = new double[mx + 1, mx + 1];
        static double[,] y22 = new double[mx + 1, mx + 1];
        static double[,] qi1 = new double[mx + 1, mx + 1];
        static double[,] qi2 = new double[mx + 1, mx + 1];
        static double[,] qi3 = new double[mx + 1, mx + 1];
        static double[,] qi5 = new double[mx + 1, mx + 1];
        static double[,] h = new double[2 + 1, 3 * mx + 1];
        static double[,] phi = new double[2 + 1, 3 * mx + 1];
        static double[,] phii = new double[2 + 1, 3 * mx + 1];
        static double[,,] qf = new double[2 + 1, mx + 1, mx + 1];
        static double[,,] y2q = new double[2 + 1, mx + 1, mx + 1];
        static double[,,] co2 = new double[4 + 1, mx + 1, mx + 1];
        static FluxTable ftwo = new FluxTable();
        static FluxTable[] ft = { new FluxTable(), new FluxTable() };
        static SoilProps[] sp = { new SoilProps(), new SoilProps() };

        public static FluxTable TwoTables(FluxTable ft1, SoilProps sp1, FluxTable ft2, SoilProps sp2)
        {
            /*Generates a composite flux table from two uniform ones.
              Sets up quadratic interpolation table to get phi at interface for lower path
              from phi at interface for upper path.
              Sets up cubic interpolation tables to get fluxes from phi at interface for
              upper and lower paths.
              Solves for phi at interface in upper path that gives same fluxes in upper
              and lower paths, for all phi at upper and lower ends of composite path.
              Increases no. of fluxes in table by quadratic interpolation.
              ft1, ft2 - flux tables for upper and lower paths.
              sp1, sp2 - soil prop tables for upper and lower paths.
              Note that arrays are one indexed; 0 index is not used.
              Exception to this is flux/soil arrays where array index is not used in calculations.
              This results in a waste of memory and should be rectified once SWIM is complete.
            */


            // Set up required pointers and data
            if (ft1.fend[0].sid != ft1.fend[1].sid || ft2.fend[0].sid != ft2.fend[1].sid)
            {
                Console.WriteLine("Flux table not for uniform soil.");
                Environment.Exit[1];
            }

            ft[0] = ft1;
            ft[1] = ft2;
            sp[0] = sp1;
            sp[1] = sp2;
            for (i = 1; i <= 2; i++)
            {
                n[i] = sp[i - 1].n;
                he[i] = sp[i - 1].he;
                phie[i] = sp[i - 1].phie;
                Ks[i] = sp[i - 1].ks;
                for (int x = 1; x <= n[i]; x++)
                {
                    h[i, x] = sp[i - 1].h[x]; //test this
                    phi[i, x] = sp[i - 1].phi[x]; //and this
                }
            }

            // Discard unwanted input - use original uninterpolated values only.
            for (i = 1; i <= 2; i++)
            {
                m = ft[i - 1].fend[0].nft; //should be odd
                j = 1 + m / 2;
                for (int x = 1; x <= j; x++)
                {
                    phif[1, x] = ft[i - 1].fend[0].phif[x * 2 - 1]; //test these
                    qf[i, x, x] = ft[i - 1].ftable[x * 2 - 1, x * 2 - 1];
                }
            }

            // Extend phi2 and h2 if he1>he2, or vice-versa.
            dhe = Math.Abs(he[1] - he[2]);
            if (dhe > 0.001)
            {
                if (he[1] > he[2])
                {
                    i = 1;
                    j = 2;
                }
                else
                {
                    i = 2;
                    j = 1;
                }
                ii = Find(he[j], h, n[i], i);
                for (k = 1; k <= n[i] - ii; k++)
                {
                    h[j, n[j] + k] = h[i, ii + k];//test these
                    phi[j, n[j] + k] = phie[j] + Ks[j] * (h[i, ii + k] - he[j]);
                }
                n[j] = n[j] + n[i] - ii;
            }
            phi1max = phi[1, n[1]];

            // Get phi for same h.
            if (h[1, 1] > h[2, 1])
            {
                i = 1;
                j = 2;
            }
            else
            {
                i = 2;
                j = 1;
            }

            Matrix<double> hm = Matrix<double>.Build.DenseOfArray(h); //test
            double[] absh = hm.Column(j).ToArray();
            absh = absh.Slice(1, n[j]);
            absh = MathUtilities.Subtract_Value(absh, h[i, 1]);
            for (int x = 0; x < absh.Length; x++)
                absh[x] = Math.Abs(absh[x]);
            id = MinLoc(absh);
            if (h[j, id] >= h[i, 1])
                id--;

            //phii(j,:) for soil j will match h(i,:) from soil i and h(j, 1:id) from soil j.
            //phii(i,:) for soil i will match h(i,:) and fill in for h(j, 1:id).
            for (int iid = 1; iid <= id; iid++)
                phii[j, iid] = phi[j, iid]; // keep these values

            // But interpolate to match values that start at greater h.
            jj = id + 1; //h(j,id+1) to be checked first
            phii[j, id + n[i]] = phi[j, n[i]]; // last h values match
            for (ii = 1; ii <= n[i] - 1; ii++)
            {
                while (true) //get place of h(i,ii) in h array for soil j
                {
                    if (jj > n[j])
                    {
                        Console.WriteLine("twotbls: h[j,n[j]] <= h[i,ii]; i, j, ii, n[j] = " + i + " " + j + " " + ii + " " + n[j]);
                        break;
                    }

                    if (h[j, jj] > h[i, ii])
                        break;
                    jj += 1;
                }

                k = jj - 1; //first point for cubic interp
                if (jj + 2 > n[j])
                    k = n[j] - 3;

                double[] hCuco = new double[5];
                double[] phiCuco = new double[5];
                for (int x = k; x <= k + 3; x++)
                {
                    hCuco[x - k + 1] = h[j, x];
                    phiCuco[x - k + 1] = phi[j, x];
                }

                co = Soil.Cuco(hCuco, phiCuco); // get cubic coeffs
                v = h[i, ii] - h[j, k];
                phii[j, id + ii] = co[1] + v * (co[2] + v * (co[3] + v * co[4]));
            }
            ni = id + n[i];

            // Generate sensible missing values using quadratic extrapolation.
            co = Fluxes.quadco(new double[4] { 0, phii[j, 1], phii[j, id + 1], phii[j, id + 2] }, new double[4] { 0, 0, phi[i, 1], phi[i, 2] });
            if (co[2] > 0) // +ve slope at zero - ok
            {
                for (int x = 1; x <= id; x++)
                {
                    xval[x] = phii[j, x] - phii[j, 1];
                    phii[i, x] = co[i] + xval[x] * (co[2] + xval[x] * co[3]);
                }
            }
            else // -ve slope at zero, use quadratic with zero slope at zero
            {
                co[3] = phi[i, 1] / Math.Pow(phii[j, id + 1], 2);
                for (int x = 1; x <= id; x++)
                    phii[i, x] = co[3] * Math.Pow(phii[j, x], 2);
            }

            // phii(i,id+1:ni)=phi(i,1:n(i))
            double[] phin = new double[n[i] + 1];
            for (int x = 1; x <= n[i]; x++)
                phin[x - 1] = phi[j, x];
            for (int x = id + 1; x <= ni; x++)
                phii[i, x] = phin[x - id + 1];

            for (int x = 1; x < id; x++)
                hi[x] = h[j, x];

            // hi(id+1:ni)=h(i,1:n(i))
            double[] hin = new double[n[i] + 1];
            for (int x = 1; x <= n[i]; x++)
                hin[x - 1] = h[j, x];

            for (int x = id + 1; x <= ni; x++)
                hi[x] = h[i, x - id + 1];

            /* hi(1:ni) are h values for the interface tables.
                * phii(1,1:ni) are corresponding interface phi values for upper layer.
                * phii(2,1:ni) are corresponding interface phi values for lower layer.
                * Set up quadratic interpolation coeffs to get phii2 given phii1.
            */
            Matrix<double> coqM = Matrix<double>.Build.DenseOfArray(coq);
            Vector<double>[] quadcoV = new Vector<double>[ni - 2 + 1];
            for (i = 1; i <= ni - 2; i++)
            {
                quadcoV[i] = Vector<double>.Build.DenseOfArray(Fluxes.quadco(new double[4] { 0, phii[1, i], phii[1, i + 1], phii[1, i + 2] }, new double[4] { 0, phii[2, i], phii[2, i + 1], phii[2, i + 2] }));
                coqM.SetColumn(i, quadcoV[i]);
            }
            Vector<double> lincoV = Vector<double>.Build.DenseOfArray(linco(new double[3] { 0, phii[1, ni - 1], phii[1, ni] }, new double[3] { 0, phii[2, ni - 1], phii[2, ni] }));
            coqM.SetColumn(ni - 1, lincoV);
            coq = coqM.ToArray();
            coq[3, ni - 1] = 0;

            // Set up cubic coeffs to get fluxes q given phi.
            for (j = 1; j <= nft[2]; j++)
            {
                k = 1;
                ip = 1;
                while (true)
                {
                    phico2[k] = phif[2, ip];
                    double[] co2co = Soil.Cuco(new double[5] { 0, phif[2, ip], phif[2, ip + 1], phif[2, ip + 2], phif[2, ip + 3] }, new double[5] { 0, qf[2, ip, j], qf[2, ip + 1, j], qf[2, ip + 2, j], qf[2, ip + 3, j] });
                    for (int x = 1; x < co2co.Length; x++)
                        co2[x, k, j] = co2co[x];
                    ip += 3;
                    if (ip == nft[2])
                        break;
                    if (ip > nft[2])
                        ip = nft[2] - 3;
                    k++;
                }
                nco2 = k;

                // Get fluxes
                nit = 0;
                for (i = 1; i <= nft[1]; i++)
                {
                    vlast = phif[1, i];
                    k = 1;
                    ip = 1;
                    Matrix<double> co1M = Matrix<double>.Build.DenseOfArray(co1);
                    while (true)
                    {
                        phico1[k] = phif[1, ip];
                        co1M.SetColumn(k, Soil.Cuco(new double[5] { 0, phif[1, ip], phif[1, ip + 1], phif[1, ip + 2], phif[1, ip + 3] },
                                                    new double[5] { 0, qf[1, i, ip], qf[1, i, ip + 1], qf[1, i, ip + 2], qf[1, i, ip + 3] }));
                        ip += 3;
                        if (ip == nft[1])
                            break;
                        if (ip > nft[1])
                            ip = nft[1] - 3;
                        k++;
                    }
                    nco1 = k;
                    for (j = 1; j <= nft[2]; j++) // bottom phis
                    {
                        v = vlast;
                        for (k = 1; k <= maxit; k++) // solve for upper interface phi giving same fluxes
                        {
                            q1 = fd(v, f, df);
                            nit += 1;
                            dx = f / df; // Newton's method - almost always works
                            v = Math.Min(10.0 * phif[1, nft[1]], Math.Max(phii[1, 1], v - dx));
                            e = Math.Abs(f / q1);
                            if (e < rerr)
                                break;
                            vlast = v;
                        }
                        if (k > maxit) //failed - bracket q and use bisection
                        {
                            v1 = phii[1, 1];
                            q1 = fd(v1, f1, df);
                            if (f1 <= 0.0) // answer is off table - use end value
                            {
                                qp[i, j] = q1;
                                continue;
                            }
                            v2 = phii[1, ni];
                            q1 = fd(v2, f2, df);
                            for (k = 1; k <= maxit; k++)
                            {
                                if (f1 * f2 < 0.0)
                                    break;
                                v1 = v2;
                                f1 = f2;
                                v2 = 2.0 * v1;
                                q1 = fd(v2, f2, df);
                            }
                            if (k > maxit)
                            {
                                Console.WriteLine(v1 + " " + v2 + " " + f1 + " " + f2);
                                v1 = phii[1, 1];
                                q1 = fd(v1, f1, df);
                                Console.WriteLine(v1 + " " + f1);
                                Console.WriteLine("twotbls: too many iterations at i, j = " + i + " " + j);
                                Environment.Exit[1];
                            }
                            for (k = 1; k <= maxit; k++)
                            {
                                v = 0.5 * (v1 + v2);
                                q1 = fd(v, f, df);
                                e = Math.Abs(f / q1);
                                if (e < err)
                                    break;
                                if (f > 0.0)
                                {
                                    v1 = v;
                                    f1 = f;
                                }
                                else
                                {
                                    v2 = v;
                                    f2 = f;
                                }
                            }
                            vlast = v;
                            if (k > maxit)
                            {
                                Console.WriteLine("twotbls: too many iterations at i, j = " + i + " " + j);
                                Environment.Exit[1];
                            }
                        }
                        // Solved
                        qp[i, j] = q1;
                    }
                }

                //interpolate extra fluxes
                for (i = 1; i <= 2; i++)
                {
                    nfi[i] = nft[i] - 1;
                    for (int x = 1; x <= nfi[i]; x++)
                        phifi[i, x] = 0.5 * (phif[1, x] + (x > 1 ? phif[i, x] : 0)); //ternary op to make phif(i,1:nfi(i))+phif(i,2:nft(i)) easier by dropping first index for second phif call
                }

                Matrix<double> phifM = Matrix<double>.Build.DenseOfArray(phif);
                Matrix<double> phifiM = Matrix<double>.Build.DenseOfArray(phifi);
                Matrix<double> qpM = Matrix<double>.Build.DenseOfArray(qp);
                Matrix<double> qi1M = Matrix<double>.Build.DenseOfArray(qi1);
                Matrix<double> qi2M = Matrix<double>.Build.DenseOfArray(qi2);
                Matrix<double> qi3M = Matrix<double>.Build.DenseOfArray(qi3);

                for (i = 1; i <= nft[1]; i++)
                {
                    qi1M.SetColumn(i, Fluxes.quadinterp(phifM.Row[2].ToArray(), qpM.Row(i).ToArray(), nft[2], phifiM.Row[2].ToArray()));
                }
                for (j = 1; j <= nft[2]; j++)
                {
                    qi2M.SetColumn(j, Fluxes.quadinterp(phifM.Row[1].ToArray(), qpM.Column(j).ToArray(), nft[1], phifiM.Row[1].ToArray()));
                }
                for (j = 1; j <= nfi[2]; j++)
                {
                    qi3M.SetColumn(j, Fluxes.quadinterp(phifM.Row[1].ToArray(), qi1M.Column(j).ToArray(), nft[1], phifiM.Row[1].ToArray()));
                }
                // Put all the fluxes together
                i = nft[1] + nfi[1];
                j = nft[2] + nfi[2];

                /*
                  qi5(1:i:2,1:j:2)=qp(1:nft[1],1:nft[2])
                  qi5(1:i:2,2:j:2)=qi1(1:nft[1],1:nfi[2])
                  qi5(2:i:2,1:j:2)=qi2(1:nfi[1],1:nft[2])
                  qi5(2:i:2,2:j:2)=qi3(1:nfi[1],1:nfi[2])
                  phii5(1,1:i:2)=phif(1,1:nft[1])
                  phii5(1,2:i:2)=phifi(1,1:nfi[1])
                  phii5(2,1:j:2)=phif(2,1:nft[2])
                  phii5(2,2:j:2)=phifi(2,1:nfi[2])
                */
                for (int a = 1; a <= i; a += 2)
                    for (int b = 1; b <= j; b += 2)
                        for (int c = 1; c <= nft[1]; c++)
                            for (int d = 1; d < nft[2]; d++)
                            {
                                qi5[a, b] = qp[c, d];
                                phii5[1, a] = phif[1, c];
                            }
                for (int a = 1; a <= i; a += 2)
                    for (int b = 2; b <= j; b += 2)
                        for (int c = 1; c <= nft[1]; c++)
                            for (int d = 1; d < nfi[2]; d++)
                            {
                                qi5[a, b] = qi1[c, d];
                            }
                for (int a = 2; a <= i; a += 2)
                    for (int b = 1; b <= j; b += 2)
                        for (int c = 1; c <= nfi[1]; c++)
                            for (int d = 1; d < nft[2]; d++)
                            {
                                qi5[a, b] = qi2[c, d];
                                phii5[1, a] = phifi[1, c];
                                phii5[2, b] = phif[2, d];
                            }
                for (int a = 2; a <= i; a += 2)
                    for (int b = 2; b <= j; b += 2)
                        for (int c = 1; c <= nfi[1]; c++)
                            for (int d = 1; d < nfi[2]; d++)
                            {
                                qi5[a, b] = qi3[c, d];
                                phii5[2, b] = phifi[2, d];
                            }
            }

            // Assemble flux table
            for (ie = 1; ie <= 2; ie++)
            {
                FluxEnd pe = ftwo.fend[ie];
                FluxEnd qe = ft[ie].fend[1];
                pe.sid = sp[ie].sid;
                pe.nfu = qe.nfu;
                pe.nft = qe.nft;
                pe.dz = qe.dz;
                pe.phif = qe.phif;
            }

            double[,] qi5Slice = new double[i + 1, j + 1];
            for (int x = 1; x <= i; x++)
                for (int y = 1; y <= j; y++)
                    qi5Slice[x, y] = qi5[x, y];
            ftwo.ftable = qi5Slice;

            return ftwo;
        }

        private static double fd(double phia, out double f, out double d, out double q)
        {
            //Returns flux difference f, deriv d and upper flux q.
            //phia - phi at interface in upper path.
            double h, phib, der, v, vm1, qv, qvm1, q1, q1d, q2, q2d;
            if (phia != phialast) {
                if (phia > phi1max) // both saturated - calc der and lower interface phi
                {
                    h = he[1] + (phia - phie[1]) / Ks[1]);
                    phib = phie[2] + Ks[2] * (h - he[2]);
                    der = Ks[2] / Ks[1];
                }
                else // use quadratic interpolation to get them
                {
                    ii = find(phia, phii[1, 1:ni), ni)
        v = phia - phii[1, ii];
                    der = coq[2, ii] + v * 2.0 * coq[3, ii];
                    phib = coq[1, ii] + v * (coq[2, ii] + v * coq[3, ii]);
                }
                // Get upper flux and deriv.
                v = phif[1, nft[1]];
                if (phia > v) // off table - extrapolate
                {
                    vm1 = phif[1, nft[1] - 1];
                    qv = qf[1, i, nft[1]];
                    qvm1 = qf[1, i, nft[1] - 1];
                    q1d = (qv - qvm1) / (v - vm1);
                    q1 = qv + q1d * (phia - v);
             else // use cubic interpolation
                call ceval1(phia, q1, q1d)
      }
                phialast = phia;
            }
            // Get lower flux and deriv in same way.
            v = phif[2, nft[2]];
            if (phib > v)
            {
                vm1 = phif(2, nft[2] - 1);
             qv = qf(2, nft[2], j);
             qvm1 = qf(2, nft[2] - 1, j);
             q2d = (qv - qvm1) / (v - vm1);
             q2 = qv + q2d * (phib - v);
                              }
            else

                call ceval2(j, phib, q2, q2d)
  // Set return values.
  f = q1 - q2;
            d = q1d - q2d * der;
            q = q1;
        }


        private static void ceval1(double phi, out double q, out double qd)
        {
            //Return flux q and deriv qd given phi.
            //Use cubic interpolation in table(phico1, co1).
            int i1, i2, im;
            double x;

            i1 = 1;
            i2 = ncol + 1; // allow for last interval in table
            while(true) //use bisection to find place
            {
                if (i2 - i1 <= 1)
                    break;

                im = (i1 + i2) / 2;
                if (phico1[im] > phi)
                    i2 = im;
                else i1 = im;
            }

            //Interpolate
            x = phi - phico1[i1];
            q = co1[1, i1] + x * (co1[2, i1] + x * (co1[3, i1] + x * co1[4, i1]));
            qd = co1[2, i1] + x * (2.0 * co1[3, i1] + x * 3.0 * co1[4, i1]);
        }

        private static void ceval2(int j, double phi, out double q, out double qd)
        {
            //Return flux q and deriv qd given phi.
            //Use cubic interpolation in table(phico1, co1).
            int i1, i2, im;
            double x;

            i1 = 1;
            i2 = ncol + 1; // allow for last interval in table
            while (true) //use bisection to find place
            {
                if (i2 - i1 <= 1)
                    break;

                im = (i1 + i2) / 2;
                if (phico1[im] > phi)
                    i2 = im;
                else i1 = im;
            }

            //Interpolate
            x = phi - phico1[i1];
            q = co2[1, i1, j] + x * (co2[2, i1, j] + x * (co2[3, i1, j] + x * co2[4, i1, j]));
            qd = co2[2, i1, j] + x * (2.0 * co2[3, i1, j] + x * 3.0 * co2[4, i1, j]);
        }

        private static int Find(double x, double[] xa, int n)
        {
            int i1 = 1;
            int i2 = n - 1;
            int im;

            //Return i where xa(i)<=x<xa(i+1)
            while (true)
            {
                if (i2 - i1 <= 1)
                    break;

                im = (i1 + i2) / 2;
                if (x >= xa[im])
                    i1 = im;
                else
                    i2 = im;
            }
            return i1;
        }

        private static int MinLoc (double[] array)
        {
            int pos=1;
            for (int i = 1; i < array.Length; i++)
                if (array[i] < array[pos])
                    pos = i;

            return pos;
        }

        private static double[] linco (double[] x, double[] y)
        {
            return new double[4];
        }
    }
}
 