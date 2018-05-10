// -----------------------------------------------------------------------
// <copyright file="stock_padd.cs" company="CSIRO">
// CSIRO Agriculture & Food
// </copyright>
// -----------------------------------------------------------------------

namespace Models.GrazPlan
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Models.PMF.Interfaces;
    using Models.PMF.Organs;
    using Models.Core;
    using CMPServices;

    /*
     GRAZPLAN animal biology model for AusFarm - TPaddockList & TForageList classes                                                                   
                                                                               
     * TPaddockList contains information about paddocks within the animal      
       biology model. Paddocks have the following attributes:                  
       - ID   (integer)                                                        
       - name (text)                                                           
       - area (ha)                                                             
       - slope (degrees) - this is converted to a steepness value              
       - amount and composition of forage present in the paddock               
       - amount and composition of supplement present in the paddock           
       This information is held in TPaddockInfo objects; TPaddockList is a     
       list of TPaddockInfo.                                                   
    */

    /// <summary>
    /// Chemistry data for the forage
    /// </summary>
    [Serializable]
    public struct ChemData
    {
        /// <summary>
        /// Mass in kg/ha 
        /// </summary>
        public double dMass_KgHa;
        /// <summary>
        /// N in kg/ha
        /// </summary>
        public double dNitrogen_KgHa;
        /// <summary>
        /// P in kg/ha
        /// </summary>
        public double dPhosphorus_KgHa;
        /// <summary>
        /// S in kg/ha
        /// </summary>
        public double dSulphur_KgHa;
        /// <summary>
        /// Ash alkalinity mol/ha
        /// </summary>
        public double dAshAlk_MolHa;
    }

    /// <summary>
    /// Up to 12 classes with separate digestible and indigestible pools
    /// </summary>
    [Serializable]
    public class ForageInfo
    {
        /// <summary>
        /// Chemistry of the forage
        /// </summary>
        protected enum ForageChemistry
        {
            /// <summary>
            /// 
            /// </summary>
            fcUnknown,
            /// <summary>
            /// Digestible vs indigestible
            /// </summary>
            fcDigInDig,       
            /// <summary>
            /// Mean digestibility provided - this component to distribute over DMD classes
            /// </summary>
            fcMeanDMD,        
            /// <summary>
            /// 80%, 70%, ... 30% digestible
            /// </summary>
            fcDMDClasses6,    
            /// <summary>
            /// 
            /// </summary>
            fcVarDMDClasses
        };
#pragma warning disable 1591 //missing xml comment
        protected const double MISSING_POINT = 99999.9;
        protected const double CLASSWIDTH = 0.10;
        protected const double HIGHEST_DMD = 0.85;
        protected const double EPSILON = 1.0E-6;
        protected const int MAX_CHEM_CLASSES = 24;
        protected const int MAX_DDM_CLASSES = 12;
        protected const int NOT_SEED = 0;

        protected const string postfix_LEGUME = "_legume";
        protected const string postfix_C4GRASS = "_c4grass";
#pragma warning restore 1591 //missing xml comment
        string[,] CHEMISTRY_CLASSES = { 
                            {"",        "",          "",     "",     "",     "",     "",     "",      "",     "",     "",     "",     "",     "",     "",     "",     "",     "",     "",     "",     "",     "",     "",     ""     },
                            {"ddm",     "idm",       "",     "",     "",     "",     "",     "",      "",     "",     "",     "",     "",     "",     "",     "",     "",     "",     "",     "",     "",     "",     "",     ""     },
                            {"ddm_mean","idm_mean",  "",     "",     "",     "",     "",     "",      "",     "",     "",     "",     "",     "",     "",     "",     "",     "",     "",     "",     "",     "",     "",     ""     },
                            {"dmd80",   "dmd70",     "dmd60","dmd50","dmd40","dmd30","",     "",      "",     "",     "",     "",     "",     "",     "",     "",     "",     "",     "",     "",     "",     "",     "",     ""     },
                            {"ddm01",   "idm01",     "ddm02","idm02","ddm03","idm03","ddm04","idm04", "ddm05","idm05","ddm06","idm06","ddm07","idm07","ddm08","idm08","ddm09","idm09","ddm10","idm10","ddm11","idm11","ddm12","idm12"} };

        /// <summary>
        /// Chemistry types that could expected in the AvailableToAnimals data
        /// </summary>
        protected int[] CHEM_COUNT = { 0, 2, 2, GrazType.DigClassNo, 2 * MAX_DDM_CLASSES };

        private double getBottom()
        {
            if (this.FBottom_MM == MISSING_POINT)
                return 0.0;
            else
                return this.FBottom_MM;
        }
        private double getTop()
        {
            return this.FTop_MM;
        }

        /// <summary>
        /// Look for a code appended to the chemistry code, and strip it off if found
        /// </summary>
        /// <param name="sValue">e.g. dig_legume. Returns the chem.</param>
        /// <param name="sCode">Lowercase code to find</param>
        /// <returns>True if the code is appended</returns>
        private bool FindPostFix(ref string sValue, string sCode)
        {
            int pos = sValue.ToLower().IndexOf(sCode);
            if (pos >= 0)
            {
                sValue = sValue.Substring(0, pos);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// The total live herbage used as input in TGrazingInputs
        /// Units: the same as the forage object
        /// </summary>
        public double TotalLive
        {
            get
            {
                return this.FForageData.TotalGreen;
            }
        }

        /// <summary>
        /// The total dead herbage used as input in TGrazingInputs
        /// Units: the same as the forage object
        /// </summary>
        public double TotalDead
        {
            get
            {
                return this.FForageData.TotalDead;
            }
        }

        // The computed attributes of this "forage", in the form used by TAnimalGroup
        /// <summary>
        /// Use the forage data
        /// </summary>
        protected bool FUseForageData;
        /// <summary>
        /// The grazing forage data
        /// </summary>
        protected GrazType.TGrazingInputs FForageData = new GrazType.TGrazingInputs();
        /// <summary>
        /// Green mass of the forage
        /// </summary>
        public double FGreenMass;
        /// <summary>
        /// Bulk density of the green
        /// </summary>
        protected double FGreenBulkDensity;

        // Information calculated from data in an "avalabletoanimal" value
        /// <summary>
        /// Chemistry type
        /// </summary>
        protected ForageChemistry FChemistryType;
        /// <summary>
        /// Is this "forage" green or dead?
        /// </summary>
        protected bool FIsGreen;  
        /// <summary>
        /// Mass of the legume
        /// </summary>
        protected double FLegumeMass;
        /// <summary>
        /// Mass of the C4 grass
        /// </summary>
        protected double FC4GrassMass;
        /// <summary>
        /// 0 = non-seed, UNRIPE or RIPE
        /// </summary>
        protected int FSeedType;   
        /// <summary>
        /// 
        /// </summary>
        protected double FBottom_MM;
        /// <summary>
        /// 
        /// </summary>
        protected double FTop_MM;
        /// <summary>
        /// Chemistry details for each chem class
        /// </summary>
        protected ChemData[] FChemData = new ChemData[MAX_CHEM_CLASSES - 1];

        /// <summary>
        /// 
        /// </summary>
        protected double[] dHerbageDMDFract = new double[GrazType.DigClassNo + 1]; // [1..DigClassNo]
        /// <summary>
        /// 
        /// </summary>
        protected double[] dSeedRipeFract = new double[3];  // [1..2]

        /// <summary>
        /// 
        /// </summary>
        public void summariseInitHerbage()
        {
            double fHR;
            double dBulkDensity;  // Herbage bulk density in kg/m^3
            int iClass;
            int iChem;

            if (FUseForageData)
            {
                FGreenMass = FForageData.TotalGreen;
                FGreenBulkDensity = GrazType.REF_HERBAGE_BD;
                if ((FForageData.TotalGreen + FForageData.TotalDead) > 0.0)
                {
                    fHR = 0.0;
                    for (iClass = 1; iClass <= GrazType.DigClassNo; iClass++)
                        fHR = fHR + FForageData.Herbage[iClass].Biomass * FForageData.Herbage[iClass].HeightRatio;
                    fHR = fHR / (FForageData.TotalGreen + FForageData.TotalDead);
                    if (fHR > 0.0)
                        FGreenBulkDensity = GrazType.REF_HERBAGE_BD / fHR;
                }
            }

            else
            {
                FGreenMass = 0.0;
                FGreenBulkDensity = 0.0;
                if (FIsGreen)
                {
                    dBulkDensity = this.dHerbageBulkDensity();
                    for (iChem = 0; iChem <= this.CHEM_COUNT[(int)FChemistryType] - 1; iChem++)
                    {
                        FGreenMass = FGreenMass + this.FChemData[iChem].dMass_KgHa;
                        FGreenBulkDensity = FGreenBulkDensity + this.FChemData[iChem].dMass_KgHa * dBulkDensity;
                    }
                }

                if (FGreenMass > 0.0)
                    FGreenBulkDensity = FGreenBulkDensity / FGreenMass;
                else
                    FGreenBulkDensity = GrazType.REF_HERBAGE_BD;
            }
        }

        /// <summary>
        /// Calculate the bulk density
        /// </summary>
        /// <returns></returns>
        protected double dHerbageBulkDensity()
        {
            double dCohortDM;
            double dMaxHeight_MM;
            double dMinHeight_MM;
            int iChem;

            dCohortDM = 0.0;
            dMaxHeight_MM = 0.0;
            dMinHeight_MM = 999999.9;

            for (iChem = 0; iChem <= this.CHEM_COUNT[(int)FChemistryType]; iChem++)
                dCohortDM = dCohortDM + this.FChemData[iChem].dMass_KgHa;
            dMaxHeight_MM = Math.Max(dMaxHeight_MM, FTop_MM);
            dMinHeight_MM = Math.Min(dMinHeight_MM, FBottom_MM);

            if (dMinHeight_MM < dMaxHeight_MM)
                return 10.0 * dCohortDM / (dMaxHeight_MM - dMinHeight_MM);
            else
                return GrazType.REF_HERBAGE_BD;
        }

        /// <summary>
        /// * The providing forage component is specifying a quantity of forage with
        ///   a single digestibility, as part of a distribution of digestibilities
        ///   *provided by the source component*.
        /// * We therefore calculate the DMD and then place all forage mass in the
        ///   TAnimalGroup DMD class that contains that DMD value
        /// </summary>
        /// <param name="dAvailPropn"></param>
        /// <param name="dBulkDensity"></param>
        /// <returns></returns>
        public GrazType.TGrazingInputs convertChemistry_DigInDig(double dAvailPropn, double dBulkDensity)
        {
            const int DDM = 0;
            const int IDM = 1;

            double dTotalDM;
            double dMeanDMD;
            int iDMD;

            GrazType.TGrazingInputs Result = new GrazType.TGrazingInputs();
            GrazType.zeroGrazingInputs(ref Result);
            for (iDMD = 1; iDMD <= GrazType.DigClassNo; iDMD++)
                Result.Herbage[iDMD].HeightRatio = 1.0;

            this.dHerbageDMDFract = new double[GrazType.DigClassNo + 1];
            this.dSeedRipeFract = new double[3];

            dTotalDM = this.FChemData[DDM].dMass_KgHa + this.FChemData[IDM].dMass_KgHa;
            // Leaf & stem pools
            if ((FSeedType == NOT_SEED) && (dTotalDM > 0.0))
            {
                dMeanDMD = this.FChemData[DDM].dMass_KgHa / dTotalDM;
                iDMD = Convert.ToInt32(Math.Min(1, Math.Max(1 + Math.Truncate((HIGHEST_DMD - dMeanDMD) / CLASSWIDTH), GrazType.DigClassNo)));  // TODO: may need testing

                populateIntakeRecord(ref Result.Herbage[iDMD], iDMD, true,
                                      dTotalDM, dMeanDMD, dAvailPropn,
                                      this.FChemData[DDM].dNitrogen_KgHa, this.FChemData[IDM].dNitrogen_KgHa,
                                      this.FChemData[DDM].dPhosphorus_KgHa, this.FChemData[IDM].dPhosphorus_KgHa,
                                      this.FChemData[DDM].dSulphur_KgHa, this.FChemData[IDM].dSulphur_KgHa,
                                      this.FChemData[DDM].dAshAlk_MolHa, this.FChemData[IDM].dAshAlk_MolHa,
                                      dBulkDensity);

                if (FIsGreen)
                    Result.TotalGreen = Result.Herbage[iDMD].Biomass;
                else
                    Result.TotalDead = Result.Herbage[iDMD].Biomass;

                populateHerbageType(ref Result);

                this.dHerbageDMDFract[iDMD] = Result.Herbage[iDMD].Biomass; // for later division by the total for the DMD class
            }

            // Seed pools
            else if (((FSeedType == GrazType.UNRIPE) || (FSeedType == GrazType.RIPE)) && (dTotalDM > 0.0))
            {
                populateSeedRecord(ref Result, dAvailPropn, DDM, IDM);

                // The relative intake calculations for seed use dmd's from the herbage array. If this is a
                // seed only forage then the herbage dmd should be set
                dMeanDMD= Result.Seeds[1, FSeedType].Digestibility;
                iDMD= Convert.ToInt32(Math.Min(1, Math.Max(1 + Math.Truncate((HIGHEST_DMD - dMeanDMD) / CLASSWIDTH), GrazType.DigClassNo)));
                Result.SeedClass[1, FSeedType] = iDMD;                          // will then use the [iDMD] digestibility class from the herbage array
                Result.Herbage[iDMD].Digestibility = dMeanDMD;

                this.dSeedRipeFract[FSeedType] = Result.Seeds[1, FSeedType].Biomass; // for later division by the total for the seed ripeness class
            }
            return Result;
        }

        /// <summary>
        /// The providing forage component is specifying a quantity of forage with
        ///   a known average digestibility, and is signalling that *this* component
        ///   should distribute the forage across the TAnimalGroup DMD pools
        /// </summary>
        /// <param name="dAvailPropn"></param>
        /// <param name="dBulkDensity"></param>
        /// <returns></returns>
        public GrazType.TGrazingInputs convertChemistry_MeanDMD(double dAvailPropn, double dBulkDensity)
        {
            const int DDM_MEAN = 0;
            const int IDM_MEAN = 1;

            double dTotalDM;
            double dMeanDMD;
            double[] dDMDPropns = new double[GrazType.DigClassNo + 1];
            int iDMD;

            GrazType.TGrazingInputs Result = new GrazType.TGrazingInputs();

            for (iDMD = 1; iDMD <= GrazType.DigClassNo; iDMD++)
                Result.Herbage[iDMD].HeightRatio = 1.0;

            this.dHerbageDMDFract = new double[GrazType.DigClassNo + 1];
            this.dSeedRipeFract = new double[3];

            dTotalDM = FChemData[DDM_MEAN].dMass_KgHa + FChemData[IDM_MEAN].dMass_KgHa;

            // Leaf & stem pools
            if ((FSeedType == NOT_SEED) && (dTotalDM > 0.0))
            {
                dMeanDMD = FChemData[DDM_MEAN].dMass_KgHa / dTotalDM;
                if (FIsGreen)
                    dDMDPropns = ForageInfo.calcDMDDistribution(dMeanDMD, 0.85, 0.45);    // FIX ME: the DMD ranges should be organ- and development-specific values
                else
                    dDMDPropns = ForageInfo.calcDMDDistribution(dMeanDMD, 0.70, 0.30);

                for (iDMD = 1; iDMD <= GrazType.DigClassNo; iDMD++)
                {
                    populateIntakeRecord(ref Result.Herbage[iDMD], iDMD, (dDMDPropns[iDMD] == 1.0),
                                          dTotalDM, dMeanDMD, dAvailPropn * dDMDPropns[iDMD],
                                          FChemData[DDM_MEAN].dNitrogen_KgHa, FChemData[IDM_MEAN].dNitrogen_KgHa,
                                          FChemData[DDM_MEAN].dPhosphorus_KgHa, FChemData[IDM_MEAN].dPhosphorus_KgHa,
                                          FChemData[DDM_MEAN].dSulphur_KgHa, FChemData[IDM_MEAN].dSulphur_KgHa,
                                          FChemData[DDM_MEAN].dAshAlk_MolHa, FChemData[IDM_MEAN].dAshAlk_MolHa,
                                          dBulkDensity);

                    if (FIsGreen)
                        Result.TotalGreen = Result.TotalGreen + Result.Herbage[iDMD].Biomass;
                    else
                        Result.TotalDead = Result.TotalDead + Result.Herbage[iDMD].Biomass;

                    this.dHerbageDMDFract[iDMD] = Result.Herbage[iDMD].Biomass; // for later division by the total for the DMD class
                }  // for iDMD = 1 to DigClassNo

                populateHerbageType(ref Result);
            }

            // Seed pools
            else if (((FSeedType == GrazType.UNRIPE) || (FSeedType == GrazType.RIPE)) && (dTotalDM > 0.0))
            {
                populateSeedRecord(ref Result, dAvailPropn, DDM_MEAN, DDM_MEAN);
                this.dSeedRipeFract[FSeedType] = Result.Seeds[1, FSeedType].Biomass; // for later division by the total for the seed ripeness class
            }
            return Result;
        }

        /// <summary>
        /// The providing forage component is specifying a list of quantities of forage,
        ///   each of which is assumed to have DMD uniformly distributed across one of
        ///   the TAnimalGroup DMD pools
        /// </summary>
        /// <param name="dAvailPropn"></param>
        /// <param name="dBulkDensity"></param>
        /// <returns></returns>
        public GrazType.TGrazingInputs convertChemistry_DMDClasses6(double dAvailPropn, double dBulkDensity)
        {
            int iChem;
            int iDMD;

            GrazType.TGrazingInputs Result = new GrazType.TGrazingInputs();
            for (iDMD = 1; iDMD <= GrazType.DigClassNo; iDMD++)
                Result.Herbage[iDMD].HeightRatio = 1.0;

            this.dHerbageDMDFract = new double[GrazType.DigClassNo + 1];
            this.dSeedRipeFract = new double[3];

            // Leaf & stem pools
            if (FSeedType == NOT_SEED)
            {
                for (iChem = 0; iChem <= this.CHEM_COUNT[(int)ForageChemistry.fcDMDClasses6] - 1; iChem++)
                {
                    iDMD = iChem + 1;

                    Result.Herbage[iDMD].Biomass = dAvailPropn * FChemData[iChem].dMass_KgHa;
                    Result.Herbage[iDMD].Digestibility = GrazType.ClassDig[iDMD];
                    Result.Herbage[iDMD].Degradability = Math.Min(0.90, Result.Herbage[iDMD].Digestibility + 0.10); // <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
                    if (Result.Herbage[iDMD].Biomass > 0.0)
                    {
                        Result.Herbage[iDMD].CrudeProtein = FChemData[iChem].dNitrogen_KgHa / FChemData[iChem].dMass_KgHa * GrazType.N2Protein;
                        Result.Herbage[iDMD].PhosContent = FChemData[iChem].dPhosphorus_KgHa / FChemData[iChem].dMass_KgHa;
                        Result.Herbage[iDMD].SulfContent = FChemData[iChem].dSulphur_KgHa / FChemData[iChem].dMass_KgHa;
                        Result.Herbage[iDMD].AshAlkalinity = FChemData[iChem].dAshAlk_MolHa / FChemData[iChem].dMass_KgHa;
                    }
                    Result.Herbage[iDMD].HeightRatio = GrazType.REF_HERBAGE_BD / dBulkDensity;
                    if (FIsGreen)
                        Result.TotalGreen = Result.TotalGreen + Result.Herbage[iDMD].Biomass;
                    else
                        Result.TotalDead = Result.TotalDead + Result.Herbage[iDMD].Biomass;

                    this.dHerbageDMDFract[iDMD] = Result.Herbage[iDMD].Biomass; // for later division by the total for the DMD class
                }

                populateHerbageType(ref Result);
            }

            // Seed pools: not permitted
            else if ((FSeedType == GrazType.UNRIPE) || (FSeedType == GrazType.RIPE))
                throw new Exception("Chemistry \"DMDnn\" may not be used when the organ is seeds, heads or ears");

            return Result;
        }


        /// <summary>
        /// Calculate the proportions in a DMD distribution
        /// </summary>
        /// <param name="fDMD">The mean DMD</param>
        /// <param name="fMaxDMD">Upper range</param>
        /// <param name="fMinDMD">Lower range</param>
        /// <returns></returns>
        static public double[] calcDMDDistribution(double fDMD, double fMaxDMD, double fMinDMD)
        {
            int iHighClass;
            int iLowClass;
            double fRelDMD;

            double[] Result = new double[GrazType.DigClassNo + 1];


            iHighClass = 1 + Convert.ToInt32(Math.Truncate((HIGHEST_DMD - fMaxDMD + EPSILON) / CLASSWIDTH));
            iLowClass = 1 + Convert.ToInt32(Math.Truncate((HIGHEST_DMD - fMinDMD - EPSILON) / CLASSWIDTH));
            if (iHighClass != iLowClass)
                fRelDMD = Math.Max(0.0, Math.Min((fDMD - GrazType.ClassDig[iLowClass])
                                          / (GrazType.ClassDig[iHighClass] - GrazType.ClassDig[iLowClass]), 1.0));
            else
                fRelDMD = 1.0;

            switch (iLowClass - iHighClass + 1)
            {
                case 1: Result[iHighClass] = 1.0;
                    break;
                case 2:
                    {
                        Result[iHighClass + 0] = fRelDMD;
                        Result[iHighClass + 1] = 1.0 - fRelDMD;
                    }
                    break;
                case 3:
                    {
                        Result[iHighClass + 0] = Math.Pow(fRelDMD, 2);
                        Result[iHighClass + 1] = 2.0 * fRelDMD * (1.0 - fRelDMD);
                        Result[iHighClass + 2] = Math.Pow(1.0 - fRelDMD, 2);
                    }
                    break;
                case 4:
                    {
                        Result[iHighClass + 0] = Math.Pow(fRelDMD, 3);
                        Result[iHighClass + 1] = 3.0 * Math.Pow(fRelDMD, 2) * (1.0 - fRelDMD);
                        Result[iHighClass + 2] = 3.0 * fRelDMD * Math.Pow(1.0 - fRelDMD, 2);
                        Result[iHighClass + 3] = Math.Pow(1.0 - fRelDMD, 3);
                    }
                    break;
                case 5:
                    {
                        Result[iHighClass + 0] = Math.Pow(fRelDMD, 4);
                        Result[iHighClass + 1] = 4.0 * Math.Pow(fRelDMD, 3) * (1.0 - fRelDMD);
                        Result[iHighClass + 2] = 6.0 * Math.Pow(fRelDMD, 2) * Math.Pow(1.0 - fRelDMD, 2);
                        Result[iHighClass + 3] = 4.0 * fRelDMD * Math.Pow(1.0 - fRelDMD, 3);
                        Result[iHighClass + 4] = Math.Pow(1.0 - fRelDMD, 4);
                    }
                    break;
                default:
                    throw new Exception("Error in DMD distribution calculation");
            }

            return Result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="IR"></param>
        /// <param name="iDMD"></param>
        /// <param name="bUseMeanDMD"></param>
        /// <param name="dTotalMass"></param>
        /// <param name="dMeanDMD"></param>
        /// <param name="dMassFract"></param>
        /// <param name="dDDM_N"></param>
        /// <param name="dIDM_N"></param>
        /// <param name="dDDM_P"></param>
        /// <param name="dIDM_P"></param>
        /// <param name="dDDM_S"></param>
        /// <param name="dIDM_S"></param>
        /// <param name="dDDM_AA"></param>
        /// <param name="dIDM_AA"></param>
        /// <param name="dBulkDensity"></param>
        public void populateIntakeRecord(ref GrazType.IntakeRecord IR,
                                        int iDMD,
                                        bool bUseMeanDMD,
                                        double dTotalMass,
                                        double dMeanDMD,
                                        double dMassFract,
                                        double dDDM_N, double dIDM_N,
                                        double dDDM_P, double dIDM_P,
                                        double dDDM_S, double dIDM_S,
                                        double dDDM_AA, double dIDM_AA,
                                        double dBulkDensity)
        {
            double dDDMPropn;
            double dIDMPropn;

            if (bUseMeanDMD)
                IR.Digestibility = dMeanDMD;
            else
                IR.Digestibility = GrazType.ClassDig[iDMD];

            if (dMassFract > 0.0)
            {
                dDDMPropn = dMassFract * (IR.Digestibility / dMeanDMD);
                if ((1.0 - dMeanDMD) != 0)
                    dIDMPropn = dMassFract * ((1.0 - IR.Digestibility) / (1.0 - dMeanDMD));
                else                // trap div/0
                    dIDMPropn = 0;

                IR.Biomass = dMassFract * dTotalMass;
                IR.CrudeProtein = GrazType.N2Protein * (dDDMPropn * dDDM_N + dIDMPropn * dIDM_N) / IR.Biomass;
                IR.Degradability = Math.Min(0.90, IR.Digestibility + 0.10);
                IR.PhosContent = (dDDMPropn * dDDM_P + dIDMPropn * dIDM_P) / IR.Biomass;
                IR.SulfContent = (dDDMPropn * dDDM_S + dIDMPropn * dIDM_S) / IR.Biomass;
                IR.AshAlkalinity = (dDDMPropn * dDDM_AA + dIDMPropn * dIDM_AA) / IR.Biomass;
                IR.HeightRatio = GrazType.REF_HERBAGE_BD / dBulkDensity;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="GI"></param>
        /// <param name="dAvailPropn"></param>
        /// <param name="iDDM"></param>
        /// <param name="iIDM"></param>
        public void populateSeedRecord(ref GrazType.TGrazingInputs GI, double dAvailPropn, int iDDM, int iIDM)
        {
            double dTotalDM;
            double dMeanDMD;

            dTotalDM = FChemData[iDDM].dMass_KgHa + FChemData[iIDM].dMass_KgHa;
            dMeanDMD = FChemData[iDDM].dMass_KgHa / dTotalDM;

            GI.Seeds[1, FSeedType].Biomass = dAvailPropn * dTotalDM;
            GI.Seeds[1, FSeedType].Digestibility = dMeanDMD;
            GI.Seeds[1, FSeedType].Degradability = Math.Min(0.90, dMeanDMD + 0.10);
            if (GI.Seeds[1, FSeedType].Biomass > 0.0)
            {
                GI.Seeds[1, FSeedType].CrudeProtein = (FChemData[iDDM].dNitrogen_KgHa + FChemData[iIDM].dNitrogen_KgHa) / dTotalDM * GrazType.N2Protein;
                GI.Seeds[1, FSeedType].PhosContent = (FChemData[iDDM].dPhosphorus_KgHa + FChemData[iIDM].dPhosphorus_KgHa) / dTotalDM;
                GI.Seeds[1, FSeedType].SulfContent = (FChemData[iDDM].dSulphur_KgHa + FChemData[iIDM].dSulphur_KgHa) / dTotalDM;
                GI.Seeds[1, FSeedType].AshAlkalinity = (FChemData[iDDM].dAshAlk_MolHa + FChemData[iIDM].dAshAlk_MolHa) / dTotalDM;
            }
            GI.Seeds[1, FSeedType].HeightRatio = 0.0;
        }

        /// <summary>
        /// Set the LegumePropn, SelectFactor and TropLegume fields of a TGrazingInputs
        /// * Expects that the TotalGreen and TotalDead fields have already been computed
        /// </summary>
        /// <param name="GI"></param>
        public void populateHerbageType(ref GrazType.TGrazingInputs GI)
        {
            if (GI.TotalGreen + GI.TotalDead > 0.0)
            {
                GI.LegumePropn = FLegumeMass / (GI.TotalGreen + GI.TotalDead);
                GI.SelectFactor = 0.16 * this.FC4GrassMass / (GI.TotalGreen + GI.TotalDead);
                GI.LegumeTrop = 0.0; // FIX ME
            }
            else
            {
                GI.LegumePropn = 0.0;
                GI.SelectFactor = 0.0;
                GI.LegumeTrop = 0.0;
            }
        }
        
        /// <summary>
        /// Full identifier for this forage e.g. Crop or pasture component full path name
        /// </summary>
        public string sName;             
        /// <summary>
        /// The cohortID from the incoming AvailableToAnimal forage component
        /// </summary>
        public string sCohortID;             
        /// <summary>
        /// Forage organ
        /// </summary>
        public string sOrgan;
        /// <summary>
        /// Forage item age class
        /// </summary>
        public string sAgeClass;

        /// <summary>
        /// The paddock of this forage
        /// </summary>
        public PaddockInfo InPaddock;
        /// <summary>
        /// The herbage info
        /// </summary>
        public GrazType.TPopnHerbageData HerbageData;
        /// <summary>
        /// Amount of this forage removed (output)
        /// </summary>
        public GrazType.TGrazingOutputs RemovalKG;                                       

        /// <summary>
        /// Construct a forage info
        /// </summary>
        public ForageInfo()
        {
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="forageInputs"></param>
        public void setAvailForage(GrazType.TGrazingInputs forageInputs)
        {
            FUseForageData = true;
            FForageData.CopyFrom(forageInputs);
        }

        /// <summary>
        /// Calculates the TGrazingInputs values from the values stored during addForageData() 
        /// </summary>
        /// <returns></returns>
        public GrazType.TGrazingInputs availForage()
        {
            GrazType.TGrazingInputs Result = new GrazType.TGrazingInputs();
                        
            Result.CopyFrom(FForageData);
            
            return Result;
        }

        /// <summary>
        /// Returns True if something has been removed from herbage or seed pool.
        /// </summary>
        /// <returns></returns>
        public bool somethingRemoved()
        {
            int i;

            bool result = false;

            // iterate through all the classes
            i = 1;
            while ((i <= GrazType.DigClassNo) && (!result))
            {
                if (RemovalKG.Herbage[i] > 0.0)
                    result = true;
                i++;
            }
            if (!result)
            {
                i = 1;
                while ((i <= GrazType.MaxPlantSpp) && (!result))
                {
                    if ((RemovalKG.Seed[i, GrazType.UNRIPE] > 0.0) || (RemovalKG.Seed[i, GrazType.RIPE] > 0.0))
                        result = true;
                    i++;
                }
            }
            return result;
        }

        /// <summary>
        /// Bottom of forage
        /// </summary>
        public double dBottom
        {
            get { return getBottom(); }
            set { this.FBottom_MM = value; }
        }
        /// <summary>
        /// Top of forage
        /// </summary>
        public double dTop
        {
            get { return getTop(); }
            set { FTop_MM = value; }
        }
    }

    // ============================================================================
    /// <summary>
    /// List of ForageInfo forages 
    /// </summary>
    [Serializable]
    public class ForageList
    {
        private ForageInfo[] FList;
        bool FOwnsList;

        /// <summary>
        /// Construct the forage list
        /// </summary>
        /// <param name="ownsForages"></param>
        public ForageList(bool ownsForages)
        {
            FOwnsList = ownsForages;
            Array.Resize(ref FList, 0);
        }
        /// <summary>
        /// Count of forages
        /// </summary>
        /// <returns></returns>
        public int Count()
        {
            return FList.Length;
        }
        /// <summary>
        /// Add a forage item
        /// </summary>
        /// <param name="Info"></param>
        public void Add(ForageInfo Info)
        {
            int Idx;

            Idx = FList.Length;
            Array.Resize(ref FList, Idx + 1);
            FList[Idx] = Info;
        }
        /// <summary>
        /// Add a forage by name
        /// </summary>
        /// <param name="sName"></param>
        /// <returns></returns>
        public ForageInfo Add(string sName)
        {
            ForageInfo newInfo;

            newInfo = new ForageInfo();
            newInfo.sName = sName.ToLower();
            this.Add(newInfo);
            return newInfo;
        }
        /// <summary>
        /// Delete a forage by index
        /// </summary>
        /// <param name="iValue"></param>
        public void Delete(int iValue)
        {
            int Idx;

            if (FOwnsList)
                FList[iValue] = null;
            for (Idx = iValue + 1; Idx <= FList.Length - 1; Idx++)
                FList[Idx - 1] = FList[Idx];
            Array.Resize(ref FList, FList.Length - 1);
        }
        /// <summary>
        /// Get a forage by index
        /// </summary>
        /// <param name="Idx"></param>
        /// <returns></returns>
        public ForageInfo byIndex(int Idx)
        {
            ForageInfo result = null;
            if ((Idx >= 0) && (Idx < FList.Length))
                result = FList[Idx];

            return result;
        }
        /// <summary>
        /// Get a forage by name
        /// </summary>
        /// <param name="sName"></param>
        /// <returns></returns>
        public ForageInfo byName(string sName)
        {
            int Idx;

            Idx = IndexOf(sName);
            if (Idx >= 0)
                return this.byIndex(Idx);
            else
                return null;
        }
        /// <summary>
        /// Get the index of a forage by name
        /// </summary>
        /// <param name="sName"></param>
        /// <returns></returns>
        public int IndexOf(string sName)
        {
            int Result = this.Count() - 1;
            while ((Result >= 0) && (this.byIndex(Result).sName.ToLower() != sName.ToLower()))
            {
                Result--;
            }

            return Result;
        }
    }

    /// <summary>
    /// New forage interface for AvailableToAnimal support
    /// TForageProvider maps to a cmp component such as Plant or AgPasture.
    /// Each of these can contain 0..n forage. The forage will be named
    /// with the cohortid string.
    /// </summary>
    [Serializable]
    public class ForageProvider
    {
        bool FUseCohorts;                       // uses new cohorts array type
        private ForageList FForages;
        private string FForageHostName;         // host crop, pasture component name
        private int FHostID;                    // plant/pasture comp
        private string FPaddockOwnerName;       // owning paddock FQN
        private int FSetterID;                  // setting property ID (or published event ID)
        private int FDriverID;                  // driving property ID
        private PaddockInfo FOwningPaddock;     // ptr to the paddock object in the model
        /// <summary>
        /// Construct the forage provider
        /// </summary>
        public ForageProvider()
        {
            FForages = new ForageList(true);
            OwningPaddock = null;
            FUseCohorts = false;
        }

        /// <summary>
        /// Use forage cohorts
        /// </summary>
        public bool UseCohorts { get { return FUseCohorts; } set { FUseCohorts = value; } }
        /// <summary>
        /// The owning paddock
        /// </summary>
        public PaddockInfo OwningPaddock { get { return FOwningPaddock; } set { FOwningPaddock = value; } }
        /// <summary>
        /// The paddock owner name
        /// </summary>
        public string PaddockOwnerName { get { return FPaddockOwnerName; } set { FPaddockOwnerName = value; } }
        /// <summary>
        /// Forage host name
        /// </summary>
        public string ForageHostName { get { return FForageHostName; } set { FForageHostName = value; } }
        /// <summary>
        /// Component id of the host
        /// </summary>
        public int HostID { get { return FHostID; } set { FHostID = value; } }
        /// <summary>
        /// Driver id
        /// </summary>
        public int DriverID { get { return FDriverID; } set { FDriverID = value; } }
        /// <summary>
        /// Setter id
        /// </summary>
        public int SetterID { get { return FSetterID; } set { FSetterID = value; } }
        /// <summary>
        /// The crop, pasture component
        /// </summary>
        public Object ForageObj { get; set; }

        /// <summary>
        /// Update the forage data for this crop/agpasture object
        /// </summary>
        /// <param name="forageObj">The crop/pasture object</param>
        public void UpdateForages(Object forageObj)
        {
            //ensure this forage is in the list
            //the forage key in this case is component name
            ForageInfo forage = FForages.byName(ForageHostName);
            if (forage == null)  //if this cohort doesn't exist in the forage list then
            {
                forage = new ForageInfo();
                forage.sName = ForageHostName.ToLower();
                FOwningPaddock.AssignForage(forage);              //the paddock in the model can access this forage
                FForages.Add(forage); //create a new forage for this cohort
            }
            
            // TODO: just assuming one forage cohort in this component (expand here?)
            passGrazingInputs(forage, Crop2GrazingInputs(forageObj), "g/m^2"); //then update it's value
        }

        /// <summary>
        /// The forage name is the name of the cohort.
        /// </summary>
        /// <param name="forageName"></param>
        /// <returns></returns>
        public ForageInfo ForageByName(string forageName)
        {
            return FForages.byName(forageName);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="idx">idx = 0..n</param>
        /// <returns></returns>
        public ForageInfo ForageByIndex(int idx)
        {
            return FForages.byIndex(idx);
        }

        /// <summary>
        /// Use the GrazingInputs to initialise the forage object
        /// </summary>
        /// <param name="forage"></param>
        /// <param name="Grazing"></param>
        /// <param name="sUnit"></param>
        public void passGrazingInputs(ForageInfo forage,
                                      GrazType.TGrazingInputs Grazing,
                                      string sUnit)
        {
            double fScale;

            if (sUnit == "kg/ha")                                                     // Convert to kg/ha                      
                fScale = 1.0;
            else if (sUnit == "g/m^2")
                fScale = 10.0;
            else
                throw new Exception("Stock: Unit (" + sUnit + ") not recognised");

            if (forage != null)
                forage.setAvailForage(GrazType.scaleGrazingInputs(Grazing, fScale));
            else
                throw new Exception("Stock: Forage not recognised");
        }

        /// <summary>
        /// Copies a Plant/AgPasture object biomass organs into TGrazingInputs object
        /// This object may then get scaled to kg/ha
        /// </summary>
        /// <param name="forageObj">The forage object - a Plant/AgPasture component</param>
        /// <returns>The grazing inputs</returns>
        private GrazType.TGrazingInputs Crop2GrazingInputs(Object forageObj)
        {
            GrazType.TGrazingInputs result = new GrazType.TGrazingInputs();
            GrazType.zeroGrazingInputs(ref result);
            
            result.TotalGreen = 0;
            result.TotalDead = 0;

            double totalDMD = 0;
            double totalN = 0;
            double meanDMD = 0;
            double dmd;

            // calculate the total live and dead biomass
            foreach (IRemovableBiomass biomass in Apsim.Children((IModel)forageObj, typeof(IRemovableBiomass)))
            {
                if (biomass.IsAboveGround)
                {
                    if (biomass.Live.Wt > 0 || biomass.Dead.Wt > 0)
                    {
                        result.TotalGreen += biomass.Live.Wt;   // g/m^2
                        result.TotalDead += biomass.Dead.Wt;

                        dmd = ((biomass.Live.DMDOfStructural * biomass.Live.StructuralWt) + (1 * biomass.Live.StorageWt) + (1 * biomass.Live.MetabolicWt));    // storage and metab are 100% dmd
                        dmd += ((biomass.Dead.DMDOfStructural * biomass.Dead.StructuralWt) + (1 * biomass.Dead.StorageWt) + (1 * biomass.Dead.MetabolicWt));
                        totalDMD += dmd;
                        totalN += biomass.Live.N + biomass.Dead.N;
                    }
                }
            }

            double totalDM = result.TotalGreen + result.TotalDead;

            // TODO: Improve this routine

            if (totalDM > 0)
            {
                meanDMD = totalDMD / totalDM; //calc the average dmd for the plant

                //get the dmd distribution
                double[] dDMDPropns = new double[GrazType.DigClassNo + 1];

                //green 0.85-0.45, dead 0.70-0.30
                dDMDPropns = ForageInfo.calcDMDDistribution(meanDMD, 0.85, 0.45);    // FIX ME: the DMD ranges should be organ- and development-specific values

                for (int idx = 1; idx <= GrazType.DigClassNo; idx++)
                {
                    result.Herbage[idx].Biomass = dDMDPropns[idx] * totalDM;
                    result.Herbage[idx].CrudeProtein = dDMDPropns[idx] * totalN * GrazType.N2Protein;
                    result.Herbage[idx].Digestibility = GrazType.ClassDig[idx];
                    result.Herbage[idx].Degradability = Math.Min(0.90, result.Herbage[idx].Digestibility + 0.10);
                    result.Herbage[idx].HeightRatio = 1;
                    result.Herbage[idx].PhosContent = 0;    //N * 0.05?
                    result.Herbage[idx].SulfContent = 0;    //N * 0.07?
                    result.Herbage[idx].AshAlkalinity = 0.70;   // TODO: use a modelled value
                }

                result.LegumePropn = 0.0;   // TODO: set from Plant model value
                result.SelectFactor = 0;    // TODO: set from Plant model value

                // TODO: Store the seed pools
            }

            return result;
        }

        /// <summary>
        /// The herbage is removed from the plant/agpasture
        /// </summary>
        public void RemoveHerbageFromPlant()
        {
            string chemType = string.Empty;
            int forageIdx = 0;

            ForageInfo forage = this.ForageByIndex(forageIdx);
            while (forage != null)
            {
                double area = forage.InPaddock.fArea;
                GrazType.TGrazingOutputs removed = forage.RemovalKG;

                // total the amount removed kg/ha
                double totalRemoved = 0.0;
                for (int i = 0; i < removed.Herbage.Length; i++)
                    totalRemoved += removed.Herbage[i];
                double propnRemoved = totalRemoved / (forage.TotalLive + forage.TotalDead);

                // calculations of proportions each organ of the total plant removed (in the native units)
                double totalDM = 0;
                foreach (IRemovableBiomass organ in Apsim.Children((IModel)this.ForageObj, typeof(IRemovableBiomass)))
                {
                    if (organ.IsAboveGround && (organ.Live.Wt + organ.Dead.Wt) > 0)
                    {
                        totalDM += organ.Live.Wt + organ.Dead.Wt;
                    }
                }

                foreach (IRemovableBiomass organ in Apsim.Children((IModel)this.ForageObj, typeof(IRemovableBiomass)))
                {
                    if (organ.IsAboveGround && (organ.Live.Wt + organ.Dead.Wt) > 0)
                    {
                        double propnOfPlantDM = (organ.Live.Wt + organ.Dead.Wt) / totalDM;
                        double prpnToRemove = propnRemoved * propnOfPlantDM / (1.0 - propnOfPlantDM);
                        PMF.OrganBiomassRemovalType removal = new PMF.OrganBiomassRemovalType();
                        removal.FractionDeadToRemove = prpnToRemove;
                        removal.FractionLiveToRemove = prpnToRemove;
                        organ.RemoveBiomass("Graze", removal);
                    }
                }

                forageIdx++;
                forage = this.ForageByIndex(forageIdx);
            }
        }

        /// <summary>
        /// Return the removal
        /// </summary>
        /// <param name="forage"></param>
        /// <param name="sUnit"></param>
        /// <returns></returns>
        public GrazType.TGrazingOutputs ReturnRemoval(ForageInfo forage, string sUnit)
        {
            double fArea;
            double fScale;
            int iClass;
            int iSpecies;
            int iRipe;

            GrazType.TGrazingOutputs Result = new GrazType.TGrazingOutputs();

            if (forage != null)
            {
                Result = forage.RemovalKG;
                fArea = forage.InPaddock.fArea;
            }
            else
            {
                Result = new GrazType.TGrazingOutputs();
                fArea = 0.0;
            }

            if (fArea > 0.0)
            {
                if (sUnit == "kg")
                    fScale = 1.0;
                else if (sUnit == "g/m^2")
                    fScale = 0.10 / fArea;
                else if (sUnit == "kg/ha")
                    fScale = 1.0 / fArea;
                else
                    throw new Exception("Stock: Unit (" + sUnit + ") not recognised");

                if (fScale != 1.0)
                {
                    for (iClass = 1; iClass <= GrazType.DigClassNo; iClass++)
                        Result.Herbage[iClass] = fScale * Result.Herbage[iClass];
                    for (iSpecies = 1; iSpecies <= GrazType.MaxPlantSpp; iSpecies++)
                        for (iRipe = GrazType.UNRIPE; iRipe <= GrazType.RIPE; iRipe++)
                            Result.Seed[iSpecies, iRipe] = fScale * Result.Seed[iSpecies, iRipe];
                }
            }
            return Result;
        }

        /// <summary>
        /// Test the Removal to determine if there is any quantity of forage removed.
        /// </summary>
        /// <returns></returns>
        public bool somethingRemoved()
        {
            int forageIdx;
            ForageInfo aForage;

            bool result = false;

            //get the removal for each forage
            forageIdx = 0;
            aForage = ForageByIndex(forageIdx);
            while ((aForage != null) && (!result))
            {
                result = aForage.somethingRemoved();
                forageIdx++;
                aForage = ForageByIndex(forageIdx);
            }
            return result;
        }
    }

    /// <summary>
    /// ForageProviders is a collection of forage/cmp components that each in turn
    /// supply 1..n forage plants/species
    /// </summary>
    [Serializable]
    public class ForageProviders
    {
        private List<ForageProvider> FForageProviderList;
        /// <summary>
        /// Construct a forage provider
        /// </summary>
        public ForageProviders()
        {
            FForageProviderList = new List<ForageProvider>();
        }
        /// <summary>
        /// Count of forage providers
        /// </summary>
        /// <returns></returns>
        public int Count()
        {
            return FForageProviderList.Count();
        }

        /// <summary>
        /// Add a forage provider component
        /// </summary>
        /// <param name="paddock"></param>
        /// <param name="paddName"></param>
        /// <param name="forageName"></param>
        /// <param name="hostID"></param>
        /// <param name="driverID"></param>
        /// <param name="forageObj"></param>
        /// <param name="usePlantCohorts"></param>
        public void AddProvider(PaddockInfo paddock, string paddName, string forageName, int hostID, int driverID, Object forageObj, bool usePlantCohorts)
        {
            ForageProvider forageProvider;

            //this is a forage provider
            // this provider can host a number of forages/species
            forageProvider = new ForageProvider();
            forageProvider.PaddockOwnerName = paddName;    // owning paddock
            forageProvider.ForageHostName = forageName;    // host pasture/plant component name
            forageProvider.HostID = hostID;                // plant/pasture comp
            forageProvider.DriverID = driverID;            // driving property ID
            forageProvider.ForageObj = forageObj;          // setting property ID
            // keep a ptr to the paddock owned by the model so the forages can be assigned there as they become available
            forageProvider.OwningPaddock = paddock;
            forageProvider.UseCohorts = usePlantCohorts;   // use cohorts array type

            FForageProviderList.Add(forageProvider);
        }

        /// <summary>
        /// Find the forage provider for this forage/provider name
        /// </summary>
        /// <param name="providerName"></param>
        /// <returns></returns>
        public ForageProvider FindProvider(string providerName)
        {
            ForageProvider provider;
            int i;

            provider = null;
            i = 0;
            while ((provider == null) && (i <= FForageProviderList.Count - 1))
            {
                //if the name matches
                if (FForageProviderList[i].ForageHostName == providerName)
                    provider = FForageProviderList[i];
                i++;
            }
            return provider;
        }

        /// <summary>
        /// Find the forage provider for this component ID. Ensure this is the correct driver also.
        /// </summary>
        /// <param name="hostID"></param>
        /// <param name="driverID"></param>
        /// <returns></returns>
        public ForageProvider FindProvider(int hostID, int driverID)
        {
            ForageProvider provider;
            int i;

            provider = null;
            i = 0;
            while ((provider == null) || (i <= FForageProviderList.Count - 1))
            {
                // if the host and driver match
                if ((FForageProviderList[i].DriverID == driverID) && (FForageProviderList[i].HostID == hostID))
                    provider = FForageProviderList[i];
                i++;
            }
            return provider;
        }

        /// <summary>
        /// Find the forage provider for this component ID.
        /// </summary>
        /// <param name="hostID"></param>
        /// <returns></returns>
        public ForageProvider FindProvider(int hostID)
        {
            ForageProvider provider;
            int i;

            provider = null;
            i = 0;
            while ((provider == null) && (i <= FForageProviderList.Count - 1))
            {
                // if the host matches
                if (FForageProviderList[i].HostID == hostID)
                    provider = FForageProviderList[i];
                i++;
            }
            return provider;
        }

        /// <summary>
        /// Get a forage provider from the list. idx = 0..n
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        public ForageProvider ForageProvider(int idx)
        {
            if ((idx >= 0) && (idx < FForageProviderList.Count))
                return FForageProviderList[idx];
            else
                return null;
        }
    }

    /// <summary>
    /// Paddock details
    /// </summary>
    [Serializable]
    public class PaddockInfo
    {
        /// <summary>
        /// Missing item
        /// </summary>
        public const int NOTTHERE = -1;

        /// <summary>
        /// Slope in degrees
        /// </summary>
        private double FSlope;                                      
        /// <summary>
        /// Steepness code (1-2)
        /// </summary>
        private double FSteepness;                                     
        /// <summary>
        /// CAREFUL - FForages does not own its members
        /// </summary>
        private ForageList FForages;                                       
        private TSupplementRation FSuppInPadd;                                                  
        private bool FUseHerbageAmt;

        private void SetSlope(double fValue)
        {
            FSlope = fValue;
            FSteepness = 1.0 + Math.Min(1.0, Math.Sqrt(Math.Sin(FSlope * Math.PI / 180) / Math.Cos(FSlope * Math.PI / 180)));
        }
#pragma warning disable 1591 //missing xml comment
        public double FSummedGreenMass;
        public string sName;
        public int iPaddID;
        public Object paddObj;
        public string sExcretionDest;
        public int iExcretionID;
        public string sUrineDest;
        public int iAddFaecesID;                                    // id of published event
        public Object AddFaecesObj;
        public int iAddUrineID;                                     // id of published event
        public Object AddUrineObj;
        public double fArea;                                        // Paddock area (ha)                     
        public double fWaterlog;                                    // Waterlogging index (0-1)              

        public double fSummedPotIntake;                             // total pot. intake                     
        public double SuppRemovalKG;
#pragma warning restore 1591 //missing xml comment
        /// <summary>
        /// Paddock slope
        /// </summary>
        public double Slope
        {
            get { return FSlope; }
            set { SetSlope(value); }
        }
        /// <summary>
        /// Steepness code (1-2)
        /// </summary>
        public double Steepness
        {
            get { return FSteepness; }
        }
        /// <summary>
        /// Supplement that is in the paddock
        /// </summary>
        public TSupplementRation SuppInPadd
        {
            get { return FSuppInPadd; }
        }
        /// <summary>
        /// Get the forage list
        /// </summary>
        public ForageList Forages
        {
            get { return FForages; }
        }
        /// <summary>
        /// Create the PaddockInfo
        /// </summary>
        public PaddockInfo()
        {
            FForages = new ForageList(false);
            FSuppInPadd = new TSupplementRation();
            this.fArea = 1.0;
            FSlope = 0.0;
            iExcretionID = NOTTHERE;
            iAddUrineID = NOTTHERE;
            iAddFaecesID = NOTTHERE;
        }
        /// <summary>
        /// Assign a forage to this paddock
        /// </summary>
        /// <param name="Forage"></param>
        public void AssignForage(ForageInfo Forage)
        {
            FForages.Add(Forage);
            Forage.InPaddock = this;
        }
        /// <summary>
        /// Set the herbage used
        /// </summary>
        /// <param name="bValue"></param>
        public void setUseHerbageAmount(bool bValue)
        {
            FUseHerbageAmt = bValue;
        }

        /// <summary>
        /// { Aggregates the initial forage availability of each species in the list       
        /// * If FForages.Count=0, then the aggregate forage availability is taken to    
        ///   have been passed at the paddock level using setGrazingInputs()             
        /// </summary>
        public void computeTotals()
        {

            int Jdx;

            FSummedGreenMass = 0.0;
            for (Jdx = 0; Jdx <= Forages.Count() - 1; Jdx++)
            {
                Forages.byIndex(Jdx).summariseInitHerbage();
                FSummedGreenMass = FSummedGreenMass + Forages.byIndex(Jdx).FGreenMass;
            }
        }
        /// <summary>
        /// Zero the removal amounts
        /// </summary>
        public void zeroRemoval()
        {
            int Jdx;

            SuppRemovalKG = 0.0;
            for (Jdx = 0; Jdx <= Forages.Count() - 1; Jdx++)
                Forages.byIndex(Jdx).RemovalKG = new GrazType.TGrazingOutputs();
        }
        /// <summary>
        /// Feed the supplement
        /// </summary>
        /// <param name="fNewAmount"></param>
        /// <param name="NewSupp"></param>
        public void FeedSupplement(double fNewAmount, TSupplement NewSupp)
        {
            TSupplement aSupp;
            bool bFound;
            int Idx;

            if (fNewAmount > 0.0)
            {
                Idx = 0;
                bFound = false;
                while (!bFound && (Idx < SuppInPadd.Count))
                {
                    bFound = NewSupp.isSameAs(SuppInPadd[Idx]);
                    if (!bFound)
                        Idx++;
                }
                if (bFound)
                    SuppInPadd[Idx].Amount = SuppInPadd[Idx].Amount + fNewAmount;    
                else
                {
                    aSupp = new TSupplement();
                    aSupp.Assign(NewSupp);
                    SuppInPadd.Add(aSupp, fNewAmount);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void ClearSupplement()
        {
            SuppInPadd.Clear();
        }
    }

    /// <summary>
    /// The TPaddockList class
    /// </summary>
    [Serializable]
    public class TPaddockList
    {
        private PaddockInfo[] FList;
        /// <summary>
        /// Create the TPaddockList
        /// </summary>
        public TPaddockList()
        {
            Array.Resize(ref FList, 0);
        }
        /// <summary>
        /// Get the count of paddocks
        /// </summary>
        /// <returns></returns>
        public int Count()
        {
            return FList.Length;
        }
        /// <summary>
        /// Add a paddock
        /// </summary>
        /// <param name="Info"></param>
        public void Add(PaddockInfo Info)
        {
            int Idx;

            Idx = FList.Length;
            Array.Resize(ref FList, Idx + 1);
            FList[Idx] = Info;
        }
        /// <summary>
        /// Add a new paddock using ID and name
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="sName"></param>
        public void Add(int ID, string sName)
        {
            PaddockInfo NewPadd;

            NewPadd = new PaddockInfo();
            NewPadd.iPaddID = ID;
            NewPadd.sName = sName.ToLower();
            this.Add(NewPadd);
        }
        /// <summary>
        /// Add a new paddock object using reference and name
        /// </summary>
        /// <param name="paddObj"></param>
        /// <param name="sName"></param>
        public void Add(Object paddObj, string sName)
        {
            PaddockInfo NewPadd;

            NewPadd = new PaddockInfo();
            NewPadd.iPaddID = this.Count() + 1; //???????? ID is 1 based here
            NewPadd.paddObj = paddObj;
            NewPadd.sName = sName.ToLower();
            this.Add(NewPadd);
        }
        /// <summary>
        /// Delete the paddock at the index
        /// </summary>
        /// <param name="iValue"></param>
        public void Delete(int iValue)
        {
            int Idx;

            FList[iValue] = null;
            for (Idx = iValue + 1; Idx <= FList.Length - 1; Idx++)
                FList[Idx - 1] = FList[Idx];
            Array.Resize(ref FList, FList.Length - 1);
        }
        /// <summary>
        /// Get the paddock info at the index
        /// </summary>
        /// <param name="iValue">0-n</param>
        /// <returns></returns>
        public PaddockInfo byIndex(int iValue)
        {
            return FList[iValue];
        }
        /// <summary>
        /// Get the paddock by ID
        /// </summary>
        /// <param name="iValue"></param>
        /// <returns></returns>
        public PaddockInfo byID(int iValue)
        {
            int Idx;

            PaddockInfo Result = null;
            Idx = 0;
            while ((Idx < this.Count()) && (Result == null))
            {
                if (this.byIndex(Idx).iPaddID == iValue)
                    Result = this.byIndex(Idx);
                else
                    Idx++;
            }
            return Result;
        }
        /// <summary>
        /// Get the paddock by object
        /// </summary>
        /// <param name="paddObj">The paddock object</param>
        /// <returns>The paddock info</returns>
        public PaddockInfo byObj(Object paddObj)
        {
            int Idx;

            PaddockInfo Result = null;
            Idx = 0;
            while ((Idx < this.Count()) && (Result == null))
            {
                if (this.byIndex(Idx).paddObj == paddObj)
                    Result = this.byIndex(Idx);
                else
                    Idx++;
            }
            return Result;
        }
        /// <summary>
        /// Get the paddock by name
        /// </summary>
        /// <param name="sName"></param>
        /// <returns></returns>
        public PaddockInfo byName(string sName)
        {
            int Idx;

            Idx = IndexOf(sName);
            if (Idx >= 0)
                return this.byIndex(Idx);
            else
                return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sName"></param>
        /// <returns></returns>
        public int IndexOf(string sName)
        {
            int Result = this.Count() - 1;
            while ((Result >= 0) && (this.byIndex(Result).sName != sName.ToLower()))
                Result--;
            return Result;
        }

        /// <summary>
        /// Initialise at the first timestep
        /// </summary>
        public void beginTimeStep()
        {
            int iPadd;

            for (iPadd = 0; iPadd <= this.Count() - 1; iPadd++)
            {
                this.byIndex(iPadd).ClearSupplement();
                this.byIndex(iPadd).zeroRemoval();
            }
        }
    }
    
}