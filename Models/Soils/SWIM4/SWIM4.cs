using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Models.Core;
using Models.Interfaces;
using MathNet.Numerics.LinearAlgebra;
using APSIM.Shared.Utilities;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Models.Soils.SWIM4
{
    /// <summary>
    /// Implementation of SWIM with pre-calculate flux values
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Soil))]
    class SWIM4 : Model
    {
        [Link]
        Clock Clock;
        [Link]
        Soil Soil;

        int n = 1;
        int nt = 2;
        int ns = 2; // 1 layers, 2 soil types, 2 solutes
        int j;
        int[] jt, sidx;//n
        int nsteps;
        int[] nssteps; //ns
        double drn, evap, h0, h1, h2, infil, qevap, qprec, runoff;
        double S1, S2, ti, tf, ts, win, wp, wpi;
        double[] bd, dis; //nt
        double[] c0, cin, sdrn, sinfil, soff, spi;//ns
        double[] h, S, x;//n
        double[,] sm;//(n, ns)
        //define isotype and isopar for solute 2
        string[] isotype;//(nt)
        double[,] isopar; //2 params (nt,2)

        /// <summary>Initialise soil and generate flux tables</summary>
        /// <param name="sender">The sender model</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            nt = Soil.BD.Count();
            ns = Soil.BD.Count();

            SoilTables.SoilProperties = new Dictionary<string, SoilProps>();
            Fluxes.FluxTables = new Dictionary<string, FluxTable>();
            GenerateFlux();

            c0 = new double[ns + 1];
            cin = new double[ns + 1];
            h = new double[n + 1];
            S = new double[n + 1];
            isotype = new string[nt + 1];
            isopar = new double[nt + 1, 2 + 1];
            soff = new double[ns + 1];
            sdrn = new double[ns + 1];
            sinfil = new double[ns + 1];
            SoilData sd = new SoilData();
            Flow.sink = new SinkDripperDrain(); //set the type of sink to use
            jt = new int[n + 1];
            nssteps = new int[ns + 1];
            sidx = new int[n + 1];
            sm = new double[ns + 1, n + 1];
            //set a list of soil layers(cm).
            x = new double[] { 0, 10.0, 20.0, 30.0, 40.0, 60.0, 80.0, 100.0, 120.0, 160.0, 200.0 };
            for (int c = 1; c <= n; c++)
                sidx[c] = c < 5 ? 103 : 109; //soil ident of layers
                                             //set required soil hydraulic params
            sd.GetTables(n, sidx, x);
            SolProps solProps = new SolProps(nt, ns);
            Array.Copy(Soil.BD, 0, new double[Soil.BD.Length + 1], 1, Soil.BD.Length);

            dis = new double[] { Soil.BD.Length + 1}; //TODO: put some real data here
            //set isotherm type and params for solute 2 here
            isotype[1] = "Fr";
            isotype[2] = "La";
            isopar[1, 1] = 1.0;
            isopar[1, 2] = 1.0;
            isopar[2, 1] = 0.5;
            isopar[2, 2] = 0.01;
            Matrix<double> isoparM = Matrix<double>.Build.DenseOfArray(isopar);
            for (j = 1; j <= nt; j++) //set params
            {
                solProps.Solpar(j, bd[j], dis[j]);
                //set isotherm type and params
                solProps.Setiso(j, 2, isotype[j], isoparM.Column(j).ToArray());
            }
            //initialise for run
            ts = 0.0; //start time
                      //dSmax controls time step.Use 0.05 for a fast but fairly accurate solution.
                      //Use 0.001 to get many steps to test execution time per step.
            Flow.dSmax = 0.01; //0.01 ensures very good accuracy
            for (int c = 1; c <= n; c++)
                jt[c] = c; //currenty using 1 SWIM layer per APSIM layer
            h0 = 0.0; //pond depth initially zero
            h1 = -1000.0;
            h2 = -400.0; //initial matric heads
            double Sh = 0; //not used for this call but required as C# does not have 'present' operator
            for (int c = 1; c <= n; c++)
                sd.Sofh(h1, 1, out S[c], out Sh); //solve uses degree of satn
            wpi = MathUtilities.Sum(MathUtilities.Multiply(MathUtilities.Multiply(sd.ths, S), sd.dx)); //water in profile initially
            nsteps = 0; //no.of time steps for water soln(cumulative)
            win = 0.0; //water input(total precip)
            evap = 0.0;
            runoff = 0.0;
            infil = 0.0;
            drn = 0.0;
            for (int col = 1; col < sm.GetLength(0); col++)
                sm[col, 1] = 1000.0 / sd.dx[1];
            //initial solute concn(mass units per cc of soil)
            //solute in profile initially
            spi = new[] { 1000.0, 1000.0 };
            Flow.dsmmax = 0.1 * sm[1, 1]; //solute stepsize control param
            Flow.nwsteps = 10;
            MathUtilities.Zero(c0);
            MathUtilities.Zero(cin); //no solute input
            nssteps.Populate(0); //no.of time steps for solute soln(cumulative)
            MathUtilities.Zero(soff);
            MathUtilities.Zero(sinfil);
            MathUtilities.Zero(sdrn);
            qprec = 1.0; //precip at 1 cm / h for first 24 h
            ti = ts;
            qevap = 0.05;// potential evap rate from soil surface
            double[,] wex = new double[1, 1]; //unused option params in FORTRAN... must be a better way of doing this
            double[,,] sex = new double[1, 1, 1];
        }

        public static void GenerateFlux()
        {

            MVG.TestParams(103, 9.0, 0.99670220130280185, 9.99999999999998460E-003);
            SoilProps sp = SoilTables.gensptbl(1.0, new SoilParam(10, 103, 0.4, 2.0, -2.0, -10.0, 1.0 / 3.0, 1.0), true);
            Fluxes.FluxTable(5.0, sp);
            FluxTable ft = Fluxes.ft;

            string output = string.Empty;
            //define test soils
            SoilParam[] soils = new SoilParam[2];
            soils[0] = new SoilParam(10, 103, 0.4, 2.0, -2.0, -10.0, 1.0 / 3.0, 1.0);
            soils[1] = new SoilParam(10, 109, 0.6, 0.2, -2.0, -40.0, 1.0 / 9.0, 1.0);

            string[] ftname = new string[2];
            int[] sidx;
            int i, j;
            int[] ndz;
            double dzmin;
            double[] x;
            double[,] dz = new double[2, 10]; //only for testing? if not will need to change hardcoded dimensions.
            bool Kgiven = true;

            //define soil profile
            x = new double[] { 10, 20, 30, 40, 60, 80, 100, 120, 160, 200 }; //length = num soil layers
            sidx = new int[] { 103, 103, 103, 103, 109, 109, 109, 109, 109, 109 }; //soil ident of layers
            dzmin = 1.0; // smallest likely path length
            ndz = new int[] { 2, 4 }; // for the two soil types - gives six flux tables
            //can be done in loops, but clearer this way and will only be used for testing
            dz[0, 0] = 5;
            dz[0, 1] = 10;
            dz[1, 0] = 10;
            dz[1, 1] = 20;
            dz[1, 2] = 30;
            dz[1, 4] = 40;
            for (i = 0; i < 2; i++)
            {
                MVG.Params(soils[i].sid, soils[i].ths, soils[i].ks, soils[i].he, soils[i].hd, soils[i].p, soils[i].hg, soils[i].em, soils[i].en); //set MVG params
                soils[i].sp = SoilTables.gensptbl(dzmin, soils[i], Kgiven); // generate soil props
                SoilTables.SoilProperties.Add("soil" + soils[i].sid, soils[i].sp);
                for (j = 0; j <= ndz[i]; j++)
                {
                    Fluxes.FluxTable(dz[i, j], soils[i].sp); // generate flux tables
                    using (MemoryStream ms = new MemoryStream()) // make copies of the tables or they get overwritten
                    {
                        BinaryFormatter fm = new BinaryFormatter();
                        fm.Serialize(ms, Fluxes.ft);
                        ms.Position = 0;
                        Fluxes.FluxTables.Add("soil" + soils[i].sid + "dz" + (dz[i, j] * 10), (FluxTable)fm.Deserialize(ms));
                    }
                }
            }
            SoilProps sp1 = SoilTables.ReadProps("soil103");
            SoilProps sp2 = SoilTables.ReadProps("soil109");
            FluxTable ft1 = Fluxes.ReadFluxTable("soil103dz50");
            FluxTable ft2 = Fluxes.ReadFluxTable("soil109dz100");
            FluxTable ftwo = TwoFluxes.TwoTables(ft1, sp1, ft2, sp2);
            Fluxes.FluxTables.Add("soil103dz50_soil109dz100", ftwo);
        }
    }
}
