using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using APSIM.Shared.Utilities;

namespace SWIMFrame
{
    public class SoilData
    {
        public int n, nsp, nft;
        int[] soilloc, pathloc;
        int[,] philoc;
        public double[] x, dx, ths, he;
        SoilProps[] sp;
        FluxTable[] ft;
        FluxPath[] fpath;
        PathEnd pe1, pe2;
        int nphi = 101;
        double[] rdS;
        double[,] S, phi, dphidS, K, dKdS;

        /// <summary>
        /// Reads tables and sets up data
        /// </summary>
        /// <param name="nin">Number of layers.</param>
        /// <param name="sid">Soil ID of layers.</param>
        /// <param name="xin">Depth to bottoms of layers.</param>
        public void GetTables(int nin, int[] sid, double[] xin)
        {
            double small = 1E-5;
            string id, mm, sfile;
            string[] ftname = new string[2];
            int i, j, ns, np, nc, i1;
            int[] isoil = new int[nin + 1];
            int[,] isid = new int[nin + 1 + 1, 2 + 1];
            int[] jt = new int[nin + 1]; //0-based FORTRAN array
            double[,] dz = new double[nin + 1 + 1, 2 + 1];
            double[] hdx = new double[nin + 1]; //0-based FORTRAN array
            double x1;

            n = nin;
            x = xin;
            dx = MathUtilities.Subtract(x.Slice(2, n), x.Slice(1, n - 1));

            //  Set up locations in soil, path, S and phi arrays.
            for (int x = 1; x < philoc.GetLength(0); x++)
                for (int y = 1; y < philoc.GetLength(1); y++)
                    philoc[x, y] = 1;

            // Get ns different soil idents in array isoil.
            ns = 0;
            for (i = 1; i <= n; i++)
            {
                if (isoil.Select(x => x == sid[i]).Count() == 0) //TEST
                {
                    ns++;
                    isoil[ns] = sid[i];
                }

                double[] tempisoil = new double[i + 1];
                for (int x = 1; x <= i; x++)
                    tempisoil[x] = Math.Abs(isoil[x] - sid[x]);
                soilloc[i] = TwoFluxes.MinLoc(tempisoil);
            }

            // Get soil idents in array isid and lengths in array dz for the np paths.
            dx = MathUtilities.Subtract(x, x.Skip(x.Length - 1).Concat(x.Take(x.Length - 1)).ToArray()); //TEST
            Array.Copy(dx, 1, x, 1, dx.Length / 2); //TEST

            //hdx=(/0.0,0.5*dx,0.0/) ! half delta x
            for (int x = 0; x < hdx.Length; x++)
            {
                if (x == 0 || x == hdx.Length - 1)
                    hdx[x] = 0;
                else
                    hdx[x] = dx[x] / 2;
            }

            Array.Copy(sid, 1, jt, 0, sid.Length); //just TEST everything...
            np = 0;
            for (i = 0; i <= n; i++)
            {
                isid[np + 1, 1] = jt[i];
                isid[np + 1, 2] = jt[i + 1];
                if (jt[i] == jt[i + 1]) //no interface between types
                {
                    dz[np + 1, 1] = hdx[i] + hdx[i + 1];
                    dz[np + 1, 2] = 0;
                }
                else
                {
                    dz[np + 1, 1] = hdx[i];
                    dz[np + 1, 2] = hdx[i + 1];
                }

                //Increment np if this path is new.
                for (j = 1; j <= np; j++)
                {
                    if (isid[j, 1] != isid[np + 1, 1] || isid[j, 2] != isid[np + 1, 2])
                        continue;

                    double[] absdz = new double[dz.GetLength(1)];
                    for (int x = 0; x < absdz.Length; x++)
                    {
                        absdz[x] = Math.Abs(dz[j, x] = dz[np + 1, x]);
                    }
                    if (MathUtilities.Sum(absdz) < small)
                        break;
                }
                if (j > np)
                    np++;
                pathloc[i] = j; //store path location
            }

            // Read soil properties
            nsp = ns;
            sp = new SoilProps[nsp + 1];
            for (i = 1; i <= ns; i++)
            {
                sp[i] = Soil.ReadProps("soil" + isoil[i] + ".dat");
            }

            //Set ths and he
            for (i = 1; i <= n; i++)
            {
                ths[i] = sp[soilloc[i]].ths;
                he[i] = sp[soilloc[i]].he;
            }

            //Set up S and phi arrays to get phi from S.
            for (i = 1; i <= ns; i++)
            {
                nc = sp[i].nc;

                //S(:,i)=sp(i)%Sc(1)+(/(j,j=0,nphi-1)/)*(sp(i)%Sc(nc)-sp(i)%Sc(1))/(nphi-1)
                for (int x = 0; x < nphi; x++)
                    S[x, i] = sp[i].Sc[1] + x * (sp[i].Sc[nc] - sp[i].Sc[1]) / (nphi - 1);
                phi[1, i] = sp[i].phic[1];
                phi[nphi, i] = sp[i].phic[nc];
                j = 1;
                for (i1 = 2; i1 <= nphi - 1; i1++)
                {
                    while (true)
                    {
                        if (S[i1, i] < sp[i].Sc[j + 1])
                            break;
                        j++;
                    }
                    x1 = S[i1, i] - sp[i].Sc[j];
                    phi[i1, i] = sp[i].phic[j] + x1 * (sp[i].phico[1, j] + x1 * (sp[i].phico[2, j] + x1 * sp[i].phico[3, j]));
                    x1 = phi[i1, i] - sp[i].phic[j];
                    K[i1, i] = sp[i].Kc[j] + x1 * (sp[i].Kco[1, j] + x1 * (sp[i].Kco[2, j] + x1 * sp[i].Kco[3, j]));
                }

                rdS[i] = 1.0 / (S[2, i] - S[1, i]);

                for (int x = 2; x <= nphi; x++)
                {
                    dphidS[x - 1, i] = rdS[i] * (phi[x, i] - phi[x - 1, i]);
                    dKdS[x - 1, i] = rdS[i] * (K[x, i] - K[x - 1, i]);
                }
            }

            // Read flux tables and form flux paths.
            ft = new FluxTable[np];
            fpath = new FluxPath[np];
            nft = np;
            for (i = 1; i <= np; i++)
            {
                for (j = 1; j <= 2; j++)
                {
                    id = isid[i, j].ToString();
                    mm = Math.Round(10.0 * dz[i, j]).ToString();
                    Console.WriteLine(id);
                    Console.WriteLine(mm);
                    ftname[j] = "soil" + id + "dz" + mm;
                }
                if (isid[i, 1] == isid[i, 2])
                    sfile = ftname[1] + ".dat";
                else
                    sfile = ftname[1] + "_" + ftname[2] + ".dat";
                ft[i] = Fluxes.ReadFluxTable(sfile);

                // Set up flux path data.
                j = jsp(ft[i].fend[1].sid, ns, isoil);
                pe1.nfu = ft[i].fend[1].nfu;
                pe1.nft = ft[i].fend[1].nft;
                pe1.nld = sp[j].nld;
                pe1.ths = sp[j].ths;
                pe1.rdS = rdS[j];
                pe1.Ks = sp[j].ks;
                pe1.S = Extensions.GetRowCol(S, j, false);
                pe1.phi = Extensions.GetRowCol(phi, j, false);
                pe1.dphidS = Extensions.GetRowCol(dphidS, j, false);
                pe1.K = Extensions.GetRowCol(K, j, false);
                pe1.dKdS = Extensions.GetRowCol(dKdS, j, false);
                pe1.Sd = sp[j].Sd;
                pe1.lnh = sp[j].lnh;
                pe1.phif = ft[i].fend[1].phif;
                fpath[i].dz = ft[i].fend[1].dz;
                if (ft[i].fend[2].sid == ft[i].fend[1].sid)
                    pe2 = pe1;
                else //composite path
                {
                    j = jsp(ft[i].fend[2].sid, ns, isoil);
                    pe2.nfu = ft[i].fend[2].nfu;
                    pe2.nft = ft[i].fend[2].nft;
                    pe2.nld = sp[j].nld;
                    pe2.ths = sp[j].ths;
                    pe2.rdS = rdS[j];
                    pe2.Ks = sp[j].ks;
                    pe2.S = Extensions.GetRowCol(S, j, false);
                    pe2.phi = Extensions.GetRowCol(phi, j, false);
                    pe2.dphidS = Extensions.GetRowCol(dphidS, j, false);
                    pe2.K = Extensions.GetRowCol(K, j, false);
                    pe2.dKdS = Extensions.GetRowCol(dKdS, j, false);
                    pe2.Sd = sp[j].Sd;
                    pe2.lnh = sp[j].lnh;
                    pe2.phif = ft[i].fend[2].phif;
                    fpath[i].dz = ft[i].fend[2].dz;
                }
                fpath[i].pend[1] = pe1;
                fpath[i].pend[2] = pe2;
                fpath[i].ftable = ft[i].ftable;
            }
        }

        private int jsp(int sid, int ns, int[] isoil)
        {
            int jsp;
            // Get soil prop location in isoil list
            for (jsp = 1; jsp <= ns; jsp++)
                if (sid == isoil[jsp])
                    return jsp;

            if (jsp > ns)
            {
                Console.WriteLine("Data for soil " + sid + " not found");
                Environment.Exit(1);
            }
            return jsp;
        }

        /// <summary>
        /// Gets fluxes q and partial derivs qya, qyb from stored tables.
        /// </summary>
        /// <param name="iq">flux number (0 to n, with 0 and n top and bottom fluxes).</param>
        /// <param name="iS">satn status (0 if unsat, 1 if sat) of layers above and below. NOTE: It's 'iS' in C# as 'is' is a reserved word.</param>
        /// <param name="x">S or h-he of layers above and below.</param>
        /// <param name="q">flux.</param>
        /// <param name="qya">partial derivs of q wrt x(1) and x(2).</param>
        /// <param name="qyb">partial derivs of q wrt x(1) and x(2).</param>
        public void GetQ(int iq, int[] iS, double[] x, out double q, out double qya, out double qyb)
        {
            bool vapour;
            int i, j, i2;
            int[] k = new int[2];
            double dlnhdS, lnh, h, cvs, rhow, dv, vc;
            double[] hr = new double[2];
            double[] dhrdS = new double[2];
            double[] poros = new double[2];
            double[] dv1 = new double[2];
            double[] cv = new double[2];
            double[] dcvdS = new double[2];
            double v, phii, f1, f2, f3, f4, Smin;
            double[] phix = new double[2];
            double[] rdphif = new double[2];
            double[] u = new double[2];
            double[] omu = new double[2];
            double[] phif;
            double[,] qf;
            FluxPath path;
            PathEnd pe;

            vapour = false;
            path = fpath[pathloc[iq]];
            for (j = 1; j <= 2; j++)
            {
                pe = path.pend[j];
                phif = pe.phif;
                if (iS[j] == 0) //end unsaturated
                {
                    Smin = pe.S[1];
                    v = Math.Min(x[j], 0.99999);

                    // set up for vapour flux if needed
                    hr[j] = 1.0;
                    dhrdS[j] = 0.0;
                    poros[j] = pe.ths * (1.0 - v); //could use (ths/0.93-th)?
                    if (v < pe.Sd[pe.nld]) //get rel humidity etc
                    {
                        vapour = true;
                        i = pe.nld - 1;
                        while (true) //search down
                        {
                            if (pe.Sd[i] <= v)
                                break;
                            i--;
                        }
                        dlnhdS = (pe.lnh[i + 1] - pe.lnh[i]) / (pe.Sd[i + 1] - pe.Sd[i]);
                        lnh = pe.lnh[i] + dlnhdS * (v - pe.Sd[i]); //linear interp
                        h = -Math.Exp(lnh);
                        hr[j] = Math.Exp(7.25E-7 * h); //get rel humidy from h
                        dhrdS[j] = 7.25E-7 * hr[j] * h * dlnhdS;
                    }

                    // Get phi for flux table from S using linear interp.
                    if (v < Smin)
                        v = Smin;
                    i = 1 + (int)Math.Truncate(pe.rdS * (v - Smin));
                    phix[j] = pe.dphidS[i];
                    phii = pe.phi[i] + phix[j] * (v - pe.S[i]);
                }
                else //end saturated
                {
                    // Set up for vapour flux if needed.
                    hr[j] = 1.0;
                    dhrdS[j] = 0.0;
                    poros[j] = 0.0;

                    //Get phi for flux table from h-he (x(j)).
                    phix[j] = pe.Ks;
                    i = pe.nfu;
                    phii = phif[i] + x[j] * pe.Ks;
                }

                // Get place in flux table.
                i = philoc[iq, j];
                i2 = pe.nft;
                if (phii >= phif[i])
                {
                    while (true) //search up
                    {
                        if (phii < phif[i + 1]) //found
                            break;
                        if (i + 1 == i2)
                            break; //outside array
                        i++;
                    }
                }
                else
                {
                    while (true) //search down
                    {
                        i--;
                        if (i < 1)
                        {
                            Console.WriteLine("GetQ: phi < phif[1] in table.");
                            Environment.Exit(1);
                        }
                        if (phii > phif[i]) //found
                            break;
                    }
                }
                philoc[iq, j] = i; // save location in phif
                rdphif[j] = 1.0 / (phif[i + 1] - phif[i]);
                u[j] = rdphif[j] * (phii - phif[i]);
                k[j] = i;
            }

            // Get flux from table
            qf = path.ftable;
            f1 = qf[k[1], k[2]]; //use bilinear interp
            f2 = qf[k[1] + 1, k[2]];
            f3 = qf[k[1], k[2] + 1];
            f4 = qf[k[1] + 1, k[2] + 1];
            omu = MathUtilities.Subtract_Value(u, 1.0);
            q = omu[1] * omu[2] * f1 + u[1] * omu[2] * f2 + omu[1] * u[2] * f3 + u[1] * u[2] * f4;
            qya = phix[1] * rdphif[1] * (omu[2] * (f2 - f1) + u[2] * (f4 - f3));
            qyb = phix[2] * rdphif[2] * (omu[1] * (f3 - f1) + u[1] * (f4 - f2));
            if (vapour) // add vapour flux
            {
                for (j = 1; j <= 2; j++)
                {
                    cvs = 0.0173E-6; // to vary with temp
                    rhow = 0.9982E-3; // ditto
                    dv = 864.0; //ditto
                    dv1[j] = 0.66 * poros[j] * dv / rhow;
                    cv[j] = hr[j] * cvs;
                }
                vc = 0.5 * (dv1[1] + dv1[2]) / path.dz;
                q = q + vc * (cv[1] - cv[2]);
                qya = qya + vc * dcvdS[1];
                qyb = qyb - vc * dcvdS[2];
            }
        }

        /// <summary>
        /// Gets conductivity and deriv from stored tables.
        /// </summary>
        /// <param name="iq">x number (0 to n, with 0 and n top and bottom fluxes).</param>
        /// <param name="iS">satn status (0 if unsat, 1 if sat) of layers above and below.</param>
        /// <param name="x">S or h-he of layers above and below.</param>
        /// <param name="q">K for x.</param>
        /// <param name="qya">deriv of K wrt x.></param>
        public void GetK(int iq, int iS, double x, out double q, out double qya)
        {
            int i;
            double v;
            double Smin;
            FluxPath path;
            PathEnd pe;

            path = fpath[pathloc[iq]];
            pe = path.pend[1];
            if(iS==0) //end unsaturated
            {
                Smin = pe.S[1];
                v = Math.Min(x, 0.99999);
                if (v < Smin)
                    v = Smin;
                if (v < Smin)
                    v = Smin;
                i=(int)Math.Truncate(pe.rdS * (v - Smin));
                qya = pe.dKdS[i];
                q = pe.K[i] + qya * (v - pe.S[i]);
            }
            else //end saturated
            {
                q = pe.Ks;
                qya = 0.0;
            }
        }

        // Returns S and, if required, Sh given h and soil layer no. il.
        // note in FORTRAN Sh is optional -JF
        public void Sofh(double h, int il, out double S, out double Sh)
        {
            int i, j;
            double d, lnh;
            i = soilloc[il];
            if (h > sp[i].he)
            {
                S = 1.0;
                Sh = 0.0;
            }
            else if (h >= sp[i].h[1]) // use linear interp in (ln(-h),S)
            {
                j = TwoFluxes.Find(h, sp[i].h);
                d = (sp[i].S[j + 1] - sp[i].S[j]) / Math.Log(sp[i].h[j + 1] / sp[i].h[j]);
                S = sp[i].S[j] + d * Math.Log(h / sp[i].h[j]);
                Sh = d / h;
            }
            else
            {
                lnh = Math.Log(-h);
                if (-lnh >= -sp[i].lnh[1]) // use linear interp in (ln(-h),S)
                {
                    j = TwoFluxes.Find(-lnh, sp[i].lnh);
                    d = (sp[i].Sd[j + 1] - sp[i].Sd[j]) / (sp[i].lnh[j + 1] - sp[i].lnh[j]);
                    S = sp[i].Sd[j] + d * (lnh - sp[i].lnh[j]);
                    Sh = d / h;
                }
                else
                {
                    S = sp[i].Sd[1];
                    Sh = 0.0;
                }
            }    
        }

        // Returns h and, if required, hS given S and soil layer no. il.
        // note in FORTRAN hS is optional -JF
        public void hofS(double S, int il, out double h, out double hS)
        {
            int i, j;
            double d, lnh;
            h = 0;
            hS = 0;
            i = soilloc[il];

            if (S > 1.0 || S < 0.0)
            {
                Console.WriteLine("hofS: S out of range (0, 1), S = " + S);
                Environment.Exit(1);
            }

            if (S >= sp[i].S[1]) // then !use linear interp in (S, ln(-h))
            {
                j = TwoFluxes.Find(S, sp[i].S);
                d = Math.Log(sp[i].h[j + 1] / sp[i].h[j]) / (sp[i].S[j + 1] - sp[i].S[j]);
                lnh = Math.Log(-sp[i].h[j]) + d * (S - sp[i].S[j]);
                h = -Math.Exp(lnh);
                hS = d * h;
            }
            else if (S >= sp[i].Sd[1]) // then !use linear interp in (S, ln(-h))
            {
                j = TwoFluxes.Find(S, sp[i].Sd);
                d = (sp[i].lnh[j + 1] - sp[i].lnh[j]) / (sp[i].Sd[j + 1] - sp[i].Sd[j]);
                lnh = sp[i].lnh[j] + d * (S - sp[i].Sd[j]);
                h = -Math.Exp(lnh);
                hS = d * h;
            }
        }

        //Returns K and, if required, KS given S and soil layer no. il.
        // note in FORTRAN hS is optional -JF
        private void Kofs(double S, int il, out double K, out double KS)
        {
            int i, j;
            double Kphi, phi, phiS, x;
            i = soilloc[il];

            if (S > 1.0 || S < 0.0)
            {
                Console.WriteLine("KofS: S out of range (0, 1), S = " + S);
                Environment.Exit(1);
            }

            if (S >= sp[i].Sc[1]) //then !use cubic interp
            {
                j = TwoFluxes.Find(S, sp[i].Sc);
                x = S - sp[i].Sc[j];
                phi = sp[i].phic[j] + x * (sp[i].phico[1, j] + x * (sp[i].phico[2, j] + x * sp[i].phico[3, j]));
                phiS = sp[i].phico[1, j] + x * (2.0 * sp[i].phico[2, j] + x * 3.0 * sp[i].phico[3, j]);
                x = phi - sp[i].phic[j];
                K = sp[i].Kc[j] + x * (sp[i].Kco[1, j] + x * (sp[i].Kco[2, j] + x * sp[i].Kco[3, j]));
                Kphi = sp[i].Kco[1, j] + x * (2.0 * sp[i].Kco[2, j] + x * 3.0 * sp[i].Kco[3, j]);
                KS = Kphi * phiS;
            }
            else
            { 
                    K = sp[i].Kc[1]; 
                    KS = 0.0;
            }
        }


    }

        public struct PathEnd
    {
        // all needed refs to end data
        public int nfu, nft, nld;
        public double ths, rdS, Ks;
        public double[] S, phi, dphidS, K, dKdS, Sd, lnh, phif;
    }

    public struct FluxPath
    {
        public double dz; // total path length
        public PathEnd[] pend;
        public double[,] ftable;
    }
}
