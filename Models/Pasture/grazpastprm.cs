using static Models.GrazPlan.GrazType;
using System.Collections.Generic;
using System;

#pragma warning disable CS1591
namespace Models.GrazPlan
{
    // Author: A.D. Moore
    // GRAZPLAN pasture model: parameter set.

    /// <summary>
    /// The pasture parameters object
    /// </summary>
    static public class TGPastureParams
    {
        static private TParameterSet _GPastureParams = null;
        /// <summary>
        /// The object that contains the pasture parameters
        /// </summary>
        /// <returns>Reference to the static instance of the pasture parameters</returns>
        static public TPastureParamSet PastureParamsGlb()
        {
            if (_GPastureParams == null)
            {
                _GPastureParams = new TPastureParamSet();
                TGParamFactory.ParamXMLFactory().readDefaults("PASTURE_PARAM_GLB", ref _GPastureParams);
            }
            return (TPastureParamSet)_GPastureParams;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public enum ReproTriggerEnum { trigLongDayLength, trigDegDay, trigShortDayLength };

    /// <summary>
    ///
    /// </summary>
    public class TPastureParamSet : TParameterSet
    {
        private List<string> FEquivFrom;
        private List<string> FEquivTo;

        public string sEditor = "";
        public string sEditDate = "";
        public int iWaterUptakeVersion;

        public bool bAnnual;
        public bool bHasSeeds;
        public bool bVernReqd;                                      // Vernalization requirement?
        public bool bLongDay;                                       // Long day requirement?
        public bool bShortDay;                                      // Short day requirement?
        public ReproTriggerEnum ReproTrigger;
        public bool bSummerDormant;
        public bool bWinterDormant;
        public bool bHasSetback;
        public double[] DevelopK = new double[31];          // K(V)
        public double[] LightK = new double[11];            // K(I)
        public double[] WaterUseK = new double[7];          // K(WU)
        public double[] RadnUseK = new double[7];           // K(RU)
        public double[] TranspEffK = new double[3];         // K(BT)
        public double[] LowT_K = new double[3];             // K(T)
        public double[] WaterK = new double[2];             // K(W)
        public double[] WaterLogK = new double[3];          // K(WL)
        public double[] MeristemK = new double[2];          // K(MR)
        public double[] TranslocK = new double[5];          // K(TL)
        public double[] RespireK = new double[5];           // K(RE)
        public double[] AllocK = new double[6];             // K(A)
        public double[] MorphK = new double[2];             // K(MO)
        public double[] RootK = new double[11];             // K(R)
        public double[] DeathK = new double[10];            // K(D)
        public double[] RootLossK = new double[5];          // K(DR)
        public double[,] FallK = new double[5, 3];          // array[1.. 4,ptLEAF..ptSTEM]  // K(F)
        public double[,] BreakdownK = new double[6, 3];     // array[1.. 5,ptLEAF..ptSTEM]  // K(SH)
        public double[,] MatureK = new double[7, 3];        // array[1.. 6,ptLEAF..ptSTEM]  // K(Q)
        public double[] DecayK = new double[10];            // K(Y,1)-K(Y,10)
        public double[] SeedK = new double[5];              // K(S,1)-K(S,4)
        public double[] SeedDeathK = new double[3];         // K(S,5,s)
        public double[] GermnK = new double[9];             // K(G)
        public double[] SeedlK = new double[2];             // K(Z)
        public double[,,] NutrConcK = new double[5, 4, 5];  // array[1..4,TPlantElement,ptLEAF..ptSEED]     // K(nu,1)-K(nu,4)
        public double[] NFixK = new double[6];              // K(nu,5)-K(nu,9)
        public double[] NutrEffK = new double[4];           // array[TPlantNutrient]            // K(nu,10,nutr)
        public double[] NutrRelocateK = new double[4];      // array[TPlantElement]             // K(nu,11,elem)
        public double[,] NutrCO2K = new double[4, 5];       // array[TPlantElement,ptLEAF..ptSEED]         // K(nu,12,elem)
        public double[] AshAlkK = new double[7];

        public bool bGrass;
        public bool bLegume;
        public bool bisC4;
        public double[] Protein = new double[GrazType.HerbClassNo + 1];      // CP
        public double[] DgProtein = new double[GrazType.HerbClassNo + 1];    // DG
        public int[] Seed_Class = new int[3];                       // array[UNRIPE..RIPE] of 0..DigClassNo     // QS
        public double[] Seed_Dig = new double[3];
        public double Seed_Prot;
        public double HeightRatio;                                  // HR
        public double SelectFactor;                                 // SF

        public double[,,] BaseDeathRates = new double[3, 3, GrazType.HerbClassNo + 1];   // array[1..2,ptLEAF..ptSTEM,1..HerbClassNo]    // Death rates per degree-day
        public double[,,] DMDRates = new double[3, 3, GrazType.HerbClassNo + 1];         // array[1..2,ptLEAF..ptSTEM,1..HerbClassNo]    // DMD decline rates per degree-day
        public double[,,] NewTissuePropns = new double[3, 3, GrazType.HerbClassNo + 1];  // array[1..2,ptLEAF..ptSTEM,1..HerbClassNo]    //   (1=vegetative, 2=reproductive)



        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        override protected TParameterSet makeChild()
        {
            return new TPastureParamSet(this);
        }

        /// <summary>
        ///
        /// </summary>
        override protected void defineEntries()
        {
            defineParameters("editor", ptyText);
            defineParameters("edited", ptyText);

            defineParameters("grass", ptyBool);
            defineParameters("legume", ptyBool);
            defineParameters("annual", ptyBool);
            defineParameters("isc4", ptyBool);
            defineParameters("longday", ptyBool);
            defineParameters("shortday", ptyBool);
            defineParameters("k-v-1:30", ptyReal);
            defineParameters("k-i-1:10", ptyReal);
            defineParameters("k-wu-1:6", ptyReal);
            defineParameters("k-ru-1:6", ptyReal);
            defineParameters("k-bt-1:2", ptyReal);
            defineParameters("k-t-1:2", ptyReal);
            defineParameters("k-w-1", ptyReal);
            defineParameters("k-wl-1:2", ptyReal);
            defineParameters("k-mr-1", ptyReal);
            defineParameters("k-tl-1:4", ptyReal);
            defineParameters("k-re-1:4", ptyReal);
            defineParameters("k-a-1:5", ptyReal);
            defineParameters("k-mo-1:1", ptyReal);
            defineParameters("k-r-1:10", ptyReal);
            defineParameters("k-d-1:9", ptyReal);
            defineParameters("k-dr-1:4", ptyReal);
            defineParameters("k-f1-leaf;stem", ptyReal);
            defineParameters("k-f2-2:4", ptyReal);
            defineParameters("k-br1-leaf;stem", ptyReal);
            defineParameters("k-br2-2:5", ptyReal);
            defineParameters("k-q-leaf;stem-1:6", ptyReal);
            defineParameters("k-y-1:9", ptyReal);
            defineParameters("k-s-1:4", ptyReal);
            defineParameters("k-s-5-soft;hard", ptyReal);
            defineParameters("k-g-1:8", ptyReal);
            defineParameters("k-z-1", ptyReal);

            defineParameters("k-conc-n;p;s-leaf;stem;root;seed-1:5", ptyReal);
            defineParameters("k-fix-1:5", ptyReal);
            defineParameters("k-eff-no3;nh4;pox;so4", ptyReal);
            defineParameters("k-reloc-n;p;s", ptyReal);
            defineParameters("k-aa-1:6", ptyReal);

            defineParameters("k-cp-1:12", ptyReal);
            defineParameters("k-dg-1:12", ptyReal);
            defineParameters("k-seed-unripe;ripe", ptyInt);
            defineParameters("k-dmdseed-unripe;ripe", ptyReal);
            defineParameters("k-cpseed", ptyReal);
            defineParameters("k-hr", ptyReal);
            defineParameters("k-sf", ptyReal);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sTagList"></param>
        /// <param name="Idx"></param>
        /// <returns></returns>
        protected int GetPart(string[] sTagList, int Idx)
        {
            int Result;
            if (sTagList[Idx] == "leaf")
                Result = ptLEAF;
            else if (sTagList[Idx] == "stem")
                Result = ptSTEM;
            else if (sTagList[Idx] == "root")
                Result = ptROOT;
            else if (sTagList[Idx] == "seed")
                Result = ptSEED;
            else
                Result = 0;
            return Result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sTagList"></param>
        /// <returns></returns>
        override protected double getRealParam(string[] sTagList)
        {
            GrazType.TPlantElement Elem;
            GrazType.TPlantNutrient Nutr;
            int iPart;
            int Idx;

            double Result = 0.0;
            if (sTagList[0] == "k")
            {
                if (sTagList[1] == "v")
                    Result = DevelopK[iIndex(sTagList, 2)];
                else if (sTagList[1] == "i")
                    Result = LightK[iIndex(sTagList, 2)];
                else if (sTagList[1] == "wu")
                    Result = WaterUseK[iIndex(sTagList, 2)];
                else if (sTagList[1] == "ru")
                    Result = RadnUseK[iIndex(sTagList, 2)];
                else if (sTagList[1] == "bt")
                    Result = TranspEffK[iIndex(sTagList, 2)];
                else if (sTagList[1] == "t")
                    Result = LowT_K[iIndex(sTagList, 2)];
                else if (sTagList[1] == "w")
                    Result = WaterK[iIndex(sTagList, 2)];
                else if (sTagList[1] == "wl")
                    Result = WaterLogK[iIndex(sTagList, 2)];
                else if (sTagList[1] == "mr")
                    Result = MeristemK[iIndex(sTagList, 2)];
                else if (sTagList[1] == "tl")
                    Result = TranslocK[iIndex(sTagList, 2)];
                else if (sTagList[1] == "re")
                    Result = RespireK[iIndex(sTagList, 2)];
                else if (sTagList[1] == "a")
                    Result = AllocK[iIndex(sTagList, 2)];
                else if (sTagList[1] == "mo")
                    Result = MorphK[iIndex(sTagList, 2)];
                else if (sTagList[1] == "r")
                    Result = RootK[iIndex(sTagList, 2)];
                else if (sTagList[1] == "d")
                    Result = DeathK[iIndex(sTagList, 2)];
                else if (sTagList[1] == "dr")
                    Result = RootLossK[iIndex(sTagList, 2)];
                else if (sTagList[1] == "f1")
                {
                    iPart = GetPart(sTagList, 2);
                    Result = FallK[1, iPart];
                }
                else if (sTagList[1] == "f2")
                {
                    Idx = iIndex(sTagList, 2);
                    Result = FallK[Idx, ptLEAF];
                }
                else if (sTagList[1] == "br1")
                {
                    iPart = GetPart(sTagList, 2);
                    Result = BreakdownK[1, iPart];
                }
                else if (sTagList[1] == "br2")
                {
                    Idx = iIndex(sTagList, 2);
                    Result = BreakdownK[Idx, ptLEAF];
                }
                else if (sTagList[1] == "q")
                {
                    iPart = GetPart(sTagList, 2);
                    Idx = iIndex(sTagList, 3);
                    Result = MatureK[Idx, iPart];
                }
                else if (sTagList[1] == "y")
                {
                    Idx = iIndex(sTagList, 2);
                    Result = DecayK[Idx];
                }
                else if (sTagList[1] == "s")
                {
                    Idx = iIndex(sTagList, 2);
                    if (Idx <= 4)
                        Result = SeedK[iIndex(sTagList, 2)];
                    else if ((Idx == 5) && (sTagList[3] == "soft"))
                        Result = SeedDeathK[SOFT];
                    else if ((Idx == 5) && (sTagList[3] == "hard"))
                        Result = SeedDeathK[HARD];
                }
                else if (sTagList[1] == "g")
                    Result = GermnK[iIndex(sTagList, 2)];
                else if (sTagList[1] == "z")
                    Result = SeedlK[iIndex(sTagList, 2)];

                else if (sTagList[1] == "conc")
                {
                    if (sTagList[2] == "n")
                        Elem = GrazType.TPlantElement.N;
                    else if (sTagList[2] == "p")
                        Elem = GrazType.TPlantElement.P;
                    else
                        Elem = GrazType.TPlantElement.S;
                    iPart = GetPart(sTagList, 3);
                    Idx = iIndex(sTagList, 4);
                    if (Idx <= 4)
                        Result = NutrConcK[Idx, (int)Elem, iPart];
                    else if (Idx == 5)
                        Result = NutrCO2K[(int)Elem, iPart];
                }

                else if (sTagList[1] == "fix")
                    Result = NFixK[iIndex(sTagList, 2)];
                else if (sTagList[1] == "eff")
                {
                    if (sTagList[2] == "no3")
                        Nutr = TPlantNutrient.pnNO3;
                    else if (sTagList[2] == "nh4")
                        Nutr = TPlantNutrient.pnNH4;
                    else if (sTagList[2] == "pox")
                        Nutr = TPlantNutrient.pnPOx;
                    else
                        Nutr = TPlantNutrient.pnSO4;
                    Result = NutrEffK[(int)Nutr];
                }
                else if (sTagList[1] == "reloc")
                {
                    if (sTagList[2] == "n")
                        Elem = GrazType.TPlantElement.N;
                    else if (sTagList[2] == "p")
                        Elem = GrazType.TPlantElement.P;
                    else
                        Elem = GrazType.TPlantElement.S;
                    Result = NutrRelocateK[(int)Elem];
                }

                else if (sTagList[1] == "aa")
                    Result = AshAlkK[iIndex(sTagList, 2)];

                else if (sTagList[1] == "cp")
                    Result = Protein[iIndex(sTagList, 2)];
                else if (sTagList[1] == "dg")
                    Result = DgProtein[iIndex(sTagList, 2)];
                else if (sTagList[1] == "dmdseed")
                {
                    if (sTagList[2] == "unripe")
                        Result = Seed_Dig[UNRIPE];
                    else if (sTagList[2] == "ripe")
                        Result = Seed_Dig[RIPE];
                }
                else if (sTagList[1] == "cpseed")
                    Result = Seed_Prot;
                else if (sTagList[1] == "hr")
                    Result = HeightRatio;
                else if (sTagList[1] == "sf")
                    Result = SelectFactor;
            }
            return Result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sTagList"></param>
        /// <returns></returns>
        override protected int getIntParam(string[] sTagList)
        {
            int Result = 0;
            if ((sTagList[0] == "k") && (sTagList[1] == "seed"))
            {
                if (sTagList[2] == "unripe")
                    Result = Seed_Class[UNRIPE];
                else if (sTagList[2] == "ripe")
                    Result = Seed_Class[RIPE];
            }
            return Result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sTagList"></param>
        /// <returns></returns>
        override protected bool getBoolParam(string[] sTagList)
        {
            bool Result = false;
            if (sTagList[0] == "grass")
                Result = bGrass;
            else if (sTagList[0] == "legume")
                Result = bLegume;
            else if (sTagList[0] == "isc4")
                Result = bisC4;
            else if (sTagList[0] == "annual")
                Result = bAnnual;
            else if (sTagList[0] == "longday")
                Result = bLongDay;
            else if (sTagList[0] == "shortday")
                Result = bShortDay;

            return Result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sTagList"></param>
        /// <returns></returns>
        override protected string getTextParam(string[] sTagList)
        {
            string Result = "";
            if (sTagList[0] == "editor")
                Result = sEditor;
            else if (sTagList[0] == "edited")
                Result = sEditDate;

            return Result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sTagList"></param>
        /// <param name="fValue"></param>
        override protected void setRealParam(string[] sTagList, double fValue)
        {
            TPlantElement Elem;
            TPlantNutrient Nutr;
            int iPart;
            int Idx;

            if (sTagList[0] == "k")
            {
                if (sTagList[1] == "v")
                    DevelopK[iIndex(sTagList, 2)] = fValue;
                else if (sTagList[1] == "i")
                    LightK[iIndex(sTagList, 2)] = fValue;
                else if (sTagList[1] == "wu")
                    WaterUseK[iIndex(sTagList, 2)] = fValue;
                else if (sTagList[1] == "ru")
                    RadnUseK[iIndex(sTagList, 2)] = fValue;
                else if (sTagList[1] == "bt")
                    TranspEffK[iIndex(sTagList, 2)] = fValue;
                else if (sTagList[1] == "t")
                    LowT_K[iIndex(sTagList, 2)] = fValue;
                else if (sTagList[1] == "w")
                    WaterK[iIndex(sTagList, 2)] = fValue;
                else if (sTagList[1] == "wl")
                    WaterLogK[iIndex(sTagList, 2)] = fValue;
                else if (sTagList[1] == "mr")
                    MeristemK[iIndex(sTagList, 2)] = fValue;
                else if (sTagList[1] == "tl")
                    TranslocK[iIndex(sTagList, 2)] = fValue;
                else if (sTagList[1] == "re")
                    RespireK[iIndex(sTagList, 2)] = fValue;
                else if (sTagList[1] == "a")
                    AllocK[iIndex(sTagList, 2)] = fValue;
                else if (sTagList[1] == "mo")
                    MorphK[iIndex(sTagList, 2)] = fValue;
                else if (sTagList[1] == "r")
                    RootK[iIndex(sTagList, 2)] = fValue;
                else if (sTagList[1] == "d")
                    DeathK[iIndex(sTagList, 2)] = fValue;
                else if (sTagList[1] == "dr")
                    RootLossK[iIndex(sTagList, 2)] = fValue;
                else if (sTagList[1] == "f1")
                {
                    iPart = GetPart(sTagList, 2);
                    FallK[1, iPart] = fValue;
                }
                else if (sTagList[1] == "f2")
                {
                    Idx = iIndex(sTagList, 2);
                    FallK[Idx, ptLEAF] = fValue;
                    FallK[Idx, ptSTEM] = fValue;
                }
                else if (sTagList[1] == "br1")
                {
                    iPart = GetPart(sTagList, 2);
                    BreakdownK[1, iPart] = fValue;
                }
                else if (sTagList[1] == "br2")
                {
                    Idx = iIndex(sTagList, 2);
                    BreakdownK[Idx, ptLEAF] = fValue;
                    BreakdownK[Idx, ptSTEM] = fValue;
                }
                else if (sTagList[1] == "q")
                {
                    iPart = GetPart(sTagList, 2);
                    Idx = iIndex(sTagList, 3);
                    MatureK[Idx, iPart] = fValue;
                }
                else if (sTagList[1] == "y")
                {
                    Idx = iIndex(sTagList, 2);
                    DecayK[Idx] = fValue;
                }
                else if (sTagList[1] == "s")
                {
                    Idx = iIndex(sTagList, 2); ;
                    if (Idx <= 4)
                        SeedK[iIndex(sTagList, 2)] = fValue;
                    else if ((Idx == 5) && (sTagList[3] == "soft"))
                        SeedDeathK[SOFT] = fValue;
                    else if ((Idx == 5) && (sTagList[3] == "hard"))
                        SeedDeathK[HARD] = fValue;
                }
                else if (sTagList[1] == "g")
                    GermnK[iIndex(sTagList, 2)] = fValue;
                else if (sTagList[1] == "z")
                    SeedlK[iIndex(sTagList, 2)] = fValue;

                else if (sTagList[1] == "conc")
                {
                    if (sTagList[2] == "n")
                        Elem = TPlantElement.N;
                    else if (sTagList[2] == "p")
                        Elem = TPlantElement.P;
                    else
                        Elem = TPlantElement.S;
                    iPart = GetPart(sTagList, 3);
                    Idx = iIndex(sTagList, 4);
                    if (Idx <= 4)
                        NutrConcK[Idx, (int)Elem, iPart] = fValue;
                    else if (Idx == 5)
                        NutrCO2K[(int)Elem, iPart] = fValue;
                }
                else if (sTagList[1] == "fix")
                    NFixK[iIndex(sTagList, 2)] = fValue;
                else if (sTagList[1] == "eff")
                {
                    if (sTagList[2] == "no3")
                        Nutr = TPlantNutrient.pnNO3;
                    else if (sTagList[2] == "nh4")
                        Nutr = TPlantNutrient.pnNH4;
                    else if (sTagList[2] == "pox")
                        Nutr = TPlantNutrient.pnPOx;
                    else
                        Nutr = TPlantNutrient.pnSO4;
                    NutrEffK[(int)Nutr] = fValue;
                }
                else if (sTagList[1] == "reloc")
                {
                    if (sTagList[2] == "n")
                        Elem = TPlantElement.N;
                    else if (sTagList[2] == "p")
                        Elem = TPlantElement.P;
                    else
                        Elem = TPlantElement.S;
                    NutrRelocateK[(int)Elem] = fValue;
                }
                else if (sTagList[1] == "aa")
                    AshAlkK[iIndex(sTagList, 2)] = fValue;

                else if (sTagList[1] == "cp")
                    Protein[iIndex(sTagList, 2)] = fValue;
                else if (sTagList[1] == "dg")
                    DgProtein[iIndex(sTagList, 2)] = fValue;
                else if (sTagList[1] == "dmdseed")
                {
                    if (sTagList[2] == "unripe")
                        Seed_Dig[UNRIPE] = fValue;
                    else if (sTagList[2] == "ripe")
                        Seed_Dig[RIPE] = fValue;
                }
                else if (sTagList[1] == "cpseed")
                    Seed_Prot = fValue;
                else if (sTagList[1] == "hr")
                    HeightRatio = fValue;
                else if (sTagList[1] == "sf")
                    SelectFactor = fValue;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sTagList"></param>
        /// <param name="iValue"></param>
        override protected void setIntParam(string[] sTagList, int iValue)
        {
            if ((sTagList[0] == "k") && (sTagList[1] == "seed"))
            {
                if (sTagList[2] == "unripe")
                    Seed_Class[UNRIPE] = iValue;
                else if (sTagList[2] == "ripe")
                    Seed_Class[RIPE] = iValue;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sTagList"></param>
        /// <param name="bValue"></param>
        override protected void setBoolParam(string[] sTagList, bool bValue)
        {
            if (sTagList[0] == "grass")
                bGrass = bValue;
            else if (sTagList[0] == "legume")
                bLegume = bValue;
            else if (sTagList[0] == "isc4")
                bisC4 = bValue;
            else if (sTagList[0] == "annual")
                bAnnual = bValue;
            else if (sTagList[0] == "longday")
                bLongDay = bValue;
            else if (sTagList[0] == "shortday")
                bShortDay = bValue;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sTagList"></param>
        /// <param name="sValue"></param>
        override protected void setTextParam(string[] sTagList, string sValue)
        {
            if (sTagList[0] == "editor")
                sEditor = sValue;
            else if (sTagList[0] == "edited")
                sEditDate = sValue;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public TPastureParamSet() : base(null, null)
        {
            Init();
        }

        public TPastureParamSet(TPastureParamSet paramSet) : base(paramSet)
        {
            Init();
        }

        /// <summary>
        /// Copy Constructor
        /// </summary>
        /// <param name="aParent"></param>
        /// <param name="srcSet"></param>
        public TPastureParamSet(TParameterSet aParent, TParameterSet srcSet) : base(aParent, srcSet)
        {
            Init();
        }

        public void Init()
        {
            FEquivFrom = new List<string>();
            FEquivTo = new List<string>();
        }

        protected const double EPS = 1.0E-6;
        protected const double INFINITY = 9.9E+9;
        protected double[] DMDLimits = { 0.85, 0.80, 0.75, 0.70, 0.65, 0.60, 0.55, 0.50, 0.45, 0.40, 0.35, 0.30, 0.25 };

        /// <summary>
        ///
        /// </summary>
        override public void deriveParams()
        {
            double fHighDMD;
            double fLowDMD;
            double fDeathDMD;
            double[] fClassDegDays = new double[HerbClassNo + 1]; // [0..12]
            int iStage;
            int iPart;
            int iDMD;

            if (bIsDefined("k-wu-3"))
                iWaterUptakeVersion = 2;
            else
                iWaterUptakeVersion = 1;
            bHasSeeds = (bAnnual || bIsDefined("k-s-1"));
            bVernReqd = bIsDefined("k-v-1");
            if (!bIsDefined("shortday"))
                bShortDay = false;
            if (bIsDefined("k-v-4"))
            {
                if (DevelopK[4] >= 0.0)
                    ReproTrigger = ReproTriggerEnum.trigLongDayLength;
                else
                    ReproTrigger = ReproTriggerEnum.trigShortDayLength;
            }
            else
                ReproTrigger = ReproTriggerEnum.trigDegDay;

            if (!bIsDefined("k-v-9"))
            {
                DevelopK[9] = 9.0E99;
                DevelopK[21] = 9.9E99;
            }
            else if (!bIsDefined("k-v-21"))
                DevelopK[21] = 9.9E99;

            bSummerDormant = bIsDefined("k-v-11");
            bWinterDormant = bIsDefined("k-v-26");
            bHasSetback = bIsDefined("k-v-22");

            if (!bIsDefined("k-i-9"))                                            // Default extinction coefficients for dry pasture
                LightK[9] = 0.50;
            if (!bIsDefined("k-i-10"))
                LightK[10] = 0.50;

            if (!bIsDefined("k-mo-1"))                                            // Default is uniform distribution of leaf over the vertical profile
                MorphK[1] = 1.0;

            if (!bIsDefined("k-br2-5"))
                BreakdownK[5, ptLEAF] = 100.0; // g/m^2
            if (!bIsDefined("k-y-9"))
                DecayK[9] = 200.0; // g/m^2

            MatureK[4, ptSTEM] = 0.0;

            for (iStage = 1; iStage <= 2; iStage++)
            {
                for (iPart = ptLEAF; iPart <= ptSTEM; iPart++)
                {
                    fHighDMD = MatureK[1, iPart];
                    if (iStage == 1)
                        fLowDMD = MatureK[2, iPart];
                    else
                        fLowDMD = MatureK[3, iPart];
                    fDeathDMD = fLowDMD + (fHighDMD - fLowDMD) * Math.Exp(-MatureK[5, iPart] * DeathK[1]);

                    for (iDMD = 1; iDMD <= HerbClassNo; iDMD++)
                        BaseDeathRates[iStage, iPart, iDMD] = DeathK[2] * Math.Max(0.0, Math.Min((fDeathDMD - DMDLimits[iDMD]) / (DMDLimits[iDMD - 1] - DMDLimits[iDMD]), 1.0));

                    fClassDegDays[0] = 0.0;
                    for (iDMD = 1; iDMD <= HerbClassNo; iDMD++)
                    {
                        if (DMDLimits[iDMD] >= fHighDMD)
                            fClassDegDays[iDMD] = 0.0;
                        else if (DMDLimits[iDMD] < fLowDMD + EPS)
                            fClassDegDays[iDMD] = INFINITY;
                        else
                            fClassDegDays[iDMD] = MatureK[4, iPart] - Math.Log((DMDLimits[iDMD] - fLowDMD) / (fHighDMD - fLowDMD)) / MatureK[5, iPart];
                    }
                    for (iDMD = 1; iDMD <= HerbClassNo; iDMD++)
                    {
                        if ((fClassDegDays[iDMD] == 0.0) || (fClassDegDays[iDMD - 1] >= MatureK[4, ptLEAF]))
                            NewTissuePropns[iStage, iPart, iDMD] = 0.0;
                        else if (fClassDegDays[iDMD] <= MatureK[4, ptLEAF])
                            NewTissuePropns[iStage, iPart, iDMD] = 1.0;
                        else
                            NewTissuePropns[iStage, iPart, iDMD] = (MatureK[4, ptLEAF] - fClassDegDays[iDMD - 1])
                                                                  / (fClassDegDays[iDMD] - fClassDegDays[iDMD - 1]);
                    }
                    for (iDMD = HerbClassNo; iDMD >= 1; iDMD--)
                    {
                        if (fClassDegDays[iDMD] < INFINITY)
                            fClassDegDays[iDMD] = fClassDegDays[iDMD] - fClassDegDays[iDMD - 1];
                    }

                    for (iDMD = 1; iDMD <= HerbClassNo; iDMD++)
                    {
                        if (fClassDegDays[iDMD] == 0.0)
                            DMDRates[iStage, iPart, iDMD] = 1.0;
                        else if (fClassDegDays[iDMD] < INFINITY)
                            DMDRates[iStage, iPart, iDMD] = (DMDLimits[iDMD - 1] - DMDLimits[iDMD]) / fClassDegDays[iDMD];
                        else
                            DMDRates[iStage, iPart, iDMD] = 0.0;
                    }
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="Idx"></param>
        /// <returns></returns>
        public TPastureParamSet GetParamSet(int Idx)
        {
            return (TPastureParamSet)getNode(Idx);
        }

        /// <summary>
        ///  Returns a pointer to a pasture parameter set for the species with a given
        /// name.If no direct match is found, tries to locate an equivalence between
        /// names(previously defined with makeNameEquivalent()) and use that.Returns
        ///
        /// NIL if no match is found.
        /// Parameter:
        ///    sMatchName
        /// </summary>
        /// <param name="sMatchName">Name of the species for which a parameter set is to be found.</param>
        /// <returns></returns>
        public TPastureParamSet Match(string sMatchName)
        {
            int Jdx;
            TPastureParamSet Result = (TPastureParamSet)getNode(sMatchName);

            if (Result == null)                                                        // No direct match - look for equivalence
            {
                Jdx = FEquivFrom.IndexOf(sMatchName.ToLower());
                // The equivalence list is in lower case    }
                if (Jdx >= 0)
                    Result = Match(FEquivTo[Jdx]);
            }

            return Result;
        }

        /// <summary>
        /// Lets the class know that references in the Match() method to one name
        /// (not in the parameter set) should be treated as references to another name.
        /// </summary>
        /// <param name="sUnknownName">Name to be equivalenced</param>
        /// <param name="sKnownName">Name to which the equivalence refers</param>
        public void MakeNameEquivalent(string sUnknownName, string sKnownName)
        {
            int Jdx;

            if (Match(sKnownName) == null)
                throw new Exception($"Species {sKnownName} is not available");


            sUnknownName = sUnknownName.ToLower();                                      // Force to lowercase for comparisons

            Jdx = FEquivFrom.IndexOf(sUnknownName);                                     // Place in the list of equivalences, overriding any existing entry
            if (Jdx == -1)
            {
                FEquivFrom.Add(sUnknownName);
                FEquivTo.Add(sKnownName);
            }
            else
                FEquivTo[Jdx] = sKnownName;
        }

        /// <summary>
        /// Undoes the effect of makeNameEquivalent()
        /// </summary>
        /// <param name="sUnknownName">Name for which an equivalence is to be deleted</param>
        public void RemoveNameEquivalent(string sUnknownName)
        {
            int Jdx;
            {
                Jdx = FEquivFrom.IndexOf(sUnknownName.ToLower());
                if (Jdx >= 0)
                {
                    FEquivFrom.RemoveAt(Jdx);
                    FEquivTo.RemoveAt(Jdx);
                }
            }
        }
    }
}
#pragma warning restore CS1591
