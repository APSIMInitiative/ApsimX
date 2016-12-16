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
        private class MicroClimateZone
        {
            public Zone zone = null;
            /// <summary>The weather</summary>
            [Link]
            private IWeather weather = null;

            /// <summary>The _albedo</summary>
            public double _albedo = 0;

            /// <summary>The net long wave</summary>
            public double netLongWave;

            /// <summary>The sum rs</summary>
            public double sumRs;

            /// <summary>The average t</summary>
            public double averageT;

            /// <summary>The sunshine hours</summary>
            public double sunshineHours;

            /// <summary>The fraction clear sky</summary>
            public double fractionClearSky;

            /// <summary>The day length</summary>
            public double dayLength;

            /// <summary>The day length light</summary>
            public double dayLengthLight;

            /// <summary>The delta z</summary>
            public double[] DeltaZ = new double[-1 + 1];

            /// <summary>The layer ktot</summary>
            public double[] layerKtot = new double[-1 + 1];

            /// <summary>The layer la isum</summary>
            public double[] layerLAIsum = new double[-1 + 1];

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
                LightExtinction();
            }

            /// <summary>Break the combined Canopy into layers</summary>
            private void DefineLayers()
            {
                double[] nodes = new double[2 * Canopies.Count];
                int numNodes = 1;
                for (int compNo = 0; compNo <= Canopies.Count - 1; compNo++)
                {
                    double height = Canopies[compNo].HeightMetres;
                    double canopyBase = height - Canopies[compNo].DepthMetres;
                    if (Array.IndexOf(nodes, height) == -1)
                    {
                        nodes[numNodes] = height;
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
                    Array.Resize<double>(ref layerLAIsum, numLayers);

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
                {
                    DeltaZ[i] = nodes[i + 1] - nodes[i];
                }
            }

            /// <summary>Break the components into layers</summary>
            private void DivideComponents()
            {
                double[] Ld = new double[Canopies.Count];
                for (int j = 0; j <= Canopies.Count - 1; j++)
                {
                    CanopyType componentData = Canopies[j];

                    componentData.layerLAI = new double[numLayers];
                    componentData.layerLAItot = new double[numLayers];
                    Ld[j] = MathUtilities.Divide(componentData.Canopy.LAITotal, componentData.DepthMetres, 0.0);
                }
                double top = 0.0;
                double bottom = 0.0;

                for (int i = 0; i <= numLayers - 1; i++)
                {
                    bottom = top;
                    top = top + DeltaZ[i];
                    layerLAIsum[i] = 0.0;

                    // Calculate LAI for layer i and component j
                    // ===========================================
                    for (int j = 0; j <= Canopies.Count - 1; j++)
                        if ((Canopies[j].HeightMetres > bottom) && (Canopies[j].HeightMetres - Canopies[j].DepthMetres < top))
                        {
                            Canopies[j].layerLAItot[i] = Ld[j] * DeltaZ[i];
                            Canopies[j].layerLAI[i] = Canopies[j].layerLAItot[i] * MathUtilities.Divide(Canopies[j].Canopy.LAI, Canopies[j].Canopy.LAITotal, 0.0);
                            layerLAIsum[i] += Canopies[j].layerLAItot[i];
                        }

                    // Calculate fractional contribution for layer i and component j
                    // ====================================================================
                    for (int j = 0; j <= Canopies.Count - 1; j++)
                    {
                        Canopies[j].Ftot[i] = MathUtilities.Divide(Canopies[j].layerLAItot[i], layerLAIsum[i], 0.0);
                        // Note: Sum of Fgreen will be < 1 as it is green over total
                        Canopies[j].Fgreen[i] = MathUtilities.Divide(Canopies[j].layerLAI[i], layerLAIsum[i], 0.0);
                    }
                }
            }
            /// <summary>Calculate light extinction parameters</summary>
            private void LightExtinction()
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
                _albedo = 0.0;// albedo;
                netLongWave = 0;
                sumRs = 0;
                averageT = 0;
                sunshineHours = 0;
                fractionClearSky = 0;
                dayLength = 0;
                dayLengthLight = 0;
                numLayers = 0;
                DeltaZ = new double[-1 + 1];
                layerKtot = new double[-1 + 1];
                layerLAIsum = new double[-1 + 1];
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
            public double net_radn
            {
                get { return weather.Radn * (1.0 - _albedo) + netLongWave; }
            }

            /// <summary>Gets the net_rs.</summary>
            [Description("Net short-wave radiation of the whole system")]
            [Units("MJ/m2/day")]
            public double net_rs
            {
                get { return weather.Radn * (1.0 - _albedo); }
            }

            /// <summary>Gets the net_rl.</summary>
            [Description("Net long-wave radiation of the whole system")]
            [Units("MJ/m2/day")]
            public double net_rl
            {
                get { return netLongWave; }
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