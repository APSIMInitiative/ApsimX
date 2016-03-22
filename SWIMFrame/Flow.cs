using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using APSIM.Shared.Utilities;

namespace SWIMFrame
{
    class Flow
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
         !            "seepage", or "zero flux". Constant head means that matric head h
         !            is specified. Free drainage means zero gradient of matric head,
         !            i.e. unit hydraulic gradient. Seepage means zero flux when the
         !            matric head is below zero and an upper limit of zero for the head.
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

        double dSfac = 1.25, h0min = -0.02, Smax = 1.001, dh0max = 0.0;
        string botbc = "free drainage"; // bottom boundary conditions
        double h0max = 1.0e10, qprecmax = 1.0e10, hbot = 0.0, Sbot = 1.0; // boundary parameters
        double dSmax = 0.05, dSmaxr = 0.5, dtmax = 1.0e10, dtmin = 0.0, dsmmax = 1.0; // solution parameters
        int nwsteps = 10;
        bool debug = false;
        int nless;
        SoilData sd = new SoilData();

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
        public void Solve(double ts, double tfin, double qprec, double qevap, int nsol, int nex,
                          ref double h0, ref double[] S, ref double evap, ref double runoff, ref double infil, ref double drn, ref int[] nsteps, int[] jt, double[] cin,
                          ref double c0, ref double sm, ref double soff, ref double sinfil, ref double sdrn, ref int nssteps, ref double wex, ref double sex)
        {
            bool again, extraction, initpond, maxpond;
            int i, iflux, ih0, iok, isatbot, itmp, j, ns, nsat, nsatlast, nsteps0;
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
            extraction = false;
            if (nex > 0)
            {
                extraction = true;
                /*
                  qwexs=0.0 ! so unused elements won't need to be set
                  qwexsd=0.0
                  C# arrays are automatically zeroed so we don't need these lines.
                */
            }

            if(S.Length != n)
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
                sinfili = MathUtilities.CreateArrayOfValues(sinfil, sinfili.Length);
                double c0temp = c0;
                if (h0 > 0 && (cin.Select(x => x != c0temp).Count() > 0)) // count(c0 /= cin) > 0)
                    initpond = true; //initial pond with different solute concn
                else
                    initpond = false;
                for(int x=0;x>c.GetLength(0);x++)
                    for (int y=0;y<c.GetLength(1);y++)
                c[x,y] = 0; //temp storage for soln concns
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
                            sd.GetQ(sd.n, new int[] { isat[sd.n], isatbot }, new double[] { (xtbl[sd.n], xtblbot }, out q[sd.n], out qya[sd.n], out qyb[sd.n]);
                            break;
                        case "zero flux":
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
                                sd.GetQ(sd.n, new int[] { isat[n], 1 }, new double[] { xtbl[sd.n], -sd.he[sd.n] }, out q[sd.n], out qya[sd.n], out qyb[sd.n]);
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
                            dt = (h0 - half * h0min) / (q[0] - qpme);
                        else //steady state flow
             if (qpme >= q[sd.n])
                        {
                            //step to finish -but what if extraction varies with time ???
                            dt = tfin - t;
                        }
                        else
                            dt = -(h0 - half * h0min) / (qpme - q[n]) //pond going so adjust dt
}
                    if (dt > dtmax)
                        dt = dtmax //user's limit
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

                        Tri(ns, sd.n, aa, bb, cc, dd, ee, dy);
                        //dy contains dS or, for sat layers, h values
                        iok = 1;
             if (!again)
                        {
                            //check if time step ok, if not then set fac to make it less
                            iok = 1;
               for( i = 1;i<=sd.n;i++)
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
                       if (S(i) < one.and.S(i) + dy(i) > Smax)
                        then
    fac = accel * (half * (one + Smax) - S(i)) / dy(i); iok = 0; exit
    end if
                       if (S(i) >= one.and.dy(i) > half * (Smax - one))
                        then
    fac = 0.25 * (Smax - one) / dy(i); iok = 0; exit
    end if
                     end if
                   end do
                            if (iok == 1.and.ns < 1.and.h0 < h0max.and.h0 + dy(0) > h0max + dh0max)
                                then
                     !start of runoff
                 fac = (h0max + half * dh0max - h0) / dy(0); iok = 0
               end if
               if (iok == 1.and.ns < 1.and.h0 > zero.and.h0 + dy(0) < h0min)
                        then
                 !pond going
                fac = -(h0 - half * h0min) / dy(0); iok = 0
               end if
               if (iok == 0) then !reduce time step
                 t = t - dt; dt = fac * dt; t = t + dt; rsigdt = 1./ (sig * dt)
                 nless = nless + 1 !count step size reductions
                  end if
                  if (isat(1) /= 0.and.iflux == 1.and.h(1) < zero.and. &
                    h(1) + dy(1) > zero)
                        then
                 !incipient ponding - adjust state of saturated regions
                 t = t - dt; dt = 1.0e-20 * (tfin - ts); rsigdt = 1./ (sig * dt)
                 again =.true.; iok = 0
               end if
             end if
           }
                        !-----end get and solve eqns
         !-----update unknowns
          ih0 = 0
           if (.not.again)
                then
  dwoff = zero
             if (ns < 1)
                then
        h0 = h0 + dy(0)
               if (h0 < zero.and.dy(0) < zero) ih0 = 1 !pond gone
                    evap = evap + qevap * dt
               !note that fluxes required are q at sigma of time step
               dwinfil = (q(0) + sig * (qya(0) * dy(0) + qyb(0) * dy(1))) * dt
             else
               dwinfil = (q(0) + sig * qyb(0) * dy(1)) * dt
               if (maxpond)
                then
     evap = evap + qevap * dt
                 if (qprec > qprecmax) then !set input to maintain pond
                   qpme = (q(0) + sig * qyb(0) * dy(1))
                   qprec1 = qpme + qevap
                   dwoff = zero
                 else
                   dwoff = qpme * dt - dwinfil
                 end if
                 runoff = runoff + dwoff
               else
                evap = evap + qprec1 * dt - dwinfil
               end if
             end if
             infil = infil + dwinfil
             if (nsol > 0) then !get surface solute balance
               if (initpond) then !pond concn /= cin
                 if (h0 > zero)
                then
                   if (ns == 1) dy(0) = zero ! if max pond depth
                   cav = ((two * h0 - dy(0)) * c0 + qprec1 * dt * cin) / (two * h0 + dwoff + dwinfil)
                   c0 = two * cav - c0
                 else
                   cav = ((h0 - dy(0)) * c0 + qprec1 * dt * cin) / (dwoff + dwinfil)
                   initpond =.false. !pond gone
                   c0 = cin ! for output if any pond at end
                   end if
                   soff = soff + dwoff * cav
                   sinfil = sinfil + dwinfil * cav
                 else
                soff = soff + dwoff * cin
                 sinfil = sinfil + (qprec1 * dt - dwoff) * cin
               end if
             end if
             if (botbc == "constant head")
                        then
drn = drn + (q(n) + sig * qya(n) * dy(n)) * dt
             else
               drn = drn + (q(n) + sig * qya(n) * dy(n)) * dt
             end if
             if (extraction)
                    then
               if (nsol > 0)
                then
!dwexs = dwexs + (qwexs + sig * qwexsd * spread(dy(1:n), 2, nex)) * dt
                 do i = 1,nex
                    dwexs(:,i)= dwexs(:, i) + (qwexs(:, i) + sig * qwexsd(:, i) * dy(1:n)) * dt
                 end do
                end if
                if (present(wex))
                    then
!wex = wex + (qwexs + sig * qwexsd * spread(dy(1:n), 2, nex)) * dt
                 do i = 1,nex
                    wex(:,i)= wex(:, i) + (qwexs(:, i) + sig * qwexsd(:, i) * dy(1:n)) * dt
                 end do
                end if
              end if
            end if
            do i = 1,n
             if (isat(i) == 0)
                then
               if (.not.again)
                then
  S(i) = S(i) + dy(i)
                 if (S(i) > one.and.dy(i) > zero) then !saturation of layer
                   isat(i) = 1; h(i) = he(i)
                 end if
               end if
             else
                    h(i) = h(i) + dy(i)
               if (i == 1.and.ih0 /= 0.and.h(i) >= he(i)) h(i) = he(i) - one !pond gone
               if (h(i) < he(i)) then !desaturation of layer
                 isat(i) = 0; h(i) = he(i)
               end if
             end if
           end do
                        !-----end update unknowns
         if (.not.again)
                exit
end do
                if (dt <= dtmin)
                    then
       write (*, *) "solve: time step = ",dt
        stop
       end if
     !-----end take next time step
     !remove negative h0 (optional)
     if (h0 < zero.and.isat(1) == 0)
                then
infil = infil + h0
       S(1) = S(1) + h0 / (ths(1) * dx(1)); h0 = zero
     end if
     nsteps = nsteps + 1
     !solve for solute transport if required
     if (nwsteps * (nsteps / nwsteps) == nsteps)
                        then
call getsolute()
     end if
   end do
                    !-----end solve until tfin
 !finalise solute transport if required
        }
    }

        private void Tri(int ns, int n, double[] aa, double[] bb, double[] cc, double[] dd, double[] ee, double[] dy)
        {
            throw new NotImplementedException();
        }
    }
