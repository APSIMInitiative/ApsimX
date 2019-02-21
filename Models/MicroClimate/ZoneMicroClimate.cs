using System;
using APSIM.Shared.Utilities;
using System.Collections.Generic;
using Models.Core;
using Models.Interfaces;

namespace Models
{
    public partial class MicroClimate
    {
        /// <summary>
        /// MicroClimateZone
        /// </summary>
        [Serializable]
        private class ZoneMicroClimate
        {
            public Zone zone = null;
            /// <summary>The weather</summary>
            [Link]
            private IWeather weather = null;

            /// <summary>The Albedo of the combined soil-plant system for this zone</summary>
            public double Albedo = 0;
            /// <summary>
            /// Emissivity of the combined soil-plant system for this zone
            /// </summary>
            public double Emissivity = 0;

            /// <summary>Net long-wave radiation of the whole system</summary>
            [Description("Net long-wave radiation of the whole system")]
            [Units("MJ/m2/day")]
            public double NetLongWaveRadiation;

            /// <summary>The sum rs</summary>
            public double sumRs;

            /// <summary>The incoming rs</summary>
            public double IncomingRs;

            /// <summary>The shortwave radiation reaching the surface</summary>
            public double SurfaceRs = 0;


            /// <summary>The delta z</summary>
            public double[] DeltaZ = new double[-1 + 1];

            /// <summary>The layer ktot</summary>
            public double[] layerKtot = new double[-1 + 1];

            /// <summary>The layer la isum</summary>
            public double[] LAItotsum = new double[-1 + 1];

            /// <summary>The number layers</summary>
            public int numLayers;

            /// <summary>The soil_heat</summary>
            public double soil_heat = 0;

            /// <summary>The dryleaffraction</summary>
            public double dryleaffraction = 0;

            /// <summary>Gets or sets the component data.</summary>
            public List<CanopyType> Canopies = new List<CanopyType>();

            /// <summary>Canopies the compartments.</summary>
            public void DoCanopyCompartments()
            {
                DefineLayers();
                DivideComponents();
                CalculateLightExtinctionVariables();
            }

            /// <summary>Break the combined Canopy into layers</summary>
            private void DefineLayers()
            {
                double[] nodes = new double[2 * Canopies.Count];
                int numNodes = 1;
                for (int compNo = 0; compNo <= Canopies.Count - 1; compNo++)
                {
                    double HeightMetres = Math.Round(Canopies[compNo].Canopy.Height, 5) / 1000.0; // Round off a bit and convert mm to m } }
                    double DepthMetres = Math.Round(Canopies[compNo].Canopy.Depth, 5) / 1000.0; // Round off a bit and convert mm to m } }
                    double canopyBase = HeightMetres - DepthMetres;
                    if (Array.IndexOf(nodes, HeightMetres) == -1)
                    {
                        nodes[numNodes] = HeightMetres;
                        numNodes = numNodes + 1;
                    }
                    if (Array.IndexOf(nodes, canopyBase) == -1)
                    {
                        nodes[numNodes] = canopyBase;
                        numNodes = numNodes + 1;
                    }
                }
                Array.Resize<double>(ref nodes, numNodes);
                Array.Sort(nodes);
                numLayers = numNodes - 1;
                if (DeltaZ.Length != numLayers)
                {
                    // Number of layers has changed; adjust array lengths
                    Array.Resize<double>(ref DeltaZ, numLayers);
                    Array.Resize<double>(ref layerKtot, numLayers);
                    Array.Resize<double>(ref LAItotsum, numLayers);

                    for (int j = 0; j <= Canopies.Count - 1; j++)
                    {
                        Array.Resize<double>(ref Canopies[j].Ftot, numLayers);
                        Array.Resize<double>(ref Canopies[j].Fgreen, numLayers);
                        Array.Resize<double>(ref Canopies[j].Rs, numLayers);
                        Array.Resize<double>(ref Canopies[j].Rl, numLayers);
                        Array.Resize<double>(ref Canopies[j].Rsoil, numLayers);
                        Array.Resize<double>(ref Canopies[j].Gc, numLayers);
                        Array.Resize<double>(ref Canopies[j].Ga, numLayers);
                        Array.Resize<double>(ref Canopies[j].PET, numLayers);
                        Array.Resize<double>(ref Canopies[j].PETr, numLayers);
                        Array.Resize<double>(ref Canopies[j].PETa, numLayers);
                        Array.Resize<double>(ref Canopies[j].Omega, numLayers);
                        Array.Resize<double>(ref Canopies[j].interception, numLayers);
                    }
                }
                for (int i = 0; i <= numNodes - 2; i++)
                    DeltaZ[i] = nodes[i + 1] - nodes[i];
            }

            /// <summary>Break the components into layers</summary>
            private void DivideComponents()
            {
                double top = 0.0;
                double bottom = 0.0;
                for (int i = 0; i <= numLayers - 1; i++)
                {
                    bottom = top;
                    top = top + DeltaZ[i];
                    LAItotsum[i] = 0.0;

                    // Calculate LAI for layer i and component j
                    // ===========================================
                    for (int j = 0; j <= Canopies.Count - 1; j++)
                    {
                        Array.Resize(ref Canopies[j].LAI, numLayers);
                        Array.Resize(ref Canopies[j].LAItot, numLayers);
                        double HeightMetres = Math.Round(Canopies[j].Canopy.Height, 5) / 1000.0; // Round off a bit and convert mm to m } }
                        double DepthMetres = Math.Round(Canopies[j].Canopy.Depth, 5) / 1000.0; // Round off a bit and convert mm to m } }
                        if ((HeightMetres > bottom) && (HeightMetres - DepthMetres < top))
                        {
                            double Ld = MathUtilities.Divide(Canopies[j].Canopy.LAITotal, DepthMetres, 0.0);
                            Canopies[j].LAItot[i] = Ld * DeltaZ[i];
                            Canopies[j].LAI[i] = Canopies[j].LAItot[i] * MathUtilities.Divide(Canopies[j].Canopy.LAI, Canopies[j].Canopy.LAITotal, 0.0);
                            LAItotsum[i] += Canopies[j].LAItot[i];
                        }
                    }
                    // Calculate fractional contribution for layer i and component j
                    // ====================================================================
                    for (int j = 0; j <= Canopies.Count - 1; j++)
                    {
                        Canopies[j].Ftot[i] = MathUtilities.Divide(Canopies[j].LAItot[i], LAItotsum[i], 0.0);
                        Canopies[j].Fgreen[i] = MathUtilities.Divide(Canopies[j].LAI[i], LAItotsum[i], 0.0);  // Note: Sum of Fgreen will be < 1 as it is green over total
                    }
                }
            }
            /// <summary>Calculate light extinction parameters</summary>
            private void CalculateLightExtinctionVariables()
            {
                // Calculate effective K from LAI and cover
                // =========================================
                for (int j = 0; j <= Canopies.Count - 1; j++)
                {
                    if (MathUtilities.FloatsAreEqual(Canopies[j].Canopy.CoverGreen, 1.0, 1E-10))
                        throw new Exception("Unrealistically high cover value in MicroMet i.e. > 0.999999999");

                    Canopies[j].K = MathUtilities.Divide(-Math.Log(1.0 - Canopies[j].Canopy.CoverGreen), Canopies[j].Canopy.LAI, 0.0);
                    Canopies[j].Ktot = MathUtilities.Divide(-Math.Log(1.0 - Canopies[j].Canopy.CoverTotal), Canopies[j].Canopy.LAITotal, 0.0);
                }

                // Calculate extinction for individual layers
                // ============================================
                for (int i = 0; i <= numLayers - 1; i++)
                {
                    layerKtot[i] = 0.0;
                    for (int j = 0; j <= Canopies.Count - 1; j++)
                        layerKtot[i] += Canopies[j].Ftot[i] * Canopies[j].Ktot;
                }
            }

            /// <summary>
            /// Reset class state
            /// </summary>
            public void Reset()
            {
                soil_heat = 0.0;
                dryleaffraction = 0.0;
                Albedo = 0.0;// albedo;
                NetLongWaveRadiation = 0;
                sumRs = 0;
                IncomingRs = 0;
                SurfaceRs = 0.0;
                numLayers = 0;
                DeltaZ = new double[-1 + 1];
                layerKtot = new double[-1 + 1];
                LAItotsum = new double[-1 + 1];
                Canopies.Clear();
            }

            /// <summary>Gets the petr.</summary>
            [Description("Radiation component of PET")]
            [Units("mm/day")]
            public double petr
            {
                get
                {
                    double totalPetr = 0.0;
                    for (int i = 0; i <= numLayers - 1; i++)
                        for (int j = 0; j <= Canopies.Count - 1; j++)
                            totalPetr += Canopies[j].PETr[i];
                    return totalPetr;
                }
            }

            /// <summary>Gets the peta.</summary>
            [Description("Aerodynamic component of PET")]
            [Units("mm/day")]
            public double peta
            {
                get
                {
                    double totalPeta = 0.0;
                    for (int i = 0; i <= numLayers - 1; i++)
                        for (int j = 0; j <= Canopies.Count - 1; j++)
                            totalPeta += Canopies[j].PETa[i];
                    return totalPeta;
                }
            }
            /// <summary>Gets the net_radn.</summary>
            [Description("Net all-wave radiation of the whole system")]
            [Units("MJ/m2/day")]
            public double NetRadiation
            {
                get { return weather.Radn * (1.0 - Albedo) + NetLongWaveRadiation; }
            }

            /// <summary>Gets the net_rs.</summary>
            [Description("Net short-wave radiation of the whole system")]
            [Units("MJ/m2/day")]
            public double NetShortWaveRadiation
            {
                get { return weather.Radn * (1.0 - Albedo); }
            }

            /// <summary>
            /// Calculate the proportion of light intercepted by a given component that corresponds to green leaf
            /// </summary>
            public double RadnGreenFraction(int j)
            {
                double klGreen = -Math.Log(1.0 - Canopies[j].Canopy.CoverGreen);
                double klTot = -Math.Log(1.0 - Canopies[j].Canopy.CoverTotal);
                return MathUtilities.Divide(klGreen, klTot, 0.0);
            }
        }
    }
}