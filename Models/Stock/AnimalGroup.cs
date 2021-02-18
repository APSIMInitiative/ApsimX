namespace Models.GrazPlan
{
    using APSIM.Shared.Utilities;
    using Models.Interfaces;
    using Models.Core;
    using StdUnits;
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    /// <summary>
    /// AnimalGroup class
    /// </summary>
    [Serializable]
    public class AnimalGroup
    {
        /// <summary>
        /// AnimalsDynamicGlb differentiates between the "static" version of the      
        /// model used in GrazFeed and the "dynamic" version used elsewhere           
        /// </summary>
        private const bool animalsDynamicGlb = true;
       
        /// <summary>
        /// 
        /// </summary>
        private const int latePregLength = 42;
        
        /// <summary>
        /// Depth of wool left after shearing (cm)
        /// </summary>
        private const double stubble_mm = 0.5;

        /// <summary>The weather model.</summary>
        [NonSerialized]
        private IWeather weather;

        /// <summary>The clock model.</summary>
        [NonSerialized]
        private Clock clock;

        /// <summary>The parent stock list</summary>
        [NonSerialized]
        private StockList stockList = null;

        /// <summary>
        /// Paramters of the animal mated to
        /// </summary>
        private Genotype matedToGenotypeParameters;
        
        /// <summary>
        /// Distribution of ages
        /// </summary>
        private AgeList ages;

        /// <summary>
        /// All weights in kg
        /// </summary>
        private double totalWeight;
        
        /// <summary>
        /// Greasy fleece weight (including stubble)
        /// </summary>
        private double woolWt;

        /// <summary>
        /// Lactation status
        /// </summary>
        private GrazType.LactType lactStatus;
        
        /// <summary>
        /// Number of foetuses
        /// </summary>
        private int numberFoetuses;
        
        /// <summary>
        /// Number of offspring
        /// </summary>
        private int numberOffspring;
        
        /// <summary>
        /// Previous offspring
        /// </summary>
        private int previousOffspring;

        /// <summary>
        /// The mothers animal group
        /// </summary>
        private AnimalGroup mothers;

        /// <summary>
        /// Day in the mating cycle; -1 if not mating
        /// </summary>
        private int mateCycle;
        
        /// <summary>
        /// Days left in joining period
        /// </summary>
        private int daysToMate;
        
        /// <summary>
        /// Days since conception
        /// </summary>
        private int foetalAge;

        /// <summary>
        /// Base weight 42 days before parturition   
        /// </summary>
        private double midLatePregWeight;

        /// <summary>
        /// Highest previous weight (kg)
        /// </summary>
        private double maxPrevWeight;
        
        /// <summary>
        /// Hair or fleece depth (cm)
        /// </summary>
        private double coatDepth;

        /// <summary>
        /// Phosphorus in base weight (kg)
        /// </summary>
        private double basePhosphorusWeight;
        
        /// <summary>
        /// Sulphur in base weight (kg)
        /// </summary>
        private double baseSulphurWeight;

        /// <summary>
        /// Weight of these animals at birth (kg)
        /// </summary>
        private double birthWeight;
        
        /// <summary>
        /// Normal weight (kg)
        /// </summary>
        private double normalWeight;
        
        /// <summary>
        /// Days since parturition (if lactating)
        /// </summary>
        private int daysLactating;

        /// <summary>
        /// 
        /// </summary>
        private double milkPhosphorusProduction;
        
        /// <summary>
        /// 
        /// </summary>
        private double milkSulphurProduction;

        /// <summary>
        /// Proportion of potential milk production  
        /// </summary>
        private double proportionOfMaxMilk;
        
        /// <summary>
        /// Scales max. intake etc for underweight in lactating animals  
        /// </summary>
        private double lactationAdjustment;
        
        /// <summary>
        /// 
        /// </summary>
        private double lactationRatio;

        /// <summary>
        /// 
        /// </summary>
        private double dryOffTime;

        /// <summary>
        /// 
        /// </summary>
        private double feedingLevel;
        
        /// <summary>
        /// 
        /// </summary>
        private double startFU;
        
        /// <summary>
        /// Fraction of base weight gain from solid intake. 
        /// </summary>
        private double baseWeightGainSolid;

        /// <summary>
        /// 
        /// </summary>
        private double[] netSupplementDMI;
        
        /// <summary>
        /// Sub time step value
        /// </summary>
        private double[] timeStepNetSupplementDMI;
        
        /// <summary>
        /// Chill index
        /// </summary>
        private double chillIndex;
        
        /// <summary>
        /// 
        /// </summary>
        private double implantEffect;

        /// <summary>
        /// Output at this step
        /// </summary>
        private AnimalOutput timeStepState;

        /// <summary>
        /// ptr to the hosts random number factory
        /// </summary>
        private MyRandom randFactory;

        // ------------------ Constructors ------------------

        /// <summary>
        /// Animal group constructor
        /// </summary>
        /// <param name="Params"></param>
        /// <param name="Repro"></param>
        /// <param name="Number"></param>
        /// <param name="AgeD"></param>
        /// <param name="LiveWt"></param>
        /// <param name="GFW"></param>
        /// <param name="RandomFactory"></param>
        /// <param name="clockModel">The clock model.</param>
        /// <param name="weatherModel">The weather model.</param>
        /// <param name="stockListModel">The stock list model.</param>
        /// <param name="bTakeParams"></param>
        public AnimalGroup(Genotype Params,
                           GrazType.ReproType Repro,
                           int Number,
                           int AgeD,
                           double LiveWt,
                           double GFW,                   // NB this is a *fleece* weight             
                           MyRandom RandomFactory,
                           Clock clockModel,
                           IWeather weatherModel,
                           StockList stockListModel,
                           bool bTakeParams = false)
        {
            clock = clockModel;
            weather = weatherModel;
            stockList = stockListModel;

            for (int i = 0; i < 2; i++)
                this.PastIntakeRate[i] = new GrazType.GrazingOutputs();

            Construct(Params, Repro, Number, AgeD, LiveWt, GFW, RandomFactory, bTakeParams);
        }

        /// <summary>
        /// AnimalGroup constructor for creating young.
        /// </summary>
        /// <param name="Parents"></param>
        /// <param name="LiveWt"></param>
        /// <param name="clockModel">The clock model.</param>
        /// <param name="weatherModel">The weather model.</param>
        public AnimalGroup(AnimalGroup Parents, double LiveWt,
                           Clock clockModel,
                           IWeather weatherModel)
        {
            clock = clockModel;
            weather = weatherModel;
            int Number, iAgeDays;
            double YoungWoolWt;
            Genotype youngParams;

            randFactory = Parents.randFactory;
            youngParams = Parents.ConstructOffspringParams();
            Number = Parents.NoOffspring * Parents.FemaleNo;
            iAgeDays = Parents.daysLactating;
            YoungWoolWt = 0.5 * (StockUtilities.DefaultFleece(Parents.Genotype, iAgeDays, GrazType.ReproType.Male, iAgeDays)
                                  + StockUtilities.DefaultFleece(Parents.Genotype, iAgeDays, GrazType.ReproType.Empty, iAgeDays));

            Construct(youngParams, GrazType.ReproType.Male, Number, iAgeDays, LiveWt, YoungWoolWt, randFactory, true);

            MaleNo = Number / 2;
            FemaleNo = Number - MaleNo;

            ages = null;
            ages = new AgeList(randFactory);
            ages.Input(iAgeDays, MaleNo, FemaleNo);

            lactStatus = GrazType.LactType.Suckling;
            numberOffspring = Parents.NoOffspring;
            mothers = Parents;

            for (int i = 0; i < 2; i++)
                this.PastIntakeRate[i] = new GrazType.GrazingOutputs();

            ComputeSRW();                                                              // Must do this after assigning a value to Mothers  
            CalculateWeights();
        }

        /// <summary>
        /// Represents no difference
        /// </summary>
        public DifferenceRecord NODIFF { get; set; } = new DifferenceRecord() { StdRefWt = 0, BaseWeight = 0, FleeceWt = 0 };

        // ------------------ Initialisation properties and outputs ------------------

        /// <summary>
        /// The animals genotype
        /// </summary>
        [Units("-")]
        public Genotype Genotype { get; private set; }

        /// <summary>
        /// Number of animals in the group
        /// </summary>
        [Units("-")]
        public int NoAnimals
        {
            get { return GetNoAnimals(); }
            set { SetNoAnimals(value); }
        }

        /// <summary>
        /// Gets or sets the number of males
        /// </summary>
        [Units("-")]
        public int MaleNo { get; set; }

        /// <summary>
        /// Gets or sets the number of females
        /// </summary>
        [Units("-")]
        public int FemaleNo { get; set; }

        /// <summary>
        /// Gets or sets the mean age of the group
        /// </summary>
        [Units("d")]
        public int AgeDays { get; set; }

        /// <summary>
        /// Standard reference weight of the group
        /// </summary>
        [Units("kg")]
        public double standardReferenceWeight { get; set; }

        /// <summary>
        /// Gets or sets the live weight of the group
        /// </summary>
        [Units("kg")]
        public double LiveWeight
        {
            get { return totalWeight; }
            set { SetLiveWt(value); }
        }

        /// <summary>
        /// Gets or sets the animal base weight
        /// </summary>
        [Units("kg")]
        public double BaseWeight { get; set; }

        /// <summary>
        /// Gets or sets the fleece-free, conceptus-free weight, but including the wool stubble        
        /// </summary>
        [Units("kg")]
        public double EmptyShornWeight
        {
            get { return BaseWeight + CoatDepth2Wool(stubble_mm); }
            set
            {
                BaseWeight = value - CoatDepth2Wool(stubble_mm);
                CalculateWeights();
            }
        }

        /// <summary>
        /// Gets or sets the cut weight of fleece
        /// </summary>
        [Units("kg")]
        public double FleeceCutWeight
        {
            get { return GetFleeceCutWt(); }
            set { SetFleeceCutWt(value); }
        }

        /// <summary>
        /// Gets or sets the wool weight
        /// </summary>
        [Units("kg")]
        public double WoolWeight
        {
            get { return woolWt; }
            set { SetWoolWt(value); }
        }

        /// <summary>
        /// Gets or sets the depth of coat
        /// </summary>
        [Units("cm")]
        public double CoatDepth
        {
            get { return coatDepth; }
            set { SetCoatDepth(value); }
        }

        /// <summary>
        /// Gets or sets the maximum previous weight
        /// </summary>
        [Units("kg")]
        public double MaxPrevWeight
        {
            get { return maxPrevWeight; }
            set { SetMaxPrevWt(value); }
        }

        /// <summary>
        /// Gets or sets the wool fibre diameter
        /// </summary>
        [Units("um")]
        public double FibreDiam { get; set; }

        /// <summary>
        /// Gets or sets the animal parameters for the animal mated to
        /// </summary>
        [Units("-")]
        public Genotype MatedTo
        {
            get { return matedToGenotypeParameters; }
            set { SetMatedTo(value); }
        }

        /// <summary>
        /// Gets or sets the stage of pregnancy. Days since conception. 
        /// </summary>
        [Units("d")]
        public int Pregnancy
        {
            get { return foetalAge; }
            set { SetPregnancy(value); }
        }

        /// <summary>
        /// Gets or sets the days lactating
        /// </summary>
        [Units("d")]
        public int Lactation
        {
            get { return daysLactating; }
            set { SetLactation(value); }
        }

        /// <summary>
        /// Gets or sets the number of foetuses
        /// </summary>
        [Units("-")]
        public int NoFoetuses
        {
            get { return numberFoetuses; }
            set { SetNoFoetuses(value); }
        }

        /// <summary>
        /// Gets or sets the number of offspring
        /// </summary>
        [Units("-")]
        public int NoOffspring
        {
            get { return numberOffspring; }
            set { SetNoOffspring(value); }
        }

        /// <summary>
        /// Gets or sets the condition at birth
        /// </summary>
        [Units("-")]
        public double BirthCondition { get; set; }

        /// <summary>
        /// Gets or sets the daily deaths
        /// </summary>
        [Units("-")]
        public int Deaths { get; set; }

        /// <summary>
        /// Pointers to the young of lactating animals, or the mothers of suckling ones
        /// </summary>
        [Units("-")]
        public AnimalGroup Young { get; set; }

        /// <summary>
        /// Animal output
        /// </summary>
        [Units("-")]
        public AnimalOutput AnimalState { get; set; } = new AnimalOutput();

        /// <summary>
        /// Gets or sets the steepness code (1-2) of the paddock 
        /// </summary>
        [Units("1-2")]
        public double PaddSteep { get; set; }

        /// <summary>
        /// Gets or sets the herbage being eaten
        /// </summary>
        [Units("-")]
        public GrazType.GrazingInputs Herbage { get; set; } = new GrazType.GrazingInputs();


        /// <summary>
        /// Organic faeces
        /// </summary>
        [Units("-")]
        public GrazType.DM_Pool OrgFaeces
        {
            get { return this.GetOrgFaeces(); }
        }

        /// <summary>
        /// Gets the inorganic faeces
        /// </summary>
        [Units("-")]
        public GrazType.DM_Pool InOrgFaeces
        {
            get { return this.GetInOrgFaeces(); }
        }

        /// <summary>
        /// Gets the urine value
        /// </summary>
        [Units("-")]
        public GrazType.DM_Pool Urine
        {
            get { return this.GetUrine(); }
        }

        /// <summary>
        /// Gets the excretion information
        /// </summary>
        [Units("-")]
        public ExcretionInfo Excretion
        {
            get { return this.GetExcretion(); }
        }

        /// <summary>
        /// Gets or sets the paddock occupied
        /// </summary>
        [Units("-")]
        public PaddockInfo PaddOccupied { get; set; }

        /// <summary>
        /// Gets or sets the tag number
        /// </summary>
        [Units("-")]
        public int Tag { get; set; }

        /// <summary>
        /// 0=mothers, 1=suckling young
        /// </summary>
        [Units("-")]
        public AnimalStateInfo[] InitState = new AnimalStateInfo[2];

        /// <summary>
        /// RDP factor
        /// </summary>
        [Units("0-1")]
        public double[] RDPFactor = new double[2];      // [0..1] 

        /// <summary>
        /// Index is to forage-within-paddock
        /// </summary>
        [Units("-")]
        public GrazType.GrazingInputs[] InitForageInputs;

        /// <summary>
        /// Forage inputs
        /// </summary>
        [Units("-")]
        public GrazType.GrazingInputs[] StepForageInputs;

        /// <summary>
        /// Paddock grazing inputs
        /// </summary>
        [Units("-")]
        public GrazType.GrazingInputs PaddockInputs;

        /// <summary>
        /// Pasture intake
        /// </summary>
        [Units("-")]
        public GrazType.GrazingOutputs[] PastIntakeRate = new GrazType.GrazingOutputs[2];

        /// <summary>
        /// Supplement intake
        /// </summary>
        [Units("kg/hd/d")]
        public double[] SuppIntakeRate = new double[2];

        // Management events .............................................

        /// <summary>
        ///  Commence joining                                                          
        /// </summary>
        /// <param name="maleParams"></param>
        /// <param name="matingPeriod"></param>
        public void Join(Genotype maleParams, int matingPeriod)
        {
            if ((this.ReproState == GrazType.ReproType.Empty) && (this.AgeDays > this.Genotype.Puberty[0]))
            {
                if (maleParams.Animal != this.Genotype.Animal)
                    throw new Exception("Attempt to mate female " + GrazType.AnimalText[(int)this.Genotype.Animal].ToLower() + " with male " + GrazType.AnimalText[(int)maleParams.Animal].ToLower());

                this.matedToGenotypeParameters = new Genotype(maleParams);
                this.daysToMate = matingPeriod;
                if (this.daysToMate > 0)
                    this.mateCycle = this.Genotype.OvulationPeriod / 2;
                else
                    this.mateCycle = -1;
            }
        }

        /// <summary>
        /// Wean male or female lambs/calves
        /// </summary>
        /// <param name="weanFemales"></param>
        /// <param name="weanMales"></param>
        /// <param name="newGroups"></param>
        /// <param name="weanedOff"></param>
        public void Wean(bool weanFemales, bool weanMales, ref List<AnimalGroup> newGroups, ref List<AnimalGroup> weanedOff)
        {
            int totalYoung;
            int malePropn;
            double femaleDiff;
            DifferenceRecord diffs;
            AnimalGroup maleYoung;
            AnimalGroup femaleYoung;

            if (this.NoAnimals == 0)
            {
                this.Young = null;
                this.Lactation = 0;
            }

            else if ((Young != null) && ((weanMales && (this.Young.MaleNo > 0))
                                       || (weanFemales && (this.Young.FemaleNo > 0))))
            {
                totalYoung = this.Young.NoAnimals;
                malePropn = this.Young.MaleNo / totalYoung;

                if (this.Young.MaleNo == 0)
                {
                    // Divide the male from the female lambs or calves                              
                    femaleYoung = this.Young;
                    maleYoung = null;
                }
                else if (this.Young.FemaleNo == 0)
                {
                    maleYoung = this.Young;
                    femaleYoung = null;
                }
                else
                {
                    // TODO: this code had a nasty With block. It may need testing
                    femaleDiff = StdMath.XDiv(this.Young.FemaleWeight - this.Young.MaleWeight, this.Young.LiveWeight);
                    diffs = new DifferenceRecord() { StdRefWt = this.NODIFF.StdRefWt, BaseWeight = this.NODIFF.BaseWeight, FleeceWt = this.NODIFF.FleeceWt };
                    diffs.BaseWeight = femaleDiff * this.Young.BaseWeight;
                    diffs.FleeceWt = femaleDiff * this.Young.woolWt;
                    diffs.StdRefWt = this.Young.standardReferenceWeight * StdMath.XDiv(this.Young.NoAnimals, this.Genotype.SRWScalars[(int)this.Young.ReproState] * this.Young.MaleNo + this.Young.FemaleNo)
                                                 * (1.0 - this.Genotype.SRWScalars[(int)this.Young.ReproState]);

                    maleYoung = this.Young;
                    femaleYoung = maleYoung.SplitSex(0, this.Young.FemaleNo, false, diffs);
                }
                if (femaleYoung != null)
                    femaleYoung.ReproState = GrazType.ReproType.Empty;

                this.Young = null;                                                                      // Detach weaners from their mothers        
                this.previousOffspring = this.numberOffspring;
                this.numberOffspring = this.previousOffspring;

                if (weanMales)                                                                          // Export the weaned lambs or calves        
                    this.ExportWeaners(ref maleYoung, ref weanedOff);
                if (weanFemales)
                    this.ExportWeaners(ref femaleYoung, ref weanedOff);

                if (!weanMales)                                                                         // Export ewes or cows which still have     
                    this.SplitMothers(ref maleYoung, totalYoung, malePropn, ref newGroups);             // lambs or calves                        
                if (!weanFemales)
                    this.SplitMothers(ref femaleYoung, totalYoung, 1.0 - malePropn, ref newGroups);

                if (this.Genotype.Animal == GrazType.AnimalType.Sheep)                                   // Sheep don't continue lactation           
                    this.SetLactation(0);

                this.numberOffspring = 0;
            } //// _ IF (Young <> NIL) etc _
        }

        /// <summary>
        /// Shear the animals and return the cfw per head
        /// </summary>
        /// <param name="shearAdults">Shear adults?</param>
        /// <param name="shearYoung">Shear lambs?</param>
        /// <returns>CFW per head</returns>
        public double Shear(bool shearAdults, bool shearYoung)
        {
            double CFWHead = 0;
            if (shearAdults)
            {
                double greasyFleece = this.FleeceCutWeight;
                woolWt = this.woolWt - greasyFleece;
                totalWeight = this.totalWeight - greasyFleece;
                CalculateCoatDepth();
                CFWHead = Genotype.WoolC[3] * greasyFleece;
            }
            if (shearYoung && Young != null)
                CFWHead += Young.Shear(true, false);
            return CFWHead;
        }

        /// <summary>
        /// End lactation in cows whose calves have already been weaned               
        /// </summary>
        public void DryOff()
        {
            if ((this.Young == null) && (this.lactStatus == GrazType.LactType.Lactating))
                SetLactation(0);
        }

        /// <summary>
        /// Castrate the animals
        /// </summary>
        public void Castrate()
        {
            if (ReproState == GrazType.ReproType.Male)
            {
                ReproState = GrazType.ReproType.Castrated;
                ComputeSRW();
                CalculateWeights();
            }
        }

        /// <summary>
        /// Move the animal group to a new paddock.
        /// </summary>
        /// <param name="paddockName"></param>
        public void MoveToPaddock(string paddockName)
        {
            var paddock = stockList.Paddocks.Find(p => p.Name == paddockName);
            if (paddock == null)
                throw new Exception($"Cannot find paddock {paddock}");
            PaddOccupied = paddock;
        }

        // Information properties ........................................
        /// <summary>
        /// Gets the animal
        /// </summary>
        [Units("-")]
        public GrazType.AnimalType Animal
        {
            get { return this.GetAnimal(); }
        }

        /// <summary>
        /// Gets the standard reference weight
        /// </summary>
        [Units("kg")]
        public double StdReferenceWt
        {
            get { return this.standardReferenceWeight; }
        }

        /// <summary>
        /// Gets the age class of the animals
        /// </summary>
        [Units("-")]
        public GrazType.AgeType AgeClass
        {
            get { return this.GetAgeClass(); }
        }

        /// <summary>
        /// Gets the reproductive state
        /// </summary>
        [Units("-")]
        public GrazType.ReproType ReproState { get; private set; }

        /// <summary>
        /// Gets the relative size of the animal. Ratio of normal weight to SRW. 0-1.0
        /// </summary>
        [Units("-")]
        public double RelativeSize { get; private set; }

        /// <summary>
        /// Body condition
        /// </summary>
        [Units("-")]
        public double BodyCondition { get; private set; }

        /// <summary>
        /// Gets the weight change
        /// </summary>
        [Units("kg/d")]
        public double WeightChange { get; private set; }

        /// <summary>
        /// Gets the clean fleece weight
        /// </summary>
        [Units("kg")]
        public double CleanFleeceWeight
        {
            get { return this.GetCFW(); }
        }

        /// <summary>
        /// Gets the clean fleece growth
        /// </summary>
        [Units("kg/d")]
        public double CleanFleeceGrowth
        {
            get { return this.GetDeltaCFW(); }
        }

        /// <summary>
        /// Gets the greasy fleece growth
        /// </summary>
        [Units("kg/d")]
        public double GreasyFleeceGrowth { get; private set; }

        /// <summary>
        /// Gets the days fibre diameter
        /// </summary>
        [Units("um")]
        public double DayFibreDiam { get; private set; }

        /// <summary>
        /// Gets the milk yield
        /// </summary>
        [Units("kg")]
        public double MilkYield { get; private set; }

        /// <summary>
        /// Gets the milk volume
        /// </summary>
        [Units("l")]
        public double MilkVolume
        {
            get { return this.GetMilkVolume(); }
        }

        /// <summary>
        /// Gets the milk yield
        /// </summary>
        [Units("kg")]
        public double MaxMilkYield
        {
            get { return this.GetMaxMilkYield(); }
        }

        /// <summary>
        /// Gets the milk energy
        /// </summary>
        [Units("MJ")]
        public double MilkEnergy { get; private set; }

        /// <summary>
        /// Gets the milk protein
        /// </summary>
        [Units("kg/kg")]
        public double MilkProtein { get; private set; }

        /// <summary>
        /// Gets the foetal weight
        /// </summary>
        [Units("kg")]
        public double FoetalWeight { get; private set; }

        /// <summary>
        /// Gets the conceptus weight
        /// </summary>
        [Units("kg")]
        public double ConceptusWeight
        {
            get { return this.ConceptusWt(); }
        }

        /// <summary>
        /// Gets the male weight
        /// </summary>
        [Units("kg")]
        public double MaleWeight
        {
            get { return this.GetMaleWeight(); }
        }

        /// <summary>
        /// Gets the female weight
        /// </summary>
        [Units("kg")]
        public double FemaleWeight
        {
            get { return this.GetFemaleWeight(); }
        }

        /// <summary>
        /// Gets the DSE
        /// </summary>
        [Units("DSE")]
        public double DrySheepEquivs
        {
            get { return this.GetDSEs(); }
        }

        /// <summary>
        /// Gets or sets the potential intake
        /// </summary>
        public double PotIntake { get; set; }

        /// <summary>
        /// Gets the methane weight
        /// </summary>
        [Units("kg")]
        public double MethaneWeight
        {
            get { return this.GetMethaneWeight(); }
        }

        /// <summary>
        /// Gets the fresh weight supplement intake
        /// </summary>
        [Units("kg")]
        public double SupplementFreshWeightIntake { get; private set; }

        /// <summary>
        /// Gets the intake of supplement
        /// </summary>
        [Units("-")]
        public FoodSupplement IntakeSupplement { get; private set; }

        /// <summary>
        /// Gets or sets the waterlogging index (0-1)
        /// </summary>
        [Units("0-1")]
        public double WaterLogging { get; set; }

        /// <summary>
        /// Gets the supplement ration used
        /// </summary>
        [Units("-")]
        public SupplementRation RationFed { get; private set; }

        /// <summary>
        /// Gets or sets the number of animals per hectare
        /// </summary>
        [Units("-")]
        public double AnimalsPerHa { get; set; }

        /// <summary>
        /// Gets or sets the distance walked
        /// </summary>
        [Units("km")]
        public double DistanceWalked { get; set; }

        /// <summary>
        /// Gets or sets the intake modifier scaling factor for potential intake
        /// </summary>
        [Units("0-1")]
        public double IntakeModifier { get; set; }

        // ------------------ Public methods ------------------

        /// <summary>
        /// Copy a AnimalGroup
        /// </summary>
        /// <returns></returns>
        public AnimalGroup Copy()
        {
            AnimalGroup theCopy = ReflectionUtilities.Clone(this) as AnimalGroup;
            theCopy.weather = weather;
            theCopy.clock = clock;
            theCopy.stockList = stockList;
            if (PaddOccupied != null)
            {
                theCopy.PaddOccupied.zone = PaddOccupied.zone;
                theCopy.PaddOccupied.AddFaecesObj = PaddOccupied.AddFaecesObj;
                theCopy.PaddOccupied.AddUrineObj = PaddOccupied.AddUrineObj;
            }
            if (theCopy.Young != null)
            {
                theCopy.Young.clock = clock;
                theCopy.Young.weather = weather;
            }
            theCopy.randFactory = this.randFactory;
            if (this.ages != null)
                theCopy.ages.RandFactory = this.randFactory;
            if (this.Young != null)
            {
                theCopy.Young.randFactory = this.randFactory;
                theCopy.Young.mothers = theCopy;
            }
            return theCopy;
        }

        /// <summary>
        /// Merge two animal groups
        /// </summary>
        /// <param name="otherGrp"></param>
        public void Merge(ref AnimalGroup otherGrp)
        {
            double fWoodFactor;
            double fWoodOther;
            int Total1;
            int Total2;


            if ((NoFoetuses != otherGrp.NoFoetuses)                                   // Necessary conditions for merging         
               || (NoOffspring != otherGrp.NoOffspring)
               || ((mothers == null) && (ReproState != otherGrp.ReproState))
               || (lactStatus != otherGrp.lactStatus))
                throw new Exception("AnimalGroup: Error in Merge method");

            Total1 = NoAnimals;
            Total2 = otherGrp.NoAnimals;

            MaleNo += otherGrp.MaleNo;                                       // Take weighted averages of all            
            FemaleNo += otherGrp.FemaleNo;                                     // appropriate fields                       
            ages.Merge(otherGrp.ages);
            AgeDays = ages.MeanAge();

            totalWeight = AverageField(Total1, Total2, totalWeight, otherGrp.totalWeight);
            woolWt = AverageField(Total1, Total2, woolWt, otherGrp.woolWt);
            GreasyFleeceGrowth = AverageField(Total1, Total2, GreasyFleeceGrowth, otherGrp.GreasyFleeceGrowth);
            FibreDiam = AverageField(Total1, Total2, FibreDiam, otherGrp.FibreDiam);
            coatDepth = AverageField(Total1, Total2, coatDepth, otherGrp.coatDepth);
            BaseWeight = AverageField(Total1, Total2, BaseWeight, otherGrp.BaseWeight);
            WeightChange = AverageField(Total1, Total2, WeightChange, otherGrp.WeightChange);
            maxPrevWeight = AverageField(Total1, Total2, maxPrevWeight, otherGrp.maxPrevWeight);
            birthWeight = AverageField(Total1, Total2, birthWeight, otherGrp.birthWeight);
            standardReferenceWeight = AverageField(Total1, Total2, standardReferenceWeight, otherGrp.standardReferenceWeight);
            PotIntake = AverageField(Total1, Total2, PotIntake, otherGrp.PotIntake);
            CalculateWeights();

            if ((ReproState == GrazType.ReproType.EarlyPreg) || (ReproState == GrazType.ReproType.LatePreg))
            {
                foetalAge = (foetalAge * Total1 + otherGrp.foetalAge * Total2)
                             / (Total1 + Total2);
                FoetalWeight = AverageField(Total1, Total2, FoetalWeight, otherGrp.FoetalWeight);
                midLatePregWeight = AverageField(Total1, Total2, midLatePregWeight, otherGrp.midLatePregWeight);
            }

            if (lactStatus == GrazType.LactType.Lactating)
            {
                daysLactating = (daysLactating * Total1 + otherGrp.daysLactating * Total2)
                                 / (Total1 + Total2);
                MilkEnergy = AverageField(Total1, Total2, MilkEnergy, otherGrp.MilkEnergy);
                MilkProtein = AverageField(Total1, Total2, MilkProtein, otherGrp.MilkProtein);
                MilkYield = AverageField(Total1, Total2, MilkYield, otherGrp.MilkYield);
                lactationRatio = AverageField(Total1, Total2, lactationRatio, otherGrp.lactationRatio);
            }
            else if ((previousOffspring == 0) && (otherGrp.previousOffspring == 0))
            {
                previousOffspring = 0;
                dryOffTime = 0;
                BirthCondition = 0.0;
                otherGrp.BirthCondition = 0.0;
            }
            else
            {
                if ((previousOffspring == 0)
                   || ((otherGrp.previousOffspring > 0) && (otherGrp.FemaleNo > FemaleNo)))
                    previousOffspring = otherGrp.previousOffspring;

                fWoodFactor = WOOD(dryOffTime, Genotype.IntakeC[8], Genotype.IntakeC[9]);
                fWoodOther = WOOD(otherGrp.dryOffTime, Genotype.IntakeC[8], Genotype.IntakeC[9]);
                fWoodFactor = AverageField(Total1, Total2, fWoodFactor, fWoodOther);
                dryOffTime = InverseWOOD(fWoodFactor, Genotype.IntakeC[8], Genotype.IntakeC[9], true);

                if (BirthCondition == 0.0)
                    BirthCondition = 1.0;
                if (otherGrp.BirthCondition == 0.0)
                    otherGrp.BirthCondition = 1.0;
            }
            BirthCondition = AverageField(Total1, Total2, BirthCondition, otherGrp.BirthCondition);
            proportionOfMaxMilk = AverageField(Total1, Total2, proportionOfMaxMilk, otherGrp.proportionOfMaxMilk);
            lactationAdjustment = AverageField(Total1, Total2, lactationAdjustment, otherGrp.lactationAdjustment);

            if (Young != null)
            {
                var y = otherGrp.Young;
                Young.Merge(ref y);
            }
            otherGrp = null;
        }

        /// <summary>
        /// Split the animal group
        /// </summary>
        /// <param name="number"></param>
        /// <param name="byAge"></param>
        /// <param name="diffs"></param>
        /// <param name="yngDiffs"></param>
        /// <returns>Animal group</returns>
        public AnimalGroup Split(int number, bool byAge, DifferenceRecord diffs, DifferenceRecord yngDiffs)
        {
            AnimalGroup Result;
            int SplitM, SplitF;
            string msg = string.Empty;

            if ((number < 0) || (number > NoAnimals))
            {
                if (number < 0)
                    msg = "Number of animals to split off should be > 0";
                if (number > NoAnimals)
                    msg = "Trying to split off more than " + NoAnimals.ToString() + " animals that exist in the " + GrazType.AgeText[(int)this.AgeClass] + " age class";
                throw new Exception("AnimalGroup: Error in Split method: " + msg);
            }

            if (mothers != null)
            {
                SplitM = Convert.ToInt32(Math.Round(StdMath.XDiv(number * 1.0 * MaleNo, NoAnimals)), CultureInfo.InvariantCulture);
                SplitF = number - SplitM;
            }
            else if ((ReproState == GrazType.ReproType.Male) || (ReproState == GrazType.ReproType.Castrated))
            {
                SplitM = number;
                SplitF = 0;
            }
            else
            {
                SplitF = number;
                SplitM = 0;
            }

            Result = SplitSex(SplitM, SplitF, byAge, diffs);
            if (Young != null)
            {
                Result.Young = Young.Split(number * NoOffspring, false, yngDiffs, NODIFF);
                Result.Young.mothers = Result.Copy();
            }
            return Result;
        }

        /// <summary>
        /// Split young
        /// </summary>
        public List<AnimalGroup> SplitYoung()
        {
            int numToSplit;
            var newGroups = new List<AnimalGroup>();

            if (this.Young != null)
            {
                if (NoOffspring == 1)
                {
                    numToSplit = this.Young.FemaleNo;
                    this.SplitNumbers(ref newGroups, numToSplit, 0, numToSplit);
                }
                else if (this.NoOffspring == 2)
                {
                    numToSplit = Convert.ToInt32(Math.Min(this.Young.MaleNo, this.Young.FemaleNo), CultureInfo.InvariantCulture) / 2;   // One male, one female                     
                    if (((this.Young.FemaleNo - numToSplit) % 2) != 0)  //if odd                                                        // Ensures Young.FemaleNo (and hence        
                        numToSplit++;                                                                                                   // Young.MaleNo) is even after the call   
                    this.SplitNumbers(ref newGroups, numToSplit, numToSplit, numToSplit);                                               // to SplitBySex                          
                    numToSplit = this.Young.FemaleNo / 2;                                                                               // Twin females                             
                    this.SplitNumbers(ref newGroups, numToSplit, 0, 2 * numToSplit);
                }
            }
            return newGroups;
        }

        /// <summary>
        /// Is an animal group similar enough to another for them to be merged?       
        /// </summary>
        /// <param name="animalGrp">An animal group</param>
        /// <returns></returns>
        public bool Similar(AnimalGroup animalGrp)
        {
            bool Result = ((Genotype.Name == animalGrp.Genotype.Name)
                  && (ReproState == animalGrp.ReproState)
                  && (NoFoetuses == animalGrp.NoFoetuses)
                  && (NoOffspring == animalGrp.NoOffspring)
                  && (mateCycle == animalGrp.mateCycle)
                  && (daysToMate == animalGrp.daysToMate)
                  && (Pregnancy == animalGrp.Pregnancy)
                  && (lactStatus == animalGrp.lactStatus)
                  && (Math.Abs(Lactation - animalGrp.Lactation) < 7)
                  && ((Young == null) == (animalGrp.Young == null))
                  && (implantEffect == animalGrp.implantEffect));
            if (AgeDays < 365)
                Result = (Result && (AgeDays == animalGrp.AgeDays));
            else
                Result = (Result && (Math.Min(AgeDays / 30, 37) == Math.Min(animalGrp.AgeDays / 30, 37)));
            if (Young != null)
                Result = (Result && (Young.ReproState == animalGrp.Young.ReproState));

            return Result;
        }

        /// <summary>
        /// Condition score
        /// </summary>
        /// <param name="system"></param>
        /// <returns></returns>
        public double ConditionScore(StockUtilities.Cond_System system) { return StockUtilities.Condition2CondScore(BodyCondition, system); }

        /// <summary>
        /// Set the condition score
        /// </summary>
        /// <param name="value"></param>
        /// <param name="system"></param>
        public void SetConditionScore(double value, StockUtilities.Cond_System system)
        {
            BaseWeight = normalWeight * StockUtilities.CondScore2Condition(value, system);
            CalculateWeights();
        }

        /// <summary>
        /// Sets the value of MaxPrevWeight using current base weight, age and a      
        /// (relative) body condition. Intended for use with young animals.           
        /// </summary>
        /// <param name="bodyCondition"></param>
        public void SetConditionAtWeight(double bodyCondition)
        {
            double fMaxNormWt;
            double fNewMaxPrevWt;

            fMaxNormWt = MaxNormWtFunc(standardReferenceWeight, birthWeight, AgeDays, Genotype);
            if (BaseWeight >= fMaxNormWt)
                fNewMaxPrevWt = BaseWeight;
            else
            {
                fNewMaxPrevWt = (BaseWeight - bodyCondition * Genotype.GrowthC[3] * fMaxNormWt)
                                 / (bodyCondition * (1.0 - Genotype.GrowthC[3]));
                fNewMaxPrevWt = Math.Max(BaseWeight, Math.Min(fNewMaxPrevWt, fMaxNormWt));
            }

            SetMaxPrevWt(fNewMaxPrevWt);
        }

        /// <summary>
        /// Age the animals
        /// </summary>
        /// <param name="numDays"></param>
        /// <param name="newGroups"></param>
        public void Age(int numDays, ref List<AnimalGroup> newGroups)
        {
            int newOffset, i;

            if (chillIndex == StdMath.DMISSING)
                chillIndex = ChillFunc(weather.MeanT, weather.Wind, weather.Rain);
            else
                chillIndex = 16.0 / 17.0 * chillIndex + 1.0 / 17.0 * ChillFunc(weather.MeanT, weather.Wind, weather.Rain);

            if (newGroups != null)
                newOffset = newGroups.Count;
            else
                newOffset = 0;
            if (mothers == null)                                                    // Deaths must be done before age is        
                Kill(chillIndex, ref newGroups);                                          //   incremented                           

            if (YoungStopSuckling())
                Lactation = 0;

            AdvanceAge(this, numDays, ref newGroups);
            if (newGroups != null)
                for (i = newOffset; i <= newGroups.Count - 1; i++)
                    AdvanceAge(newGroups[i], numDays, ref newGroups);

            switch (ReproState)
            {
                case GrazType.ReproType.Empty:
                    if (mateCycle >= 0)
                    {
                        daysToMate--;
                        if (daysToMate <= 0)
                            mateCycle = -1;
                        else
                            mateCycle = (mateCycle + 1) % Genotype.OvulationPeriod;
                        if (mateCycle == 0)
                            Conceive(ref newGroups);
                    }
                    break;
                case GrazType.ReproType.EarlyPreg:
                    foetalAge++;
                    if (foetalAge >= Genotype.Gestation - latePregLength)
                        ReproState = GrazType.ReproType.LatePreg;
                    break;
                case GrazType.ReproType.LatePreg:
                    foetalAge++;
                    if (animalsDynamicGlb)
                        if ((Animal == GrazType.AnimalType.Sheep) && (foetalAge == Genotype.Gestation - latePregLength / 2))
                            midLatePregWeight = BaseWeight;
                        else if (foetalAge == Genotype.Gestation - 1)
                            KillEndPreg(ref newGroups);
                        else if (foetalAge >= Genotype.Gestation)
                        {
                            Lactation = 1;                                // Create lambs or calves                   
                            NoOffspring = NoFoetuses;
                            Young.BaseWeight = FoetalWeight - Young.woolWt;
                            Young.maxPrevWeight = Young.BaseWeight;
                            Pregnancy = 0;                                // End pregnancy                            
                        }
                    break;
            }
        }

        /// <summary>
        /// Routine to compute the potential intake of a group of animals.  The       
        /// result is stored as TheAnimals.IntakeLimit.  A variety of other fields   
        /// of TheAnimals are also updated: the normal weight, mature normal weight, 
        /// highest previous weight (in young animals), relative size and relative    
        /// condition.                                                                
        /// </summary>
        public void CalculateIntakeLimit()
        {
            double lactTime;                                                               // Scaled days since birth of young         
            double weightLoss;                                                              // Mean daily weight loss during lactation  
                                                                                            // as a proportion of SRW                 
            double criticalLoss;                                                            // Threshold value of WeightLoss            
            double tempDiff;
            double tempAmpl;
            double belowLCT;
            double X;
            double condFactor;
            double youngFactor;
            double heatFactor;
            double lactFactor;
            int lactNum;
            double shapeParam;

            CalculateWeights();                                                             // Compute size and condition               

            if (BodyCondition > 1.0)  // and (LactStatus <> Lactating) then  // No longer exclude lactating females. See bug#2223 
                condFactor = BodyCondition * (this.Genotype.IntakeC[20] - BodyCondition) / (this.Genotype.IntakeC[20] - 1.0);
            else
                condFactor = 1.0;

            if (this.lactStatus == GrazType.LactType.Suckling)
                youngFactor = (1.0 - mothers.proportionOfMaxMilk)
                                / (1.0 + Math.Exp(-this.Genotype.IntakeC[3] * (AgeDays - this.Genotype.IntakeC[4])));
            else
                youngFactor = 1.0;

            if (this.weather.MinT < this.AnimalState.LowerCritTemp)
            {
                // Integrate sinusoidal temperature function over the part below LCT       
                tempDiff = this.weather.MeanT - this.AnimalState.LowerCritTemp;
                tempAmpl = 0.5 * (this.weather.MaxT - this.weather.MinT);
                X = Math.Acos(Math.Max(-1.0, Math.Min(1.0, tempDiff / tempAmpl)));
                belowLCT = (-tempDiff * X + tempAmpl * Math.Sin(X)) / Math.PI;
                heatFactor = 1.0 + this.Genotype.IntakeC[17] * belowLCT * StdMath.DIM(1.0, this.weather.Rain / this.Genotype.IntakeC[18]);
            }
            else
                heatFactor = 1.0;

            if (this.weather.MinT >= this.Genotype.IntakeC[7])                                 // High temperatures depress intake         
                heatFactor = heatFactor * (1.0 - this.Genotype.IntakeC[5] * StdMath.DIM(this.weather.MeanT, this.Genotype.IntakeC[6]));
            if (this.lactStatus != GrazType.LactType.Lactating)
                this.lactationAdjustment = 1.0;
#pragma warning disable 162 // unreachable code
            else if (!animalsDynamicGlb)                                                        // In the dynamic model, LactAdjust is a    
            {                                                                                   // state variable computed in the         
                weightLoss = RelativeSize * StdMath.XDiv(BirthCondition - BodyCondition,               // lactation routine; for GrazFeed, it is 
                                             daysLactating);                                    // estimated with these equations         
                criticalLoss = Genotype.IntakeC[14] * Math.Exp(-Math.Pow(Genotype.IntakeC[13] * daysLactating, 2));
                if (weightLoss > criticalLoss)
                    this.lactationAdjustment = (1.0 - Genotype.IntakeC[12] * weightLoss / Genotype.IntakeC[13]);
                else
                    this.lactationAdjustment = 1.0;
            }
#pragma warning restore 162
            if (this.lactStatus == GrazType.LactType.Lactating)
            {
                lactTime = this.daysLactating;
                lactNum = this.NoSuckling();
            }
            else
            {
                lactTime = this.dryOffTime;
                lactNum = this.previousOffspring;
            }

            if (((this.ReproState == GrazType.ReproType.Male || this.ReproState == GrazType.ReproType.Castrated)) || (this.mothers != null))
                lactFactor = 1.0;
            else
            {
                if (this.NoSuckling() > 0)
                {
                    shapeParam = Genotype.IntakeC[9];
                }
                else
                {
                    shapeParam = Genotype.IntakeC[21];
                }
                lactFactor = 1.0 + this.Genotype.IntakeLactC[lactNum]
                                     * ((1.0 - this.Genotype.IntakeC[15]) + this.Genotype.IntakeC[15] * this.BirthCondition)
                                     * this.WOOD(lactTime, this.Genotype.IntakeC[8], shapeParam)
                                     * this.lactationAdjustment;
            }
            this.PotIntake = this.Genotype.IntakeC[1] * this.standardReferenceWeight * this.RelativeSize * (this.Genotype.IntakeC[2] - this.RelativeSize)
                           * condFactor * youngFactor * heatFactor * lactFactor * this.IntakeModifier;
        }

        /// <summary>
        /// Reset the grazing values
        /// </summary>
        public void ResetGrazing()
        {
            this.AnimalState = new AnimalOutput();
            this.SupplementFreshWeightIntake = 0.0;
            this.startFU = 1.0;
            Array.Resize(ref this.netSupplementDMI, this.RationFed.Count);
            Array.Resize(ref this.timeStepNetSupplementDMI, this.RationFed.Count);
            for (int Idx = 0; Idx < this.netSupplementDMI.Length; Idx++)
                this.netSupplementDMI[Idx] = 0.0;
        }

        /// <summary>
        /// Do grazing
        /// </summary>
        /// <param name="deltaT">Fraction of an animal's active day</param>
        /// <param name="reset">TRUE at the start of the day</param>
        /// <param name="feedSuppFirst">Feed supplement first</param>
        /// <param name="pastIntakeRate"></param>
        /// <param name="suppIntakeRate"></param>
        public void Grazing(double deltaT,
                             bool reset,
                             bool feedSuppFirst,
                             ref GrazType.GrazingOutputs pastIntakeRate,
                             ref double suppIntakeRate)
        {
            double[] herbageRI = new double[GrazType.DigClassNo + 1];
            double maintMEIScalar;
            double waterLogScalar;
            double[,] seedRI = new double[GrazType.MaxPlantSpp + 1, 3];
            double suppRI = 0;

            // Do this before resetting AnimalState!    
            if ((!animalsDynamicGlb || (this.AnimalState.ME_Intake.Total == 0.0) || (this.WaterLogging == 0.0) || (this.PaddSteep > 1.0)))    // Waterlogging effect only on level ground 
                waterLogScalar = 1.0;                                                 // The published model assumes WaterLog=0   
            else if ((this.AnimalState.EnergyUse.Gain == 0.0) || (this.AnimalState.Efficiency.Gain == 0.0))
                waterLogScalar = 1.0;
            else
            {
                maintMEIScalar = Math.Max(0.0, (this.AnimalState.EnergyUse.Gain / this.AnimalState.Efficiency.Gain) / this.AnimalState.ME_Intake.Total);
                waterLogScalar = StdMath.DIM(1.0, maintMEIScalar * WaterLogging);
            }

            if (reset)                                                                  // First time step of the day?              
                this.ResetGrazing();

            this.timeStepState = new AnimalOutput();

            this.CalculateRelIntake(this, deltaT, feedSuppFirst,
                                waterLogScalar,                                       // The published model assumes WaterLog=0   
                                ref herbageRI, ref seedRI, ref suppRI);
            this.DescribeTheDiet(ref herbageRI, ref seedRI, ref suppRI, ref this.timeStepState);
            this.UpdateAnimalState(deltaT, feedSuppFirst, suppRI);

            pastIntakeRate.CopyFrom(this.timeStepState.IntakePerHead);

            suppIntakeRate = StdMath.XDiv(this.PotIntake * suppRI, this.IntakeSupplement.DMPropn);
        }

        /// <summary>
        /// Nutrition function
        /// </summary>
        public void Nutrition()
        {
            this.Efficiencies();
            this.ComputeMaintenance();
            this.ComputeDPLS();

            if ((this.ReproState == GrazType.ReproType.EarlyPreg) || (this.ReproState == GrazType.ReproType.LatePreg))
                this.ComputePregnancy();

            if (this.lactStatus == GrazType.LactType.Lactating)
                this.ComputeLactation();

            if (Animal == GrazType.AnimalType.Sheep)
                this.ComputeWool(0.0);

            this.AdjustKGain();
            this.ComputeChilling();

            this.AdjustKGain();
            this.ComputeGain();

            if (this.Animal == GrazType.AnimalType.Sheep)
                this.ApplyWoolGrowth();
            this.ComputePhosphorus();                                                       // These must be done after DeltaFleeceWt   
            this.ComputeSulfur();                                                           // is known                               
            this.ComputeAshAlk();

            this.totalWeight = this.BaseWeight + this.ConceptusWt();                    // TotalWeight is meant to be the weight    
            if (this.Animal == GrazType.AnimalType.Sheep)                               // "on the scales", including conceptus   
                this.totalWeight = this.totalWeight + this.woolWt;                      // and/or fleece.                         
            this.AnimalState.IntakeLimitLegume = this.PotIntake * (1.0 + this.Genotype.GrazeC[2] * this.Herbage.LegumePropn);
        }

        /// <summary>
        /// Test whether intake of RDP matches the requirement for RDP.               
        /// </summary>
        /// <returns></returns>
        public double RDPIntakeFactor()
        {
            Diet tempCorrDg = new Diet();
            Diet tempUDP = new Diet();
            double tempRDPI = 0.0;
            double tempRDPR = 0.0;
            double tempFL;
            double oldResult, tempResult;
            int idx;

            // testResult : float;
            double result;

            if ((this.AnimalState.DM_Intake.Solid < GrazType.VerySmall) || (this.AnimalState.RDP_Intake >= this.AnimalState.RDP_Reqd))
                result = 1.0;
            else
            {
                result = this.AnimalState.RDP_Intake / this.AnimalState.RDP_Reqd;
                if ((Genotype.IntakeC[16] > 0.0) && (this.Genotype.IntakeC[16] < 1.0))
                    result = 1.0 + this.Genotype.IntakeC[16] * (result - 1.0);
                idx = 0;
                do
                {
                    oldResult = result;
                    tempFL = (oldResult * this.AnimalState.ME_Intake.Total) / this.AnimalState.EnergyUse.Maint - 1.0;
                    this.ComputeRDP(oldResult, tempFL,
                                ref tempCorrDg, ref tempRDPI, ref tempRDPR, ref tempUDP);
                    tempResult = StdMath.XDiv(tempRDPI, tempRDPR);
                    if ((this.Genotype.IntakeC[16] > 0.0) && (this.Genotype.IntakeC[16] < 1.0))
                        tempResult = 1.0 + this.Genotype.IntakeC[16] * (tempResult - 1.0);
                    result = Math.Max(0.0, Math.Min(1.0 - 0.5 * (1.0 - oldResult), tempResult));
                    idx++;
                }
                while ((idx < 5) && (Math.Abs(result - oldResult) >= 0.001));  //UNTIL (Idx >= 5) or (Abs(Result-OldResult) < 0.001);
            }
            return result;
        }

        /// <summary>
        /// Complete growth function
        /// </summary>
        /// <param name="rdpFactor"></param>
        public void CompleteGrowth(double rdpFactor)
        {
            double lifeWG, dayWG;

            this.AnimalState.RDP_IntakeEffect = rdpFactor;

            if ((this.MaleNo == 0) || (this.FemaleNo == 0))
                this.baseWeightGainSolid = 0.0;
            else
            {
                lifeWG = StdMath.DIM(BaseWeight - WeightChange, this.birthWeight);
                dayWG = Math.Max(WeightChange, 0.0);
                baseWeightGainSolid = StdMath.XDiv(lifeWG * baseWeightGainSolid + dayWG * StdMath.XDiv(this.AnimalState.ME_Intake.Solid, this.AnimalState.ME_Intake.Total), lifeWG + dayWG);
            }
        }

        /// <summary>
        /// Records state information prior to the grazing and nutrition calculations     
        /// so that it can be restored if there is an RDP insufficiency.                
        /// </summary>
        /// <param name="animalInfo"></param>
        public void StoreStateInfo(ref AnimalStateInfo animalInfo)
        {
            animalInfo.BaseWeight = BaseWeight;
            animalInfo.WoolWt = woolWt;
            animalInfo.WoolMicron = FibreDiam;
            animalInfo.CoatDepth = coatDepth;
            animalInfo.FoetalWt = FoetalWeight;
            animalInfo.LactAdjust = lactationAdjustment;
            animalInfo.LactRatio = lactationRatio;
            animalInfo.BasePhos = basePhosphorusWeight;
            animalInfo.BaseSulf = baseSulphurWeight;
        }

        /// <summary>
        /// Restores state information about animal groups if there is an RDP insufficiency.                                                              
        /// </summary>
        /// <param name="animalInfo"></param>
        public void RevertStateInfo(AnimalStateInfo animalInfo)
        {
            BaseWeight = animalInfo.BaseWeight;
            woolWt = animalInfo.WoolWt;
            FibreDiam = animalInfo.WoolMicron;
            coatDepth = animalInfo.CoatDepth;
            FoetalWeight = animalInfo.FoetalWt;
            lactationAdjustment = animalInfo.LactAdjust;
            lactationRatio = animalInfo.LactRatio;
            basePhosphorusWeight = animalInfo.BasePhos;
            baseSulphurWeight = animalInfo.BaseSulf;
        }

        /// <summary>
        /// Calculate relative intake
        /// </summary>
        /// <param name="theAnimals">The Animal group</param>
        /// <param name="timeStepLength"></param>
        /// <param name="feedSuppFirst"></param>
        /// <param name="waterLogScalar"></param>
        /// <param name="herbageRI"></param>
        /// <param name="seedRI"></param>
        /// <param name="suppRelIntake"></param>
        public void CalculateRelIntake(AnimalGroup theAnimals,
                              double timeStepLength,
                              bool feedSuppFirst,
                              double waterLogScalar,
                              ref double[] herbageRI,
                              ref double[,] seedRI,
                              ref double suppRelIntake)
        {
            const double CLASSWIDTH = 0.1;

            double[] availFeed = new double[GrazType.DigClassNo + 2]; // 1..DigClassNo+1   // Grazeable DM in each quality class    
            double[] heightRatio = new double[GrazType.DigClassNo + 2];                    // "Height ratio"                        
            double legume;                                                                 // Legume fraction                       
            double legumeTrop;                                                             // Legume tropicality }
            double selectFactor;                                                           // SF, adjusted for legume content       
            double[] relQ = new double[GrazType.DigClassNo + 2];                           // Function of herbage class digestib'ty 
            double suppRelQ;                                                               // Function of supplement digestibility  
            double suppFWPerHead;
            double suppDWPerHead;
            double totalFeed;

            double OMD_Supp;
            double proteinFactor;                                                          // DOM/protein and lactation factors for 
            double milkFactor;                                                             // modifying substitution rate         
            double substSuppRelQ;

            double[] relIntake = new double[GrazType.DigClassNo + 2];
            double suppEntry;
            double fillRemaining;                                                          // Proportion of maximum relative fill  that is yet to be satisfied         
            bool suppRemains;                                                              // TRUE if the animals have yet to select a supplement that is present        
            double legumeAdjust;
            int speciesIdx,
            classIdx,
            ripeIdx;

            // Start by aggregating herbage and seed into selection classes              
            for (classIdx = 1; classIdx <= GrazType.DigClassNo; classIdx++)
            {
                availFeed[classIdx] = theAnimals.Herbage.Herbage[classIdx].Biomass;
                heightRatio[classIdx] = theAnimals.Herbage.Herbage[classIdx].HeightRatio;
            }
            availFeed[GrazType.DigClassNo + 1] = 0.0;
            heightRatio[GrazType.DigClassNo + 1] = 1.0;

            for (speciesIdx = 1; speciesIdx <= GrazType.MaxPlantSpp; speciesIdx++)
            {
                for (ripeIdx = GrazType.UNRIPE; ripeIdx <= GrazType.RIPE; ripeIdx++)
                {
                    classIdx = theAnimals.Herbage.SeedClass[speciesIdx, ripeIdx];
                    if ((classIdx > 0) && (theAnimals.Herbage.Seeds[speciesIdx, ripeIdx].Biomass > GrazType.VerySmall))
                    {
                        WeightAverage(ref heightRatio[classIdx],
                                        availFeed[classIdx],
                                        theAnimals.Herbage.Seeds[speciesIdx, ripeIdx].HeightRatio,
                                        theAnimals.Herbage.Seeds[speciesIdx, ripeIdx].Biomass);
                        availFeed[classIdx] = availFeed[classIdx] + theAnimals.Herbage.Seeds[speciesIdx, ripeIdx].Biomass;
                    }
                }
            }

            totalFeed = 0.0;
            for (classIdx = 1; classIdx <= GrazType.DigClassNo + 1; classIdx++)
                totalFeed = totalFeed + availFeed[classIdx];

            legume = theAnimals.Herbage.LegumePropn;
            legumeTrop = theAnimals.Herbage.LegumeTrop;

            theAnimals.IntakeSupplement = theAnimals.RationFed.AverageSuppt();
            suppFWPerHead = theAnimals.RationFed.TotalAmount;
            suppDWPerHead = suppFWPerHead * theAnimals.IntakeSupplement.DMPropn;

            herbageRI = new double[GrazType.DigClassNo + 1];                                // Sundry initializations                
            seedRI = new double[GrazType.MaxPlantSpp + 1, GrazType.RIPE + 1];
            suppRelIntake = 0.0;
            relIntake = new double[GrazType.DigClassNo + 2];

            selectFactor = (1.0 - legume * (1.0 - legumeTrop)) * theAnimals.Herbage.SelectFactor;         // Herbage relative quality calculation  
            for (classIdx = 1; classIdx <= GrazType.DigClassNo; classIdx++)
                relQ[classIdx] = 1.0 - theAnimals.Genotype.GrazeC[3] * StdMath.DIM(theAnimals.Genotype.GrazeC[1] - selectFactor, theAnimals.Herbage.Herbage[classIdx].Digestibility); // Eq. 21 
            relQ[GrazType.DigClassNo + 1] = 1;                                             // fixes range check error. Set this to the value that was calc'd when range check error was in place

            suppRemains = (suppFWPerHead > GrazType.VerySmall);                           // Compute relative quality of supplement (if present)
            if (suppRemains)
            {
                suppRelQ = Math.Min(theAnimals.Genotype.GrazeC[14],
                                   1.0 - theAnimals.Genotype.GrazeC[3] * (theAnimals.Genotype.GrazeC[1] - theAnimals.IntakeSupplement.DMDigestibility));

                if (theAnimals.lactStatus == GrazType.LactType.Lactating)
                    milkFactor = theAnimals.Genotype.GrazeC[15] * Math.Exp(-StdMath.Sqr(theAnimals.daysLactating / theAnimals.Genotype.GrazeC[8]));
                else
                    milkFactor = 0.0;

                OMD_Supp = Math.Min(1.0, 1.05 * theAnimals.IntakeSupplement.DMDigestibility - 0.01);
                if (OMD_Supp > 0.0)
                    proteinFactor = theAnimals.Genotype.GrazeC[16] * StdMath.RAMP(theAnimals.IntakeSupplement.CrudeProt / OMD_Supp, theAnimals.Genotype.GrazeC[9], theAnimals.Genotype.GrazeC[10]);
                else
                    proteinFactor = 0.0;

                substSuppRelQ = suppRelQ - milkFactor - proteinFactor;
            }
            else
            {
                suppRelQ = 0.0;
                substSuppRelQ = 0.0;
            }

            fillRemaining = theAnimals.startFU;

            if (suppRemains && (feedSuppFirst || (totalFeed <= GrazType.VerySmall)))
            {
                // Case where supplement is fed first
                EatSupplement(theAnimals, timeStepLength, suppDWPerHead, theAnimals.IntakeSupplement, suppRelQ, true, ref suppRelIntake, ref fillRemaining);
                theAnimals.startFU = fillRemaining;
                suppRemains = false;
            }

            if (totalFeed > GrazType.VerySmall)
            {
                // Case where there is pasture available to the animals
                classIdx = 1;
                while ((classIdx <= GrazType.DigClassNo + 1) && (fillRemaining >= GrazType.VerySmall))
                {
                    suppEntry = Math.Min(1.0, 0.5 + (substSuppRelQ - relQ[classIdx])
                                                   / (CLASSWIDTH * theAnimals.Genotype.GrazeC[3]));
                    if (suppRemains && (suppEntry > 0.0))
                    {
                        // This gives a continuous response to changes in supplement DMD
                        this.EatPasture(theAnimals, (1.0 - suppEntry) * availFeed[classIdx], totalFeed, heightRatio[classIdx], relQ[classIdx], ref relIntake[classIdx], ref fillRemaining);
                        this.EatSupplement(theAnimals, timeStepLength, suppDWPerHead, theAnimals.IntakeSupplement, suppRelQ, false, ref suppRelIntake, ref fillRemaining);
                        this.EatPasture(theAnimals, suppEntry * availFeed[classIdx], totalFeed, heightRatio[classIdx], relQ[classIdx], ref relIntake[classIdx], ref fillRemaining);

                        suppRemains = false;
                    }
                    else
                        this.EatPasture(theAnimals,
                                    availFeed[classIdx],
                                    totalFeed,
                                    heightRatio[classIdx],
                                    relQ[classIdx],
                                    ref relIntake[classIdx],
                                    ref fillRemaining);
                    classIdx++;
                }

                // Still supplement left? 
                if (suppRemains)
                    this.EatSupplement(theAnimals, timeStepLength, suppDWPerHead, theAnimals.IntakeSupplement, suppRelQ, false, ref suppRelIntake, ref fillRemaining);

                legumeAdjust = theAnimals.Genotype.GrazeC[2] * StdMath.Sqr(1.0 - fillRemaining) * legume;         // Adjustment to intake rate for waterlogging and legume content        
                for (classIdx = 1; classIdx <= GrazType.DigClassNo; classIdx++)
                    relIntake[classIdx] = relIntake[classIdx] * waterLogScalar * (1.0 + legumeAdjust);
            }

            for (classIdx = 1; classIdx <= GrazType.DigClassNo; classIdx++)
            {
                // Distribute relative intakes between herbage and seed
                herbageRI[classIdx] = relIntake[classIdx] * StdMath.XDiv(theAnimals.Herbage.Herbage[classIdx].Biomass, availFeed[classIdx]);
            }

            for (speciesIdx = 1; speciesIdx <= GrazType.MaxPlantSpp; speciesIdx++)
            {
                for (ripeIdx = GrazType.UNRIPE; ripeIdx <= GrazType.RIPE; ripeIdx++)
                {
                    classIdx = theAnimals.Herbage.SeedClass[speciesIdx, ripeIdx];
                    if ((classIdx > 0) && (theAnimals.Herbage.Seeds[speciesIdx, ripeIdx].Biomass > GrazType.VerySmall))
                        seedRI[speciesIdx, ripeIdx] = relIntake[classIdx] * theAnimals.Herbage.Seeds[speciesIdx, ripeIdx].Biomass / availFeed[classIdx];
                }
            }
        }

        /// <summary>
        /// Feasible range of weights for a given age and (relative) body condition   
        /// This weight range is a consequence of the normal weight function          
        /// (AnimalGroup.NormalWeightFunc)                                           
        /// </summary>
        /// <param name="reprod"></param>
        /// <param name="ageDays">Age in days</param>
        /// <param name="bodyCond">Body condition</param>
        /// <param name="paramSet">Animal params</param>
        /// <param name="lowBaseWt"></param>
        /// <param name="highBaseWt"></param>
        public void WeightRangeForCond(GrazType.ReproType reprod,
                                      int ageDays,
                                      double bodyCond,
                                      Genotype paramSet,
                                      ref double lowBaseWt,
                                      ref double highBaseWt)
        {
            double maxNormWt;

            maxNormWt = GrowthCurve(ageDays, reprod, paramSet);
            highBaseWt = bodyCond * maxNormWt;
            if (bodyCond >= 1.0)
                lowBaseWt = highBaseWt;
            else
                lowBaseWt = highBaseWt * paramSet.GrowthC[3] / (1.0 - bodyCond * (1.0 - paramSet.GrowthC[3]));
        }

        /// <summary>
        /// Returns the number of male and female animals  
        /// which are aged greater than a number of days
        /// </summary>
        /// <param name="ageDays">Days of age</param>
        /// <param name="numMale">Number of male</param>
        /// <param name="numFemale">Number of female</param>
        public void GetOlder(int ageDays, ref int numMale, ref int numFemale)
        {
            this.ages.GetOlder(ageDays, ref numMale, ref numFemale);
        }

        // ------------------ Private model logic ------------------

        /// <summary>
        /// 
        /// </summary>
        /// <param name="classAttr"></param>
        /// <param name="netClassIntake"></param>
        /// <param name="summaryIntake"></param>
        private void AddDietElement(ref GrazType.IntakeRecord classAttr, double netClassIntake, ref GrazType.IntakeRecord summaryIntake)
        {
            if (netClassIntake > 0.0)
            {
                summaryIntake.Biomass = summaryIntake.Biomass + netClassIntake;
                summaryIntake.Digestibility = summaryIntake.Digestibility + netClassIntake * classAttr.Digestibility;
                summaryIntake.CrudeProtein = summaryIntake.CrudeProtein + netClassIntake * classAttr.CrudeProtein;
                summaryIntake.Degradability = summaryIntake.Degradability + netClassIntake * classAttr.CrudeProtein * classAttr.Degradability;
                summaryIntake.PhosContent = summaryIntake.PhosContent + netClassIntake * classAttr.PhosContent;
                summaryIntake.SulfContent = summaryIntake.SulfContent + netClassIntake * classAttr.SulfContent;
                summaryIntake.AshAlkalinity = summaryIntake.AshAlkalinity + netClassIntake * classAttr.AshAlkalinity;
            }
        }

        /// <summary>
        /// Summarise the intake record
        /// </summary>
        /// <param name="summaryIntake">The intake record</param>
        private void SummariseIntakeRecord(ref GrazType.IntakeRecord summaryIntake)
        {
            double trivialIntake = 1.0E-6; // (kg/head)

            if (summaryIntake.Biomass < trivialIntake)
                summaryIntake = new GrazType.IntakeRecord();
            else
            {
                summaryIntake.Digestibility = summaryIntake.Digestibility / summaryIntake.Biomass;
                if (summaryIntake.CrudeProtein > 0.0)
                    summaryIntake.Degradability = summaryIntake.Degradability / summaryIntake.CrudeProtein;
                else
                    summaryIntake.Degradability = 0.75;

                summaryIntake.CrudeProtein = summaryIntake.CrudeProtein / summaryIntake.Biomass;
                summaryIntake.PhosContent = summaryIntake.PhosContent / summaryIntake.Biomass;
                summaryIntake.SulfContent = summaryIntake.SulfContent / summaryIntake.Biomass;
                summaryIntake.AshAlkalinity = summaryIntake.AshAlkalinity / summaryIntake.Biomass;
            }
        }

        /// <summary>
        ///  DescribeTheDiet                                                           
        /// Calculate the following for each applicable component of the diet         
        /// (herbage, supplement and milk):                                             
        ///   - Dry weight of intake             - Intake of ME                       
        ///   - Weight of protein in the intake  - Intake of P                        
        ///   - Digestibility                    - Intake of S                        
        ///   - Digestible organic matter (DOM)  - Concentration of protein           
        ///   - ME:DM ratio                                                           
        /// These results are all stored in the TimeStepState static variable for     
        /// reference by other routines.                                              
        /// </summary>
        /// <param name="herbageRI">"Relative intakes" of each herbage digestibility class</param>
        /// <param name="seedRI">"Relative intakes" of seeds</param>
        /// <param name="suppRI">"Relative intakes" of supplement</param>
        /// <param name="timeStepState"></param>
        private void DescribeTheDiet(
                                   ref double[] herbageRI,         
                                   ref double[,] seedRI,           
                                   ref double suppRI,                                             
                                   ref AnimalOutput timeStepState)
        {
            GrazType.IntakeRecord suppInput = new GrazType.IntakeRecord();
            double gutPassage;
            double supp_ME2DM;                                              // Used to compute ME_2_DM.Supp          
            int species, classIdx, ripeIdx, idx;

            for (classIdx = 1; classIdx <= GrazType.DigClassNo; classIdx++)
                timeStepState.IntakePerHead.Herbage[classIdx] = this.PotIntake * herbageRI[classIdx];
            for (species = 1; species <= GrazType.MaxPlantSpp; species++)
            {
                for (ripeIdx = GrazType.UNRIPE; ripeIdx <= GrazType.RIPE; ripeIdx++)
                    timeStepState.IntakePerHead.Seed[species, ripeIdx] = this.PotIntake * seedRI[species, ripeIdx];
            }
            timeStepState.PaddockIntake = new GrazType.IntakeRecord();              // Summarise herbage+seed intake         
            for (classIdx = 1; classIdx <= GrazType.DigClassNo; classIdx++)
                this.AddDietElement(ref this.Herbage.Herbage[classIdx], timeStepState.IntakePerHead.Herbage[classIdx], ref timeStepState.PaddockIntake);
            for (species = 1; species <= GrazType.MaxPlantSpp; species++)
            {
                for (ripeIdx = GrazType.UNRIPE; ripeIdx <= GrazType.RIPE; ripeIdx++)
                    this.AddDietElement(ref this.Herbage.Seeds[species, ripeIdx], timeStepState.IntakePerHead.Seed[species, ripeIdx], ref timeStepState.PaddockIntake);
            }

            this.SummariseIntakeRecord(ref timeStepState.PaddockIntake);
            if (timeStepState.PaddockIntake.Biomass == 0.0) // i.e. less than fTrivialIntake
                timeStepState.IntakePerHead = new GrazType.GrazingOutputs();

            timeStepState.SuppIntake = new GrazType.IntakeRecord();                 // Summarise supplement intake           
            supp_ME2DM = 0.0;
            if ((this.RationFed.TotalAmount > 0.0) && (suppRI * this.PotIntake > 0.0))
            {
                // The supplements must be treated separately because of the non-linearity in the gut passage term
                for (idx = 0; idx <= this.RationFed.Count - 1; idx++)                                 
                {                                                                   
                    suppInput.Digestibility = this.RationFed[idx].DMDigestibility;           
                    suppInput.CrudeProtein = this.RationFed[idx].CrudeProt;
                    suppInput.Degradability = this.RationFed[idx].DegProt;
                    suppInput.PhosContent = this.RationFed[idx].Phosphorus;
                    suppInput.SulfContent = this.RationFed[idx].Sulphur;
                    suppInput.AshAlkalinity = this.RationFed[idx].AshAlkalinity;

                    if (this.Animal == GrazType.AnimalType.Cattle)
                        gutPassage = this.RationFed[idx].MaxPassage * StdMath.RAMP(this.RationFed.TotalAmount / PotIntake, 0.20, 0.75);
                    else
                        gutPassage = 0.0;
                    this.timeStepNetSupplementDMI[idx] = (1.0 - gutPassage) * this.RationFed.GetFWFract(idx) * (PotIntake * suppRI);

                    this.AddDietElement(ref suppInput, this.timeStepNetSupplementDMI[idx], ref timeStepState.SuppIntake);
                    supp_ME2DM = supp_ME2DM + this.timeStepNetSupplementDMI[idx] * this.RationFed[idx].ME2DM;
                }

                this.SummariseIntakeRecord(ref timeStepState.SuppIntake);
                if (timeStepState.SuppIntake.Biomass == 0.0) 
                {
                    // i.e. less than fTrivialIntake
                    for (idx = 0; idx <= this.RationFed.Count - 1; idx++)
                        this.timeStepNetSupplementDMI[idx] = 0.0;
                    supp_ME2DM = 0.0;
                }
                else
                    supp_ME2DM = StdMath.XDiv(supp_ME2DM, timeStepState.SuppIntake.Biomass);
            }
            else
                for (idx = 0; idx <= this.RationFed.Count - 1; idx++)
                    this.timeStepNetSupplementDMI[idx] = 0.0;

            timeStepState.DM_Intake.Herbage = timeStepState.PaddockIntake.Biomass;                                  // Dry matter intakes                    
            timeStepState.DM_Intake.Supp = timeStepState.SuppIntake.Biomass;
            timeStepState.DM_Intake.Solid = timeStepState.DM_Intake.Herbage + timeStepState.DM_Intake.Supp;
            timeStepState.DM_Intake.Total = timeStepState.DM_Intake.Solid;                                          // Milk doesn't count for DM intake      

            timeStepState.Digestibility.Herbage = timeStepState.PaddockIntake.Digestibility;                        // Digestibilities                       
            timeStepState.Digestibility.Supp = timeStepState.SuppIntake.Digestibility;
            timeStepState.Digestibility.Solid = StdMath.XDiv(
                                           timeStepState.Digestibility.Supp * timeStepState.DM_Intake.Supp +
                                           timeStepState.Digestibility.Herbage * timeStepState.DM_Intake.Herbage,
                                           timeStepState.DM_Intake.Solid);

            if (this.lactStatus == GrazType.LactType.Suckling)                                                                         
            {
                // Milk terms
                timeStepState.CP_Intake.Milk = this.mothers.MilkProtein / this.NoOffspring;
                timeStepState.Phos_Intake.Milk = this.mothers.milkPhosphorusProduction / this.NoOffspring;
                timeStepState.Sulf_Intake.Milk = this.mothers.milkSulphurProduction / this.NoOffspring;
                timeStepState.ME_Intake.Milk = this.mothers.MilkEnergy / this.NoOffspring;
            }
            else
            {
                timeStepState.CP_Intake.Milk = 0.0;
                timeStepState.Phos_Intake.Milk = 0.0;
                timeStepState.Sulf_Intake.Milk = 0.0;
                timeStepState.ME_Intake.Milk = 0.0;
            }

            timeStepState.CP_Intake.Herbage = timeStepState.PaddockIntake.Biomass * timeStepState.PaddockIntake.CrudeProtein;   // Crude protein intakes and contents    
            timeStepState.CP_Intake.Supp = timeStepState.SuppIntake.Biomass * timeStepState.SuppIntake.CrudeProtein;
            timeStepState.CP_Intake.Solid = timeStepState.CP_Intake.Herbage + timeStepState.CP_Intake.Supp;
            timeStepState.CP_Intake.Total = timeStepState.CP_Intake.Solid + timeStepState.CP_Intake.Milk;
            timeStepState.ProteinConc.Herbage = timeStepState.PaddockIntake.CrudeProtein;
            timeStepState.ProteinConc.Supp = timeStepState.SuppIntake.CrudeProtein;
            timeStepState.ProteinConc.Solid = StdMath.XDiv(timeStepState.CP_Intake.Solid, timeStepState.DM_Intake.Solid);

            timeStepState.Phos_Intake.Herbage = timeStepState.PaddockIntake.Biomass * timeStepState.PaddockIntake.PhosContent;  // Phosphorus intakes                    
            timeStepState.Phos_Intake.Supp = 0.0;
            timeStepState.Phos_Intake.Solid = timeStepState.Phos_Intake.Herbage + timeStepState.Phos_Intake.Supp;
            timeStepState.Phos_Intake.Total = timeStepState.Phos_Intake.Solid + timeStepState.Phos_Intake.Milk;

            timeStepState.Sulf_Intake.Herbage = timeStepState.PaddockIntake.Biomass * timeStepState.PaddockIntake.SulfContent;  // Sulphur intakes                       
            timeStepState.Sulf_Intake.Supp = 0.0;
            timeStepState.Sulf_Intake.Solid = timeStepState.Sulf_Intake.Herbage + timeStepState.Sulf_Intake.Supp;
            timeStepState.Sulf_Intake.Total = timeStepState.Sulf_Intake.Solid + timeStepState.Sulf_Intake.Milk;

            timeStepState.ME_2_DM.Herbage = GrazType.HerbageE2DM * timeStepState.Digestibility.Herbage - 2.0;                   // Metabolizable energy intakes and contents     
            timeStepState.ME_2_DM.Supp = supp_ME2DM;                                                                                                        
            timeStepState.ME_Intake.Supp = timeStepState.ME_2_DM.Supp * timeStepState.DM_Intake.Supp;
            timeStepState.ME_Intake.Herbage = timeStepState.ME_2_DM.Herbage * timeStepState.DM_Intake.Herbage;
            timeStepState.ME_Intake.Solid = timeStepState.ME_Intake.Herbage + timeStepState.ME_Intake.Supp;
            timeStepState.ME_Intake.Total = timeStepState.ME_Intake.Solid + timeStepState.ME_Intake.Milk;
            timeStepState.ME_2_DM.Solid = StdMath.XDiv(timeStepState.ME_Intake.Solid, timeStepState.DM_Intake.Solid);
        }

        /// <summary>
        /// Compute RDP intake and requirement for a given MEI and feeding level      
        /// </summary>
        /// <param name="intakeScale"></param>
        /// <param name="feedingLevel"></param>
        /// <param name="corrDg"></param>
        /// <param name="rdpi"></param>
        /// <param name="rdpr"></param>
        /// <param name="udpis"></param>
        private void ComputeRDP(double intakeScale,            // Assumed scaling factor for intake        
                                  double feedingLevel,                     // Assumed feeding level                    
                                  ref Diet corrDg,
                                  ref double rdpi, ref double rdpr,
                                  ref Diet udpis)
        {
            Diet RDPIs;
            double suppFME_Intake;                                                                      // Fermentable ME intake of supplement      
            int idx;

            corrDg.Herbage = this.AnimalState.PaddockIntake.Degradability                               // Correct the protein degradability        
                              * (1.0 - (Genotype.DgProtC[1] - this.Genotype.DgProtC[2] * this.AnimalState.Digestibility.Herbage)// for feeding level                     
                                       * Math.Max(feedingLevel, 0.0));
            corrDg.Supp = this.AnimalState.SuppIntake.Degradability
                              * (1.0 - this.Genotype.DgProtC[3] * Math.Max(feedingLevel, 0.0));

            RDPIs.Herbage = intakeScale * this.AnimalState.CP_Intake.Herbage * this.AnimalState.CorrDgProt.Herbage;
            RDPIs.Supp = intakeScale * this.AnimalState.CP_Intake.Supp * this.AnimalState.CorrDgProt.Supp;
            RDPIs.Solid = RDPIs.Herbage + RDPIs.Supp;
            RDPIs.Milk = 0.0;                                                                           // This neglects any degradation of milk    
            udpis.Herbage = intakeScale * this.AnimalState.CP_Intake.Herbage - RDPIs.Herbage;           // CPI late in lactation when the rumen   
            udpis.Supp = intakeScale * this.AnimalState.CP_Intake.Supp - RDPIs.Supp;                    // has begun to develop                   
            udpis.Milk = this.AnimalState.CP_Intake.Milk;
            udpis.Solid = udpis.Herbage + udpis.Supp;
            rdpi = RDPIs.Solid + RDPIs.Milk;

            suppFME_Intake = StdMath.DIM(intakeScale * this.AnimalState.ME_Intake.Supp,                 // Fermentable ME intake of supplement      
                                   GrazType.ProteinE2DM * udpis.Supp);                                  // leaves out the ME derived from         
            for (idx = 0; idx <= RationFed.Count - 1; idx++)                                            // undegraded protein and oils            
                suppFME_Intake = StdMath.DIM(suppFME_Intake,
                                       GrazType.FatE2DM * RationFed[idx].EtherExtract * intakeScale * netSupplementDMI[idx]);

            rdpr = (this.Genotype.DgProtC[4] + this.Genotype.DgProtC[5] * (1.0 - Math.Exp(-this.Genotype.DgProtC[6] * (feedingLevel + 1.0))))       // RDP requirement                          
                    * (intakeScale * this.AnimalState.ME_Intake.Herbage
                        * (1.0 + Genotype.DgProtC[7] * (weather.Latitude / 40.0)
                                            * Math.Sin(GrazEnv.DAY2RAD * clock.Today.DayOfYear)) + suppFME_Intake);
        }
        
        /// <summary>
        /// Set the standard reference weight of a group of animals based on breed  
        /// and sex                                                                   
        /// </summary>
        private void ComputeSRW()
        {
            double SRW;                                                             // Breed standard reference weight (i.e.    
                                                                                    // normal weight of a mature, empty female)

            if (mothers != null)                                                    // For lambs and calves, take both parents' 
                SRW = Genotype.BreedSRW;     // 0.5 * (BreedSRW + MaleSRW)           // breed SRW's into account               
            else
                SRW = Genotype.BreedSRW;

            if (MaleNo == 0)                                                       // Now take into account different SRWs of  
                standardReferenceWeight = SRW;                                                     // males and females and different        
            else                                                                    // scalars for entire and castrated males 
                standardReferenceWeight = SRW * StdUnits.StdMath.XDiv(FemaleNo + MaleNo * Genotype.SRWScalars[(int)ReproState],       // TODO: check this
                                        FemaleNo + MaleNo);
        }

        /// <summary>
        /// Reference birth weight, adjusted for number of foetuses and relative size 
        /// </summary>
        /// <returns></returns>
        private double BirthWeightForSize()
        {
            return this.Genotype.StdBirthWt(NoFoetuses) * ((1.0 - this.Genotype.PregC[4]) + this.Genotype.PregC[4] * RelativeSize);
        }
        
        /// <summary>
        ///  "Normal weight" of the foetus and the weight of the conceptus in pregnant animals.         
        /// </summary>
        /// <returns>The normal weight</returns>
        private double FoetalNormWt()
        {
            if ((this.ReproState == GrazType.ReproType.EarlyPreg) || (this.ReproState == GrazType.ReproType.LatePreg))
                return BirthWeightForSize() * Gompertz(foetalAge, this.Genotype.PregC[1], this.Genotype.PregC[2], this.Genotype.PregC[3]);
            else
                return 0.0;
        }

        /// <summary>
        /// Gompertz function, constrained to give f(A)=1.0                              
        /// </summary>
        /// <param name="t"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        private double Gompertz(double t, double a, double b, double c)
        {
            return Math.Exp(b * (1.0 - Math.Exp(c * (1.0 - t / a))));
        }

        /// <summary>
        /// Weight of the conceptus, i.e. foetus(es) plus uterus etc                  
        /// </summary>
        /// <returns>Conceptus weight</returns>
        private double ConceptusWt()
        {
            if ((ReproState == GrazType.ReproType.EarlyPreg) || (ReproState == GrazType.ReproType.LatePreg))
                return NoFoetuses
                          * (this.Genotype.PregC[5] * BirthWeightForSize() * Gompertz(foetalAge, this.Genotype.PregC[1], this.Genotype.PregC[6], this.Genotype.PregC[7])
                             + FoetalWeight - FoetalNormWt());
            else
                return 0.0;
        }
       
        /// <summary>
        /// Normal weight equation                                                 
        /// </summary>
        /// <param name="ageDays"></param>
        /// <param name="maxOldWt"></param>
        /// <param name="weighting"></param>
        /// <returns></returns>
        private double NormalWeightFunc(int ageDays, double maxOldWt, double weighting)
        {
            double fMaxNormWt;

            fMaxNormWt = MaxNormWtFunc(standardReferenceWeight, birthWeight, ageDays, Genotype);
            if (maxOldWt < fMaxNormWt)                                           // Delayed deveopment of frame size         
                return weighting * fMaxNormWt + (1.0 - weighting) * maxOldWt;
            else
                return fMaxNormWt;
        }
        
        /// <summary>
        /// Calculate normal weight, size and condition of a group of animals.      
        /// </summary>
        private void CalculateWeights()
        {
            maxPrevWeight = Math.Max(BaseWeight, maxPrevWeight);                             // Store the highest weight reached to date 
            normalWeight = NormalWeightFunc(AgeDays, maxPrevWeight, Genotype.GrowthC[3]);
            RelativeSize = normalWeight / standardReferenceWeight;
            BodyCondition = BaseWeight / normalWeight;
        }
        
        /// <summary>
        /// Compute coat depth from GFW and fibre diameter                              
        /// </summary>
        private void CalculateCoatDepth()
        {
            double fibreCount;
            double fibreArea;

            // WITH AParams DO
            if (this.Animal == GrazType.AnimalType.Cattle)
                this.coatDepth = 1.0;
            else
            {
                fibreCount = this.Genotype.WoolC[11] * this.Genotype.ChillC[1] * Math.Pow(this.normalWeight, 2.0 / 3.0);
                fibreArea = Math.PI / 4.0 * Math.Pow(this.FibreDiam * 1E-6, 2.0);
                this.coatDepth = 100.0 * this.Genotype.WoolC[3] * this.woolWt / (fibreCount * this.Genotype.WoolC[10] * fibreArea);
            }
        }

        /// <summary>
        /// In sheep, the coat depth is used to set the total wool weight (this is the  
        /// way that shearing is done)                                                  
        /// </summary>
        /// <param name="coatDepth">Coat depth for which a greasy wool weight is to be calculated (cm)</param>
        /// <returns>Wool weight</returns>
        private double CoatDepth2Wool(double coatDepth)
        {
            double fibreCount;
            double fibreArea;

            if (this.Animal == GrazType.AnimalType.Sheep)
            {
                fibreCount = Genotype.WoolC[11] * Genotype.ChillC[1] * Math.Pow(this.normalWeight, 2.0 / 3.0);
                fibreArea = Math.PI / 4.0 * Math.Pow(FibreDiam * 1E-6, 2);
                return (fibreCount * Genotype.WoolC[10] * fibreArea) * coatDepth / (100.0 * Genotype.WoolC[3]);
            }
            else
                return 0.0;
        }

        /// <summary>
        /// Get the conception rates array
        /// </summary>
        /// <returns>Conception rates</returns>
        private double[] GetConceptionRates()
        {
            const double STD_LATITUDE = -35.0;      // Latitude (in degrees) for which the DayLengthConst[] parameters are set    
            int iDOY;
            double fDLFactor;
            double fPropn;
            int N;

            double[] result = new double[4];        // TConceptionArray

            iDOY = clock.Today.DayOfYear;
            fDLFactor = (1.0 - Math.Sin(GrazEnv.DAY2RAD * (iDOY + 10)))
                         * Math.Sin(GrazEnv.DEG2RAD * weather.Latitude) / Math.Sin(GrazEnv.DEG2RAD * STD_LATITUDE);
            for (N = 1; N <= Genotype.MaxYoung; N++)                              // First we calculate the proportion of   
            {                                                                    // females with at least N young          
                if (Genotype.ConceiveSigs[N][0] < 5.0)
                    fPropn = StdMath.DIM(1.0, Genotype.DayLengthConst[N] * fDLFactor)
                              * StdMath.SIG(RelativeSize * BodyCondition, Genotype.ConceiveSigs[N]);
                else
                    fPropn = 0.0;

                if (N == 1)
                    result[N] = fPropn;
                else
                {
                    result[N] = fPropn * result[N - 1];
                    result[N - 1] = result[N - 1] - result[N];
                }
            }

            for (N = 1; N <= Genotype.MaxYoung - 1; N++)
            {
                result[N] = StdMath.DIM(result[N], result[N + 1]);
            }

            return result;
        }

        /// <summary>
        /// Make the animals pregnant
        /// </summary>
        /// <param name="conceptionRate">Conception rates</param>
        /// <param name="newGroups">The new animal groups</param>
        private void MakePregnantAnimals(double[] conceptionRate, ref List<AnimalGroup> newGroups)
        {
            int initialNumber;
            DifferenceRecord fertileDiff;
            AnimalGroup pregGroup;
            int numPreg, n;

            // A weight differential between conceiving and barren animals
            fertileDiff = new DifferenceRecord() { StdRefWt = NODIFF.StdRefWt, BaseWeight = NODIFF.BaseWeight, FleeceWt = NODIFF.FleeceWt };
            fertileDiff.BaseWeight = Genotype.FertWtDiff;

            initialNumber = NoAnimals;
            for (n = 1; n <= Genotype.MaxYoung; n++)
            {
                numPreg = Math.Min(NoAnimals, randFactory.RndPropn(initialNumber, conceptionRate[n]));
                pregGroup = Split(numPreg, false, fertileDiff, NODIFF);
                if (pregGroup != null)
                {
                    pregGroup.Pregnancy = 1;
                    pregGroup.NoFoetuses = n;
                    CheckAnimList(ref newGroups);
                    newGroups.Add(pregGroup);
                }
            }
        }

        /// <summary>
        /// Used in createYoung() to set up the genotypic parameters of the lambs     
        /// or calves that are about to be born/created.                              
        /// </summary>
        /// <returns></returns>
        private Genotype ConstructOffspringParams()
        {
            if (matedToGenotypeParameters != null)
            {
                return new Genotype(null, Genotype, matedToGenotypeParameters, 0.5, 0.5);
            }
            else
                return new Genotype( Genotype);
        }

        /// <summary>
        ///  Carry out one cycle's worth of conceptions                                
        /// </summary>
        /// <param name="newGroups"></param>
        private void Conceive(ref List<AnimalGroup> newGroups)
        {
            if ((ReproState == GrazType.ReproType.Empty)
               && (!((this.Genotype.Animal == GrazType.AnimalType.Sheep) && (lactStatus == GrazType.LactType.Lactating)))
               && (mateCycle == 0))
                MakePregnantAnimals(GetConceptionRates(), ref newGroups);
        }

        /// <summary>
        /// Death rate calculation
        /// </summary>
        /// <returns>The death rate</returns>
        private double DeathRateFunc()
        {
            double growthRate;
            double deltaNormalWt;
            double result;

            growthRate = this.Genotype.GrowthC[1] / Math.Pow(standardReferenceWeight, Genotype.GrowthC[2]);
            deltaNormalWt = (standardReferenceWeight - birthWeight) * (Math.Exp(-growthRate * (AgeDays - 1)) - Math.Exp(-growthRate * AgeDays));

            result = 1.0 - ExpectedSurvival(1);
            if ((lactStatus != GrazType.LactType.Suckling) && (this.BodyCondition < this.Genotype.MortCondConst) && (WeightChange < 0.2 * deltaNormalWt))
                result = result + this.Genotype.MortIntensity * (this.Genotype.MortCondConst - this.BodyCondition);
            return result;
        }

        /// <summary>
        /// Exposure calculations
        /// </summary>
        /// <returns>Exposure value</returns>
        private double ExposureFunc()
        {
            double exposureOdds;
            double exp_ExpOdds;
            double result;

            exposureOdds = this.Genotype.ExposureConsts[0] - this.Genotype.ExposureConsts[1] * BodyCondition + this.Genotype.ExposureConsts[2] * chillIndex;
            if (NoOffspring > 1)
                exposureOdds = exposureOdds + this.Genotype.ExposureConsts[3];
            exp_ExpOdds = Math.Exp(exposureOdds);
            result = exp_ExpOdds / (1.0 + exp_ExpOdds);
            return result;
        }

        /// <summary>
        /// Mortality submodel                                                        
        /// </summary>
        /// <param name="chill"></param>
        /// <param name="newGroups"></param>
        private void Kill(double chill, ref List<AnimalGroup> newGroups)
        {
            double deathRate;
            DifferenceRecord Diffs;
            int maleLosses;
            int femaleLosses;
            int NoLosses;
            int YoungLosses;
            int YoungToKill;
            AnimalGroup DeadGroup;
            AnimalGroup SplitGroup;

            Diffs = new DifferenceRecord() { StdRefWt = NODIFF.StdRefWt, BaseWeight = NODIFF.BaseWeight, FleeceWt = NODIFF.FleeceWt };
            Diffs.BaseWeight = -Genotype.MortWtDiff * BaseWeight;

            deathRate = DeathRateFunc();
            femaleLosses = randFactory.RndPropn(FemaleNo, deathRate);
            maleLosses = randFactory.RndPropn(MaleNo, deathRate);
            NoLosses = maleLosses + femaleLosses;
            if ((Animal == GrazType.AnimalType.Sheep) && (Young != null) && (Young.AgeDays == 1))
                YoungLosses = randFactory.RndPropn(Young.NoAnimals, ExposureFunc());
            else
                YoungLosses = 0;
            Deaths = NoLosses;
            if ((Young == null) && (NoLosses > 0))
                SplitSex(maleLosses, femaleLosses, false, Diffs);

            else if ((Young != null) && (femaleLosses + YoungLosses > 0))
            {
                if (femaleLosses > 0)                                               // For now, unweaned young of dying animals 
                {                                                                   //   die with them                       
                    DeadGroup = Split(femaleLosses, false, Diffs, NODIFF);
                    YoungToKill = StdMath.IDIM(YoungLosses, DeadGroup.Young.NoAnimals);
                    DeadGroup = null;
                }
                else
                    YoungToKill = YoungLosses;

                if (YoungToKill > 0)                                                // Any further young to kill are removed as 
                {                                                                   //   evenly as possible from their mothers  
                    if (FemaleNo > 0)
                        LoseYoung(this, YoungToKill / FemaleNo);
                    if (YoungToKill % FemaleNo > 0)
                    {
                        SplitGroup = Split(YoungToKill % FemaleNo, false, NODIFF, NODIFF);
                        LoseYoung(SplitGroup, 1);
                        CheckAnimList(ref newGroups);
                        newGroups.Add(SplitGroup);
                    }
                } //// _ IF (YoungToKill > 0) 
            } //// ELSE IF (Young <> NIL) and (NoLosses + YoungLosses > 0) 
        }

        /// <summary>
        /// Decrease the number of young by N per mother                               
        /// </summary>
        /// <param name="animalGrp"></param>
        /// <param name="number">Number of animals</param>
        private void LoseYoung(AnimalGroup animalGrp, int number)
        {
            DifferenceRecord YoungDiffs;
            int iMaleYoung;
            int iFemaleYoung;
            int iYoungToLose;
            int iMalesToLose;
            int iFemalesToLose;

            if (number == animalGrp.NoOffspring)
            {
                animalGrp.Young = null;
                animalGrp.SetNoOffspring(0);
            }
            else if (number > 0)
            {
                YoungDiffs = new DifferenceRecord() { StdRefWt = NODIFF.StdRefWt, BaseWeight = NODIFF.BaseWeight, FleeceWt = NODIFF.FleeceWt };
                YoungDiffs.BaseWeight = -animalGrp.Young.Genotype.MortWtDiff * animalGrp.Young.BaseWeight;

                iMaleYoung = animalGrp.Young.MaleNo;
                iFemaleYoung = animalGrp.Young.FemaleNo;
                iYoungToLose = number * animalGrp.FemaleNo;

                iMalesToLose = Convert.ToInt32(Math.Round(iYoungToLose * StdMath.XDiv(iMaleYoung, iMaleYoung + iFemaleYoung)), CultureInfo.InvariantCulture);
                iMalesToLose = Math.Min(iMalesToLose, iMaleYoung);

                iFemalesToLose = iYoungToLose - iMalesToLose;
                if (iFemalesToLose > iFemaleYoung)
                {
                    iMalesToLose += iFemalesToLose - iFemaleYoung;
                    iFemalesToLose = iFemaleYoung;
                }

                animalGrp.Young.SplitSex(iMalesToLose, iFemalesToLose, false, YoungDiffs);
                animalGrp.numberOffspring -= number;
                animalGrp.Young.numberOffspring -= number;
            }
        }

        /// <summary>
        /// Pregnancy toxaemia and dystokia                                           
        /// </summary>
        /// <param name="newGroups">The new groups</param>
        private void KillEndPreg(ref List<AnimalGroup> newGroups)
        {
            double DystokiaRate;
            double ToxaemiaRate;
            AnimalGroup DystGroup;
            int numLosses;

            if ((Animal == GrazType.AnimalType.Sheep) && (foetalAge == Genotype.Gestation - 1))
                if (NoFoetuses == 1)                                                    // Calculate loss of young due to           
                {                                                                       // dystokia and move the corresponding      
                    DystokiaRate = StdMath.SIG((FoetalWeight / Genotype.StdBirthWt(1)) *     // number of mothers into a new animal      
                                           Math.Max(RelativeSize, 1.0),                         // group                                    
                                         Genotype.DystokiaSigs);
                    numLosses = randFactory.RndPropn(FemaleNo, DystokiaRate);
                    if (numLosses > 0)
                    {
                        DystGroup = Split(numLosses, false, NODIFF, NODIFF);
                        DystGroup.Pregnancy = 0;
                        CheckAnimList(ref newGroups);
                        newGroups.Add(DystGroup);
                    } ////  IF (NoLosses > 0)
                } //// IF (NoYoung = 1) 

                else if (NoFoetuses >= 2)                                          // Deaths of sheep with multiple young      
                {                                                                  // due to pregnancy toxaemia              
                    ToxaemiaRate = StdMath.SIG((midLatePregWeight - BaseWeight) / normalWeight,
                                         Genotype.ToxaemiaSigs);
                    numLosses = randFactory.RndPropn(FemaleNo, ToxaemiaRate);
                    Deaths += numLosses;
                    if (numLosses > 0)
                        Split(numLosses, false, NODIFF, NODIFF);
                } //// ELSE IF (NoFoetuses >= 2) 
        }

        /// <summary>
        /// Automatic end to lactation in response to reduced milk production         
        /// </summary>
        /// <returns></returns>
        private bool YoungStopSuckling()
        {
            return ((Young != null)
                      && (Young.lactStatus == GrazType.LactType.Suckling)
                      && (Young.AgeDays >= 7)
                      && (MilkYield / NoSuckling() < Genotype.SelfWeanPropn
                                                    * (Young.AnimalState.PaddockIntake.Biomass
                                                       + Young.AnimalState.SuppIntake.Biomass)));
        }

        /// <summary>
        /// Number of offspring that are actually suckling
        /// </summary>
        /// <returns>The number of suckling young</returns>
        private int NoSuckling()
        {
            if ((Young != null) && (Young.lactStatus == GrazType.LactType.Suckling))
                return NoOffspring;
            else
                return 0;
        }
        
        /// <summary>
        /// TODO: check that this function returns changed values
        /// </summary>
        /// <param name="ag"></param>
        /// <param name="x"></param>
        /// <param name="diffs"></param>
        private void AdjustRecords(AnimalGroup ag, double x, DifferenceRecord diffs)
        {
            ag.BaseWeight = ag.BaseWeight + x * diffs.BaseWeight;
            if (Genotype.Animal == GrazType.AnimalType.Sheep)
                ag.woolWt = ag.woolWt + x * diffs.FleeceWt;
            ag.standardReferenceWeight = ag.standardReferenceWeight + x * diffs.StdRefWt;
            ag.CalculateWeights();
            ag.totalWeight = ag.BaseWeight + ag.ConceptusWt();                            // TotalWeight is meant to be the weight  
            if (Genotype.Animal == GrazType.AnimalType.Sheep)                              // "on the scales", including conceptus   
                ag.totalWeight = ag.totalWeight + ag.woolWt;                              // and/or fleece.                         
        }

        /// <summary>
        /// Used by the public Split function
        /// </summary>
        /// <param name="numberMales"></param>
        /// <param name="numberFemales"></param>
        /// <param name="byAge"></param>
        /// <param name="diffs"></param>
        /// <returns></returns>
        private AnimalGroup SplitSex(int numberMales, int numberFemales, bool byAge, DifferenceRecord diffs)
        {
            double PropnGoing;

            if ((numberMales > MaleNo) || (numberFemales > FemaleNo))
                throw new Exception("AnimalGroup: Error in SplitSex method");

            AnimalGroup Result = Copy();                                                 // Create the new animal group              
            if ((numberMales == MaleNo) && (numberFemales == FemaleNo))
            {
                MaleNo = 0;
                FemaleNo = 0;
                ages.Clear();
            }
            else
            {
                PropnGoing = StdMath.XDiv(numberMales + numberFemales, MaleNo + FemaleNo);        // Adjust weights etc                       
                AdjustRecords(this, -PropnGoing, diffs);
                AdjustRecords(Result, 1.0 - PropnGoing, diffs);

                Result.MaleNo = numberMales;                                                 // Set up numbers in the two groups and     
                Result.FemaleNo = numberFemales;                                             // split up the age list                  
                Result.ages = this.ages.Split(numberMales, numberFemales, byAge);
                Result.AgeDays = Result.ages.MeanAge();

                this.MaleNo = this.MaleNo - numberMales;
                this.FemaleNo = this.FemaleNo - numberFemales;
                this.AgeDays = this.ages.MeanAge();
            }
            return Result;
        }

        /// <summary>
        /// Get the total number of females and males
        /// </summary>
        /// <returns>Total number of animals</returns>
        private int GetNoAnimals()
        {
            return this.MaleNo + this.FemaleNo;
        }

        /// <summary>
        /// Set the number of animals
        /// </summary>
        /// <param name="count">Number of animals</param>
        private void SetNoAnimals(int count)
        {
            if (this.mothers != null)
            {
                this.MaleNo = count / 2;
                this.FemaleNo = count - this.MaleNo;
            }
            else if ((this.ReproState == GrazType.ReproType.Male) || (this.ReproState == GrazType.ReproType.Castrated))
            {
                this.MaleNo = count;
                this.FemaleNo = 0;
            }
            else
            {
                this.MaleNo = 0;
                this.FemaleNo = count;
            }

            if (this.ages.Count == 0)
                ages.Input(this.AgeDays, this.MaleNo, this.FemaleNo);
            else
                ages.Resize(this.MaleNo, this.FemaleNo);
        }

        /// <summary>
        /// Set the live weight
        /// </summary>
        /// <param name="liveWeight">Live weight</param>
        private void SetLiveWt(double liveWeight)
        {
            BaseWeight = liveWeight - ConceptusWt() - woolWt;
            totalWeight = liveWeight;
            CalculateWeights();
        }

        /// <summary>
        /// Weight of fleece that would be cut if the animals were shorn (kg greasy) 
        /// </summary>
        /// <returns></returns>
        private double GetFleeceCutWt()
        {
            return StdUnits.StdMath.DIM(woolWt, CoatDepth2Wool(stubble_mm));
        }

        /// <summary>
        /// Set the weight of fleece
        /// </summary>
        /// <param name="gfw"></param>
        private void SetFleeceCutWt(double gfw)
        {
            SetWoolWt(CoatDepth2Wool(stubble_mm) + Math.Max(gfw, 0.0));
        }

        /// <summary>
        /// Total weight of wool including stubble (kg greasy)                        
        /// </summary>
        /// <param name="woolWeight"></param>
        private void SetWoolWt(double woolWeight)
        {
            woolWt = woolWeight;
            BaseWeight = totalWeight - ConceptusWt() - woolWt;
            CalculateWeights();
        }

        /// <summary>
        /// Set the maximum previous weight
        /// </summary>
        /// <param name="maxPrevWeight"></param>
        private void SetMaxPrevWt(double maxPrevWeight)
        {
            this.maxPrevWeight = maxPrevWeight;
            CalculateWeights();
        }

        /// <summary>
        /// In sheep, the coat depth is used to set the total wool weight 
        /// </summary>
        /// <param name="newCoatDepth">New coat depth (cm)</param>
        private void SetCoatDepth(double newCoatDepth)
        {
            coatDepth = newCoatDepth;
            SetWoolWt(CoatDepth2Wool(newCoatDepth));
        }

        /// <summary>
        /// Set the animal to be mated to
        /// </summary>
        /// <param name="value"></param>
        private void SetMatedTo(Genotype value)
        {
            matedToGenotypeParameters = null;
            if (value == null)
                matedToGenotypeParameters = null;
            else
                matedToGenotypeParameters = value;
        }

        /// <summary>
        /// Set the pregnancy progress
        /// </summary>
        /// <param name="p"></param>
        private void SetPregnancy(int p)
        {
            double ConditionFactor;
            double OldLiveWt;
            int Idx;

            if (p != foetalAge)
            {
                if (p == 0)
                {
                    ReproState = GrazType.ReproType.Empty;                         // Don't re-set the base weight here as     
                    foetalAge = 0;                                                  // this is usually used at birth, where   
                    FoetalWeight = 0.0;                                                 // the conceptus is lost                  
                    midLatePregWeight = 0.0;
                    SetNoFoetuses(0);
                    mateCycle = -1;
                }
                else if (p != 0)
                {
                    OldLiveWt = LiveWeight;                                              // Store live weight                        
                    if (p >= Genotype.Gestation - latePregLength)
                        ReproState = GrazType.ReproType.LatePreg;
                    else
                        ReproState = GrazType.ReproType.EarlyPreg;
                    foetalAge = p;
                    if (NoFoetuses == 0)
                        SetNoFoetuses(1);
                    mateCycle = -1;
                    daysToMate = 0;
                    for (Idx = 1; Idx <= 3; Idx++)                                         // This piece of code estimates the weight  
                    {                                                                      // of the foetus and implicitly the       
                        ConditionFactor = (BodyCondition - 1.0)                                // conceptus while keeping the live       
                                           * FoetalNormWt() / Genotype.StdBirthWt(NoFoetuses);   // weight constant                        
                        if (BodyCondition >= 1.0)
                            FoetalWeight = FoetalNormWt() * (1.0 + ConditionFactor);
                        else
                            FoetalWeight = FoetalNormWt() * (1.0 + Genotype.PregScale[NoFoetuses] * ConditionFactor);
                        LiveWeight = OldLiveWt;
                        CalculateWeights();
                    }
                    if (p >= Genotype.Gestation - latePregLength / 2)
                        midLatePregWeight = BaseWeight;
                }
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="l"></param>
        private void SetLactation(int l)
        {
            // AnimalGroup MyClass;

            if (l != daysLactating)
            {
                if (l == 0)
                {
                    lactStatus = GrazType.LactType.Dry;                                     // Set this before calling setDryoffTime()  
                    if (Young == null)
                        SetDryoffTime(daysLactating, 0, previousOffspring);
                    else                                                                    // This happens when self-weaning occurs    
                    {
                        SetDryoffTime(daysLactating, 0, NoOffspring);
                        Young.lactStatus = GrazType.LactType.Dry;
                    }
                    daysLactating = 0;                                                      // ConditionAtBirthing, PropnOfMaxMilk and  
                }                                                                           // LactAdjust are left at their final values
                else
                {
                    lactStatus = GrazType.LactType.Lactating;
                    if (NoOffspring == 0)
                        SetNoOffspring(1);
                    BirthCondition = BodyCondition;
                    daysLactating = l;
                    dryOffTime = 0.0;
                    lactationAdjustment = 1.0;
                    proportionOfMaxMilk = 1.0;
                    previousOffspring = 0;
                    Young = null;
                    //MyClass = this;                                                         //TODO: not 100% sure this is right
                    Young = new AnimalGroup(this, 0.5 * (GrowthCurve(l, GrazType.ReproType.Male, Genotype)
                                                        + GrowthCurve(l, GrazType.ReproType.Empty, Genotype)),
                                            clock, weather);
                }
                MilkEnergy = 0.0;
                MilkProtein = 0.0;
                MilkYield = 0.0;
                lactationRatio = 1.0;
            }
        }

        /// <summary>
        /// Set the number of foetuses
        /// </summary>
        /// <param name="value"></param>
        private void SetNoFoetuses(int value)
        {
            int iDaysPreg;

            if (value == 0)
            {
                Pregnancy = 0;
                numberFoetuses = 0;
            }
            else if ((value <= Genotype.MaxYoung) && (value != NoFoetuses))
            {
                iDaysPreg = Pregnancy;
                Pregnancy = 0;
                numberFoetuses = value;
                Pregnancy = iDaysPreg;
            }
        }

        /// <summary>
        ///  On creation, lambs and calves are always suckling their mothers. This may 
        /// change in the course of a simulation (see the YoungStopSuckling function) 
        /// </summary>
        /// <param name="value"></param>
        private void SetNoOffspring(int value)
        {
            int iDaysLact;

            if (value != NoOffspring)
            {
                iDaysLact = Lactation;                                                 // Store the current stage of lactation     
                Lactation = 0;
                if (Young != null)
                {
                    Young = null;
                    Young = null;
                }

                numberOffspring = value;

                if (value == 0)
                    Young = null;
                else if (value <= Genotype.MaxYoung)
                    SetLactation(iDaysLact);                                            // This creates a new group of (suckling) lambs or calves  
            }                                                                                                   
        }

        /// <summary>
        /// Return the total faecal carbon and nitrogen an urine nitrogen produced by 
        /// a group of animals.  The values are in kilograms, not kg/head (i.e. they  
        /// are totalled over all animals in the group)                               
        /// </summary>
        /// <returns></returns>
        private GrazType.DM_Pool GetOrgFaeces()
        {
            GrazType.DM_Pool Result = new GrazType.DM_Pool();

            Result = MultiplyDMPool(this.AnimalState.OrgFaeces, NoAnimals);
            if (Young != null)
                Result = AddDMPool(Result, Young.OrgFaeces);
            return Result;
        }

        /// <summary>
        /// Get the inorganic faeces amount
        /// </summary>
        /// <returns></returns>
        private GrazType.DM_Pool GetInOrgFaeces()
        {
            GrazType.DM_Pool Result = new GrazType.DM_Pool();

            Result = MultiplyDMPool(this.AnimalState.InOrgFaeces, NoAnimals);
            if (Young != null)
                Result = AddDMPool(Result, Young.InOrgFaeces);
            return Result;
        }

        /// <summary>
        /// Get the urine amount
        /// </summary>
        /// <returns></returns>
        private GrazType.DM_Pool GetUrine()
        {
            GrazType.DM_Pool Result = MultiplyDMPool(this.AnimalState.Urine, NoAnimals);
            if (Young != null)
                Result = AddDMPool(Result, Young.Urine);
            return Result;
        }

        /// <summary>
        /// Get excretion parameters
        /// </summary>
        /// <returns></returns>
        private ExcretionInfo GetExcretion()
        {
            // these will have to go into the parameter set eventually...
            double[] faecesDensity = { 1000.0, 1000.0 };       // kg/m^3
            // double[] dFaecesMoisture = { 4.0, 5.0 };         // kg water/kg DM
            double[] refNormalWt = { 50.0, 600.0 };            // kg
            double[] faecesRefLength = { 0.012, 0.30 };        // m
            double[] faecesPower = { 0.00, 1.0 / 3.0 };
            double[] faecesWidthToLength = { 0.80, 1.00 };
            double[] faecesHeightToLength = { 0.70, 0.12 };
            double[] faecalMoistureHerbageMin = { 6.0, 7.5 };  // kg water/kg DM
            double[] faecalMoistureSuppMin = { 3.0, 3.0 };
            double[] faecalMoistureMax = { 0.0, 0.0 };
            double[] faecesNO3Propn = { 0.25, 0.25 };
            double[] urineRefLength = { 0.20, 0.60 };          // m
            double[] urineWidthToLength = { 1.00, 1.00 };
            double[] urineRefVolume = { 0.00015, 0.00200 };    // m^3
            double[] dailyUrineRefVol = { 0.0030, 0.0250 };    // m^3/head/d

            double faecalLongAxis;         // metres
            double faecalHeight;           // metres
            double faecalMoistureHerbage;
            double faecalMoistureSupp;
            double faecalFreshWeight;      // kg/head
            double urineLongAxis;          // metres
            double volumePerUrination;     // m^3
            double dailyUrineVolume;       // m^3
            FoodSupplement tempSuppt;

            ExcretionInfo result = new ExcretionInfo();

            result.OrgFaeces = MultiplyDMPool(this.AnimalState.OrgFaeces, NoAnimals);
            result.InOrgFaeces = MultiplyDMPool(this.AnimalState.InOrgFaeces, NoAnimals);
            result.Urine = MultiplyDMPool(this.AnimalState.Urine, NoAnimals);

            // In sheep, we treat each faecal pellet as a separate defaecation.
            // Sheep pellets are assumed to have constant size; cattle pats vary with
            // linear dimension of the animal

            faecalLongAxis = faecesRefLength[(int)Animal] * Math.Pow(normalWeight / refNormalWt[(int)Animal], faecesPower[(int)Animal]);
            faecalHeight = faecalLongAxis * faecesHeightToLength[(int)Animal];

            // Faecal moisture content seems to be lower when animals are not at pasture,
            // so estimate it separately for herbage and supplement components of the diet
            tempSuppt = RationFed.AverageSuppt();
            faecalMoistureHerbage = faecalMoistureHerbageMin[(int)Animal] + (faecalMoistureMax[(int)Animal] - faecalMoistureHerbageMin[(int)Animal]) * this.AnimalState.Digestibility.Herbage;
            faecalMoistureSupp = faecalMoistureSuppMin[(int)Animal] + (faecalMoistureMax[(int)Animal] - faecalMoistureSuppMin[(int)Animal]) * (1.0 - tempSuppt.DMPropn);
            faecalFreshWeight = this.AnimalState.DM_Intake.Herbage * (1.0 - this.AnimalState.Digestibility.Herbage) * (1.0 + faecalMoistureHerbage)
                                      + this.AnimalState.DM_Intake.Supp * (1.0 - this.AnimalState.Digestibility.Supp) * (1.0 + faecalMoistureSupp);
            tempSuppt = null;

            // Defaecations are assumed to be ellipsoidal prisms:
            result.DefaecationEccentricity = Math.Sqrt(1.0 - StdMath.Sqr(faecesWidthToLength[(int)Genotype.Animal]));
            result.DefaecationArea = Math.PI / 4.0 * StdMath.Sqr(faecalLongAxis) * faecesWidthToLength[(int)Genotype.Animal];
            result.DefaecationVolume = result.DefaecationArea * faecalHeight;
            result.Defaecations = NoAnimals * (faecalFreshWeight / faecesDensity[(int)Genotype.Animal]) / result.DefaecationVolume;
            result.FaecalNO3Propn = faecesNO3Propn[(int)Genotype.Animal];

            urineLongAxis = urineRefLength[(int)Animal] * Math.Pow(normalWeight / refNormalWt[(int)Animal], 1.0 / 3.0);
            volumePerUrination = urineRefVolume[(int)Animal] * Math.Pow(normalWeight / refNormalWt[(int)Animal], 1.0);
            dailyUrineVolume = dailyUrineRefVol[(int)Animal] * Math.Pow(normalWeight / refNormalWt[(int)Animal], 1.0);

            // Urinations are assumed to be ellipsoidal
            result.dUrinationEccentricity = Math.Sqrt(1.0 - StdMath.Sqr(urineWidthToLength[(int)Genotype.Animal]));
            result.UrinationArea = Math.PI / 4.0 * StdMath.Sqr(urineLongAxis) * urineWidthToLength[(int)Genotype.Animal];
            result.UrinationVolume = volumePerUrination;
            result.Urinations = NoAnimals * dailyUrineVolume / result.UrinationVolume;

            return result;
        }

        /// <summary>
        /// Get the animal type
        /// </summary>
        /// <returns></returns>
        private GrazType.AnimalType GetAnimal()
        {
            return Genotype.Animal;
        }

        /// <summary>
        /// Get the breed name
        /// </summary>
        /// <returns></returns>
        private string GetBreed()
        {
            return Genotype.Name;
        }

        //TODO: Test this function
        /// <summary>
        /// Get the age class 
        /// </summary>
        /// <returns></returns>
        private GrazType.AgeType GetAgeClass()
        {
            // Array[AnimalType,0..3] of AgeType
            GrazType.AgeType[,] AgeClassMap = new GrazType.AgeType[2, 4]
                                { {GrazType.AgeType.Weaner, GrazType.AgeType.Yearling, GrazType.AgeType.Mature, GrazType.AgeType.Mature},         //sheep
                                {GrazType.AgeType.Weaner, GrazType.AgeType.Yearling, GrazType.AgeType.TwoYrOld, GrazType.AgeType.Mature} };       //cattle
            if ((mothers != null) || (lactStatus == GrazType.LactType.Suckling))
                return GrazType.AgeType.LambCalf;
            else
                return AgeClassMap[(int)Genotype.Animal, Math.Min(AgeDays / 365, 3)];
        }

        /// <summary>
        /// Get the weight of the male
        /// </summary>
        /// <returns></returns>
        private double GetMaleWeight()
        {
            double SRWMale;
            double SRWFemale;
            double MaleNWt;
            double FemaleNWt;
            double GroupNWt;
            double Male2Fem;
            double Result;

            if (MaleNo == 0)
                Result = 0.0;
            else if (FemaleNo == 0)
                Result = LiveWeight;
            else
            {
                SRWFemale = standardReferenceWeight * StdMath.XDiv(NoAnimals, Genotype.SRWScalars[(int)ReproState] * MaleNo + FemaleNo);
                SRWMale = Genotype.SRWScalars[(int)ReproState] * SRWFemale;
                MaleNWt = MaxNormWtFunc(SRWMale, birthWeight, AgeDays, Genotype);
                FemaleNWt = MaxNormWtFunc(SRWFemale, birthWeight, AgeDays, Genotype);
                GroupNWt = StdMath.XDiv(MaleNWt * MaleNo + FemaleNWt * FemaleNo, NoAnimals);
                Male2Fem = 1.0 + (MaleNWt / FemaleNWt - 1.0) * Math.Min(1.0, BaseWeight / GroupNWt) * baseWeightGainSolid;
                Result = LiveWeight * (double)NoAnimals / ((double)MaleNo + (double)FemaleNo / Male2Fem);
            }
            return Result;
        }

        /// <summary>
        /// Get the weight of the female
        /// </summary>
        /// <returns></returns>
        private double GetFemaleWeight()
        {
            if (FemaleNo == 0)
                return 0.0;
            else
                return LiveWeight + (double)MaleNo / (double)FemaleNo * (LiveWeight - MaleWeight);
        }

        /// <summary>
        /// Get the animal DSE's
        /// </summary>
        /// <returns></returns>
        private double GetDSEs()
        {
            const double DSE_REF_MEI = 8.8;                     // ME intake corresponding to 1.0 dry sheep 
            double Result;
            double MEIPerHead;

            MEIPerHead = this.AnimalState.ME_Intake.Solid;
            if (Young != null)
                MEIPerHead = MEIPerHead + NoOffspring * Young.AnimalState.ME_Intake.Solid;
            Result = NoAnimals * MEIPerHead / DSE_REF_MEI;

            return Result;
        }

        /// <summary>
        /// Get the clean fleece weight
        /// </summary>
        /// <returns></returns>
        private double GetCFW() { return FleeceCutWeight * Genotype.WoolC[3]; }
        
        /// <summary>
        /// CleanFleeceGrowth
        /// </summary>
        /// <returns></returns>
        private double GetDeltaCFW()
        {
            return GreasyFleeceGrowth * Genotype.WoolC[3];
        }

        /// <summary>
        /// Get the maximum milk yield
        /// </summary>
        /// <returns>kg</returns>
        private double GetMaxMilkYield()
        {
            if (Lactation == 0)
                return 0.0;
            else
                return StdMath.XDiv(MilkYield, proportionOfMaxMilk);
        }

        /// <summary>
        /// Get the milk volume
        /// </summary>
        /// <returns>Litres</returns>
        private double GetMilkVolume()
        {
            if (Lactation == 0)
                return 0.0;
            else
                return StdMath.XDiv(MilkYield, Genotype.LactC[25]);
        }

        /// <summary>
        /// Get the methane energy
        /// </summary>
        /// <returns>MJ</returns>
        private double GetMethaneEnergy()
        {
            return this.Genotype.MethC[1] * this.AnimalState.DM_Intake.Solid
                      * (this.Genotype.MethC[2] + this.Genotype.MethC[3] * this.AnimalState.ME_2_DM.Solid
                          + (feedingLevel + 1.0) * (this.Genotype.MethC[4] - this.Genotype.MethC[5] * this.AnimalState.ME_2_DM.Solid));
        }

        /// <summary>
        /// Get the methane weight
        /// </summary>
        /// <returns>kg</returns>
        private double GetMethaneWeight()
        {
            return Genotype.MethC[6] * GetMethaneEnergy();
        }

        /// <summary>
        /// GrowthCurve calculates MaxNormalWt (see below) for an animal with the   
        /// default birth weight.                                                   
        /// </summary>
        /// <param name="srw"></param>
        /// <param name="bw"></param>
        /// <param name="ageDays"></param>
        /// <param name="parameters"></param>
        /// <returns>Maximum normal weight</returns>
        private static double MaxNormWtFunc(double srw, double bw,
                                int ageDays,
                                Genotype parameters)
        {
            double GrowthRate;

            GrowthRate = parameters.GrowthC[1] / Math.Pow(srw, parameters.GrowthC[2]);
            return srw - (srw - bw) * Math.Exp(-GrowthRate * ageDays);
        }

        /// <summary>
        /// Normal weight as a function of age and sex                                
        /// </summary>
        /// <param name="ageDays"></param>
        /// <param name="reprdType"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private static double GrowthCurve(int ageDays, GrazType.ReproType reprdType, Genotype parameters)
        {
            double SRW;

            SRW = parameters.BreedSRW;
            if ((reprdType == GrazType.ReproType.Male) || (reprdType == GrazType.ReproType.Castrated))
                SRW = SRW * parameters.SRWScalars[(int)reprdType];                                           // TODO: check indexing here
            return MaxNormWtFunc(SRW, parameters.StdBirthWt(1), ageDays, parameters);
        }

        /// <summary>
        /// Used during construction
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="reproductiveStatus"></param>
        /// <param name="number"></param>
        /// <param name="age"></param>
        /// <param name="liveWeight"></param>
        /// <param name="gfw"></param>
        /// <param name="randomFactory"></param>
        /// <param name="takeParams"></param>
        private void Construct(Genotype parameters,
                                 GrazType.ReproType reproductiveStatus,
                                 int number,
                                 int age,
                                 double liveWeight,
                                 double gfw,                   // NB this is a *fleece* weight             
                                 MyRandom randomFactory,
                                 bool takeParams = false)
        {
            double fWoolAgeFactor;

            randFactory = randomFactory;

            if (takeParams)
                Genotype = parameters;
            else
                Genotype = new Genotype(parameters);

            if ((reproductiveStatus == GrazType.ReproType.Male) || (reproductiveStatus == GrazType.ReproType.Castrated))
            {
                ReproState = reproductiveStatus;
                MaleNo = number;
            }
            else
            {
                ReproState = GrazType.ReproType.Empty;
                FemaleNo = number;
            }
            ComputeSRW();
            implantEffect = 1.0;
            IntakeModifier = 1.0;

            AgeDays = age;                                                          // Age of the animals                       
            ages = new AgeList(randFactory);
            ages.Input(age, MaleNo, FemaleNo);

            RationFed = new SupplementRation();
            IntakeSupplement = new FoodSupplement();

            mateCycle = -1;                                                          // Not recently mated                       

            LiveWeight = liveWeight;
            birthWeight = Math.Min(Genotype.StdBirthWt(1), BaseWeight);
            CalculateWeights();

            if (Animal == GrazType.AnimalType.Sheep)
            {
                FibreDiam = Genotype.MaxFleeceDiam;                                   // Calculation of FleeceCutWeight depends   
                FleeceCutWeight = gfw;                                                // on the values of NormalWt & WoolMicron 

                fWoolAgeFactor = Genotype.WoolC[5] + (1.0 - Genotype.WoolC[5]) * (1.0 - Math.Exp(-Genotype.WoolC[12] * AgeDays));
                GreasyFleeceGrowth = Genotype.FleeceRatio * standardReferenceWeight * fWoolAgeFactor / 365.0;
            }

            CalculateCoatDepth();
            totalWeight = BaseWeight + woolWt;

            if (AgeClass == GrazType.AgeType.Mature)                                  // This will re-calculate size and condition
                SetMaxPrevWt(Math.Max(standardReferenceWeight, BaseWeight));
            else
                SetMaxPrevWt(BaseWeight);

            BirthCondition = BodyCondition;                                         // These terms affect the calculation of  
            proportionOfMaxMilk = 1.0;                                                    // potential intake                     
            lactationAdjustment = 1.0;

            basePhosphorusWeight = BaseWeight * Genotype.PhosC[9];
            baseSulphurWeight = BaseWeight * Genotype.GainC[12] / GrazType.N2Protein * Genotype.SulfC[1];

        }

        /// <summary>
        /// Weighted average of corresponding fields in the two TAnimalGroups.    }
        /// </summary>
        /// <param name="total1"></param>
        /// <param name="total2"></param>
        /// <param name="field1"></param>
        /// <param name="field2"></param>
        private double AverageField(int total1, int total2, double field1, double field2)
        {
            if (total1 + total2 > 0)
                return (field1 * total1 + field2 * total2) / (total1 + total2);    // The result of the averaging process goes "into place"                      
            return field1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="maleScale"></param>
        /// <param name="numMale"></param>
        /// <param name="numFemale"></param>
        /// <returns></returns>
        private double SexAve(double maleScale, int numMale, int numFemale)
        {
            return StdMath.XDiv(maleScale * numMale + numFemale, numMale + numFemale);
        }

        /// <summary>
        /// Split the numbers off the group
        /// </summary>
        /// <param name="newGroups"></param>
        /// <param name="nf"></param>
        /// <param name="nym"></param>
        /// <param name="nyf"></param>
        private void SplitNumbers(ref List<AnimalGroup> newGroups, int nf, int nym, int nyf)
        {
            AnimalGroup TempYoung;
            AnimalGroup SplitGroup;
            AnimalGroup SplitYoung;
            double DiffRatio;
            DifferenceRecord YngDiffs;

            YngDiffs = new DifferenceRecord() { StdRefWt = this.NODIFF.StdRefWt, BaseWeight = this.NODIFF.BaseWeight, FleeceWt = this.NODIFF.FleeceWt };
            //// WITH Young DO
            if ((this.Young.MaleNo > 0) && (this.Young.FemaleNo > 0))
            {
                DiffRatio = (this.SexAve(this.Genotype.SRWScalars[(int)Young.ReproState], nym, nyf)
                              - this.SexAve(this.Genotype.SRWScalars[(int)Young.ReproState], this.Young.MaleNo - nym, this.Young.FemaleNo - nyf))
                            / this.SexAve(this.Genotype.SRWScalars[(int)Young.ReproState], this.Young.MaleNo, this.Young.FemaleNo);
                YngDiffs.StdRefWt = this.standardReferenceWeight * DiffRatio;
                YngDiffs.BaseWeight = this.BaseWeight * DiffRatio;
                YngDiffs.FleeceWt = this.woolWt * DiffRatio;
            }

            TempYoung = this.Young;
            this.Young = null;
            SplitGroup = this.SplitSex(0, nf, false, this.NODIFF);
            SplitYoung = TempYoung.SplitSex(nym, nyf, false, YngDiffs);
            this.Young = TempYoung;
            this.Young.mothers = this;
            SplitGroup.Young = SplitYoung;
            SplitGroup.Young.mothers = SplitGroup;
            this.CheckAnimList(ref newGroups);
            newGroups.Add(SplitGroup);
        }

        /// <summary>
        /// Wood-type function, scaled to give a maximum of 1.0 at time Tmax          
        /// </summary>
        /// <param name="t"></param>
        /// <param name="tmax"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private double WOOD(double t, double tmax, double b)
        {
            return Math.Pow(t / tmax, b) * Math.Exp(b * (1 - t / tmax));
        }

        /// <summary>
        /// Inverse of the WOOD function, evaluated iteratively                       
        /// </summary>
        /// <param name="y"></param>
        /// <param name="tmax"></param>
        /// <param name="b"></param>
        /// <param name="declining"></param>
        private double InverseWOOD(double y, double tmax, double b, bool declining)
        {
            double x0, x1;
            double result;

            if (y <= 0.0)
                result = 0.0;
            else if (y >= 1.0)
                result = tmax;
            else
            {
                if (!declining)                                                   // Initial guess                            
                    x1 = Math.Min(0.99, y);
                else
                    x1 = Math.Max(1.01, Math.Exp(b * (1.0 - y)));

                bool more = true;
                do                                                                 // Newton-Raphson solution                   
                {
                    x0 = x1;
                    x1 = x0 - (1.0 - y / WOOD(x0, 1.0, b)) * x0 / (b * (1.0 - x0));
                    more = !(Math.Abs(x0 - x1) < 1.0E-5);
                }
                while (more);
                result = (x0 + x1) / 2.0 * tmax;
            }
            return result;
        }

        /// <summary>
        /// Set the drying off time
        /// </summary>
        /// <param name="daysSinceBirth">Days since birth</param>
        /// <param name="daysSinceDryoff">Days since drying off</param>
        /// <param name="prevSuckling"></param>
        private void SetDryoffTime(int daysSinceBirth, int daysSinceDryoff, int prevSuckling = 1)
        {
            double lactLength;
            double woodFunc;

            previousOffspring = prevSuckling;

            lactLength = daysSinceBirth - daysSinceDryoff;
            if ((lactStatus != GrazType.LactType.Dry) || (lactLength <= 0))
                dryOffTime = 0.0;
            else if (lactLength >= Genotype.IntakeC[8])
                dryOffTime = lactLength + this.Genotype.IntakeC[19] * daysSinceDryoff;
            else
            {
                woodFunc = WOOD(lactLength, this.Genotype.IntakeC[8], this.Genotype.IntakeC[9]);
                lactLength = InverseWOOD(woodFunc, Genotype.IntakeC[8], this.Genotype.IntakeC[9], true);
                dryOffTime = lactLength + this.Genotype.IntakeC[19] * daysSinceDryoff;
            }
        }

        /// <summary>
        /// Used in GrazFeed to initialise the state variables for which yesterday's  
        /// value must be known in order to get today's calculation                   
        /// </summary>
        /// <param name="prevGroup"></param>
        private void SetUpForYesterday(AnimalGroup prevGroup)
        {
            PotIntake = prevGroup.PotIntake;
            feedingLevel = prevGroup.feedingLevel;
            MilkEnergy = prevGroup.MilkEnergy;
            MilkProtein = prevGroup.MilkProtein;
            proportionOfMaxMilk = prevGroup.proportionOfMaxMilk;
            this.AnimalState.LowerCritTemp = prevGroup.AnimalState.LowerCritTemp;
            if ((Young != null) && (prevGroup.Young != null))
                Young.SetUpForYesterday(prevGroup.Young);
        }

        /// <summary>
        /// Advance the age of the animals
        /// </summary>
        /// <param name="amimalGrp"></param>
        /// <param name="numDays"></param>
        /// <param name="newGroups"></param>
        private void AdvanceAge(AnimalGroup amimalGrp, int numDays, ref List<AnimalGroup> newGroups)
        {
            amimalGrp.AgeDays += numDays;
            amimalGrp.ages.AgeBy(numDays);
            if (amimalGrp.Young != null)
                amimalGrp.Young.Age(numDays, ref newGroups);
            if (amimalGrp.lactStatus == GrazType.LactType.Lactating)
                amimalGrp.daysLactating += numDays;
            else if (amimalGrp.dryOffTime > 0.0)
                amimalGrp.dryOffTime = amimalGrp.dryOffTime + Genotype.IntakeC[19] * numDays;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="full"></param>
        /// <param name="fullDenom"></param>
        /// <param name="ts"></param>
        /// <param name="tsDenom"></param>
        /// <param name="dt"></param>
        private void UpdateAve(ref double full, double fullDenom, double ts, double tsDenom, double dt)
        {
            full = StdMath.XDiv(full * fullDenom + ts * (dt * tsDenom), fullDenom + (dt * tsDenom));
        }

        /// <summary>
        /// Update the grazing outputs
        /// </summary>
        /// <param name="timeStep"></param>
        /// <param name="full"></param>
        /// <param name="ts"></param>
        private void UpdateGrazingOutputs(double timeStep, ref GrazType.GrazingOutputs full, GrazType.GrazingOutputs ts)
        {
            int I, Sp;

            for (I = 1; I <= GrazType.DigClassNo; I++)
                full.Herbage[I] = full.Herbage[I] + timeStep * ts.Herbage[I];
            for (Sp = 1; Sp <= GrazType.MaxPlantSpp; Sp++)
            {
                for (I = GrazType.UNRIPE; I <= GrazType.RIPE; I++)
                    full.Seed[Sp, I] = full.Seed[Sp, I] + timeStep * ts.Seed[Sp, I];
            }
        }

        /// <summary>
        /// Update the intake record
        /// </summary>
        /// <param name="full"></param>
        /// <param name="ts"></param>
        /// <param name="dt"></param>
        private void UpdateIntakeRecord(ref GrazType.IntakeRecord full, GrazType.IntakeRecord ts, double dt)
        {
            full.Digestibility = StdMath.XDiv(full.Digestibility * full.Biomass +
                                       ts.Digestibility * (dt * ts.Biomass),
                                     full.Biomass + dt * ts.Biomass);
            full.CrudeProtein = StdMath.XDiv(full.CrudeProtein * full.Biomass +
                                       ts.CrudeProtein * (dt * ts.Biomass),
                                     full.Biomass + dt * ts.Biomass);
            full.Degradability = StdMath.XDiv(full.Degradability * (full.CrudeProtein * full.Biomass) +
                                       ts.Degradability * (ts.CrudeProtein * dt * ts.Biomass),
                                     full.CrudeProtein * full.Biomass +
                                       dt * ts.CrudeProtein * ts.Biomass);
            full.HeightRatio = StdMath.XDiv(full.HeightRatio * full.Biomass +
                                       ts.HeightRatio * (dt * ts.Biomass),
                                     full.Biomass + dt * ts.Biomass);
            full.Biomass = full.Biomass + dt * ts.Biomass;
        }

        /// <summary>
        /// Update the diet record
        /// </summary>
        /// <param name="timeStep">The fraction of the timestep</param>
        /// <param name="suppFullDay"></param>
        /// <param name="full">The full diet</param>
        /// <param name="ts">The extra diet</param>
        private void UpdateDietRecord(double timeStep, bool suppFullDay, ref Diet full, Diet ts)
        {
            full.Herbage = full.Herbage + timeStep * ts.Herbage;
            if (suppFullDay)
                full.Supp = full.Supp + ts.Supp;
            else
                full.Supp = full.Supp + timeStep * ts.Supp;
            full.Milk = full.Milk + timeStep * ts.Milk;
            full.Solid = full.Herbage + full.Supp;
            full.Total = full.Solid + full.Milk;
        }

        /// <summary>
        /// Update the diet
        /// </summary>
        /// <param name="full">The full diet</param>
        /// <param name="fullDenom"></param>
        /// <param name="ts"></param>
        /// <param name="tsDenom"></param>
        /// <param name="herbDT"></param>
        /// <param name="suppDT"></param>
        private void UpdateDietAve(ref Diet full, Diet fullDenom, Diet ts, Diet tsDenom, double herbDT, double suppDT)
        {
            this.UpdateAve(ref full.Herbage, fullDenom.Herbage, ts.Herbage, tsDenom.Herbage, herbDT);
            this.UpdateAve(ref full.Supp, fullDenom.Supp, ts.Supp, tsDenom.Supp, suppDT);
        }

        /// <summary>
        /// Update the animal state
        /// </summary>
        /// <param name="timeStep"></param>
        /// <param name="suppFullDay"></param>
        /// <param name="suppRI"></param>
        private void UpdateAnimalState(double timeStep, bool suppFullDay, double suppRI)
        {
            double suppTS;

            if (suppFullDay)
                suppTS = 1.0;
            else
                suppTS = timeStep;

            if (timeStep == 1)
                this.AnimalState = this.timeStepState.Copy();
            else
            {
                this.UpdateGrazingOutputs(timeStep, ref this.AnimalState.IntakePerHead, this.timeStepState.IntakePerHead);
                this.UpdateIntakeRecord(ref this.AnimalState.PaddockIntake, this.timeStepState.PaddockIntake, timeStep);
                this.UpdateIntakeRecord(ref this.AnimalState.SuppIntake, this.timeStepState.SuppIntake, suppTS);

                // compute these averages *before* cumulating DM_Intake & CP_Intake 

                this.UpdateDietAve(ref this.AnimalState.Digestibility, this.AnimalState.DM_Intake,
                               this.timeStepState.Digestibility, this.timeStepState.DM_Intake,
                               timeStep, suppTS);
                this.UpdateDietAve(ref this.AnimalState.ProteinConc, this.AnimalState.DM_Intake,
                               this.timeStepState.ProteinConc, this.timeStepState.DM_Intake,
                               timeStep, suppTS);
                this.UpdateDietAve(ref this.AnimalState.ME_2_DM, this.AnimalState.DM_Intake,
                               this.timeStepState.ME_2_DM, this.timeStepState.DM_Intake,
                               timeStep, suppTS);
                this.UpdateDietAve(ref this.AnimalState.CorrDgProt, this.AnimalState.CP_Intake,
                               this.timeStepState.CorrDgProt, this.timeStepState.CP_Intake,
                               timeStep, suppTS);

                this.UpdateDietRecord(timeStep, suppFullDay, ref this.AnimalState.CP_Intake, this.timeStepState.CP_Intake);
                this.UpdateDietRecord(timeStep, suppFullDay, ref this.AnimalState.ME_Intake, this.timeStepState.ME_Intake);
                this.UpdateDietRecord(timeStep, suppFullDay, ref this.AnimalState.DM_Intake, this.timeStepState.DM_Intake);

                // compute these averages *after* cumulating DM_Intake & CP_Intake
                this.AnimalState.Digestibility.Solid = StdMath.XDiv(this.AnimalState.Digestibility.Supp * this.AnimalState.DM_Intake.Supp +
                                             this.AnimalState.Digestibility.Herbage * this.AnimalState.DM_Intake.Herbage,
                                             this.AnimalState.DM_Intake.Solid);
                this.AnimalState.ProteinConc.Solid = StdMath.XDiv(this.AnimalState.CP_Intake.Solid, this.AnimalState.DM_Intake.Solid);
                this.AnimalState.ME_2_DM.Solid = StdMath.XDiv(this.AnimalState.ME_Intake.Solid, this.AnimalState.DM_Intake.Solid);
            }   ////_ WITH FullState _

            this.SupplementFreshWeightIntake = this.SupplementFreshWeightIntake + suppTS * StdMath.XDiv(this.PotIntake * suppRI, this.IntakeSupplement.DMPropn);
            for (int idx = 0; idx < this.netSupplementDMI.Length; idx++)
                this.netSupplementDMI[idx] = this.netSupplementDMI[idx] + suppTS * this.timeStepNetSupplementDMI[idx];
        }

        /// <summary>
        /// Compute proportional contribution of diet components (milk, fodder and      
        /// supplement) and the efficiencies of energy use                            
        /// This procedure corresponds to section 5 of the model specification        
        /// </summary>
        private void Efficiencies()
        {
            double herbageEfficiency;                                                       // Efficiencies for gain from herbage &     
            double suppEfficiency;                                                          // supplement intake                      

            this.AnimalState.DietPropn.Milk = StdMath.XDiv(this.AnimalState.ME_Intake.Milk, this.AnimalState.ME_Intake.Total);
            this.AnimalState.DietPropn.Solid = 1.0 - this.AnimalState.DietPropn.Milk;
            this.AnimalState.DietPropn.Supp = this.AnimalState.DietPropn.Solid * StdMath.XDiv(this.AnimalState.ME_Intake.Supp, this.AnimalState.ME_Intake.Solid);
            this.AnimalState.DietPropn.Herbage = this.AnimalState.DietPropn.Solid - this.AnimalState.DietPropn.Supp;

            if (this.AnimalState.ME_Intake.Total < GrazType.VerySmall)                             
            {
                // Efficiencies of various uses of ME
                this.AnimalState.Efficiency.Maint = this.Genotype.EfficC[4];
                this.AnimalState.Efficiency.Lact = this.Genotype.EfficC[7];
                this.AnimalState.Efficiency.Preg = this.Genotype.EfficC[8];
            }
            else
            {
                this.AnimalState.Efficiency.Maint = this.AnimalState.DietPropn.Solid * (this.Genotype.EfficC[1] + this.Genotype.EfficC[2] * this.AnimalState.ME_2_DM.Solid) +
                                    this.AnimalState.DietPropn.Milk * this.Genotype.EfficC[3];
                this.AnimalState.Efficiency.Lact = this.Genotype.EfficC[5] + this.Genotype.EfficC[6] * this.AnimalState.ME_2_DM.Solid;
                this.AnimalState.Efficiency.Preg = this.Genotype.EfficC[8];
            }

            herbageEfficiency = this.Genotype.EfficC[13]
                                 * (1.0 + this.Genotype.EfficC[14] * this.Herbage.LegumePropn)
                                 * (1.0 + this.Genotype.EfficC[15] * (weather.Latitude / 40.0) * Math.Sin(GrazEnv.DAY2RAD * clock.Today.DayOfYear))
                                 * this.AnimalState.ME_2_DM.Herbage;
            suppEfficiency = this.Genotype.EfficC[16] * this.AnimalState.ME_2_DM.Supp;
            this.AnimalState.Efficiency.Gain = this.AnimalState.DietPropn.Herbage * herbageEfficiency
                                 + this.AnimalState.DietPropn.Supp * suppEfficiency
                                 + this.AnimalState.DietPropn.Milk * this.Genotype.EfficC[12];
        }

        /// <summary>
        /// Basal metabolism routine.  Outputs (EnergyUse.Metab,EnergyUse.Maint,      
        /// ProteinUse.Maint) are stored in AnimalState.                              
        /// </summary>
        private void ComputeMaintenance()
        {
            double metabScale;
            double grazeMoved_KM;       // Distance walked during grazing (km)      
            double eatingEnergy;        // Energy requirement for grazing           
            double movingEnergy;        // Energy requirement for movement          
            double endoUrineN;

            if (this.lactStatus == GrazType.LactType.Suckling)
                metabScale = 1.0 + this.Genotype.MaintC[5] * this.AnimalState.DietPropn.Milk;
            else if ((this.ReproState == GrazType.ReproType.Male) && (this.AgeDays >= this.Genotype.Puberty[1]))               // Puberty[true]
                metabScale = 1.0 + this.Genotype.MaintC[15];
            else
                metabScale = 1.0;
            this.AnimalState.EnergyUse.Metab = metabScale * this.Genotype.MaintC[2] * Math.Pow(this.BaseWeight, 0.75) // Basal metabolism                         
                               * Math.Max(Math.Exp(-this.Genotype.MaintC[3] * this.AgeDays), this.Genotype.MaintC[4]);

            eatingEnergy = this.Genotype.MaintC[6] * this.BaseWeight * this.AnimalState.DM_Intake.Herbage             // Work of eating fibrous diets             
                                         * StdMath.DIM(this.Genotype.MaintC[7], this.AnimalState.Digestibility.Herbage);

            if (Herbage.TotalGreen > 100.0)                                                                      // Energy requirement for movement          
                grazeMoved_KM = 1.0 / (this.Genotype.MaintC[8] * this.Herbage.TotalGreen + this.Genotype.MaintC[9]);
            else if (Herbage.TotalDead > 100.0)
                grazeMoved_KM = 1.0 / (this.Genotype.MaintC[8] * this.Herbage.TotalDead + this.Genotype.MaintC[9]);
            else
                grazeMoved_KM = 0.0;
            if (this.AnimalsPerHa > this.Genotype.MaintC[17])
                grazeMoved_KM = grazeMoved_KM * (this.Genotype.MaintC[17] / this.AnimalsPerHa);

            movingEnergy = this.Genotype.MaintC[16] * this.LiveWeight * this.PaddSteep * (grazeMoved_KM + this.DistanceWalked);

            this.AnimalState.EnergyUse.Maint = (this.AnimalState.EnergyUse.Metab + eatingEnergy + movingEnergy) / this.AnimalState.Efficiency.Maint
                               + this.Genotype.MaintC[1] * this.AnimalState.ME_Intake.Total;
            this.feedingLevel = this.AnimalState.ME_Intake.Total / this.AnimalState.EnergyUse.Maint - 1.0;

            //// ...........................................................................  MAINTENANCE PROTEIN REQUIREMENT          

            this.AnimalState.EndoFaeces.Nu[(int)GrazType.TOMElement.n] = (this.Genotype.MaintC[10] * this.AnimalState.DM_Intake.Solid + this.Genotype.MaintC[11] * this.AnimalState.ME_Intake.Milk) / GrazType.N2Protein;

            if (this.Animal == GrazType.AnimalType.Cattle)
            {
                endoUrineN = (this.Genotype.MaintC[12] * Math.Log(this.BaseWeight) - this.Genotype.MaintC[13]) / GrazType.N2Protein;
                this.AnimalState.DermalNLoss = this.Genotype.MaintC[14] * Math.Pow(this.BaseWeight, 0.75) / GrazType.N2Protein;
            }
            else  
            {
                // sheep
                endoUrineN = (this.Genotype.MaintC[12] * this.BaseWeight + this.Genotype.MaintC[13]) / GrazType.N2Protein;
                this.AnimalState.DermalNLoss = 0.0;
            }
            this.AnimalState.ProteinUse.Maint = GrazType.N2Protein * (this.AnimalState.EndoFaeces.Nu[(int)GrazType.TOMElement.n] + endoUrineN + this.AnimalState.DermalNLoss);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="isRoughage"></param>
        /// <param name="cp"></param>
        /// <param name="dg"></param>
        /// <param name="adip2Cp"></param>
        /// <returns></returns>
        private double DUDPFunc(bool isRoughage, double cp, double dg, double adip2Cp)
        {
            double result;
            if (isRoughage)
                result = Math.Max(this.Genotype.ProtC[1], Math.Min(this.Genotype.ProtC[3] * cp - this.Genotype.ProtC[4], this.Genotype.ProtC[2]));
            else if (dg >= 1.0)
                result = 0.0;
            else
                result = this.Genotype.ProtC[9] * (1.0 - adip2Cp / (1.0 - dg));
            return result;
        }

        /// <summary>
        /// Compute microbial crude protein and DPLS
        /// </summary>
        private void ComputeDPLS()
        {
            Diet UDPIntakes = new Diet();
            Diet DUDP = new Diet();
            double dgCorrect;
            int idx;

            this.ComputeRDP(1.0, this.feedingLevel,
                        ref this.AnimalState.CorrDgProt, ref this.AnimalState.RDP_Intake, ref this.AnimalState.RDP_Reqd, ref UDPIntakes);
            this.AnimalState.UDP_Intake = UDPIntakes.Solid + UDPIntakes.Milk;
            dgCorrect = StdMath.XDiv(this.AnimalState.CorrDgProt.Supp, this.AnimalState.SuppIntake.Degradability);

            this.AnimalState.MicrobialCP = this.Genotype.ProtC[6] * this.AnimalState.RDP_Reqd;                       // Microbial crude protein synthesis        

            DUDP.Milk = Genotype.ProtC[5];
            DUDP.Herbage = DUDPFunc(true, this.AnimalState.ProteinConc.Herbage, this.AnimalState.CorrDgProt.Herbage, 0.0);
            DUDP.Supp = 0.0;
            for (idx = 0; idx <= RationFed.Count - 1; idx++)
                DUDP.Supp = DUDP.Supp + StdMath.XDiv(this.netSupplementDMI[idx], this.AnimalState.DM_Intake.Supp)        // Fraction of net supplement intake        
                                          * this.DUDPFunc(this.RationFed[idx].IsRoughage,                           // DUDP of this part of the ration          
                                                      this.RationFed[idx].CrudeProt,
                                                      this.RationFed[idx].DegProt * dgCorrect,
                                                      this.RationFed[idx].ADIP2CP);

            this.AnimalState.DPLS_MCP = this.Genotype.ProtC[7] * this.AnimalState.MicrobialCP;                            // DPLS from microbial crude protein        
            this.AnimalState.DPLS_Milk = DUDP.Milk * UDPIntakes.Milk;                                                    // Store DPLS from milk separately          
            this.AnimalState.DPLS = DUDP.Herbage * UDPIntakes.Herbage
                            + DUDP.Supp * UDPIntakes.Supp
                            + this.AnimalState.DPLS_Milk
                            + this.AnimalState.DPLS_MCP;
            if (UDPIntakes.Solid > 0.0)
                this.AnimalState.UDP_Dig = (DUDP.Herbage * UDPIntakes.Herbage + DUDP.Supp * UDPIntakes.Supp) / UDPIntakes.Solid;
            else
                this.AnimalState.UDP_Dig = DUDP.Herbage;

            this.AnimalState.OrgFaeces.DM = this.AnimalState.DM_Intake.Solid * (1.0 - this.AnimalState.Digestibility.Solid);       // Faecal DM & N:                           
            this.AnimalState.OrgFaeces.Nu[(int)GrazType.TOMElement.n] = ((1.0 - DUDP.Herbage) * UDPIntakes.Herbage       //   Undigested UDP                         
                               + (1.0 - DUDP.Supp) * UDPIntakes.Supp
                               + (1.0 - DUDP.Milk) * UDPIntakes.Milk
                               + Genotype.ProtC[8] * this.AnimalState.MicrobialCP)                                        //   Undigested MCP                         
                               / GrazType.N2Protein + this.AnimalState.EndoFaeces.Nu[(int)GrazType.TOMElement.n];        //   Endogenous component                   
            this.AnimalState.InOrgFaeces.Nu[(int)GrazType.TOMElement.n] = 0.0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        private double DeltaGompertz(double t, double a, double b, double c)
        {
            return b * c / a * Math.Exp(c * (1.0 - t / a) + b * (1.0 - Math.Exp(c * (1.0 - t / a))));
        }

        /// <summary>
        /// Requirements for pregnancy:                                               
        ///   'Normal' weight of foetus is calculated from its age, maturity of       
        ///   the mother and her no. of young and is adjusted for mother's            
        ///   condition. The "FoetalWt" field of TheAnimals^ is updated here, as      
        ///   are the "EnergyUse.Preg" and "ProteinUse.Preg" fields of TimeStepState   
        /// </summary>
        private void ComputePregnancy()
        {
            double birthWt;                                         // Reference birth weight (kg)              
            double birthConceptus;                                  // Reference value of conceptus weight at birth  
            double foetalNWt;                                       // Normal weight of foetus (kg)             
            double foetalNGrowth;                                   // Normal growth rate of foetus (kg/day)    
            double conditionFactor;                                 // Effect of maternal condition on foetal growth 
            double foetalCondition;                                 // Foetal body condition                    
            double prevConceptusWt;

            prevConceptusWt = this.ConceptusWt();
            birthWt = this.BirthWeightForSize();
            birthConceptus = NoFoetuses * this.Genotype.PregC[5] * birthWt;

            foetalNWt = this.FoetalNormWt();
            foetalNGrowth = birthWt * this.DeltaGompertz(this.foetalAge, this.Genotype.PregC[1], this.Genotype.PregC[2], this.Genotype.PregC[3]);
            conditionFactor = (this.BodyCondition - 1.0) * foetalNWt / Genotype.StdBirthWt(this.NoFoetuses);
            if (this.BodyCondition >= 1.0)
                this.FoetalWeight = this.FoetalWeight + foetalNGrowth * (1.0 + conditionFactor);
            else
                this.FoetalWeight = this.FoetalWeight + foetalNGrowth * (1.0 + this.Genotype.PregScale[NoFoetuses] * conditionFactor);
            foetalCondition = FoetalWeight / foetalNWt;

            // ConceptusWt is a function of foetal age. Advance the age temporarily for this calucation.
            foetalAge++;
            this.AnimalState.ConceptusGrowth = ConceptusWt() - prevConceptusWt;
            foetalAge--;

            this.AnimalState.EnergyUse.Preg = this.Genotype.PregC[8] * birthConceptus * foetalCondition
                                * DeltaGompertz(foetalAge, this.Genotype.PregC[1], this.Genotype.PregC[9], this.Genotype.PregC[10])
                                / this.AnimalState.Efficiency.Preg;
            this.AnimalState.ProteinUse.Preg = this.Genotype.PregC[11] * birthConceptus * foetalCondition
                                * DeltaGompertz(foetalAge, this.Genotype.PregC[1], this.Genotype.PregC[12], this.Genotype.PregC[13]);

        }

        /// <summary>
        /// Requirements for lactation:                                             
        ///   The potential production of milk on the particular day of lactation,  
        ///   expressed as the ME value of the milk for the young, is predicted     
        ///   from a Wood-type function, scaled for the absolute and relative size  
        ///   of the mother, her condition at parturition and the no. of young.     
        ///   If ME intake is inadequate for potential production, yield is reduced 
        ///   by a proportion of the energy deficit.                                
        /// </summary>
        private void ComputeLactation()
        {
            double potMilkMJ;                                                               // Potential production of milk (MP')       
            double maxMilkMJ;                                                               // Milk prodn after energy deficit (MP'')   
            double energySurplus;
            double availMJ;
            double availRatio;
            double availDays;
            double condFactor;                                                              // Function of condition affecting milk     
                                                                                            // vs body reserves partition (CFlact)    
            double milkLimit;                                                               // Max. milk consumption by young (kg/hd)   
            double dayRatio;                                                                // Today's value of Milk_MJProd:PotMilkMJ   

            condFactor = 1.0 - this.Genotype.IntakeC[15] + this.Genotype.IntakeC[15] * BirthCondition;
            if (this.NoSuckling() > 0)                                                      // Potential milk production in MJ          
                potMilkMJ = this.Genotype.PeakLactC[this.NoSuckling()]
                             * Math.Pow(this.standardReferenceWeight, 0.75) * this.RelativeSize
                             * condFactor * this.lactationAdjustment
                             * this.WOOD(this.daysLactating + this.Genotype.LactC[1], this.Genotype.LactC[2], this.Genotype.LactC[3]);
            else
                potMilkMJ = this.Genotype.LactC[5] * this.Genotype.LactC[6] * this.Genotype.PeakMilk // peakmilk must have a value
                             * condFactor * this.lactationAdjustment
                             * this.WOOD(this.daysLactating + this.Genotype.LactC[1], this.Genotype.LactC[2], this.Genotype.LactC[4]); 

            energySurplus = this.AnimalState.ME_Intake.Total - this.AnimalState.EnergyUse.Maint - this.AnimalState.EnergyUse.Preg;
            availMJ = Genotype.LactC[5] * this.AnimalState.Efficiency.Lact * energySurplus;
            availRatio = availMJ / potMilkMJ;                                               // Effects of available energy, stage of    
            availDays = Math.Max(daysLactating, availRatio / (2.0 * Genotype.LactC[22]));    // lactation and body condition on        
            maxMilkMJ = potMilkMJ * this.Genotype.LactC[7]                                        // milk production                        
                                         / (1.0 + Math.Exp(this.Genotype.LactC[19] - this.Genotype.LactC[20] * availRatio
                                                       - this.Genotype.LactC[21] * availDays * (availRatio - this.Genotype.LactC[22] * availDays)
                                                       + this.Genotype.LactC[23] * BodyCondition * (availRatio - this.Genotype.LactC[24] * BodyCondition)));
            if (this.NoSuckling() > 0)
            {
                milkLimit = this.Genotype.LactC[6]
                             * this.NoSuckling()
                             * Math.Pow(this.Young.BaseWeight, 0.75)
                             * (this.Genotype.LactC[12] + this.Genotype.LactC[13] * Math.Exp(-this.Genotype.LactC[14] * daysLactating));
                MilkEnergy = Math.Min(maxMilkMJ, milkLimit);                              // Milk_MJ_Prodn becomes less than MaxMilkMJ
                proportionOfMaxMilk = MilkEnergy / milkLimit;                                  // when the young are not able to consume 
            }                                                                               // the amount of milk the mothers are     
            else                                                                            // capable of producing                   
            {
                MilkEnergy = maxMilkMJ;
                proportionOfMaxMilk = 1.0;
            }

            this.AnimalState.EnergyUse.Lact = MilkEnergy / (Genotype.LactC[5] * this.AnimalState.Efficiency.Lact);
            this.AnimalState.ProteinUse.Lact = Genotype.LactC[15] * MilkEnergy / Genotype.LactC[6];

            if (animalsDynamicGlb)
                if (daysLactating < Genotype.LactC[16] * Genotype.LactC[2])
                {
                    lactationAdjustment = 1.0;
                    lactationRatio = 1.0;
                }
                else
                {
                    dayRatio = StdMath.XDiv(MilkEnergy, potMilkMJ);
                    if (dayRatio < lactationRatio)
                    {
                        lactationAdjustment = lactationAdjustment - Genotype.LactC[17] * (lactationRatio - dayRatio);
                        lactationRatio = Genotype.LactC[18] * dayRatio + (1.0 - Genotype.LactC[18]) * lactationRatio;
                    }
                }
        }

        /// <summary>
        /// Wool production is calculated from the intake of ME, except that used   
        /// for pregnancy and lactation, and from the intake of undegraded dietary  
        /// protein. N.B. that the stored fleece weights are on a greasy basis      
        /// </summary>
        /// <param name="dplsAdjust"></param>
        private void ComputeWool(double dplsAdjust)
        {
            double AgeFactor;
            double DayLenFactor;
            double ME_Avail_Wool;
            double DPLS_To_CFW;                   // kg CFW grown per kg available DPLS       
            double ME_To_CFW;                     // kg CFW grown per kg available ME         
            double DayCFWGain;

            AgeFactor = this.Genotype.WoolC[5] + (1.0 - this.Genotype.WoolC[5]) * (1.0 - Math.Exp(-this.Genotype.WoolC[12] * this.AgeDays));
            DayLenFactor = 1.0 + this.Genotype.WoolC[6] * (weather.CalculateDayLength(-6.0) - 12);
            DPLS_To_CFW = this.Genotype.WoolC[7] * this.Genotype.FleeceRatio * AgeFactor * DayLenFactor;
            ME_To_CFW = this.Genotype.WoolC[8] * this.Genotype.FleeceRatio * AgeFactor * DayLenFactor;
            this.AnimalState.DPLS_Avail_Wool = StdMath.DIM(this.AnimalState.DPLS + dplsAdjust,
                                    this.Genotype.WoolC[9] * (this.AnimalState.ProteinUse.Lact + this.AnimalState.ProteinUse.Preg));
            ME_Avail_Wool = StdMath.DIM(this.AnimalState.ME_Intake.Total, this.AnimalState.EnergyUse.Lact + this.AnimalState.EnergyUse.Preg);
            DayCFWGain = Math.Min(DPLS_To_CFW * this.AnimalState.DPLS_Avail_Wool, ME_To_CFW * ME_Avail_Wool);
#pragma warning disable 162 //unreachable code
            if (animalsDynamicGlb)
                this.AnimalState.ProteinUse.Wool = (1 - this.Genotype.WoolC[4]) * (this.Genotype.WoolC[3] * this.GreasyFleeceGrowth) +            // Smoothed wool growth                     
                                   this.Genotype.WoolC[4] * DayCFWGain;
            else
                this.AnimalState.ProteinUse.Wool = DayCFWGain;
#pragma warning restore 162
            this.AnimalState.EnergyUse.Wool = this.Genotype.WoolC[1] * StdMath.DIM(this.AnimalState.ProteinUse.Wool, this.Genotype.WoolC[2] * this.RelativeSize) /      // Energy use for fleece                    
                               this.Genotype.WoolC[3];
        }

        /// <summary>
        /// Apply the wool growth
        /// </summary>
        private void ApplyWoolGrowth()
        {
            double ageFactor;
            double potCleanGain;
            double diamPower;
            double gain_Length;

            this.GreasyFleeceGrowth = this.AnimalState.ProteinUse.Wool / this.Genotype.WoolC[3];                // Convert clean to greasy fleece           
            this.woolWt = this.woolWt + this.GreasyFleeceGrowth;
            this.AnimalState.TotalWoolEnergy = this.Genotype.WoolC[1] * this.GreasyFleeceGrowth;                // Reporting only                           

            // Changed to always TRUE for use with AgLab API, since we want to
            // be able to report the change in coat depth
            if (true) // AnimalsDynamicGlb  then                                                        // This section deals with fibre diameter   
            {
                ageFactor = this.Genotype.WoolC[5] + (1.0 - this.Genotype.WoolC[5]) * (1.0 - Math.Exp(-this.Genotype.WoolC[12] * this.AgeDays));
                potCleanGain = (this.Genotype.WoolC[3] * this.Genotype.FleeceRatio * this.standardReferenceWeight) * ageFactor / 365;
                if (this.AnimalState.EnergyUse.Gain >= 0.0)
                    diamPower = this.Genotype.WoolC[13];
                else
                    diamPower = this.Genotype.WoolC[14];
                this.DayFibreDiam = this.Genotype.MaxFleeceDiam * Math.Pow(this.AnimalState.ProteinUse.Wool / potCleanGain, diamPower);
                if (BaseWeight <= 0)
                    throw new Exception("Base weight is zero or less for " + this.NoAnimals.ToString() + " " + GetBreed() + " animals aged " + this.AgeDays.ToString() + " days");
                if (this.DayFibreDiam > 0.0)
                    gain_Length = 100.0 * 4.0 / Math.PI * this.AnimalState.ProteinUse.Wool /              // Computation of fibre diameter assumes    
                                           (this.Genotype.WoolC[10] * this.Genotype.WoolC[11] *             // that the day's growth is cylindrical   
                                             this.Genotype.ChillC[1] * Math.Pow(this.BaseWeight, 2.0 / 3.0) *   // in shape                               
                                             StdMath.Sqr(this.DayFibreDiam * 1E-6));
                else
                    gain_Length = 0.0;
                this.FibreDiam = StdMath.XDiv(this.coatDepth * this.FibreDiam +                      // Running average fibre diameter           
                                           gain_Length * this.DayFibreDiam,
                                         coatDepth + gain_Length);
                this.coatDepth = this.coatDepth + gain_Length;

            }
        }

        /// <summary>
        /// Chilling routine.                                                       
        /// Energy use in maintaining body temperature is computed in 2-hour blocks.
        /// Although the "day" in the animal model runs from 9 am, we first compute 
        /// the value of the insulation and the lower critical temperature in the   
        /// middle of the night (i.e. at the time of minimum temperature).  Even    
        /// though wind increases during the day, the minimum value of the          
        /// Insulation variable will be no less than half the value of Insulation   
        /// at this time for any reasonable value of wind speed; we can therefore   
        /// put a bound on LCT.                                                     
        /// </summary>
        private void ComputeChilling()
        {
            const double Sin60 = 0.8660254;
            double[] HourSines = { 0, 0.5, Sin60, 1.0, Sin60, 0.5, 0.0, -0.5, -Sin60, -1.0, -Sin60, -0.5, 0.0 }; //[1..12]
            double SurfaceArea;                                                            // Surface area of animal, sq m             
            double BodyRadius;                                                             // Radius of body, cm                       
            double Factor1, Factor2;                                                       // Function of body radius and coat depth   
            double Factor3, WetFactor;
            double HeatPerArea;
            double LCT_Base;
            double PropnClearSky;                                                          // Proportion of night with clear sky       
            double TissueInsulation;
            double Insulation;
            double LCT;                                                                    // Lower critical temp. for a 2-hour period 
            double EnergyRate;

            double AveTemp, TempRange;
            double AveWind, WindRange;
            double Temp2Hr, Wind2Hr;
            int Time;

            this.AnimalState.Therm0HeatProdn = this.AnimalState.ME_Intake.Total            // Thermoneutral heat production            
                           - this.AnimalState.Efficiency.Preg * this.AnimalState.EnergyUse.Preg
                           - this.AnimalState.Efficiency.Lact * this.AnimalState.EnergyUse.Lact
                           - this.AnimalState.Efficiency.Gain * (this.AnimalState.ME_Intake.Total - this.AnimalState.EnergyUse.Maint - this.AnimalState.EnergyUse.Preg - this.AnimalState.EnergyUse.Lact)
                         + this.Genotype.ChillC[16] * this.ConceptusWt();
            SurfaceArea = this.Genotype.ChillC[1] * Math.Pow(this.BaseWeight, 2.0 / 3.0);
            BodyRadius = this.Genotype.ChillC[2] * Math.Pow(this.normalWeight, 1.0 / 3.0);


            // Means and amplitudes for temperature and windrun     
            AveTemp = 0.5 * (weather.MaxT + weather.MinT);                                                          
            TempRange = (weather.MaxT - weather.MinT) / 2.0;
            AveWind = 0.4 * weather.Wind;                                               // 0.4 corrects wind to animal height       
            WindRange = 0.35 * AveWind;
            PropnClearSky = 0.7 * Math.Exp(-0.25 * weather.Rain);                   // Equation J.4                             


            TissueInsulation = Genotype.ChillC[3] * Math.Min(1.0, 0.4 + 0.02 * AgeDays) *    // Reduce tissue insulation for animals under 1 month old
                                            (Genotype.ChillC[4] + (1.0 - Genotype.ChillC[4]) * BodyCondition);    // Tissue insulation calculated as a fn     
                                                                                                            // of species and body condition          
            Factor1 = BodyRadius / (BodyRadius + CoatDepth);                                // These factors are used in equation J.8   
            Factor2 = BodyRadius * Math.Log(1.0 / Factor1);
            WetFactor = this.Genotype.ChillC[5] + (1.0 - this.Genotype.ChillC[5]) *
                                            Math.Exp(-this.Genotype.ChillC[6] * weather.Rain / CoatDepth);
            HeatPerArea = this.AnimalState.Therm0HeatProdn / SurfaceArea;                   // These factors are used in equation J.10  
            LCT_Base = this.Genotype.ChillC[11] - HeatPerArea * TissueInsulation;
            Factor3 = HeatPerArea / (HeatPerArea - this.Genotype.ChillC[12]);

            this.AnimalState.EnergyUse.Cold = 0.0;
            this.AnimalState.LowerCritTemp = 0.0;
            for (Time = 1; Time <= 12; Time++)
            {
                Temp2Hr = AveTemp + TempRange * HourSines[Time];
                Wind2Hr = AveWind + WindRange * HourSines[Time];

                Insulation = WetFactor *                                                    // External insulation due to hair cover or 
                              (Factor1 / (Genotype.ChillC[7] + Genotype.ChillC[8] * Math.Sqrt(Wind2Hr)) +     // fleece is calculated from Blaxter (1977)      
                               Factor2 * (Genotype.ChillC[9] - Genotype.ChillC[10] * Math.Sqrt(Wind2Hr)));                                     

                LCT = LCT_Base + (this.Genotype.ChillC[12] - HeatPerArea) * Insulation;
                if ((Time >= 7) && (Time <= 11) && (Temp2Hr > 10.0))                        // Night-time, i.e. 7 pm to 5 am            
                    LCT = LCT + PropnClearSky * this.Genotype.ChillC[13] * Math.Exp(-Genotype.ChillC[14] * StdMath.Sqr(StdMath.DIM(Temp2Hr, Genotype.ChillC[15])));

                EnergyRate = SurfaceArea * StdMath.DIM(LCT, Temp2Hr)
                                                / (Factor3 * TissueInsulation + Insulation);
                this.AnimalState.EnergyUse.Cold = this.AnimalState.EnergyUse.Cold + 1.0 / 12.0 * EnergyRate;
                this.AnimalState.LowerCritTemp = this.AnimalState.LowerCritTemp + 1.0 / 12.0 * LCT;

            } ////_ FOR Time _

            this.AnimalState.EnergyUse.Maint = this.AnimalState.EnergyUse.Maint + this.AnimalState.EnergyUse.Cold;
        }

        /// <summary>
        /// Computes the efficiency of energy use for weight change.  This routine  
        /// is called twice if chilling energy use is computed                      
        /// </summary>
        private void AdjustKGain()
        {
            if (this.AnimalState.ME_Intake.Total < this.AnimalState.EnergyUse.Maint + this.AnimalState.EnergyUse.Preg + this.AnimalState.EnergyUse.Lact)
            {                                                                                                           // Efficiency of energy use for weight change     
                if (lactStatus == GrazType.LactType.Lactating)
                    this.AnimalState.Efficiency.Gain = this.AnimalState.Efficiency.Lact / this.Genotype.EfficC[10];                     // Lactating animals in -ve energy balance 
                else
                    this.AnimalState.Efficiency.Gain = this.AnimalState.Efficiency.Maint / this.Genotype.EfficC[11];                    // Dry animals in -ve energy balance        
            }
            else if (lactStatus == GrazType.LactType.Lactating)
                this.AnimalState.Efficiency.Gain = this.Genotype.EfficC[9] * this.AnimalState.Efficiency.Lact;                          // Lactating animals in +ve energy balance  
        }

        /// <summary>
        /// The remaining surplus of net energy is converted to weight gain in a      
        /// logistic function dependent on the relative size of the animal.           
        /// </summary>
        private void ComputeGain()
        {
            double Eff_DPLS;                                                               // Efficiency of DPLS use                   
            double DPLS_Used;
            double[] GainSigs = new double[2];
            double fGainSize;
            double SizeFactor1;
            double SizeFactor2;
            double NetProtein;
            double PrevWoolEnergy;
            double MilkScalar;
            double EmptyBodyGain;


            this.AnimalState.EnergyUse.Gain = this.AnimalState.Efficiency.Gain * (this.AnimalState.ME_Intake.Total - (this.AnimalState.EnergyUse.Maint + this.AnimalState.EnergyUse.Preg + this.AnimalState.EnergyUse.Lact))
                             - this.AnimalState.EnergyUse.Wool;

            Eff_DPLS = this.Genotype.GainC[2] / (1.0 + (this.Genotype.GainC[2] / this.Genotype.GainC[3] - 1.0) *               // Efficiency of use of protein from milk   
                                                StdMath.XDiv(this.AnimalState.DPLS_Milk, this.AnimalState.DPLS));           // is higher than from solid sources      
            DPLS_Used = (this.AnimalState.ProteinUse.Maint + this.AnimalState.ProteinUse.Preg + this.AnimalState.ProteinUse.Lact) / Eff_DPLS;
            if (Animal == GrazType.AnimalType.Sheep)                                                                        // Efficiency of use of protein for wool is 
                DPLS_Used = DPLS_Used + this.AnimalState.ProteinUse.Wool / Genotype.GainC[1];                                // 0.6 regardless of source               
            this.AnimalState.ProteinUse.Gain = Eff_DPLS * (this.AnimalState.DPLS - DPLS_Used);


            fGainSize = this.NormalWeightFunc(this.AgeDays, this.maxPrevWeight, 0.0) / this.standardReferenceWeight;
            GainSigs[0] = Genotype.GainC[5];
            GainSigs[1] = Genotype.GainC[4];
            SizeFactor1 = StdMath.SIG(fGainSize, GainSigs);
            SizeFactor2 = StdMath.RAMP(fGainSize, this.Genotype.GainC[6], this.Genotype.GainC[7]);

            this.AnimalState.GainEContent = this.Genotype.GainC[8]                                             // Generalization of the SCA equations      
                               - SizeFactor1 * (this.Genotype.GainC[9] - this.Genotype.GainC[10] * (this.feedingLevel - 1.0))
                               + SizeFactor2 * this.Genotype.GainC[11] * (this.BodyCondition - 1.0);
            this.AnimalState.GainPContent = this.Genotype.GainC[12]
                               + SizeFactor1 * (this.Genotype.GainC[13] - this.Genotype.GainC[14] * (this.feedingLevel - 1.0))
                               - SizeFactor2 * this.Genotype.GainC[15] * (BodyCondition - 1.0);

            this.AnimalState.UDP_Reqd = StdMath.DIM(DPLS_Used +
                                     (this.AnimalState.EnergyUse.Gain / this.AnimalState.GainEContent) * this.AnimalState.GainPContent / Eff_DPLS,
                                    this.AnimalState.DPLS_MCP)
                               / this.AnimalState.UDP_Dig;

            NetProtein = this.AnimalState.ProteinUse.Gain - this.AnimalState.GainPContent * this.AnimalState.EnergyUse.Gain / this.AnimalState.GainEContent;

            if ((NetProtein < 0) && (this.AnimalState.ProteinUse.Lact > GrazType.VerySmall))                // Deficiency of protein, i.e. protein is   
            {                                                                                               //  more limiting than ME                  
                MilkScalar = Math.Max(0.0, 1.0 + Genotype.GainC[16] * NetProtein /                           // Redirect protein from milk to weight change    
                                                                this.AnimalState.ProteinUse.Lact);                                           
                this.AnimalState.EnergyUse.Gain = this.AnimalState.EnergyUse.Gain + (1.0 - MilkScalar) * MilkEnergy;
                this.AnimalState.ProteinUse.Gain = this.AnimalState.ProteinUse.Gain + (1.0 - MilkScalar) * this.AnimalState.ProteinUse.Lact;
                NetProtein = this.AnimalState.ProteinUse.Gain - this.AnimalState.GainPContent * this.AnimalState.EnergyUse.Gain / this.AnimalState.GainEContent;

                MilkEnergy = MilkScalar * MilkEnergy;
                this.AnimalState.EnergyUse.Lact = MilkScalar * this.AnimalState.EnergyUse.Lact;
                this.AnimalState.ProteinUse.Lact = MilkScalar * this.AnimalState.ProteinUse.Lact;
            }
            MilkProtein = this.AnimalState.ProteinUse.Lact;
            MilkYield = MilkEnergy / (Genotype.LactC[5] * Genotype.LactC[6]);

            if (NetProtein >= 0)
                this.AnimalState.ProteinUse.Gain = this.AnimalState.GainPContent * this.AnimalState.EnergyUse.Gain / this.AnimalState.GainEContent;
            else
                this.AnimalState.EnergyUse.Gain = this.AnimalState.EnergyUse.Gain + Genotype.GainC[17] * this.AnimalState.GainEContent *
                                                    NetProtein / this.AnimalState.GainPContent;

            if ((this.AnimalState.ProteinUse.Gain < 0) && (Animal == GrazType.AnimalType.Sheep))                    // If protein is being catabolised, it can  
            {                                                                                                       // be utilized to increase wool growth    
                PrevWoolEnergy = this.AnimalState.EnergyUse.Wool;                                                   // Maintain the energy balance by           
                ComputeWool(Math.Abs(this.AnimalState.ProteinUse.Gain));                                           // transferring any extra energy use for  
                this.AnimalState.EnergyUse.Gain = this.AnimalState.EnergyUse.Gain - (this.AnimalState.EnergyUse.Wool - PrevWoolEnergy);  // wool out of weight change              
            }

            EmptyBodyGain = this.AnimalState.EnergyUse.Gain / this.AnimalState.GainEContent;

            WeightChange = Genotype.GainC[18] * EmptyBodyGain;
            BaseWeight = BaseWeight + WeightChange;

            this.AnimalState.ProteinUse.Total = this.AnimalState.ProteinUse.Maint + this.AnimalState.ProteinUse.Gain +
                                 this.AnimalState.ProteinUse.Preg + this.AnimalState.ProteinUse.Lact +
                                 this.AnimalState.ProteinUse.Wool;
            this.AnimalState.Urine.Nu[(int)GrazType.TOMElement.n] = StdMath.DIM(this.AnimalState.CP_Intake.Total / GrazType.N2Protein,   // Urinary loss of N                        
                                      (this.AnimalState.ProteinUse.Total - this.AnimalState.ProteinUse.Maint) / GrazType.N2Protein       // This is retention of N                 
                                      + this.AnimalState.OrgFaeces.Nu[(int)GrazType.TOMElement.n]                                   // This is other excretion                
                                      + this.AnimalState.InOrgFaeces.Nu[(int)GrazType.TOMElement.n]
                                      + this.AnimalState.DermalNLoss);
        }

        /// <summary>
        /// Usage of and mass balance for phosphorus                                  
        /// * Only a proportion of the phosphorus intake is absorbed (available).     
        /// * There are endogenous losses of P which will appear in the excreta       
        ///   regardless of intake.                                                   
        /// * P content of the day's conceptus growth varies with stage of pregnancy. 
        /// * P contents of milk and wool are constants.                              
        /// * P usage in liveweight change is computed to try and maintain body P     
        ///   content at PhosC[9].                                                    
        /// * All P is excreted in faeces, but some is organic and the rest is        
        ///   inorganic.  Organic P excretion is a constant proportion of DMI.        
        /// </summary>
        private void ComputePhosphorus()
        {
            double availPhos;
            double excretePhos;
            int p = (int)GrazType.TOMElement.p;

            availPhos = this.Genotype.PhosC[1] * this.AnimalState.Phos_Intake.Solid + this.Genotype.PhosC[2] * this.AnimalState.Phos_Intake.Milk;
            this.AnimalState.EndoFaeces.Nu[p] = this.Genotype.PhosC[3] * BaseWeight;

            if (((ReproState == GrazType.ReproType.EarlyPreg) || (ReproState == GrazType.ReproType.LatePreg)) || (lactStatus == GrazType.LactType.Lactating))
                this.AnimalState.EndoFaeces.Nu[p] = this.Genotype.PhosC[11] * this.AnimalState.DM_Intake.Total + this.Genotype.PhosC[12] * BaseWeight;
            else
                this.AnimalState.EndoFaeces.Nu[p] = this.Genotype.PhosC[9] * this.AnimalState.DM_Intake.Total + this.Genotype.PhosC[10] * BaseWeight;

            this.AnimalState.Phos_Use.Maint = Math.Min(availPhos, this.AnimalState.EndoFaeces.Nu[p]);
            this.AnimalState.Phos_Use.Preg = Math.Max(Genotype.PhosC[4], this.Genotype.PhosC[5] * foetalAge - this.Genotype.PhosC[6]) * this.AnimalState.ConceptusGrowth;
            this.AnimalState.Phos_Use.Lact = this.Genotype.PhosC[7] * MilkYield;
            this.AnimalState.Phos_Use.Wool = this.Genotype.PhosC[8] * GreasyFleeceGrowth;
            this.AnimalState.Phos_Use.Gain = WeightChange *
                                 (this.Genotype.PhosC[13] + this.Genotype.PhosC[14] * Math.Pow(standardReferenceWeight / BaseWeight, this.Genotype.PhosC[15]));
            this.AnimalState.Phos_Use.Gain = Math.Min(availPhos - (this.AnimalState.Phos_Use.Maint + this.AnimalState.Phos_Use.Preg + this.AnimalState.Phos_Use.Lact + this.AnimalState.Phos_Use.Wool),
                                    this.AnimalState.Phos_Use.Gain);
            //// WITH AnimalState.Phos_Use DO
            this.AnimalState.Phos_Use.Total = this.AnimalState.Phos_Use.Maint + this.AnimalState.Phos_Use.Preg + this.AnimalState.Phos_Use.Lact + this.AnimalState.Phos_Use.Wool + this.AnimalState.Phos_Use.Gain;
            basePhosphorusWeight = basePhosphorusWeight - this.AnimalState.EndoFaeces.Nu[p] + this.AnimalState.Phos_Use.Maint + this.AnimalState.Phos_Use.Gain;
            milkPhosphorusProduction = this.AnimalState.Phos_Use.Lact;

            excretePhos = this.AnimalState.EndoFaeces.Nu[p] + this.AnimalState.Phos_Intake.Total - this.AnimalState.Phos_Use.Total;
            this.AnimalState.OrgFaeces.Nu[p] = 0.0;
            this.AnimalState.InOrgFaeces.Nu[p] = excretePhos - this.AnimalState.OrgFaeces.Nu[p];
            this.AnimalState.Urine.Nu[p] = 0.0;
        }

        /// <summary>
        /// Usage of and mass balance for sulphur                                     
        /// </summary>
        private void ComputeSulfur()
        {
            double excreteSulf;
            int s = (int)GrazType.TOMElement.s;

            this.AnimalState.EndoFaeces.Nu[s] = this.Genotype.SulfC[1] * this.AnimalState.EndoFaeces.Nu[(int)GrazType.TOMElement.n];

            this.AnimalState.Sulf_Use.Maint = this.AnimalState.EndoFaeces.Nu[s];
            this.AnimalState.Sulf_Use.Preg = this.Genotype.SulfC[1] * this.AnimalState.ProteinUse.Preg / GrazType.N2Protein;
            this.AnimalState.Sulf_Use.Lact = this.Genotype.SulfC[2] * this.AnimalState.ProteinUse.Lact / GrazType.N2Protein;
            this.AnimalState.Sulf_Use.Wool = this.Genotype.SulfC[3] * this.AnimalState.ProteinUse.Wool / GrazType.N2Protein;
            ////WITH AnimalState.Sulf_Use DO
            this.AnimalState.Sulf_Use.Gain = Math.Min(this.Genotype.SulfC[1] * this.AnimalState.ProteinUse.Gain / GrazType.N2Protein,
                                    this.AnimalState.Sulf_Intake.Total - (this.AnimalState.Sulf_Use.Maint + this.AnimalState.Sulf_Use.Preg + this.AnimalState.Sulf_Use.Lact + this.AnimalState.Sulf_Use.Wool));
            ////WITH AnimalState.Sulf_Use DO
            this.AnimalState.Sulf_Use.Total = this.AnimalState.Sulf_Use.Maint + this.AnimalState.Sulf_Use.Preg + this.AnimalState.Sulf_Use.Lact + this.AnimalState.Sulf_Use.Wool + this.AnimalState.Sulf_Use.Gain;

            excreteSulf = this.AnimalState.EndoFaeces.Nu[s] + this.AnimalState.Sulf_Intake.Total - this.AnimalState.Sulf_Use.Total;
            this.AnimalState.OrgFaeces.Nu[s] = Math.Min(excreteSulf, this.Genotype.SulfC[4] * this.AnimalState.DM_Intake.Total);
            this.AnimalState.InOrgFaeces.Nu[s] = 0;
            this.AnimalState.Urine.Nu[s] = excreteSulf - this.AnimalState.OrgFaeces.Nu[s];
            baseSulphurWeight = baseSulphurWeight + this.AnimalState.Sulf_Use.Gain;
            milkSulphurProduction = this.AnimalState.Sulf_Use.Lact;

        }

        /// <summary>
        /// Proton balance                                                            
        /// </summary>
        private void ComputeAshAlk()
        {
            double intakeMoles;                                                             // These are all on a per-head basis        
            double accumMoles;

            intakeMoles = this.AnimalState.PaddockIntake.AshAlkalinity * this.AnimalState.PaddockIntake.Biomass
                             + this.AnimalState.SuppIntake.AshAlkalinity * this.AnimalState.SuppIntake.Biomass;
            accumMoles = Genotype.AshAlkC[1] * (WeightChange + this.AnimalState.ConceptusGrowth);
            if (Animal == GrazType.AnimalType.Sheep)
                accumMoles = accumMoles + this.Genotype.AshAlkC[2] * GreasyFleeceGrowth;

            this.AnimalState.OrgFaeces.AshAlk = this.Genotype.AshAlkC[3] * this.AnimalState.OrgFaeces.DM;
            this.AnimalState.Urine.AshAlk = intakeMoles - accumMoles - this.AnimalState.OrgFaeces.AshAlk;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="animalList"></param>
        private void CheckAnimList(ref List<AnimalGroup> animalList)
        {
            if (animalList == null)
                animalList = new List<AnimalGroup>();
        }
        
        /// <summary>
        /// Export the animal group
        /// </summary>
        /// <param name="weanedGroup"></param>
        /// <param name="weanedOff"></param>
        private void ExportWeaners(ref AnimalGroup weanedGroup, ref List<AnimalGroup> weanedOff)
        {
            if (weanedGroup != null)
            {
                weanedGroup.lactStatus = GrazType.LactType.Dry;
                weanedGroup.numberOffspring = 0;
                weanedGroup.mothers = null;
                this.CheckAnimList(ref weanedOff);
                weanedOff.Add(weanedGroup);
            }
        }
        
        /// <summary>
        /// Export the group with young
        /// </summary>
        /// <param name="motherGroup"></param>
        /// <param name="youngGroup"></param>
        /// <param name="numberYoung"></param>
        /// <param name="newGroups"></param>
        private void ExportWithYoung(ref AnimalGroup motherGroup, ref AnimalGroup youngGroup, int numberYoung, ref List<AnimalGroup> newGroups)
        {
            motherGroup.Young = youngGroup;
            motherGroup.numberOffspring = numberYoung;
            youngGroup.mothers = motherGroup;
            youngGroup.numberOffspring = numberYoung;
            CheckAnimList(ref newGroups);
            newGroups.Add(motherGroup);
        }

        /// <summary>
        /// In the case where only one sex of lambs has been weaned, re-constitute
        /// groups of mothers with unweaned lambs or calves.                      
        /// For example, if male lambs have been weaned (bDoFemales=TRUE), then:  
        /// - if pre-weaning lambs/ewe = 1, 100% of the ewe lambs become singles  
        /// - if pre-weaning lambs/ewe = 2, 50% of the ewe lambs become singles   
        ///                                 50% remain as twins                   
        /// - if pre-weaning lambs/ewe = 3, 25% of the ewe lambs become singles   
        ///                                 50% become twins                      
        ///                                 25% remain as triplets                
        /// * We then have to round the numbers of lambs (or calves) that remain  
        ///   twins or triplets down so that they have an integer number of       
        ///   mothers.                                                            
        /// * In order to conserve animals numbers, the number remaining as       
        ///   singles is done by difference                                       
        /// * The re-constituted groups of mothers are sent off to the NewGroups  
        ///   list, leaving Self as the group of mothers whach has had all its    
        ///   offspring weaned                                                    
        /// </summary>
        /// <param name="youngGroup"></param>
        /// <param name="totalYoung"></param>
        /// <param name="GroupPropn"></param>
        /// <param name="newGroups"></param>
        private void SplitMothers(ref AnimalGroup youngGroup, int totalYoung, double GroupPropn, ref List<AnimalGroup> newGroups)
        {
            // becoming : single twin triplet

            // [0..3,1..3] first element [0] in 2nd dimension is a dummy
            double[,] PropnRemainingLambsAs = new double[4, 4]  {  {0, 0,    0,     0      }, // starting out: empty
                                                                   {0, 1,    0,     0      },               // single
                                                                   {0, 1.0/2.0,  1.0/2.0,   0      },       // twin
                                                                   {0, 1.0/4.0,  1.0/2.0,   1.0/4.0    }};  // triplet

            bool doFemales;
            int keptLambs;
            int[] lambsByParity = new int[4];
            int[] ewesByParity = new int[4];
            AnimalGroup stillMothers;
            AnimalGroup stillYoung;
            int NY;

            if ((this.NoOffspring > 3) || (this.NoOffspring < 0))
                throw new Exception("Weaning-by-sex logic can only cope with triplets");

            if (youngGroup != null)
            {
                if ((youngGroup.MaleNo > 0) && (youngGroup.FemaleNo > 0))
                    throw new Exception("Weaning-by-sex logic: only one sex at a time");
                doFemales = (youngGroup.ReproState == GrazType.ReproType.Empty);

                // Compute numbers of mothers & offspring that remain feeding/suckling
                // with each parity
                keptLambs = youngGroup.NoAnimals;
                for (NY = 3; NY >= 2; NY--)
                {
                    lambsByParity[NY] = Convert.ToInt32(Math.Truncate((PropnRemainingLambsAs[this.NoOffspring, NY] * keptLambs) + 0.5), CultureInfo.InvariantCulture);
                    ewesByParity[NY] = (lambsByParity[NY] / NY);
                    lambsByParity[NY] = NY * ewesByParity[NY];
                }
                lambsByParity[1] = keptLambs - lambsByParity[2] - lambsByParity[3];
                ewesByParity[1] = Math.Min(lambsByParity[1], this.FemaleNo - ewesByParity[2] - ewesByParity[3]); // allow for previous rounding

                // Split off the mothers & offspring that remain feeding/suckling
                for (NY = 3; NY >= 1; NY--)
                {
                    if (ewesByParity[NY] > 0)
                    {
                        stillMothers = this.Split(ewesByParity[NY], false, this.NODIFF, this.NODIFF);
                        if (doFemales)
                            stillYoung = youngGroup.SplitSex(0, lambsByParity[NY], false, this.NODIFF);
                        else
                            stillYoung = youngGroup.SplitSex(lambsByParity[NY], 0, false, this.NODIFF);
                        this.ExportWithYoung(ref stillMothers, ref stillYoung, NY, ref newGroups);
                    }
                }
                if (youngGroup.NoAnimals != 0)
                    throw new Exception("Weaning-by-sex logic failed");

                youngGroup = null;
            }
        }

        /// <summary>
        /// Integration of the age-dependent mortality function                       
        /// </summary>
        /// <param name="overDays">Number of days</param>
        /// <returns>Integrated value</returns>
        private double ExpectedSurvival(int overDays)
        {
            double dayDeath;
            int dayCount;
            int age;

            age = this.AgeDays;
            double result = 1.0;

            while (overDays > 0)
            {
                if ((this.lactStatus == GrazType.LactType.Suckling) || (age >= Math.Round(this.Genotype.MortAge[2])))
                {
                    dayDeath = this.Genotype.MortRate[1];
                    dayCount = overDays;
                }
                else if (age < Math.Round(this.Genotype.MortAge[1]))
                {
                    dayDeath = this.Genotype.MortRate[2];
                    dayCount = Convert.ToInt32(Math.Min(overDays, Math.Round(this.Genotype.MortAge[1]) - age), CultureInfo.InvariantCulture);
                }
                else
                {
                    dayDeath = this.Genotype.MortRate[1] + (this.Genotype.MortRate[2] - this.Genotype.MortRate[1])
                                                       * StdMath.RAMP(age, this.Genotype.MortAge[2], this.Genotype.MortAge[1]);
                    dayCount = 1;
                }

                result = result * Math.Pow(1.0 - dayDeath, dayCount);
                overDays -= dayCount;
                age += dayCount;
            }
            return result;
        }

        /// <summary>
        /// Combine two pools
        /// </summary>
        /// <param name="pool1">Pool one</param>
        /// <param name="pool2">Pool two</param>
        /// <returns>Combined pool</returns>
        private GrazType.DM_Pool AddDMPool(GrazType.DM_Pool pool1, GrazType.DM_Pool pool2)
        {
            int N = (int)GrazType.TOMElement.n;
            int P = (int)GrazType.TOMElement.p;
            int S = (int)GrazType.TOMElement.s;

            GrazType.DM_Pool result = new GrazType.DM_Pool();
            result.DM = pool1.DM + pool2.DM;
            result.Nu[N] = pool1.Nu[N] + pool2.Nu[N];
            result.Nu[S] = pool1.Nu[S] + pool2.Nu[S];
            result.Nu[P] = pool1.Nu[P] + pool2.Nu[P];
            result.AshAlk = pool1.AshAlk + pool2.AshAlk;

            return result;
        }

        /// <summary>
        /// Multiply pools
        /// </summary>
        /// <param name="srcPool"></param>
        /// <param name="factor"></param>
        /// <returns>The product</returns>
        private GrazType.DM_Pool MultiplyDMPool(GrazType.DM_Pool srcPool, double factor)
        {
            int N = (int)GrazType.TOMElement.n;
            int P = (int)GrazType.TOMElement.p;
            int S = (int)GrazType.TOMElement.s;

            GrazType.DM_Pool result = new GrazType.DM_Pool();
            result.DM = srcPool.DM * factor;
            result.Nu[N] = srcPool.Nu[N] * factor;
            result.Nu[S] = srcPool.Nu[S] * factor;
            result.Nu[P] = srcPool.Nu[P] * factor;
            result.AshAlk = srcPool.AshAlk * factor;

            return result;
        }

        /// <summary>
        /// Supplement relative intake.
        /// </summary>
        /// <param name="theAnimals">The animal group</param>
        /// <param name="timeStepLength"></param>
        /// <param name="suppDWPerHead"></param>
        /// <param name="supp"></param>
        /// <param name="suppRQ"></param>
        /// <param name="eatenFirst"></param>
        /// <param name="suppRI"></param>
        /// <param name="fracUnsat"></param>
        private void EatSupplement(AnimalGroup theAnimals,
                                    double timeStepLength,
                                    double suppDWPerHead,
                                    FoodSupplement supp,
                                    double suppRQ,
                                    bool eatenFirst,
                                    ref double suppRI,
                                    ref double fracUnsat)
        {
            double suppRelFill;

            if (theAnimals.PotIntake < GrazType.VerySmall)
                suppRelFill = 0.0;
            else
            {
                if (eatenFirst)                                                     // Relative fill of supplement           
                    suppRelFill = Math.Min(fracUnsat,
                                          suppDWPerHead / (theAnimals.PotIntake * suppRQ));
                else
                    suppRelFill = Math.Min(fracUnsat,
                                          suppDWPerHead / (theAnimals.PotIntake * timeStepLength * suppRQ));

                if ((supp.ME2DM > 0.0) && (!supp.IsRoughage))
                {
                    if (theAnimals.lactStatus == GrazType.LactType.Lactating)
                        suppRelFill = Math.Min(suppRelFill, theAnimals.Genotype.GrazeC[20] / supp.ME2DM);
                    else
                        suppRelFill = Math.Min(suppRelFill, theAnimals.Genotype.GrazeC[11] / supp.ME2DM);
                }
            }

            suppRI = suppRQ * suppRelFill;
            fracUnsat = StdMath.DIM(fracUnsat, suppRelFill);
        }

        /// <summary>
        /// "Relative fill" of pasture [F(d)]                                     
        /// </summary>
        /// <param name="theAnimals">The animal group</param>
        /// <param name="fu"></param>
        /// <param name="classFeed"></param>
        /// <param name="totalFeed"></param>
        /// <param name="hr"></param>
        /// <returns></returns>
        private double RelativeFill(AnimalGroup theAnimals, double fu, double classFeed, double totalFeed, double hr)
        {

            double heightFactor,
            sizeFactor,
            scaledFeed,
            propnFactor,
            rateTerm,
            timeTerm;

            double result;

            // Equation numbers refer to June 2008 revision of Freer, Moore, and Donnelly 
            heightFactor = Math.Max(0.0, (1.0 - theAnimals.Genotype.GrazeC[12]) + theAnimals.Genotype.GrazeC[12] * hr);       // Eq. 18 : HF 
            sizeFactor = 1.0 + StdMath.DIM(theAnimals.Genotype.GrazeC[7], theAnimals.RelativeSize);                                  // Eq. 19 : ZF 
            scaledFeed = heightFactor * sizeFactor * classFeed;                                                             // Part of Eqs. 16, 16 : HF * ZF * B 
            propnFactor = 1.0 + theAnimals.Genotype.GrazeC[13] * StdMath.XDiv(classFeed, totalFeed);                         // Part of Eqs. 16, 17 : 1 + Cr13 * Phi 
            rateTerm = 1.0 - Math.Exp(-propnFactor * theAnimals.Genotype.GrazeC[4] * scaledFeed);                            // Eq. 16 
            timeTerm = 1.0 + theAnimals.Genotype.GrazeC[5] * Math.Exp(-propnFactor * Math.Pow(theAnimals.Genotype.GrazeC[6] * scaledFeed, 2)); // Eq. 17 
            result = fu * rateTerm * timeTerm;                                                                              // Eq. 14 

            return result;
        }

        /// <summary>
        /// Eat some pasture
        /// </summary>
        /// <param name="theAnimals">The animal group</param>
        /// <param name="classFeed"></param>
        /// <param name="totalFeed"></param>
        /// <param name="hr"></param>
        /// <param name="relQ"></param>
        /// <param name="ri"></param>
        /// <param name="fu"></param>
        private void EatPasture(AnimalGroup theAnimals, double classFeed,
                                    double totalFeed,
                                    double hr,
                                    double relQ,
                                    ref double ri,
                                    ref double fu)
        {
            double relFill;

            relFill = this.RelativeFill(theAnimals, fu, classFeed, totalFeed, hr);
            ri = ri + relFill * relQ;
            fu = StdMath.DIM(fu, relFill);
        }

        /// <summary>
        /// Weighted average of two values                                            
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        private void WeightAverage(ref double x1, double y1, double x2, double y2)
        {
            x1 = StdMath.XDiv(x1 * y1 + x2 * y2, y1 + y2);
        }

        /// <summary>
        /// Chill index
        /// </summary>
        /// <param name="temp">Temperature value</param>
        /// <param name="wind">Wind speed</param>
        /// <param name="precip">The rainfall</param>
        /// <returns>Chill value</returns>
        private double ChillFunc(double temp, double wind, double precip)
        {
            return 481.0 + (11.7 + 3.1 * Math.Sqrt(wind)) * (40.0 - temp)
                               + 418 * (1.0 - Math.Exp(-0.04 * Math.Min(80, precip)));
        }
    }
}