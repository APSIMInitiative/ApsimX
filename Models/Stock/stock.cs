// -----------------------------------------------------------------------
// <copyright file="stock.cs" company="CSIRO">
// CSIRO Agriculture
// </copyright>
// -----------------------------------------------------------------------

namespace Models.Stock
{
    using System;
    using System.Collections.Generic;
    using Models.Core;
    using Models.Grazplan;
    using StdUnits;

    /// <summary>
    /// The main GrazPlan stock class
    /// </summary>
    [Serializable]
    [ViewName("")]
    [PresenterName("")]
    public class Stock : Model
    {
        private List<string> FUserForages;      // user specified forage component names
        private List<string> FUserPaddocks;
        private TStockList FModel;
        //private TAnimalWeather FWeather;

        //private bool FFirstStep;
        private TSingleGenotypeInits[] FGenotypeInits = new TSingleGenotypeInits[0];
        private TAnimalInits[] FAnimalInits;
        private bool FPaddocksGiven;
        private int FRandSeed = 0;
        private TMyRandom FRandFactory;
        private TSupplement FSuppFed;
        private TExcretionInfo FExcretion;
        internal const int UNKNOWN = -1;
        /*
        /// <summary>
        /// The simulation
        /// </summary>
        [Link]
        Simulation Simulation = null;
*/
        /// <summary>
        /// The Stock class constructor
        /// </summary>
        public Stock()
        {
            this.FUserForages = new List<string>();
            this.FUserPaddocks = new List<string>();
            this.FRandFactory = new TMyRandom(this.FRandSeed);       // random number generator
            this.FModel = new TStockList(this.FRandFactory);

            Array.Resize(ref FGenotypeInits, 0);
            Array.Resize(ref FAnimalInits, 0);
            FSuppFed = new TSupplement();
            FExcretion = new TExcretionInfo();
            //FPaddocksGiven = false;
            //FFirstStep = true;
            FRandSeed = 0;
        }

        #region Initialisation properties ====================================================
        /// <summary>
        /// Seed for the random number generator
        /// </summary>
        [Description("Seed for the random number generator. Used when computing numbers of animals dying and conceiving from the equations for mortality and conception rates")]
        [Units("")]
        public int RandSeed
        {
            get { return FRandSeed; }
            set { FRandSeed = value; }
        }

        /// <summary>
        /// Information about each animal genotype
        /// </summary>
        [Description("Information about each animal genotype")]
        [Units("")]
        public TStockGeno[] GenoTypes
        {
            get
            {
                TStockGeno[] geno = new TStockGeno[1];
                StockVars.MakeGenotypesValue(FModel, ref geno);
                return geno;
            }
            set
            {
                Array.Resize(ref FGenotypeInits, value.Length);
                for (int Idx = 0; Idx < value.Length; Idx++)
                {
                    FGenotypeInits[Idx] = new TSingleGenotypeInits();
                    FModel.Value2GenotypeInits(value[Idx], ref FGenotypeInits[Idx]);
                }
            }
        }

        /// <summary>
        /// Initial state of each animal group for sheep
        /// </summary>
        [Description("Initial state of each animal group for sheep")]
        public TSheepInit[] Sheep
        {
            get
            {
                TSheepInit[] sheep = new TSheepInit[1];
                StockVars.MakeSheepValue(FModel, GrazType.AnimalType.Sheep, ref sheep);
                return sheep;
            }
            set
            {
                int iOffset = FAnimalInits.Length;
                Array.Resize(ref FAnimalInits, iOffset + value.Length);
                for (int Idx = 0; Idx < value.Length; Idx++)
                    FModel.SheepValue2AnimalInits(value[Idx], ref FAnimalInits[iOffset + Idx]);
            }
        }

        /// <summary>
        /// Initial state of each animal group for cattle
        /// </summary>
        [Description("Initial state of each animal group for cattle")]
        public TCattleInit[] Cattle
        {
            get
            {
                TCattleInit[] cattle = new TCattleInit[1];
                StockVars.MakeCattleValue(FModel, GrazType.AnimalType.Cattle, ref cattle);
                return cattle;
            }
            set
            {
                int iOffset = FAnimalInits.Length;
                Array.Resize(ref FAnimalInits, iOffset + value.Length);
                for (int Idx = 0; Idx < value.Length; Idx++)
                    FModel.CattleValue2AnimalInits(value[Idx], ref FAnimalInits[iOffset + Idx]);
            }
        }

        /// <summary>
        /// Manually-specified structure of paddocks and forages 
        /// </summary>
        [Description("Manually-specified structure of paddocks and forages")]
        public TPaddInit[] PaddockList
        {
            get
            {
                TPaddInit[] paddocks = new TPaddInit[1];
                StockVars.MakePaddockList(FModel, ref paddocks);
                return paddocks;
            }
            set
            {
                FPaddocksGiven = (value.Length > 0);
                TPaddockInfo aPadd;
                if (FPaddocksGiven)
                {
                    while (FModel.Paddocks.Count() > 0)
                        FModel.Paddocks.Delete(FModel.Paddocks.Count() - 1);

                    for (int Idx = 0; Idx < value.Length; Idx++)
                    {
                        FModel.Paddocks.Add(Idx, value[Idx].name);

                        aPadd = FModel.Paddocks.byIndex(Idx - 1);
                        aPadd.sExcretionDest = value[Idx].excretion;
                        aPadd.sUrineDest = value[Idx].urine;
                        aPadd.iExcretionID = UNKNOWN;
                        aPadd.iAddFaecesID = UNKNOWN;
                        aPadd.iAddUrineID = UNKNOWN;
                        aPadd.fArea = value[Idx].area;
                        aPadd.Slope = value[Idx].slope;
                        for (int Jdx = 0; Jdx < value[Idx].forages.Length; Jdx++)
                        {
                            FUserForages.Add(value[Idx].forages[Jdx]);    //keep a local list of these for queryInfos later
                            FUserPaddocks.Add(value[Idx].name);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Livestock enterprises and their management options
        /// </summary>
        [Description("Livestock enterprises and their management options")]
        public TEnterpriseInfo[] EnterpriseList
        {
            get
            {
                TEnterpriseInfo[] ents = new TEnterpriseInfo[FModel.Enterprises.Count];
                for (int i = 0; i < FModel.Enterprises.Count; i++)
                    ents[i] = FModel.Enterprises.byIndex(i);
                return ents;
            }
            set
            {
                if (FModel.Enterprises != null)
                {
                    while (FModel.Enterprises.Count > 0)
                        FModel.Enterprises.Delete(FModel.Enterprises.Count - 1);
                    for (int i = 0; i < value.Length; i++)
                        FModel.Enterprises.Add(value[i]);
                }
            }
        }

        /// <summary>
        /// Livestock grazing rotations
        /// </summary>
        [Description("Livestock grazing rotations")]
        public TGrazingPeriod[] GrazingPeriods
        {
            get
            {
                TGrazingPeriod[] periods = new TGrazingPeriod[FModel.GrazingPeriods.Count()];
                for (int i = 0; i < FModel.GrazingPeriods.Count(); i++)
                    periods[i] = FModel.GrazingPeriods.byIndex(i);
                return periods;
            }
            set
            {
                if (FModel.GrazingPeriods != null)
                {
                    while (FModel.GrazingPeriods.Count() > 0)
                    {
                        FModel.GrazingPeriods.Delete(FModel.GrazingPeriods.Count() - 1);
                    }
                    for (int i = 0; i < value.Length; i++)
                        FModel.GrazingPeriods.Add(value[i]);
                }
            }
        }

        #endregion

        #region Readable properties ====================================================
        /// <summary>
        /// Mass of grazers per unit area
        /// </summary>
        [Description("Mass of grazers per unit area. The value returned depends on the requesting component")]
        [Units("kg/ha")]
        public double Trampling
        {
            get
            {
                ////////////////////TForageProvider forageProvider;
                //using the compenent ID
                //return the mass per area for all forages
                /////////////forageProvider = FModel.ForagesAll.FindProvider(iRequestFrom);
                /////////////return FModel.returnMassPerArea(iRequestFrom, forageProvider, "kg/ha"); //by paddock or from forage ref
                return 0;
            }
        }

        /// <summary>
        /// Consumption of supplementary feed by animals
        /// </summary>
        [Description("Consumption of supplementary feed by animals")]
        public TSupplementEaten[] SuppEaten
        {
            get
            {
                TSupplementEaten[] value = new TSupplementEaten[1];
                StockVars.MakeSuppEaten(FModel, ref value);
                return value;
            }
        }

        /// <summary>
        /// Number of animal groups
        /// </summary>
        [Description("Number of animal groups")]
        public int NoGroups
        {
            get
            {
                return FModel.Count();
            }
        }

        // =============== All ============
        /// <summary>
        /// Number of animals in each group
        /// </summary>
        [Description("Number of animals in each group")]
        public int[] Number
        {
            get
            {
                int[] numbers = new int[FModel.Count()];
                StockVars.PopulateNumberValue(FModel, StockVars.CountType.eBoth, false, false, false, ref numbers);
                return numbers;
            }
        }

        /// <summary>
        /// Total number of animals
        /// </summary>
        [Description("Total number of animals")]
        public int NumberAll
        {
            get
            {
                int[] numbers = new int[1];
                StockVars.PopulateNumberValue(FModel, StockVars.CountType.eBoth, false, true, false, ref numbers);
                return numbers[0];
            }
        }

        /// <summary>
        /// Number of animals in each tag group
        /// </summary>
        [Description("Number of animals in each tag group")]
        public int[] NumberTag
        {
            get
            {
                int[] numbers = new int[FModel.iHighestTag()];
                StockVars.PopulateNumberValue(FModel, StockVars.CountType.eBoth, false, false, true, ref numbers);
                return numbers;
            }
        }

        // ============== Young ============
        /// <summary>
        /// Number of unweaned young animals in each group
        /// </summary>
        [Description("Number of unweaned young animals in each group")]
        public int[] NumberYng
        {
            get
            {
                int[] numbers = new int[FModel.Count()];
                StockVars.PopulateNumberValue(FModel, StockVars.CountType.eBoth, true, false, false, ref numbers);
                return numbers;
            }
        }

        /// <summary>
        /// Total number of unweaned young animals
        /// </summary>
        [Description("Number of unweaned young animals")]
        public int NumberYngAll
        {
            get
            {
                int[] numbers = new int[1];
                StockVars.PopulateNumberValue(FModel, StockVars.CountType.eBoth, true, true, false, ref numbers);
                return numbers[0];
            }
        }

        /// <summary>
        /// Number of unweaned young animals in each group
        /// </summary>
        [Description("Number of unweaned young animals in each tag group")]
        public int[] NumberYngTag
        {
            get
            {
                int[] numbers = new int[FModel.iHighestTag()];
                StockVars.PopulateNumberValue(FModel, StockVars.CountType.eBoth, true, false, true, ref numbers);
                return numbers;
            }
        }

        // ================ Female ============
        /// <summary>
        /// Number of female animals in each group
        /// </summary>
        [Description("Number of female animals in each group")]
        public int[] NoFemale
        {
            get
            {
                int[] numbers = new int[FModel.Count()];
                StockVars.PopulateNumberValue(FModel, StockVars.CountType.eFemale, false, false, false, ref numbers);
                return numbers;
            }
        }

        /// <summary>
        /// Total number of female animals
        /// </summary>
        [Description("Total number of female animals")]
        public int NoFemaleAll
        {
            get
            {
                int[] numbers = new int[1];
                StockVars.PopulateNumberValue(FModel, StockVars.CountType.eFemale, false, true, false, ref numbers);
                return numbers[0];
            }
        }

        /// <summary>
        /// Number of female animals in each tag group
        /// </summary>
        [Description("Number of female animals in each tag group")]
        public int[] NoFemaleTag
        {
            get
            {
                int[] numbers = new int[FModel.iHighestTag()];
                StockVars.PopulateNumberValue(FModel, StockVars.CountType.eFemale, false, false, true, ref numbers);
                return numbers;
            }
        }

        // ================ Female Young ============
        /// <summary>
        /// Number of unweaned female animals in each group
        /// </summary>
        [Description("Number of unweaned female animals in each group")]
        public int[] NoFemaleYng
        {
            get
            {
                int[] numbers = new int[FModel.Count()];
                StockVars.PopulateNumberValue(FModel, StockVars.CountType.eFemale, true, false, false, ref numbers);
                return numbers;
            }
        }

        /// <summary>
        /// Total number of unweaned female animals
        /// </summary>
        [Description("Total number of unweaned female animals")]
        public int NoFemaleYngAll
        {
            get
            {
                int[] numbers = new int[1];
                StockVars.PopulateNumberValue(FModel, StockVars.CountType.eFemale, true, true, false, ref numbers);
                return numbers[0];
            }
        }

        /// <summary>
        /// Number of unweaned female animals in each tag group
        /// </summary>
        [Description("Number of unweaned female animals in each tag group")]
        public int[] NoFemaleYngTag
        {
            get
            {
                int[] numbers = new int[FModel.iHighestTag()];
                StockVars.PopulateNumberValue(FModel, StockVars.CountType.eFemale, true, false, true, ref numbers);
                return numbers;
            }
        }

        // ================ Male ============
        /// <summary>
        /// Number of male animals in each group
        /// </summary>
        [Description("Number of male animals in each group")]
        public int[] NoMale
        {
            get
            {
                int[] numbers = new int[FModel.Count()];
                StockVars.PopulateNumberValue(FModel, StockVars.CountType.eMale, false, false, false, ref numbers);
                return numbers;
            }
        }

        /// <summary>
        /// Total number of male animals
        /// </summary>
        [Description("Total number of male animals")]
        public int NoMaleAll
        {
            get
            {
                int[] numbers = new int[1];
                StockVars.PopulateNumberValue(FModel, StockVars.CountType.eMale, false, true, false, ref numbers);
                return numbers[0];
            }
        }

        /// <summary>
        /// Number of male animals in each tag group
        /// </summary>
        [Description("Number of male animals in each tag group")]
        public int[] NoMaleTag
        {
            get
            {
                int[] numbers = new int[FModel.iHighestTag()];
                StockVars.PopulateNumberValue(FModel, StockVars.CountType.eMale, false, false, true, ref numbers);
                return numbers;
            }
        }

        // ================ Male Young ============
        /// <summary>
        /// Number of unweaned male animals in each group
        /// </summary>
        [Description("Number of unweaned male animals in each group")]
        public int[] NoMaleYng
        {
            get
            {
                int[] numbers = new int[FModel.Count()];
                StockVars.PopulateNumberValue(FModel, StockVars.CountType.eMale, true, false, false, ref numbers);
                return numbers;
            }
        }

        /// <summary>
        /// Total number of unweaned male animals
        /// </summary>
        [Description("Total number of unweaned male animals")]
        public int NoMaleYngAll
        {
            get
            {
                int[] numbers = new int[1];
                StockVars.PopulateNumberValue(FModel, StockVars.CountType.eMale, true, true, false, ref numbers);
                return numbers[0];
            }
        }

        /// <summary>
        /// Number of unweaned male animals in each tag group
        /// </summary>
        [Description("Number of unweaned male animals in each tag group")]
        public int[] NoMaleYngTag
        {
            get
            {
                int[] numbers = new int[FModel.iHighestTag()];
                StockVars.PopulateNumberValue(FModel, StockVars.CountType.eMale, true, false, true, ref numbers);
                return numbers;
            }
        }

        /// <summary>
        /// See the sex field of the sheep and cattle initialisation variables
        /// </summary>
        [Description("See the sex field of the sheep and cattle initialisation variables. Returns 'heifer' for cows under two years of age")]
        public string[] Sex
        {
            get
            {
                string[] values = new string[FModel.Count()];
                for (int Idx = 0; Idx < FModel.Count(); Idx++)
                    values[Idx] = FModel.SexString((int)Idx, false);
                return values;
            }
        }

        // =========== Ages ==================
        /// <summary>
        /// Age of animals by group
        /// </summary>
        [Description("Age of animals by group")]
        [Units("d")]
        public double[] Age
        {
            get
            {
                double[] values = new double[FModel.Count()];
                StockVars.PopulateRealValue(FModel, StockProps.prpAGE, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Age of animals total
        /// </summary>
        [Description("Age of animals total")]
        [Units("d")]
        public double AgeAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(FModel, StockProps.prpAGE, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Age of animals by tag number
        /// </summary>
        [Description("Age of animals by tag number")]
        [Units("d")]
        public double[] AgeTag
        {
            get
            {
                double[] values = new double[FModel.iHighestTag()];
                StockVars.PopulateRealValue(FModel, StockProps.prpAGE, false, false, true, ref values);
                return values;
            }
        }

        // =========== Ages of young ==================
        /// <summary>
        /// Age of unweaned young animals by group
        /// </summary>
        [Description("Age of unweaned young animals by group")]
        [Units("d")]
        public double[] AgeYng
        {
            get
            {
                double[] values = new double[FModel.Count()];
                StockVars.PopulateRealValue(FModel, StockProps.prpAGE, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Age of unweaned young animals total
        /// </summary>
        [Description("Age of unweaned young animals total")]
        [Units("d")]
        public double AgeYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(FModel, StockProps.prpAGE, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Age of unweaned young animals by tag number
        /// </summary>
        [Description("Age of unweaned young animals by tag number")]
        [Units("d")]
        public double[] AgeYngTag
        {
            get
            {
                double[] values = new double[FModel.iHighestTag()];
                StockVars.PopulateRealValue(FModel, StockProps.prpAGE, true, false, true, ref values);
                return values;
            }
        }

        // =========== Ages months ==================
        /// <summary>
        /// Age of animals, in months by group
        /// </summary>
        [Description("Age of animals, in months by group")]
        public double[] AgeMonths
        {
            get
            {
                double[] values = new double[FModel.Count()];
                StockVars.PopulateRealValue(FModel, StockProps.prpAGE_MONTHS, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Age of animals, in months total
        /// </summary>
        [Description("Age of animals, in months total")]
        public double AgeMonthsAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(FModel, StockProps.prpAGE_MONTHS, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Age of animals, in months by tag number
        /// </summary>
        [Description("Age of animals, in months by tag number")]
        public double[] AgeMonthsTag
        {
            get
            {
                double[] values = new double[FModel.iHighestTag()];
                StockVars.PopulateRealValue(FModel, StockProps.prpAGE_MONTHS, false, false, true, ref values);
                return values;
            }
        }

        // =========== Ages of young in months ==================
        /// <summary>
        /// Age of unweaned young animals, in months by group
        /// </summary>
        [Description("Age of unweaned young animals, in months by group")]
        public double[] AgeMonthsYng
        {
            get
            {
                double[] values = new double[FModel.Count()];
                StockVars.PopulateRealValue(FModel, StockProps.prpAGE_MONTHS, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Age of unweaned young animals, in months total
        /// </summary>
        [Description("Age of unweaned young animals, in months total")]
        public double AgeMonthsYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(FModel, StockProps.prpAGE_MONTHS, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Age of unweaned young animals, in months by tag number
        /// </summary>
        [Description("Age of unweaned young animals, in months by tag number")]
        public double[] AgeMonthsYngTag
        {
            get
            {
                double[] values = new double[FModel.iHighestTag()];
                StockVars.PopulateRealValue(FModel, StockProps.prpAGE_MONTHS, true, false, true, ref values);
                return values;
            }
        }

        // =========== Weight ==================
        /// <summary>
        /// Average live weight by group
        /// </summary>
        [Description("Average live weight by group")]
        [Units("kg")]
        public double[] Weight
        {
            get
            {
                double[] values = new double[FModel.Count()];
                StockVars.PopulateRealValue(FModel, StockProps.prpLIVE_WT, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Averge live weight total
        /// </summary>
        [Description("Averge live weight total")]
        [Units("kg")]
        public double WeightAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(FModel, StockProps.prpLIVE_WT, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Average live weight by tag number
        /// </summary>
        [Description("Average live weight by tag number")]
        [Units("kg")]
        public double[] WeightTag
        {
            get
            {
                double[] values = new double[FModel.iHighestTag()];
                StockVars.PopulateRealValue(FModel, StockProps.prpLIVE_WT, false, false, true, ref values);
                return values;
            }
        }

        // =========== Weight of young ==================
        /// <summary>
        /// Average live weight of unweaned young animals by group
        /// </summary>
        [Description("Average live weight of unweaned young animals by group")]
        [Units("kg")]
        public double[] WeightYng
        {
            get
            {
                double[] values = new double[FModel.Count()];
                StockVars.PopulateRealValue(FModel, StockProps.prpLIVE_WT, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Average live weight of unweaned young animals total
        /// </summary>
        [Description("Average live weight of unweaned young animals total")]
        [Units("kg")]
        public double WeightYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(FModel, StockProps.prpLIVE_WT, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Average live weight of unweaned young animals by tag number
        /// </summary>
        [Description("Average live weight of unweaned young animals by tag number")]
        [Units("kg")]
        public double[] WeightYngTag
        {
            get
            {
                double[] values = new double[FModel.iHighestTag()];
                StockVars.PopulateRealValue(FModel, StockProps.prpLIVE_WT, true, false, true, ref values);
                return values;
            }
        }

        // =========== Fleece-free, conceptus-free weight ==================
        /// <summary>
        /// Fleece-free, conceptus-free weight by group
        /// </summary>
        [Description("Fleece-free, conceptus-free weight by group")]
        [Units("kg")]
        public double[] BaseWt
        {
            get
            {
                double[] values = new double[FModel.Count()];
                StockVars.PopulateRealValue(FModel, StockProps.prpBASE_WT, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Fleece-free, conceptus-free weight total
        /// </summary>
        [Description("Fleece-free, conceptus-free weight total")]
        [Units("kg")]
        public double BaseWtAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(FModel, StockProps.prpBASE_WT, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Fleece-free, conceptus-free weight by tag number
        /// </summary>
        [Description("Fleece-free, conceptus-free weight by tag number")]
        [Units("kg")]
        public double[] BaseWtTag
        {
            get
            {
                double[] values = new double[FModel.iHighestTag()];
                StockVars.PopulateRealValue(FModel, StockProps.prpBASE_WT, false, false, true, ref values);
                return values;
            }
        }

        // =========== Fleece-free, conceptus-free weight young ==================
        /// <summary>
        /// Fleece-free, conceptus-free weight of unweaned young animals by group
        /// </summary>
        [Description("Fleece-free, conceptus-free weight of unweaned young animals by group")]
        [Units("kg")]
        public double[] BaseWtYng
        {
            get
            {
                double[] values = new double[FModel.Count()];
                StockVars.PopulateRealValue(FModel, StockProps.prpBASE_WT, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Fleece-free, conceptus-free weight of unweaned young animals total
        /// </summary>
        [Description("Fleece-free, conceptus-free weight of unweaned young animals total")]
        [Units("kg")]
        public double BaseWtYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(FModel, StockProps.prpBASE_WT, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Fleece-free, conceptus-free weight of unweaned young animals by tag number
        /// </summary>
        [Description("Fleece-free, conceptus-free weight of unweaned young animals by tag number")]
        [Units("kg")]
        public double[] BaseWtYngTag
        {
            get
            {
                double[] values = new double[FModel.iHighestTag()];
                StockVars.PopulateRealValue(FModel, StockProps.prpBASE_WT, true, false, true, ref values);
                return values;
            }
        }

        // =========== Condition score of animals ==================
        /// <summary>
        /// Condition score of animals (1-5 scale) by group
        /// </summary>
        [Description("Condition score of animals (1-5 scale) by group")]
        public double[] CondScore
        {
            get
            {
                double[] values = new double[FModel.Count()];
                StockVars.PopulateRealValue(FModel, StockProps.prpCOND_SCORE, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Condition score of animals (1-5 scale) total
        /// </summary>
        [Description("Condition score of animals (1-5 scale) total")]
        public double CondScoreAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(FModel, StockProps.prpCOND_SCORE, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Condition score of animals (1-5 scale) by tag number
        /// </summary>
        [Description("Condition score of animals (1-5 scale) by tag number")]
        public double[] CondScoreTag
        {
            get
            {
                double[] values = new double[FModel.iHighestTag()];
                StockVars.PopulateRealValue(FModel, StockProps.prpCOND_SCORE, false, false, true, ref values);
                return values;
            }
        }

        // =========== Condition score of animals (1-5 scale) of young ==================
        /// <summary>
        /// Condition score of unweaned young animals (1-5 scale) by group
        /// </summary>
        [Description("Condition score of unweaned young animals (1-5 scale) by group")]
        public double[] CondScoreYng
        {
            get
            {
                double[] values = new double[FModel.Count()];
                StockVars.PopulateRealValue(FModel, StockProps.prpCOND_SCORE, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Condition score of unweaned young animals (1-5 scale) total
        /// </summary>
        [Description("Condition score of unweaned young animals (1-5 scale) total")]
        public double CondScoreYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(FModel, StockProps.prpCOND_SCORE, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Condition score of unweaned young animals (1-5 scale) by tag number
        /// </summary>
        [Description("Condition score of unweaned young animals (1-5 scale) by tag number")]
        public double[] CondScoreYngTag
        {
            get
            {
                double[] values = new double[FModel.iHighestTag()];
                StockVars.PopulateRealValue(FModel, StockProps.prpCOND_SCORE, true, false, true, ref values);
                return values;
            }
        }

        // =========== Maximum previous basal weight ==================
        /// <summary>
        /// Maximum previous basal weight (fleece-free, conceptus-free) attained by each animal group
        /// </summary>
        [Description("Maximum previous basal weight (fleece-free, conceptus-free) attained by each animal group")]
        [Units("kg")]
        public double[] MaxPrevWt
        {
            get
            {
                double[] values = new double[FModel.Count()];
                StockVars.PopulateRealValue(FModel, StockProps.prpMAX_PREV_WT, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Maximum previous basal weight (fleece-free, conceptus-free) attained total
        /// </summary>
        [Description("Maximum previous basal weight (fleece-free, conceptus-free) attained total")]
        [Units("kg")]
        public double MaxPrevWtAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(FModel, StockProps.prpMAX_PREV_WT, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Maximum previous basal weight (fleece-free, conceptus-free) attained by tag number
        /// </summary>
        [Description("Maximum previous basal weight (fleece-free, conceptus-free) attained by tag number")]
        [Units("kg")]
        public double[] MaxPrevWtTag
        {
            get
            {
                double[] values = new double[FModel.iHighestTag()];
                StockVars.PopulateRealValue(FModel, StockProps.prpMAX_PREV_WT, false, false, true, ref values);
                return values;
            }
        }

        // =========== Maximum previous basal weight young ==================
        /// <summary>
        /// Maximum previous basal weight (fleece-free, conceptus-free) attained of unweaned young animals by group
        /// </summary>
        [Description("Maximum previous basal weight (fleece-free, conceptus-free) attained of unweaned young animals by group")]
        [Units("kg")]
        public double[] MaxPrevWtYng
        {
            get
            {
                double[] values = new double[FModel.Count()];
                StockVars.PopulateRealValue(FModel, StockProps.prpMAX_PREV_WT, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Maximum previous basal weight (fleece-free, conceptus-free) attained unweaned young animals total
        /// </summary>
        [Description("Maximum previous basal weight (fleece-free, conceptus-free) attained of unweaned young animals total")]
        [Units("kg")]
        public double MaxPrevWtYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(FModel, StockProps.prpMAX_PREV_WT, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Maximum previous basal weight (fleece-free, conceptus-free) attained of unweaned young animals by tag number
        /// </summary>
        [Description("Maximum previous basal weight (fleece-free, conceptus-free) attained of unweaned young animals by tag number")]
        [Units("kg")]
        public double[] MaxPrevWtYngTag
        {
            get
            {
                double[] values = new double[FModel.iHighestTag()];
                StockVars.PopulateRealValue(FModel, StockProps.prpMAX_PREV_WT, true, false, true, ref values);
                return values;
            }
        }

        // =========== Current greasy fleece weight ==================
        /// <summary>
        /// Current greasy fleece weight by group
        /// </summary>
        [Description("Current greasy fleece weight by group")]
        [Units("kg")]
        public double[] FleeceWt
        {
            get
            {
                double[] values = new double[FModel.Count()];
                StockVars.PopulateRealValue(FModel, StockProps.prpFLEECE_WT, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Current greasy fleece weight total
        /// </summary>
        [Description("Current greasy fleece weight total")]
        [Units("kg")]
        public double FleeceWtAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(FModel, StockProps.prpFLEECE_WT, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Current greasy fleece weight by tag number
        /// </summary>
        [Description("Current greasy fleece weight by tag number")]
        [Units("kg")]
        public double[] FleeceWtTag
        {
            get
            {
                double[] values = new double[FModel.iHighestTag()];
                StockVars.PopulateRealValue(FModel, StockProps.prpFLEECE_WT, false, false, true, ref values);
                return values;
            }
        }

        // =========== Current greasy fleece weight young ==================
        /// <summary>
        /// Current greasy fleece weight of unweaned young animals by group
        /// </summary>
        [Description("Current greasy fleece weight of unweaned young animals by group")]
        [Units("kg")]
        public double[] FleeceWtYng
        {
            get
            {
                double[] values = new double[FModel.Count()];
                StockVars.PopulateRealValue(FModel, StockProps.prpFLEECE_WT, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Current greasy fleece weight of unweaned young animals total
        /// </summary>
        [Description("Current greasy fleece weight of unweaned young animals total")]
        [Units("kg")]
        public double FleeceWtYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(FModel, StockProps.prpFLEECE_WT, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Current greasy fleece weight of unweaned young animals by tag number
        /// </summary>
        [Description("Current greasy fleece weight of unweaned young animals by tag number")]
        [Units("kg")]
        public double[] FleeceWtYngTag
        {
            get
            {
                double[] values = new double[FModel.iHighestTag()];
                StockVars.PopulateRealValue(FModel, StockProps.prpFLEECE_WT, true, false, true, ref values);
                return values;
            }
        }

        // =========== Current clean fleece weight ==================
        /// <summary>
        /// Current clean fleece weight by group
        /// </summary>
        [Description("Current clean fleece weight by group")]
        [Units("kg")]
        public double[] CFleeceWt
        {
            get
            {
                double[] values = new double[FModel.Count()];
                StockVars.PopulateRealValue(FModel, StockProps.prpCFLEECE_WT, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Current clean fleece weight total
        /// </summary>
        [Description("Current clean fleece weight total")]
        [Units("kg")]
        public double CFleeceWtAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(FModel, StockProps.prpCFLEECE_WT, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Current clean fleece weight by tag number
        /// </summary>
        [Description("Current clean fleece weight by tag number")]
        [Units("kg")]
        public double[] CFleeceWtTag
        {
            get
            {
                double[] values = new double[FModel.iHighestTag()];
                StockVars.PopulateRealValue(FModel, StockProps.prpCFLEECE_WT, false, false, true, ref values);
                return values;
            }
        }

        // =========== Current clean fleece weight young ==================
        /// <summary>
        /// Current clean fleece weight of unweaned young animals by group
        /// </summary>
        [Description("Current clean fleece weight of unweaned young animals by group")]
        [Units("kg")]
        public double[] CFleeceWtYng
        {
            get
            {
                double[] values = new double[FModel.Count()];
                StockVars.PopulateRealValue(FModel, StockProps.prpCFLEECE_WT, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Current clean fleece weight of unweaned young animals total
        /// </summary>
        [Description("Current clean fleece weight of unweaned young animals total")]
        [Units("kg")]
        public double CFleeceWtYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(FModel, StockProps.prpCFLEECE_WT, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Current clean fleece weight of unweaned young animals by tag number
        /// </summary>
        [Description("Current clean fleece weight of unweaned young animals by tag number")]
        [Units("kg")]
        public double[] CFleeceWtYngTag
        {
            get
            {
                double[] values = new double[FModel.iHighestTag()];
                StockVars.PopulateRealValue(FModel, StockProps.prpCFLEECE_WT, true, false, true, ref values);
                return values;
            }
        }

        // =========== Current average wool fibre diameter ==================
        /// <summary>
        /// Current average wool fibre diameter by group
        /// </summary>
        [Description("Current average wool fibre diameter by group")]
        [Units("um")]
        public double[] FibreDiam
        {
            get
            {
                double[] values = new double[FModel.Count()];
                StockVars.PopulateRealValue(FModel, StockProps.prpFIBRE_DIAM, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Current average wool fibre diameter total
        /// </summary>
        [Description("Current average wool fibre diameter total")]
        [Units("um")]
        public double FibreDiamAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(FModel, StockProps.prpFIBRE_DIAM, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Current average wool fibre diameter by tag number
        /// </summary>
        [Description("Current average wool fibre diameter by tag number")]
        [Units("um")]
        public double[] FibreDiamTag
        {
            get
            {
                double[] values = new double[FModel.iHighestTag()];
                StockVars.PopulateRealValue(FModel, StockProps.prpFIBRE_DIAM, false, false, true, ref values);
                return values;
            }
        }

        // =========== Current average wool fibre diameter young ==================
        /// <summary>
        /// Current average wool fibre diameter of unweaned young animals by group
        /// </summary>
        [Description("Current average wool fibre diameter of unweaned young animals by group")]
        [Units("um")]
        public double[] FibreDiamYng
        {
            get
            {
                double[] values = new double[FModel.Count()];
                StockVars.PopulateRealValue(FModel, StockProps.prpFIBRE_DIAM, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Current average wool fibre diameter of unweaned young animals total
        /// </summary>
        [Description("Current average wool fibre diameter of unweaned young animals total")]
        [Units("um")]
        public double FibreDiamYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(FModel, StockProps.prpFIBRE_DIAM, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Current average wool fibre diameter of unweaned young animals by tag number
        /// </summary>
        [Description("Current average wool fibre diameter of unweaned young animals by tag number")]
        [Units("um")]
        public double[] FibreDiamYngTag
        {
            get
            {
                double[] values = new double[FModel.iHighestTag()];
                StockVars.PopulateRealValue(FModel, StockProps.prpFIBRE_DIAM, true, false, true, ref values);
                return values;
            }
        }

        // =========== If the animals are pregnant, the number of days since conception; zero otherwise ==================
        /// <summary>
        /// If the animals are pregnant, the number of days since conception; zero otherwise, by group
        /// </summary>
        [Description("If the animals are pregnant, the number of days since conception; zero otherwise, by group")]
        [Units("d")]
        public double[] Pregnant
        {
            get
            {
                double[] values = new double[FModel.Count()];
                StockVars.PopulateRealValue(FModel, StockProps.prpPREGNANT, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// If the animals are pregnant, the number of days since conception; zero otherwise, total
        /// </summary>
        [Description("If the animals are pregnant, the number of days since conception; zero otherwise, total")]
        [Units("d")]
        public double PregnantAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(FModel, StockProps.prpPREGNANT, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// If the animals are pregnant, the number of days since conception; zero otherwise, by tag number
        /// </summary>
        [Description("If the animals are pregnant, the number of days since conception; zero otherwise, by tag number")]
        [Units("d")]
        public double[] PregnantTag
        {
            get
            {
                double[] values = new double[FModel.iHighestTag()];
                StockVars.PopulateRealValue(FModel, StockProps.prpPREGNANT, false, false, true, ref values);
                return values;
            }
        }

        // =========== If the animals are lactating, the number of days since birth of the lamb or calf; zero otherwise ==================
        /// <summary>
        /// If the animals are lactating, the number of days since birth of the lamb or calf; zero otherwise, by group
        /// </summary>
        [Description("If the animals are lactating, the number of days since birth of the lamb or calf; zero otherwise, by group")]
        [Units("d")]
        public double[] Lactating
        {
            get
            {
                double[] values = new double[FModel.Count()];
                StockVars.PopulateRealValue(FModel, StockProps.prpLACTATING, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// If the animals are lactating, the number of days since birth of the lamb or calf; zero otherwise, total
        /// </summary>
        [Description("If the animals are lactating, the number of days since birth of the lamb or calf; zero otherwise, total")]
        [Units("d")]
        public double LactatingAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(FModel, StockProps.prpLACTATING, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// If the animals are lactating, the number of days since birth of the lamb or calf; zero otherwise, by tag number
        /// </summary>
        [Description("If the animals are lactating, the number of days since birth of the lamb or calf; zero otherwise, by tag number")]
        [Units("d")]
        public double[] LactatingTag
        {
            get
            {
                double[] values = new double[FModel.iHighestTag()];
                StockVars.PopulateRealValue(FModel, StockProps.prpLACTATING, false, false, true, ref values);
                return values;
            }
        }

        // =========== Number of foetuses per head ==================
        /// <summary>
        /// Number of foetuses per head by group
        /// </summary>
        [Description("Number of foetuses per head by group")]
        public double[] NoFoetuses
        {
            get
            {
                double[] values = new double[FModel.Count()];
                StockVars.PopulateRealValue(FModel, StockProps.prpNO_FOETUSES, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Number of foetuses per head total
        /// </summary>
        [Description("Number of foetuses per head total")]
        public double NoFoetusesAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(FModel, StockProps.prpNO_FOETUSES, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Number of foetuses per head by tag number
        /// </summary>
        [Description("Number of foetuses per head by tag number")]
        public double[] NoFoetusesTag
        {
            get
            {
                double[] values = new double[FModel.iHighestTag()];
                StockVars.PopulateRealValue(FModel, StockProps.prpNO_FOETUSES, false, false, true, ref values);
                return values;
            }
        }

        //AddScalarSet(ref   Idx, StockProps.prpNO_SUCKLING, "no_suckling", TTypedValue.TBaseType.ITYPE_DOUBLE, "", false, "Number of unweaned lambs or calves per head", "");
        // =========== Number of unweaned lambs or calves per head ==================
        /// <summary>
        /// Number of unweaned lambs or calves per head by group
        /// </summary>
        [Description("Number of unweaned lambs or calves per head by group")]
        public double[] NoSuckling
        {
            get
            {
                double[] values = new double[FModel.Count()];
                StockVars.PopulateRealValue(FModel, StockProps.prpNO_SUCKLING, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Number of unweaned lambs or calves per head total
        /// </summary>
        [Description("Number of unweaned lambs or calves per head total")]
        public double NoSucklingAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(FModel, StockProps.prpNO_SUCKLING, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Number of unweaned lambs or calves per head by tag number
        /// </summary>
        [Description("Number of unweaned lambs or calves per head by tag number")]
        public double[] NoSucklingTag
        {
            get
            {
                double[] values = new double[FModel.iHighestTag()];
                StockVars.PopulateRealValue(FModel, StockProps.prpNO_SUCKLING, false, false, true, ref values);
                return values;
            }
        }

        // =========== Condition score at last parturition; zero if lactating=0 ==================
        /// <summary>
        /// Condition score at last parturition; zero if lactating=0, by group
        /// </summary>
        [Description("Condition score at last parturition; zero if lactating=0, by group")]
        public double[] BirthCS
        {
            get
            {
                double[] values = new double[FModel.Count()];
                StockVars.PopulateRealValue(FModel, StockProps.prpBIRTH_CS, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Condition score at last parturition; zero if lactating=0, total
        /// </summary>
        [Description("Condition score at last parturition; zero if lactating=0, total")]
        public double BirthCSAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(FModel, StockProps.prpBIRTH_CS, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Condition score at last parturition; zero if lactating=0, by tag number
        /// </summary>
        [Description("Condition score at last parturition; zero if lactating=0, by tag number")]
        public double[] BirthCSTag
        {
            get
            {
                double[] values = new double[FModel.iHighestTag()];
                StockVars.PopulateRealValue(FModel, StockProps.prpBIRTH_CS, false, false, true, ref values);
                return values;
            }
        }

        /// <summary>
        /// Paddock occupied by each animal group
        /// </summary>
        [Description("Paddock occupied by each animal group")]
        public string[] Paddock
        {
            get
            {
                string[] paddocks = new string[FModel.Count()];
                for (int Idx = 1; Idx <= FModel.Count(); Idx++)
                    paddocks[Idx - 1] = FModel.getInPadd((int)Idx);
                return paddocks;
            }
        }

        /// <summary>
        /// Tag value assigned to each animal group
        /// </summary>
        [Description("Tag value assigned to each animal group")]
        public int[] TagNo
        {
            get
            {
                int[] tags = new int[FModel.Count()];
                for (int Idx = 1; Idx <= FModel.Count(); Idx++)
                    tags[Idx - 1] = FModel.getTag((int)Idx);
                return tags;
            }
        }

        /// <summary>
        /// Priority score assigned to each animal group; used in drafting
        /// </summary>
        [Description("Priority score assigned to each animal group; used in drafting")]
        public int[] Priority
        {
            get
            {
                int[] priorities = new int[FModel.Count()];
                for (int Idx = 1; Idx <= FModel.Count(); Idx++)
                    priorities[Idx - 1] = FModel.getPriority((int)Idx);
                return priorities;
            }
        }

        // =========== Dry sheep equivalents, based on potential intake ==================
        /// <summary>
        /// Dry sheep equivalents, based on potential intake by group
        /// </summary>
        [Description("Dry sheep equivalents, based on potential intake by group")]
        public double[] DSE
        {
            get
            {
                double[] values = new double[FModel.Count()];
                StockVars.PopulateRealValue(FModel, StockProps.prpDSE, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Dry sheep equivalents, based on potential intake total
        /// </summary>
        [Description("Dry sheep equivalents, based on potential intake total")]
        public double DSEAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(FModel, StockProps.prpDSE, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Dry sheep equivalents, based on potential intake by tag number
        /// </summary>
        [Description("Dry sheep equivalents, based on potential intake by tag number")]
        public double[] DSETag
        {
            get
            {
                double[] values = new double[FModel.iHighestTag()];
                StockVars.PopulateRealValue(FModel, StockProps.prpDSE, false, false, true, ref values);
                return values;
            }
        }

        // =========== Dry sheep equivalents, based on potential intake young ==================
        /// <summary>
        /// Dry sheep equivalents, based on potential intake of unweaned young animals by group
        /// </summary>
        [Description("Dry sheep equivalents, based on potential intake of unweaned young animals by group")]
        public double[] DSEYng
        {
            get
            {
                double[] values = new double[FModel.Count()];
                StockVars.PopulateRealValue(FModel, StockProps.prpDSE, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Dry sheep equivalents, based on potential intake of unweaned young animals total
        /// </summary>
        [Description("Dry sheep equivalents, based on potential intake of unweaned young animals total")]
        public double DSEYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(FModel, StockProps.prpDSE, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Dry sheep equivalents, based on potential intake of unweaned young animals by tag number
        /// </summary>
        [Description("Dry sheep equivalents, based on potential intake of unweaned young animals by tag number")]
        public double[] DSEYngTag
        {
            get
            {
                double[] values = new double[FModel.iHighestTag()];
                StockVars.PopulateRealValue(FModel, StockProps.prpDSE, true, false, true, ref values);
                return values;
            }
        }

        //AddScalarSet(ref   Idx, StockProps., "wt_change", TTypedValue.TBaseType.ITYPE_DOUBLE, "kg/d", true, "Rate of change of base weight of each animal group", "");
        // =========== Rate of change of base weight of each animal group ==================
        /// <summary>
        /// Rate of change of base weight of each animal by group
        /// </summary>
        [Description("Rate of change of base weight of each animal by group")]
        [Units("kg/d")]
        public double[] WtChange
        {
            get
            {
                double[] values = new double[FModel.Count()];
                StockVars.PopulateRealValue(FModel, StockProps.prpWT_CHANGE, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Rate of change of base weight of each animal total
        /// </summary>
        [Description("Rate of change of base weight of each animal total")]
        [Units("kg/d")]
        public double WtChangeAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(FModel, StockProps.prpWT_CHANGE, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Rate of change of base weight of each animal by tag number
        /// </summary>
        [Description("Rate of change of base weight of each animal by tag number")]
        [Units("kg/d")]
        public double[] WtChangeTag
        {
            get
            {
                double[] values = new double[FModel.iHighestTag()];
                StockVars.PopulateRealValue(FModel, StockProps.prpWT_CHANGE, false, false, true, ref values);
                return values;
            }
        }

        // =========== Rate of change of base weight of each animal group young ==================
        /// <summary>
        /// Rate of change of base weight of unweaned young animals by group
        /// </summary>
        [Description("Rate of change of base weight of unweaned young animals by group")]
        [Units("kg/d")]
        public double[] WtChangeYng
        {
            get
            {
                double[] values = new double[FModel.Count()];
                StockVars.PopulateRealValue(FModel, StockProps.prpWT_CHANGE, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Rate of change of base weight of unweaned young animals total
        /// </summary>
        [Description("Rate of change of base weight of unweaned young animals total")]
        [Units("kg/d")]
        public double WtChangeYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(FModel, StockProps.prpWT_CHANGE, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Rate of change of base weight of unweaned young animals by tag number
        /// </summary>
        [Description("Rate of change of base weight of unweaned young animals by tag number")]
        [Units("kg/d")]
        public double[] WtChangeYngTag
        {
            get
            {
                double[] values = new double[FModel.iHighestTag()];
                StockVars.PopulateRealValue(FModel, StockProps.prpWT_CHANGE, true, false, true, ref values);
                return values;
            }
        }

        // =========== Total intake per head of dry matter and nutrients by each animal group ==================
        /// <summary>
        /// Total intake per head of dry matter and nutrients by each animal group
        /// </summary>
        [Description("Total intake per head of dry matter and nutrients by each animal group")]
        public TDMPoolHead[] Intake
        {
            get
            {
                TDMPoolHead[] pools = new TDMPoolHead[FModel.Count()];
                StockVars.PopulateDMPoolValue(FModel, StockProps.prpINTAKE, false, false, false, ref pools);
                return pools;
            }
        }

        /// <summary>
        /// Total intake per head of dry matter and nutrients
        /// </summary>
        [Description("Total intake per head of dry matter and nutrients")]
        public TDMPoolHead IntakeAll
        {
            get
            {
                TDMPoolHead[] pools = new TDMPoolHead[1];
                StockVars.PopulateDMPoolValue(FModel, StockProps.prpINTAKE, false, true, false, ref pools);
                return pools[0];
            }
        }

        /// <summary>
        /// Total intake per head of dry matter and nutrients by tag
        /// </summary>
        [Description("Total intake per head of dry matter and nutrients by tag")]
        public TDMPoolHead[] IntakeTag
        {
            get
            {
                TDMPoolHead[] pools = new TDMPoolHead[FModel.iHighestTag()];
                StockVars.PopulateDMPoolValue(FModel, StockProps.prpINTAKE, false, false, true, ref pools);
                return pools;
            }
        }

        // =========== Total intake per head of dry matter and nutrients of unweaned animals by group ==================
        /// <summary>
        /// Total intake per head of dry matter and nutrients of unweaned animals by group
        /// </summary>
        [Description("Total intake per head of dry matter and nutrients of unweaned animals by group")]
        public TDMPoolHead[] IntakeYng
        {
            get
            {
                TDMPoolHead[] pools = new TDMPoolHead[FModel.Count()];
                StockVars.PopulateDMPoolValue(FModel, StockProps.prpINTAKE, true, false, false, ref pools);
                return pools;
            }
        }

        /// <summary>
        /// Total intake per head of dry matter and nutrients of unweaned animals
        /// </summary>
        [Description("Total intake per head of dry matter and nutrients of unweaned animals")]
        public TDMPoolHead IntakeYngAll
        {
            get
            {
                TDMPoolHead[] pools = new TDMPoolHead[1];
                StockVars.PopulateDMPoolValue(FModel, StockProps.prpINTAKE, true, true, false, ref pools);
                return pools[0];
            }
        }

        /// <summary>
        /// Total intake per head of dry matter and nutrients of unweaned animals by tag
        /// </summary>
        [Description("Total intake per head of dry matter and nutrients of unweaned animals by tag")]
        public TDMPoolHead[] IntakeYngTag
        {
            get
            {
                TDMPoolHead[] pools = new TDMPoolHead[FModel.iHighestTag()];
                StockVars.PopulateDMPoolValue(FModel, StockProps.prpINTAKE, true, false, true, ref pools);
                return pools;
            }
        }

        // =========== Intake per head of pasture dry matter and nutrients by each animal group ==================
        /// <summary>
        /// Intake per head of pasture dry matter and nutrients by each animal group
        /// </summary>
        [Description("Intake per head of pasture dry matter and nutrients by each animal group")]
        public TDMPoolHead[] PastIntake
        {
            get
            {
                TDMPoolHead[] pools = new TDMPoolHead[FModel.Count()];
                StockVars.PopulateDMPoolValue(FModel, StockProps.prpINTAKE_PAST, false, false, false, ref pools);
                return pools;
            }
        }

        /// <summary>
        /// Intake per head of pasture dry matter and nutrients
        /// </summary>
        [Description("Intake per head of pasture dry matter and nutrients")]
        public TDMPoolHead PastIntakeAll
        {
            get
            {
                TDMPoolHead[] pools = new TDMPoolHead[1];
                StockVars.PopulateDMPoolValue(FModel, StockProps.prpINTAKE_PAST, false, true, false, ref pools);
                return pools[0];
            }
        }

        /// <summary>
        /// Intake per head of pasture dry matter and nutrients by tag
        /// </summary>
        [Description("Intake per head of pasture dry matter and nutrients by tag")]
        public TDMPoolHead[] PastIntakeTag
        {
            get
            {
                TDMPoolHead[] pools = new TDMPoolHead[FModel.iHighestTag()];
                StockVars.PopulateDMPoolValue(FModel, StockProps.prpINTAKE_PAST, false, false, true, ref pools);
                return pools;
            }
        }

        // =========== Intake per head of pasture dry matter and nutrients of unweaned animals by group ==================
        /// <summary>
        /// Intake per head of pasture dry matter and nutrients of unweaned animals by group
        /// </summary>
        [Description("Intake per head of pasture dry matter and nutrients of unweaned animals by group")]
        public TDMPoolHead[] PastIntakeYng
        {
            get
            {
                TDMPoolHead[] pools = new TDMPoolHead[FModel.Count()];
                StockVars.PopulateDMPoolValue(FModel, StockProps.prpINTAKE_PAST, true, false, false, ref pools);
                return pools;
            }
        }

        /// <summary>
        /// Intake per head of pasture dry matter and nutrients of unweaned animals
        /// </summary>
        [Description("Intake per head of pasture dry matter and nutrients of unweaned animals")]
        public TDMPoolHead PastIntakeYngAll
        {
            get
            {
                TDMPoolHead[] pools = new TDMPoolHead[1];
                StockVars.PopulateDMPoolValue(FModel, StockProps.prpINTAKE_PAST, true, true, false, ref pools);
                return pools[0];
            }
        }

        /// <summary>
        /// Intake per head of pasture dry matter and nutrients of unweaned animals by tag
        /// </summary>
        [Description("Intake per head of pasture dry matter and nutrients of unweaned animals by tag")]
        public TDMPoolHead[] PastIntakeYngTag
        {
            get
            {
                TDMPoolHead[] pools = new TDMPoolHead[FModel.iHighestTag()];
                StockVars.PopulateDMPoolValue(FModel, StockProps.prpINTAKE_PAST, true, false, true, ref pools);
                return pools;
            }
        }

        // =========== Intake per head of supplement dry matter and nutrients by each animal group ==================
        /// <summary>
        /// Intake per head of supplement dry matter and nutrients by each animal group
        /// </summary>
        [Description("Intake per head of supplement dry matter and nutrients by each animal group")]
        public TDMPoolHead[] SuppIntake
        {
            get
            {
                TDMPoolHead[] pools = new TDMPoolHead[FModel.Count()];
                StockVars.PopulateDMPoolValue(FModel, StockProps.prpINTAKE_SUPP, false, false, false, ref pools);
                return pools;
            }
        }

        /// <summary>
        /// Intake per head of supplement dry matter and nutrients
        /// </summary>
        [Description("Intake per head of supplement dry matter and nutrients")]
        public TDMPoolHead SuppIntakeAll
        {
            get
            {
                TDMPoolHead[] pools = new TDMPoolHead[1];
                StockVars.PopulateDMPoolValue(FModel, StockProps.prpINTAKE_SUPP, false, true, false, ref pools);
                return pools[0];
            }
        }

        /// <summary>
        /// Intake per head of supplement dry matter and nutrients by tag
        /// </summary>
        [Description("Intake per head of supplement dry matter and nutrients by tag")]
        public TDMPoolHead[] SuppIntakeTag
        {
            get
            {
                TDMPoolHead[] pools = new TDMPoolHead[FModel.iHighestTag()];
                StockVars.PopulateDMPoolValue(FModel, StockProps.prpINTAKE_SUPP, false, false, true, ref pools);
                return pools;
            }
        }

        // =========== Intake per head of supplement dry matter and nutrients of unweaned animals by group ==================
        /// <summary>
        /// Intake per head of supplement dry matter and nutrients of unweaned animals by group
        /// </summary>
        [Description("Intake per head of supplement dry matter and nutrients of unweaned animals by group")]
        public TDMPoolHead[] SuppIntakeYng
        {
            get
            {
                TDMPoolHead[] pools = new TDMPoolHead[FModel.Count()];
                StockVars.PopulateDMPoolValue(FModel, StockProps.prpINTAKE_SUPP, true, false, false, ref pools);
                return pools;
            }
        }

        /// <summary>
        /// Intake per head of supplement dry matter and nutrients of unweaned animals
        /// </summary>
        [Description("Intake per head of supplement dry matter and nutrients of unweaned animals")]
        public TDMPoolHead SuppIntakeYngAll
        {
            get
            {
                TDMPoolHead[] pools = new TDMPoolHead[1];
                StockVars.PopulateDMPoolValue(FModel, StockProps.prpINTAKE_SUPP, true, true, false, ref pools);
                return pools[0];
            }
        }

        /// <summary>
        /// Intake per head of supplement dry matter and nutrients of unweaned animals by tag
        /// </summary>
        [Description("Intake per head of supplement dry matter and nutrients of unweaned animals by tag")]
        public TDMPoolHead[] SuppIntakeYngTag
        {
            get
            {
                TDMPoolHead[] pools = new TDMPoolHead[FModel.iHighestTag()];
                StockVars.PopulateDMPoolValue(FModel, StockProps.prpINTAKE_SUPP, true, false, true, ref pools);
                return pools;
            }
        }

        // =========== Intake per head of metabolizable energy ==================
        /// <summary>
        /// Intake per head of metabolizable energy by group
        /// </summary>
        [Description("Intake per head of metabolizable energy by group")]
        [Units("MJ/d")]
        public double[] MEIntake
        {
            get
            {
                double[] values = new double[FModel.Count()];
                StockVars.PopulateRealValue(FModel, StockProps.prpME_INTAKE, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Intake per head of metabolizable energy total
        /// </summary>
        [Description("Intake per head of metabolizable energy total")]
        [Units("MJ/d")]
        public double MEIntakeAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(FModel, StockProps.prpME_INTAKE, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Intake per head of metabolizable energy by tag number
        /// </summary>
        [Description("Intake per head of metabolizable energy by tag number")]
        [Units("MJ/d")]
        public double[] MEIntakeTag
        {
            get
            {
                double[] values = new double[FModel.iHighestTag()];
                StockVars.PopulateRealValue(FModel, StockProps.prpME_INTAKE, false, false, true, ref values);
                return values;
            }
        }

        // =========== Intake per head of metabolizable energy of young ==================
        /// <summary>
        /// Intake per head of metabolizable energy of unweaned young animals by group
        /// </summary>
        [Description("Intake per head of metabolizable energy of unweaned young animals by group")]
        [Units("MJ/d")]
        public double[] MEIntakeYng
        {
            get
            {
                double[] values = new double[FModel.Count()];
                StockVars.PopulateRealValue(FModel, StockProps.prpME_INTAKE, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Intake per head of metabolizable energy of unweaned young animals total
        /// </summary>
        [Description("Intake per head of metabolizable energy of unweaned young animals total")]
        [Units("MJ/d")]
        public double MEIntakeYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(FModel, StockProps.prpME_INTAKE, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Intake per head of metabolizable energy of unweaned young animals by tag number
        /// </summary>
        [Description("Intake per head of metabolizable energy of unweaned young animals by tag number")]
        [Units("MJ/d")]
        public double[] MEIntakeYngTag
        {
            get
            {
                double[] values = new double[FModel.iHighestTag()];
                StockVars.PopulateRealValue(FModel, StockProps.prpME_INTAKE, true, false, true, ref values);
                return values;
            }
        }

        // =========== Crude protein intake per head ==================
        /// <summary>
        /// Crude protein intake per head by group
        /// </summary>
        [Description("Crude protein intake per head by group")]
        [Units("kg/d")]
        public double[] CPIntake
        {
            get
            {
                double[] values = new double[FModel.Count()];
                StockVars.PopulateRealValue(FModel, StockProps.prpCPI_INTAKE, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Crude protein intake per head total
        /// </summary>
        [Description("Crude protein intake per head total")]
        [Units("kg/d")]
        public double CPIntakeAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(FModel, StockProps.prpCPI_INTAKE, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Crude protein intake per head by tag number
        /// </summary>
        [Description("Crude protein intake per head by tag number")]
        [Units("kg/d")]
        public double[] CPIntakeTag
        {
            get
            {
                double[] values = new double[FModel.iHighestTag()];
                StockVars.PopulateRealValue(FModel, StockProps.prpCPI_INTAKE, false, false, true, ref values);
                return values;
            }
        }

        // =========== Crude protein intake per head of young ==================
        /// <summary>
        /// Crude protein intake per head of unweaned young animals by group
        /// </summary>
        [Description("Crude protein intake per head of unweaned young animals by group")]
        [Units("kg/d")]
        public double[] CPIntakeYng
        {
            get
            {
                double[] values = new double[FModel.Count()];
                StockVars.PopulateRealValue(FModel, StockProps.prpCPI_INTAKE, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Crude protein intake per head of unweaned young animals total
        /// </summary>
        [Description("Crude protein intake per head of unweaned young animals total")]
        [Units("kg/d")]
        public double CPIntakeYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(FModel, StockProps.prpCPI_INTAKE, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Crude protein intake per head of unweaned young animals by tag number
        /// </summary>
        [Description("Crude protein intake per head of unweaned young animals by tag number")]
        [Units("kg/d")]
        public double[] CPIntakeYngTag
        {
            get
            {
                double[] values = new double[FModel.iHighestTag()];
                StockVars.PopulateRealValue(FModel, StockProps.prpCPI_INTAKE, true, false, true, ref values);
                return values;
            }
        }

        // =========== Growth rate of clean fleece ==================
        /// <summary>
        /// Growth rate of clean fleece by group
        /// </summary>
        [Description("Growth rate of clean fleece by group")]
        [Units("kg/d")]
        public double[] CFleeceGrowth
        {
            get
            {
                double[] values = new double[FModel.Count()];
                StockVars.PopulateRealValue(FModel, StockProps.prpCFLEECE_GROWTH, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Growth rate of clean fleece total
        /// </summary>
        [Description("Growth rate of clean fleece total")]
        [Units("kg/d")]
        public double CFleeceGrowthAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(FModel, StockProps.prpCFLEECE_GROWTH, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Growth rate of clean fleece by tag number
        /// </summary>
        [Description("Growth rate of clean fleece by tag number")]
        [Units("kg/d")]
        public double[] CFleeceGrowthTag
        {
            get
            {
                double[] values = new double[FModel.iHighestTag()];
                StockVars.PopulateRealValue(FModel, StockProps.prpCFLEECE_GROWTH, false, false, true, ref values);
                return values;
            }
        }

        // =========== Growth rate of clean fleece of young ==================
        /// <summary>
        /// Growth rate of clean fleece of unweaned young animals by group
        /// </summary>
        [Description("Growth rate of clean fleece of unweaned young animals by group")]
        [Units("kg/d")]
        public double[] CFleeceGrowthYng
        {
            get
            {
                double[] values = new double[FModel.Count()];
                StockVars.PopulateRealValue(FModel, StockProps.prpCFLEECE_GROWTH, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Growth rate of clean fleece of unweaned young animals total
        /// </summary>
        [Description("Growth rate of clean fleece of unweaned young animals total")]
        [Units("kg/d")]
        public double CFleeceGrowthYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(FModel, StockProps.prpCFLEECE_GROWTH, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Growth rate of clean fleece of unweaned young animals by tag number
        /// </summary>
        [Description("Growth rate of clean fleece of unweaned young animals by tag number")]
        [Units("kg/d")]
        public double[] CFleeceGrowthYngTag
        {
            get
            {
                double[] values = new double[FModel.iHighestTag()];
                StockVars.PopulateRealValue(FModel, StockProps.prpCFLEECE_GROWTH, true, false, true, ref values);
                return values;
            }
        }

        // =========== Fibre diameter of the current day's wool growth ==================
        /// <summary>
        /// Fibre diameter of the current day's wool growth by group
        /// </summary>
        [Description("Fibre diameter of the current day's wool growth by group")]
        [Units("um")]
        public double[] FibreGrowthDiam
        {
            get
            {
                double[] values = new double[FModel.Count()];
                StockVars.PopulateRealValue(FModel, StockProps.prpDAY_FIBRE_DIAM, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Fibre diameter of the current day's wool growth total
        /// </summary>
        [Description("Fibre diameter of the current day's wool growth total")]
        [Units("um")]
        public double FibreGrowthDiamAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(FModel, StockProps.prpDAY_FIBRE_DIAM, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Fibre diameter of the current day's wool growth by tag number
        /// </summary>
        [Description("Fibre diameter of the current day's wool growth by tag number")]
        [Units("um")]
        public double[] FibreGrowthDiamTag
        {
            get
            {
                double[] values = new double[FModel.iHighestTag()];
                StockVars.PopulateRealValue(FModel, StockProps.prpDAY_FIBRE_DIAM, false, false, true, ref values);
                return values;
            }
        }

        // =========== Fibre diameter of the current day's wool growth of young ==================
        /// <summary>
        /// Fibre diameter of the current day's wool growth of unweaned young animals by group
        /// </summary>
        [Description("Fibre diameter of the current day's wool growth of unweaned young animals by group")]
        [Units("um")]
        public double[] FibreGrowthDiamYng
        {
            get
            {
                double[] values = new double[FModel.Count()];
                StockVars.PopulateRealValue(FModel, StockProps.prpDAY_FIBRE_DIAM, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Fibre diameter of the current day's wool growth of unweaned young animals total
        /// </summary>
        [Description("Fibre diameter of the current day's wool growth of unweaned young animals total")]
        [Units("um")]
        public double FibreGrowthDiamYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(FModel, StockProps.prpDAY_FIBRE_DIAM, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Fibre diameter of the current day's wool growth of unweaned young animals by tag number
        /// </summary>
        [Description("Fibre diameter of the current day's wool growth of unweaned young animals by tag number")]
        [Units("um")]
        public double[] FibreGrowthDiamYngTag
        {
            get
            {
                double[] values = new double[FModel.iHighestTag()];
                StockVars.PopulateRealValue(FModel, StockProps.prpDAY_FIBRE_DIAM, true, false, true, ref values);
                return values;
            }
        }

        // =========== Weight of milk produced per head, on a 4pc fat-corrected basis ==================
        /// <summary>
        /// Weight of milk produced per head, on a 4pc fat-corrected basis by group
        /// </summary>
        [Description("Weight of milk produced per head, on a 4pc fat-corrected basis by group")]
        [Units("kg/d")]
        public double[] MilkWt
        {
            get
            {
                double[] values = new double[FModel.Count()];
                StockVars.PopulateRealValue(FModel, StockProps.prpMILK_WT, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Weight of milk produced per head, on a 4pc fat-corrected basis total
        /// </summary>
        [Description("Weight of milk produced per head, on a 4pc fat-corrected basis total")]
        [Units("kg/d")]
        public double MilkWtAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(FModel, StockProps.prpMILK_WT, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Weight of milk produced per head, on a 4pc fat-corrected basis by tag number
        /// </summary>
        [Description("Weight of milk produced per head, on a 4pc fat-corrected basis by tag number")]
        [Units("kg/d")]
        public double[] MilkWtTag
        {
            get
            {
                double[] values = new double[FModel.iHighestTag()];
                StockVars.PopulateRealValue(FModel, StockProps.prpMILK_WT, false, false, true, ref values);
                return values;
            }
        }

        // =========== Metabolizable energy produced in milk (per head) by each animal group ==================
        /// <summary>
        /// Metabolizable energy produced in milk (per head) by each animal group by group
        /// </summary>
        [Description("Metabolizable energy produced in milk (per head) by each animal group by group")]
        [Units("MJ/d")]
        public double[] MilkME
        {
            get
            {
                double[] values = new double[FModel.Count()];
                StockVars.PopulateRealValue(FModel, StockProps.prpMILK_ME, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Metabolizable energy produced in milk (per head) by each animal group total
        /// </summary>
        [Description("Metabolizable energy produced in milk (per head) by each animal group total")]
        [Units("MJ/d")]
        public double MilkMEAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(FModel, StockProps.prpMILK_ME, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Metabolizable energy produced in milk (per head) by each animal group by tag number
        /// </summary>
        [Description("Metabolizable energy produced in milk (per head) by each animal group by tag number")]
        [Units("MJ/d")]
        public double[] MilkMETag
        {
            get
            {
                double[] values = new double[FModel.iHighestTag()];
                StockVars.PopulateRealValue(FModel, StockProps.prpMILK_ME, false, false, true, ref values);
                return values;
            }
        }

        // =========== Nitrogen retained within the animals, on a per-head basis ==================
        /// <summary>
        /// Nitrogen retained within the animals, on a per-head basis by group
        /// </summary>
        [Description("Nitrogen retained within the animals, on a per-head basis by group")]
        [Units("kg/d")]
        public double[] RetainedN
        {
            get
            {
                double[] values = new double[FModel.Count()];
                StockVars.PopulateRealValue(FModel, StockProps.prpRETAINED_N, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Nitrogen retained within the animals, on a per-head basis total
        /// </summary>
        [Description("Nitrogen retained within the animals, on a per-head basis total")]
        [Units("kg/d")]
        public double RetainedNAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(FModel, StockProps.prpRETAINED_N, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Nitrogen retained within the animals, on a per-head basis by tag number
        /// </summary>
        [Description("Nitrogen retained within the animals, on a per-head basis by tag number")]
        [Units("kg/d")]
        public double[] RetainedNTag
        {
            get
            {
                double[] values = new double[FModel.iHighestTag()];
                StockVars.PopulateRealValue(FModel, StockProps.prpRETAINED_N, false, false, true, ref values);
                return values;
            }
        }

        // =========== Nitrogen retained within the animals, on a per-head basis of young ==================
        /// <summary>
        /// Nitrogen retained within the animals, on a per-head basis of unweaned young animals by group
        /// </summary>
        [Description("Nitrogen retained within the animals, on a per-head basis of unweaned young animals by group")]
        [Units("kg/d")]
        public double[] RetainedNYng
        {
            get
            {
                double[] values = new double[FModel.Count()];
                StockVars.PopulateRealValue(FModel, StockProps.prpRETAINED_N, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Nitrogen retained within the animals, on a per-head basis of unweaned young animals total
        /// </summary>
        [Description("Nitrogen retained within the animals, on a per-head basis of unweaned young animals total")]
        [Units("kg/d")]
        public double RetainedNYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(FModel, StockProps.prpRETAINED_N, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Nitrogen retained within the animals, on a per-head basis of unweaned young animals by tag number
        /// </summary>
        [Description("Nitrogen retained within the animals, on a per-head basis of unweaned young animals by tag number")]
        [Units("kg/d")]
        public double[] RetainedNYngTag
        {
            get
            {
                double[] values = new double[FModel.iHighestTag()];
                StockVars.PopulateRealValue(FModel, StockProps.prpRETAINED_N, true, false, true, ref values);
                return values;
            }
        }

        //AddScalarSet(ref   Idx, StockProps., "retained_p", TTypedValue.TBaseType.ITYPE_DOUBLE, "", true, "", "");
        // =========== Phosphorus retained within the animals, on a per-head basis ==================
        /// <summary>
        /// Phosphorus retained within the animals, on a per-head basis by group
        /// </summary>
        [Description("Phosphorus retained within the animals, on a per-head basis by group")]
        [Units("kg/d")]
        public double[] RetainedP
        {
            get
            {
                double[] values = new double[FModel.Count()];
                StockVars.PopulateRealValue(FModel, StockProps.prpRETAINED_P, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Phosphorus retained within the animals, on a per-head basis total
        /// </summary>
        [Description("Phosphorus retained within the animals, on a per-head basis total")]
        [Units("kg/d")]
        public double RetainedPAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(FModel, StockProps.prpRETAINED_P, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Phosphorus retained within the animals, on a per-head basis by tag number
        /// </summary>
        [Description("Phosphorus retained within the animals, on a per-head basis by tag number")]
        [Units("kg/d")]
        public double[] RetainedPTag
        {
            get
            {
                double[] values = new double[FModel.iHighestTag()];
                StockVars.PopulateRealValue(FModel, StockProps.prpRETAINED_P, false, false, true, ref values);
                return values;
            }
        }

        // =========== Phosphorus retained within the animals, on a per-head basis of young ==================
        /// <summary>
        /// Phosphorus retained within the animals, on a per-head basis of unweaned young animals by group
        /// </summary>
        [Description("Phosphorus retained within the animals, on a per-head basis of unweaned young animals by group")]
        [Units("kg/d")]
        public double[] RetainedPYng
        {
            get
            {
                double[] values = new double[FModel.Count()];
                StockVars.PopulateRealValue(FModel, StockProps.prpRETAINED_P, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Phosphorus retained within the animals, on a per-head basis of unweaned young animals total
        /// </summary>
        [Description("Phosphorus retained within the animals, on a per-head basis of unweaned young animals total")]
        [Units("kg/d")]
        public double RetainedPYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(FModel, StockProps.prpRETAINED_P, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Phosphorus retained within the animals, on a per-head basis of unweaned young animals by tag number
        /// </summary>
        [Description("Phosphorus retained within the animals, on a per-head basis of unweaned young animals by tag number")]
        [Units("kg/d")]
        public double[] RetainedPYngTag
        {
            get
            {
                double[] values = new double[FModel.iHighestTag()];
                StockVars.PopulateRealValue(FModel, StockProps.prpRETAINED_P, true, false, true, ref values);
                return values;
            }
        }


        // =========== Sulphur retained within the animals, on a per-head basis ==================
        /// <summary>
        /// Sulphur retained within the animals, on a per-head basis by group
        /// </summary>
        [Description("Sulphur retained within the animals, on a per-head basis by group")]
        public double[] RetainedS
        {
            get
            {
                double[] values = new double[FModel.Count()];
                StockVars.PopulateRealValue(FModel, StockProps.prpRETAINED_S, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Sulphur retained within the animals, on a per-head basis total
        /// </summary>
        [Description("Sulphur retained within the animals, on a per-head basis total")]
        [Units("kg/d")]
        public double RetainedSAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(FModel, StockProps.prpRETAINED_S, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Sulphur retained within the animals, on a per-head basis by tag number
        /// </summary>
        [Description("Sulphur retained within the animals, on a per-head basis by tag number")]
        [Units("kg/d")]
        public double[] RetainedSTag
        {
            get
            {
                double[] values = new double[FModel.iHighestTag()];
                StockVars.PopulateRealValue(FModel, StockProps.prpRETAINED_S, false, false, true, ref values);
                return values;
            }
        }

        // =========== Sulphur retained within the animals, on a per-head basis of young ==================
        /// <summary>
        /// Sulphur retained within the animals, on a per-head basis of unweaned young animals by group
        /// </summary>
        [Description("Sulphur retained within the animals, on a per-head basis of unweaned young animals by group")]
        [Units("kg/d")]
        public double[] RetainedSYng
        {
            get
            {
                double[] values = new double[FModel.Count()];
                StockVars.PopulateRealValue(FModel, StockProps.prpRETAINED_S, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Sulphur retained within the animals, on a per-head basis of unweaned young animals total
        /// </summary>
        [Description("Sulphur retained within the animals, on a per-head basis of unweaned young animals total")]
        [Units("kg/d")]
        public double RetainedSYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(FModel, StockProps.prpRETAINED_S, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Sulphur retained within the animals, on a per-head basis of unweaned young animals by tag number
        /// </summary>
        [Description("Sulphur retained within the animals, on a per-head basis of unweaned young animals by tag number")]
        [Units("kg/d")]
        public double[] RetainedSYngTag
        {
            get
            {
                double[] values = new double[FModel.iHighestTag()];
                StockVars.PopulateRealValue(FModel, StockProps.prpRETAINED_S, true, false, true, ref values);
                return values;
            }
        }

        // =========== Faecal dry matter and nutrients per head ==================
        /// <summary>
        /// Faecal dry matter and nutrients per head by each animal group
        /// </summary>
        [Description("Faecal dry matter and nutrients per head by each animal group")]
        public TDMPoolHead[] Faeces
        {
            get
            {
                TDMPoolHead[] pools = new TDMPoolHead[FModel.Count()];
                StockVars.PopulateDMPoolValue(FModel, StockProps.prpFAECES, false, false, false, ref pools);
                return pools;
            }
        }

        /// <summary>
        /// Faecal dry matter and nutrients per head
        /// </summary>
        [Description("Faecal dry matter and nutrients per head")]
        public TDMPoolHead FaecesAll
        {
            get
            {
                TDMPoolHead[] pools = new TDMPoolHead[1];
                StockVars.PopulateDMPoolValue(FModel, StockProps.prpFAECES, false, true, false, ref pools);
                return pools[0];
            }
        }

        /// <summary>
        /// Faecal dry matter and nutrients per head by tag
        /// </summary>
        [Description("Faecal dry matter and nutrients per head by tag")]
        public TDMPoolHead[] FaecesTag
        {
            get
            {
                TDMPoolHead[] pools = new TDMPoolHead[FModel.iHighestTag()];
                StockVars.PopulateDMPoolValue(FModel, StockProps.prpFAECES, false, false, true, ref pools);
                return pools;
            }
        }

        // =========== Faecal dry matter and nutrients per head of unweaned animals ==================
        /// <summary>
        /// Faecal dry matter and nutrients per head of unweaned animals by group
        /// </summary>
        [Description("Faecal dry matter and nutrients per head of unweaned animals by group")]
        public TDMPoolHead[] FaecesYng
        {
            get
            {
                TDMPoolHead[] pools = new TDMPoolHead[FModel.Count()];
                StockVars.PopulateDMPoolValue(FModel, StockProps.prpFAECES, true, false, false, ref pools);
                return pools;
            }
        }

        /// <summary>
        /// Faecal dry matter and nutrients per head of unweaned animals
        /// </summary>
        [Description("Faecal dry matter and nutrients per head of unweaned animals")]
        public TDMPoolHead FaecesYngAll
        {
            get
            {
                TDMPoolHead[] pools = new TDMPoolHead[1];
                StockVars.PopulateDMPoolValue(FModel, StockProps.prpFAECES, true, true, false, ref pools);
                return pools[0];
            }
        }

        /// <summary>
        /// Faecal dry matter and nutrients per head of unweaned animals by tag
        /// </summary>
        [Description("Faecal dry matter and nutrients per head of unweaned animals by tag")]
        public TDMPoolHead[] FaecesYngTag
        {
            get
            {
                TDMPoolHead[] pools = new TDMPoolHead[FModel.iHighestTag()];
                StockVars.PopulateDMPoolValue(FModel, StockProps.prpFAECES, true, false, true, ref pools);
                return pools;
            }
        }

        // =========== Inorganic nutrients excreted in faeces, per head ==================
        /// <summary>
        /// Inorganic nutrients excreted in faeces, per head by each animal group
        /// </summary>
        [Description("Inorganic nutrients excreted in faeces, per head by each animal group")]
        public TInorgFaeces[] FaecesInorg
        {
            get
            {
                TDMPoolHead[] pools = new TDMPoolHead[FModel.Count()];
                TInorgFaeces[] inorgpools = new TInorgFaeces[pools.Length];
                StockVars.PopulateDMPoolValue(FModel, StockProps.prpINORG_FAECES, false, false, false, ref pools);
                for (int i = 0; i < pools.Length; i++)
                {
                    inorgpools[i].n = pools[i].n;
                    inorgpools[i].p = pools[i].p;
                    inorgpools[i].s = pools[i].s;
                }
                return inorgpools;
            }
        }

        /// <summary>
        /// Inorganic nutrients excreted in faeces, per head
        /// </summary>
        [Description("Inorganic nutrients excreted in faeces, per head")]
        public TInorgFaeces FaecesInorgAll
        {
            get
            {
                TDMPoolHead[] pools = new TDMPoolHead[1];
                TInorgFaeces[] inorgpools = new TInorgFaeces[pools.Length];
                StockVars.PopulateDMPoolValue(FModel, StockProps.prpINORG_FAECES, false, true, false, ref pools);
                inorgpools[0].n = pools[0].n;
                inorgpools[0].p = pools[0].p;
                inorgpools[0].s = pools[0].s;
                return inorgpools[0];
            }
        }

        /// <summary>
        /// Inorganic nutrients excreted in faeces, per head by tag
        /// </summary>
        [Description("Inorganic nutrients excreted in faeces, per head by tag")]
        public TInorgFaeces[] FaecesInorgTag
        {
            get
            {
                TDMPoolHead[] pools = new TDMPoolHead[FModel.iHighestTag()];
                TInorgFaeces[] inorgpools = new TInorgFaeces[pools.Length];
                StockVars.PopulateDMPoolValue(FModel, StockProps.prpINORG_FAECES, false, false, true, ref pools);
                for (int i = 0; i < pools.Length; i++)
                {
                    inorgpools[i].n = pools[i].n;
                    inorgpools[i].p = pools[i].p;
                    inorgpools[i].s = pools[i].s;
                }
                return inorgpools;
            }
        }

        // =========== Inorganic nutrients excreted in faeces, per head of unweaned animals ==================
        /// <summary>
        /// Inorganic nutrients excreted in faeces, per head of unweaned animals by group
        /// </summary>
        [Description("Inorganic nutrients excreted in faeces, per head of unweaned animals by group")]
        public TInorgFaeces[] FaecesInorgYng
        {
            get
            {
                TDMPoolHead[] pools = new TDMPoolHead[FModel.Count()];
                TInorgFaeces[] inorgpools = new TInorgFaeces[pools.Length];
                StockVars.PopulateDMPoolValue(FModel, StockProps.prpINORG_FAECES, true, false, false, ref pools);
                for (int i = 0; i < pools.Length; i++)
                {
                    inorgpools[i].n = pools[i].n;
                    inorgpools[i].p = pools[i].p;
                    inorgpools[i].s = pools[i].s;
                }
                return inorgpools;
            }
        }

        /// <summary>
        /// Inorganic nutrients excreted in faeces, per head of unweaned animals
        /// </summary>
        [Description("Inorganic nutrients excreted in faeces, per head of unweaned animals")]
        public TInorgFaeces FaecesInorgYngAll
        {
            get
            {
                TDMPoolHead[] pools = new TDMPoolHead[1];
                TInorgFaeces[] inorgpools = new TInorgFaeces[pools.Length];
                StockVars.PopulateDMPoolValue(FModel, StockProps.prpINORG_FAECES, true, true, false, ref pools);
                inorgpools[0].n = pools[0].n;
                inorgpools[0].p = pools[0].p;
                inorgpools[0].s = pools[0].s;
                return inorgpools[0];
            }
        }

        /// <summary>
        /// Inorganic nutrients excreted in faeces, per head of unweaned animals by tag
        /// </summary>
        [Description("Inorganic nutrients excreted in faeces, per head of unweaned animals by tag")]
        public TInorgFaeces[] FaecesInorgYngTag
        {
            get
            {
                TDMPoolHead[] pools = new TDMPoolHead[FModel.iHighestTag()];
                TInorgFaeces[] inorgpools = new TInorgFaeces[pools.Length];
                StockVars.PopulateDMPoolValue(FModel, StockProps.prpINORG_FAECES, true, false, true, ref pools);
                for (int i = 0; i < pools.Length; i++)
                {
                    inorgpools[i].n = pools[i].n;
                    inorgpools[i].p = pools[i].p;
                    inorgpools[i].s = pools[i].s;
                }
                return inorgpools;
            }
        }

        // =========== Output of methane (per head) ==================
        /// <summary>
        /// Output of methane (per head) by group
        /// </summary>
        [Description("Output of methane (per head) by group")]
        [Units("kg/d")]
        public double[] Methane
        {
            get
            {
                double[] values = new double[FModel.Count()];
                StockVars.PopulateRealValue(FModel, StockProps.prpCH4_OUTPUT, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Output of methane (per head) total
        /// </summary>
        [Description("Output of methane (per head) total")]
        [Units("kg/d")]
        public double MethaneAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(FModel, StockProps.prpCH4_OUTPUT, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Output of methane (per head) by tag number
        /// </summary>
        [Description("Output of methane (per head) by tag number")]
        [Units("kg/d")]
        public double[] MethaneTag
        {
            get
            {
                double[] values = new double[FModel.iHighestTag()];
                StockVars.PopulateRealValue(FModel, StockProps.prpCH4_OUTPUT, false, false, true, ref values);
                return values;
            }
        }

        // =========== Output of methane (per head) of young ==================
        /// <summary>
        /// Output of methane (per head) of unweaned young animals by group
        /// </summary>
        [Description("Output of methane (per head) of unweaned young animals by group")]
        [Units("kg/d")]
        public double[] MethaneYng
        {
            get
            {
                double[] values = new double[FModel.Count()];
                StockVars.PopulateRealValue(FModel, StockProps.prpCH4_OUTPUT, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Output of methane (per head) of unweaned young animals total
        /// </summary>
        [Description("Output of methane (per head) of unweaned young animals total")]
        [Units("kg/d")]
        public double MethaneYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(FModel, StockProps.prpCH4_OUTPUT, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Output of methane (per head) of unweaned young animals by tag number
        /// </summary>
        [Description("Output of methane (per head) of unweaned young animals by tag number")]
        [Units("kg/d")]
        public double[] MethaneYngTag
        {
            get
            {
                double[] values = new double[FModel.iHighestTag()];
                StockVars.PopulateRealValue(FModel, StockProps.prpCH4_OUTPUT, true, false, true, ref values);
                return values;
            }
        }

        // =========== Urinary nitrogen output per head ==================
        /// <summary>
        /// Urinary nitrogen output per head by group
        /// </summary>
        [Description("Urinary nitrogen output per head by group")]
        [Units("kg/d")]
        public double[] UrineN
        {
            get
            {
                double[] values = new double[FModel.Count()];
                StockVars.PopulateRealValue(FModel, StockProps.prpURINE_N, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Urinary nitrogen output per head total
        /// </summary>
        [Description("Urinary nitrogen output per head total")]
        [Units("kg/d")]
        public double UrineNAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(FModel, StockProps.prpURINE_N, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Urinary nitrogen output per head by tag number
        /// </summary>
        [Description("Urinary nitrogen output per head by tag number")]
        [Units("kg/d")]
        public double[] UrineNTag
        {
            get
            {
                double[] values = new double[FModel.iHighestTag()];
                StockVars.PopulateRealValue(FModel, StockProps.prpURINE_N, false, false, true, ref values);
                return values;
            }
        }

        // =========== Urinary nitrogen output per head of young ==================
        /// <summary>
        /// Urinary nitrogen output per head of unweaned young animals by group
        /// </summary>
        [Description("Urinary nitrogen output per head of unweaned young animals by group")]
        [Units("kg/d")]
        public double[] UrineNYng
        {
            get
            {
                double[] values = new double[FModel.Count()];
                StockVars.PopulateRealValue(FModel, StockProps.prpURINE_N, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Urinary nitrogen output per head of unweaned young animals total
        /// </summary>
        [Description("Urinary nitrogen output per head of unweaned young animals total")]
        [Units("kg/d")]
        public double UrineNYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(FModel, StockProps.prpURINE_N, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Urinary nitrogen output per head of unweaned young animals by tag number
        /// </summary>
        [Description("Urinary nitrogen output per head of unweaned young animals by tag number")]
        [Units("kg/d")]
        public double[] UrineNYngTag
        {
            get
            {
                double[] values = new double[FModel.iHighestTag()];
                StockVars.PopulateRealValue(FModel, StockProps.prpURINE_N, true, false, true, ref values);
                return values;
            }
        }

        // =========== Urinary phosphorus output per head ==================
        /// <summary>
        /// Urinary phosphorus output per head by group
        /// </summary>
        [Description("Urinary phosphorus output per head by group")]
        [Units("kg/d")]
        public double[] UrineP
        {
            get
            {
                double[] values = new double[FModel.Count()];
                StockVars.PopulateRealValue(FModel, StockProps.prpURINE_P, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Urinary phosphorus output per head total
        /// </summary>
        [Description("Urinary phosphorus output per head total")]
        [Units("kg/d")]
        public double UrinePAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(FModel, StockProps.prpURINE_P, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Urinary phosphorus output per head by tag number
        /// </summary>
        [Description("Urinary phosphorus output per head by tag number")]
        [Units("kg/d")]
        public double[] UrinePTag
        {
            get
            {
                double[] values = new double[FModel.iHighestTag()];
                StockVars.PopulateRealValue(FModel, StockProps.prpURINE_P, false, false, true, ref values);
                return values;
            }
        }

        // =========== Urinary phosphorus output per head of young ==================
        /// <summary>
        /// Urinary phosphorus output per head of unweaned young animals by group
        /// </summary>
        [Description("Urinary phosphorus output per head of unweaned young animals by group")]
        [Units("kg/d")]
        public double[] UrinePYng
        {
            get
            {
                double[] values = new double[FModel.Count()];
                StockVars.PopulateRealValue(FModel, StockProps.prpURINE_P, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Urinary phosphorus output per head of unweaned young animals total
        /// </summary>
        [Description("Urinary phosphorus output per head of unweaned young animals total")]
        [Units("kg/d")]
        public double UrinePYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(FModel, StockProps.prpURINE_P, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Urinary phosphorus output per head of unweaned young animals by tag number
        /// </summary>
        [Description("Urinary phosphorus output per head of unweaned young animals by tag number")]
        [Units("kg/d")]
        public double[] UrinePYngTag
        {
            get
            {
                double[] values = new double[FModel.iHighestTag()];
                StockVars.PopulateRealValue(FModel, StockProps.prpURINE_P, true, false, true, ref values);
                return values;
            }
        }

        // =========== Urinary sulphur output per head ==================
        /// <summary>
        /// Urinary sulphur output per head by group
        /// </summary>
        [Description("Urinary sulphur output per head by group")]
        [Units("kg/d")]
        public double[] UrineS
        {
            get
            {
                double[] values = new double[FModel.Count()];
                StockVars.PopulateRealValue(FModel, StockProps.prpURINE_S, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Urinary sulphur output per head total
        /// </summary>
        [Description("Urinary sulphur output per head total")]
        [Units("kg/d")]
        public double UrineSAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(FModel, StockProps.prpURINE_S, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Urinary sulphur output per head by tag number
        /// </summary>
        [Description("Urinary sulphur output per head by tag number")]
        [Units("kg/d")]
        public double[] UrineSTag
        {
            get
            {
                double[] values = new double[FModel.iHighestTag()];
                StockVars.PopulateRealValue(FModel, StockProps.prpURINE_S, false, false, true, ref values);
                return values;
            }
        }

        // =========== Urinary sulphur output per head of young ==================
        /// <summary>
        /// Urinary sulphur output per head of unweaned young animals by group
        /// </summary>
        [Description("Urinary sulphur output per head of unweaned young animals by group")]
        [Units("kg/d")]
        public double[] UrineSYng
        {
            get
            {
                double[] values = new double[FModel.Count()];
                StockVars.PopulateRealValue(FModel, StockProps.prpURINE_S, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Urinary sulphur output per head of unweaned young animals total
        /// </summary>
        [Description("Urinary sulphur output per head of unweaned young animals total")]
        [Units("kg/d")]
        public double UrineSYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(FModel, StockProps.prpURINE_S, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Urinary sulphur output per head of unweaned young animals by tag number
        /// </summary>
        [Description("Urinary sulphur output per head of unweaned young animals by tag number")]
        [Units("kg/d")]
        public double[] UrineSYngTag
        {
            get
            {
                double[] values = new double[FModel.iHighestTag()];
                StockVars.PopulateRealValue(FModel, StockProps.prpURINE_S, true, false, true, ref values);
                return values;
            }
        }

        // =========== Intake per head of rumen-degradable protein ==================
        /// <summary>
        /// Intake per head of rumen-degradable protein by group
        /// </summary>
        [Description("Intake per head of rumen-degradable protein by group")]
        [Units("kg/d")]
        public double[] RDPIntake
        {
            get
            {
                double[] values = new double[FModel.Count()];
                StockVars.PopulateRealValue(FModel, StockProps.prpRDPI, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Intake per head of rumen-degradable protein total
        /// </summary>
        [Description("Intake per head of rumen-degradable protein total")]
        [Units("kg/d")]
        public double RDPIntakeAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(FModel, StockProps.prpRDPI, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Intake per head of rumen-degradable protein by tag number
        /// </summary>
        [Description("Intake per head of rumen-degradable protein by tag number")]
        [Units("kg/d")]
        public double[] RDPIntakeTag
        {
            get
            {
                double[] values = new double[FModel.iHighestTag()];
                StockVars.PopulateRealValue(FModel, StockProps.prpRDPI, false, false, true, ref values);
                return values;
            }
        }

        // =========== Intake per head of rumen-degradable protein of young ==================
        /// <summary>
        /// Intake per head of rumen-degradable protein of unweaned young animals by group
        /// </summary>
        [Description("Intake per head of rumen-degradable protein of unweaned young animals by group")]
        [Units("kg/d")]
        public double[] RDPIntakeYng
        {
            get
            {
                double[] values = new double[FModel.Count()];
                StockVars.PopulateRealValue(FModel, StockProps.prpRDPI, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Intake per head of rumen-degradable protein of unweaned young animals total
        /// </summary>
        [Description("Intake per head of rumen-degradable protein of unweaned young animals total")]
        [Units("kg/d")]
        public double RDPIntakeYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(FModel, StockProps.prpRDPI, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Intake per head of rumen-degradable protein of unweaned young animals by tag number
        /// </summary>
        [Description("Intake per head of rumen-degradable protein of unweaned young animals by tag number")]
        [Units("kg/d")]
        public double[] RDPIntakeYngTag
        {
            get
            {
                double[] values = new double[FModel.iHighestTag()];
                StockVars.PopulateRealValue(FModel, StockProps.prpRDPI, true, false, true, ref values);
                return values;
            }
        }

        // =========== Requirement per head of rumen-degradable protein ==================
        /// <summary>
        /// Requirement per head of rumen-degradable protein by group
        /// </summary>
        [Description("Requirement per head of rumen-degradable protein by group")]
        [Units("kg/d")]
        public double[] RDPReqd
        {
            get
            {
                double[] values = new double[FModel.Count()];
                StockVars.PopulateRealValue(FModel, StockProps.prpRDPR, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Requirement per head of rumen-degradable protein total
        /// </summary>
        [Description("Requirement per head of rumen-degradable protein total")]
        [Units("kg/d")]
        public double RDPReqdAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(FModel, StockProps.prpRDPR, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Requirement per head of rumen-degradable protein by tag number
        /// </summary>
        [Description("Requirement per head of rumen-degradable protein by tag number")]
        [Units("kg/d")]
        public double[] RDPReqdTag
        {
            get
            {
                double[] values = new double[FModel.iHighestTag()];
                StockVars.PopulateRealValue(FModel, StockProps.prpRDPR, false, false, true, ref values);
                return values;
            }
        }

        // =========== Requirement per head of rumen-degradable protein of young ==================
        /// <summary>
        /// Requirement per head of rumen-degradable protein of unweaned young animals by group
        /// </summary>
        [Description("Requirement per head of rumen-degradable protein of unweaned young animals by group")]
        [Units("kg/d")]
        public double[] RDPReqdYng
        {
            get
            {
                double[] values = new double[FModel.Count()];
                StockVars.PopulateRealValue(FModel, StockProps.prpRDPR, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Requirement per head of rumen-degradable protein of unweaned young animals total
        /// </summary>
        [Description("Requirement per head of rumen-degradable protein of unweaned young animals total")]
        [Units("kg/d")]
        public double RDPReqdYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(FModel, StockProps.prpRDPR, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Requirement per head of rumen-degradable protein of unweaned young animals by tag number
        /// </summary>
        [Description("Requirement per head of rumen-degradable protein of unweaned young animals by tag number")]
        [Units("kg/d")]
        public double[] RDPReqdYngTag
        {
            get
            {
                double[] values = new double[FModel.iHighestTag()];
                StockVars.PopulateRealValue(FModel, StockProps.prpRDPR, true, false, true, ref values);
                return values;
            }
        }

        // =========== Effect of rumen-degradable protein availability on rate of intake  ==================
        /// <summary>
        /// Effect of rumen-degradable protein availability on rate of intake (1 = no limitation to due lack of RDP) by group
        /// </summary>
        [Description("Effect of rumen-degradable protein availability on rate of intake (1 = no limitation to due lack of RDP) by group")]
        public double[] RDPFactor
        {
            get
            {
                double[] values = new double[FModel.Count()];
                StockVars.PopulateRealValue(FModel, StockProps.prpRDP_EFFECT, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Effect of rumen-degradable protein availability on rate of intake (1 = no limitation to due lack of RDP) total
        /// </summary>
        [Description("Effect of rumen-degradable protein availability on rate of intake (1 = no limitation to due lack of RDP) total")]
        public double RDPFactorAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(FModel, StockProps.prpRDP_EFFECT, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Effect of rumen-degradable protein availability on rate of intake (1 = no limitation to due lack of RDP) by tag number
        /// </summary>
        [Description("Effect of rumen-degradable protein availability on rate of intake (1 = no limitation to due lack of RDP) by tag number")]
        public double[] RDPFactorTag
        {
            get
            {
                double[] values = new double[FModel.iHighestTag()];
                StockVars.PopulateRealValue(FModel, StockProps.prpRDP_EFFECT, false, false, true, ref values);
                return values;
            }
        }

        // =========== Effect of rumen-degradable protein availability on rate of intake of young ==================
        /// <summary>
        /// Effect of rumen-degradable protein availability on rate of intake (1 = no limitation to due lack of RDP) of unweaned young animals by group
        /// </summary>
        [Description("Effect of rumen-degradable protein availability on rate of intake (1 = no limitation to due lack of RDP) of unweaned young animals by group")]
        public double[] RDPFactorYng
        {
            get
            {
                double[] values = new double[FModel.Count()];
                StockVars.PopulateRealValue(FModel, StockProps.prpRDP_EFFECT, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Effect of rumen-degradable protein availability on rate of intake (1 = no limitation to due lack of RDP) of unweaned young animals total
        /// </summary>
        [Description("Effect of rumen-degradable protein availability on rate of intake (1 = no limitation to due lack of RDP) of unweaned young animals total")]
        public double RDPFactorYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(FModel, StockProps.prpRDP_EFFECT, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Effect of rumen-degradable protein availability on rate of intake (1 = no limitation to due lack of RDP) of unweaned young animals by tag number
        /// </summary>
        [Description("Effect of rumen-degradable protein availability on rate of intake (1 = no limitation to due lack of RDP) of unweaned young animals by tag number")]
        public double[] RDPFactorYngTag
        {
            get
            {
                double[] values = new double[FModel.iHighestTag()];
                StockVars.PopulateRealValue(FModel, StockProps.prpRDP_EFFECT, true, false, true, ref values);
                return values;
            }
        }

        /// <summary>
        /// List of all paddocks identified by the component. In decreasing order of herbage relative intake (computed for the first group of animals in the list)
        /// </summary>
        [Description("List of all paddocks identified by the component. In decreasing order of herbage relative intake (computed for the first group of animals in the list)")]
        public string[] PaddockRank
        {
            get
            {
                string[] ranks = new string[1];
                StockVars.MakePaddockRank(FModel, ref ranks);
                return ranks;
            }
        }

        // =========== Externally-imposed scaling factor for potential intake ==================
        /// <summary>
        /// Externally-imposed scaling factor for potential intake. This property is resettable by group
        /// </summary>
        [Description("Externally-imposed scaling factor for potential intake. This property is resettable by group")]
        public double[] IntakeModifier
        {
            get
            {
                double[] values = new double[FModel.Count()];
                StockVars.PopulateRealValue(FModel, StockProps.prpINTAKE_MOD, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Externally-imposed scaling factor for potential intake. This property is resettable, total
        /// </summary>
        [Description("Externally-imposed scaling factor for potential intake. This property is resettable, total")]
        public double IntakeModifierAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(FModel, StockProps.prpINTAKE_MOD, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Externally-imposed scaling factor for potential intake. This property is resettable by tag number
        /// </summary>
        [Description("Externally-imposed scaling factor for potential intake. This property is resettable by tag number")]
        public double[] IntakeModifierTag
        {
            get
            {
                double[] values = new double[FModel.iHighestTag()];
                StockVars.PopulateRealValue(FModel, StockProps.prpINTAKE_MOD, false, false, true, ref values);
                return values;
            }
        }

        // =========== Externally-imposed scaling factor for potential intake of young ==================
        /// <summary>
        /// Externally-imposed scaling factor for potential intake. This property is resettable, of unweaned young animals by group
        /// </summary>
        [Description("Externally-imposed scaling factor for potential intake. This property is resettable, of unweaned young animals by group")]
        public double[] IntakeModifierYng
        {
            get
            {
                double[] values = new double[FModel.Count()];
                StockVars.PopulateRealValue(FModel, StockProps.prpINTAKE_MOD, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Externally-imposed scaling factor for potential intake. This property is resettable, of unweaned young animals total
        /// </summary>
        [Description("Externally-imposed scaling factor for potential intake. This property is resettable, of unweaned young animals total")]
        public double IntakeModifierYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(FModel, StockProps.prpINTAKE_MOD, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Externally-imposed scaling factor for potential intake. This property is resettable, of unweaned young animals by tag number
        /// </summary>
        [Description("Externally-imposed scaling factor for potential intake. This property is resettable, of unweaned young animals by tag number")]
        public double[] IntakeModifierYngTag
        {
            get
            {
                double[] values = new double[FModel.iHighestTag()];
                StockVars.PopulateRealValue(FModel, StockProps.prpINTAKE_MOD, true, false, true, ref values);
                return values;
            }
        }

        #endregion

        #region Subscribed events ====================================================
        /// <summary>
        /// 
        /// </summary>
        [EventSubscribe("Sort")]
        public void OnSort()
        {
            TStockSort anEvent = new TStockSort();
            FModel.doStockManagement(FModel, (IStockEvent)anEvent);
        }

        /// <summary>
        /// Initialisation step
        /// </summary>
        [EventSubscribe("InitStep")]
        public void InitStep()
        {
            /*
            if (!FFirstStep)
                        iCondition = 1;
                    else
                    {
                        sendQueryInfo("daylength", TypeSpec.KIND_OWNED, eventID);
                        if (!FPaddocksGiven)                                                    // Paddock list not specified - query    
                            sendQueryInfo("area", TypeSpec.KIND_OWNED, eventID);                //   the simulation                      
                    }
            */
        }

        #endregion
    }
}