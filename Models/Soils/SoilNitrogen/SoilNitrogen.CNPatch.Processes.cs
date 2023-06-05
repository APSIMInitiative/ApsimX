using System;
using APSIM.Shared.Utilities;

namespace Models.Soils
{

    /// <remarks>
    /// This partial class contains part of the SoilCN patch, with most of the processes of SoilNitrogen
    /// </remarks>
    public partial class SoilNitrogen
    {
        /// <remarks>
        /// This partial class contains most of the soil processes of SoilNitrogen
        /// </remarks>
        public partial class soilCNPatch
        {

            #region >>  The soil C and N processes

            #region »   OM processes

            /// <summary>
            /// Calculate rate of nitrogen mineralisation/immobilisation of surface residues
            /// </summary>
            /// <remarks>
            /// This will test to see whether adequate mineral nitrogen is available to sustain potential rate of decomposition of
            /// surface residues, which was somputed by SurfaceOM. It aslo calculates net rate of nitrogen mineralisation/immobilisation
            /// </remarks>
            public void DecomposeResidues()
            {
                // 1. clear some deltas
                Array.Clear(dlt_c_res_to_biom, 0, dlt_c_res_to_biom.Length);
                Array.Clear(dlt_c_res_to_hum, 0, dlt_c_res_to_hum.Length);
                Array.Clear(dlt_c_res_to_atm, 0, dlt_c_res_to_atm.Length);
                Array.Clear(dlt_res_nh4_min, 0, dlt_res_nh4_min.Length);
                Array.Clear(dlt_res_no3_min, 0, dlt_res_no3_min.Length);
                dlt_c_decomp = new double[g.nResidues][];
                dlt_n_decomp = new double[g.nResidues][];
                for (int residue = 0; residue < g.nResidues; residue++)
                {
                    dlt_c_decomp[residue] = new double[g.nLayers];
                    dlt_n_decomp[residue] = new double[g.nLayers];
                }

                // 2. get the amounts of C decomposed
                if (g.isPondActive)
                {
                    // There is a pond in the system, the POND module will decompose residues - not SoilNitrogen
                    //   the pond module computes the amounts of C added to the soil, here these are added to the top layer
                    //   with the C:N ratio of the respective SOM pools (Not sure how N balance is kept here, or whether it is)

                    dlt_c_res_to_biom[0] += g.pond_biom_C;   // humic material from breakdown of residues in pond
                    dlt_c_res_to_hum[0] += g.pond_hum_C;     // biom material from breakdown of residues in pond
                }
                else
                {
                    // There is no pond, decomposition of surface residues is done in tandem by SurfaceOM and SoilN
                    // check whether there is any potential residue decomposition
                    //if (g.SumDoubleArray(g.pot_c_decomp) > -g.epsilon)
                    //{
                    // Surface OM sent some potential decomposition, here we verify the C-N balance over the immobilisation layer

                    double[] no3_available = new double[g.nLayers];           // no3 available for mineralisation
                    double[] nh4_available = new double[g.nLayers];           // nh4 available for mineralisation
                    double[] dltC_into_biom = new double[g.nResidues];      // C mineralized converted to biomass
                    double[] dltC_into_hum = new double[g.nResidues];       // C mineralized converted to humus
                    int ImmobilisationLayer = g.getCumulativeIndex(g.ResiduesDecompDepth, g.dlayer);  // soil layer down to which soil N is available for decemposition

                    // 2.1. get the potential transfers to m. biomass and humic pools
                    for (int residue = 0; residue < g.nResidues; residue++)
                    {
                        dltC_into_biom[residue] = g.pot_c_decomp[residue] * (1.0 - g.ResiduesRespirationFactor) * g.ResiduesFractionIntoBiomass;
                        dltC_into_hum[residue] = g.pot_c_decomp[residue] * (1.0 - g.ResiduesRespirationFactor) * (1.0 - g.ResiduesFractionIntoBiomass);
                    }

                    // 2.2. test whether there is adequate N available to meet immobilisation demand

                    // 2.2.1. get the available mineral N in the soil close to surface (mineralisation depth)
                    double MineralNAvailable = 0.0;
                    double cumDepth = 0.0;
                    double[] fracLayer = new double[g.nLayers];
                    for (int layer = 0; layer <= ImmobilisationLayer; layer++)
                    {
                        fracLayer[layer] = Math.Min(1.0, MathUtilities.Divide(g.ResiduesDecompDepth - cumDepth, g.dlayer[layer], 0.0));
                        cumDepth += g.dlayer[layer];
                        no3_available[layer] = Math.Max(0.0, no3[layer]) * fracLayer[layer];
                        nh4_available[layer] = Math.Max(0.0, nh4[layer]) * fracLayer[layer];
                        MineralNAvailable += nh4_available[layer] + no3_available[layer];
                    }

                    // 2.2.2. total available N for this process
                    double NAvailable = MineralNAvailable + g.SumDoubleArray(g.pot_n_decomp);

                    // 2.2.3. potential N demanded for conversion of residues into soil OM
                    double NDemand = MathUtilities.Divide(g.SumDoubleArray(dltC_into_biom), g.MBiomassCNr, 0.0);

                    for (int layer = 0; layer <= ImmobilisationLayer; layer++)
                    {
                        double fraction = MathUtilities.Divide(g.dlayer[layer] * fracLayer[layer], g.ResiduesDecompDepth, 0.0);
                        NDemand += MathUtilities.Divide(g.SumDoubleArray(dltC_into_hum) * fraction, g.HumusCNr[layer], 0.0);
                    }

                    // 2.2.4. factor to reduce mineralisation rate, if N available is insufficient
                    double ReductionFactor = 1.0;
                    if (NDemand > NAvailable)
                    {
                        ReductionFactor = MathUtilities.Divide(MineralNAvailable, NDemand - g.SumDoubleArray(g.pot_n_decomp), 0.0);
                        ReductionFactor = Math.Max(0.0, Math.Min(1.0, ReductionFactor));
                    }

                    // 2.3. partition the additions of C and N to layers
                    double dltN_decomp_tot = 0.0;
                    double dltC_into_atm = 0.0;
                    double fractionIntoLayer = 1.0;
                    for (int layer = 0; layer <= ImmobilisationLayer; layer++)
                    {
                        // 2.3.1. fraction of mineralised stuff going in this layer
                        fractionIntoLayer = MathUtilities.Divide(g.dlayer[layer] * fracLayer[layer], g.ResiduesDecompDepth, 0.0);

                        // 2.3.2. adjust C and N amounts for each residue and add to soil OM pools
                        for (int residue = 0; residue < g.nResidues; residue++)
                        {
                            dlt_c_decomp[residue][layer] = g.pot_c_decomp[residue] * ReductionFactor * fractionIntoLayer;
                            dlt_n_decomp[residue][layer] = g.pot_n_decomp[residue] * ReductionFactor * fractionIntoLayer;
                            dltN_decomp_tot += dlt_n_decomp[residue][layer];

                            dlt_c_res_to_biom[layer] += dltC_into_biom[residue] * ReductionFactor * fractionIntoLayer;
                            dlt_c_res_to_hum[layer] += dltC_into_hum[residue] * ReductionFactor * fractionIntoLayer;
                            dltC_into_atm = g.pot_c_decomp[residue] * Math.Max(0.0, g.ResiduesRespirationFactor);
                            dlt_c_res_to_atm[layer] += dltC_into_atm * ReductionFactor * fractionIntoLayer;
                        }
                    }

                    // 2.4. get the net N mineralised/immobilised (hg/ha) - positive means mineralisation, negative is immobilisation
                    double dlt_MineralN = dltN_decomp_tot - NDemand * ReductionFactor;

                    // 2.5. partition mineralised/immobilised N into mineral forms
                    if (dlt_MineralN > g.epsilon)
                    {
                        // 2.5a. we have mineralisation into NH4, distribute it over the layers
                        for (int layer = 0; layer <= ImmobilisationLayer; layer++)
                        {
                            fractionIntoLayer = MathUtilities.Divide(g.dlayer[layer] * fracLayer[layer], g.ResiduesDecompDepth, 0.0);
                            dlt_res_nh4_min[layer] = dlt_MineralN * fractionIntoLayer;
                        }
                    }
                    else if (dlt_MineralN < -g.epsilon)
                    {
                        // 2.5b. we have immobilisation, soak up any N required from NH4 then NO3
                        for (int layer = 0; layer <= ImmobilisationLayer; layer++)
                        {
                            dlt_res_nh4_min[layer] = -Math.Min(nh4_available[layer], Math.Abs(dlt_MineralN));
                            dlt_MineralN -= dlt_res_nh4_min[layer];
                        }

                        for (int layer = 0; layer <= ImmobilisationLayer; layer++)
                        {
                            dlt_res_no3_min[layer] = -Math.Min(no3_available[layer], Math.Abs(dlt_MineralN));
                            dlt_MineralN -= dlt_res_no3_min[layer];
                        }

                        // check that there is no remaining immobilisation demand
                        if (Math.Abs(dlt_MineralN) >= g.epsilon)
                            throw new Exception("Value for remaining immobilisation is out of range");
                    }
                    // else, there is no net N transformation
                    //}
                    // else, there is no residue decomposition

                    // 3. Pack information to send back to surfaceOM
                    PackActualResidueDecomposition();
                }

                // 4. Update variables - add/remove C and N in appropriate pools
                if (g.SumDoubleArray(dlt_c_res_to_biom) + g.SumDoubleArray(dlt_c_res_to_hum) >= g.epsilon)
                {
                    for (int layer = 0; layer < g.nLayers; layer++)
                    {
                        // organic C pools
                        biom_c[layer] += dlt_c_res_to_biom[layer];
                        hum_c[layer] += dlt_c_res_to_hum[layer];

                        // organic N balance
                        hum_n[layer] = MathUtilities.Divide(hum_c[layer], g.HumusCNr[layer], 0.0);
                        biom_n[layer] = MathUtilities.Divide(biom_c[layer], g.MBiomassCNr, 0.0);

                        // soil mineral N
                        nh4[layer] += dlt_res_nh4_min[layer];
                        no3[layer] += dlt_res_no3_min[layer];
                    }
                }
                // else, no changes
            }

            /// <summary>
            /// Check and compute the mineralisation/immobilisation processes for each soil OM
            /// </summary>
            public void ConvertSoilOM()
            {
                int poolsComputed = 0;      // number of SOM/FOM pools actually considered

                // 1. get the mineralisation of humic pool
                if (g.SumDoubleArray(hum_c) >= g.epsilon)
                {
                    poolsComputed += 1;
                    for (int layer = 0; layer < g.nLayers; layer++)
                        MineraliseHumus(layer);
                }
                else
                {
                    Array.Clear(dlt_c_hum_to_biom, 0, g.nLayers);
                    Array.Clear(dlt_c_hum_to_atm, 0, g.nLayers);
                    Array.Clear(dlt_n_hum_to_min, 0, g.nLayers);
                }

                // 2. get the mineralisation of m. biomass pool
                if (g.SumDoubleArray(biom_c) >= g.epsilon)
                {
                    poolsComputed += 1;
                    for (int layer = 0; layer < g.nLayers; layer++)
                        MineraliseMBiomass(layer);
                }
                else
                {
                    Array.Clear(dlt_c_biom_to_hum, 0, g.nLayers);
                    Array.Clear(dlt_c_biom_to_atm, 0, g.nLayers);
                    Array.Clear(dlt_n_biom_to_min, 0, g.nLayers);
                }

                // 3. get the decomposition of FOM pools
                if ((g.SumDoubleArray(fom_c[0]) + g.SumDoubleArray(fom_c[1]) + g.SumDoubleArray(fom_c[2])) >= g.epsilon)
                {
                    poolsComputed += 1;
                    for (int layer = 0; layer < g.nLayers; layer++)
                        DecomposeFOM(layer);
                }
                else
                {
                    for (int pool = 0; pool < 3; pool++)
                    {
                        Array.Clear(dlt_c_fom_to_biom[pool], 0, g.nLayers);
                        Array.Clear(dlt_c_fom_to_hum[pool], 0, g.nLayers);
                        Array.Clear(dlt_c_fom_to_atm[pool], 0, g.nLayers);
                        Array.Clear(dlt_n_fom[pool], 0, g.nLayers);
                    }
                    Array.Clear(dlt_n_fom_to_min, 0, g.nLayers);
                }

                // 4. make changes effective
                if (poolsComputed > 0)
                {
                    // some of the OM pools has potentially changed
                    for (int layer = 0; layer < g.nLayers; layer++)
                    {
                        // 4.1. update SOM pools
                        biom_c[layer] += dlt_c_hum_to_biom[layer] - dlt_c_biom_to_hum[layer] - dlt_c_biom_to_atm[layer] +
                                       dlt_c_fom_to_biom[0][layer] + dlt_c_fom_to_biom[1][layer] + dlt_c_fom_to_biom[2][layer];
                        hum_c[layer] += dlt_c_biom_to_hum[layer] - dlt_c_hum_to_biom[layer] - dlt_c_hum_to_atm[layer] +
                                       dlt_c_fom_to_hum[0][layer] + dlt_c_fom_to_hum[1][layer] + dlt_c_fom_to_hum[2][layer];

                        biom_n[layer] = MathUtilities.Divide(biom_c[layer], g.MBiomassCNr, 0.0);
                        hum_n[layer] = MathUtilities.Divide(hum_c[layer], g.HumusCNr[layer], 0.0);

                        // 4.2. update FOM pools
                        for (int pool = 0; pool < 3; pool++)
                        {
                            fom_c[pool][layer] -= (dlt_c_fom_to_biom[pool][layer] + dlt_c_fom_to_hum[pool][layer] + dlt_c_fom_to_atm[pool][layer]);
                            fom_n[pool][layer] -= dlt_n_fom[pool][layer];
                        }

                        // 4.3. update soil mineral N after mineralisation/immobilisation
                        // starts with nh4
                        nh4[layer] += dlt_n_hum_to_min[layer] + dlt_n_biom_to_min[layer] + dlt_n_fom_to_min[layer];
                        if (nh4[layer] < -g.epsilon)
                        {
                            nh4_deficit_immob[layer] = -nh4[layer];
                            nh4[layer] = 0.0;
                        }
                        else
                            nh4_deficit_immob[layer] = 0.0;

                        // now change no3
                        no3[layer] -= nh4_deficit_immob[layer];
                        if (no3[layer] < -g.epsilon)
                            throw new Exception("N immobilisation resulted in mineral N in layer(" + (layer + 1).ToString() + ") to go below minimum");
                        // note: tests for adequate mineral N for immobilisation have been made so this no3 should not go below no3_min
                    }
                }
            }

            /// <summary>
            /// Calculate the transformations of the the soil humic pool, mineralisation (+ve) or immobilisation (-ve)
            /// </summary>
            /// <remarks>
            /// It is assumed that the inert_C component of the humic pool is not subject to mineralisation
            /// some constants have different values when there's a pond, as anaerobic conditions dominate
            /// </remarks>
            /// <param name="layer">the node number representing soil layer for which calculations will be made</param>
            public void MineraliseHumus(int layer)
            {
                // index = 0 for aerobic conditions, 1 for anaerobic conditions
                int index = (g.isPondActive) ? 1 : 0;

                // get the potential mineralisation
                double pot_miner = (hum_c[layer] - inert_c[layer]) * g.AHumusTurnOverRate[index];

                if (pot_miner >= g.epsilon)
                {
                    // get the soil temperature factor
                    double stf = SoilTempFactor(layer, index, g.SOMMiner_TemperatureFactorData);

                    // get the soil water factor
                    double swf = SoilMoistFactor(layer, index, g.SOMMiner_MoistureFactorData);

                    // compute the mineralisation amounts of C and N from the humic pool
                    double dlt_c_miner = pot_miner * stf * swf;
                    double dlt_n_miner = MathUtilities.Divide(dlt_c_miner, g.HumusCNr[layer], 0.0);

                    // distribute the mineralised N and C
                    dlt_c_hum_to_biom[layer] = dlt_c_miner * (1.0 - g.AHumusRespirationFactor);
                    dlt_c_hum_to_atm[layer] = dlt_c_miner * g.AHumusRespirationFactor;

                    // calculate net mineralisation
                    dlt_n_hum_to_min[layer] = dlt_n_miner - MathUtilities.Divide(dlt_c_hum_to_biom[layer], g.MBiomassCNr, 0.0);
                }
                else
                {
                    // there is no mineralisation - only reset the delta variables
                    dlt_c_hum_to_biom[layer] = 0.0;
                    dlt_c_hum_to_atm[layer] = 0.0;
                    dlt_n_hum_to_min[layer] = 0.0;
                }
            }

            /// <summary>
            /// Calculate the transformations of the soil biomass pool, mineralisation (+ve) or immobilisation (-ve)
            /// </summary>
            /// <param name="layer">the node number representing soil layer for which calculations will be made</param>
            public void MineraliseMBiomass(int layer)
            {
                // index = 0 for aerobic and 0 for anaerobic conditions
                int index = (g.isPondActive) ? 1 : 0;

                // get the potential mineralisation
                double pot_miner = biom_c[layer] * g.MBiomassTurnOverRate[index];

                if (pot_miner >= g.epsilon)
                {
                    // get the soil temperature factor
                    double stf = SoilTempFactor(layer, index, g.SOMMiner_TemperatureFactorData);

                    // get the soil water factor
                    double swf = SoilMoistFactor(layer, index, g.SOMMiner_MoistureFactorData);

                    // compute the mineralisation amounts of C and N from the m. biomass pool
                    double dlt_c_miner = pot_miner * stf * swf;
                    double dlt_n_miner = MathUtilities.Divide(dlt_c_miner, g.MBiomassCNr, 0.0);

                    // distribute the mineralised N and C
                    dlt_c_biom_to_hum[layer] = dlt_c_miner * (1.0 - g.MBiomassRespirationFactor) * (1.0 - g.MBiomassFractionIntoBiomass);
                    dlt_c_biom_to_atm[layer] = dlt_c_miner * g.MBiomassRespirationFactor;

                    // calculate net mineralisation
                    dlt_n_biom_to_min[layer] = dlt_n_miner - MathUtilities.Divide(dlt_c_biom_to_hum[layer], g.HumusCNr[layer], 0.0) -
                                       MathUtilities.Divide((dlt_c_miner - dlt_c_biom_to_atm[layer] - dlt_c_biom_to_hum[layer]), g.MBiomassCNr, 0.0);
                }
                else
                {
                    // there is no mineralisation - only reset the delta variables
                    dlt_c_biom_to_hum[layer] = 0.0;
                    dlt_c_biom_to_atm[layer] = 0.0;
                    dlt_n_biom_to_min[layer] = 0.0;
                }
            }

            /// <summary>
            /// Calculate the decomposition of the soil Fresh OM, mineralisation (+ve) or immobilisation (-ve)
            /// </summary>
            /// <remarks>
            /// - parameters are given in pairs, for aerobic and anaerobic conditions (with pond)
            /// </remarks>
            /// <param name="layer">the node number representing soil layer for which calculations will be made</param>
            public void DecomposeFOM(int layer)
            {
                // index = 0 for aerobic and 1 for anaerobic conditions
                int index = (g.isPondActive) ? 1 : 0;

                // get total available mineral N (kg/ha)
                double mineralN_available = Math.Max(0.0, no3[layer] + nh4[layer]);

                // calculate gross amount of C & N released due to mineralisation of the fresh organic matter.
                if ((fom_c[0][layer] + fom_c[1][layer] + fom_c[2][layer]) >= g.epsilon)
                {
                    double dlt_n_fom_gross_miner = 0.0; // amount of fresh organic N mineralized across fpools (kg/ha)
                    double dlt_c_fom_gross_miner = 0.0; // total C mineralized (kg/ha) summed across fpools
                    double[] dlt_n_gross_decomp = new double[3]; // amount of fresh organic N mineralized in each pool (kg/ha)
                    double[] dlt_c_gross_decomp = new double[3]; // amount of C mineralized (kg/ha) from each pool

                    // get the soil temperature factor
                    double stf = SoilTempFactor(layer, index, g.FOMDecomp_TemperatureFactorData);

                    // get the soil water factor
                    double swf = SoilMoistFactor(layer, index, g.FOMDecomp_MoistureFactorData);

                    // ratio of C in fresh OM to N available for decay
                    double cnr = MathUtilities.Divide(fom_c[0][layer] + fom_c[1][layer] + fom_c[2][layer],
                                                    fom_n[0][layer] + fom_n[1][layer] + fom_n[2][layer] + mineralN_available, 0.0);

                    // calculate the C:N ratio factor
                    double cnrf = CNratioFactor(layer, index, g.FOMDecomp_CNThreshold, g.FOMDecomp_CNCoefficient);

                    // C:N ratio of fom
                    double fom_cn = MathUtilities.Divide(fom_c[0][layer] + fom_c[1][layer] + fom_c[2][layer],
                                                    fom_n[0][layer] + fom_n[1][layer] + fom_n[2][layer], 0.0);

                    // get the decomposition of carbohydrate-like, cellulose-like and lignin-like fractions (fom pools) in turn.
                    for (int pool = 0; pool < 3; pool++)
                    {
                        // get the max decomposition rate for each fpool
                        double drate = FOMTurnOverRate(pool)[index] * cnrf * stf * swf;

                        // calculate the gross amount of fresh organic carbon mineralised (kg/ha)
                        dlt_c_gross_decomp[pool] = drate * fom_c[pool][layer];

                        // calculate the gross amount of N released from fresh organic matter (kg/ha)
                        dlt_n_gross_decomp[pool] = drate * fom_n[pool][layer];

                        // sum up values
                        dlt_c_fom_gross_miner += dlt_c_gross_decomp[pool];
                        dlt_n_fom_gross_miner += dlt_n_gross_decomp[pool];
                    }

                    // calculate potential transfers of C mineralised to biomass
                    double dlt_c_biom_tot = dlt_c_fom_gross_miner * (1.0 - g.FOMRespirationFactor) * g.FOMFractionIntoBiomass;

                    // calculate potential transfers of C mineralised to humus
                    double dlt_c_hum_tot = dlt_c_fom_gross_miner * (1.0 - g.FOMRespirationFactor) * (1.0 - g.FOMFractionIntoBiomass);

                    // test whether there is adequate N available to meet immobilisation demand
                    double n_demand = MathUtilities.Divide(dlt_c_biom_tot, g.MBiomassCNr, 0.0) +
                                      MathUtilities.Divide(dlt_c_hum_tot, g.HumusCNr[layer], 0.0);
                    double n_available = mineralN_available + dlt_n_fom_gross_miner;

                    // factor to reduce mineralisation rates if insufficient N to meet immobilisation demand
                    double reductionFactor = 1.0;
                    if (n_demand > n_available)
                        reductionFactor = Math.Max(0.0, Math.Min(1.0, MathUtilities.Divide(mineralN_available, n_demand - dlt_n_fom_gross_miner, 0.0)));

                    // now adjust carbon transformations etc. and similarly for N pools
                    for (int fractn = 0; fractn < 3; fractn++)
                    {
                        double dlt_c_act_decomp = dlt_c_gross_decomp[fractn] * reductionFactor;
                        dlt_c_fom_to_biom[fractn][layer] = dlt_c_act_decomp * (1.0 - g.FOMRespirationFactor) * g.FOMFractionIntoBiomass;
                        dlt_c_fom_to_hum[fractn][layer] = dlt_c_act_decomp * (1.0 - g.FOMRespirationFactor) * (1.0 - g.FOMFractionIntoBiomass);
                        dlt_c_fom_to_atm[fractn][layer] = dlt_c_act_decomp * g.FOMRespirationFactor;
                        dlt_n_fom[fractn][layer] = dlt_n_gross_decomp[fractn] * reductionFactor;
                    }
                    dlt_n_fom_to_min[layer] = (dlt_n_fom_gross_miner - n_demand) * reductionFactor;
                }
                else
                {
                    // tehre is no decomposition - only reset the delta variables
                    for (int fractn = 0; fractn < 3; fractn++)
                    {
                        dlt_c_fom_to_biom[fractn][layer] = 0.0;
                        dlt_c_fom_to_hum[fractn][layer] = 0.0;
                        dlt_c_fom_to_atm[fractn][layer] = 0.0;
                        dlt_n_fom[fractn][layer] = 0.0;
                    }
                    dlt_n_fom_to_min[layer] = 0.0;
                }
            }

            #endregion OM processes

            #region »   N processes

            /// <summary>
            /// Check and compute the amount of urea converted to NH4 via hydrolysis
            /// </summary>
            public void ConvertUrea()
            {
                if (g.SumDoubleArray(urea) >= g.epsilon)
                {
                    // there is some urea in the soil
                    for (int layer = 0; layer < g.nLayers; layer++)
                    {
                        // get amount hydrolysed
                        dlt_urea_hydrolysis[layer] = UreaHydrolysis(layer);
                        // update soil mineral N
                        urea[layer] -= dlt_urea_hydrolysis[layer];
                        nh4[layer] += dlt_urea_hydrolysis[layer];
                    }
                }
                else
                    Array.Clear(dlt_urea_hydrolysis, 0, g.nLayers);
            }

            /// <summary>
            /// Check and compute the amount of NH4 converted to NO3 via nitrification
            /// </summary>
            public void ConvertAmmonium()
            {
                if (g.SumDoubleArray(nh4) >= g.epsilon)
                {
                    // there is some ammonium in the soil
                    for (int layer = 0; layer < g.nLayers; layer++)
                    {
                        if (g.usingNewNitrification)
                        {
                            // get the N converted during nitritation
                            double dlt_nitritation = Nitritation(layer);

                            // get the N2O loss during ammonia oxidation
                            dlt_n2o_nitrif[layer] = N2OProducedDuringNitritation(dlt_nitritation, layer);

                            // update amount of NO2
                            no2[layer] += dlt_nitritation - dlt_n2o_nitrif[layer];

                            // get the amount of N codenitrified
                            dlt_codenitrification[layer] = Codenitrification(layer);

                            // get the N2 fraction on denitrification
                            double fractionN2 = CodenitrificationN2Fraction(layer);

                            // get the N2O produced during codenitrfication
                            dlt_n2o_codenit[layer] = dlt_codenitrification[layer] * (1.0 - fractionN2);

                            // update N pools
                            nh3[layer] -= 0.5 * dlt_codenitrification[layer];
                            no2[layer] -= 0.5 * dlt_codenitrification[layer];

                            // get the N converted during nitratation
                            dlt_nitrification[layer] = Nitratation(layer);

                            // update soil mineral N
                            nh4[layer] -= dlt_nitritation;
                            no3[layer] += dlt_nitrification[layer];
                        }
                        else
                        {
                            // get the nitrification of ammonium-N
                            dlt_nitrification[layer] = Nitrification(layer);

                            // N2O loss to atmosphere during nitrification
                            dlt_n2o_nitrif[layer] = N2OProducedDuringNitrification(layer);

                            // update soil mineral N
                            nh4[layer] -= dlt_nitrification[layer];
                            no3[layer] += dlt_nitrification[layer] - dlt_n2o_nitrif[layer];
                        }
                    }
                }
                else
                {
                    Array.Clear(dlt_nitrification, 0, g.nLayers);
                    Array.Clear(dlt_n2o_nitrif, 0, g.nLayers);
                }
            }

            /// <summary>
            /// Check and compute the amount of NO3 converted to gas via dinitrification
            /// </summary>
            public void ConvertNitrate()
            {
                if (g.SumDoubleArray(no3) >= g.epsilon)
                {
                    // there is some nitrate in the soil
                    for (int layer = 0; layer < g.nLayers; layer++)
                    {
                        // get the denitrification amount
                        dlt_no3_dnit[layer] = Denitrification(layer);
                        // N2O loss to atmosphere due to denitrification
                        double N2N2O = Denitrification_Nratio(layer);
                        dlt_n2o_dnit[layer] = dlt_no3_dnit[layer] / (N2N2O + 1.0);
                        // update soil mineral N
                        no3[layer] -= dlt_no3_dnit[layer];
                    }
                }
                else
                {
                    Array.Clear(dlt_no3_dnit, 0, g.nLayers);
                    Array.Clear(dlt_n2o_dnit, 0, g.nLayers);
                }
            }

            /// <summary>
            /// Calculate the amount of urea converted to NH4 via hydrolysis (kgN/ha)
            /// </summary>
            /// <remarks>
            /// - very small amounts of urea are hydrolysed promptly, regardless the hydrolysis settings
            /// - parameters are given in pairs, for aerobic and anaerobic conditions (with pond)
            /// </remarks>
            /// <param name="layer">the node number representing soil layer for which calculations will be made</param>
            /// <returns>delta N coverted from urea into NH4</returns>
            private double UreaHydrolysis(int layer)
            {
                double result;

                // index = 0 for aerobic and 1 for anaerobic conditions
                int index = (g.isPondActive) ? 1 : 0;

                if (urea[layer] < g.epsilon)
                {
                    // urea amount is too small, all will be hydrolysed
                    result = urea[layer];
                }
                else
                {
                    // potential fraction of urea being hydrolysed
                    double totalC = (hum_c[layer] + biom_c[layer]) * g.convFactor[layer] / 10000;  // (100/1000000) = convert to ppm and then to %
                    double pot_hydrol_rate = g.UreaHydrol_parmA + g.UreaHydrol_parmB * totalC +
                             g.UreaHydrol_parmC * g.ph[layer] + g.UreaHydrol_parmD * totalC * g.ph[layer];
                    pot_hydrol_rate = Math.Max(g.UreaHydrol_MinRate, Math.Min(1.0, pot_hydrol_rate));

                    if (pot_hydrol_rate >= g.epsilon)
                    {
                        // get the soil temperature factor
                        double stf = SoilTempFactor(layer, index, g.UreaHydrolysis_TemperatureFactorData);

                        // get the soil water factor
                        double swf = SoilMoistFactor(layer, index, g.UreaHydrolysis_MoistureFactorData);

                        // actual amount hydrolysed;
                        result = Math.Max(0.0, Math.Min(urea[layer], pot_hydrol_rate * urea[layer] * Math.Min(swf, stf)));
                    }
                    else
                        result = 0.0;
                }

                return result;
            }

            /// <summary>
            /// Calculate the amount of NH4 converted to NO3 via nitrification
            /// </summary>
            /// <remarks>
            /// - This routine is much simplified from original CERES code
            /// - pH effect on nitrification is not used as pH is not simulated
            /// - parameters are given in pairs, for aerobic and anaerobic conditions (with pond)
            /// </remarks>
            /// <param name="layer">the node number representing soil layer for which calculations will be made</param>
            /// <returns>delta N coverted from NH4 into NO3</returns>
            private double Nitrification(int layer)
            {
                double result;

                // index = 0 for aerobic and 1 for anaerobic conditions
                int index = (g.isPondActive) ? 1 : 0;

                // get the potential rate of nitrification for layer
                double nh4ppm = nh4[layer] * g.convFactor[layer];
                double pot_nitrif_rate_ppm = MathUtilities.Divide(g.NitrificationMaxPotential * nh4ppm, nh4ppm + g.NitrificationNH4ForHalfRate, 0.0);

                if (pot_nitrif_rate_ppm >= g.epsilon)
                {
                    // get the soil temperature factor
                    double stf = SoilTempFactor(layer, index, g.Nitrification_TemperatureFactorData);

                    // get the soil water factor
                    double swf = SoilMoistFactor(layer, index, g.Nitrification_MoistureFactorData);

                    // get the soil pH factor
                    double phf = SoilpHFactor(layer, index, g.Nitrification_pHFactorData);

                    // get most limiting factor
                    double pni = Math.Min(swf, Math.Min(stf, phf));

                    // get the actual rate of nitrification
                    double nitrif_rate = pot_nitrif_rate_ppm * pni * Math.Max(0.0, 1.0 - g.inhibitionFactor_Nitrification[layer]);

                    // check that the nitrification rate is not greater than nh4 content
                    nitrif_rate = Math.Min(nitrif_rate, nh4ppm);

                    result = MathUtilities.Divide(nitrif_rate, g.convFactor[layer], 0.0);   // convert back to kg/ha
                }
                else
                    result = 0.0;

                return result;
            }

            /// <summary>
            /// Calculate the amount of N2O produced during nitrification
            /// </summary>
            /// <param name="layer">the soil layer index for which calculations will be made</param>
            /// <returns>delta N coverted into N2O during nitrification</returns>
            private double N2OProducedDuringNitrification(int layer)
            {
                double result = dlt_nitrification[layer] * g.Nitrification_DenitLossFactor;
                return result;
            }

            /// <summary>
            /// Calculate amount of NO3 transformed via denitrification
            /// </summary>
            /// <remarks>
            /// - parameters are given in pairs, for aerobic and anaerobic conditions (with pond)
            /// </remarks>
            /// <param name="layer">the soil layer index for which calculations will be made</param>
            /// <returns>delta N coverted from NO3 into gaseous forms</returns>
            private double Denitrification(int layer)
            {
                // Notes:
                //   Denitrification will happend whenever:
                //       - the soil water in the layer > the drained upper limit (Godwin et al., 1984),
                //       - the NO3 nitrogen concentration > 1 mg N/kg soil,
                //       - the soil temperature >= a minimum temperature.

                // + Assumptions
                //   That there is a root system present.  Rolston et al. say that the denitrification rate coeffficient (dnit_rate_coeff) of non-cropped
                //     plots was 0.000168 and for cropped plots 3.6 times more (dnit_rate_coeff = 0.0006). The larger rate coefficient was required
                //     to account for the effects of the root system in consuming oxygen and in adding soluble organic C to the soil.

                //+  Notes
                //     Reference: Rolston DE, Rao PSC, Davidson JM, Jessup RE (1984). "Simulation of denitrification losses of Nitrate fertiliser applied
                //      to uncropped, cropped, and manure-amended field plots". Soil Science Vol 137, No 4, pp 270-278.
                //
                //     Reference for Carbon availability factor: Reddy KR, Khaleel R, Overcash MR (). "Carbon transformations in land areas receiving
                //      organic wastes in relation to nonpoint source pollution: A conceptual model".  J.Environ. Qual. 9:434-442.

                double result;
                int index = 0; // denitrification calcs are not different whether there is pond or not. use 0 as default

                // get available carbon from soil organic pools (ppm)
                double totalC = (hum_c[layer] + fom_c[0][layer] + fom_c[1][layer] + fom_c[2][layer]) * g.convFactor[layer];
                //// Note: Ceres wheat has active_c = 0.4* fom_C_pool1 + 0.0031 * 0.58 * hum_C_conc + 24.5
                //// Suggest use new definition, but this need test and probably reparameterisation
                //// totalC = (hum_c[layer] - inert_c[layer] + biom_c[layer] + fom_c[0][layer]) * g.convFactor[layer];

                waterSoluble_c[layer] = g.actC_parmA + g.actC_parmB * totalC;
                //// Note: this calculation would be better using a power function, ensuring zero if no C is available
                //// waterSoluble_c[layer] = g.actCExp_parmA * Math.Pow(totalC, g.actCExp_parmB);

                // get the potential denitrification rate
                double pot_denit_rate = g.DenitrificationRateCoefficient * waterSoluble_c[layer];

                if (pot_denit_rate >= g.epsilon)
                {
                    // get the soil temperature factor
                    double stf = SoilTempFactor(layer, index, g.Denitrification_TemperatureFactorData);

                    // get the soil water factor
                    double swf = SoilMoistFactor(layer, index, g.Denitrification_MoistureFactorData);

                    // calculate denitrification rate  - kg/ha
                    result = pot_denit_rate * no3[layer] * swf * stf;

                    // check that the denitrification rate is not greater than no3 content
                    result = Math.Min(result, no3[layer]);
                }
                else
                    result = 0.0;

                return result;
            }

            /// <summary>
            /// Calculate the N2 to N2O ratio during denitrification
            /// </summary>
            /// <remarks>
            /// parameters are given in pairs, for aerobic and anaerobic conditions (with pond)
            /// </remarks>
            /// <param name="layer">the soil layer index for which calculations will be made</param>
            /// <returns>The ratio between N2 and N2O (0-1)</returns>
            private double Denitrification_Nratio(int layer)
            {
                double result;
                //int index = 0; // denitrification calcs are not different whether there is pond or not. use 0 as default

                // the water filled pore space (%)
                double WFPS = g.waterBalance.SWmm[layer] / g.soilPhysical.SATmm[layer] * 100.0;

                // CO2 production today (kgC/ha)
                double CO2_prod = co2_atm[layer];

                // calculate the terms for the formula from Thornburn et al (2010)
                bool didInterpolate;
                double CO2effect = 0.0;
                if (CO2_prod > g.epsilon)
                    CO2effect = Math.Exp(g.N2N2O_parmB * (no3[layer] / CO2_prod));
                CO2effect = Math.Max(g.N2N2O_parmA, CO2effect);
                double WFPSeffect = MathUtilities.LinearInterpReal(WFPS, g.Denitrification_WFPSFactorData.xVals, g.Denitrification_WFPSFactorData.yVals, out didInterpolate);
                result = Math.Max(0.0, g.Denit_k1 * CO2effect * WFPSeffect);

                return result;
            }

            /// <summary>
            /// Calculate the amount of NH4 converted to NO2 via nitritation
            /// </summary>
            /// <param name="layer">the node number representing soil layer for which calculations will be made</param>
            /// <returns>delta N coverted from NH4 into NO2</returns>
            private double Nitritation(int layer)
            {
                double result;

                // get the potential nitritation rate for this layer
                double nh4ppm = nh4[layer] * g.convFactor[layer];
                double potNitritationRate = MathUtilities.Divide(g.NitritationMaxPotential * nh4ppm, nh4ppm + g.NitritationNH4ForHalfRate, 0.0);

                if (potNitritationRate >= g.epsilon)
                {
                    // get the soil temperature factor
                    double stf = SoilTempFactor(layer, 0, g.Nitrification2_TemperatureFactorData);

                    // get the soil water factor
                    double swf = SoilMoistFactor(layer, 0, g.Nitrification2_MoistureFactorData);

                    // get the soil pH factor
                    double phf = SoilpHFactor(layer, 0, g.Nitritation_pHFactorData);

                    // get most limiting factor
                    double limitingFactor = Math.Min(swf, Math.Min(stf, phf));

                    // get the actual rate of nitritation
                    double nitritationRate = potNitritationRate * limitingFactor * Math.Max(0.0, 1.0 - g.inhibitionFactor_Nitrification[layer]);

                    // check that the nitritation rate is not greater than nh4 content
                    nitritationRate = Math.Min(nitritationRate, nh4ppm);

                    result = MathUtilities.Divide(nitritationRate, g.convFactor[layer], 0.0);   // convert back to kg/ha
                }
                else
                    result = 0.0;

                return result;
            }

            /// <summary>
            /// Calculate the amount of NO2 converted to NO3 via nitratation
            /// </summary>
            /// <param name="layer">the node number representing soil layer for which calculations will be made</param>
            /// <returns>delta N coverted from NO2 into NO3</returns>
            private double Nitratation(int layer)
            {
                double result;

                // get the potential nitratation rate for this layer
                double no2ppm = no2[layer] * g.convFactor[layer];
                double potNitratationRate = MathUtilities.Divide(g.NitritationMaxPotential * no2ppm, no2ppm + g.NitratationNH4ForHalfRate, 0.0);

                if (potNitratationRate >= g.epsilon)
                {
                    // get the soil temperature factor
                    double stf = SoilTempFactor(layer, 0, g.Nitrification2_TemperatureFactorData);

                    // get the soil water factor
                    double swf = SoilMoistFactor(layer, 0, g.Nitrification2_MoistureFactorData);

                    // get the soil pH factor
                    double phf = SoilpHFactor(layer, 0, g.Nitratation_pHFactorData);

                    // get most limiting factor
                    double limitingFactor = Math.Min(swf, Math.Min(stf, phf));

                    // get the actual rate of nitratation
                    double nitratationRate = potNitratationRate * limitingFactor;

                    // check that the nitratation rate is not greater than no2 content
                    nitratationRate = Math.Min(nitratationRate, no2ppm);

                    result = MathUtilities.Divide(nitratationRate, g.convFactor[layer], 0.0);   // convert back to kg/ha
                }
                else
                    result = 0.0;

                return result;
            }

            /// <summary>
            /// Calculate the amount of N2O produced during nitritation
            /// </summary>
            /// <param name="deltaNH3Oxidation">the deltaNH3Oxidation</param>
            /// <param name="layer">the node number representing soil layer for which calculations will be made</param>
            /// <returns>delta N coverted from NH2OH into N2O</returns>
            private double N2OProducedDuringNitritation(double deltaNH3Oxidation, int layer)
            {
                double result = g.AmmoxLossParam1 * (Math.Exp(deltaNH3Oxidation * g.AmmoxLossParam2) - 1.0);
                result = Math.Min(deltaNH3Oxidation, result);
                result = MathUtilities.Divide(result, g.convFactor[layer], 0.0);   // convert back to kg/ha
                return result;
            }

            /// <summary>
            /// Calculate amount of gaseous N produced via co-denitrification
            /// </summary>
            /// <param name="layer">the soil layer index for which calculations will be made</param>
            /// <returns>delta N coverted into gaseous forms</returns>
            private double Codenitrification(int layer)
            {
                double result;

                // get available C and N from soil organic pools
                double totalC = (hum_c[layer] - inert_c[layer] + biom_c[layer] + fom_c[0][layer]) * g.convFactor[layer];

                //get the waterSoluble C and N
                waterSoluble_c[layer] = g.actCExp_parmA * Math.Pow(totalC, g.actCExp_parmB);
                double waterSolubleOrganicN = Math.Min(biom_n[layer] + fom_n[0][layer], nh3[layer]);

                // get the potential codenitrification rate
                double potCodenitrificationRate = g.CodenitrificationRateCoefficient * waterSoluble_c[layer];
                double potNCodenitrifiable = 2.0 * Math.Min(no2[layer], waterSolubleOrganicN);

                if ((potCodenitrificationRate >= g.epsilon) && (potNCodenitrifiable >= g.epsilon))
                {
                    // get the soil temperature factor
                    double stf = SoilTempFactor(layer, 0, g.Codenitrification_TemperatureFactorData);

                    // get the soil water factor
                    double swf = SoilMoistFactor(layer, 0, g.Codenitrification_MoistureFactorData);

                    // get the soil pH factor
                    double phf = SoilpHFactor(layer, 0, g.Codenitrification_pHFactorData);

                    // get most limiting factor
                    double limitingFactor = Math.Min(swf, Math.Min(stf, phf));

                    // calculate codenitrification rate  - kg/ha
                    result = potCodenitrificationRate * potNCodenitrifiable * limitingFactor;
                }
                else
                    result = 0.0;

                return result;
            }

            /// <summary>
            /// Calculate the N2 fraction during codenitrification
            /// </summary>
            /// <param name="layer">the soil layer index for which calculations will be made</param>
            /// <returns>The fraction of N2 (0-1)</returns>
            private double CodenitrificationN2Fraction(int layer)
            {
                bool DidInterpolate;
                double totalNN = (nh3[layer] + no2[layer]) * g.convFactor[layer];
                double result = MathUtilities.LinearInterpReal(totalNN,
                                g.Codenitrification_NH3NO2FactorData.xVals,
                                g.Codenitrification_NH3NO2FactorData.yVals,
                                out DidInterpolate);
                return result;
            }

            #endregion

            #region Old FOM auxiliary functions

            private double[] FOMTurnOverRate(int pool)
            {
                switch (pool)
                {
                    case 0: return g.Pool1FOMTurnOverRate;
                    case 1: return g.Pool2FOMTurnOverRate;
                    case 2: return g.Pool3FOMTurnOverRate;
                    default: throw new Exception("Coding error: bad fraction in FractRDFom");
                }
            }

            #endregion  old functions

            #region >>  Environmental factors

            /// <summary>
            /// Calculate a temperature factor (0-1) for C and N processes
            /// </summary>
            /// <param name="layer">The soil layer to calculate</param>
            /// <param name="index">Parameter indication whether pond exists</param>
            /// <param name="Parameters">Parameter data</param>
            /// <returns>Temperature limiting factor (0-1)</returns>
            private double SoilTempFactor(int layer, int index, BentStickData Parameters)
            {
                // + Assumptions
                //     index = 0 for aerobic conditions, 1 for anaerobic

                if (index > Parameters.xValueForOptimum.Length - 1)
                    throw new Exception("SoilNitrogen.SoilTempFactor - invalid value for \"index\" parameter");

                double Toptimum = Parameters.xValueForOptimum[index];
                double Fzero = Parameters.yValueAtZero[index];
                double CurveN = Parameters.CurveExponent[index];
                double AuxV = Math.Pow(Fzero, 1 / CurveN);
                double Tzero = Toptimum * AuxV / (AuxV - 1);
                double beta = 1 / (Toptimum - Tzero);

                return Math.Min(1.0, Math.Pow(beta * Math.Max(0.0, g.soilTemperature.Value[layer] - Tzero), CurveN));
            }

            /// <summary>
            /// Calculate a soil moist factor (0-1) for C and N processes
            /// </summary>
            /// <param name="layer">The soil layer to calculate</param>
            /// <param name="index">Parameter indication whether pond exists</param>
            /// <param name="Parameters">Parameter data</param>
            /// <returns>Soil moisture limiting factor (0-1)</returns>
            private double SoilMoistFactor(int layer, int index, BrokenStickData Parameters)
            {
                // + Assumptions
                //   index = 0 for aerobic conditions, 1 for anaerobic

                if (index == 0)
                {
                    bool didInterpolate;

                    // get the modified soil water variable
                    double[] yVals = { 0.0, 1.0, 2.0, 3.0 };
                    double[] xVals = { 0.0, g.soilPhysical.LL15[layer], g.soilPhysical.DUL[layer], g.soilPhysical.SAT[layer] };
                    double myX = MathUtilities.LinearInterpReal(g.waterBalance.SW[layer], xVals, yVals, out didInterpolate);

                    // get the soil moist factor
                    return MathUtilities.LinearInterpReal(myX, Parameters.xVals, Parameters.yVals, out didInterpolate);
                }
                else if (index == 1) // if pond is active
                    return 1.0;
                else
                    throw new Exception("SoilNitrogen.SoilMoistFactor - invalid value for \"index\" parameter");
            }

            /// <summary>
            /// Calculate a water filled pore space factor for denitrification processes
            /// </summary>
            /// <param name="layer">The soil layer to calculate</param>
            /// <param name="index">Parameter indication whether pond exists</param>
            /// <param name="Parameters">Parameter data</param>
            /// <returns>limiting factor due to water filled pore space (0-1)</returns>
            private double WaterFilledPoreSpaceFactor(int layer, int index, BrokenStickData Parameters)
            {
                // + Assumptions
                //   index = 0 for aerobic conditions, 1 for anaerobic

                if (index == 0)
                {
                    bool didInterpolate;

                    // get the WFPS value (%)
                    double WFPS = g.waterBalance.SWmm[layer] / g.soilPhysical.SATmm[layer] * 100.0;

                    // get the WFPS factor
                    return MathUtilities.LinearInterpReal(WFPS, Parameters.xVals, Parameters.yVals, out didInterpolate);
                }
                else if (index == 1) // if pond is active
                    return 1.0;
                else
                    throw new Exception("SoilNitrogen.SoilMoistFactor - invalid value for \"index\" parameter");
            }

            /// <summary>
            /// Calculate a pH factor for C and N processes
            /// </summary>
            /// <param name="layer">The soil layer to calculate</param>
            /// <param name="index">Parameter indication whether pond exists</param>
            /// <param name="Parameters">Parameter data</param>
            /// <returns>Soil pH limiting factor (0-1)</returns>
            private double SoilpHFactor(int layer, int index, BrokenStickData Parameters)
            {
                bool DidInterpolate;
                return MathUtilities.LinearInterpReal(g.ph[layer], Parameters.xVals, Parameters.yVals, out DidInterpolate);
            }

            /// <summary>
            /// Calculate a C:N ratio factor for C and N processes
            /// </summary>
            /// <param name="layer">The soil layer to calculate</param>
            /// <param name="index">Parameter indication whether pond exists</param>
            /// <param name="OptCN">The optimum CN ration, below which there is no limitations</param>
            /// <param name="rateCN">A rate factor to increase limitation as function of increasing CN ratio</param>
            /// <returns>The CN ratio limiting factor</returns>
            private double CNratioFactor(int layer, int index, double OptCN, double rateCN)
            {
                // get total available mineral N (kg/ha)
                double nitTot = Math.Max(0.0, no3[layer] + nh4[layer]);

                // get the amounts of fresh organic carbon and nitrogen (kg/ha)
                double fomC = 0.0;
                double fomN = 0.0;
                for (int pool = 0; pool < 3; pool++)
                {
                    fomC += fom_c[pool][layer];
                    fomN += fom_n[pool][layer];
                }

                // ratio of C in fresh OM to N available for decay
                double cnr = MathUtilities.Divide(fomC, fomN + nitTot, 0.0);

                return Math.Max(0.0, Math.Min(1.0, Math.Exp(-rateCN * (cnr - OptCN) / OptCN)));
            }

            #endregion Envmt factors

            #endregion C and N processes
        }
    }
}
