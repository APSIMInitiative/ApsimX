using System;
using System.Collections.Generic;

namespace Models.Stock
{
    /// <summary>
    /// The stock genotype
    /// </summary>
    public class TStockGeno
    {
        /// <summary>
        /// 
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string dam_breed { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string sire_breed { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int generation { get; set; }
        /// <summary>
        /// kg
        /// </summary>
        public double srw { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public double[] conception;
        /// <summary>
        /// /y
        /// </summary>
        public double death_rate { get; set; }
        /// <summary>
        /// kg
        /// </summary>
        public double ref_fleece_wt { get; set; }
        /// <summary>
        /// um
        /// </summary>
        public double max_fibre_diam { get; set; }
        /// <summary>
        /// kg/kg
        /// </summary>
        public double fleece_yield { get; set; }
        /// <summary>
        /// kg
        /// </summary>
        public double peak_milk { get; set; }
        /// <summary>
        /// /y
        /// </summary>
        public double wnr_death_rate { get; set; }
    }

    /// <summary>
    /// Parent class for the sheep and cattle init classes
    /// </summary>
    public class TAnimalInit
    {
        /// <summary>
        /// 
        /// </summary>
        public string genotype { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int number { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string sex { get; set; }
        /// <summary>
        /// d
        /// </summary>
        public double age { get; set; }
        /// <summary>
        /// kg
        /// </summary>
        public double weight { get; set; }
        /// <summary>
        /// kg
        /// </summary>
        public double max_prev_wt { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string mated_to { get; set; }
        /// <summary>
        /// d
        /// </summary>
        public int pregnant { get; set; }
        /// <summary>
        /// d
        /// </summary>
        public int lactating { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public double birth_cs { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string paddock { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int tag { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int priority { get; set; }
    }

    /// <summary>
    /// The sheep init type
    /// </summary>
    public class TSheepInit : TAnimalInit
    {
        /// <summary>
        /// kg
        /// </summary>
        public double fleece_wt { get; set; }
        /// <summary>
        /// um
        /// </summary>
        public double fibre_diam { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int no_young { get; set; }
        /// <summary>
        /// kg
        /// </summary>
        public double lamb_wt { get; set; }
        /// <summary>
        /// kg
        /// </summary>
        public double lamb_fleece_wt { get; set; }
    }

    /// <summary>
    /// The cattle init type
    /// </summary>
    public class TCattleInit : TAnimalInit
    {
        /// <summary>
        /// 
        /// </summary>
        public int no_foetuses { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int no_suckling { get; set; }
        /// <summary>
        /// kg
        /// </summary>
        public double calf_wt { get; set; }

    }

    /// <summary>
    /// The paddock init type
    /// </summary>
    public class TPaddInit
    {
        /// <summary>
        /// 
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// ha
        /// </summary>
        public double area { get; set; }
        /// <summary>
        /// deg
        /// </summary>
        public double slope { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string[] forages;
        /// <summary>
        /// dest for excret or faeces
        /// </summary>
        public string excretion { get; set; }
        /// <summary>
        /// dest for urine
        /// </summary>
        public string urine { get; set; }
    }

    /// <summary>
    /// Supplement eaten type
    /// </summary>
    public class TSupplementEaten
    {
        /// <summary>
        /// Paddock name
        /// </summary>
        public string paddock { get; set; }
        /// <summary>
        /// kg
        /// </summary>
        public double eaten { get; set; }
    }

    /// <summary>
    /// Dry matter pool
    /// </summary>
    public class TDMPoolHead
    {
        /// <summary>
        /// kg/d
        /// </summary>
        public double weight { get; set; }
        /// <summary>
        /// kg/d
        /// </summary>
        public double n { get; set; }
        /// <summary>
        /// kg/d
        /// </summary>
        public double p { get; set; }
        /// <summary>
        /// mol/d
        /// </summary>
        public double s { get; set; }
        /// <summary>
        /// mol/d
        /// </summary>
        public double ash_alk { get; set; }
    }

    /// <summary>
    /// Inorganic faeces type
    /// </summary>
    public class TInorgFaeces
    {
        /// <summary>
        /// kg/d
        /// </summary>
        public double n { get; set; }
        /// <summary>
        /// kg/d
        /// </summary>
        public double p { get; set; }
        /// <summary>
        /// mol/d
        /// </summary>
        public double s { get; set; }
    }

    /// <summary>
    /// Definitions of many property constants in the Stock component
    /// </summary>
    public static class StockProps
    {
#pragma warning disable 1591 //missing xml comment
        // Reporting variables -------------------------------------------------------}
        public const int prpAGE = 1;
        public const int prpAGE_MONTHS = 2;
        public const int prpLIVE_WT = 3;
        public const int prpBASE_WT = 4;
        public const int prpCOND_SCORE = 5;
        public const int prpMAX_PREV_WT = 6;
        public const int prpFLEECE_WT = 7;
        public const int prpCFLEECE_WT = 8;
        public const int prpFIBRE_DIAM = 9;
        public const int prpPREGNANT = 10;
        public const int prpLACTATING = 11;
        public const int prpNO_FOETUSES = 12;
        public const int prpNO_SUCKLING = 13;
        public const int prpBIRTH_CS = 14;

        public const int prpPADDOCK = 15;
        public const int prpTAG = 16;
        public const int prpPRIORITY = 17;

        public const int prpDSE = 18;
        public const int prpWT_CHANGE = 19;
        public const int prpINTAKE = 20;
        public const int prpINTAKE_PAST = 21;
        public const int prpINTAKE_SUPP = 22;
        public const int prpME_INTAKE = 23;
        public const int prpCPI_INTAKE = 24;
        public const int prpCFLEECE_GROWTH = 25;
        public const int prpDAY_FIBRE_DIAM = 26;
        public const int prpMILK_WT = 27;
        public const int prpMILK_ME = 28;
        public const int prpRETAINED_N = 29;
        public const int prpRETAINED_P = 30;
        public const int prpRETAINED_S = 31;
        public const int prpFAECES = 32;
        public const int prpURINE_N = 33;
        public const int prpURINE_P = 34;
        public const int prpURINE_S = 35;
        public const int prpINORG_FAECES = 36;
        public const int prpCH4_OUTPUT = 37;
        public const int prpRDPI = 38;
        public const int prpRDPR = 39;
        public const int prpRDP_EFFECT = 40;
        public const int prpPADD_RANK = 41;
        public const int prpINTAKE_MOD = 42;
#pragma warning restore 1591 //missing xml comment
        /*   
            // Initialisation variables --------------------------------------------------}
            
            /// <summary>
            /// Organic matter
            /// from pitypes.pas 
            /// </summary>
            public const string typeORG_MATTER = "<field name=\"weight\"  unit=\"kg/ha\"  kind=\"double\"/>"
                        + "<field name=\"n\"       unit=\"kg/ha\"  kind=\"double\"/>"
                        + "<field name=\"p\"       unit=\"kg/ha\"  kind=\"double\"/>"
                        + "<field name=\"s\"       unit=\"kg/ha\"  kind=\"double\"/>"
                        + "<field name=\"ash_alk\" unit=\"mol/ha\" kind=\"double\"/>";

            /// <summary>
            /// Excretion to soil
            /// </summary>
            public const string typeEXCRETA2SOIL = "<type>"
                       + "<field name=\"faeces_om\">"
                       + typeORG_MATTER
                       + "</field>"
                       + "<field name=\"faeces_inorg\">"
                       + "<field name=\"n\" unit=\"kg/ha\" kind=\"double\"/>"
                       + "<field name=\"p\" unit=\"kg/ha\" kind=\"double\"/>"
                       + "<field name=\"s\" unit=\"kg/ha\" kind=\"double\"/>"
                       + "</field>"
                       + "<field name=\"urine\">"
                       + "<field name=\"volume\"  unit=\"m^3/ha\" kind=\"double\"/>"
                       + "<field name=\"urea\"    unit=\"kg/ha\"  kind=\"double\"/>"
                       + "<field name=\"pox\"     unit=\"kg/ha\"  kind=\"double\"/>"
                       + "<field name=\"so4\"     unit=\"kg/ha\"  kind=\"double\"/>"
                       + "<field name=\"ash_alk\" unit=\"mol/ha\" kind=\"double\"/>"
                       + "</field>"
                       + "<field name=\"urine_area\" unit=\"m^2/m^2\" kind=\"double\"/>"
                     + "</type>";
            /// <summary>
            /// Forage cohort available
            /// </summary>
            public const string typeCOHORTAVAIL = "<type name=\"AvailableToAnimal\" array=\"T\">"
                     + "<element>"
                     + "<field name=\"CohortID\"       kind=\"string\"/>"    //e.g. seedling, established, senescing, dead, litter
                     + "<field name=\"Organ\"          kind=\"string\"/>"    //e.g. leaf, stem, head
                     + "<field name=\"AgeID\"          kind=\"string\"/>"    //e.g. DMD80-85, DMD75-80...
                     + "<field name=\"Bottom\"         kind=\"double\" unit=\"mm\"/>"
                     + "<field name=\"Top\"            kind=\"double\" unit=\"mm\"/>"
                     + "<field name=\"Chem\"           kind=\"string\"/>"    //e.g. \"DDM\"/\"IDM\"
                     + "<field name=\"Weight\"         kind=\"double\" unit=\"kg/ha\"/>"
                     + "<field name=\"N\"              kind=\"double\" unit=\"kg/ha\"/>"
                     + "<field name=\"P\"              kind=\"double\" unit=\"kg/ha\"/>"
                     + "<field name=\"S\"              kind=\"double\" unit=\"kg/ha\"/>"
                     + "<field name=\"AshAlk\"         kind=\"double\" unit=\"mol/ha\"/>"
                     + "</element>"
                     + "</type>";
            /// <summary>
            /// Cohort removed by the animals
            /// </summary>
            public const string typeCOHORTREM = "<type name=\"RemovedByAnimal\" array=\"T\">"
                    + "<element>"
                    + "<field name=\"CohortID\"       kind=\"string\"/>"    //e.g. seedling, established, senescing, dead, litter
                    + "<field name=\"Organ\"          kind=\"string\"/>"    //e.g. leaf, stem, head
                    + "<field name=\"AgeID\"          kind=\"string\"/>"    //e.g. DMD80-85, DMD75-80...
                    + "<field name=\"Bottom\"         kind=\"double\" unit=\"mm\"/>"
                    + "<field name=\"Top\"            kind=\"double\" unit=\"mm\"/>"
                    + "<field name=\"Chem\"           kind=\"string\"/>"    //e.g. \"DDM\"/\"IDM\"
                    + "<field name=\"WeightRemoved\"  kind=\"double\" unit=\"kg/ha\"/>"
                    + "</element>"
                    + "</type>";
            /// <summary>
            /// Pasture removal
            /// </summary>
            public const string typeREMOVAL = "<type>"
                     + "<field name=\"herbage\" unit=\"kg/ha\" kind=\"double\" array=\"T\"/>"
                     + "<field name=\"seed\"    unit=\"kg/ha\" kind=\"double\" array=\"T\"/>"
                     + "</type>";
            /// <summary>
            /// Diet specifics
            /// </summary>
            private const string typeDIET = "<element>"
                             + "<field name=\"dm\"           unit=\"kg/ha\"  kind=\"double\"/>"
                             + "<field name=\"dmd\"          unit=\"-\"      kind=\"double\"/>"
                             + "<field name=\"cp_conc\"      unit=\"kg/kg\"  kind=\"double\"/>"
                             + "<field name=\"p_conc\"       unit=\"kg/kg\"  kind=\"double\"/>"
                             + "<field name=\"s_conc\"       unit=\"kg/kg\"  kind=\"double\"/>"
                             + "<field name=\"prot_dg\"      unit=\"kg/kg\"  kind=\"double\"/>"
                             + "<field name=\"ash_alk\"      unit=\"mol/kg\" kind=\"double\"/>"
                             + "<field name=\"height_ratio\" unit=\"-\"      kind=\"double\"/>"
                             + "</element>";
            /// <summary>
            /// Pasture to stock
            /// </summary>
            public const string typePLANT2STOCK = "<type>"
                         + "<field name = \"herbage\" array=\"T\">"
                         + typeDIET
                         + "</field>"
                         + "<field name=\"propn_green\"   unit=\"-\" kind=\"double\"/>"
                         + "<field name=\"legume\"        unit=\"-\" kind=\"double\"/>"
                         + "<field name=\"select_factor\" unit=\"-\" kind=\"double\"/>"
                         + "<field name=\"seed\"          array=\"T\">"
                         + typeDIET
                         + "</field>"
                         + "<field name=\"seed_class\"    unit=\"-\" kind=\"integer4\" array=\"T\"/>"
                       + "</type>";
        */
    }

    /// <summary>
    /// Container for Stock property access
    /// </summary>
    public static class StockVars
    {
        /// <summary>
        /// Convert to days
        /// </summary>
        public const double MONTH2DAY = 365.25 / 12;

        
        /// <summary>
        /// Copies the parameters into an array of genotype structures
        /// </summary>
        /// <param name="Model"></param>
        /// <param name="aValue"></param>
        public static void MakeGenotypesValue(TStockList Model, ref TStockGeno[] aValue)
        {
            TAnimalParamSet Params;
            string sDamBreed = "";
            string sSireBreed = "";
            int iGeneration = 0;
            int Idx, Jdx;

            Array.Resize(ref aValue, Model.iGenotypeCount());
            for (Idx = 0; Idx <= Model.iGenotypeCount() - 1; Idx++)
            {
                Params = Model.getGenotype(Idx);

                if (Params.iParentageCount() == 1)
                {
                    sDamBreed = Params.sParentageBreed(0);
                    sSireBreed = sDamBreed;
                    iGeneration = 0;
                }
                else if ((Params.iParentageCount() == 2) && (Params.fParentagePropn(0) > 0))
                {
                    sDamBreed = Params.sParentageBreed(0);
                    sSireBreed = Params.sParentageBreed(1);
                    iGeneration = Convert.ToInt32(Math.Max(0, Math.Round(Math.Log(Params.fParentagePropn(0)) / Math.Log(0.5))));    //TODO: may need checking
                }
                else if (Params.iParentageCount() == 2)
                {
                    sSireBreed = Params.sParentageBreed(1);
                    sDamBreed = sSireBreed;
                    iGeneration = 0;
                }
                else
                {
                    sDamBreed = Params.sParentageBreed(0);
                    sSireBreed = Params.sParentageBreed(1);
                    iGeneration = 0;
                }

                aValue[Idx].name = Params.sName;
                aValue[Idx].dam_breed = sDamBreed;
                aValue[Idx].sire_breed = sSireBreed;
                aValue[Idx].generation = iGeneration;

                aValue[Idx].srw = Params.BreedSRW;
                aValue[Idx].death_rate = Params.AnnualDeaths(false);
                aValue[Idx].wnr_death_rate = Params.AnnualDeaths(true);
                aValue[Idx].ref_fleece_wt = Params.PotentialGFW;
                aValue[Idx].max_fibre_diam = Params.MaxMicrons;
                aValue[Idx].fleece_yield = Params.FleeceYield;
                aValue[Idx].peak_milk = Params.PotMilkYield;

                if (Params.Animal == GrazType.AnimalType.Sheep)
                    Array.Resize(ref aValue[Idx].conception, 3);
                else
                    Array.Resize(ref aValue[Idx].conception, 2);
                for (Jdx = 0; Jdx < aValue[Idx].conception.Length; Jdx++)
                    aValue[Idx].conception[Jdx] = Params.Conceptions[Jdx];
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Model"></param>
        /// <param name="Animal"></param>
        /// <param name="aValue"></param>
        public static void MakeSheepValue(TStockList Model, GrazType.AnimalType Animal, ref TSheepInit[] aValue)
        {
            TAnimalGroup aGroup;
            int iCount;
            int Idx, Jdx;

            iCount = 0;
            for (Idx = 1; Idx <= Model.Count(); Idx++)
                if (Model.At(Idx).Genotype.Animal == Animal)
                    iCount++;
            Array.Resize(ref aValue, iCount);

            Jdx = 0;
            for (Idx = 1; Idx <= Model.Count(); Idx++)
            {
                if (Model.At(Idx).Genotype.Animal == Animal)
                {
                    aGroup = Model.At(Idx);

                    aValue[Jdx] = new TSheepInit();

                    aValue[Jdx].genotype = aGroup.Genotype.sName;
                    aValue[Jdx].number = aGroup.NoAnimals;
                    aValue[Jdx].sex = Model.SexString(Idx, false);
                    aValue[Jdx].age = aGroup.AgeDays;
                    aValue[Jdx].weight = aGroup.LiveWeight;
                    aValue[Jdx].max_prev_wt = aGroup.MaxPrevWeight;
                    aValue[Jdx].pregnant = aGroup.Pregnancy;
                    aValue[Jdx].lactating = aGroup.Lactation;

                    if (Animal == GrazType.AnimalType.Sheep)
                    {
                        aValue[Jdx].fleece_wt = aGroup.FleeceCutWeight;
                        aValue[Jdx].fibre_diam = aGroup.FibreDiam;
                        aValue[Jdx].no_young = Math.Max(aGroup.NoFoetuses, aGroup.NoOffspring);
                    }
                    /*else if (Animal == GrazType.AnimalType.Cattle)
                    {
                        aValue[Jdx].no_foetuses = aGroup.NoFoetuses;
                        aValue[Jdx].no_suckling = aGroup.NoOffspring;
                    }*/

                    if (aGroup.Lactation > 0)
                        aValue[Jdx].birth_cs = aGroup.BirthCondition;

                    if ((aGroup.Pregnancy > 0) || (aGroup.Young != null))
                    {
                        if (aGroup.MatedTo != null)
                            aValue[Jdx].mated_to = aGroup.MatedTo.sName;
                        else
                            aValue[Jdx].mated_to = "";
                    }
                    else
                        aValue[Jdx].mated_to = "";

                    if (aGroup.Young != null)
                    {
                        if (Animal == GrazType.AnimalType.Sheep)
                        {
                            aValue[Jdx].lamb_wt = aGroup.Young.LiveWeight;
                            aValue[Jdx].lamb_fleece_wt = aGroup.Young.FleeceCutWeight;
                        }
                        /*else if (Animal == GrazType.AnimalType.Cattle)
                            aValue[Jdx].calf_wt = aGroup.Young.LiveWeight;*/

                        aValue[Jdx].paddock = Model.getInPadd(Idx);
                        aValue[Jdx].tag = Model.getTag(Idx);
                        aValue[Jdx].priority = Model.getPriority(Idx);
                    }
                }
                Jdx++;
            } // next animal
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Model"></param>
        /// <param name="Animal"></param>
        /// <param name="aValue"></param>
        public static void MakeCattleValue(TStockList Model, GrazType.AnimalType Animal, ref TCattleInit[] aValue)
        {
            TAnimalGroup aGroup;
            int iCount;
            int Idx, Jdx;

            iCount = 0;
            for (Idx = 1; Idx <= Model.Count(); Idx++)
                if (Model.At(Idx).Genotype.Animal == Animal)
                    iCount++;
            Array.Resize(ref aValue, iCount);

            Jdx = 0;
            for (Idx = 1; Idx <= Model.Count(); Idx++)
            {
                if (Model.At(Idx).Genotype.Animal == Animal)
                {
                    aGroup = Model.At(Idx);

                    aValue[Jdx] = new TCattleInit();

                    aValue[Jdx].genotype = aGroup.Genotype.sName;
                    aValue[Jdx].number = aGroup.NoAnimals;
                    aValue[Jdx].sex = Model.SexString(Idx, false);
                    aValue[Jdx].age = aGroup.AgeDays;
                    aValue[Jdx].weight = aGroup.LiveWeight;
                    aValue[Jdx].max_prev_wt = aGroup.MaxPrevWeight;
                    aValue[Jdx].pregnant = aGroup.Pregnancy;
                    aValue[Jdx].lactating = aGroup.Lactation;

                    /*if (Animal == GrazType.AnimalType.Sheep)
                    {
                        aValue[Jdx].fleece_wt = aGroup.FleeceCutWeight;
                        aValue[Jdx].fibre_diam = aGroup.FibreDiam;
                        aValue[Jdx].no_young = Math.Max(aGroup.NoFoetuses, aGroup.NoOffspring);
                    }
                    else*/
                    if (Animal == GrazType.AnimalType.Cattle)
                    {
                        aValue[Jdx].no_foetuses = aGroup.NoFoetuses;
                        aValue[Jdx].no_suckling = aGroup.NoOffspring;
                    }

                    if (aGroup.Lactation > 0)
                        aValue[Jdx].birth_cs = aGroup.BirthCondition;

                    if ((aGroup.Pregnancy > 0) || (aGroup.Young != null))
                    {
                        if (aGroup.MatedTo != null)
                            aValue[Jdx].mated_to = aGroup.MatedTo.sName;
                        else
                            aValue[Jdx].mated_to = "";
                    }
                    else
                        aValue[Jdx].mated_to = "";

                    if (aGroup.Young != null)
                    {
                        /*if (Animal == GrazType.AnimalType.Sheep)
                        {
                            aValue[Jdx].lamb_wt = aGroup.Young.LiveWeight;
                            aValue[Jdx].lamb_fleece_wt = aGroup.Young.FleeceCutWeight;
                        }
                        else*/
                        if (Animal == GrazType.AnimalType.Cattle)
                            aValue[Jdx].calf_wt = aGroup.Young.LiveWeight;

                        aValue[Jdx].paddock = Model.getInPadd(Idx);
                        aValue[Jdx].tag = Model.getTag(Idx);
                        aValue[Jdx].priority = Model.getPriority(Idx);
                    }
                }
                Jdx++;
            } // next animal
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Model"></param>
        /// <param name="aValue"></param>
        public static void MakePaddockList(TStockList Model, ref TPaddInit[] aValue)
        {
            TPaddockInfo aPadd;
            int Idx, Jdx;

            Array.Resize(ref aValue, Model.Paddocks.Count());

            for (Idx = 0; Idx < aValue.Length; Idx++)
            {
                aPadd = Model.Paddocks.byIndex(Idx);
                aValue[Idx].name = aPadd.sName;                                       // "name"                                
                aValue[Idx].area = aPadd.fArea;                                       // "area"                                
                aValue[Idx].slope = aPadd.Slope;                                       // "slope"                               
                Array.Resize(ref aValue[Idx].forages, aPadd.Forages.Count());
                for (Jdx = 0; Jdx < aPadd.Forages.Count(); Jdx++)
                    aValue[Idx].forages[Jdx] = aPadd.Forages.byIndex(Jdx).sName;
                aValue[Idx].excretion = aPadd.sExcretionDest;                              // "excretion"                           
            }
        }

        /// <summary>
        /// Rank the paddocks
        /// </summary>
        /// <param name="Model"></param>
        /// <param name="aValue"></param>
        public static void MakePaddockRank(TStockList Model, ref string[] aValue)
        {
            List<string> sList = new List<string>();

            Model.rankPaddocks(sList);
            aValue = sList.ToArray();
            /*Array.Resize(ref aValue, sList.Count);
            for (int Idx = 0; Idx < aValue.Length; Idx++)
                aValue[Idx] = sList[Idx];*/
        }

        /// <summary>
        /// The output counts for these type of animals
        /// </summary>
        public enum CountType
        {
            /// <summary>
            /// Both males and females
            /// </summary>
            eBoth,
            /// <summary>
            /// Female animals
            /// </summary>
            eFemale,
            /// <summary>
            /// Male animals
            /// </summary>
            eMale
        };

        /// <summary>
        /// Populate the numbers array for the type of output required
        /// </summary>
        /// <param name="Model"></param>
        /// <param name="code"></param>
        /// <param name="bUseYoung"></param>
        /// <param name="bUseAll"></param>
        /// <param name="bUseTag"></param>
        /// <param name="numbers"></param>
        /// <returns></returns>
        public static bool PopulateNumberValue(TStockList Model, CountType code, bool bUseYoung, bool bUseAll, bool bUseTag, ref int[] numbers)
        {
            int iNoPasses;
            TAnimalGroup aGroup;
            int iValue;
            int iTotal;
            int iPass, Idx;

            bool Result = true;
            Array.Clear(numbers, 0, numbers.Length);

            if (bUseTag)
                iNoPasses = numbers.Length;
            else
                iNoPasses = 1;

            for (iPass = 1; iPass <= iNoPasses; iPass++)
            {
                iTotal = 0;

                for (Idx = 1; Idx <= Model.Count(); Idx++)
                    if (!bUseTag || (Model.getTag(Idx) == iPass))
                    {
                        if (!bUseYoung)
                            aGroup = Model.At(Idx);
                        else
                            aGroup = Model.At(Idx).Young;

                        iValue = 0;
                        if (aGroup != null)
                        {
                            switch (code)
                            {
                                case CountType.eBoth:
                                    iValue = aGroup.NoAnimals;
                                    break;
                                case CountType.eFemale:
                                    iValue = aGroup.FemaleNo;
                                    break;
                                case CountType.eMale:
                                    iValue = aGroup.MaleNo;
                                    break;
                                default:
                                    Result = false;
                                    break;
                            }
                        }
                        if (!bUseTag && !bUseAll)
                            numbers[Idx - 1] = iValue;
                        else
                            iTotal = iTotal + iValue;
                    } // _ loop over animal groups 

                if (bUseAll)
                    numbers[0] = iTotal;
                else if (bUseTag)
                    numbers[iPass - 1] = iTotal;
            } //_ loop over passes _
            return Result;
        }

        /// <summary>
        /// Fill the double[] with values from the model
        /// </summary>
        /// <param name="Model"></param>
        /// <param name="varCode"></param>
        /// <param name="bUseYoung"></param>
        /// <param name="bUseAll"></param>
        /// <param name="bUseTag"></param>
        /// <param name="aValue"></param>
        /// <returns></returns>
        public static bool PopulateRealValue(TStockList Model, int varCode, bool bUseYoung, bool bUseAll, bool bUseTag, ref double[] aValue)
        {
            int iNoPasses;
            TAnimalGroup aGroup;
            double dValue;
            double dTotal;
            int iDenom;
            int iPass, Idx;

            bool Result = true;
            Array.Clear(aValue, 0, aValue.Length);

            if (bUseTag)
                iNoPasses = aValue.Length;
            else
                iNoPasses = 1;

            for (iPass = 1; iPass <= iNoPasses; iPass++)
            {
                dTotal = 0.0;
                iDenom = 0;

                for (Idx = 1; Idx <= Model.Count(); Idx++)
                {
                    if (!bUseTag || (Model.getTag(Idx) == iPass))
                    {
                        if (!bUseYoung)
                            aGroup = Model.At(Idx);
                        else
                            aGroup = Model.At(Idx).Young;

                        dValue = 0.0;
                        if (aGroup != null)
                        {
                            int N = (int)GrazType.TOMElement.N;
                            int P = (int)GrazType.TOMElement.P;
                            int S = (int)GrazType.TOMElement.S;
                            switch (varCode)
                            {
                                case StockProps.prpAGE: dValue = aGroup.AgeDays; break;
                                case StockProps.prpAGE_MONTHS: dValue = aGroup.AgeDays / MONTH2DAY; break;
                                case StockProps.prpLIVE_WT: dValue = aGroup.LiveWeight; break;
                                case StockProps.prpBASE_WT: dValue = aGroup.BaseWeight; break;
                                case StockProps.prpCOND_SCORE: dValue = aGroup.fConditionScore(TAnimalParamSet.TCond_System.csSYSTEM1_5); break;
                                case StockProps.prpMAX_PREV_WT: dValue = aGroup.MaxPrevWeight; break;
                                case StockProps.prpFLEECE_WT: dValue = aGroup.FleeceCutWeight; break;
                                case StockProps.prpCFLEECE_WT: dValue = aGroup.CleanFleeceWeight; break;
                                case StockProps.prpFIBRE_DIAM: dValue = aGroup.FibreDiam; break;
                                case StockProps.prpPREGNANT: dValue = aGroup.Pregnancy; break;
                                case StockProps.prpLACTATING: dValue = aGroup.Lactation; break;
                                case StockProps.prpNO_FOETUSES: dValue = aGroup.NoFoetuses; break;
                                case StockProps.prpNO_SUCKLING: dValue = aGroup.NoOffspring; break;
                                case StockProps.prpBIRTH_CS: dValue = aGroup.BirthCondition; break;
                                case StockProps.prpDSE: dValue = aGroup.DrySheepEquivs; break;
                                case StockProps.prpWT_CHANGE: dValue = aGroup.WeightChange; break;
                                case StockProps.prpME_INTAKE: dValue = aGroup.AnimalState.ME_Intake.Total; break;
                                case StockProps.prpCPI_INTAKE: dValue = aGroup.AnimalState.CP_Intake.Total; break;
                                case StockProps.prpCFLEECE_GROWTH: dValue = aGroup.CleanFleeceGrowth; break;
                                case StockProps.prpDAY_FIBRE_DIAM: dValue = aGroup.DayFibreDiam; break;
                                case StockProps.prpMILK_WT: dValue = aGroup.MilkYield; break;
                                case StockProps.prpMILK_ME: dValue = aGroup.MilkEnergy; break;
                                case StockProps.prpRETAINED_N:
                                    dValue = aGroup.AnimalState.CP_Intake.Total / GrazType.N2Protein - (aGroup.AnimalState.InOrgFaeces.Nu[N] + aGroup.AnimalState.OrgFaeces.Nu[N] + aGroup.AnimalState.Urine.Nu[N]);
                                    break;
                                case StockProps.prpRETAINED_P:
                                    dValue = aGroup.AnimalState.Phos_Intake.Total - (aGroup.AnimalState.InOrgFaeces.Nu[P] + aGroup.AnimalState.OrgFaeces.Nu[P] + aGroup.AnimalState.Urine.Nu[P]);
                                    break;
                                case StockProps.prpRETAINED_S:
                                    dValue = aGroup.AnimalState.Sulf_Intake.Total - (aGroup.AnimalState.InOrgFaeces.Nu[S] + aGroup.AnimalState.OrgFaeces.Nu[S] + aGroup.AnimalState.Urine.Nu[S]);
                                    break;
                                case StockProps.prpURINE_N: dValue = aGroup.AnimalState.Urine.Nu[N]; break;
                                case StockProps.prpURINE_P: dValue = aGroup.AnimalState.Urine.Nu[P]; break;
                                case StockProps.prpURINE_S: dValue = aGroup.AnimalState.Urine.Nu[S]; break;
                                case StockProps.prpCH4_OUTPUT:
                                    dValue = 0.001 * aGroup.MethaneWeight;         // Convert g/d to kg/d                  
                                    break;
                                case StockProps.prpRDPI: dValue = aGroup.AnimalState.RDP_Intake; break;
                                case StockProps.prpRDPR: dValue = aGroup.AnimalState.RDP_Reqd; break;
                                case StockProps.prpRDP_EFFECT: dValue = aGroup.AnimalState.RDP_IntakeEffect; break;
                                case StockProps.prpINTAKE_MOD: dValue = aGroup.IntakeModifier; break;
                                default: Result = false; break;
                            }
                        }

                        if (!bUseTag && !bUseAll)
                            aValue[Idx - 1] = dValue;
                        else if (varCode == StockProps.prpDSE)                                     // Sum DSE's; take a weighted average of 
                            dTotal = dTotal + dValue;                                            //   all other values                    
                        else if (aGroup != null)
                        {
                            dTotal = dTotal + aGroup.NoAnimals * dValue;
                            iDenom = iDenom + aGroup.NoAnimals;
                        }
                    } //_ loop over animal groups _
                }

                if ((varCode != StockProps.prpDSE) && (iDenom > 0))
                    dTotal = dTotal / iDenom;
                if (bUseAll)
                    aValue[0] = dTotal;
                else if (bUseTag)
                    aValue[iPass - 1] = dTotal;
            } //_ loop over passes _
            return Result;
        }

        /// <summary>
        /// Convert the dry matter pool
        /// </summary>
        /// <param name="Pool"></param>
        /// <param name="aValue"></param>
        /// <param name="bNPSVal"></param>
        public static void DMPool2Value(GrazType.DM_Pool Pool, ref TDMPoolHead aValue, bool bNPSVal = false)
        {
            if (!bNPSVal)
            {
                aValue.weight = Pool.DM;                                          // Item[1] = "weight"   kg/head          
                aValue.n = Pool.Nu[(int)GrazType.TOMElement.N];                   // Item[2] = "n"        kg/head          
                aValue.p = Pool.Nu[(int)GrazType.TOMElement.P];                   // Item[3] = "p"        kg/head          
                aValue.s = Pool.Nu[(int)GrazType.TOMElement.S];                   // Item[4] = "s"        kg/head          
                aValue.ash_alk = Pool.AshAlk;                                     // Item[5] = "ash_alk"  mol/head         
            }
            else
            {
                aValue.n = Pool.Nu[(int)GrazType.TOMElement.N];                   // Item[1] = "n"        kg/head          
                aValue.p = Pool.Nu[(int)GrazType.TOMElement.P];                   // Item[2] = "p"        kg/head          
                aValue.s = Pool.Nu[(int)GrazType.TOMElement.S];                   // Item[3] = "s"        kg/head          
            }
        }

        /// <summary>
        /// Populate the dry matter pool
        /// </summary>
        /// <param name="Model"></param>
        /// <param name="iCode"></param>
        /// <param name="bUseYoung"></param>
        /// <param name="bUseAll"></param>
        /// <param name="bUseTag"></param>
        /// <param name="aValue"></param>
        /// <returns></returns>
        public static bool PopulateDMPoolValue(TStockList Model, int iCode, bool bUseYoung, bool bUseAll, bool bUseTag, ref TDMPoolHead[] aValue)
        {
            int iNoPasses;
            TAnimalGroup aGroup;
            GrazType.DM_Pool Pool = new GrazType.DM_Pool();
            GrazType.DM_Pool TotalPool = new GrazType.DM_Pool();
            int iDenom;
            int iPass, Idx;

            bool Result = true;
            Array.Clear(aValue, 0, aValue.Length);

            if (bUseTag)
                iNoPasses = aValue.Length;
            else
                iNoPasses = 1;

            for (iPass = 1; iPass <= iNoPasses; iPass++)
            {
                GrazType.ZeroDMPool(ref TotalPool);
                iDenom = 0;

                for (Idx = 1; Idx <= Model.Count(); Idx++)
                {
                    if (!bUseTag || (Model.getTag(Idx) == iPass))
                    {
                        if (!bUseYoung)
                            aGroup = Model.At(Idx);
                        else
                            aGroup = Model.At(Idx).Young;

                        GrazType.ZeroDMPool(ref Pool);
                        if (aGroup != null)
                        {
                            int N = (int)GrazType.TOMElement.N;
                            int P = (int)GrazType.TOMElement.P;
                            int S = (int)GrazType.TOMElement.S;

                            switch (iCode)
                            {
                                case StockProps.prpINTAKE:
                                    Pool.DM = aGroup.AnimalState.DM_Intake.Total;
                                    Pool.Nu[N] = aGroup.AnimalState.CP_Intake.Total / GrazType.N2Protein;
                                    Pool.Nu[P] = aGroup.AnimalState.Phos_Intake.Total;
                                    Pool.Nu[S] = aGroup.AnimalState.Sulf_Intake.Total;
                                    Pool.AshAlk = aGroup.AnimalState.PaddockIntake.AshAlkalinity * aGroup.AnimalState.PaddockIntake.Biomass
                                                   + aGroup.AnimalState.SuppIntake.AshAlkalinity * aGroup.AnimalState.SuppIntake.Biomass;
                                    break;
                                case StockProps.prpINTAKE_PAST:
                                    Pool.DM = aGroup.AnimalState.DM_Intake.Herbage;
                                    Pool.Nu[N] = aGroup.AnimalState.CP_Intake.Herbage / GrazType.N2Protein;
                                    Pool.Nu[P] = aGroup.AnimalState.Phos_Intake.Herbage;
                                    Pool.Nu[S] = aGroup.AnimalState.Sulf_Intake.Herbage;
                                    Pool.AshAlk = aGroup.AnimalState.PaddockIntake.AshAlkalinity * aGroup.AnimalState.PaddockIntake.Biomass;
                                    break;
                                case StockProps.prpINTAKE_SUPP:
                                    Pool.DM = aGroup.AnimalState.DM_Intake.Supp;
                                    Pool.Nu[N] = aGroup.AnimalState.CP_Intake.Supp / GrazType.N2Protein;
                                    Pool.Nu[P] = aGroup.AnimalState.Phos_Intake.Supp;
                                    Pool.Nu[S] = aGroup.AnimalState.Sulf_Intake.Supp;
                                    Pool.AshAlk = aGroup.AnimalState.SuppIntake.AshAlkalinity * aGroup.AnimalState.SuppIntake.Biomass;
                                    break;
                                case StockProps.prpFAECES:
                                    GrazType.AddDMPool(aGroup.AnimalState.OrgFaeces, Pool);
                                    GrazType.AddDMPool(aGroup.AnimalState.InOrgFaeces, Pool);
                                    break;
                                case StockProps.prpINORG_FAECES:
                                    GrazType.AddDMPool(aGroup.AnimalState.InOrgFaeces, Pool);
                                    break;
                                default:
                                    Result = false;
                                    break;
                            }
                        }

                        if (!bUseTag && !bUseAll)
                            DMPool2Value(Pool, ref aValue[Idx - 1], (iCode == StockProps.prpINORG_FAECES));
                        else if (aGroup != null)
                        {
                            GrazType.AddDMPool(GrazType.MultiplyDMPool(Pool, aGroup.NoAnimals), TotalPool);
                            iDenom = iDenom + aGroup.NoAnimals;
                        }
                    }
                } //_ loop over animal groups _

                if (bUseTag || bUseAll)
                {
                    if (iDenom > 0)
                        TotalPool = GrazType.PoolFraction(TotalPool, 1.0 / iDenom);
                    if (bUseAll)
                        DMPool2Value(TotalPool, ref aValue[0], (iCode == StockProps.prpINORG_FAECES));
                    else
                        DMPool2Value(TotalPool, ref aValue[iPass - 1], (iCode == StockProps.prpINORG_FAECES));
                }
            }
            return Result;
        }

        /// <summary>
        /// Copy the supplement eaten into a TSupplementEaten[]
        /// </summary>
        /// <param name="Model"></param>
        /// <param name="aValue"></param>
        public static void MakeSuppEaten(TStockList Model, ref TSupplementEaten[] aValue)
        {
            int iCount;
            int iPadd;
            uint Idx;

            iCount = 0;
            for (iPadd = 0; iPadd <= Model.Paddocks.Count() - 1; iPadd++)
            {
                if (Model.Paddocks.byIndex(iPadd).SuppRemovalKG > 0.0)
                    iCount++;
            }
            Array.Resize(ref aValue, iCount);
            Idx = 0;
            for (iPadd = 0; iPadd <= Model.Paddocks.Count() - 1; iPadd++)
                if (Model.Paddocks.byIndex(iPadd).SuppRemovalKG > 0.0)
                {
                    aValue[Idx].paddock = Model.Paddocks.byIndex(iPadd).sName;
                    aValue[Idx].eaten = Model.Paddocks.byIndex(iPadd).SuppRemovalKG;
                    Idx++;
                }
        }
    }
}
