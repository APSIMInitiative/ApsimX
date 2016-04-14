using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SWIMFrame
{
    /* Example module for extraction of water and solutes using sink terms.
     * Drippers are sources (-ve sinks) of water and solute while drains are sinks.
     * A layer may not have both drippers and drains. Solute movement is calculated
     * every few steps of water movement calculation. The water flow routine
     * accumulates total water extraction for each layer while the solute transport
     * routine calculates solute extraction.
    */
    public class SinkDripperDrain : ISink
    {
        public int nex { get; set; }
        public bool drip;

        int idrip;
        int idrn;
        int n;
        int ns;
        double dcond;
        double driprate;
        double[] dripsol;

        //Set sink parameters.
        public void SetSinks(int n, int ns, int idrip, int idrn, double driprate, double[] dripsol, double dcond)
        {
            this.n = n;
            this.ns = ns;
            this.idrip = idrip;
            this.idrn = idrn;
            if (idrip == idrn)
            {
                Console.WriteLine("setsinks: can't have dripper and drain in same layer");
                Environment.Exit(1);
            }
            this.driprate = driprate;
            this.dcond = dcond;
            this.dripsol = dripsol;
            drip = false; //set drippers off
        }

        /// <summary>
        /// Get layer water extraction rates (cm/h).
        /// </summary>
        /// <param name="t">Current time.</param>
        /// <param name="isat">Layer saturation 0 for unsat, 1 for sat.</param>
        /// <param name="var">Layer sat S or head diff h - he.</param>
        /// <param name="qwex">Layer extraction rates.</param>
        /// <param name="qwexd">Partial derivs of qwex wrt S or h.</param>
        public void Wsinks(double t, int[] isat, double[] var, out double[,] qwex, out double[,] qwexd)
        {
            qwex = new double[n, n]; //check this
            qwexd = new double[n, n]; //and this
            if (drip)
                qwex[idrip, 1] = -driprate; //-ve because source
            else
                qwex[idrip, 1] = 0.0;

            qwexd[idrip, 1] = 0.0;
    /*        if (isat[idrn] != 0 && var[idrn] + he[idrn] > 0.0)
            {
                qwex[idrn, 1] = dcond * (var[idrn] + he[idrn]); //current drainage rate
                qwexd[idrn, 1] = dcond; //current derivative
            }
            else
            {
                qwex[idrn, 1] = 0.0;
                qwexd[idrn, 1] = 0.0;
            }*/
        }

        /// <summary>
        /// Get layer solute extraction rates (mass/h).
        /// </summary>
        /// <param name="t">Current time.</param>
        /// <param name="ti">Initial time.</param>
        /// <param name="tf">Final time.</param>
        /// <param name="dwex">Water extraction from layers.</param>
        /// <param name="c">Layer concentrations.</param>
        /// <param name="qsex">Layer extraction rates.</param>
        /// <param name="qsexd">Partial derivitives of qsex wrt c</param>
        public void Ssinks(double t, double ti, double tf, int isol, double[,] dwex, double[] c, out double[,] qsex, out double[,] qsexd)
        {
            qsex = new double[n, n];
            qsexd = new double[n, n];
            qsex[idrip, 1] = dripsol[isol] * dwex[idrip, 1] / (tf - ti); //dripper solute
            qsex[idrn, 1] = c[idrn] * dwex[idrn, 1] / (tf - ti); // solute in drainage
            qsexd[idrn, 1] = dwex[idrn, 1] / (tf - ti);
        }
    }
}
