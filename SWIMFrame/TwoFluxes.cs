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
        static int i, j, k, m, ni, id, ip, nco1, nco2, nit, ie, ii, jj;
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
        static double[,] phif = new double[mx + 1, 2 + 1];
        static double[,] phifi = new double[mx + 1, 2 + 1];
        static double[,] phii5 = new double[mx + 1, 2 + 1];
        static double[,] coq = new double[3 * mx + 1, 3 + 1];
        static double[,] co1 = new double[mx + 1, 4 + 1];
        static double[,] qp = new double[mx + 1, mx + 1];
        static double[,] y22 = new double[mx + 1, mx + 1];
        static double[,] qi1 = new double[mx + 1, mx + 1];
        static double[,] qi2 = new double[mx + 1, mx + 1];
        static double[,] qi3 = new double[mx + 1, mx + 1];
        static double[,] qi5 = new double[mx + 1, mx + 1];
        static double[,] h = new double[3 * mx + 1, 2 + 1];
        static double[,] phi = new double[3 * mx + 1, 2 + 1];
        static double[,] phii = new double[3 * mx + 1, 2 + 1];
        static double[,,] qf = new double[mx + 1, mx + 1, 2 + 1];
        static double[,,] y2q = new double[mx + 1, mx + 1, 2 + 1];
        static double[,,] co2 = new double[mx + 1, mx + 1, 4 + 1];
        static FluxTable ftwo = new FluxTable();
        static FluxTable[] ft = { new FluxTable(), new FluxTable() };
        static SoilProps[] sp = { new SoilProps(), new SoilProps() };

        /// <summary>
        /// Only used in unit tests. Resets all static variables
        /// </summary>
        public static void TestReset()
        {
            mx = 100;
            maxit = 20;
            i = j = k = m = ni = id = ip = nco1 = nco2 = nit = ie = ii = jj = 0;
            nft = new int[2 + 1];
            nfu = new int[2 + 1];
            n = new int[2 + 1];
            nfi = new int[2 + 1];
            rerr = 1e-3;
            phi1max = dhe = v = vlast = dx = e = f = df = q1 = phialast = v1 = v2 = f1 = f2 = 0;
            he = new double[2 + 1];
            phie = new double[2 + 1];
            Ks = new double[2 + 1];
            co = new double[4 + 1];
            xval = new double[mx + 1];
            phico1 = new double[mx + 1];
            phico2 = new double[mx + 1];
            y2 = new double[3 * mx + 1];
            hi = new double[3 * mx + 1];
            phif = new double[mx + 1, 2 + 1];
            phifi = new double[mx + 1, 2 + 1];
            phii5 = new double[mx + 1, 2 + 1];
            coq = new double[3 * mx + 1, 3 + 1];
            co1 = new double[mx + 1, 4 + 1];
            qp = new double[mx + 1, mx + 1];
            y22 = new double[mx + 1, mx + 1];
            qi1 = new double[mx + 1, mx + 1];
            qi2 = new double[mx + 1, mx + 1];
            qi3 = new double[mx + 1, mx + 1];
            qi5 = new double[mx + 1, mx + 1];
            h = new double[3 * mx + 1, 2 + 1];
            phi = new double[3 * mx + 1, 2 + 1];
            phii = new double[3 * mx + 1, 2 + 1];
            qf = new double[mx + 1, mx + 1, 2 + 1];
            y2q = new double[mx + 1, mx + 1, 2 + 1];
            co2 = new double[mx + 1, mx + 1, 4 + 1];
            ftwo = new FluxTable();
            ft = new FluxTable[] { new FluxTable(), new FluxTable() };
            sp = new SoilProps[] { new SoilProps(), new SoilProps() };
        }

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
              sp1, sp2 - soil prop tables for upper and lower paths. -PR
              
             Note that arrays are one indexed; 0 index is not used.
              Exception to this is flux/soil arrays where array index is not used in calculations.
              This results in a waste of memory and should be rectified once SWIM is complete. -JF
            */


            // Set up required pointers and data
            if (ft1.fend[0].sid != ft1.fend[1].sid || ft2.fend[0].sid != ft2.fend[1].sid)
            {
                Console.WriteLine("Flux table not for uniform soil.");
                Environment.Exit(1);
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
                    h[x, i] = sp[i - 1].h[x];
                    phi[x, i] = sp[i - 1].phi[x];
                }
            }

            // Discard unwanted input - use original uninterpolated values only.
            for (i = 1; i <= 2; i++)
            {
                m = ft[i - 1].fend[0].nft; //should be odd
                j = 1 + m / 2;
                nft[i] = j;
                nfu[i] = 1 + ft[i - 1].fend[1].nfu / 2; //ft[i].fend[1].nfu should be odd
            }

            // do matrices seperatly
            for (i = 1; i <= 2; i++)
                for (int x = 1; x <= j; x++)
                {
                    phif[x, i] = ft[i - 1].fend[0].phif[x * 2 - 1]; //discard every second
                    for (int y = 1; y <= m; y++)
                        qf[y, x, i] = ft[i - 1].ftable[x * 2 - 1, y * 2 - 1];
                }
            // phif = Matrix<double>.Build.DenseOfArray(phif).Transpose().ToArray();

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
                double[] hFind = new double[n[i] + 1];
                for (int x = 1; x <= n[i] + 1; x++)
                    hFind[x - n[i] + 1] = h[i, x];
                ii = Find(he[j], hFind);
                for (k = 1; k <= n[i] - ii; k++)
                {
                    h[j, n[j] + k] = h[i, ii + k];//test these
                    phi[j, n[j] + k] = phie[j] + Ks[j] * (h[i, ii + k] - he[j]);
                }
                n[j] = n[j] + n[i] - ii;
            }
            phi1max = phi[n[1], 1];

            // Get phi for same h.
            if (h[1, 1] > h[1, 2])
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
            absh = MathUtilities.Subtract_Value(absh, h[1, i]);
            for (int x = 0; x < absh.Length; x++)
                absh[x] = Math.Abs(absh[x]);
            id = MinLoc(absh);
            if (h[id, j] >= h[1, 1])
                id--;

            //phii(j,:) for soil j will match h(i,:) from soil i and h(j, 1:id) from soil j.
            //phii(i,:) for soil i will match h(i,:) and fill in for h(j, 1:id).
            for (int iid = 1; iid <= id; iid++)
                phii[iid, j] = phi[iid, j]; // keep these values

            // But interpolate to match values that start at greater h.
            jj = id + 1; //h(j,id+1) to be checked first
            phii[id + n[i], j] = phi[n[j], j]; // last h values match
            for (ii = 1; ii <= n[i] - 1; ii++)
            {
                while (true) //get place of h(i,ii) in h array for soil j
                {
                    if (jj > n[j])
                    {
                        Console.WriteLine("twotbls: h[j,n[j]] <= h[i,ii]; i, j, ii, n[j] = " + i + " " + j + " " + ii + " " + n[j]);
                        Environment.Exit(1);
                    }

                    if (h[jj, j] > h[ii, i])
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
                    hCuco[x - k + 1] = h[x, j];
                    phiCuco[x - k + 1] = phi[x, j];
                }

                co = Soil.Cuco(hCuco, phiCuco); // get cubic coeffs
                v = h[ii, i] - h[k, j];
                phii[id + ii, j] = co[1] + v * (co[2] + v * (co[3] + v * co[4]));
            }
            ni = id + n[i];

            // Generate sensible missing values using quadratic extrapolation.
            co = Fluxes.quadco(new double[4] { 0, phii[1, j], phii[id + 1, j], phii[id + 2, j] }, new double[4] { 0, 0, phi[1, i], phi[2, i] });
            if (co[2] > 0) // +ve slope at zero - ok
            {
                for (int x = 1; x <= id; x++)
                {
                    xval[x] = phii[x, j] - phii[1, j];
                    phii[x, i] = co[i] + xval[x] * (co[2] + xval[x] * co[3]);
                }
            }
            else // -ve slope at zero, use quadratic with zero slope at zero
            {
                co[3] = phi[1, i] / Math.Pow(phii[id + 1, j], 2);
                for (int x = 1; x <= id; x++)
                    phii[x, i] = co[3] * Math.Pow(phii[x, j], 2);
            }

            // phii(i,id+1:ni)=phi(i,1:n(i))
            double[] phin = new double[ni - id + 1];
            for (int x = 1; x <= n[i]; x++)
                phin[x] = phi[x, i];
            for (int x = id + 1; x <= ni; x++)
                phii[x, i] = phin[x - (id + 1) + 1];

            //hi(1:id) = h(j, 1:id)
            for (int x = 1; x <= id; x++)
                hi[x] = h[x, j];

            // hi(id+1:ni)=h(i,1:n(i))
            for (int x = 1; x <= n[i]; x++)
                hi[id + x] = h[x, i];

            /* hi(1:ni) are h values for the interface tables.
             * phii(1,1:ni) are corresponding interface phi values for upper layer.
             * phii(2,1:ni) are corresponding interface phi values for lower layer.
             * Set up quadratic interpolation coeffs to get phii2 given phii1.
         */
            Matrix<double> coqM = Matrix<double>.Build.DenseOfArray(coq);
            Vector<double>[] quadcoV = new Vector<double>[ni - 2 + 1];
            for (i = 1; i <= ni - 2; i++)
            {
                quadcoV[i] = Vector<double>.Build.DenseOfArray(Fluxes.quadco(new double[4] { 0, phii[i, 1], phii[i + 1, 1], phii[i + 2, 1] }, new double[4] { 0, phii[i, 2], phii[i + 1, 2], phii[i + 2, 2] }));
                coqM.SetRow(i, quadcoV[i]);
            }
            Vector<double> lincoV = Vector<double>.Build.DenseOfArray(linco(new double[3] { 0, phii[ni - 1, 1], phii[ni, 1] }, new double[3] { 0, phii[ni - 1, 2], phii[ni, 2] }));
            coqM.SetRow(ni - 1, lincoV);
            coq = coqM.ToArray();
            coq[ni - 1, 3] = 0;
            coq[1, 3] = 0.10467559869575349; //debug

            double[,] getco = new double[20, 20];

            // Set up cubic coeffs to get fluxes q given phi.
            Extensions.Log("twofluxes h", "d2", h);
            for (j = 1; j <= nft[2]; j++)
            {
                k = 1;
                ip = 1;
                while (true)
                {
                    phico2[k] = phif[ip, 2];
                    double[] co2co = Soil.Cuco(new double[5] { 0, phif[ip, 2], phif[ip + 1, 2], phif[ip + 2, 2], phif[ip + 3, 2] },
                                               new double[5] { 0, qf[j, ip, 2], qf[j, ip + 1, 2], qf[j, ip + 2, 2], qf[j, ip + 3, 2] });
                    for (int x = 1; x < co2co.Length; x++)
                        co2[j, k, x] = co2co[x];
                    ip += 3;
                    if (ip == nft[2])
                        break;
                    if (ip > nft[2])
                        ip = nft[2] - 3;
                    k++;
                }
                for (int x = 1; x < 20; x++)
                    for (int y = 1; y < 20; y++)
                        getco[y, x] = co2[y, x, 1];
            }

            nco2 = k;

            // Get fluxes
            nit = 0;
            for (i = 1; i <= nft[1]; i++) //step through top phis
            {
                vlast = phif[i, 1];
                k = 1;
                ip = 1;
                Matrix<double> co1M = Matrix<double>.Build.DenseOfArray(co1);
                while (true)
                {
                    phico1[k] = phif[ip, 1];
                    co1M.SetRow(k, Soil.Cuco(new double[5] { 0, phif[ip, 1], phif[ip + 1, 1], phif[ip + 2, 1], phif[ip + 3, 1] },
                                                new double[5] { 0, qf[ip, i, 1], qf[ip + 1, i, 1], qf[ip + 2, i, 1], qf[ip + 3, i, 1] }));
                    ip += 3;
                    if (ip == nft[1])
                        break;
                    if (ip > nft[1])
                        ip = nft[1] - 3;
                    k++;
                }
                co1 = co1M.ToArray();
                nco1 = k;
                for (j = 1; j <= nft[2]; j++) // bottom phis
                {
                    v = vlast;
                    for (k = 1; k <= maxit; k++) // solve for upper interface phi giving same fluxes
                    {
                        fd(v, out f, out df, out q1);
                        nit += 1;
                        dx = f / df; // Newton's method - almost always works
                        v = Math.Min(10.0 * phif[nft[1], 1], Math.Max(phii[1, 1], v - dx));
                        e = Math.Abs(f / q1);
                        if (e < rerr)
                            break;
                        vlast = v;
                    }
                    if (k > maxit) //failed - bracket q and use bisection
                    {
                        v1 = phii[1, 1];
                        fd(v1, out f1, out df, out q1);
                        if (f1 <= 0.0) // answer is off table - use end value
                        {
                            qp[j, i] = q1;
                            continue;
                        }
                        v2 = phii[ni, 1];
                        fd(v2, out f2, out df, out q1);
                        for (k = 1; k <= maxit; k++)
                        {
                            if (f1 * f2 < 0.0)
                                break;
                            v1 = v2;
                            f1 = f2;
                            v2 = 2.0 * v1;
                            fd(v2, out f2, out df, out q1);
                        }
                        if (k > maxit)
                        {
                            Console.WriteLine(v1 + " " + v2 + " " + f1 + " " + f2);
                            v1 = phii[1, 1];
                            fd(v1, out f1, out df, out q1);
                            Console.WriteLine(v1 + " " + f1);
                            Console.WriteLine("twotbls: too many iterations at i, j = " + i + " " + j);
                            Environment.Exit(1);
                        }
                        for (k = 1; k <= maxit; k++)
                        {
                            v = 0.5 * (v1 + v2);
                            fd(v, out f, out df, out q1);
                            e = Math.Abs(f / q1);
                            if (e < rerr)
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
                            Environment.Exit(1);
                        }
                    }
                    Extensions.Log("twotables f", "d", f);
                    Extensions.Log("twotables df", "d", df);
                    Extensions.Log("twotables q1", "d", q1);
                    // Solved
                    qp[j, i] = q1;
                } //end j
            } //end i

            //interpolate extra fluxes
            for (i = 1; i <= 2; i++)
            {
                nfi[i] = nft[i] - 1;
                for (int x = 1; x <= nfi[i]; x++)
                    phifi[x, i] = 0.5 * (phif[x, i] + phif[x + 1, i]);
            }

            Matrix<double> phifM = Matrix<double>.Build.DenseOfArray(phif);
            Matrix<double> phifiM = Matrix<double>.Build.DenseOfArray(phifi);
            Matrix<double> qpM = Matrix<double>.Build.DenseOfArray(qp);
            Matrix<double> qi1M = Matrix<double>.Build.DenseOfArray(qi1);
            Matrix<double> qi2M = Matrix<double>.Build.DenseOfArray(qi2);
            Matrix<double> qi3M = Matrix<double>.Build.DenseOfArray(qi3);

            for (i = 1; i <= nft[1]; i++)
            {
                qi1M.SetColumn(i, Fluxes.quadinterp(phifM.Column(2).ToArray(), qpM.Column(i).ToArray(), nft[2], phifiM.Column(2).ToArray()));
            }
            for (j = 1; j <= nft[2]; j++)
            {
                qi2M.SetRow(j, Fluxes.quadinterp(phifM.Column(1).ToArray(), qpM.Row(j).ToArray(), nft[1], phifiM.Column(1).ToArray()));
            }
            for (j = 1; j <= nfi[2]; j++)
            {
                qi3M.SetRow(j, Fluxes.quadinterp(phifM.Column(1).ToArray(), qi1M.Row(j).ToArray(), nft[1], phifiM.Column(1).ToArray()));
            }

            qi1 = qi1M.ToArray();
            qi2 = qi2M.ToArray();
            qi3 = qi3M.ToArray();

            // Put all the fluxes together
            i = nft[1] + nfi[1];
            j = nft[2] + nfi[2];

            // qi5(1:i:2,1:j:2)=qp(1:nft[1],1:nft[2])
            for (int i = 1; i < 101; i += 2)
                for (int j = 1; j < 101; j += 2)
                    qi5[j, i] = qp[j / 2 + 1, i / 2 + 1];

            // qi5(1:i: 2, 2:j: 2) = qi1(1:nft[1], 1:nfi[2])
            for (int i = 1; i < 101; i += 2)
                for (int j = 2; j < 101; j += 2)
                    qi5[j, i] = qi1[(j-1) / 2 + 1, i / 2 + 1];

            // qi5(2:i:2,1:j:2)=qi2(1:nfi[1],1:nft[2])
            for (int i = 2; i < 101; i += 2)
                for (int j = 1; j < 101; j += 2)
                    qi5[j, i] = qi2[j / 2 + 1, (i - 1) / 2 + 1];

            // qi5(2:i:2,2:j:2)=qi3(1:nfi[1],1:nfi[2])
            for (int i = 2; i < 101; i += 2)
                for (int j = 2; j < 101; j += 2)
                    qi5[j, i] = qi3[(j - 1) / 2 + 1, (i - 1) / 2 + 1];

            // phii5(1, 1:i: 2) = phif(1, 1:nft[1])
            for (int i = 1; i < 101; i += 2)
                phii5[i, 1] = phif[i / 2 + 1, 1];

            // phii5(1,2:i:2)=phifi(1,1:nfi[1])
            for (int i = 2; i < 101; i += 2)
                phii5[i, 1] = phifi[(i - 1) / 2 + 1, 1];

            // phii5(2,1:j:2)=phif(2,1:nft[2])
            for (int i = 1; i < 101; i += 2)
                phii5[i, 2] = phif[i / 2 + 1, 2];

            // phii5(2,2:j:2)=phifi(2,1:nfi[2])
            for (int i = 2; i < 101; i += 2)
                phii5[i, 2] = phifi[(i-1) / 2 + 1, 2];


            // Assemble flux table
            ftwo.fend = new FluxEnd[2];
            for (ie = 0; ie < 2; ie++)
            {
                ftwo.fend[ie].sid = sp[ie].sid;
                ftwo.fend[ie].nfu = ft[ie].fend[1].nfu;
                ftwo.fend[ie].nft = ft[ie].fend[1].nft;
                ftwo.fend[ie].dz = ft[ie].fend[1].dz;
                ftwo.fend[ie].phif = ft[ie].fend[1].phif;
            }

            double[,] qi5Slice = new double[i + 1, j + 1];
            for (int x = 1; x <= i; x++)
                for (int y = 1; y <= j; y++)
                    qi5Slice[x, y] = qi5[x, y];
            ftwo.ftable = qi5Slice;

            return ftwo;
        }

        public static double[] Testfd(double phia, int[] nft, double[] phie, double[] Ks,
                                      double[,] phif, double[,,] qf, double[] he, double[,] coq,
                                      double[,] co1, int nco1, double[] phico1, double phi1max,
                                      double[,] phii, double[,,] co2, double[] phico2, int j)
        {
            double f;
            double d;
            double q;
            TwoFluxes.nft = nft;
            TwoFluxes.phie = phie;
            TwoFluxes.Ks = Ks;
            TwoFluxes.phif = phif;
            TwoFluxes.qf = qf;
            TwoFluxes.he = he;
            TwoFluxes.coq = coq;
            TwoFluxes.co1 = co1;
            TwoFluxes.nco1 = nco1;
            TwoFluxes.phico1 = phico1;
            TwoFluxes.phi1max = phi1max;
            TwoFluxes.phii = phii;
            TwoFluxes.co2 = co2;
            TwoFluxes.phico2 = phico2;
            TwoFluxes.j = j;
            fd(phia, out f, out d, out q);
            return new double[] { f, d, q };
        }

        private static void fd(double phia, out double f, out double d, out double q)
        {
            //Returns flux difference f, deriv d and upper flux q.
            //phia - phi at interface in upper path.
            double h, phib, der, v, vm1, qv, qvm1, q1, q1d, q2, q2d;
            phib = 0;
            q1 = 0;
            der = 0;
            q1d = 0;

            Extensions.Log("fd phia", "d", phia);
            Extensions.Log("fd phialast", "d", phialast);
            Extensions.Log("fd phi1max", "d", phi1max);
            if (phia != phialast)
            {
                if (phia > phi1max) // both saturated - calc der and lower interface phi
                {
                    h = he[1] + (phia - phie[1]) / Ks[1];
                    phib = phie[2] + Ks[2] * (h - he[2]);
                    der = Ks[2] / Ks[1];
                }
                else // use quadratic interpolation to get them
                {
                    double[] phiiFind = new double[ni + 1];
                    for (int x = 1; x <= ni; x++)
                        phiiFind[x] = phii[x, 1];
                    ii = Find(phia, phiiFind);
                    v = phia - phii[ii, 1];
                    der = coq[ii, 2] + v * 2.0 * coq[ii, 3];
                    phib = coq[ii, 1] + v * (coq[ii, 2] + v * coq[ii, 3]);
                }
                // Get upper flux and deriv.
                v = phif[nft[1], 1];
                if (phia > v) // off table - extrapolate
                {
                    vm1 = phif[nft[1] - 1, 1];
                    qv = qf[nft[1], i, 1];
                    qvm1 = qf[nft[1] - 1, i, 1];
                    q1d = (qv - qvm1) / (v - vm1);
                    q1 = qv + q1d * (phia - v);
                }
                else // use cubic interpolation
                {
                    ceval1(phia, out q1, out q1d);
                    Extensions.Log("fd q1", "d", q1);
                    Extensions.Log("fd q1d", "d", q1d);
                }
                phialast = phia;
            }
            // Get lower flux and deriv in same way.
            v = phif[nft[2], 2];
            Extensions.Log("fd v-lower", "d", v);
            if (phib > v)
            {
                vm1 = phif[nft[2] - 1, 2];
                qv = qf[j, nft[2], 2];
                qvm1 = qf[j, nft[2] - 1, 2];
                q2d = (qv - qvm1) / (v - vm1);
                q2 = qv + q2d * (phib - v);
            }
            else
                ceval2(j, phib, out q2, out q2d);
            // Set return values.
            f = q1 - q2;
            d = q1d - q2d * der;
            q = q1;
        }


        public static double[] Testceval1(double phi, double[,] co1, double[] phico1, int nco1)
        {
            double q;
            double qd;
            TwoFluxes.co1 = co1;
            TwoFluxes.phico1 = phico1;
            TwoFluxes.nco1 = nco1;
            ceval1(phi, out q, out qd);
            return new double[] { q, qd };
        }

        private static void ceval1(double phi, out double q, out double qd)
        {
            //Return flux q and deriv qd given phi.
            //Use cubic interpolation in table(phico1, co1).
            int i1, i2, im;
            double x;
            i1 = 1;
            i2 = nco1 + 1; // allow for last interval in table
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
            q = co1[i1, 1] + x * (co1[i1, 2] + x * (co1[i1, 3] + x * co1[i1, 4]));
            qd = co1[i1, 2] + x * (2.0 * co1[i1, 3] + x * 3.0 * co1[i1, 4]);
        }

        public static double[] Testceval2(double phi, double[] co2, double[] phico2, int j, int nco1)
        {
            double q;
            double qd;
            TwoFluxes.co2[1, 1, 1] = co2[0];
            TwoFluxes.co2[1, 1, 2] = co2[1];
            TwoFluxes.co2[1, 1, 3] = co2[2];
            TwoFluxes.co2[1, 1, 4] = co2[3];
            TwoFluxes.phico2 = phico2;
            TwoFluxes.j = j;
            TwoFluxes.nco1 = nco1;
            ceval2(j, phi, out q, out qd);
            return new double[] { q, qd };
        }

        private static void ceval2(int j, double phi, out double q, out double qd)
        {
            //Return flux q and deriv qd given phi.
            //Use cubic interpolation in table(phico1, co1).
            int i1, i2, im;
            double x;

            i1 = 1;
            i2 = nco2 + 1; // allow for last interval in table
            while (true) //use bisection to find place
            {
                if (i2 - i1 <= 1)
                    break;

                im = (i1 + i2) / 2;
                if (phico2[im] > phi)
                    i2 = im;
                else i1 = im;
            }

            //Interpolate
            x = phi - phico2[i1];
            q = co2[j, i1, 1] + x * (co2[j, i1, 2] + x * (co2[j, i1, 3] + x * co2[j, i1, 4]));
            qd = co2[j, i1, 2] + x * (2.0 * co2[j, i1, 3] + x * 3.0 * co2[j, i1, 4]);
        }

        //Return i where xa(i) <= x < xa(i+1)
        public static int Find(double x, double[] xa)
        {
            int i1 = 1;
            int i2 = Array.IndexOf(xa.Skip(1).ToArray(), 0);
            int im;

            if (i2 == -1) // in case there are no 0's in xa
                i2 = xa.Length;

            while (true) //use bisection
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

        public static int MinLoc(double[] array)
        {
            int pos = 1;
            for (int i = 1; i < array.Length; i++)
                if (array[i] < array[pos])
                    pos = i;

            return pos;
        }

        private static double[] linco(double[] x, double[] y)
        {
            double[] co = new double[4];
            //Return linear interpolation coeffs co.
            co[1] = y[1];
            co[2] = (y[2] - y[1]) / (x[2] - x[1]);
            return co;
        }
    }
}