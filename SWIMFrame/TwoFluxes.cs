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
            int mx = 100;
            int maxit = 20;
            int i, j, k, m, ne, ni, id, ip, nco1, nco2, nit, ie, ii, jj;
            int[] nft = new int[2 + 1];
            int[] nfu = new int[2 + 1];
            int[] n = new int[2 + 1];
            int[] nfi = new int[2 + 1];
            double rerr = 1e-3;
            double phi1max, dhe, v, vlast, dx, e, f, df, q1, phialast, v1, v2, f1, f2;
            double[] he = new double[2 + 1];
            double[] phie = new double[2 + 1];
            double[] Ks = new double[2 + 1];
            double[] co = new double[4 + 1];
            double[] xval = new double[mx + 1];
            double[] phico1 = new double[mx + 1];
            double[] phico2 = new double[mx + 1];
            double[] y2 = new double[3 * mx + 1];
            double[] hi = new double[3 * mx + 1];
            double[,] phif = new double[2, mx + 1];
            double[,] phifi = new double[2, mx + 1];
            double[,] phii5 = new double[2, mx + 1];
            double[,] coq = new double[3 + 1, 3 * mx + 1];
            double[,] co1 = new double[4 + 1, mx + 1];
            double[,] qp = new double[mx + 1, mx + 1];
            double[,] y22 = new double[mx + 1, mx + 1];
            double[,] qi1 = new double[mx + 1, mx + 1];
            double[,] qi2 = new double[mx + 1, mx + 1];
            double[,] qi3 = new double[mx + 1, mx + 1];
            double[,] qi4 = new double[mx + 1, mx + 1];
            double[,] h = new double[2 + 1, 3 * mx + 1];
            double[,] phi = new double[2 + 1, 3 * mx + 1];
            double[,] phii = new double[2 + 1, 3 * mx + 1];
            double[,,] qf = new double[2 + 1, mx + 1, mx + 1];
            double[,,] y2q = new double[2 + 1, mx + 1, mx + 1];
            double[,,] co2 = new double[4 + 1, mx + 1, mx + 1];
            FluxTable ftwo = new FluxTable();
            FluxTable[] ft = { new FluxTable(), new FluxTable() };
            SoilProps[] sp = { new SoilProps(), new SoilProps() };

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
            Matrix<double> phiim = Matrix<double>.Build.DenseOfArray(phii);
            Matrix<double> phim = Matrix<double>.Build.DenseOfArray(phi);
            phii[j]

            return new FluxTable();
        }

        private static int Find(double x, double[,] h, int n, int i)
        {
            throw new NotImplementedException();
        }

        private static int MinLoc (double[] array)
        {
            int pos=1;
            for (int i = 1; i < array.Length; i++)
                if (array[i] < array[pos])
                    pos = i;

            return pos;
        }
    }
}