using System;
using System.Reflection;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Xml;
using Models.Core;
using Models;
using APSIM.Shared.Utilities;

namespace Models.Soils
{

    /// <remarks>
    /// This partial class contains part of the SoilCN patch, with variables and general processes
    /// </remarks>
    public partial class SoilNitrogen
    {

        /// <summary>
        /// Class containing all the state variables and specific soil C and N processes
        /// </summary>
        /// <remarks>
        /// This can instanciated many times, used for describing soil variability
        /// </remarks>
        [Serializable]
        public partial class soilCNPatch
        {
            /// <summary>The soilCNPatch constructor</summary>
            public soilCNPatch(SoilNitrogen MainSoilNitrogen)
            { g = MainSoilNitrogen; }

            #region Patch general variables

            /// <summary>
            /// Name of this patch
            /// </summary>
            public string PatchName = "base";

            /// <summary>
            /// Relative area of this patch (0-1)
            /// </summary>
            public double RelativeArea = 1.0;

            /// <summary>
            /// Date at which this patch was created
            /// </summary>
            public DateTime CreationDate;

            /// <summary>
            /// Reference to main SoilNitrogen Class - for accessing the parameters and input variables
            /// </summary>
            private SoilNitrogen g;

            #endregion

            #region Input variables

            #region Get/set variables

            #region Values for mineral N

            /// <summary>Amount of soil urea nitrogen (kgN/ha)</summary>
            public double[] urea;

            /// <summary>Amount of soil ammonium nitrogen (kgN/ha)</summary>
            public double[] nh4;

            /// <summary>Amount of soil nitrate nitrogen (kgN/ha)</summary>
            public double[] no3;

            /// <summary>Amount of soil ammonia nitrogen (kgN/ha)</summary>
            public double[] nh3;

            /// <summary>Amount of soil nitrite nitrogen (kgN/ha)</summary>
            public double[] no2;

            /// <summary>
            /// Amount of NH4 plus NO3 in the root zone (kg/ha)
            /// </summary>
            internal double totalMineralNInRootZone = 0.0;

            #endregion

            #region Values for soil organic C and N

            /// <summary>
            /// Amount of C for each soil layer in each FOM pool
            /// </summary>
            public double[][] fom_c = new double[3][];

            /// <summary>
            /// Nitrogen amount in FOM (per pool)
            /// </summary>
            public double[][] fom_n = new double[3][];

            /// <summary>
            /// Amount of C for each soil layer in soil m. biomass pool
            /// </summary>
            public double[] biom_c;

            /// <summary>
            /// Amount of water soluble C for each soil layer (for denitrification)
            /// </summary>
            public double[] waterSoluble_c;

            /// <summary>
            /// Nitrogen amount in soil m. biomass
            /// </summary>
            public double[] biom_n;

            /// <summary>
            /// Amount of C for each soil layer in soil a. humus pool
            /// </summary>
            public double[] hum_c;

            /// <summary>
            /// Nitrogen amount in soil humus
            /// </summary>
            public double[] hum_n;

            /// <summary>
            /// Amount of C for each soil layer in soil inert humus pool
            /// </summary>
            public double[] inert_c;

            /// <summary>
            /// Nitrogen amount in soil inert humus
            /// </summary>
            public double[] inert_n;

            #endregion

            #region Values for soil pH

            /// <summary>Soil pH value</summary>
            public double[] pH;

            #endregion

            #endregion

            #region Settable only variables

            #region Deltas in mineral nitrogen

            /// <summary>
            /// Variations in urea as given by another component
            /// </summary>
            /// <remarks>
            /// This property checks changes in the amount of urea at each soil layer
            ///  - If values are not supplied for all layers, these will be assumed zero (no changes)
            ///  - If values are supplied in excess, these will ignored
            ///  - The actual amounts are also checked for negative values
            /// </remarks>
            public double[] dlt_urea
            {
                set
                {
                    for (int layer = 0; layer < Math.Min(value.Length, g.nLayers); ++layer)
                    {
                        // update variable and check its value
                        urea[layer] += value[layer];
                        g.CheckNegativeValues(ref urea[layer], layer, "urea", "Patch[" + PatchName + "].deltaUrea");

                        // record these values to use as outputs
                        if (g.senderModule == "WaterModule".ToLower())
                            urea_flow[layer] += value[layer];
                        else if (g.senderModule == "Plant".ToLower())
                            urea_uptake[layer] += value[layer];
                        else if (g.senderModule == "Fertiliser".ToLower())
                            urea_fertiliser[layer] += value[layer];
                        else
                            urea_ChangedOther[layer] += value[layer];
                    }
                }
            }

            /// <summary>
            /// Variations in nh4 as given by another component
            /// </summary>
            /// <remarks>
            /// This property checks changes in the amount of urea at each soil layer
            ///  - If values are not supplied for all layers, these will be assumed zero (no changes)
            ///  - If values are supplied in excess, these will ignored
            ///  - The actual amounts are also checked for negative values
            /// </remarks>
            public double[] dlt_nh4
            {
                set
                {
                    for (int layer = 0; layer < Math.Min(value.Length, g.nLayers); ++layer)
                    {
                        // update variable and check its value
                        nh4[layer] += value[layer];
                        g.CheckNegativeValues(ref nh4[layer], layer, "nh4", "Patch[" + PatchName + "].deltaNH4");

                        // record these values to use as outputs
                        if (g.senderModule == "WaterModule".ToLower())
                            nh4_flow[layer] += value[layer];
                        else if (g.senderModule == "Plant".ToLower())
                            nh4_uptake[layer] += value[layer];
                        else if (g.senderModule == "Fertiliser".ToLower())
                            nh4_fertiliser[layer] += value[layer];
                        else
                            nh4_ChangedOther[layer] += value[layer];
                    }
                }
            }

            /// <summary>
            /// Variations in no3 as given by another component
            /// </summary>
            /// <remarks>
            /// This property checks changes in the amount of urea at each soil layer
            ///  - If values are not supplied for all layers, these will be assumed zero (no changes)
            ///  - If values are supplied in excess, these will ignored
            ///  - The actual amounts are also checked for negative values
            /// </remarks>
            public double[] dlt_no3
            {
                set
                {
                    for (int layer = 0; layer < Math.Min(value.Length, g.nLayers); ++layer)
                    {
                        // update variable and check its value
                        no3[layer] += value[layer];
                        g.CheckNegativeValues(ref no3[layer], layer, "no3", "Patch[" + PatchName + "].deltaNO3");

                        // record these values to use as outputs
                        if (g.senderModule == "WaterModule".ToLower())
                            no3_flow[layer] += value[layer];
                        else if (g.senderModule == "Plant".ToLower())
                            no3_uptake[layer] += value[layer];
                        else if (g.senderModule == "Fertiliser".ToLower())
                            no3_fertiliser[layer] += value[layer];
                        else
                            no3_ChangedOther[layer] += value[layer];
                    }
                }
            }

            #endregion

            #region Delta soil organic C and N

            /// <summary>
            /// Variation in soil FOM C as sent by another component
            /// </summary>
            public double[][] dlt_fom_c
            {
                set
                {
                    int nPools = value.Length;
                    for (int pool = 0; pool < nPools; pool++)
                    {
                        for (int layer = 0; layer < Math.Min(value[pool].Length, g.nLayers); ++layer)
                        {
                            fom_c[pool][layer] += value[pool][layer];
                            g.CheckNegativeValues(ref fom_c[pool][layer], layer, "FOM_C[" + (pool + 1).ToString() + "]", "Patch[" + PatchName + "].dltFOM");
                        }
                    }
                }
            }

            /// <summary>
            /// Variation in soil FOM N as sent by another component
            /// </summary>
            public double[][] dlt_fom_n
            {
                set
                {
                    int nPools = value.Length;
                    for (int pool = 0; pool < nPools; pool++)
                    {
                        for (int layer = 0; layer < Math.Min(value[pool].Length, g.nLayers); ++layer)
                        {
                            fom_n[pool][layer] += value[pool][layer];
                            g.CheckNegativeValues(ref fom_n[pool][layer], layer, "FOM_N[" + (pool + 1).ToString() + "]", "Patch[" + PatchName + "].dltFOM");
                        }
                    }
                }
            }

            #endregion

            #endregion

            #endregion

            #region Outputs variables

            #region Outputs for Nitrogen

            /// <summary>
            /// Total N in soil
            /// </summary>
            public double[] nit_tot
            {
                get
                {
                    double[] result = null;
                    if (g.dlayer != null)
                    {
                        result = new double[g.nLayers];
                        for (int layer = 0; layer < g.nLayers; ++layer)
                            result[layer] = fom_n[0][layer] +
                                            fom_n[1][layer] +
                                            fom_n[2][layer] +
                                            hum_n[layer] +
                                            biom_n[layer] +
                                            no3[layer] +
                                            nh4[layer] +
                                            urea[layer];
                    }
                    return result;
                }
            }

            /// <summary>
            /// Amount of soil ammonium nitrogen made available to plants (kgN/ha)
            /// </summary>
            internal double[] nh4AvailableToPlants
            {
                get
                {
                    double depthFromSurface = 0.0;
                    double[] result = new double[g.nLayers];
                    double fractionAvailable = Math.Min(1.0,
                           MathUtilities.Divide(g.maxTotalNAvailableToPlants, totalMineralNInRootZone, 0.0));
                    for (int layer = 0; layer < g.nLayers; layer++)
                    {
                        result[layer] = nh4[layer] * fractionAvailable;
                        depthFromSurface += g.dlayer[layer];
                        if (depthFromSurface >= g.rootDepth)
                            break;
                    }
                    return result;
                }
            }

            /// <summary>
            /// Amount of soil nitrate nitrogen made available to plants (kgN/ha)
            /// </summary>
            internal double[] no3AvailableToPlants
            {
                get
                {
                    double depthFromSurface = 0.0;
                    double[] result = new double[g.nLayers];
                    double fractionAvailable = Math.Min(1.0,
                        MathUtilities.Divide(g.maxTotalNAvailableToPlants, totalMineralNInRootZone, 0.0));
                    for (int layer = 0; layer < g.nLayers; layer++)
                    {
                        result[layer] = no3[layer] * fractionAvailable;
                        depthFromSurface += g.dlayer[layer];
                        if (depthFromSurface >= g.rootDepth)
                            break;
                    }
                    return result;
                }
            }

            /// <summary>
            /// N carried out in sediment via runoff/erosion
            /// </summary>
            public double dlt_n_loss_in_sed;

            /// <summary>
            /// Net NH4 mineralisation from residue decomposition
            /// </summary>
            public double[] dlt_res_nh4_min;

            /// <summary>
            /// Net NO3 mineralisation from residue decomposition
            /// </summary>
            public double[] dlt_res_no3_min;

            /// <summary>
            /// Amount of N converted from each FOM pool
            /// </summary>
            public double[][] dlt_n_fom = new double[3][];

            /// <summary>
            /// Net FOM N mineralised (negative for immobilisation)
            /// </summary>
            public double[] dlt_n_fom_to_min;

            /// <summary>
            /// Net N mineralised for humic pool
            /// </summary>
            public double[] dlt_n_hum_to_min;

            /// <summary>
            /// Net N mineralised from m. biomass pool
            /// </summary>
            public double[] dlt_n_biom_to_min;

            /// <summary>
            /// Nitrogen coverted by hydrolisys (urea into NH4)
            /// </summary>
            public double[] dlt_urea_hydrolysis;

            /// <summary>
            /// Nitrogen coverted by nitrification (NH4 into NO3)
            /// </summary>
            public double[] dlt_nitrification;

            /// <summary>
            /// N2O N produced during nitrification
            /// </summary>
            public double[] dlt_n2o_nitrif;

            /// <summary>
            /// NO3 N denitrified
            /// </summary>
            public double[] dlt_no3_dnit;

            /// <summary>
            /// N2O N produced during denitrification
            /// </summary>
            public double[] dlt_n2o_dnit;

            /// <summary>
            /// Nitrogen coverted by codenitrification (N2+N2O)
            /// </summary>
            public double[] dlt_codenitrification;

            /// <summary>
            /// N2O N produced during codenitrification
            /// </summary>
            public double[] dlt_n2o_codenit;

            /// <summary>
            /// Excess N required above NH4 supply (for immobilisation)
            /// </summary>
            public double[] nh4_deficit_immob;

            // -- New outputs, estimated partiton among patches of changes made by other modules --------------------------

            /// <summary>
            /// Amount of urea changed by the soil water module
            /// </summary>
            public double[] urea_flow;

            /// <summary>
            /// Amount of NH4 changed by the soil water module
            /// </summary>
            public double[] nh4_flow;

            /// <summary>
            /// Amount of NO3 changed by the soil water module
            /// </summary>
            public double[] no3_flow;

            /// <summary>
            /// Amount of urea taken by any plant module
            /// </summary>
            public double[] urea_uptake;

            /// <summary>
            /// Amount of NH4 taken by any plant module
            /// </summary>
            public double[] nh4_uptake;

            /// <summary>
            /// Amount of NO3 taken by any plant module
            /// </summary>
            public double[] no3_uptake;

            /// <summary>
            /// Amount of urea added by the fertiliser module
            /// </summary>
            public double[] urea_fertiliser;

            /// <summary>
            /// Amount of NH4 added by the fertiliser module
            /// </summary>
            public double[] nh4_fertiliser;

            /// <summary>
            /// Amount of NO3 added by the fertiliser module
            /// </summary>
            public double[] no3_fertiliser;

            /// <summary>
            /// Amount of urea changed by any other module
            /// </summary>
            public double[] urea_ChangedOther;

            /// <summary>
            /// Amount of NH4 changed by any other module
            /// </summary>
            public double[] nh4_ChangedOther;

            /// <summary>
            /// Amount of NO3 changed by any other module
            /// </summary>
            public double[] no3_ChangedOther;

            #endregion

            #region Outputs for Carbon

            /// <summary>
            /// Total carbon amount in the soil
            /// </summary>
            public double[] carbon_tot
            {
                get
                {
                    double[] result = null;
                    if (g.dlayer != null)
                    {
                        result = new double[g.nLayers];
                        for (int layer = 0; layer < g.nLayers; layer++)
                            result[layer] += fom_c[0][layer] +
                                             fom_c[1][layer] +
                                             fom_c[2][layer] +
                                             hum_c[layer] +
                                             biom_c[layer];
                    }
                    return result;
                }
            }

            /// <summary>
            /// Carbon loss in sediment, via runoff/erosion
            /// </summary>
            public double dlt_c_loss_in_sed;

            /// <summary>
            /// Amount of C from each FOM pool converted into humus
            /// </summary>
            public double[][] dlt_c_fom_to_hum = new double[3][];

            /// <summary>
            /// Amount of C from each FOM pool converted into m. biomass
            /// </summary>
            public double[][] dlt_c_fom_to_biom = new double[3][];

            /// <summary>
            /// Amount of C from each FOM pool lost to the atmosphere
            /// </summary>
            public double[][] dlt_c_fom_to_atm = new double[3][];

            /// <summary>
            /// Humic C converted to biomass
            /// </summary>
            public double[] dlt_c_hum_to_biom;

            /// <summary>
            /// Humic C lost to atmosphere
            /// </summary>
            public double[] dlt_c_hum_to_atm;

            /// <summary>
            /// Biomass C converted to humic
            /// </summary>
            public double[] dlt_c_biom_to_hum;

            /// <summary>
            /// Biomass C lost to atmosphere
            /// </summary>
            public double[] dlt_c_biom_to_atm;

            /// <summary>
            /// Carbon from residues converted to biomass (kg/ha)
            /// </summary>
            public double[] dlt_c_res_to_biom;

            /// <summary>
            /// Carbon from residues converted to humus (kg/ha)
            /// </summary>
            public double[] dlt_c_res_to_hum;

            /// <summary>
            /// Carbon from residues lost to atmosphere during decomposition (kg/ha)
            /// </summary>
            public double[] dlt_c_res_to_atm;

            /// <summary>
            /// Total CO2 amount produced today
            /// </summary>
            public double[] co2_atm
            {
                get
                {
                    double[] result = new double[g.nLayers];
                    for (int layer = 0; layer < g.nLayers; layer++)
                        result[layer] = dlt_c_fom_to_atm[0][layer] +
                                        dlt_c_fom_to_atm[1][layer] +
                                        dlt_c_fom_to_atm[2][layer] +
                                        dlt_c_biom_to_atm[layer] +
                                        dlt_c_hum_to_atm[layer];
                    return result;
                }
            }

            #endregion

            #region Factors and other outputs

            /// <summary>
            /// amount of P coverted by residue mineralisation
            /// </summary>
            public double[] soilp_dlt_org_p;

            #endregion

            #endregion

            #region Internal variables

            #region Residue decomposition information

            /// <summary>
            /// Actual residue C decomposition (kg/ha)
            /// </summary>
            private double[][] dlt_c_decomp = new double[3][];

            /// <summary>
            /// Actual residue N decomposition (kg/ha)
            /// </summary>
            private double[][] dlt_n_decomp = new double[3][];

            /// <summary>
            /// The info with actual residue decomposition
            /// </summary>
            public SurfaceOrganicMatterDecompType SurfOMActualDecomposition;

            #endregion

            #region Miscelaneous

            /// <summary>
            /// Total C content at the beginning of the day
            /// </summary>
            public double TodaysInitialC;

            /// <summary>
            /// Total N content at the beginning of the day
            /// </summary>
            public double TodaysInitialN;

            /// <summary>
            /// Amount of  N as NH4 at the beginning of the day (kg/ha)
            /// </summary>
            public double[] TodaysInitialNH4;

            /// <summary>
            /// Amount of  N as NO3 at the beginning of the day (kg/ha)
            /// </summary>
            public double[] TodaysInitialNO3;

            #endregion

            #endregion

            #region General methods and variable handlers

            /// <summary>
            /// Computes the amount of NH4 and NO3 in the root zone
            /// </summary>
            internal void CalcTotalMineralNInRootZone()
            {
                totalMineralNInRootZone = 0.0;
                double depthFromSurface = 0.0;
                for (int layer = 0; layer < g.nLayers; layer++)
                {
                    totalMineralNInRootZone += nh4[layer] + no3[layer];
                    depthFromSurface += g.dlayer[layer];
                    if (depthFromSurface >= g.rootDepth)
                        break;
                }
            }

            /// <summary>
            /// Sets the size of arrays (with nLayers)
            /// </summary>
            /// <remarks>
            /// This is used during initialisation and whenever the soil profile changes (thus not often at all)
            /// </remarks>
            /// <param name="nLayers">The number of layers</param>
            public void ResizeLayeredVariables(int nLayers)
            {
                // Mineral N
                Array.Resize(ref urea, nLayers);
                Array.Resize(ref nh4, nLayers);
                Array.Resize(ref no3, nLayers);
                Array.Resize(ref nh3, nLayers);
                Array.Resize(ref no2, nLayers);
                Array.Resize(ref TodaysInitialNO3, nLayers);
                Array.Resize(ref TodaysInitialNH4, nLayers);

                // Organic C and N
                for (int pool = 0; pool < 3; pool++)
                {
                    Array.Resize(ref fom_c[pool], nLayers);
                    Array.Resize(ref fom_n[pool], nLayers);
                }
                Array.Resize(ref biom_c, nLayers);
                Array.Resize(ref hum_c, nLayers);
                Array.Resize(ref inert_c, nLayers);
                Array.Resize(ref waterSoluble_c, nLayers);
                Array.Resize(ref biom_n, nLayers);
                Array.Resize(ref hum_n, nLayers);
                Array.Resize(ref inert_n, nLayers);

                // deltas
                Array.Resize(ref dlt_c_res_to_biom, nLayers);
                Array.Resize(ref dlt_c_res_to_hum, nLayers);
                Array.Resize(ref dlt_c_res_to_atm, nLayers);
                Array.Resize(ref dlt_res_nh4_min, nLayers);
                Array.Resize(ref dlt_res_no3_min, nLayers);

                Array.Resize(ref dlt_urea_hydrolysis, nLayers);
                Array.Resize(ref dlt_nitrification, nLayers);
                Array.Resize(ref dlt_n2o_nitrif, nLayers);
                Array.Resize(ref dlt_no3_dnit, nLayers);
                Array.Resize(ref dlt_n2o_dnit, nLayers);
                Array.Resize(ref dlt_codenitrification, nLayers);
                Array.Resize(ref dlt_n2o_codenit, nLayers);
                Array.Resize(ref dlt_n_fom_to_min, nLayers);
                Array.Resize(ref dlt_n_biom_to_min, nLayers);
                Array.Resize(ref dlt_n_hum_to_min, nLayers);
                Array.Resize(ref nh4_deficit_immob, nLayers);
                for (int pool = 0; pool < 3; pool++)
                {
                    Array.Resize(ref dlt_c_fom_to_biom[pool], nLayers);
                    Array.Resize(ref dlt_c_fom_to_hum[pool], nLayers);
                    Array.Resize(ref dlt_c_fom_to_atm[pool], nLayers);
                    Array.Resize(ref dlt_n_fom[pool], nLayers);
                }
                Array.Resize(ref dlt_c_biom_to_hum, nLayers);
                Array.Resize(ref dlt_c_biom_to_atm, nLayers);
                Array.Resize(ref dlt_c_hum_to_biom, nLayers);
                Array.Resize(ref dlt_c_hum_to_atm, nLayers);

                // additional variables
                Array.Resize(ref urea_flow, nLayers);
                Array.Resize(ref nh4_flow, nLayers);
                Array.Resize(ref no3_flow, nLayers);
                Array.Resize(ref urea_uptake, nLayers);
                Array.Resize(ref nh4_uptake, nLayers);
                Array.Resize(ref no3_uptake, nLayers);
                Array.Resize(ref urea_fertiliser, nLayers);
                Array.Resize(ref nh4_fertiliser, nLayers);
                Array.Resize(ref no3_fertiliser, nLayers);
                Array.Resize(ref urea_ChangedOther, nLayers);
                Array.Resize(ref nh4_ChangedOther, nLayers);
                Array.Resize(ref no3_ChangedOther, nLayers);
            }

            /// <summary>
            /// Clear (zero out) the values of variables storing deltas
            /// </summary>
            /// <remarks>
            /// This is used to zero out the variables that need reseting every day, those that are not necessarily computed everyday
            /// </remarks>
            public void ClearDeltaVariables()
            {
                // miscelaneous
                Array.Clear(g.inhibitionFactor_Nitrification, 0, g.inhibitionFactor_Nitrification.Length);
                dlt_n_loss_in_sed = 0.0;
                dlt_c_loss_in_sed = 0.0;

                // variables to report changes by other modules after partitioning amongst patches
                Array.Clear(urea_flow, 0, g.nLayers);
                Array.Clear(nh4_flow, 0, g.nLayers);
                Array.Clear(no3_flow, 0, g.nLayers);
                Array.Clear(urea_uptake, 0, g.nLayers);
                Array.Clear(nh4_uptake, 0, g.nLayers);
                Array.Clear(no3_uptake, 0, g.nLayers);
                Array.Clear(urea_fertiliser, 0, g.nLayers);
                Array.Clear(nh4_fertiliser, 0, g.nLayers);
                Array.Clear(no3_fertiliser, 0, g.nLayers);
                Array.Clear(urea_ChangedOther, 0, g.nLayers);
                Array.Clear(nh4_ChangedOther, 0, g.nLayers);
                Array.Clear(no3_ChangedOther, 0, g.nLayers);
            }

            /// <summary>
            /// Store today's initial N amounts
            /// </summary>
            public void StoreStatus()
            {
                TodaysInitialN = g.SumDoubleArray(nit_tot);
                TodaysInitialC = g.SumDoubleArray(carbon_tot);

                for (int layer = 0; layer < g.nLayers; layer++)
                {
                    // store these values so they may be used to compute daily deltas
                    TodaysInitialNH4[layer] = nh4[layer];
                    TodaysInitialNO3[layer] = no3[layer];
                }
            }

            /// <summary>
            /// Gather the information about actual residue decomposition, to be sent back to surface OM
            /// </summary>
            /// <remarks>
            /// Currently P is not being computed by SoilNitrogen, so the corresponding variables are set to zero here 
            /// </remarks>
            private void PackActualResidueDecomposition()
            {
                soilp_dlt_org_p = new double[g.nLayers];
                double soilp_cpr = MathUtilities.Divide(g.SumDoubleArray(g.pot_p_decomp), g.SumDoubleArray(g.pot_c_decomp), 0.0);
                SurfOMActualDecomposition = new SurfaceOrganicMatterDecompType();
                Array.Resize(ref SurfOMActualDecomposition.Pool, g.nResidues);

                for (int residue = 0; residue < g.nResidues; residue++)
                {
                    double c_summed = g.SumDoubleArray(dlt_c_decomp[residue]);
                    if (Math.Abs(c_summed) < g.epsilon)
                        c_summed = 0.0;
                    double n_summed = g.SumDoubleArray(dlt_n_decomp[residue]);
                    if (Math.Abs(n_summed) < g.epsilon)
                        n_summed = 0.0;

                    // pack up the structure to return decompositions to SurfaceOrganicMatter
                    SurfOMActualDecomposition.Pool[residue] = new SurfaceOrganicMatterDecompPoolType();
                    SurfOMActualDecomposition.Pool[residue].FOM = new FOMType();
                    SurfOMActualDecomposition.Pool[residue].Name = g.residueName[residue];
                    SurfOMActualDecomposition.Pool[residue].OrganicMatterType = g.residueType[residue];
                    SurfOMActualDecomposition.Pool[residue].FOM.amount = 0.0F;
                    SurfOMActualDecomposition.Pool[residue].FOM.C = c_summed;
                    SurfOMActualDecomposition.Pool[residue].FOM.N = n_summed;
                    SurfOMActualDecomposition.Pool[residue].FOM.P = 0.0F;
                    SurfOMActualDecomposition.Pool[residue].FOM.AshAlk = 0.0F;
                    // Note: The values for 'amount', 'P', and 'AshAlk' will not be collected by SurfaceOrganicMatter, so send zero as default.
                }

                // dsg 131004  calculate the old dlt_org_p (from the old Decomposed event sent by residue2) for getting by soilp
                double act_c_decomp = 0.0;
                double tot_pot_c_decomp = g.SumDoubleArray(g.pot_c_decomp);
                double tot_pot_p_decomp = g.SumDoubleArray(g.pot_p_decomp);
                for (int layer = 0; layer < g.nLayers; layer++)
                {
                    act_c_decomp = dlt_c_res_to_biom[layer] + dlt_c_res_to_hum[layer] + dlt_c_res_to_atm[layer];
                    soilp_dlt_org_p[layer] = tot_pot_p_decomp * MathUtilities.Divide(act_c_decomp, tot_pot_c_decomp, 0.0);
                }
            }

            /// <summary>
            /// Check that the values of variables are ok
            /// </summary>
            public void CheckVariables()
            {
                for (int layer = 0; layer < g.nLayers; layer++)
                {
                    // 1. Organic forms
                    for (int pool = 0; pool < 3; pool++)
                    {
                        g.CheckNegativeValues(ref fom_c[pool][layer], layer, "fom_c[" + (pool + 1).ToString() + "]", "Patch[" + PatchName + "].EvaluateProcesses");
                        g.CheckNegativeValues(ref fom_n[pool][layer], layer, "fom_n[" + (pool + 1).ToString() + "]", "Patch[" + PatchName + "].EvaluateProcesses");
                    }
                    g.CheckNegativeValues(ref biom_c[layer], layer, "biom_c", "Patch[" + PatchName + "].EvaluateProcesses");
                    g.CheckNegativeValues(ref hum_c[layer], layer, "hum_c", "Patch[" + PatchName + "].EvaluateProcesses");
                    g.CheckNegativeValues(ref biom_n[layer], layer, "biom_n", "Patch[" + PatchName + "].EvaluateProcesses");
                    g.CheckNegativeValues(ref hum_n[layer], layer, "hum_n", "Patch[" + PatchName + "].EvaluateProcesses");

                    // 2. Mineral forms
                    g.CheckNegativeValues(ref urea[layer], layer, "urea", "Patch[" + PatchName + "].EvaluateProcesses");
                    g.CheckNegativeValues(ref nh4[layer], layer, "nh4", "Patch[" + PatchName + "].EvaluateProcesses");
                    g.CheckNegativeValues(ref no3[layer], layer, "no3", "Patch[" + PatchName + "].EvaluateProcesses");
                }
            }

            #endregion

            #region Auxiliary functions

            #endregion
        }
    }
}
