using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using APSIM.Shared.Utilities;
using MathNet.Numerics.LinearAlgebra;
namespace SWIMFrame
{
    public static class Flow
    {
        /*
         ! This module allows solution of the equation of continuity for water transport
         ! in a soil profile for a specified period using fluxes read from tables rather
         ! than calculated. The basic references for the methods are:
         ! Parts of Ross, P.J. 2003. Modeling soil water and solute transport - fast,
         ! simplified numerical solutions. Agron. J. 95:1352-1361.
         ! Ross, P.J. 2010. Numerical solution of the equation of continuity for soil
         ! water flow using pre-computed steady-state fluxes.
         ! The user calls sub solve as many times as required for the solution.
         ! Fluxes are provided by subroutine getq in module soildata.
         ! n      - no. of soil layers.
         ! dx     - layer thicknesses.
         ! ths    - saturated water contents of layers.
         ! he     - matric heads of layers at air entry.
         ! getq   - subroutine to find fluxes and derivatives given S and/or matric head.
         ! getK   - subroutine to get conductivity and derivs given S or matric head.
         ! wsinks - subroutine to get layer water extraction rates (cm/h).
         ! ssinks - subroutine to get layer solute extraction rates (mass/h).

         ! DEFINITIONS
         ! For default values, see below. For dimensions, see subroutine readtbls.
         ! Boundary conditions:
         ! botbc    - bottom boundary condn for water; "constant head", "free drainage",
         !            "seepage", or "0.0 flux". Constant head means that matric head h
         !            is specified. Free drainage means 0.0 gradient of matric head,
         !            i.e. unit hydraulic gradient. Seepage means 0.0 flux when the
         !            matric head is below 0.0 and an upper limit of 0.0 for the head.
         ! h0max    - max pond depth allowed before runoff.
         ! hbot     - matric head at bottom of profile when botbc is "constant head".
         ! Sbot     - degree of satn at bottom (needed when hbot<he).
         ! qprecmax - max precipitation (or water input) rate (cm/h) for use with ponded
         !            constant head infiltration. If qprec > qprecmax then actual input
         !            rate is taken to be equal to infiltration plus evaporation rates.
         ! Solution parameters:
         ! dSmax    - max change in S (the "effective saturation") of any unsaturated
         !            layer to aim for each time step; controls time step size.
         ! dSmaxr   - maximum negative relative change in S each time step. This
         !            parameter helps avoid very small or negative S.
         ! dSfac    - a change in S of up to dSfac*dSmax is accepted.
         ! Smax     - max value for layer saturation to allow some overshoot.
         ! dh0max   - allowable overshoot when pond reaches max allowed depth.
         ! h0min    - min (negative) value for surface pond when it empties.
         ! dtmax    - max time step allowed.
         ! dtmin    - min time step allowed; program stops if smaller step required.
         ! dsmmax   - max solute change per time step (see dSmax); user should set this
         !            according to solute units used. Units for different solutes can be
         !            scaled by the user (e.g. to an expected max of around 1.0).
         ! nwsteps  - the solute routine is called every nwsteps of the RE solution.
         ! Other entities:
         ! solve    - sub to call to solve eq. of con.
         ! n        - no. of soil layers.
         ! dx(:)    - layer thicknesses (top to bottom).
         ! ths(:)   - saturated water content.
         ! he(:)    - air entry matric head.
         ! debug    - flag for debugging, or for conditionally printing info.
         ! nless    - no. of step size reductions.         
         */

        public static double dSfac = 1.25, h0min = -0.02, Smax = 1.001, dh0max = 0.0;
        public static string botbc = "free drainage"; // bottom boundary conditions
        public static double h0max = 1.0e10, qprecmax = 1.0e10, hbot = 0.0, Sbot = 1.0; // boundary parameters
        public static double dSmax = 0.05, dSmaxr = 0.5, dtmax = 1.0e10, dtmin = 0.0, dsmmax = 1.0; // solution parameters
        public static int nwsteps = 10;
        public static bool debug = false;
        public static int nless;
        public static SoilData sd = new SoilData();
        public static ISink sink;

        /// <summary>
        /// Solves the equation of continuity from time ts to tfin.
        /// </summary>
        /// <param name="ts">start time (h).</param>
        /// <param name="tfin">finish time.</param>
        /// <param name="qprec">precipitation (or water input) rate (fluxes are in cm/h).</param>
        /// <param name="qevap">potl evaporation rate from soil surface.</param>
        /// <param name="nsol">no. of solutes.</param>
        /// <param name="nex">no. of water extraction streams.</param>
        /// <param name="h0">surface head, equal to depth of surface pond.</param>
        /// <param name="S">degree of saturation ("effective satn") of layers.</param>
        /// <param name="evap">cumulative evaporation from soil surface (cm, not initialised).</param>
        /// <param name="runoff">cumulative runoff.</param>
        /// <param name="infil">cumulative net infiltration (time integral of flux across surface).</param>
        /// <param name="drn">cumulative net drainage (time integral of flux across bottom).</param>
        /// <param name="nsteps">cumulative no. of time steps for RE soln.</param>
        /// <param name="jt">layer soil type numbers for solute.</param>
        /// <param name="cin">solute concns in water input (user's units/cc).</param>
        /// <param name="c0">solute concns in surface pond.</param>
        /// <param name="sm">solute (mass) concns in layers.</param>
        /// <param name="soff">cumulative solute runoff (user's units).</param>
        /// <param name="sinfil">cumulative solute infiltration.</param>
        /// <param name="sdrn">cumulative solute drainage.</param>
        /// <param name="nssteps">cumulative no. of time steps for ADE soln.</param>
        /// <param name="wex">cumulative water extractions from layers.</param>
        /// <param name="sex">cumulative solute extractions from layers.</param>
        public static void Solve(SolProps sol, double ts, double tfin, double qprec, double qevap, int nsol, int nex,
                          ref double h0, ref double[] S, ref double evap, ref double runoff, ref double infil, ref double drn, ref int nsteps, int[] jt, double[] cin,
                          ref double[] c0, ref double[,] sm, ref double[] soff, ref double[] sinfil, ref double[] sdrn, ref int[] nssteps, ref double[,] wex, ref double[,,] sex)
        {
            bool again, extraction, initpond, maxpond;
            int i, iflux, ih0, iok, isatbot, itmp, ns, nsat, nsatlast, nsteps0;
            int[] isat = new int[S.Length];
            double accel, dmax, dt, dwinfil, dwoff, fac, infili, qpme, qprec1, rsig, rsigdt, sig, t, ti, win, xtblbot;
            double[] dSdt = new double[S.Length + 1];
            double[] h = new double[S.Length + 1];
            double[] xtbl = new double[S.Length + 1];
            double[] thi = new double[S.Length + 1];
            double[] thf = new double[S.Length + 1];
            double[] qwex = new double[S.Length + 1];
            double[] qwexd = new double[S.Length + 1];
            double[,] dwexs = new double[S.Length + 1, nex + 1];
            double[,] qwexs = new double[S.Length + 1, nex + 1];
            double[,] qwexsd = new double[S.Length + 1, nex + 1];
            double[] aa = new double[S.Length]; // these are 0 based
            double[] bb = new double[S.Length];
            double[] cc = new double[S.Length];
            double[] dd = new double[S.Length];
            double[] dy = new double[S.Length];
            double[] ee = new double[S.Length];
            double[] q = new double[S.Length];
            double[] qya = new double[S.Length];
            double[] qyb = new double[S.Length];
            double[] cav = new double[nsol + 1];
            double[] sinfili = new double[nsol + 1];
            double[,] c = new double[sd.n + 1, nsol + 1];
            ISink sink = new SinkDripperDrain(); // Sink type can be changed here.
            /*
             ! The saturation status of a layer is stored as 0 or 1 in isat since S may be
             ! >1 (because of previous overshoot) when a layer desaturates. Fluxes at the
             ! beginning of a time step and their partial derivs wrt S or h of upper and
             ! lower layers or boundaries are stored in q, qya and qyb.
            */
            isatbot = 0;
            xtblbot = 0;
            dt = 0.0;
            fac = 0.0;
            initpond = false;
            extraction = false;
            dwoff = 0.0;
            ti = 0.0;
            infili = 0.0;

            if (nex > 0)
            {
                extraction = true;
                /*
                  qwexs=0.0 ! so unused elements won't need to be set
                  qwexsd=0.0
                  C# arrays are automatically zeroed so we don't need these lines.
                */
            }

            if (S.Length != sd.n)
            {
                Console.WriteLine("solve: Size of S differs from table data.");
                Environment.Exit(1);
            }

            //-----set up for boundary conditions
            if (botbc == "constant head") // !h at bottom bdry specified
                if (hbot < sd.he[sd.n])
                {
                    isatbot = 0;
                    xtblbot = Sbot;
                }
                else
                {
                    isatbot = 1;
                    xtblbot = hbot - sd.he[sd.n];
                }
            //-----end set up for boundary conditions
            //-----initialise
            t = ts;
            nsteps0 = nsteps;
            nsat = 0;
            //initialise saturated regions
            for (int x = 1; x < S.Length; x++)
                if (S[x] >= 1.0)
                {
                    isat[x] = 1;
                    h[x] = sd.he[x];
                }
                else
                {
                    isat[x] = 0;
                    h[x] = sd.he[x] - 1.0;
                }

            if (nsol > 0)
            {
                //set solute info
                thi = MathUtilities.Multiply(sd.ths, S); //initial th
                for (int x = 0; x < dwexs.GetLength(0); x++)
                    for (int y = 0; y < dwexs.GetLength(1); y++)
                        dwexs[x, y] = 0; //initial water extracted from layers

                ti = t;
                infili = infil;
                sinfili = sinfil;
                double c0temp = c0[1];
                if (h0 > 0 && (cin.Select(x => x != c0temp).Count() > 0)) // count(c0 /= cin) > 0)
                    initpond = true; //initial pond with different solute concn
                else
                    initpond = false;
                for (int x = 0; x > c.GetLength(0); x++)
                    for (int y = 0; y < c.GetLength(1); y++)
                        c[x, y] = 0; //temp storage for soln concns
            }
            //-----end initialise
            //-----solve until tfin

            while (t < tfin)
            {
                //-----take next time step
                for (iflux = 1; iflux <= 2; iflux++) //sometimes need twice to adjust h at satn
                {
                    nsatlast = nsat; // for detecting onset of profile saturation
                    nsat = (int)MathUtilities.Sum(isat); //no.of sat layers
                    sig = 0.5;
                    if (nsat != 0)
                        sig = 1.0; //time weighting sigma
                    rsig = 1.0 / sig;
                    //-----get fluxes and derivs
                    //     get table entries
                    for (int x = 1; x < isat.Length; x++)
                        if (isat[x] == 0)
                            xtbl[x] = S[x];
                        else
                            xtbl[x] = h[x] - sd.he[x];

                    //get surface flux
                    qpme = qprec - qevap; //input rate at saturation
                    qprec1 = qprec; //may change qprec1 to maintain pond if required
                    if (h[1] <= 0 && h0 <= 0 && nsat < sd.n) //no ponding
                    {
                        SoilData sd = new SoilData();
                        ns = 1; //start index for eqns
                        sd.GetQ(0, new int[] { 0, isat[1] }, new double[] { 0, xtbl[1] }, out q[0], out qya[0], out qyb[0]);
                        if (q[0] < qpme)
                        {
                            q[0] = qpme;
                            qyb[0] = 0;
                        }
                        maxpond = false;
                    }
                    else //ponding
                    {
                        ns = 0;
                        sd.GetQ(0, new int[] { 1, isat[1] }, new double[] { h0 - sd.he[1], xtbl[1] }, out q[0], out qya[0], out qyb[0]);
                        if (h0 >= h0max && qpme > q[0])
                        {
                            maxpond = true;
                            ns = 1;
                        }
                        else
                            maxpond = false;
                    }

                    //get profile fluxes
                    for (i = 1; i <= sd.n - 1; i++)
                        sd.GetQ(i, new int[] { isat[i], isat[i + 1] }, new double[] { xtbl[i], xtbl[i + 1] }, out q[i], out qya[i], out qyb[i]);

                    //get bottom flux
                    switch (botbc)
                    {
                        case "constant head":
                            sd.GetQ(sd.n, new int[] { isat[sd.n], isatbot }, new double[] { xtbl[sd.n], xtblbot }, out q[sd.n], out qya[sd.n], out qyb[sd.n]);
                            break;
                        case "0.0 flux":
                            q[sd.n] = 0;
                            qya[sd.n] = 0;
                            break;
                        case "free drainage":
                            sd.GetK(sd.n, isat[sd.n], xtbl[sd.n], out q[sd.n], out qya[sd.n]);
                            break;
                        case "seepage":
                            if (h[sd.n] <= -0.5 * sd.dx[sd.n])
                            {
                                q[sd.n] = 0;
                                qya[sd.n] = 0;
                            }
                            else
                            {
                                sd.GetQ(sd.n, new int[] { isat[sd.n], 1 }, new double[] { xtbl[sd.n], -sd.he[sd.n] }, out q[sd.n], out qya[sd.n], out qyb[sd.n]);
                            }
                            break;
                        default:
                            Console.Out.WriteLine("solve: illegal bottom boundary condn");
                            Environment.Exit(1);
                            break;
                    }

                    if (extraction) //get rate of extraction
                    {
                        sink.Wsinks(t, isat, xtbl, out qwexs, out qwexsd);

                        for (int x = 1; x < qwex.Length; x += 2)
                        {
                            //  qwex = sum(qwexs, 2)  //will need to see what the fortran throws out
                            //qwexd = sum(qwexsd, 2)
                        }
                    }
                    again = false; //flag for recalcn of fluxes
                                   //-----end get fluxes and derivs
                                   //----estimate time step dt
                    dmax = 0;
                    dSdt = MathUtilities.CreateArrayOfValues(0, dSdt.Length);
                    if (extraction)
                    {
                        for (int x = 0; x < isat.Length; x++)
                        {
                            if (isat[x] == 0)
                            {
                                for (int y = 1; y <= sd.n; y++)
                                    dSdt[x] = Math.Abs(q[x] - q[x - 1] + qwex[x]) / (sd.ths[x] * sd.dx[x]);
                            }

                        }
                    }
                    else
                    {
                        for (int x = 0; x < isat.Length; x++)
                        {
                            if (isat[x] == 0)
                            {
                                for (int y = 1; y <= sd.n; y++)
                                    dSdt[x] = Math.Abs(q[x] - q[x - 1]) / (sd.ths[x] * sd.dx[x]);
                            }
                        }
                    }
                    dmax = MathUtilities.Max(dSdt); //Max derivative | dS / dt |
                    if (dmax > 0)
                    {
                        dt = dSmax / dmax;
                        // if pond going adjust dt
                        if (h0 > 0 && (q[0] - qpme) * dt > h0)
                            dt = (h0 - 0.5 * h0min) / (q[0] - qpme);
                        else //steady state flow
             if (qpme >= q[sd.n])
                        {
                            //step to finish -but what if extraction varies with time ???
                            dt = tfin - t;
                        }
                        else
                            dt = -(h0 - 0.5 * h0min) / (qpme - q[sd.n]); //pond going so adjust dt
}
                    if (dt > dtmax)
                        dt = dtmax; //user's limit
           // if initial step, improve h where S>= 1
           if (nsteps == nsteps0 && nsat > 0 && iflux == 1)
                    {
                        again = true;
                        dt = 1.0e-20 * (tfin - ts);
                    }
                    if (nsat == sd.n && nsatlast < sd.n && iflux == 1)
                    {
                        //profile has just become saturated so adjust h values
                        again = true;
                        dt = 1.0e-20 * (tfin - ts);
                    }
                    if (t + 1.1 * dt > tfin) //step to finish
                    {
                        dt = tfin - t;
                        t = tfin;
                    }
                    else
                        t = t + dt; //tentative update

                    //-----end estimate time step dt
                    //-----get and solve eqns
                    rsigdt = 1.0 / (sig * dt);
                    //aa, bb, cc and dd hold coeffs and rhs of tridiag eqn set
                    for (int x = ns; x <= sd.n - 1; x++)
                    {
                        aa[x + 1] = qya[x];
                        cc[x] = -qyb[x];
                    }
                    if (extraction)
                    {
                        for (int x = 1; x <= sd.n; x++)
                            dd[x] = -(q[x - 1] - q[x] - qwex[x]) * rsig;
                    }
                    else
                    {
                        for (int x = 1; x <= sd.n; x++)
                            dd[x] = -(q[x - 1] - q[x]) * rsig;
                    }

                    iok = 0; //flag for time step test
                    itmp = 0; //counter to abort if not getting solution
                    while (iok == 0) //keep reducing time step until all ok
                    {
                        itmp = itmp + 1;
                        accel = 1.0 - 0.05 * Math.Min(10, Math.Max(0, itmp - 4)); //acceleration
                        if (itmp > 20)
                        {
                            Console.Out.WriteLine("solve: too many iterations of equation solution");
                            Environment.Exit(1);
                        }
                        if (ns < 1)
                        {
                            bb[0] = -qya[0] - rsigdt;
                            dd[0] = -(qpme - q[0]) * rsig;
                        }
                        if (extraction)
                        {
                            for (int x = 0; x < isat.Length; x++)
                            {
                                if (isat[x] == 0)
                                {
                                    for (int y = 1; y <= sd.n; y++)
                                        bb[y] = qyb[y - 1] - qya[y] - qwexd[y] - sd.ths[x] * sd.dx[x] * rsigdt;
                                }
                            }
                        }
                        else
                        {
                            for (int x = 0; x < isat.Length; x++)
                            {
                                if (isat[x] == 0)
                                {
                                    for (int y = 1; y <= sd.n; y++)
                                        bb[y] = qyb[y - 1] - qya[y] - sd.ths[x] * sd.dx[x] * rsigdt;
                                }
                                else
                                {
                                    for (int y = 1; y <= sd.n; y++)
                                        bb[y] = qyb[y - 1] - qya[y];
                                }
                            }
                        }

                        Tri(ns, sd.n, aa, ref bb, cc, dd, ref ee, ref dy);
                        //dy contains dS or, for sat layers, h values
                        iok = 1;
                        if (!again)
                        {
                            //check if time step ok, if not then set fac to make it less
                            iok = 1;
                            for (i = 1; i <= sd.n; i++)
                                if (isat[i] == 0) //check change in S
                                {
                                    if (Math.Abs(dy[i]) > dSfac * dSmax)
                                    {
                                        fac = Math.Max(0.5, accel * Math.Abs(dSmax / dy[i]));
                                        iok = 0;
                                        break;
                                    }
                                    if (-dy[i] > dSmaxr * S[i])
                                    {
                                        fac = Math.Max(0.5, accel * dSmaxr * S[i] / (-dSfac * dy[i]));
                                        iok = 0;
                                        break;
                                    }
                                    if (S[i] < 1.0 && S[i] + dy[i] > Smax)
                                    {
                                        fac = accel * (0.5 * (1.0 + Smax) - S[i]) / dy[i];
                                        iok = 0;
                                        break;
                                    }
                                    if (S[i] >= 1.0 && dy[i] > 0.5 * (Smax - 1.0))
                                    {
                                        fac = 0.25 * (Smax - 1.0) / dy[i]; iok = 0;
                                        break;
                                    }
                                }
                        }
                        if (iok == 1 && ns < 1 && h0 < h0max && h0 + dy[0] > h0max + dh0max)
                        {
                            //start of runoff
                            fac = (h0max + 0.5 * dh0max - h0) / dy[0];
                            iok = 0;
                        }
                        if (iok == 1 && ns < 1 && h0 > 0.0 && h0 + dy[0] < h0min)
                        {
                            //pond going
                            fac = -(h0 - 0.5 * h0min) / dy[0];
                            iok = 0;
                        }
                        if (iok == 0) //reduce time step
                        {
                            t = t - dt;
                            dt = fac * dt;
                            t = t + dt;
                            rsigdt = 1.0 / (sig * dt);
                            nless = nless + 1; //count step size reductions
                        }
                        if (isat[1] != 0 && iflux == 1 && h[1] < 0.0 && h[1] + dy[1] > 0.0)
                        {
                            //incipient ponding - adjust state of saturated regions
                            t = t - dt;
                            dt = 1.0e-20 * (tfin - ts);
                            rsigdt = 1.0 / (sig * dt);
                            again = true;
                            iok = 0;
                        }
                    }

                    //-----end get and solve eqns
                    //-----update unknowns
                    ih0 = 0;
                    if (!again)
                        dwoff = 0.0;
                    if (ns < 1)
                    {
                        h0 = h0 + dy[0];
                        if (h0 < 0.0 && dy[0] < 0.0)
                            ih0 = 1; //pond g1.0

                        evap = evap + qevap * dt;
                        //note that fluxes required are q at sigma of time step
                        dwinfil = (q[0] + sig * (qya[0] * dy[0] + qyb[0] * dy[1])) * dt;
                    }
                    else
                    {
                        dwinfil = (q[0] + sig * qyb[0] * dy[1]) * dt;
                        if (maxpond)
                        {
                            evap = evap + qevap * dt;
                            if (qprec > qprecmax) // set input to maintain pond
                            {
                                qpme = q[0] + sig * qyb[0] * dy[1];
                                qprec1 = qpme + qevap;
                                dwoff = 0.0;
                            }
                            else
                                dwoff = qpme * dt - dwinfil;

                            runoff = runoff + dwoff;
                        }
                        else
                            evap = evap + qprec1 * dt - dwinfil;
                    }
                    infil = infil + dwinfil;
                    if (nsol > 0) //get surface solute balance
                    {
                        if (initpond) //pond concn != cin
                        {
                            if (h0 > 0.0)
                            {
                                if (ns == 1) // if max pond depth
                                {
                                    dy[0] = 0.0;
                                }
                                for (int x = 1; x < cin.Length; x++)
                                {
                                    cav[x] = ((2.0 * h0 - dy[0]) * c0 + qprec1 * dt * cin[x]) / (2.0 * h0 + dwoff + dwinfil);
                                }
                                c0 = 2.0 * cav[1] - c0; //This needs to be tested from FORTRAN; no example in original code.
                            }
                            else
                            {
                                for (int x = 1; x < cin.Length; x++)
                                {
                                    cav[x] = ((h0 - dy[0]) * c0 + qprec1 * dt * cin[x]) / (dwoff + dwinfil);
                                }
                                initpond = false; //pond gone
                                c0 = cin[1]; // for output if any pond at end
                            }
                            soff = MathUtilities.Add(soff, MathUtilities.Multiply_Value(cav, dwoff));
                            sinfil = MathUtilities.Add(sinfil, MathUtilities.Multiply_Value(cav, dwinfil));
                        }
                        else
                        {
                            soff = MathUtilities.Add(soff, MathUtilities.Multiply_Value(cav, dwoff));
                            sinfil = MathUtilities.Add(sinfil, MathUtilities.Multiply_Value(cin, qprec1 * dt - dwoff));
                        }
                    }

                        if (botbc == "constant head")
                        {
                            drn = drn + (q[sd.n] + sig * qya[sd.n] * dy[sd.n]) * dt;
                        }
                        else
                        {
                            drn = drn + (q[sd.n] + sig * qya[sd.n] * dy[sd.n]) * dt;
                        }

                    if (extraction)
                    {

                        Matrix<double> dwexsM = Matrix<double>.Build.DenseOfArray(dwexs);
                        Matrix<double> qwexsM = Matrix<double>.Build.DenseOfArray(qwexs);
                        Matrix<double> qwexsdM = Matrix<double>.Build.DenseOfArray(qwexsd);
                        Matrix<double> wexM = Matrix<double>.Build.DenseOfArray(wex);
                        Vector<double> wexV;
                        Vector<double> dwexsV;
                        Vector<double> qwexsV;
                        Vector<double> qwexsdV;
                        if (nsol > 0)
                        {
                            //dwexs = dwexs + (qwexs + sig * qwexsd * spread(dy(1:n), 2, nex)) * dt
                            for (i = 1; i <= nex; i++)
                            {
                                dwexsV = dwexsM.Column(i);
                                qwexsV = qwexsM.Column(i);
                                qwexsdV = qwexsdM.Column(i);
                                dwexsV = dwexsV + (qwexsV + sig * qwexsdV * Vector<double>.Build.DenseOfArray(dy.Slice(1,sd.n))) * dt;
                                dwexsM.Column(i, dwexsV);
                            }
                            dwexs = dwexsM.ToArray();
                        }

                        //wex = wex + (qwexs + sig * qwexsd * spread(dy(1:n), 2, nex)) * dt
                        for (i = 1; i <= nex; i++)
                        {
                            qwexsV = qwexsM.Column(i);
                            qwexsdV = qwexsdM.Column(i);
                            wexV = wexM.Column(i);
                            wexV = wexV + (qwexsV + sig * qwexsdV * Vector<double>.Build.DenseOfArray(dy.Slice(1, sd.n))) * dt;
                        }
                        wex = wexM.ToArray();
                    }
                    
                    for (i = 1; i <= sd.n; i++)
                    {
                        if (isat[i] == 0)
                        {
                            if (!again)
                            {
                                S[i] = S[i] + dy[i];
                                if (S[i] > 1.0 && dy[i] > 0.0) //saturation of layer
                                {
                                    isat[i] = 1;
                                    h[i] = sd.he[i];
                                }
                            }
                        }
                        else
                        {
                            h[i] = h[i] + dy[i];
                            if (i == 1 && ih0 != 0 && h[i] >= sd.he[i]) h[i] = sd.he[i] - 1.0; //pond g1.0
                            if (h[i] < sd.he[i])  //desaturation of layer
                            {
                                isat[i] = 0;
                                h[i] = sd.he[i];
                            }
                        }
                    } //-----end update unknowns
                    if (!again)
                        break;
                }
                if (dt <= dtmin)
                {
                    Console.WriteLine("solve: time step = " + dt);
                    Environment.Exit(1);
                }
                //-----end take next time step
                //remove negative h0 (optional)
                if (h0 < 0.0 && isat[1] == 0)
                {
                    infil = infil + h0;
                    S[1] = S[1] + h0 / (sd.ths[1] * sd.dx[1]);
                    h0 = 0.0;
                }
                nsteps = nsteps + 1;
                //solve for solute transport if required
                if (nwsteps * (nsteps / nwsteps) == nsteps)
                {
                    if (nsol > 0 && t > ti)
                    {
                        thf = MathUtilities.Multiply(sd.ths, S); //final th before call
                        win = infil - infili; //water in at top over time interval
                        cav = MathUtilities.Divide_Value(MathUtilities.Subtract(sinfil, sinfili), win); //average concn in win
                        Solute(ti, t, thi, thf, dwexs, win, cav, sd.n, nsol, nex, sd.dx, jt, dsmmax, ref sm, ref sdrn, ref nssteps, ref c, ref sex, extraction, sol);
                        ti = t;
                        thi = thf;
                        dwexs.Populate2D(0);
                        infili = infil;
                        sinfili = sinfil; // for next interval
                    }
                }
            }
            //-----end solve until tfin
            //finalise solute transport if required
            if (nsol > 0 && t > ti)
            {
                thf = MathUtilities.Multiply(sd.ths, S); //final th before call
                win = infil - infili; //water in at top over time interval
                cav = MathUtilities.Divide_Value(MathUtilities.Subtract(sinfil, sinfili), win); //average concn in win
                Solute(ti, t, thi, thf, dwexs, win, cav, sd.n, nsol, nex, sd.dx, jt, dsmmax, ref sm, ref sdrn, ref nssteps, ref c, ref sex, extraction, sol);
                ti = t;
                thi = thf;
                dwexs.Populate2D(0);
                infili = infil;
                sinfili = sinfil; // for next interval
            }
        }

        /// <summary>
        /// Solves the ADE from time ti to tf. Diffusion of solute ignored - dispersion
        /// coeff = dispersivity* abs(pore water velocity).
        /// </summary>
        /// <param name="ti">start time (h).</param>
        /// <param name="tf">finish time.</param>
        /// <param name="thi">initial layer water contents.</param>
        /// <param name="thf">initial layer water contents.</param>
        /// <param name="dwexs">water extracted from layers over period ti to tf.</param>
        /// <param name="win">water in at top of profile.</param>
        /// <param name="cav">solute concn in win.</param>
        /// <param name="n">no. of soil layers.</param>
        /// <param name="nsol">no. of solutes.</param>
        /// <param name="nex">no. of water extraction streams.</param>
        /// <param name="dx">layer thicknesses.</param>
        /// <param name="jt">layer soil type numbers for solute.</param>
        /// <param name="dsmmax">max change in sm of any layer to aim for each time step; controls time step size.</param>
        /// <param name="sm">layer masses of solute per cc.</param>
        /// <param name="sdrn">cumulative solute drainage.</param>
        /// <param name="nssteps">cumulative no. of time steps for ADE soln.</param>
        /// <param name="c"></param>
        /// <param name="sex">cumulative solute extractions in water extraction streams.</param>
        private static void Solute(double ti, double tf, double[] thi, double[] thf, double[,] dwexs, double win, double[] cin, int n, int ns, int nex, double[] dx, int[] jt, double dsmmax, ref double[,] sm, ref double[] sdrn, ref int[] nssteps, ref double[,] c, ref double[,,] sex, bool extraction, SolProps solProps)
        {
            int itmax = 20; //max iterations for finding c from sm
            double eps = 0.00001; // for stopping
            int i, it, j, k;
            double dc, dm, dmax, dt=0, f=0, fc=0, r, rsig, rsigdt, sig, sigdt, t, tfin, th, v1, v2;
            double[] dz = new double[n - 1 + 1];
            double[] coef1 = new double[n - 1 + 1];
            double[] coef2 = new double[n - 1 + 1];
            double[] csm = new double[n + 1];
            double[] tht = new double[n + 1];
            double[] dwex = new double[n + 1];
            double[] qsex = new double[n + 1];
            double[] qsexd = new double[n + 1];
            double[,] qsexs = new double[n + 1, nex + 1];
            double[,] qsexsd = new double[n + 1, nex + 1];
            double[] aa = new double[n]; // these are 0 based
            double[] bb = new double[n];
            double[] cc = new double[n];
            double[] dd = new double[n];
            double[] dy = new double[n];
            double[] ee = new double[n];
            double[] q = new double[n];
            double[] qw = new double[n]; 
            double[] qya = new double[n];
            double[] qyb = new double[n];

            sig = 0.5;
            rsig = 1.0 / sig;
            tfin = tf;
            for(int x=1;x<= n;x++)
                dz[x] = 0.5 * (dx[x - 1] + dx[x + 1]); //TEST
            //get average water fluxes
            //dwex = sum(dwexs, 2) !total changes in sink water extraction since last call
            Matrix<double> dwexsM = Matrix<double>.Build.DenseOfArray(dwexs);
            for(int x=0;x<dwexs.GetLength(1);x++)
            {
                dwex[x] = MathUtilities.Sum(dwexsM.Column(x));
            }

            r = 1.0 / (tf - ti);
            qw[0] = r * win;

            for (int x = 0; x < thf.Length; x++)
                tht[x] = r * (thf[x] - thi[x]);
            for (i = 1; i <= n; i++)
                qw[i] = qw[i - 1] - dx[i] * tht[i] - r * dwex[i];

            //get constant coefficients
            for (i = 1; i <= n - 1; i++)
            {
                v1 = 0.5 * qw[i];
                v2 = 0.5 * (solProps.dis[jt[i]] + solProps.dis[jt[i + 1]]) * Math.Abs(qw[i]) / dz[i];
                coef1[i] = v1 + v2; coef2[i] = v1 - v2;
            }

            for (j = 1; j <= ns; j++)
            {
                t = ti;
                if (qw[0] > 0.0)
                    q[0] = qw[0] * cin[j];
                else
                    q[0] = 0.0;

                qyb[0] = 0.0;

                while (t < tfin)
                //get fluxes
                {
                    for (i = 1; i <= n; i++)
                    {
                        //get c and csm = dc / dsm(with theta constant)
                        k = jt[i];
                        th = thi[i] + (t - ti) * tht[i];
                        if (solProps.isotype[k, j] == "no" || sm[i, j] < 0.0) //handle sm < 0 here
                        {
                            csm[i] = 1.0 / th;
                            c[i, j] = csm[i] * sm[i, j];
                        }
                        else if (solProps.isotype[k, j] == "li")
                        {
                            csm[i] = 1.0 / (th + solProps.bd[k] * solProps.isopar[k, j].ElementAt(1));
                            c[i, j] = csm[i] * sm[i, j];
                        }
                        else
                        {
                            for (it = 1; it <= itmax; it++) //get c from sm using Newton's method and bisection
                            {
                                if (c[i, j] < 0.0)
                                    c[i, j] = 0.0; //c and sm are >= 0
                                solProps.Isosub(solProps.isotype[k, j], c[i, j], dsmmax, ref solProps.isopar[k, j], out f, out fc);
                                csm[i] = 1.0 / (th + solProps.bd[k] * fc);
                                dm = sm[i, j] - (solProps.bd[k] * f + th * c[i, j]);
                                dc = dm * csm[i];
                                if (sm[i, j] >= 0.0 && c[i, j] + dc < 0.0)
                                    c[i, j] = 0.5 * c[i, j];
                                else
                                    c[i, j] = c[i, j] + dc;
                                if (Math.Abs(dm) < eps * (sm[i, j] + 10.0 * dsmmax))
                                    break;
                                if (it == itmax)
                                {
                                    Console.WriteLine("solute: too many iterations getting c");
                                    Environment.Exit(1);
                                }
                            }
                        }
                    }
                }
                for (int x = 1; x <= n - 1; x++)
                {
                    q[x] = coef1[x] * c[x, j] + coef2[x] * c[x+1, j];
                    qya[x] = coef1[x] * csm[x];
                    qyb[x] = coef2[x] * csm[x+1];
                }
                q[n] = qw[n] * c[n, j];
                qya[n] = qw[n] * csm[n];
                //get time step
                double[] absQ = new double[n];
                for (int x = 0; x < n; x++)
                    absQ[x] = Math.Abs(q[n + 1] - q[n] / dx[x]);

                dmax = MathUtilities.Max(absQ);
                if (dmax == 0.0)
                {
                    dt = tfin - t;
                }
                else if (dmax < 0.0)
                {
                    Console.WriteLine("solute: errors in fluxes prevent continuation");
                    Environment.Exit(1);
                }
                else
                    dt = dsmmax / dmax;

                if (t + 1.1 * dt > tfin)
                {
                    dt = tfin - t;
                    t = tfin;
                }
                else
                    t = t + dt;

                sigdt = sig * dt; rsigdt = 1.0 / sigdt;
                //adjust q for change in theta
                for (int x = 1; x <= n - 1; x++)
                    q[x] = q[x] - sigdt * (qya[x] * tht[x] * c[x, j] + qyb[x] * tht[x + 1] * c[x, j]);
                q[n] = q[n] - sigdt * qya[n] * tht[n] * c[n, j];
                //get and solve eqns
                for (int x = 1; x <= n - 1; x++)
                {
                    aa[x + 1] = qya[x];
                    cc[x] = -qyb[x];
                }
                Matrix<double> qsexsM = Matrix<double>.Build.DenseOfArray(qsexs);
                Matrix<double> qsexsdM = Matrix<double>.Build.DenseOfArray(qsexsd);
                if (extraction)  //get extraction
                {
                    double[] ctemp = new double[n-1];
                    for (int x = 1; x <= n; x++)
                        ctemp[x] = c[x, j];

                    qsexs = qsexsM.ToArray();
                    qsexsd = qsexsdM.ToArray();
                    sink.Ssinks(t, ti, tf, j, dwexs, ctemp, out qsexs, out qsexsd);
                    qsex = qsexsM.ColumnSums().ToArray();
                    qsexd = qsexsdM.ColumnSums().ToArray();
                    for (int x = 1; x <= n; x++)
                    {
                        bb[x] = qyb[x-1] - qya[x] - qsexd[x] * csm[x] - sd.dx[x] * rsigdt;
                        dd[x] = -(q[x-1] - q[x] - qsex[x]) * rsig;
                    }
                }
                else
                {
                    for (int x = 1; x <= n; x++)
                    {
                        bb[x] = qyb[x-1] - qya[x] - sd.dx[x] * rsigdt;
                        dd[x] = -(q[x-1] - q[x]) * rsig;
                    }
                }

                Tri(1, n, aa, ref bb, cc, dd, ref ee, ref dy);
                //update unknowns
                Matrix<double> smM = Matrix<double>.Build.DenseOfArray(sm);
                qsexsM = Matrix<double>.Build.DenseOfArray(qsexs);
                qsexsdM = Matrix<double>.Build.DenseOfArray(qsexsd);
                sdrn[j] = sdrn[j] + (q[n] + sig * qya[n] * dy[n]) * dt;
                smM.SetRow(j,  smM.Row(j) + Vector<double>.Build.DenseOfArray(dy.Slice(1,n)));
                sm = smM.ToArray();
                if (extraction)
                {
                    Matrix<double> sexM = Matrix<double>.Build.Dense(sex.GetLength(0), sex.GetLength(1));
                    for (int x = 0; x < sex.GetLength(0); x++)
                        for (int y = 0; y < sex.GetLength(1); y++)
                            sexM[x, y] = sex[x, y, j];

                    Vector<double> dysub = Vector<double>.Build.DenseOfArray(dy.Slice(1, n));
                    for (i = 1; i <= nex; i++)
                    {
                       sexM.SetColumn(i, sexM.Column(i) + (qsexsM.Column(i) + sig * qsexsdM.Column(i) * Vector<double>.Build.DenseOfArray(csm) * dysub) * dt);
                    }

                    for (int x = 0; x < sex.GetLength(0); x++)
                        for (int y = 0; y < sex.GetLength(1); y++)
                            sex[x, y, j] = sexM[x, y];
                }
                nssteps[j] = nssteps[j] + 1;
            }
        }

        /// <summary>
        /// Solves tridiag set of linear eqns. Coeff arrays aa and cc left intact.
        /// </summary>
        /// <param name="ns">start index for eqns.</param>
        /// <param name="n">end index.</param>
        /// <param name="aa">coeffs below diagonal; ns+1:n used.</param>
        /// <param name="bb">coeffs on diagonal; ns:n used.</param>
        /// <param name="cc">coeffs above diagonal; ns:n-1 used.</param>
        /// <param name="dd">rhs coeffs; ns:n used.</param>
        /// <param name="ee">work space.</param>
        /// <param name="dy">solution in ns:n.</param>
        private static void Tri(int ns, int n, double[] aa, ref double[] bb, double[] cc, double[] dd, ref double[] ee, ref double[] dy)
        {
            int i;
            dy[ns] = dd[ns]; //decomposition and forward substitution
            for (i = ns; i <= n - 1; i++)
            {
                ee[i] = cc[i] / bb[i];
                dy[i] = dy[i] / bb[i];
                bb[i + 1] = bb[i + 1] - aa[i + 1] * ee[i];
                dy[i + 1] = dd[i + 1] - aa[i + 1] * dy[i];
            }
            dy[n] = dy[n] / bb[n]; //back substitution
            for (i = n - 1; i >= ns; i--)
                dy[i] = dy[i] - ee[i] * dy[i + 1];

        }
    }
}