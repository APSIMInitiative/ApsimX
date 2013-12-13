using System;
using System.Reflection;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Xml;
using Models.Core;

namespace Models.Soils
{

    /// <summary>
    /// This partial class contains the SoilCN patch class
    /// </summary>
    public partial class SoilNitrogen
    {

        /// <summary>
        /// Class containing all the specific soil C and N processes
        /// </summary>
        /// <remarks>
        /// It can instanciated many times, thus describing several patches (soil variability)
        /// </remarks>
        [Serializable]
        class soilCNPatch
        {

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
            /// Reference to main SoilNitrogen Class - for accessing the parameters and input variables
            /// </summary>
            private SoilNitrogen g;

            #endregion

            #region Parameters that do or may change during simulation

            #region Input and output variables

            #region Values for soil organic matter (som)

            /// <summary>
            /// Total soil organic carbon content (%)
            /// </summary>
            public double[] oc
            {
                get
                {
                    double[] result;
                    result = new double[g.dlayer.Length];
                    for (int i = 0; i < g.dlayer.Length; i++)
                        result[i] = (hum_c[i] + biom_c[i]) * convFactor_kgha2ppm(i) / 10000;  // (100/1000000) = convert to ppm and then to %
                    return result;
                }
                private set { }	// setting is actually done via FOM, Hum, Biom, etc.
            }

            #endregion

            #region Values for soil mineral N

            // soil urea nitrogen amount (kgN/ha)
            private double[] _urea;     // Internal variable holding the urea amounts
            public double[] urea
            {
                get { return _urea; }
                set
                {
                    for (int layer = 0; layer < Math.Max(value.Length, g.dlayer.Length); ++layer)
                    {
                        if (layer >= g.dlayer.Length)
                        {
                            g.Summary.WriteMessage(g.FullPath, " Attempt to assign urea value to a non-existent soil layer - extra values will be ignored");
                            break;
                        }
                        else if (layer >= value.Length)
                        {
                            // not all values were supplied, assume minimum
                            Array.Resize(ref value, value.Length + 1);
                            value[layer] = g.urea_min[layer];
                        }
                        else
                        {
                            // a value was supplied, check whether it is valid (positive and within bounds)
                            bool IsVariableOK = CheckNegativeValues(urea[layer], layer, "urea");
                            if (!IsVariableOK)
                                urea[layer] = g.urea_min[layer];
                            IsVariableOK = CheckVariableBounds(ref urea[layer], layer, "urea", g.urea_min[layer], 10000, false);
                        }
                        _urea[layer] = value[layer];
                    }
                }
            }

            //private double[] nh4_reset;      // stores initial values, can be used for a Reset operation
            private double[] _nh4;     // Internal variable holding the nh4 amounts
            public double[] nh4
            {
                get { return _nh4; }
                set
                {
                    for (int layer = 0; layer < Math.Max(value.Length, g.dlayer.Length); ++layer)
                    {
                        if (layer >= _nh4.Length)
                        {
                            g.Summary.WriteMessage(g.FullPath, " Attempt to assign ammonium value to a non-existent soil layer - extra values will be ignored");
                            break;
                        }
                        else if (layer >= value.Length)
                        {
                            // not all values were supplied, assume minimum
                            Array.Resize(ref value, value.Length + 1);
                            value[layer] = g.nh4_min[layer];
                        }
                        else
                        {
                            // a value was supplied, check whether it is valid (positive and within bounds)
                            bool IsVariableOK = CheckNegativeValues(nh4[layer], layer, "nh4");
                            if (!IsVariableOK)
                                nh4[layer] = g.nh4_min[layer];
                            IsVariableOK = CheckVariableBounds(ref nh4[layer], layer, "nh4", g.nh4_min[layer], 10000, false);
                        }
                        _nh4[layer] = value[layer];
                    }
                }
            }

            // soil nitrate nitrogen amount (kgN/ha)
            //private double[] no3_reset;      // stores initial values, can be used for a Reset operation
            private double[] _no3 = null;
            public double[] no3
            {
                get { return _no3; }
                set
                {
                    for (int layer = 0; layer < Math.Max(value.Length, g.dlayer.Length); ++layer)
                    {
                        if (layer >= _no3.Length)
                        {
                            g.Summary.WriteMessage(g.FullPath, " Attempt to assign no3 value to a non-existent soil layer - extra values will be ignored");
                            break;
                        }
                        else if (layer >= value.Length)
                        {
                            // not all values were supplied, assume minimum
                            Array.Resize(ref value, value.Length + 1);
                            value[layer] = g.no3_min[layer];
                        }
                        else
                        {
                            // a value was supplied, check whether it is valid (positive and within bounds)
                            bool IsVariableOK = CheckNegativeValues(no3[layer], layer, "no3");
                            if (!IsVariableOK)
                                no3[layer] = g.no3_min[layer];
                            IsVariableOK = CheckVariableBounds(ref no3[layer], layer, "no3", g.no3_min[layer], 10000, false);
                        }
                        _no3[layer] = value[layer];
                    }
                }
            }

            #endregion

            #endregion

            #region Settable only variables

            #region Mineral nitrogen

            // variation in urea as given by another component
            public double[] dlt_urea
            {
                set
                {
                    if (value != null)
                    {
                        for (int layer = 0; layer < Math.Max(value.Length, g.dlayer.Length); ++layer)
                        {
                            if (layer >= g.dlayer.Length)
                            {
                                g.Summary.WriteMessage(g.FullPath, " Attempt to change the urea value of a non-existent layer - extra values will be ignored");
                                break;
                            }
                            else if (layer >= value.Length)
                            {
                                // not all values were supplied, ignore these layers
                                // value[layer] = 0.0;
                            }
                            else
                            {
                                // a value was supplied, check whether it is valid
                                bool IsVariableOK = CheckVariableBounds(ref value[layer], layer, "dlt_urea", -2000.0, 2000.0, false);
                                _urea[layer] += value[layer];
                                IsVariableOK = CheckNegativeValues(_urea[layer], layer, "urea");
                                if (!IsVariableOK)
                                    _urea[layer] = g.urea_min[layer];
                                IsVariableOK = CheckVariableBounds(ref _urea[layer], layer, "urea", g.urea_min[layer], 10000, false);
                            }
                        }
                    }
                }
            }

            // variation in nh4 as given by another component
            public double[] dlt_nh4
            {
                set
                {
                    if (value != null)
                    {
                        for (int layer = 0; layer < Math.Max(value.Length, g.dlayer.Length); ++layer)
                        {
                            if (layer >= g.dlayer.Length)
                            {
                                g.Summary.WriteMessage(g.FullPath, " Attempt to change the ammonium value of a non-existent layer - extra values will be ignored");
                                break;
                            }
                            else if (layer >= value.Length)
                            {
                                // not all values were supplied, ignore these layers
                                // value[layer] = 0.0;
                            }
                            else
                            {
                                // a value was supplied, check whether it is valid
                                bool IsVariableOK = CheckVariableBounds(ref value[layer], layer, "dlt_nh4", -2000.0, 2000.0, false);
                                _nh4[layer] += value[layer];
                                IsVariableOK = CheckNegativeValues(_nh4[layer], layer, "nh4");
                                if (!IsVariableOK)
                                    _nh4[layer] = g.nh4_min[layer];
                                IsVariableOK = CheckVariableBounds(ref _nh4[layer], layer, "nh4", g.nh4_min[layer], 10000, false);
                            }
                        }
                    }
                }
            }

            // variation in no3 as given by another component
            public double[] dlt_no3
            {
                set
                {
                    if (value != null)
                    {
                        for (int layer = 0; layer < Math.Max(value.Length, g.dlayer.Length); ++layer)
                        {
                            if (layer >= g.dlayer.Length)
                            {
                                g.Summary.WriteMessage(g.FullPath, " Attempt to change the nitrate value of a non-existent layer - extra values will be ignored");
                                break;
                            }
                            else if (layer >= value.Length)
                            {
                                // not all values were supplied, ignore these layers
                                // value[layer] = 0.0;
                            }
                            else
                            {
                                // a value was supplied, check whether it is valid
                                bool IsVariableOK = CheckVariableBounds(ref value[layer], layer, "dlt_no3", -2000.0, 2000.0, false);

                                if (_no3[layer] + value[layer] < g.no3_min[layer])
                                    IsVariableOK = false;

                                _no3[layer] += value[layer];
                                IsVariableOK = CheckNegativeValues(_no3[layer], layer, "no3");
                                if (!IsVariableOK)
                                    _no3[layer] = g.no3_min[layer];
                                IsVariableOK = CheckVariableBounds(ref _no3[layer], layer, "no3", g.no3_min[layer], 10000, false);
                            }
                        }
                    }
                }
            }

            #endregion

            #region organic N and C

            public double[] dlt_org_n
            {
                set
                {
                    for (int layer = 0; layer < value.Length; ++layer)
                    {
                        bool IsVariableOK = CheckVariableBounds(ref value[layer], layer, "dlt_org_n", -10000.0, 10000.0, false);
                        fom_n[layer] += value[layer];
                        IsVariableOK = CheckNegativeValues(fom_n[layer], layer, "fom_n");
                        if (!IsVariableOK)
                            fom_n[layer] = 0.0;
                        IsVariableOK = CheckVariableBounds(ref fom_n[layer], layer, "fom_n", 0.0, 100000, false);
                    }
                }
            }

            public double[] dlt_org_c_pool1
            {
                set
                {
                    for (int layer = 0; layer < value.Length; ++layer)
                    {
                        bool IsVariableOK = CheckVariableBounds(ref value[layer], layer, "dlt_org_pool1", -100000.0, 100000.0, false);
                        fom_c_pool1[layer] += value[layer];
                        IsVariableOK = CheckNegativeValues(fom_c_pool1[layer], layer, "fom_c_pool1");
                        if (!IsVariableOK)
                            fom_c_pool1[layer] = 0.0;
                        IsVariableOK = CheckVariableBounds(ref fom_c_pool1[layer], layer, "fom_c_pool1", 0.0, 1000000, false);
                    }
                }
            }

            public double[] dlt_org_c_pool2
            {
                set
                {
                    for (int layer = 0; layer < value.Length; ++layer)
                    {
                        bool IsVariableOK = CheckVariableBounds(ref value[layer], layer, "dlt_org_pool2", -100000.0, 100000.0, false);
                        fom_c_pool2[layer] += value[layer];
                        IsVariableOK = CheckNegativeValues(fom_c_pool2[layer], layer, "fom_c_pool2");
                        if (!IsVariableOK)
                            fom_c_pool2[layer] = 0.0;
                        IsVariableOK = CheckVariableBounds(ref fom_c_pool2[layer], layer, "fom_c_pool2", 0.0, 1000000, false);
                    }
                }
            }

            public double[] dlt_org_c_pool3
            {
                set
                {
                    for (int layer = 0; layer < value.Length; ++layer)
                    {
                        bool IsVariableOK = CheckVariableBounds(ref value[layer], layer, "dlt_org_pool3", -100000.0, 100000.0, false);
                        fom_c_pool3[layer] += value[layer];
                        IsVariableOK = CheckNegativeValues(fom_c_pool3[layer], layer, "fom_c_pool3");
                        if (!IsVariableOK)
                            fom_c_pool3[layer] = 0.0;
                        IsVariableOK = CheckVariableBounds(ref fom_c_pool3[layer], layer, "fom_c_pool3", 0.0, 1000000, false);
                    }
                }
            }

            #endregion

            #endregion

            #endregion

            #region Outputs we make available to other components

            #region Outputs for Nitrogen

            #region Changes for today - deltas

            public double[] dlt_nh4_net;   // net nh4 change today

            public double[] nh4_transform_net; // net NH4 transformation today

            public double[] dlt_no3_net;   // net no3 change today

            public double[] no3_transform_net; // net NO3 transformation today

            public double[] dlt_n_min         // net mineralisation
            {
                get
                {
                    double[] result = new double[g.dlayer.Length];
                    for (int layer = 0; layer < g.dlayer.Length; layer++)
                        result[layer] = dlt_n_hum_2_min[layer] + dlt_n_biom_2_min[layer] + dlt_n_fom_2_min[layer];
                    return result;
                }
            }

            public double[] dlt_n_min_res
            {
                get
                {
                    double[] result = new double[g.dlayer.Length];
                    for (int layer = 0; layer < g.dlayer.Length; layer++)
                        result[layer] = dlt_no3_decomp[layer] + dlt_nh4_decomp[layer];
                    return result;
                }
            }

            public double[] dlt_nh4_decomp;   // Net Residue NH4 mineralisation

            public double[] dlt_no3_decomp;   // Net Residue NO3 mineralisation

            public double[] dlt_n_fom_2_min;     // net fom N mineralized (negative for immobilization) 

            public double[] dlt_n_hum_2_min;     // net humic N mineralized

            public double[] dlt_n_biom_2_min;    // net biomass N mineralized

            public double[] dlt_n_min_tot
            {
                get
                {
                    double[] result = new double[g.dlayer.Length];
                    for (int layer = 0; layer < g.dlayer.Length; layer++)
                        result[layer] = dlt_n_hum_2_min[layer] + dlt_n_biom_2_min[layer] + dlt_n_fom_2_min[layer] + dlt_no3_decomp[layer] + dlt_nh4_decomp[layer];
                    return result;
                }
            }

            public double[] dlt_urea_hydrolised;   // nitrogen coverted by hydrolysis (from urea to NH4)

            public double[] dlt_nitrification;     // nitrogen coverted by nitrification (from NH4 to either NO3 or N2O)

            public double[] effective_nitrification; // effective nitrogen coverted by nitrification (from NH4 to NO3)
            // (Alias dlt_rntrf_eff)

            public double[] dlt_nh4_dnit;      // NH4 N denitrified

            public double[] dlt_no3_dnit;      // NO3 N denitrified

            public double[] n2o_atm;           // amount of N2O produced

            public double[] n2_atm { get; set; }            // amount of N2 produced

            public double[] dnit
            {
                get
                {
                    double[] result = new double[g.dlayer.Length];
                    for (int layer = 0; layer < g.dlayer.Length; layer++)
                        result[layer] = dlt_no3_dnit[layer] + dlt_nh4_dnit[layer];
                    return result;
                }
            }

            public double dlt_n_loss_in_sed;

            public double[] nh4_deficit_immob;    // excess N required above NH4 supply    #endregion

            #endregion

            #region Amounts in various pools


            [Description("relative area of each CN patch")]
            public double patch_area
            { get { return RelativeArea; } }


            [Description("humus C in each CN patch")]
            public double[] patch_hum_c
            { get { return hum_c; } }


            public double[] fom_n         // nitrogen in FOM
            {
                get
                {
                    double[] result = new double[g.dlayer.Length];
                    for (int layer = 0; layer < g.dlayer.Length; ++layer)
                    {
                        result[layer] = fom_n_pool1[layer] + fom_n_pool2[layer] + fom_n_pool3[layer];
                    }
                    return result;
                }
            }

            public double[] fom_n_pool1;

            public double[] fom_n_pool2;

            public double[] fom_n_pool3;

            public double[] hum_n;         // Humic N

            public double[] biom_n;        // biomass nitrogen

            public double[] nit_tot           // total N in soil
            {
                get
                {
                    double[] result = null;
                    if (g.dlayer != null)
                    {
                        double[] fomn = fom_n;
                        result = new double[g.dlayer.Length];
                        for (int layer = 0; layer < g.dlayer.Length; layer++)
                            result[layer] += fomn[layer] + hum_n[layer] + biom_n[layer] + _no3[layer] + _nh4[layer] + _urea[layer];
                    }
                    return result;
                }
            }

            #endregion

            #endregion

            #region Outputs for Carbon

            #region Changes for today - deltas

            public double dlt_c_loss_in_sed;

            double[][] dlt_c_fom_2_hum = new double[3][];
            public double[] dlt_fom_c_hum  // fom C converted to humic (kg/ha)
            {
                get
                {
                    int nLayers = dlt_c_fom_2_hum[0].Length;
                    double[] result = new double[nLayers];
                    for (int layer = 0; layer < nLayers; layer++)
                        result[layer] = dlt_c_fom_2_hum[0][layer] + dlt_c_fom_2_hum[1][layer] + dlt_c_fom_2_hum[2][layer];
                    return result;
                }
            }

            double[][] dlt_c_fom_2_biom = new double[3][];
            public double[] dlt_fom_c_biom // fom C converted to biomass (kg/ha)
            {
                get
                {
                    int nLayers = dlt_c_fom_2_biom[0].Length;
                    double[] result = new double[nLayers];
                    for (int layer = 0; layer < nLayers; layer++)
                        result[layer] = dlt_c_fom_2_biom[0][layer] + dlt_c_fom_2_biom[1][layer] + dlt_c_fom_2_biom[2][layer];
                    return result;
                }
            }

            double[][] dlt_c_fom_2_atm = new double[3][];
            public double[] dlt_fom_c_atm  // fom C lost to atmosphere (kg/ha)
            {
                get
                {
                    int nLayers = dlt_c_fom_2_atm[0].Length;
                    double[] result = new double[nLayers];
                    for (int layer = 0; layer < nLayers; layer++)
                        result[layer] = dlt_c_fom_2_atm[0][layer] + dlt_c_fom_2_atm[1][layer] + dlt_c_fom_2_atm[2][layer];
                    return result;
                }
            }

            public double[] dlt_c_hum_2_biom;

            public double[] dlt_c_hum_2_atm;

            public double[] dlt_c_biom_2_hum;

            public double[] dlt_c_biom_2_atm;

            public double[][] dlt_c_res_2_biom;
            public double[] dlt_res_c_biom
            {
                get
                {
                    int nLayers = dlt_c_res_2_biom.Length;
                    double[] result = new double[nLayers];
                    for (int layer = 0; layer < nLayers; layer++)
                        result[layer] = SumDoubleArray(dlt_c_res_2_biom[layer]);
                    return result;
                }
            }

            public double[][] dlt_c_res_2_hum;
            public double[] dlt_res_c_hum
            {
                get
                {
                    int nLayers = dlt_c_res_2_hum.Length;
                    double[] result = new double[nLayers];
                    for (int layer = 0; layer < nLayers; layer++)
                        result[layer] = SumDoubleArray(dlt_c_res_2_hum[layer]);
                    return result;
                }
            }

            public double[][] dlt_c_res_2_atm;
            public double[] dlt_res_c_atm
            {
                get
                {
                    int nLayers = dlt_c_res_2_atm.Length;
                    double[] result = new double[nLayers];
                    for (int layer = 0; layer < nLayers; layer++)
                        result[layer] = SumDoubleArray(dlt_c_res_2_atm[layer]);
                    return result;
                }
            }

            public double[] dlt_fom_c_pool1;

            public double[] dlt_fom_c_pool2;

            public double[] dlt_fom_c_pool3;

            public double[] soilp_dlt_res_c_atm;

            public double[] soilp_dlt_res_c_hum;

            public double[] soilp_dlt_res_c_biom;

            #endregion

            #region Amounts in various pools

            //public double[] OC_reset;  // initial OC - needed for urea hydrolysis

            public double[] fom_c         // fresh organic C
            {
                get
                {
                    double[] result = new double[g.dlayer.Length];
                    for (int layer = 0; layer < g.dlayer.Length; ++layer)
                    {
                        result[layer] = fom_c_pool1[layer] + fom_c_pool2[layer] + fom_c_pool3[layer];
                    }
                    return result;
                }
            }

            public double[] fom_c_pool1;

            public double[] fom_c_pool2;

            public double[] fom_c_pool3;

            public double[] hum_c;         // Humic C

            public double[] biom_c;        // biomass carbon

            public double[] inert_c;       // humic C that is not subject to mineralization (kg/ha)

            public double[] carbon_tot    // total carbon in soil
            {
                get
                {
                    double[] result = new double[g.dlayer.Length];
                    for (int layer = 0; layer < g.dlayer.Length; layer++)
                    {
                        result[layer] += fom_c_pool1[layer] + fom_c_pool2[layer] + fom_c_pool3[layer] + hum_c[layer] + biom_c[layer];
                    }
                    return result;
                }
            }

            #endregion

            #endregion

            #region Factors and other outputs

            public double[] soilp_dlt_org_p;

            #endregion

            #endregion

            #region Internal variables

            private double[] nh4_yesterday;                 // yesterday's ammonium nitrogen(kg/ha)
            private double[] no3_yesterday;                 // yesterday's nitrate nitrogen (kg/ha)
            public int fom_type;
            private int num_residues = 0;                   // number of residues decomposing   
            private string[] residue_name;                  // name of residues decomposing
            private string[] residue_type;                  // type of decomposing residue
            private double[] pot_c_decomp;                  // Potential residue C decomposition (kg/ha)
            private double[] pot_n_decomp;                  // Potential residue N decomposition (kg/ha)
            private double[] pot_p_decomp;                  // Potential residue P decomposition (kg/ha)
            private double[][] dlt_c_decomp;            // residue C decomposition (kg/ha)
            private double[][] dlt_n_decomp;            // residue N decomposition (kg/ha)

            public SurfaceOrganicMatterDecompType SOMDecomp;

            #endregion

            #region Setup calculations

            public void InitCalc()
            {
                for (int layer = 0; layer < g.dlayer.Length; layer++)
                {
                    // store these values so they may be used tomorrow
                    nh4_yesterday[layer] = _nh4[layer];
                    no3_yesterday[layer] = _no3[layer];
                }
            }

            #endregion

            #region Process calculations

            #region Daily processses

            public void Process()
            {
                // + Purpose
                //     This routine performs the soil C and N balance, daily.
                //      - Assesses potential decomposition of surface residues (adjust decompostion if needed, accounts for mineralisation/immobilisation of N)
                //      - Calculates hydrolysis of urea, denitrification, transformations on soil organic matter (including N mineralisation/immobilition) and nitrification.

                int nLayers = g.dlayer.Length;                    // number of layers in the soil
                double[,] dlt_fom_n = new double[3, nLayers];   // fom N mineralised in each fraction (kg/ha)

                if (g.is_pond_active)
                {
                    // dsg 190508,  If there is a pond, the POND module will decompose residues - not SoilNitrogen
                    // dsg 110708   Get the biom & hum C decomposed in the pond and add to soil - on advice of MEP

                    // increment the hum and biom C pools in top soil layer
                    hum_c[0] += g.pond_hum_C;         // humic material from breakdown of residues in pond
                    biom_c[0] += g.pond_biom_C;       // biom material from breakdown of residues in pond

                    // reset the N amounts of N in hum and biom pools
                    hum_n[0] = Utility.Math.Divide(hum_c[0], g.hum_cn, 0.0);
                    biom_n[0] = Utility.Math.Divide(biom_c[0], g.biom_cn, 0.0);
                }
                else
                {
                    // Decompose residues
                    //  assess the potential decomposition of surface residues and calculate actual mineralisation/immobilisation
                    DecomposeResidues();

                    // update C content in hum and biom pools
                    for (int layer = 0; layer < nLayers; layer++)
                    {
                        hum_c[layer] += SumDoubleArray(dlt_c_res_2_hum[layer]);
                        biom_c[layer] += SumDoubleArray(dlt_c_res_2_biom[layer]);
                    }

                    // update N content in hum and biom pools as well as the mineral N
                    for (int layer = 0; layer < nLayers; layer++)
                    {
                        hum_n[layer] = Utility.Math.Divide(hum_c[layer], g.hum_cn, 0.0);
                        biom_n[layer] = Utility.Math.Divide(biom_c[layer], g.biom_cn, 0.0);

                        // update soil mineral N
                        _nh4[layer] += dlt_nh4_decomp[layer];
                        _no3[layer] += dlt_no3_decomp[layer];
                    }
                }

                // now take each layer in turn and compute N processes
                for (int layer = 0; layer < nLayers; layer++)
                {
                    // urea hydrolysis
                    dlt_urea_hydrolised[layer] = UreaHydrolysis(layer);
                    _nh4[layer] += dlt_urea_hydrolised[layer];
                    _urea[layer] -= dlt_urea_hydrolised[layer];

                    // nitrate-N denitrification
                    switch (g.n2o_approach)
                    {
                        case 1:
                            dlt_no3_dnit[layer] = Denitrification_NEMIS(layer);
                            //n2o_atm[layer] is calculated in Nitrification_NEMIS
                            break;
                        case 2:
                            dlt_no3_dnit[layer] = Denitrification_WNMM(layer);
                            //n2o_atm[layer] is calculated in Nitrification_WNMM
                            break;
                        case 3:
                            dlt_no3_dnit[layer] = Denitrification_CENT(layer);
                            //n2o_atm[layer] is calculated in Nitrification_CENT
                            break;
                        case 0:
                        default:
                            dlt_no3_dnit[layer] = Denitrification(layer);
                            break;
                    }
                    _no3[layer] -= dlt_no3_dnit[layer];


                    // N2O loss to atmosphere - due to denitrification
                    n2o_atm[layer] = 0.0;
                    double N2N2O = Denitrification_Nratio(layer);
                    n2o_atm[layer] = dlt_no3_dnit[layer] / (N2N2O + 1.0);

                    // Calculate transformations of soil organic matter (C and N)

                    // humic pool mineralisation
                    MineraliseHumus(layer);

                    // microbial biomass pool mineralisation
                    MineraliseBiomass(layer);

                    // mineralisation of fresh organic matter pools
                    // need to be revisited - create FOM pools as array
                    //for (int fract = 0; fract < 3; fract++)
                    //{
                    //    MinFom(layer, fract);
                    //    dlt_c_fom_2_biom[fract][layer] = dlt_fc_biom[fract];
                    //    dlt_c_fom_2_hum[fract][layer] = dlt_fc_hum[fract];
                    //    dlt_c_fom_2_atm[fract][layer] = dlt_fc_atm[fract];
                    //    dlt_fom_n[fract, layer] = dlt_f_n[fract];
                    //}

                    double[] dlt_f_n;
                    double[] dlt_fc_biom;
                    double[] dlt_fc_hum;
                    double[] dlt_fc_atm;
                    FOMdecompData MineralisedFOM = new FOMdecompData();
                    if (g.useNewProcesses)
                    {
                        MineralisedFOM = MineraliseFOM1(layer);
                        for (int fract = 0; fract < 3; fract++)
                        {
                            dlt_c_fom_2_hum[fract][layer] = MineralisedFOM.dlt_c_hum[fract];
                            dlt_c_fom_2_biom[fract][layer] = MineralisedFOM.dlt_c_biom[fract];
                            dlt_c_fom_2_atm[fract][layer] = MineralisedFOM.dlt_c_atm[fract];
                            dlt_fom_n[fract, layer] = MineralisedFOM.dlt_fom_n[fract];
                        }
                        dlt_n_fom_2_min[layer] = MineralisedFOM.dlt_n_min;
                    }
                    else
                    {
                        MineraliseFOM(layer, out dlt_fc_biom, out dlt_fc_hum, out dlt_fc_atm, out dlt_f_n, out dlt_n_fom_2_min[layer]);

                        for (int fract = 0; fract < 3; fract++)
                        {
                            dlt_c_fom_2_biom[fract][layer] = dlt_fc_biom[fract];
                            dlt_c_fom_2_hum[fract][layer] = dlt_fc_hum[fract];
                            dlt_c_fom_2_atm[fract][layer] = dlt_fc_atm[fract];
                            dlt_fom_n[fract, layer] = dlt_f_n[fract];
                        }
                    }
                    // update pools C an N contents

                    hum_c[layer] += dlt_c_biom_2_hum[layer] - dlt_c_hum_2_biom[layer] - dlt_c_hum_2_atm[layer] +
                                   dlt_c_fom_2_hum[0][layer] + dlt_c_fom_2_hum[1][layer] + dlt_c_fom_2_hum[2][layer];

                    hum_n[layer] = Utility.Math.Divide(hum_c[layer], g.hum_cn, 0.0);

                    biom_c[layer] += dlt_c_hum_2_biom[layer] - dlt_c_biom_2_hum[layer] - dlt_c_biom_2_atm[layer] +
                                   dlt_c_fom_2_biom[0][layer] + dlt_c_fom_2_biom[1][layer] + dlt_c_fom_2_biom[2][layer];

                    biom_n[layer] = Utility.Math.Divide(biom_c[layer], g.biom_cn, 0.0);

                    fom_c_pool1[layer] -= (dlt_c_fom_2_hum[0][layer] + dlt_c_fom_2_biom[0][layer] + dlt_c_fom_2_atm[0][layer]);
                    fom_c_pool2[layer] -= (dlt_c_fom_2_hum[1][layer] + dlt_c_fom_2_biom[1][layer] + dlt_c_fom_2_atm[1][layer]);
                    fom_c_pool3[layer] -= (dlt_c_fom_2_hum[2][layer] + dlt_c_fom_2_biom[2][layer] + dlt_c_fom_2_atm[2][layer]);

                    fom_n_pool1[layer] -= dlt_fom_n[0, layer];
                    fom_n_pool2[layer] -= dlt_fom_n[1, layer];
                    fom_n_pool3[layer] -= dlt_fom_n[2, layer];

                    // dsg  these 3 dlts are calculated for the benefit of soilp which needs to 'get' them
                    dlt_fom_c_pool1[layer] = dlt_c_fom_2_hum[0][layer] + dlt_c_fom_2_biom[0][layer] + dlt_c_fom_2_atm[0][layer];
                    dlt_fom_c_pool2[layer] = dlt_c_fom_2_hum[1][layer] + dlt_c_fom_2_biom[1][layer] + dlt_c_fom_2_atm[1][layer];
                    dlt_fom_c_pool3[layer] = dlt_c_fom_2_hum[2][layer] + dlt_c_fom_2_biom[2][layer] + dlt_c_fom_2_atm[2][layer];

                    // add up fom in each layer in each of the pools
                    //double fom_c = fom_c_pool1[layer] + fom_c_pool2[layer] + fom_c_pool3[layer];
                    //fom_n[layer] = fom_n_pool1[layer] + fom_n_pool2[layer] + fom_n_pool3[layer];

                    // update soil mineral N after mineralisation/immobilisation

                    // starts with nh4
                    _nh4[layer] += dlt_n_hum_2_min[layer] + dlt_n_biom_2_min[layer] + dlt_n_fom_2_min[layer];

                    // check whether there is enough NH4 to be immobilised
                    nh4_deficit_immob = new double[g.dlayer.Length];
                    if (_nh4[layer] < g.nh4_min[layer])
                    {
                        nh4_deficit_immob[layer] = g.nh4_min[layer] - _nh4[layer];
                        _nh4[layer] = g.nh4_min[layer];
                    }

                    // now change no3
                    _no3[layer] -= nh4_deficit_immob[layer];
                    if (_no3[layer] < g.no3_min[layer] - g.EPSILON)
                    {
                        // note: tests for adequate mineral N for immobilisation have been made so this no3 should not go below no3_min
                        throw new Exception("N immobilisation resulted in mineral N in layer(" + (layer + 1).ToString() + ") to go below minimum");
                    }

                    // NITRIFICATION
                    switch (g.n2o_approach)
                    {
                        case 1:
                            dlt_nitrification[layer] = Nitrification(layer);                //using default APSIM process for NEMIS
                            dlt_nh4_dnit[layer] = N2OLostInNitrification_ApsimSoilNitrogen(layer);
                            break;
                        case 2:
                            dlt_nitrification[layer] = Nitrification_WNMM(layer);
                            // dlt_nh4_dnit[layer] & n2o_atm[layer] are calculated in Nitrification_WNMM
                            break;
                        case 3:
                            dlt_nitrification[layer] = Nitrification_CENT(layer);
                            // dlt_nh4_dnit[layer] & n2o_atm[layer] are calculated in Nitrification_CENT
                            break;
                        case 0:
                        default:
                            // nitrification of ammonium-N (total)
                            dlt_nitrification[layer] = Nitrification(layer);
                            // denitrification loss during nitrification  (- n2o_atm )
                            dlt_nh4_dnit[layer] = N2OLostInNitrification_ApsimSoilNitrogen(layer);
                            // N2O loss to atmosphere from nitrification
                            n2o_atm[layer] += dlt_nh4_dnit[layer];
                            break;
                    }

                    // effective or net nitrification
                    effective_nitrification[layer] = dlt_nitrification[layer] - dlt_nh4_dnit[layer];

                    // update soil mineral N
                    _no3[layer] += effective_nitrification[layer];
                    _nh4[layer] -= dlt_nitrification[layer];

                    // check some of the values
                    if (Math.Abs(_urea[layer]) < g.EPSILON)
                        _urea[layer] = 0.0;
                    if (Math.Abs(_nh4[layer]) < g.EPSILON)
                        _nh4[layer] = 0.0;
                    if (Math.Abs(_no3[layer]) < g.EPSILON)
                        _no3[layer] = 0.0;
                    if (_urea[layer] < g.urea_min[layer] || _urea[layer] > 9000.0)
                        throw new Exception("Value for urea(layer) is out of range");
                    if (_nh4[layer] < g.nh4_min[layer] || _nh4[layer] > 9000.0)
                        throw new Exception("Value for NH4(layer) is out of range");
                    if (_no3[layer] < g.no3_min[layer] || _no3[layer] > 9000.0)
                        throw new Exception("Value for NO3(layer) is out of range");

                    // net N tansformations
                    nh4_transform_net[layer] = dlt_nh4_decomp[layer] + dlt_n_fom_2_min[layer] + dlt_n_biom_2_min[layer] + dlt_n_hum_2_min[layer] - dlt_nitrification[layer] + dlt_urea_hydrolised[layer] + nh4_deficit_immob[layer];
                    no3_transform_net[layer] = dlt_no3_decomp[layer] - dlt_no3_dnit[layer] + effective_nitrification[layer] - nh4_deficit_immob[layer];

                    // net deltas
                    dlt_nh4_net[layer] = _nh4[layer] - nh4_yesterday[layer];
                    dlt_no3_net[layer] = _no3[layer] - no3_yesterday[layer];

                    // store these values so they may be used tomorrow
                    nh4_yesterday[layer] = _nh4[layer];
                    no3_yesterday[layer] = _no3[layer];
                }
            }

            public void OnTick()
            {
                // +  Purpose:
                //      Reset potential decomposition variables

                num_residues = 0;
                Array.Resize(ref pot_c_decomp, 0);
                Array.Resize(ref pot_n_decomp, 0);
                Array.Resize(ref pot_p_decomp, 0);
            }

            private void DecomposeResidues()
            {
                // + Purpose
                //     Calculate the actual C and N mineralised/immobilised from residue decomposition
                //      Check whether adequate mineral nitrogen is available to sustain potential rate of decomposition of surface
                //       residues and calculate net rate of nitrogen mineralisation/immobilisation

                // Initialise to zero by assigning new
                int nLayers = g.dlayer.Length;
                double[] no3_available = new double[nLayers]; // no3 available for mineralisation
                double[] nh4_available = new double[nLayers]; // nh4 available for mineralisation
                dlt_c_decomp = new double[nLayers][];
                dlt_n_decomp = new double[nLayers][];
                dlt_c_res_2_biom = new double[nLayers][];
                dlt_c_res_2_hum = new double[nLayers][];
                dlt_c_res_2_atm = new double[nLayers][];
                for (int layer = 0; layer < nLayers; layer++)
                {
                    dlt_c_decomp[layer] = new double[num_residues];
                    dlt_n_decomp[layer] = new double[num_residues];
                    dlt_c_res_2_biom[layer] = new double[num_residues];
                    dlt_c_res_2_hum[layer] = new double[num_residues];
                    dlt_c_res_2_atm[layer] = new double[num_residues];
                }
                dlt_nh4_decomp = new double[nLayers];
                dlt_no3_decomp = new double[nLayers];

                // get total available mineral N in soil layer which can supply N to decomposition (min_depth)
                double[] fracLayer = new double[g.dlayer.Length];
                double cumFracLayer = 0.0;
                double cumDepth = 0.0;
                int DecompLayer = 0;
                for (int layer = 0; layer < nLayers; layer++)
                {
                    fracLayer[layer] = Math.Min(1, Math.Max(0, g.min_depth - cumDepth) / g.dlayer[layer]);
                    if (fracLayer[layer] <= g.EPSILON)
                        break;  // no need to continue calculating
                    cumFracLayer += fracLayer[layer];
                    cumDepth += g.dlayer[layer];
                    DecompLayer = layer;
                    no3_available[layer] = Math.Max(0.0, _no3[layer] - g.no3_min[layer]) * fracLayer[layer];
                    nh4_available[layer] = Math.Max(0.0, _nh4[layer] - g.nh4_min[layer]) * fracLayer[layer];
                }

                double n_available = SumDoubleArray(no3_available) + SumDoubleArray(nh4_available) + SumDoubleArray(pot_n_decomp);

                // get N demand from potential decomposition
                double n_demand = Utility.Math.Divide(SumDoubleArray(pot_c_decomp) * g.ef_res * g.fr_res_biom, g.biom_cn, 0.0) +
                                  Utility.Math.Divide(SumDoubleArray(pot_c_decomp) * g.ef_res * (1.0 - g.fr_res_biom), g.hum_cn, 0.0);

                // test whether there is adequate N available to meet potential immobilisation demand
                //      if not, calculate a factor to reduce the mineralisation rates
                double ReductionFactor = 1.0;
                if (n_demand > n_available)
                    ReductionFactor = Math.Max(0.0, Math.Min(1.0, Utility.Math.Divide(SumDoubleArray(no3_available) + SumDoubleArray(nh4_available), n_demand - SumDoubleArray(pot_n_decomp), 0.0)));

                // Partition the additions of C and N to layers
                double dlt_n_decomp_tot = 0.0;
                for (int layer = 0; layer <= DecompLayer; layer++)
                {
                    double DecompFraction = fracLayer[layer] / cumFracLayer;  // the fraction of decomposition for each soil layer
                    for (int residue = 0; residue < num_residues; residue++)
                    {
                        // adjust carbon transformations and distribute over the layers
                        dlt_c_decomp[layer][residue] = pot_c_decomp[residue] * ReductionFactor * DecompFraction;
                        dlt_n_decomp[layer][residue] = pot_n_decomp[residue] * ReductionFactor * DecompFraction;
                        dlt_n_decomp_tot += dlt_n_decomp[layer][residue];

                        // partition the decomposed C between pools and losses
                        dlt_c_res_2_biom[layer][residue] = dlt_c_decomp[layer][residue] * g.ef_res * g.fr_res_biom;
                        dlt_c_res_2_hum[layer][residue] = dlt_c_decomp[layer][residue] * g.ef_res * (1.0 - g.fr_res_biom);
                        dlt_c_res_2_atm[layer][residue] = dlt_c_decomp[layer][residue] - dlt_c_res_2_biom[layer][residue] - dlt_c_res_2_hum[layer][residue];
                    }
                }

                // net N mineralised (hg/ha)
                double dlt_n_min = dlt_n_decomp_tot - n_demand * ReductionFactor;

                if (dlt_n_min > 0.0)
                {
                    // Mineralisation occurred - distribute NH4 over the layers
                    for (int layer = 0; layer <= DecompLayer; layer++)
                    {
                        double DecompFraction = fracLayer[layer] / cumFracLayer;  // the fraction of decomposition for each soil layer
                        dlt_nh4_decomp[layer] = dlt_n_min * DecompFraction;
                    }
                }
                else if (dlt_n_min < 0.0)
                {
                    // Immobilisation occurred - soak up any N required, from NH4 first then NO3 if needed
                    for (int layer = 0; layer <= DecompLayer; layer++)
                    {
                        dlt_nh4_decomp[layer] = -Math.Min(nh4_available[layer], Math.Abs(dlt_n_min));
                        dlt_n_min -= dlt_nh4_decomp[layer];
                    }
                    for (int layer = 0; layer <= DecompLayer; layer++)
                    {
                        dlt_no3_decomp[layer] = -Math.Min(no3_available[layer], Math.Abs(dlt_n_min));
                        dlt_n_min -= dlt_no3_decomp[layer];
                    }

                    // There should now be no remaining immobilisation demand
                    if (dlt_n_min < -0.001 || dlt_n_min > 0.001)
                        throw new Exception("Value for remaining immobilisation is out of range");
                }

                // gather the info for 'SendActualResidueDecompositionCalculated'
                PackActualResidueDecomposition();
            }

            private void MineraliseHumus(int layer)
            {
                // + Purpose
                //     Calculate the daily transformation of the the soil humic pool, mineralisation (+ve) or immobilisation (-ve)

                // + Assumptions
                //     There is an inert_C component of the humic pool that is not subject to mineralisation

                // dsg 200508  use different values for some constants when there's a pond and anaerobic conditions dominate
                int index = (!g.is_pond_active) ? 1 : 2;

                // get the soil temperature factor
                double tf = (g.SoilCNParameterSet == "rothc") ? RothcTF(layer, index) : TF(layer, index);
                if (g.useNewSTFFunction)
                    if (g.useFactorsBySOMpool)
                    {
                        tf = SoilTempFactor(layer, index, g.TempFactorData_MinerSOM_Hum);
                    }
                    else
                    {
                        tf = SoilTempFactor(layer, index, g.TempFactorData_MinerSOM);
                    }

                // get the soil water factor
                double wf = WF(layer, index);
                if (g.useNewSWFFunction)
                    if (g.useFactorsBySOMpool)
                    {
                        wf = SoilMoistFactor(layer, index, g.MoistFactorData_MinerSOM_Hum);
                    }
                    else
                    {
                        wf = SoilMoistFactor(layer, index, g.MoistFactorData_MinerSOM);
                    }

                // get the rate of mineralisation of N from the humic pool
                double dlt_c_min_hum = (hum_c[layer] - inert_c[layer]) * g.rd_hum[index - 1] * tf * wf;
                double dlt_n_min_hum = Utility.Math.Divide(dlt_c_min_hum, g.hum_cn, 0.0);

                // distribute the mineralised N and C
                dlt_c_hum_2_biom[layer] = dlt_c_min_hum * g.ef_hum;
                dlt_c_hum_2_atm[layer] = dlt_c_min_hum * (1.0 - g.ef_hum);
                dlt_n_hum_2_min[layer] = dlt_n_min_hum - Utility.Math.Divide(dlt_c_hum_2_biom[layer], g.biom_cn, 0.0);
            }

            private void MineraliseBiomass(int layer)
            {
                // + Purpose
                //     Calculate the daily transformation of the soil biomass pool, mineralisation (+ve) or immobilisation (-ve)

                // dsg 200508  use different values for some constants when anaerobic conditions dominate
                int index = (!g.is_pond_active) ? 1 : 2;

                // get the soil temperature factor
                double tf = (g.SoilCNParameterSet == "rothc") ? RothcTF(layer, index) : TF(layer, index);
                if (g.useNewSTFFunction)
                    if (g.useFactorsBySOMpool)
                    {
                        tf = SoilTempFactor(layer, index, g.TempFactorData_MinerSOM_Biom);
                    }
                    else
                    {
                        tf = SoilTempFactor(layer, index, g.TempFactorData_MinerSOM);
                    }

                // get the soil water factor
                double wf = WF(layer, index);
                if (g.useFactorsBySOMpool)
                    if (g.useNewSWFFunction)
                    {
                        wf = SoilMoistFactor(layer, index, g.MoistFactorData_MinerSOM_Biom);
                    }
                    else
                    {
                        wf = SoilMoistFactor(layer, index, g.MoistFactorData_MinerSOM);
                    }

                // get the rate of mineralisation of C & N from the biomass pool
                double dlt_n_min_biom = biom_n[layer] * g.rd_biom[index - 1] * tf * wf;       // why the calculation is on n while for hum is on C?
                double dlt_c_min_biom = dlt_n_min_biom * g.biom_cn;

                // distribute the carbon
                dlt_c_biom_2_hum[layer] = dlt_c_min_biom * g.ef_biom * (1.0 - g.fr_biom_biom);
                dlt_c_biom_2_atm[layer] = dlt_c_min_biom * (1.0 - g.ef_biom);

                // calculate net N mineralisation
                dlt_n_biom_2_min[layer] = dlt_n_min_biom - Utility.Math.Divide(dlt_c_biom_2_hum[layer], g.hum_cn, 0.0) - Utility.Math.Divide((dlt_c_min_biom - dlt_c_biom_2_atm[layer] - dlt_c_biom_2_hum[layer]), g.biom_cn, 0.0);
            }

            private void MineraliseFOM(int layer, out double[] dlt_c_biom, out double[] dlt_c_hum, out double[] dlt_c_atm, out double[] dlt_fom_n, out double dlt_n_min)
            {
                // + Purpose
                //     Calculate the daily transformation of the soil fresh organic matter pools, mineralisation (+ve) or immobilisation (-ve)

                dlt_c_hum = new double[3];
                dlt_c_biom = new double[3];
                dlt_c_atm = new double[3];
                dlt_fom_n = new double[3];
                dlt_n_min = 0.0;

                // dsg 200508  use different values for some constants when anaerobic conditions dominate
                // index = 1 for aerobic conditions, 2 for anaerobic conditions
                int index = (!g.is_pond_active) ? 1 : 2;

                // get total available mineral N (kg/ha)
                double nitTot = Math.Max(0.0, (_no3[layer] - g.no3_min[layer]) + (_nh4[layer] - g.nh4_min[layer]));

                // fresh organic carbon (kg/ha)
                double fomC = fom_c_pool1[layer] + fom_c_pool2[layer] + fom_c_pool3[layer];

                // fresh organic nitrogen (kg/ha)
                double fomN = fom_n_pool1[layer] + fom_n_pool2[layer] + fom_n_pool3[layer];

                // ratio of C in fresh OM to N available for decay
                double cnr = Utility.Math.Divide(fomC, fomN + nitTot, 0.0);

                // calculate the C:N ratio factor - Bound to [0, 1]
                double cnrf = Math.Max(0.0, Math.Min(1.0, Math.Exp(-g.cnrf_coeff * (cnr - g.cnrf_optcn) / g.cnrf_optcn)));
                if (g.useNewProcesses)
                    cnrf = CNratioFactor(layer, index, g.CNFactorMinerFOM_OptCN, g.CNFactorMinerFOM_RateCN);

                // get the soil temperature factor
                double tf = (g.SoilCNParameterSet == "rothc") ? RothcTF(layer, index) : TF(layer, index);
                if (g.useNewSTFFunction)
                    tf = SoilTempFactor(layer, index, g.TempFactorData_MinerSOM);

                // get the soil water factor
                double wf = WF(layer, index);
                if (g.useNewSWFFunction)
                    wf = SoilMoistFactor(layer, index, g.MoistFactorData_MinerSOM);

                // calculate gross amount of C & N released due to mineralisation of the fresh organic matter.
                if (fomC >= g.fom_min)
                {
                    double dlt_n_min_fom = 0.0; // amount of fresh organic N mineralised across fpools (kg/ha)
                    double dlt_c_min_fom = 0.0; // total C mineralised (kg/ha) summed across fpools
                    double[] dlt_n_min_tot = new double[3]; // amount of fresh organic N mineralised in each pool (kg/ha)
                    double[] dlt_c_min_tot = new double[3]; // amount of C mineralised (kg/ha) from each pool

                    // C:N ratio of fom
                    double fom_cn = Utility.Math.Divide(fomC, fomN, 0.0);

                    // get the decomposition of carbohydrate-like, cellulose-like and lignin-like fractions (fpools) in turn.
                    for (int fractn = 0; fractn < 3; fractn++)
                    {
                        // get the max decomposition rate for each fpool
                        double decomp_rate = FractRDFom(fractn)[index - 1] * cnrf * tf * wf;

                        // calculate the gross amount of fresh organic carbon mineralised (kg/ha)
                        double gross_c_decomp = decomp_rate * FractFomC(fractn)[layer];

                        // calculate the gross amount of N released from fresh organic matter (kg/ha)
                        double gross_n_decomp = decomp_rate * FractFomN(fractn)[layer];

                        dlt_n_min_fom += gross_n_decomp;
                        dlt_c_min_tot[fractn] = gross_c_decomp;
                        dlt_n_min_tot[fractn] = gross_n_decomp;
                        dlt_c_min_fom += gross_c_decomp;
                    }

                    // calculate potential transfers of C mineralised to biomass
                    double dlt_c_biom_tot = dlt_c_min_fom * g.ef_fom * g.fr_fom_biom;

                    // calculate potential transfers of C mineralised to humus
                    double dlt_c_hum_tot = dlt_c_min_fom * g.ef_fom * (1.0 - g.fr_fom_biom);

                    // test whether there is adequate N available to meet immobilisation demand
                    double n_demand = Utility.Math.Divide(dlt_c_biom_tot, g.biom_cn, 0.0) + Utility.Math.Divide(dlt_c_hum_tot, g.hum_cn, 0.0);
                    double n_avail = nitTot + dlt_n_min_fom;

                    // factor to reduce mineralisation rates if insufficient N to meet immobilisation demand
                    double Navail_factor = 1.0;
                    if (n_demand > n_avail)
                        Navail_factor = Math.Max(0.0, Math.Min(1.0, Utility.Math.Divide(nitTot, n_demand - dlt_n_min_fom, 0.0)));

                    // now adjust carbon transformations etc. and similarly for npools
                    for (int fractn = 0; fractn < 3; fractn++)
                    {
                        dlt_c_hum[fractn] = dlt_c_min_tot[fractn] * g.ef_fom * (1.0 - g.fr_fom_biom) * Navail_factor;
                        dlt_c_biom[fractn] = dlt_c_min_tot[fractn] * g.ef_fom * g.fr_fom_biom * Navail_factor;
                        dlt_c_atm[fractn] = dlt_c_min_tot[fractn] * (1.0 - g.ef_fom) * Navail_factor;
                        dlt_fom_n[fractn] = dlt_n_min_tot[fractn] * Navail_factor;

                        dlt_c_hum[fractn] = Utility.Math.RoundToZero(dlt_c_hum[fractn]);
                        dlt_c_biom[fractn] = Utility.Math.RoundToZero(dlt_c_biom[fractn]);
                        dlt_c_atm[fractn] = Utility.Math.RoundToZero(dlt_c_atm[fractn]);
                        dlt_fom_n[fractn] = Utility.Math.RoundToZero(dlt_fom_n[fractn]);
                    }

                    dlt_n_min = (dlt_n_min_fom - n_demand) * Navail_factor;
                }
            }

            private FOMdecompData MineraliseFOM1(int layer)
            {
                // + Purpose
                //     Calculate the daily transformation of the soil fresh organic matter pools, mineralisation (+ve) or immobilisation (-ve)

                double[] dlt_c_hum = new double[3];     // dlt_c from fom to humus
                double[] dlt_c_biom = new double[3];    // dlt_c from fom to biomass
                double[] dlt_c_atm = new double[3];     // dlt_c from fom to atmosphere
                double[] dlt_fom_n = new double[3];     // dlt_n from fom pools to OM
                double dlt_n_min = 0.0;                 // dlt_n from fom to mineral

                // dsg 200508  use different values for some constants when anaerobic conditions dominate
                // index = 1 for aerobic conditions, 2 for anaerobic conditions
                int index = (!g.is_pond_active) ? 1 : 2;

                // get total available mineral N (kg/ha)
                double nitTot = Math.Max(0.0, (_no3[layer] - g.no3_min[layer]) + (_nh4[layer] - g.nh4_min[layer]));

                // fresh organic carbon (kg/ha)
                double fomC = fom_c_pool1[layer] + fom_c_pool2[layer] + fom_c_pool3[layer];

                // fresh organic nitrogen (kg/ha)
                double fomN = fom_n_pool1[layer] + fom_n_pool2[layer] + fom_n_pool3[layer];

                // ratio of C in fresh OM to N available for decay
                double cnr = Utility.Math.Divide(fomC, fomN + nitTot, 0.0);

                // calculate the C:N ratio factor - Bound to [0, 1]
                double cnrf = Math.Max(0.0, Math.Min(1.0, Math.Exp(-g.cnrf_coeff * (cnr - g.cnrf_optcn) / g.cnrf_optcn)));
                if (g.useNewProcesses)
                    cnrf = CNratioFactor(layer, index, g.CNFactorMinerFOM_OptCN, g.CNFactorMinerFOM_RateCN);

                // get the soil temperature factor
                double tf = (g.SoilCNParameterSet == "rothc") ? RothcTF(layer, index) : TF(layer, index);
                if (g.useNewSTFFunction)
                    if (g.useSingleMinerFactors)
                    {
                        tf = SoilTempFactor(layer, index, g.TempFactorData_MinerSOM);
                    }
                    else
                    {
                        if (g.useFactorsByFOMpool)
                        {
                        }
                        else
                        {
                            tf = SoilTempFactor(layer, index, g.TempFactorData_MinerFOM);
                        }
                    }

                // get the soil water factor
                double wf = WF(layer, index);
                if (g.useNewSWFFunction)
                    if (g.useSingleMinerFactors)
                    {
                        wf = SoilMoistFactor(layer, index, g.MoistFactorData_MinerSOM);
                    }
                    else
                    {
                        if (g.useFactorsByFOMpool)
                        {
                        }
                        else
                        {
                            wf = SoilMoistFactor(layer, index, g.MoistFactorData_MinerFOM);
                        }
                    }

                // calculate gross amount of C & N released due to mineralisation of the fresh organic matter.
                if (fomC >= g.fom_min)
                {
                    double dlt_n_min_fom = 0.0; // amount of fresh organic N mineralised across fpools (kg/ha)
                    double dlt_c_min_fom = 0.0; // total C mineralised (kg/ha) summed across fpools
                    double[] dlt_n_min_tot = new double[3]; // amount of fresh organic N mineralised in each pool (kg/ha)
                    double[] dlt_c_min_tot = new double[3]; // amount of C mineralised (kg/ha) from each pool

                    // C:N ratio of fom
                    double fom_cn = Utility.Math.Divide(fomC, fomN, 0.0);

                    // get the decomposition of carbohydrate-like, cellulose-like and lignin-like fractions (fpools) in turn.
                    for (int fractn = 0; fractn < 3; fractn++)
                    {
                        // get the max decomposition rate for each fpool
                        double decomp_rate = FractRDFom(fractn)[index - 1] * cnrf * tf * wf;

                        // calculate the gross amount of fresh organic carbon mineralised (kg/ha)
                        double gross_c_decomp = decomp_rate * FractFomC(fractn)[layer];

                        // calculate the gross amount of N released from fresh organic matter (kg/ha)
                        double gross_n_decomp = decomp_rate * FractFomN(fractn)[layer];

                        dlt_n_min_fom += gross_n_decomp;
                        dlt_c_min_tot[fractn] = gross_c_decomp;
                        dlt_n_min_tot[fractn] = gross_n_decomp;
                        dlt_c_min_fom += gross_c_decomp;
                    }

                    // calculate potential transfers of C mineralised to biomass
                    double dlt_c_biom_tot = dlt_c_min_fom * g.ef_fom * g.fr_fom_biom;

                    // calculate potential transfers of C mineralised to humus
                    double dlt_c_hum_tot = dlt_c_min_fom * g.ef_fom * (1.0 - g.fr_fom_biom);

                    // test whether there is adequate N available to meet immobilisation demand
                    double n_demand = Utility.Math.Divide(dlt_c_biom_tot, g.biom_cn, 0.0) + Utility.Math.Divide(dlt_c_hum_tot, g.hum_cn, 0.0);
                    double n_avail = nitTot + dlt_n_min_fom;

                    // factor to reduce mineralisation rates if insufficient N to meet immobilisation demand
                    double Navail_factor = 1.0;
                    if (n_demand > n_avail)
                        Navail_factor = Math.Max(0.0, Math.Min(1.0, Utility.Math.Divide(nitTot, n_demand - dlt_n_min_fom, 0.0)));

                    // now adjust carbon transformations etc. and similarly for npools
                    for (int fractn = 0; fractn < 3; fractn++)
                    {
                        dlt_c_hum[fractn] = dlt_c_min_tot[fractn] * g.ef_fom * (1.0 - g.fr_fom_biom) * Navail_factor;
                        dlt_c_biom[fractn] = dlt_c_min_tot[fractn] * g.ef_fom * g.fr_fom_biom * Navail_factor;
                        dlt_c_atm[fractn] = dlt_c_min_tot[fractn] * (1.0 - g.ef_fom) * Navail_factor;
                        dlt_fom_n[fractn] = dlt_n_min_tot[fractn] * Navail_factor;

                        dlt_c_hum[fractn] = Utility.Math.RoundToZero(dlt_c_hum[fractn]);
                        dlt_c_biom[fractn] = Utility.Math.RoundToZero(dlt_c_biom[fractn]);
                        dlt_c_atm[fractn] = Utility.Math.RoundToZero(dlt_c_atm[fractn]);
                        dlt_fom_n[fractn] = Utility.Math.RoundToZero(dlt_fom_n[fractn]);
                    }

                    dlt_n_min = (dlt_n_min_fom - n_demand) * Navail_factor;
                }

                FOMdecompData Result = new FOMdecompData();
                Result.dlt_c_hum = dlt_c_hum;
                Result.dlt_c_biom = dlt_c_biom;
                Result.dlt_c_atm = dlt_c_atm;
                Result.dlt_fom_n = dlt_fom_n;
                Result.dlt_n_min = dlt_n_min;

                return Result;
            }

            /// <summary>
            /// Purpose: Calculate the amount of urea converted to NH4 via hydrolysis (kgN/ha)
            /// </summary>
            /// <remarks>
            /// + Assumptions:
            ///     - very small amounts of urea are hydrolysed promptly, regardless the hydrolysis approach
            ///     - the actual hydrolysis is computed in another method according to the approach chosen
            ///     - parameters are given in pairs, for aerobic and anaerobic conditions (with pond)
            /// </remarks>
            /// <documentation>
            /// 
            /// </documentation>
            /// <param name="layer">the node number representing soil layer for which calculations will be made</param>
            /// <returns>delta N coverted from urea into NH4</returns>
            private double UreaHydrolysis(int layer)
            {
                double result = 0.0;

                if (_urea[layer] > 0.0)
                {
                    // we have urea, so can do some hydrolysis
                    if (!g.useNewProcesses)
                    {
                        // using old APSIM-SoilN method
                        result = UreaHydrolysis_ApsimSoilN(layer);
                    }
                    else
                    {
                        // get the minimum urea amount we bother to calc hydrolysis
                        double LowUrea = 0.1 * g.dlayer[layer] / 200;
                        //  its original value was 0.1 kg/ha, assuming 'typical' thickness as 20cm it was 0.005

                        if (_urea[layer] < LowUrea)
                        {
                            // urea amount is too small, all will be hydrolised
                            result = _urea[layer];
                        }
                        else
                        {
                            switch (g.UreaHydrolysisApproach)
                            {
                                case UreaHydrolysisApproaches.APSIMdefault:
                                    // use default soilNitrogen function
                                    result = UreaHydrolysis_ApsimSoilNitrogen(layer);
                                    break;
                                case UreaHydrolysisApproaches.RCichota:
                                    // use function define by RCichota
                                    result = 0;
                                    break;
                                default:
                                    throw new Exception("Method for computing urea hydrolysis is not valid");
                            }
                        }
                    }
                }

                return result;
            }

            /// <summary>
            /// Purpose: Compute the hydrolysis of urea using the approach from APSIM-SoilN
            /// </summary>
            /// <remarks>
            /// + Assumptions:
            ///     - parameters are given in pairs, for aerobic and anaerobic conditions (with pond)
            /// </remarks>
            /// <documentation>
            /// This approach was used in APSIM-SoilN module, and has been adapted from CERES. See Godwin, D.C. and Jones, C.A. (1991). Nitrogen dynamics in
            ///  soil-plant systems. In: Hanks, J. and Ritchie, J.T. Modeling plant and soil systems. pp. 287-321.
            /// This has not been tested especifically in APSIM
            /// </documentation>
            /// <param name="layer">the node number representing soil layer for which calculations will be made</param>
            /// <returns>delta N coverted from urea into NH4</returns>
            private double UreaHydrolysis_ApsimSoilN(int layer)
            {

                double result;
                if (_urea[layer] < 0.1)
                    // urea amount is too small, all will be hydrolised
                    result = _urea[layer];
                else
                {
                    // get the index for aerobic/anaerobic conditions
                    int index = (!g.is_pond_active) ? 1 : 2;

                    // get the soil water factor
                    double swf = Math.Max(0.0, Math.Min(1.0, WF(layer, index) + 0.20));

                    // get the soil temperature factor
                    double stf = Math.Max(0.0, Math.Min(1.0, (g.Tsoil[layer] / 40.0) + 0.20));

                    // note (jngh) oc & ph are not updated during simulation
                    //      mep    following equation would be better written using oc(layer) = (hum_C(layer) + biom_C(layer))

                    // get potential fraction of urea for hydrolysis
                    double ak = -1.12 + 1.31 * g.OC_reset[layer] + 0.203 * g.ph[layer] - 0.155 * g.OC_reset[layer] * g.ph[layer];
                    ak = Math.Max(0.25, Math.Min(1.0, ak));

                    //get amount hydrolysed;
                    result = Math.Max(0.0, Math.Min(_urea[layer], ak * _urea[layer] * Math.Min(swf, stf)));
                }

                return result;
            }

            /// <summary>
            /// Purpose: Compute the hydrolysis of urea using the approach from APSIM-SoilNitrogen
            /// </summary>
            /// <remarks>
            /// + Assumptions:
            ///     - parameters are given in pairs, for aerobic and anaerobic conditions (with pond)
            /// </remarks>
            /// <documentation>
            /// This approach is an updated version of original used in APSIM-SoilN module. Initially based on CERES, see Godwin, D.C. and Jones, C.A. (1991).
            ///  Nitrogen dynamics in soil-plant systems. In: Hanks, J. and Ritchie, J.T. Modeling plant and soil systems. pp. 287-321.
            /// Major differences include renaming some of the variables and allowing paramater values to be changed by user. Also organic carbon is updated
            ///  at each time step.
            /// This has not been tested especifically in APSIM
            /// </documentation>
            /// <param name="layer">the node number representing soil layer for which calculations will be made</param>
            /// <returns>delta N coverted from urea into NH4</returns>
            private double UreaHydrolysis_ApsimSoilNitrogen(int layer)
            {
                // get the index for aerobic/anaerobic conditions
                int index = (!g.is_pond_active) ? 1 : 2;

                // get the soil water factor
                double swf = Math.Max(0.0, Math.Min(1.0, WF(layer, index) + 0.20));
                if (g.useNewSWFFunction)
                    swf = SoilMoistFactor(layer, index, g.MoistFactorData_UHydrol);

                // get the soil temperature factor
                double stf = Math.Max(0.0, Math.Min(1.0, (g.Tsoil[layer] / 40.0) + 0.20));
                if (g.useNewSTFFunction)
                    stf = SoilTempFactor(layer, index, g.TempFactorData_UHydrol);

                // get the total C amount
                double totalC = g.OC_reset[layer];
                if (g.useNewProcesses)
                    totalC = (hum_c[layer] + biom_c[layer]) * convFactor_kgha2ppm(layer) / 10000;  // (100/1000000) = convert to ppm and then to %
                // RCichota: why not FOM?

                // get potential fraction of urea for hydrolysis
                double ak = g.potHydrol_parmA +
                        g.potHydrol_parmB * totalC +
                        g.potHydrol_parmC * g.ph[layer] +
                        g.potHydrol_parmD * totalC * g.ph[layer];
                ak = Math.Max(g.potHydrol_min, Math.Min(1.0, ak));
                //original eq.: double ak = Math.Max(0.25, Math.Min(1.0, -1.12 + 1.31 * OC_reset[layer] + 0.203 * g.ph[layer] - 0.155 * OC_reset[layer] * g.ph[layer]));

                //get amount N hydrolysed;
                double result = Math.Max(0.0, Math.Min(_urea[layer], ak * _urea[layer] * Math.Min(swf, stf)));
                return result;
            }

            /// <summary>
            /// Calculate the amount of NH4 converted to NO3 via nitrification
            /// </summary>
            /// <remarks>
            /// - This routine is much simplified from original CERES code
            /// - pH effect on nitrification is not invoked
            /// - The actual nitrification is computed in another method according to the approach chosen
            /// </remarks>
            /// <param name="layer">the node number representing soil layer for which calculations will be made</param>
            /// <returns>delta N coverted from NH4 into NO3</returns>
            private double Nitrification(int layer)
            {
                double result = 0.0;
                if (_nh4[layer] > g.nh4_min[layer])
                {
                    // we have ammonium, so can do some nitrification
                    if (!g.useNewProcesses)
                    {
                        // using old APSIM-SoilN method
                        result = Nitrification_ApsimSoilN(layer);
                    }
                    else
                    {
                        switch (g.NitrificationApproach)
                        {
                            case NitrificationApproaches.APSIMdefault:
                                // use default soilNitrogen function
                                result = Nitrification_ApsimSoilNitrogen(layer);
                                break;
                            case NitrificationApproaches.RCichota:
                                // use RCichota function
                                result = 0.0;
                                break;
                            default:
                                throw new Exception("Method for computing nitrification is not valid");
                        }
                    }
                }

                // check the actual nitrification rate (make sure NH4 will not go below minimum value)
                result = Math.Max(0.0, Math.Min(result, _nh4[layer] - g.nh4_min[layer]));

                return result;
            }

            /// <summary>
            /// Calculate the amount of NH4 converted to NO3 via nitrification
            /// </summary>
            /// <remarks>
            /// This approach was used in APSIM-SoilN module
            /// - parameters are given in pairs, for aerobic and anaerobic conditions (with pond)
            /// </remarks>
            /// <param name="layer">the node number representing soil layer for which calculations will be made</param>
            /// <returns>delta N coverted from NH4 into NO3</returns>
            private double Nitrification_ApsimSoilN(int layer)
            {
                // dsg 200508  use different values for some constants when anaerobic conditions dominate
                // index = 1 for aerobic and 2 for anaerobic conditions
                int index = (!g.is_pond_active) ? 1 : 2;

                // get the soil ph factor
                double phf = pHFNitrf(layer);

                // get the soil  water factor
                double swf = WFNitrf(layer, index);

                // get the soil temperature factor
                double stf = TF(layer, index);

                // get most limiting factor
                double pni = Math.Min(Math.Min(stf, swf), phf);
                // NOTE: factors to adjust rate of nitrification are used combined, with phf removed to match CERES v1

                // calculate the optimum nitrification rate (ppm)
                double nh4_ppm = _nh4[layer] * convFactor_kgha2ppm(layer);
                double opt_nitrif_rate_ppm = Utility.Math.Divide(g.nitrification_pot * nh4_ppm, nh4_ppm + g.nh4_at_half_pot, 0.0);

                // convert the optimum nitrification rate (kgN/ha)
                double opt_nitrif_rate = Utility.Math.Divide(opt_nitrif_rate_ppm, convFactor_kgha2ppm(layer), 0.0);

                // calculate the actual nitrification rate (after limiting factor and inhibition)
                double actual_nitrif_rate = opt_nitrif_rate * pni * Math.Max(0.0, 1.0 - g.InhibitionFactor_Nitrification[layer]);
                // Changes by VOS 13 Dec 09, Reviewed by RCichota (9/02/2010). Adding nitrification inhibiton

                //dlt_nh4_dnit[layer] = actual_nitrif_rate * dnit_nitrf_loss;
                //effective_nitrification[layer] = actual_nitrif_rate - dlt_nh4_dnit[layer];
                //n2o_atm[layer] += dlt_nh4_dnit[layer];

                return actual_nitrif_rate;
            }

            /// <summary>
            /// Calculate the amount of NH4 converted to NO3 via nitrification
            /// </summary>
            /// <remarks>
            /// This approach is default of APSIM-SoilNitrogen module - includes nitrification inhibition
            /// - parameters are given in pairs, for aerobic and anaerobic conditions (with pond)
            /// </remarks>
            /// <param name="layer">the node number representing soil layer for which calculations will be made</param>
            /// <returns>delta N coverted from NH4 into NO3</returns>
            private double Nitrification_ApsimSoilNitrogen(int layer)
            {
                // get the index for aerobic/anaerobic conditions
                int index = (!g.is_pond_active) ? 1 : 2;

                // get the soil ph factor
                double phf = pHFNitrf(layer);
                if (g.useNewProcesses)
                    phf = SoilpHFactor(layer, index, g.pHFactorData_Nitrif);

                // get the soil  water factor
                double swf = WFNitrf(layer, index);
                if (g.useNewSWFFunction)
                    swf = SoilMoistFactor(layer, index, g.MoistFactorData_Nitrif);

                // get the soil temperature factor
                double stf = TF(layer, index);
                if (g.useNewSTFFunction)
                    stf = SoilTempFactor(layer, index, g.TempFactorData_Nitrif);

                // calculate the optimum nitrification rate (ppm)
                double nh4_ppm = _nh4[layer] * convFactor_kgha2ppm(layer);
                double opt_nitrif_rate_ppm = Utility.Math.Divide(g.nitrification_pot * nh4_ppm, nh4_ppm + g.nh4_at_half_pot, 0.0);

                // calculate the optimum nitrification rate (kgN/ha)
                double opt_nitrif_rate = Utility.Math.Divide(opt_nitrif_rate_ppm, convFactor_kgha2ppm(layer), 0.0);

                // calculate the actual nitrification rate (after limiting factors and inhibition)
                double actual_nitrif_rate = opt_nitrif_rate * Math.Min(swf, Math.Min(stf, phf))
                    * Math.Max(0.0, 1.0 - g.InhibitionFactor_Nitrification[layer]);

                return actual_nitrif_rate;
            }

            /// <summary>
            /// Calculate the amount of N2O produced during nitrification
            /// </summary>
            /// <param name="layer">the soil layer index for which calculations will be made</param>
            /// <returns>delta N coverted into N2O during nitrification</returns>
            /// <returns></returns>
            private double N2OLostInNitrification_ApsimSoilNitrogen(int layer)
            {
                double result = dlt_nitrification[layer] * g.dnit_nitrf_loss;
                return result;
            }

            /// <summary>
            /// Calculate amount of NO3 transformed via denitrification
            /// </summary>
            /// <remarks>
            /// - The actual denitrification is computed in another method according to the approach chosen
            /// </remarks>
            /// <param name="layer">the soil layer index for which calculations will be made</param>
            /// <returns>delta N coverted from NO3 into gaseous forms</returns>
            private double Denitrification(int layer)
            {
                // Notes:
                //     Denitrification will happend whenever: 
                //         - the soil water in the layer > the drained upper limit (Godwin et al., 1984),
                //         - the NO3 nitrogen concentration > 1 mg N/kg soil,
                //         - the soil temperature >= a minimum temperature.

                // + Assumptions
                //     That there is a root system present.  Rolston et al. say that the denitrification rate coeffficient (dnit_rate_coeff) of non-cropped
                //       plots was 0.000168 and for cropped plots 3.6 times more (dnit_rate_coeff = 0.0006). The larger rate coefficient was required
                //       to account for the effects of the root system in consuming oxygen and in adding soluble organic C to the soil.

                //+  Notes
                //       Reference: Rolston DE, Rao PSC, Davidson JM, Jessup RE (1984). "Simulation of denitrification losses of Nitrate fertiliser applied
                //        to uncropped, cropped, and manure-amended field plots". Soil Science Vol 137, No 4, pp 270-278.
                //
                //       Reference for Carbon availability factor: Reddy KR, Khaleel R, Overcash MR (). "Carbon transformations in land areas receiving 
                //        organic wastes in relation to nonpoint source pollution: A conceptual model".  J.Environ. Qual. 9:434-442.


                double result = 0.0;
                if (_no3[layer] > g.no3_min[layer])
                {
                    // we have nitrate, so can do some denitrification
                    if (!g.useNewProcesses)
                    {
                        // using old APSIM-SoilN method
                        result = Denitrification_ApsimSoilN(layer);
                    }
                    else
                    {
                        switch (g.DenitrificationApproach)
                        {
                            case DenitrificationApproaches.APSIMdefault:
                                // use default soilNitrogen function
                                result = Denitrification_ApsimSoilNitrogen(layer);
                                break;
                            case DenitrificationApproaches.RCichota:
                                // use RCichota function
                                result = 0.0;
                                break;
                            default:
                                throw new Exception("Method for computing denitrification is not valid");
                        }
                    }
                }

                // prevent no3 from falling below NO3_min
                result = Math.Max(0.0, Math.Min(result, _no3[layer] - g.no3_min[layer]));

                return result;
            }

            /// <summary>
            /// Calculate amount of NO3 transformed via denitrification
            /// </summary>
            /// <remarks>
            /// This approach was used in APSIM-SoilN module
            /// - parameters are given in pairs, for aerobic and anaerobic conditions (with pond)
            /// </remarks>
            /// <param name="layer">the soil layer index for which calculations will be made</param>
            /// <returns>delta N coverted from NO3 into gaseous forms</returns>
            private double Denitrification_ApsimSoilN(int layer)
            {
                // + Purpose
                //     Calculate the amount of N2O produced during denitrification

                //+  Purpose
                //     Calculate amount of NO3 transformed via denitrification.
                //       Will happend whenever: 
                //         - the soil water in the layer > the drained upper limit (Godwin et al., 1984),
                //         - the NO3 nitrogen concentration > 1 mg N/kg soil,
                //         - the soil temperature >= a minimum temperature.

                // + Assumptions
                //     That there is a root system present.  Rolston et al. say that the denitrification rate coeffficient (dnit_rate_coeff) of non-cropped
                //       plots was 0.000168 and for cropped plots 3.6 times more (dnit_rate_coeff = 0.0006). The larger rate coefficient was required
                //       to account for the effects of the root system in consuming oxygen and in adding soluble organic C to the soil.

                //+  Notes
                //       Reference: Rolston DE, Rao PSC, Davidson JM, Jessup RE (1984). "Simulation of denitrification losses of Nitrate fertiliser applied
                //        to uncropped, cropped, and manure-amended field plots". Soil Science Vol 137, No 4, pp 270-278.
                //
                //       Reference for Carbon availability factor: Reddy KR, Khaleel R, Overcash MR (). "Carbon transformations in land areas receiving 
                //        organic wastes in relation to nonpoint source pollution: A conceptual model".  J.Environ. Qual. 9:434-442.
                // make sure no3 will not go below minimum

                // get available carbon from soil organic pools
                double totalC = (hum_c[layer] + fom_c[layer]) * convFactor_kgha2ppm(layer);
                double active_c = 0.0031 * totalC + 24.5;
                // Note: CM V2 had active_c = fom_C_conc + 0.0031*hum_C_conc + 24.5
                // Note: Ceres wheat has active_c = 0.4* fom_C_pool1 + 0.0031 * 0.58 * hum_C_conc + 24.5

                // get the soil water factor
                double swf = WFDenit(layer);

                // get the soil temperature factor
                double stf = Math.Max(0.0, Math.Min(1.0, 0.1 * Math.Exp(0.046 * g.Tsoil[layer])));
                // This is an empirical dimensionless function to account for the effect of temperature.
                // The upper limit of 1.0 means that optimum denitrification temperature is 50 oC and above.  At 0 oC it is 0.1 of optimum, and at -20 oC is about 0.04.

                // calculate denitrification rate  - kg/ha
                double result = g.dnit_rate_coeff * active_c * swf * stf * _no3[layer];

                return result;
            }

            /// <summary>
            /// Calculate amount of NO3 transformed via denitrification
            /// </summary>
            /// <remarks>
            /// This approach is default of APSIM-SoilNitrogen module
            /// - parameters are given in pairs, for aerobic and anaerobic conditions (with pond)
            /// </remarks>
            /// <param name="layer">the soil layer index for which calculations will be made</param>
            /// <returns>delta N coverted from NO3 into gaseous forms</returns>
            private double Denitrification_ApsimSoilNitrogen(int layer)
            {
                // + Purpose
                //     Calculate the amount of N2O produced during denitrification

                //+  Purpose
                //     Calculate amount of NO3 transformed via denitrification.
                //       Will happend whenever: 
                //         - the soil water in the layer > the drained upper limit (Godwin et al., 1984),
                //         - the NO3 nitrogen concentration > 1 mg N/kg soil,
                //         - the soil temperature >= a minimum temperature.

                // + Assumptions
                //     That there is a root system present.  Rolston et al. say that the denitrification rate coeffficient (dnit_rate_coeff) of non-cropped
                //       plots was 0.000168 and for cropped plots 3.6 times more (dnit_rate_coeff = 0.0006). The larger rate coefficient was required
                //       to account for the effects of the root system in consuming oxygen and in adding soluble organic C to the soil.

                //+  Notes
                //       Reference: Rolston DE, Rao PSC, Davidson JM, Jessup RE (1984). "Simulation of denitrification losses of Nitrate fertiliser applied
                //        to uncropped, cropped, and manure-amended field plots". Soil Science Vol 137, No 4, pp 270-278.
                //
                //       Reference for Carbon availability factor: Reddy KR, Khaleel R, Overcash MR (). "Carbon transformations in land areas receiving 
                //        organic wastes in relation to nonpoint source pollution: A conceptual model".  J.Environ. Qual. 9:434-442.
                // make sure no3 will not go below minimum

                // get water-soluble organic carbon, readily available for soil microbes
                double WaterSolubleCarbon = g.actC_parmA + g.actC_parmB * (hum_c[layer] + fom_c_pool1[layer] + fom_c_pool2[layer] + fom_c_pool3[layer]) * convFactor_kgha2ppm(layer);

                int index = 1; // denitrification calcs are not different whether there is pond or not. use 1 as default
                // get the soil water factor
                double swf = WFDenit(layer);
                if (g.useNewSWFFunction)
                    swf = SoilMoistFactor(layer, index, g.MoistFactorData_Denit);

                // get the soil temperature factor
                double stf = Math.Max(0.0, Math.Min(1.0, 0.1 * Math.Exp(0.046 * g.Tsoil[layer])));
                if (g.useNewSTFFunction)
                    stf = SoilTempFactor(layer, index, g.TempFactorData_Denit);

                // calculate denitrification rate - kg/ha
                double result = g.dnit_rate_coeff * WaterSolubleCarbon * swf * stf * _no3[layer];

                // prevent no3 from falling below NO3_min
                result = Math.Max(0.0, Math.Min(result, _no3[layer] - g.no3_min[layer]));

                return result;
            }

            /// <summary>
            /// Calculate the N2 to N2O ratio during denitrification
            /// </summary>
            /// <remarks>
            /// - parameters are given in pairs, for aerobic and anaerobic conditions (with pond)
            /// </remarks>
            /// <param name="layer">the soil layer index for which calculations will be made</param>
            /// <returns>The ratio between N2 and N2O (0-1)</returns>
            private double Denitrification_Nratio(int layer)
            {
                int index = 1; // denitrification calcs are not different whether there is pond or not. use 1 as default

                // the water filled pore space (%)
                double WFPS = g.sw_dep[layer] / g.sat_dep[layer] * 100.0;

                // CO2 production today (kgC/ha)
                double CO2_prod = (dlt_c_fom_2_atm[0][layer] + dlt_c_fom_2_atm[1][layer] + dlt_c_fom_2_atm[2][layer]
                    + dlt_c_biom_2_atm[layer] + dlt_c_hum_2_atm[layer]);

                // calculate the terms for the formula from Thornburn et al (2010)
                double RtermA = g.N2N2O_parmA * g.dnit_k1;
                double RtermB = 0.0;
                if (CO2_prod > 0.0)
                    RtermB = g.dnit_k1 * Math.Exp(g.N2N2O_parmB * (_no3[layer] / CO2_prod));
                double RtermC = 0.1;
                bool didInterpolate;
                double RtermD = Utility.Math.LinearInterpReal(WFPS, g.dnit_wfps, g.dnit_n2o_factor, out didInterpolate);
                // RTermD = (0.015 * WFPS) - 0.32;

                double result = Math.Max(RtermA, RtermB) * Math.Max(RtermC, RtermD);

                double nco2f = 0.0;
                double wfpsf = 0.0;

                if (g.useNewProcesses)
                {
                    nco2f = Math.Max(g.N2N2O_parmA, Math.Exp(g.N2N2O_parmB * (_no3[layer] / CO2_prod)));
                    wfpsf = WaterFilledPoreSpaceFactor(layer, index, g.WFPSFactorData_N2N2O);
                    result = g.dnit_k1 * nco2f * wfpsf;
                }

                return result;
            }

            #region N2O alternative routines, mergerd 15 Nov 2012 FLi

            private double Nitrification_WNMM(int layer)
            {
                // Calculates nitrification of NH4 in a given soil layer as WNMM.
                // Sub-Program Arguments

                double nRate;                 // rate of nitrification
                double phf;                   // g_ph factor
                double pni;                   // potential nitrification index (0-1)
                double nh4_avail;             // available ammonium (kg/ha)
                double tf;                    // temperature factor (0-1)
                double wfd;                   // water factor (0-1)

                //const double alpha = wnmm_n_alpha;   // maximum fraciton of nitrification rate as n2o  

                //pH effects:  
                phf = 1.0;
                if (g.ph[layer] < 7.0) phf = 0.307 * g.ph[layer] - 1.269;
                if (g.ph[layer] > 7.4) phf = 5.367 - 0.599 * g.ph[layer];

                // water effects:        
                wfd = 1.0;                      //when sw25 <= sw < dul 
                double sw25 = 0.25 * (g.dul_dep[layer] - g.ll15_dep[layer]);     // confirm?
                if (g.sw_dep[layer] > g.dul_dep[layer])
                    wfd = 1.0 - (g.sw_dep[layer] - g.dul_dep[layer]) / (g.sat_dep[layer] - g.dul_dep[layer]);
                if (g.sw_dep[layer] < sw25)
                    wfd = (g.sw_dep[layer] - g.ll15_dep[layer]) / (sw25 - g.ll15_dep[layer]);

                // soil temperature effects
                tf = Math.Max(0.0, 0.41 * (g.Tsoil[layer] - 5.0) / 10.0);

                // use a combined index to adjust rate of nitrification
                pni = wfd * tf * phf;

                // get actual rate of nitrification for layer
                nRate = _nh4[layer] * (1 - Math.Exp(-pni));            //Unit: kg N/Ha

                // Inhibitor - no more enabled?
                // nRate *= Math.Max(0.0, 1.0 - _nitrification_inhibition[layer]);

                // Booundary check
                nh4_avail = Math.Max(_nh4[layer] - g.nh4_min[layer], 0.0);
                nRate = Math.Max(0.0, Math.Min(nh4_avail, nRate));

                // n2o
                double fTemp = 0.1 + 0.9 * (g.Tsoil[layer] / (g.Tsoil[layer] + Math.Exp(9.93 - 0.312 * g.Tsoil[layer])));
                dlt_nh4_dnit[layer] = g.wnmm_n_alpha * nRate * wfd * fTemp;        // alpha is 'dnit_nitrf_loss';
                effective_nitrification[layer] = nRate - dlt_nh4_dnit[layer];
                n2o_atm[layer] += dlt_nh4_dnit[layer];

                return nRate;
            }

            //N2O alternatives--------------------------------------------------------------
            private double Nitrification_CENT(int layer)
            {
                // layer            //input: soil layer count
                // return           //return: nh4->no3 and &
                // n2o_atm          //must calc: N2O production 

                //+  Purpose:  
                //  1) Calculates nitrification of NH4 in a given soil layer
                //      using the approach in DayCent, based on the 'Nitrify' 
                //      process in DayCent as in 2006; 
                //  2) Need to decide weather this process will produce N2O     
                //     FLi 11-April-2011

                //find way to get this parameters in code, set a temperary average    
                double surfTempAvg = g.cent_n_soilt_ave;  // 15.0  --  average soil surface temperature (deg C)
                double maxt = g.cent_n_maxt_ave;   // 25.0  --  Long term avg max monthly air temp of the hottest month (deg C)
                double avgWFPS = g.cent_n_wfps_ave;   // 0.7	  --  avg wfps in top nitrifyDepth cm of soil (0-1)
                double pHLayer = g.ph[layer];              // 6.0   --   pH of the soil layer

                //soil_texture      
                const int COARSE = 1;
                const int MEDIUM = 2;
                const int FINE = 3;
                const int VERYFINE = 4;
                int textureIndex = (int)g.SoilTextureID[layer];             // default: fine-medium. Only distinguish "coarse" and others         

                //***** Following block calculation using unit of 'gN/m2' as in DayCent          
                double MaxNitrifRate = g.cent_n_max_rate; //default = 0.10, max fraction of ammonium to NO3 during nitrification (gN/m^2)
                const double Ha_to_SqM = 0.0001;	    // factor to convert ha to sq meters
                const double kgHa_to_gM2 = 0.1;        // @ 1 kg/ha = 0.1 g/m2
                const double min_ammonium = 0.015; 	    // min. total ammonium in soil (gN/m^2)  @ not nh4_min[layer]; 0.15

                double NH4_to_NO3 = 0.0;		        // amount of NH4 converted to NO3 due to nitrification (gN/m^2)
                double ammonium = _nh4[layer] * kgHa_to_gM2; 		  //convert layer nh4 into ammonium (gN/m^2)  
                if (ammonium >= min_ammonium)
                {
                    //  Compute the effect of wfps on Nitrification (0-1)
                    double a, b, c, d;
                    switch (textureIndex)
                    {
                        case COARSE:
                            a = 0.5;
                            b = 0.0;
                            c = 1.5;
                            d = 4.5;
                            break;

                        case FINE:
                        case VERYFINE:
                        case MEDIUM:
                        default:
                            a = 0.65;
                            b = 0.0;
                            c = 1.2;
                            d = 2.5;
                            break;
                    }
                    double base1 = ((avgWFPS - b) / (a - b));
                    double base2 = ((avgWFPS - c) / (a - c));
                    double e1 = d * ((b - a) / (a - c));
                    double e2 = d;
                    double fNwfps = Math.Pow(base1, e1) * Math.Pow(base2, e2);

                    //  Compute temperature effect on Nitrification (0-1)
                    double A0 = maxt;                // A0-A4 are parameters to parton-innis functions
                    double A1 = -5.0;
                    double A2 = 4.5;
                    double A3 = 7.0;
                    double tmp1 = (A1 - surfTempAvg) / (A1 - A0);
                    double tmp2 = 1 - Math.Pow(tmp1, A3);
                    double tmp3 = Math.Pow(tmp1, A2);
                    double fNsoilt = 0;
                    if (tmp1 > 0 && A1 != A0)
                        fNsoilt = Math.Exp(A2 * tmp2 / A3) * tmp3;

                    //  Compute pH effect on nitrification
                    const double AA0 = 5.0;
                    const double AA1 = 0.56;
                    const double AA2 = 1.0;
                    const double AA3 = 0.45;
                    double fNph = AA1 + (AA2 / Math.PI) * Math.Atan(Math.PI * AA3 * (pHLayer - AA0));

                    // Ammonium that goes to nitrate during nitrification.
                    const double base_flux = 0.1 * Ha_to_SqM;	                //convert into 0.1 gN/ha/day
                    NH4_to_NO3 = ammonium * MaxNitrifRate * fNph * fNwfps * fNsoilt + base_flux;
                    /* alternative, was in LUCI1 for reference
                    double sitepar_Ncoeff = 0.03;
                    double abiotic = Math.Max(fNwfps * fNsoilt, sitepar_Ncoeff);
                    NH4_to_NO3 = ammonium * MaxNitrifRate * fNph * abiotic + base_flux;
                    */

                    // Effects of inhibitor - disabled 
                    // NH4_to_NO3 *= Math.Max(0.0, 1.0 - _nitrification_inhibition[layer]);

                    // Do not decrease below minimum NH4
                    if ((ammonium - NH4_to_NO3) > min_ammonium)
                        ammonium -= NH4_to_NO3;
                    else
                        NH4_to_NO3 = 0.0;
                    //***** End of block using unit gN/m2
                }

                //Convert back to unit: kg/ha, and use same approach of APSIM in estimating N2O produciton 
                double result = NH4_to_NO3 / kgHa_to_gM2;   //change back to unit: kg/ha

                dlt_nh4_dnit[layer] = result * g.dnit_nitrf_loss;
                effective_nitrification[layer] = result - dlt_nh4_dnit[layer];
                n2o_atm[layer] += dlt_nh4_dnit[layer];

                return result;
            }

            //N2O alternatives--------------------------------------------------------------
            private double Denitrification_CENT(int layer)
            {
                //layer                     Soil layer counter
                //n2o_atm                   // calc: n2o_atm,  - kg/ha/day
                //dlt_dnR                   // total denitrificaiton - kg/ha/day
                //  Calculates denitrification using the approach of DayCent as in 2006, Frank Li

                // constants
                // min. nitrate concentration required in a layer for trace gas calc. (ppm N)
                double minNitratePPM = 0.1;
                // min. allowable nitrate per laye at end of day (ppm N)
                //double minNitratePPM_final = 0.05;

                //if (_no3ppm[layer] < minNitratePPM)
                double _no3ppm = no3[layer] * convFactor_kgha2ppm(layer);
                if (_no3ppm < minNitratePPM)
                {
                    n2o_atm[layer] = 0.0;
                    return 0.0;
                }
                //Note : sat, dul, ll15, and sw are water content fraction (mm/mm) in a layer
                //       sat_dep, dul_dep, ll15_dep and sw_dep are water content (mm) in a layer     
                double ll15 = g.ll15_dep[layer] / g.dlayer[layer];
                double dul = g.dul_dep[layer] / g.dlayer[layer];
                double sat = g.sat_dep[layer] / g.dlayer[layer];
                double sw = g.sw_dep[layer] / g.dlayer[layer];


                // normalized diffusivity in aggregate soil media, at a standard field capacity (0-1) why
                //  water filled pore space at field capacity (0-1)
                double wfps_fc = dul / sat;                         // dul = field capacity, sat = porosity;
                double dD0_fc = diffusivity(layer, dul);            // original code calculats diffusivity when sw = dul (i.e. at fieled capacity) 
                // why not use actual sw.
                // water filled pore space threshold (0-1)
                double WFPS_threshold = (dD0_fc >= 0.15) ? 0.80 : (dD0_fc * 250.0 + 43.0) / 100.0;
                double layerWFPS = sw / sat;

                // CO2 correction factor when WFPS has reached threshold
                double co2ppm = (dlt_c_fom_2_atm[0][layer] + dlt_c_fom_2_atm[1][layer] + dlt_c_fom_2_atm[2][layer] +
                             dlt_c_biom_2_atm[layer] + dlt_c_hum_2_atm[layer]) /
                              (g.bd[layer] * g.dlayer[layer]) * 100.0;                          // ppm

                double co2ppm_correction = co2ppm;
                if (layerWFPS > WFPS_threshold)
                {
                    double a = (dD0_fc >= 0.15) ? 0.004 : (-0.1 * dD0_fc + 0.019);
                    co2ppm_correction = co2ppm * (1.0 + a * (layerWFPS - WFPS_threshold) * 100.0);
                }

                //  denitrification flux due to soil nitrate (ppm N/day), Del Grosso et. al, GBC.
                //parameters to parton-innis functions
                double AA0 = 9.23;
                double AA1 = 1.556;
                double AA2 = 76.91;
                double AA3 = 0.00222;
                //double fDno3 = Math.Max(0.0, f_arctangent(nitratePPM(layer), A));   //no3_N ppm or no3_ppm?
                double fDno3 = AA1 + (AA2 / Math.PI) * Math.Atan(Math.PI * AA3 * (_no3ppm - AA0));
                fDno3 = Math.Max(0.0, fDno3);

                //  fDco2 (ppm N) Del Grosso et. al, GBC,
                //  denitrification flux due to CO2 concentration (ppm N/day)
                double fDco2 = Math.Max(0.0, (0.1 * Math.Pow(co2ppm_correction, 1.3) - minNitratePPM));

                // wfps effect (fDwfps, 0-1?) Del Grosso et. al, GBC
                // The x_inflection calculation should take into account the corrected CO2 concentration, 
                double M = Math.Min(0.113, dD0_fc) * (-1.25) + 0.145;
                double x_inflection = 9.0 - M * co2ppm_correction;
                double fDwfps = Math.Max(0.0, 0.45 + (Math.Atan(0.6 * Math.PI * (10.0 * layerWFPS - x_inflection))) / Math.PI);

                //Total Denitrification
                double fluxTotalDenitPPM = (fDno3 < fDco2) ? fDno3 : fDco2;  // total (N2+N2O) denitrif. flux of the layer (ppm N/day)
                fluxTotalDenitPPM = Math.Max(0.066, fluxTotalDenitPPM);
                // Minimum value for potential denitrification in simulation layer (0.066)
                // To Do: consider - adjust constant 0.066 for change in sim. depth?
                fluxTotalDenitPPM *= fDwfps;    //wfps effects
                double dlt_dnR = fluxTotalDenitPPM * (g.bd[layer] * g.dlayer[layer]) / 100.0;            //total denitrification: ppm ->kg/ha

                //  Nitrate effect on the ratio of N2 to N2O,  Del Grosso et. al, GBC
                double k1 = Math.Max(1.5, 38.4 - 350 * dD0_fc);
                double fRno3_co2 = Math.Max(0.16 * k1, k1 * Math.Exp(-0.8 * _no3ppm / co2ppm));

                //  WFPS effect on the N2/N2O Ratio Del Grosso et. al, GBC
                double fRwfps = Math.Max(0.1, 0.015 * layerWFPS * 100.0 - 0.32);

                double ratioN2N2O = Math.Max(0.1, fRno3_co2 * fRwfps);	// N2:N2O Ratio
                n2o_atm[layer] = dlt_dnR / (ratioN2N2O + 1.0);

                //Reduce nitrate in soil outside of this  funciton after return
                return dlt_dnR;
            }

            // ----------------------------------------------------------------------------
            //	Function: Estimates normalized diffusivity in soils, called by denitrification_CENT.
            //	Returns the normalized diffusivity in aggregate soil media, units 0-1:
            //	ratio of gas diffusivity through soil to gas diffusivity through air
            //	at optimum water content. Reference: Millington and Shearer (1971) Soil Science 
            //	Davidson, E.A. and S.E. Trumbore (1995).
            //* If pore space is saturated, then diffusivity = 0//	
            // ----------------------------------------------------------------------------
            //  As in DayCent 2006, used by denitrification_DayCent, FrankLi
            private double diffusivity(int layer)
            {
                double sw = g.sw_dep[layer] / g.dlayer[layer];      //fraction: soil water content (mm/mm)
                return diffusivity(layer, sw);
            }

            private double diffusivity(int layer, double sw)   //for specified water content, not current content
            {
                double dul = g.dul_dep[layer] / g.dlayer[layer];    //fraction: soil water content (mm/mm)
                double sat = g.sat_dep[layer] / g.dlayer[layer];    //fraction: soil water content (mm/mm)

                double dDO = 0;
                // volumetric air content fraction
                double vac = Math.Min(1.0, Math.Max(0.0, sat - sw));
                if (vac > 0.0)	//unsaturated
                {
                    // volumetric water content of the soil bed volume
                    double theta_V = sw;

                    // volumeetric water content per unit bed volume in inter-aggregate pore space ( of > DUL )
                    double theta_P = 0;
                    if (sw > dul) theta_P = sw - dul;

                    // volumetric water content per unit bed volume in intra-aggregate pore space (of < DUL)
                    double theta_A = sw;
                    if (sw > dul) theta_A = dul;

                    // fractional liquid saturation of the A component of total pore volume [<DUL]
                    double s_wat = Math.Min(1.0, theta_A / dul);

                    // fractional liquid saturation of the P component of total pore volume [>DUL]
                    double sw_p = Math.Min(1.0, theta_P / (sat - dul));

                    double A = dul;
                    double porosity = sat;

                    double tp1, tp2, tp3, tp4, tp5, tp6, tp7, tp8;          // intermediate variables
                    if (1.0 - s_wat > 0.0)
                        tp1 = Math.Pow((1.0 - s_wat), 2.0);
                    else
                        tp1 = 0.0;

                    tp2 = (A - theta_A) / (A + (1.0 - porosity));
                    if (tp2 > 0.0)
                        tp3 = Math.Pow(tp2, (0.5 * tp2 + 1.16));
                    else
                        tp3 = 0.0;

                    tp4 = 1.0 - Math.Pow(vac, (0.5 * vac + 1.16));
                    tp5 = vac - theta_P;
                    if (tp5 > 0.0)
                        tp6 = Math.Pow(tp5, (0.5 * tp5 + 1.16));
                    else
                        tp6 = 0.0;

                    tp7 = Math.Pow((1.0 - sw_p), 2.0);

                    tp8 = Math.Max(0.0, ((tp1 * tp3 * tp4 * (tp5 - tp6)) /
                            (1.0E-6 + (tp1 * tp3 * tp4) + tp5 - tp6) * 1.0E7));

                    // normalized diffusivity in aggregate soil media (0-1)
                    dDO = Math.Max(0.0, (tp8 / 1.0E7 + tp7 * tp6));
                }
                return dDO;
            }

            //N2O alternatives--------------------------------------------------------------
            private double Denitrification_NEMIS(int layer)
            {
                //+  Sub-Program Arguments
                //      snRate              // (OUTPUT) denitrification rate    - kg/ha/day
                //      n2o_atm             //  N2O emission 

                //+  Purpose
                //      Calculates denitrification using NEMIS approach
                //      Herault & Germon (2000) European J of Soil Sci.
                double tf = 1.0;              // temperature factor affecting denitrification rate (0-1)
                double wf = 0.0;              // soil moisture factor affecting denitrification rate (0-1), = 0 when wfps <0.62
                double phf = 1.0;             // pH factor affecting denitrification 0-1    
                double no3_avail;             // soil nitrate available (kg/ha)

                // nemis_dn_km = 22;         //ppm  these two parameters are input form UI 
                // nemis_dn_pot = 7.194;     // kgN/ha/day 


                if (_no3[layer] < g.no3_min[layer])
                {
                    n2o_atm[layer] = 0.0;
                    return 0.0;
                }

                //water effects (0-1)
                wf = 0.0;
                double wfps = (g.sw_dep[layer] - g.ll15_dep[layer]) / (g.sat_dep[layer] - g.ll15_dep[layer]);
                if (wfps > 0.62)
                {
                    wf = Math.Pow((wfps - 0.62) / 0.38, 1.74);    // 
                    wf = Math.Max(0.0, Math.Min(1.0, wf));
                }


                // soil temperature effects (0-1)       
                tf = 1.0;
                if (g.Tsoil[layer] < 11.0)
                    tf = Math.Exp(0.1 * (g.Tsoil[layer] - 11.0) * Math.Log(89, Math.E) - 9 * Math.Log(2.1, Math.E));
                else //st >= 11
                    tf = Math.Exp(0.1 * (g.Tsoil[layer] - 20.0) * Math.Log(2.1, Math.E));

                //pH effects
                phf = 1.0;
                if (g.ph[layer] < 6.5 && g.ph[layer] > 3.5)
                    phf = (g.ph[layer] - 3.5) / 3.0;
                else if (g.ph[layer] <= 3.5)
                    phf = 0.0;

                //no3 factor
                double _no3ppm = _no3[layer] * convFactor_kgha2ppm(layer);          // calculate in ppm 
                double fno3 = _no3ppm / (_no3ppm + g.nemis_dn_km);
                //double fno3 = no3ppm[layer] / (no3ppm[layer] + nemis_dn_km);                   

                //calculate denitrification rate  - dnRAte_pot in kg/ha !
                double dnRate = g.nemis_dn_pot * wf * tf * phf * fno3;
                // dnit_rate_coeff relavent? 

                // prevent NO3 - N concentration from falling below NO3_min
                no3_avail = _no3[layer] - g.no3_min[layer];
                dnRate = Math.Max(0.0, Math.Min(no3_avail, dnRate));

                //apsim exisiting gapproch for fraction of N2O, which is similar to that in DayCent 
                double WFPS = g.sw_dep[layer] / g.sat_dep[layer] * 100.0; // Water filled pore space (%)
                double CO2 = (dlt_c_fom_2_atm[0][layer] + dlt_c_fom_2_atm[1][layer] + dlt_c_fom_2_atm[2][layer] +
                              dlt_c_biom_2_atm[layer] + dlt_c_hum_2_atm[layer]) /
                              (g.bd[layer] * g.dlayer[layer]) * 100.0;                  //ppm

                double RtermA = 0.16 * g.dnit_k1;
                double RtermB = (CO2 > 0.0) ?
                     g.dnit_k1 * (Math.Exp(-0.8 * (_no3[layer] * convFactor_kgha2ppm(layer) / CO2))) : 0.0;
                double RtermC = 0.1;
                bool didInterpolate;
                double RtermD = Utility.Math.LinearInterpReal(WFPS, g.dnit_wfps, g.dnit_n2o_factor, out didInterpolate);
                // RTermD = (0.015 * WFPS) - 0.32;
                double N2N2O = Math.Max(RtermA, RtermB) * Math.Max(RtermC, RtermD);

                n2o_atm[layer] = dnRate / (N2N2O + 1.0);

                return dnRate;
            }

            //N2O alternatives--------------------------------------------------------------
            private double Denitrification_WNMM(int layer)
            {
                //+  Sub-Program Arguments
                //      snRate              // (OUTPUT) denitrification rate    - kg/ha/day
                //      n2o_atm             //  N2O emission 

                //+  Purpose
                //      Calculates denitrification using NEMIS approach
                //      Herault & Germon (2000) European J of Soil Sci.
                double tf;                    // temperature factor affecting denitrification rate (0-1)
                double wf;                    // soil moisture factor affecting denitrification rate (0-1)
                //double phf;                   // pH factor affecting denitrification 0-1    
                double no3_avail;             // soil nitrate available (kg/ha)

                //const double alpha = 0.5;     // n2o as fraction of dnRAte when wfps = 0.8   


                if (_no3[layer] < g.no3_min[layer])
                {
                    n2o_atm[layer] = 0.0;
                    return 0.0;
                }

                //water effects (0-1)
                double wfps = g.sw_dep[layer] / g.sat_dep[layer];
                if (wfps < 0.8)
                {
                    n2o_atm[layer] = 0.0;
                    return 0.0;
                }
                wf = Math.Exp(-23.77 + 23.77 * wfps);    // 

                // soil temperature effects (0-1)       
                tf = 0.1 + 0.9 * g.Tsoil[layer] / (g.Tsoil[layer] + Math.Exp(9.93 - 0.312 * g.Tsoil[layer]));

                //pH effects (No)

                //SOC % in soil 
                double SOC = 0.01 * (fom_c[layer] + biom_c[layer] + hum_c[layer]) / (g.bd[layer] * g.dlayer[layer]);
                /**** Note: 1 (kg/ha)/(g.cm^-3 * mm) = 0.0001, i.e., 0.01% */

                //calculate denitrification rate  - dnRAte_pot in kg/ha !
                double dnRate = _no3[layer] * (1 - Math.Exp(-1.4 * wf * tf * SOC));    //kgN/ha

                // prevent NO3 - N concentration from falling below NO3_min
                no3_avail = _no3[layer] - g.no3_min[layer];
                dnRate = Math.Max(0.0, Math.Min(no3_avail, dnRate));

                //n2o_atm
                if (wfps >= 1)
                    n2o_atm[layer] = 0.05 * dnRate;
                else
                    n2o_atm[layer] = g.wnmm_dn_alpha * dnRate * (1 - wf);

                return dnRate;
            }

            #endregion

            /// <summary>
            /// Send back the information about actual residue decomposition
            /// </summary>
            private void PackActualResidueDecomposition()
            {
                // Notes:
                //      Potential decomposition was given to this module by a residue/surfaceOM module.  Now we explicitly tell the
                //      module the actual decomposition rate for each of its residues.  If there isn't enough mineral N to decompose,
                //		the rate will be reduced from the potential value.

                int nLayers = g.dlayer.Length;

                soilp_dlt_res_c_atm = new double[nLayers];
                soilp_dlt_res_c_hum = new double[nLayers];
                soilp_dlt_res_c_biom = new double[nLayers];
                soilp_dlt_org_p = new double[nLayers];
                double soilp_cpr = Utility.Math.Divide(SumDoubleArray(pot_p_decomp), SumDoubleArray(pot_c_decomp), 0.0);  // C:P ratio for potential decomposition

                //SurfaceOrganicMatterDecompType SOMDecomp = new SurfaceOrganicMatterDecompType();
                SOMDecomp = new SurfaceOrganicMatterDecompType();
                Array.Resize(ref SOMDecomp.Pool, num_residues);


                for (int residue = 0; residue < num_residues; residue++)
                {
                    double c_summed = 0.0;
                    double n_summed = 0.0;
                    double[] dlt_res_c_decomp = new double[nLayers];
                    double[] dlt_res_n_decomp = new double[nLayers];
                    for (int layer = 0; layer < nLayers; layer++)
                    {
                        dlt_res_c_decomp[layer] = dlt_c_res_2_hum[layer][residue] +
                                                  dlt_c_res_2_biom[layer][residue] +
                                                  dlt_c_res_2_atm[layer][residue];
                        c_summed += dlt_res_c_decomp[layer];

                        //dlt_res_n_decomp[layer] = this.dlt_n_decomp[layer][residue];
                        dlt_res_n_decomp[layer] = dlt_n_decomp[layer][residue];
                        n_summed += dlt_res_n_decomp[layer];
                    }

                    // dsg 131103  Now, pack up the structure to return decompositions to SurfaceOrganicMatter
                    SOMDecomp.Pool[residue] = new SurfaceOrganicMatterDecompPoolType();
                    SOMDecomp.Pool[residue].FOM = new FOMType();
                    SOMDecomp.Pool[residue].Name = residue_name[residue];
                    SOMDecomp.Pool[residue].OrganicMatterType = residue_type[residue];

                    // dsg 131103   The 'amount' value will not be used by SurfaceOrganicMatter, so send zero as default
                    SOMDecomp.Pool[residue].FOM.amount = 0.0F;
                    if (Math.Abs(c_summed) < g.EPSILON)
                        c_summed = 0.0;
                    if (Math.Abs(n_summed) < g.EPSILON)
                        n_summed = 0.0;
                    SOMDecomp.Pool[residue].FOM.C = (float)c_summed;
                    SOMDecomp.Pool[residue].FOM.N = (float)n_summed;

                    // dsg 131103   The 'P' value will not be collected by SurfaceOrganicMatter, so send zero as default.
                    SOMDecomp.Pool[residue].FOM.P = 0.0F;
                    SOMDecomp.Pool[residue].FOM.AshAlk = 0.0F;

                    // dsg 131004 soilp needs some stuff - very ugly process - needs to be streamlined
                    //  create some variables which soilp can "get" - layer based arrays independent of residues
                    for (int layer = 0; layer < nLayers; layer++)
                    {
                        soilp_dlt_res_c_atm[layer] += dlt_c_res_2_atm[layer][residue];
                        soilp_dlt_res_c_hum[layer] += dlt_c_res_2_hum[layer][residue];
                        soilp_dlt_res_c_biom[layer] += dlt_c_res_2_biom[layer][residue];
                        soilp_dlt_org_p[layer] += dlt_res_c_decomp[layer] * soilp_cpr;
                    }
                }
            }

            #endregion

            #region Frequent and sporadic processes

            public void OnIncorpFOM(FOMLayerType FOMdata)
            {
                // +  Purpose:
                //      Partition the given FOM C and N into fractions in each layer.
                //      It will be assumed that the CN ratios of all fractions are equal

                bool nSpecified = false;
                for (int layer = 0; layer < FOMdata.Layer.Length; layer++)
                {
                    // If the caller specified CNR values then use them to calculate N from Amount.
                    if (FOMdata.Layer[layer].CNR > 0.0)
                        FOMdata.Layer[layer].FOM.N = (FOMdata.Layer[layer].FOM.amount * (float)g.c_in_fom) / FOMdata.Layer[layer].CNR;
                    // Was any N specified?
                    nSpecified |= FOMdata.Layer[layer].FOM.N != 0.0;
                }

                if (nSpecified)
                {
                    fom_type = 0; // use as default if fom type not found
                    for (int i = 0; i < g.fom_types.Length; i++)
                    {
                        if (g.fom_types[i] == FOMdata.Type)
                        {
                            fom_type = i;
                            break;
                        }
                    }
                    // Now convert the IncorpFOM.DeltaWt and IncorpFOM.DeltaN arrays to include fraction information and add to pools.
                    for (int layer = 0; layer < FOMdata.Layer.Length; layer++)
                    {
                        if (layer < g.dlayer.Length)
                        {
                            fom_c_pool1[layer] += FOMdata.Layer[layer].FOM.amount * g.fract_carb[fom_type] * g.c_in_fom;
                            fom_c_pool2[layer] += FOMdata.Layer[layer].FOM.amount * g.fract_cell[fom_type] * g.c_in_fom;
                            fom_c_pool3[layer] += FOMdata.Layer[layer].FOM.amount * g.fract_lign[fom_type] * g.c_in_fom;

                            fom_n_pool1[layer] += FOMdata.Layer[layer].FOM.N * g.fract_carb[fom_type];
                            fom_n_pool2[layer] += FOMdata.Layer[layer].FOM.N * g.fract_cell[fom_type];
                            fom_n_pool3[layer] += FOMdata.Layer[layer].FOM.N * g.fract_lign[fom_type];
                        }
                        else
                            g.Summary.WriteMessage(g.FullPath, " Number of FOM values given is larger than the number of layers, extra values will be ignored");
                    }
                }
            }

            public void OnIncorpFOMPool(FOMPoolType FOMPoolData)
            {
                // +  Purpose:
                //      Partition the given FOM C and N into fractions in each layer.

                for (int layer = 0; layer < FOMPoolData.Layer.Length; layer++)
                {
                    if (layer < g.dlayer.Length)
                    {
                        fom_c_pool1[layer] += FOMPoolData.Layer[layer].Pool[0].C;
                        fom_c_pool2[layer] += FOMPoolData.Layer[layer].Pool[1].C;
                        fom_c_pool3[layer] += FOMPoolData.Layer[layer].Pool[2].C;

                        fom_n_pool1[layer] += FOMPoolData.Layer[layer].Pool[0].N;
                        fom_n_pool2[layer] += FOMPoolData.Layer[layer].Pool[1].N;
                        fom_n_pool3[layer] += FOMPoolData.Layer[layer].Pool[2].N;

                        _no3[layer] += FOMPoolData.Layer[layer].no3;
                        _nh4[layer] += FOMPoolData.Layer[layer].nh4;
                    }
                    else
                        g.Summary.WriteMessage(g.FullPath, " Number of FOM values given is larger than the number of layers, extra values will be ignored");
                }
            }

            public void OnPotentialResidueDecompositionCalculated(SurfaceOrganicMatterDecompType SurfaceOrganicMatterDecomp)
            {
                //+  Purpose
                //     Get information of potential residue decomposition

                num_residues = SurfaceOrganicMatterDecomp.Pool.Length;

                Array.Resize(ref residue_name, num_residues);
                Array.Resize(ref residue_type, num_residues);
                Array.Resize(ref pot_c_decomp, num_residues);
                Array.Resize(ref pot_n_decomp, num_residues);
                Array.Resize(ref pot_p_decomp, num_residues);
                for (int layer = 0; layer < dlt_c_res_2_biom.Length; layer++)
                {
                    Array.Resize(ref dlt_c_res_2_biom[layer], num_residues);
                    Array.Resize(ref dlt_c_res_2_hum[layer], num_residues);
                    Array.Resize(ref dlt_c_res_2_atm[layer], num_residues);
                    Array.Resize(ref dlt_c_decomp[layer], num_residues);
                    Array.Resize(ref dlt_n_decomp[layer], num_residues);
                }

                for (int residue = 0; residue < num_residues; residue++)
                {
                    residue_name[residue] = SurfaceOrganicMatterDecomp.Pool[residue].Name;
                    residue_type[residue] = SurfaceOrganicMatterDecomp.Pool[residue].OrganicMatterType;
                    pot_c_decomp[residue] = SurfaceOrganicMatterDecomp.Pool[residue].FOM.C;
                    pot_n_decomp[residue] = SurfaceOrganicMatterDecomp.Pool[residue].FOM.N;
                    pot_p_decomp[residue] = SurfaceOrganicMatterDecomp.Pool[residue].FOM.P;
                }
            }

            /// <summary>
            /// Set/reset the size of all public arrays
            /// </summary>
            /// <remarks>
            /// This doesn't clear the existing values
            /// </remarks>
            /// <param name="nLayers">the new array length</param>
            public void ResizeLayerArrays(int nLayers)
            {
                Array.Resize(ref _nh4, nLayers);
                Array.Resize(ref _no3, nLayers);
                Array.Resize(ref _urea, nLayers);
                Array.Resize(ref no3_yesterday, nLayers);
                Array.Resize(ref nh4_yesterday, nLayers);
                Array.Resize(ref inert_c, nLayers);
                Array.Resize(ref biom_c, nLayers);
                Array.Resize(ref biom_n, nLayers);
                Array.Resize(ref hum_c, nLayers);
                Array.Resize(ref hum_n, nLayers);
                Array.Resize(ref fom_c_pool1, nLayers);
                Array.Resize(ref fom_c_pool2, nLayers);
                Array.Resize(ref fom_c_pool3, nLayers);
                Array.Resize(ref fom_n_pool1, nLayers);
                Array.Resize(ref fom_n_pool2, nLayers);
                Array.Resize(ref fom_n_pool3, nLayers);
                Array.Resize(ref nh4_transform_net, nLayers);
                Array.Resize(ref no3_transform_net, nLayers);
                Array.Resize(ref dlt_nh4_net, nLayers);
                Array.Resize(ref dlt_no3_net, nLayers);
                Array.Resize(ref dlt_c_hum_2_atm, nLayers);
                Array.Resize(ref dlt_c_biom_2_atm, nLayers);
                for (int i = 0; i < 3; i++)
                {
                    Array.Resize(ref dlt_c_fom_2_biom[i], nLayers);
                    Array.Resize(ref dlt_c_fom_2_hum[i], nLayers);
                    Array.Resize(ref dlt_c_fom_2_atm[i], nLayers);
                }
                Array.Resize(ref dlt_c_res_2_biom, nLayers);
                Array.Resize(ref dlt_c_res_2_hum, nLayers);
                Array.Resize(ref dlt_c_res_2_atm, nLayers);
                Array.Resize(ref dlt_c_decomp, nLayers);
                Array.Resize(ref dlt_n_decomp, nLayers);
                Array.Resize(ref dlt_nitrification, nLayers);
                Array.Resize(ref effective_nitrification, nLayers);
                Array.Resize(ref dlt_urea_hydrolised, nLayers);
                Array.Resize(ref nh4_deficit_immob, nLayers);
                Array.Resize(ref dlt_n_fom_2_min, nLayers);
                Array.Resize(ref dlt_n_biom_2_min, nLayers);
                Array.Resize(ref dlt_n_hum_2_min, nLayers);
                Array.Resize(ref dlt_fom_c_pool1, nLayers);
                Array.Resize(ref dlt_fom_c_pool2, nLayers);
                Array.Resize(ref dlt_fom_c_pool3, nLayers);
                Array.Resize(ref dlt_no3_decomp, nLayers);
                Array.Resize(ref dlt_nh4_decomp, nLayers);
                Array.Resize(ref dlt_no3_dnit, nLayers);
                Array.Resize(ref dlt_nh4_dnit, nLayers);
                Array.Resize(ref n2o_atm, nLayers);
                Array.Resize(ref dlt_c_hum_2_biom, nLayers);
                Array.Resize(ref dlt_c_biom_2_hum, nLayers);
            }

            /// <summary>
            /// Check whether profile has changed and move values between layers
            /// </summary>
            /// <param name="new_dlayer">New values for dlayer</param>
            public void CheckProfile(double[] new_dlayer)
            {
                // How to decide:
                // if bedrock is lower than lowest  profile depth, we won't see
                // any change in profile, even if there is erosion. Ideally we
                // should test both soil_loss and dlayer for changes to cater for
                // manager control. But, the latter means we have to fudge enr for the
                // loss from top layer.

                dlt_n_loss_in_sed = 0.0;
                dlt_c_loss_in_sed = 0.0;

                // move pools
                // EJZ:: Why aren't no3 and urea moved????
                dlt_n_loss_in_sed += MoveLayers(ref _nh4, new_dlayer);
                dlt_c_loss_in_sed += MoveLayers(ref inert_c, new_dlayer);
                dlt_c_loss_in_sed += MoveLayers(ref biom_c, new_dlayer);
                dlt_n_loss_in_sed += MoveLayers(ref biom_n, new_dlayer);
                dlt_c_loss_in_sed += MoveLayers(ref hum_c, new_dlayer);
                dlt_n_loss_in_sed += MoveLayers(ref hum_n, new_dlayer);
                dlt_n_loss_in_sed += MoveLayers(ref fom_n_pool1, new_dlayer);
                dlt_n_loss_in_sed += MoveLayers(ref fom_n_pool2, new_dlayer);
                dlt_n_loss_in_sed += MoveLayers(ref fom_n_pool3, new_dlayer);
                dlt_c_loss_in_sed += MoveLayers(ref fom_c_pool1, new_dlayer);
                dlt_c_loss_in_sed += MoveLayers(ref fom_c_pool2, new_dlayer);
                dlt_c_loss_in_sed += MoveLayers(ref fom_c_pool3, new_dlayer);

            }

            /// <summary>
            /// Move the values of a given varible between layers, from bottom to top
            /// </summary>
            /// <param name="variable">Variable to move layers</param>
            /// <param name="new_dlayer">new dlayer array</param>
            /// <returns>Amount of C or N lost because of changes in profile</returns>
            private double MoveLayers(ref double[] variable, double[] new_dlayer)
            {

                double profile_loss = 0.0;
                double layer_loss = 0.0;
                double layer_gain = 0.0;
                int lowest_layer = g.dlayer.Length;
                int new_lowest_layer = new_dlayer.Length;

                double yesterdays_n = SumDoubleArray(variable);

                // initialise layer loss from below profile same as bottom layer

                double profile_depth = SumDoubleArray(g.dlayer);
                double new_profile_depth = SumDoubleArray(new_dlayer);

                if (Utility.Math.FloatsAreEqual(profile_depth, new_profile_depth))
                {
                    // move from below bottom layer - assume it has same properties as bottom layer
                    layer_loss = variable[lowest_layer - 1] * LayerFract(lowest_layer - 1);
                }
                else
                {
                    // we're going into bedrock
                    layer_loss = 0.0;
                    // now see if bottom layers have been merged.
                    if (lowest_layer > new_lowest_layer && lowest_layer > 1)
                    {
                        // merge the layers
                        for (int layer = lowest_layer - 1; layer >= new_lowest_layer; layer--)
                        {
                            variable[layer - 1] += variable[layer];
                            variable[layer] = 0.0;
                        }
                        Array.Resize(ref variable, new_lowest_layer);
                    }
                }
                double profile_gain = layer_loss;

                // now move from bottom layer to top
                for (int layer = new_lowest_layer - 1; layer >= 0; layer--)
                {
                    // this layer gains what the lower layer lost
                    layer_gain = layer_loss;
                    layer_loss = variable[layer] * LayerFract(layer);
                    variable[layer] += layer_gain - layer_loss;
                }

                // now adjust top layer for enrichment
                double enr = g.enr_a_coeff * Math.Pow(g.soil_loss * 1000, -1.0 * g.enr_b_coeff);
                enr = Math.Max(1.0, Math.Min(enr, g.enr_a_coeff));

                profile_loss = layer_loss * enr;
                variable[0] = Math.Max(0.0, variable[0] + layer_loss - profile_loss);

                // check mass balance
                double todays_n = SumDoubleArray(variable);
                yesterdays_n += profile_gain - profile_loss;
                if (!Utility.Math.FloatsAreEqual(todays_n, yesterdays_n))
                {
                    throw new Exception("N mass balance out");
                }
                return profile_loss;
            }

            private double LayerFract(int layer)
            {
                // + Purpose
                //     Calculate 

                double layerFract = g.soil_loss * convFactor_kgha2ppm(layer) / 1000.0;
                if (layerFract > 1.0)
                {
                    int layerNo = layer + 1; // Convert to 1-based index for display
                    double layerPercent = layerFract * 100.0; // Convert fraction to percentage
                    throw new Exception("Soil loss is greater than depth of layer(" + layerNo.ToString() + ") by " +
                        layerPercent.ToString() + "%.\nConstrained to this layer. Re-mapping of SoilN pools will be incorrect.");
                }
                return Math.Min(0.0, layerFract);
            }

            #endregion

            #region Factor's calculation

            #region Original factors

            private double pHFNitrf(int layer)
            {
                // +  Purpose
                //      Calculates a 0-1 pH factor for nitrification.

                bool DidInterpolate;
                return Utility.Math.LinearInterpReal(g.ph[layer], g.pHf_nit_pH, g.pHf_nit_values, out DidInterpolate);
            }

            private double WFNitrf(int layer, int index)
            {
                // +  Purpose
                //      Calculates a 0-1 water factor for nitrification.

                // +  Assumptions
                //     index = 1 for aerobic conditions, 2 for anaerobic

                // temporary water factor (0-1)
                double wfd = 1.0;
                if (g.sw_dep[layer] > g.dul_dep[layer] && g.sat_dep[layer] > g.dul_dep[layer])
                {   // saturated
                    wfd = 1.0 + (g.sw_dep[layer] - g.dul_dep[layer]) / (g.sat_dep[layer] - g.dul_dep[layer]);
                    wfd = Math.Max(1.0, Math.Min(2.0, wfd));
                }
                else
                {
                    // unsaturated
                    // assumes rate of mineralisation is at optimum rate until soil moisture midway between dul and ll15
                    wfd = Utility.Math.Divide(g.sw_dep[layer] - g.ll15_dep[layer], g.dul_dep[layer] - g.ll15_dep[layer], 0.0);
                    wfd = Math.Max(0.0, Math.Min(1.0, wfd));
                }

                bool didInterpolate;
                if (index == 1)
                    return Utility.Math.LinearInterpReal(wfd, g.wfnit_index, g.wfnit_values, out didInterpolate);
                else
                    // if pond is active, and aerobic conditions dominate, assume wf_nitrf = 0
                    return 0.0;
            }

            private double WFDenit(int layer)
            {
                // + Purpose
                //     Calculates a 0-1 water factor for denitrification

                // temporary water factor (0-1); 0 is used if unsaturated
                double wfd = 0.0;
                if (g.sw_dep[layer] > g.dul_dep[layer])  // saturated
                    wfd = Math.Pow(Utility.Math.Divide(g.sw_dep[layer] - g.dul_dep[layer], g.sat_dep[layer] - g.dul_dep[layer], 0.0), g.dnit_wf_power);
                return Math.Max(0.0, Math.Min(1.0, wfd));
            }

            private double WF(int layer, int index)
            {
                // + Purpose
                //     Calculates a 0-1 water factor for mineralisation.

                // + Assumptions
                //     index = 1 for aerobic conditions, 2 for anaerobic

                // temporary water factor (0-1)
                double wfd;
                if (g.sw_dep[layer] > g.dul_dep[layer])
                { // saturated
                    wfd = Math.Max(1.0, Math.Min(2.0, 1.0 +
                        Utility.Math.Divide(g.sw_dep[layer] - g.dul_dep[layer], g.sat_dep[layer] - g.dul_dep[layer], 0.0)));
                }
                else
                { // unsaturated
                    // assumes rate of mineralisation is at optimum rate until soil moisture midway between dul and ll15
                    wfd = Math.Max(0.0, Math.Min(1.0, Utility.Math.Divide(g.sw_dep[layer] - g.ll15_dep[layer], g.dul_dep[layer] - g.ll15_dep[layer], 0.0)));
                }

                if (index == 1)
                {
                    bool didInterpolate;
                    return Utility.Math.LinearInterpReal(wfd, g.wfmin_index, g.wfmin_values, out didInterpolate);
                }
                else if (index == 2) // if pond is active, and liquid conditions dominate, assume wf = 1
                    return 1.0;
                else
                    throw new Exception("SoilN2 WF function - invalid value for \"index\" parameter");
            }

            private double TF(int layer, int index)
            {
                // + Purpose
                //     Calculate a temperature factor, based on the soil temperature of the layer, for nitrification and mineralisation

                // + Assumptions
                //     index = 1 for aerobic conditions, 2 for anaerobic

                // Alternate version from CM:
                //      tf = (soil_temp[layer] - 5.0) /30.0
                // because tf is bound between 0 and 1, the effective temperature (soil_temp) lies between 5 to 35.
                // alternative quadratic temperature function is preferred with optimum temperature (CM - used 32 deg)

                if (g.Tsoil[layer] > 0.0)
                    return Math.Max(0.0, Math.Min(1.0, Utility.Math.Divide(g.Tsoil[layer] * g.Tsoil[layer], g.opt_temp[index - 1] * g.opt_temp[index - 1], 0.0)));
                else
                    return 0.0;     // soil is too cold for mineralisation
            }

            private double RothcTF(int layer, int index)
            {
                // + Purpose
                //     Calculate a temperature factor, based on the soil temperature of the layer, for nitrification and mineralisation

                double t = Math.Min(g.Tsoil[layer], g.opt_temp[index - 1]);
                return 47.9 / (1.0 + Math.Exp(106.0 / (t + 18.3)));
            }

            private double[] FractFomC(int fract)
            {
                switch (fract)
                {
                    case 0: return fom_c_pool1;
                    case 1: return fom_c_pool2;
                    case 2: return fom_c_pool3;
                    default: throw new Exception("Coding error: bad fraction in FractFomC");
                }
            }

            private double[] FractFomN(int fract)
            {
                switch (fract)
                {
                    case 0: return fom_n_pool1;
                    case 1: return fom_n_pool2;
                    case 2: return fom_n_pool3;
                    default: throw new Exception("Coding error: bad fraction in FractFomN");
                }
            }

            private double[] FractRDFom(int fract)
            {
                switch (fract)
                {
                    case 0: return g.rd_carb;
                    case 1: return g.rd_cell;
                    case 2: return g.rd_lign;
                    default: throw new Exception("Coding error: bad fraction in FractRDFom");
                }
            }

            #endregion

            #region New Factors

            /// <summary>
            /// Calculate a temperature factor for C and N processes
            /// </summary>
            /// <param name="layer">The soil layer to calculate</param>
            /// <param name="index">Parameter indication whether pond exists</param>
            /// <param name="Parameters">Parameter data</param>
            /// <returns>Temperature limiting factor (0-1)</returns>
            private double SoilTempFactor(int layer, int index, BentStickData Parameters)
            {
                // + Assumptions
                //     index = 0 for aerobic conditions, 1 for anaerobic

                index -= 1;  // use this untill can change the whole code. (index used to be [1-2]
                if (index > Parameters.xValueForOptimum.Length - 1)
                    throw new Exception("SoilNitrogen.SoilTempFactor - invalid value for \"index\" parameter");

                double Toptimum = Parameters.xValueForOptimum[index];
                double Fzero = Parameters.yValueAtZero[index];
                double CurveN = Parameters.CurveExponent[index];
                double AuxV = Math.Pow(Fzero, 1 / CurveN);
                double Tzero = Toptimum * AuxV / (AuxV - 1);
                double beta = 1 / (Toptimum - Tzero);

                return Math.Min(1.0, Math.Pow(beta * Math.Max(0.0, g.Tsoil[layer] - Tzero), CurveN));
            }

            /// <summary>
            /// Calculate a soil moist factor for C and N processes
            /// </summary>
            /// <param name="layer">The soil layer to calculate</param>
            /// <param name="index">Parameter indication whether pond exists</param>
            /// <param name="Parameters">Parameter data</param>
            /// <returns>Soil moisture limiting factor (0-1)</returns>
            private double SoilMoistFactor(int layer, int index, BrokenStickData Parameters)
            {
                // + Assumptions
                //     index = 0 for aerobic conditions, 1 for anaerobic

                index -= 1;  // use this untill can change the whole code. (index used to be [1-2]
                if (index == 0)
                {
                    bool didInterpolate;

                    // get the modified soil water variable
                    double[] yVals = { 0.0, 1.0, 2.0, 3.0 };
                    double[] xVals = { 0.0, g.ll15_dep[layer], g.dul_dep[layer], g.sat_dep[layer] };
                    double myX = Utility.Math.LinearInterpReal(g.sw_dep[layer], xVals, yVals, out didInterpolate);

                    // get the soil moist factor
                    return Utility.Math.LinearInterpReal(myX, Parameters.xVals, Parameters.yVals, out didInterpolate);
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
                //     index = 0 for aerobic conditions, 1 for anaerobic

                index -= 1;  // use this untill can change the whole code. (index used to be [1-2]
                if (index == 0)
                {
                    bool didInterpolate;

                    // get the WFPS value (%)
                    double WFPS = g.sw_dep[layer] / g.sat_dep[layer] * 100.0;

                    // get the WFPS factor
                    return Utility.Math.LinearInterpReal(WFPS, Parameters.xVals, Parameters.yVals, out didInterpolate);
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
                return Utility.Math.LinearInterpReal(g.ph[layer], Parameters.xVals, Parameters.yVals, out DidInterpolate);
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
                double nitTot = Math.Max(0.0, (_no3[layer] - g.no3_min[layer]) + (_nh4[layer] - g.nh4_min[layer]));

                // fresh organic carbon (kg/ha)
                double fomC = fom_c_pool1[layer] + fom_c_pool2[layer] + fom_c_pool3[layer];

                // fresh organic nitrogen (kg/ha)
                double fomN = fom_n_pool1[layer] + fom_n_pool2[layer] + fom_n_pool3[layer];

                // ratio of C in fresh OM to N available for decay
                double cnr = Utility.Math.Divide(fomC, fomN + nitTot, 0.0);

                return Math.Max(0.0, Math.Min(1.0, Math.Exp(-rateCN * (cnr - OptCN) / OptCN)));
            }

            #endregion

            #endregion

            #endregion

            #region Auxiliary functions

            /// <summary>
            /// Checks whether the variable is negative, consider thresholds
            /// </summary>
            /// <param name="TheVariable">variable to be tested</param>
            /// <param name="layer">layer to which the variable belongs to</param>
            /// <param name="VariableName">name of the variable</param>
            /// <returns></returns>
            private bool CheckNegativeValues(double TheVariable, int layer, string VariableName)
            {
                bool result = true;
                if (TheVariable < g.FatalThreshold)
                {
                    result = false;
                    throw new Exception("Attempt to change " + VariableName + "[" + (layer + 1).ToString() +
                        "] in Patch[" + PatchName + "] to a value below the fatal threshold, " +
                        g.FatalThreshold.ToString());
                }
                else if (TheVariable < g.WarningThreshold)
                {
                    result = false;
                    g.Summary.WriteWarning(g.FullPath, g.Clock.Today.ToShortDateString() + " - Attempt to change " + VariableName + "[" +
                        (layer + 1).ToString() + "] in Patch[" + PatchName + "] to a value below the lower limit");
                    g.Summary.WriteMessage(g.FullPath, "  The value " + TheVariable.ToString() + " will be reset to minimum value");
                }
                else if (TheVariable < 0.0)
                    result = false;

                return result;
            }

            /// <summary>
            /// Checks whether the variable is within acceptable bounds, may reset if not
            /// </summary>
            /// <param name="TheVariable">variable to be tested</param>
            /// <param name="layer">layer to which the variable belongs to</param>
            /// <param name="VariableName">name of the variable</param>
            /// <param name="MinimumValue">minimum value for the variable</param>
            /// <param name="MaximumValue">maximum value for the variable</param>
            /// <param name="canReset">whether the value will be reset if not within bounds. Throw fatal error otherwise.</param>
            /// <returns></returns>
            private bool CheckVariableBounds(ref double TheVariable, int layer, string VariableName, double MinimumValue, double MaximumValue, bool canReset)
            {
                bool result = true;
                if (TheVariable < MinimumValue)
                {
                    result = false;
                    if (canReset)
                        TheVariable = MinimumValue;
                    else
                        throw new Exception("The value of " + VariableName + "[" + (layer + 1).ToString() +
                        "] in Patch[" + PatchName + "] is below the minimum value, , " + MinimumValue.ToString());
                }
                else if (TheVariable > MaximumValue)
                {
                    result = false;
                    if (canReset)
                        TheVariable = MaximumValue;
                    else
                        throw new Exception("The value of " + VariableName + "[" + (layer + 1).ToString() +
                        "] in Patch[" + PatchName + "] is above the maximum value, , " + MaximumValue.ToString());
                }

                return result;

            }

            private double convFactor_kgha2ppm(int layer)
            {
                // Calculate conversion factor from kg/ha to ppm (mg/kg)

                if (g.bd == null || g.dlayer == null || g.bd.Length == 0 || g.dlayer.Length == 0)
                {
                    return 0.0;
                    throw new Exception(" Error on computing convertion factor, kg/ha to ppm. Value for dlayer or bulk density not valid");
                }
                return Utility.Math.Divide(100.0, g.bd[layer] * g.dlayer[layer], 0.0);
            }

            private double SumDoubleArray(double[] anArray)
            {
                double result = 0.0;
                if (anArray != null)
                {
                    foreach (double Value in anArray)
                        result += Value;
                }
                return result;
            }

            #endregion

        }
    }

}