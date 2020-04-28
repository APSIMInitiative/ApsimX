namespace Models.GrazPlan
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Core.ApsimFile;
    using Models.PMF;
    using Newtonsoft.Json.Linq;
    using StdUnits;

    /// <summary>
    /// Information required to initialise a single animal group
    /// The YoungWt and YoungGFW fields may be set to MISSING, in which case    
    /// TStockList will estimate defaults.                                       
    /// </summary>
    [Serializable]
    public struct AnimalInits
    {
        /// <summary>
        /// Genotype of this group of animals. Must match the GenotypeName field of an element of the Genotypes property.
        /// </summary>
        public string Genotype;

        /// <summary>
        /// Number of animals
        /// </summary>
        public int Number;

        /// <summary>
        /// Sex of animals
        /// Castrated, Male, Empty, EarlyPreg, LatePreg
        /// </summary>
        public GrazType.ReproType Sex;

        /// <summary>
        /// Age in days
        /// </summary>
        [Units("d")]
        public int AgeDays;

        /// <summary>
        /// Unfasted live weight of the animals.
        /// </summary>
        [Units("kg")]
        public double Weight;

        /// <summary>
        /// Highest weight recorded to date.
        /// </summary>
        [Units("kg")]
        public double MaxPrevWt;

        /// <summary>
        /// Greasy fleece weight of the animals.
        /// </summary>
        [Units("kg")]
        public double FleeceWt;

        /// <summary>
        /// Average wool fibre diameter of the animals.
        /// </summary>
        [Units("u")]
        public double FibreDiam;

        /// <summary>
        /// Genotype of the bulls/rams to 
        /// which pregnant or lactating animals were mated. 
        /// Must match the name field of an element of the Genotypes property.
        /// </summary>
        public string MatedTo;

        /// <summary>
        /// Days pregnant
        /// Zero denotes not pregnant; 1 or more denotes the time since conception. 
        /// Only meaningful for cows/ewes.
        /// </summary>
        [Units("d")]
        public int Pregnant;

        /// <summary>
        /// Days lactating
        /// Zero denotes not lactating; 1 or more denotes the time since parturition. 
        /// Only meaningful for cows/ewes.
        /// </summary>
        [Units("d")]
        public int Lactating;

        /// <summary>
        /// Number of foetuses or suckling lambs. Only meaningful for females with Pregnant > 0.
        /// </summary>
        public int NumFoetuses;

        /// <summary>
        /// Number of suckling young. Only meaningful for cows with Lactating > 0.
        /// </summary>
        public int NumSuckling;

        /// <summary>
        /// Greasy fleece weight of suckling lambs. Only meaningful for ewes with Lactating > 0.
        /// </summary>
        [Units("kg")]
        public double YoungGFW;

        /// <summary>
        /// Unfasted live weight of suckling calves/lambs. Only meaningful for cows/ewes with lactating > 0.
        /// </summary>
        [Units("kg")]
        public double YoungWt;

        /// <summary>
        /// Birth Condition score
        /// </summary>
        public double BirthCS;

        /// <summary>
        /// Paddock occupied by the animals.
        /// </summary>
        public string Paddock;

        /// <summary>
        /// Initial tag value for the animal group.
        /// </summary>
        public int Tag;

        /// <summary>
        /// Priority accorded the animals in the Draft event
        /// </summary>
        public int Priority;
    }

    /// <summary>
    ///  Abbreviated animal initialisation set, used in TStockList.Buy                
    /// </summary>
    [Serializable]
    public struct PurchaseInfo
    {
        /// <summary>
        /// Genotype name
        /// </summary>
        public string Genotype;

        /// <summary>
        /// Number of animals
        /// </summary>
        public int Number;

        /// <summary>
        /// Live weight
        /// </summary>
        public double LiveWt;

        /// <summary>
        /// Greasy fleece weight
        /// </summary>
        public double GFW;

        /// <summary>
        /// Age in days
        /// </summary>
        public int AgeDays;

        /// <summary>
        /// Condition score
        /// </summary>
        public double CondScore;

        /// <summary>
        /// Reproduction status
        /// </summary>
        public GrazType.ReproType Repro;

        /// <summary>
        /// Mated to animal
        /// </summary>
        public string MatedTo;

        /// <summary>
        /// Pregnant days
        /// </summary>
        public int Preg;

        /// <summary>
        /// Lactation days
        /// </summary>
        public int Lact;

        /// <summary>
        /// Number of young
        /// </summary>
        public int NYoung;

        /// <summary>
        /// Weight of young
        /// </summary>
        public double YoungWt;

        /// <summary>
        /// Greasy fleece weight of young
        /// </summary>
        public double YoungGFW;
    }

    /// <summary>
    /// Attributes of a set of livstock cohorts, used in TStockList.AddStock         
    /// </summary>
    [Serializable]
    public struct CohortsInfo
    {
        /// <summary>
        /// Genotype name
        /// </summary>
        public string Genotype;

        /// <summary>
        /// Total number of animals to enter the simulation. 
        /// The animals will be distributed across the age cohorts, 
        /// taking the genotype-specific death rate into account
        /// </summary>
        public int Number;

        /// <summary>
        /// Reproduction status
        /// </summary>
        public GrazType.ReproType ReproClass;

        /// <summary>
        /// Minimum years of the youngest cohort
        /// </summary>
        public int MinYears;

        /// <summary>
        /// Maximum years of the oldest cohort
        /// </summary>
        public int MaxYears;

        /// <summary>
        /// Age offset
        /// </summary>
        public int AgeOffsetDays;

        /// <summary>
        /// Average unfasted live weight of the animals across all age cohorts
        /// </summary>
        public double MeanLiveWt;

        /// <summary>
        /// Average condition score of the animals 
        /// </summary>
        public double CondScore;

        /// <summary>
        /// Average greasy fleece weight of the animals across all age cohorts
        /// </summary>
        public double MeanGFW;

        /// <summary>
        /// Days since shearing
        /// </summary>
        public int FleeceDays;

        /// <summary>
        /// Genotype of the rams or bulls with which the animals were mated prior to entry
        /// </summary>
        public string MatedTo;

        /// <summary>
        /// Days pregnant
        /// </summary>
        public int DaysPreg;

        /// <summary>
        /// Average number of foetuses per animal (including barren animals) across all age classes
        /// </summary>
        public double Foetuses;

        /// <summary>
        /// The time since parturition in those animals that are lactating
        /// </summary>
        public int DaysLact;

        /// <summary>
        /// Average number of suckling offspring per animal (including dry animals) across all age classes
        /// </summary>
        public double Offspring;

        /// <summary>
        /// Average unfasted live weight of any suckling lambs or calves
        /// </summary>
        public double OffspringWt;

        /// <summary>
        /// Average body condition score of any suckling lambs or calves
        /// </summary>
        public double OffspringCS;

        /// <summary>
        /// Average greasy fleece weight of any suckling lambs
        /// </summary>
        public double LambGFW;
    }

    /// <summary>
    /// The container for stock
    /// </summary>
    [Serializable]
    public class StockContainer
    {
        /// <summary>
        /// Gets or sets the animal group
        /// </summary>
        public AnimalGroup Animals { get; set; }

        /// <summary>
        /// Gets or sets the paddock occupied
        /// </summary>
        public PaddockInfo PaddOccupied { get; set; }

        /// <summary>
        /// Gets or sets the tag number
        /// </summary>
        public int Tag { get; set; }

        /// <summary>
        /// Gets or sets the priority level
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// 0=mothers, 1=suckling young
        /// </summary>
        public AnimalStateInfo[] InitState = new AnimalStateInfo[2];             
        
        /// <summary>
        /// RDF factor
        /// </summary>
        public double[] RDPFactor = new double[2];      // [0..1] 
        
        /// <summary>
        /// Index is to forage-within-paddock
        /// </summary>
        public GrazType.GrazingInputs[] InitForageInputs;              
        
        /// <summary>
        /// Forage inputs
        /// </summary>
        public GrazType.GrazingInputs[] StepForageInputs;

        /// <summary>
        /// Paddock grazing inputs
        /// </summary>
        public GrazType.GrazingInputs PaddockInputs;

        /// <summary>
        /// Pasture intake
        /// </summary>
        public GrazType.GrazingOutputs[] PastIntakeRate = new GrazType.GrazingOutputs[2];
        
        /// <summary>
        /// Supplement intake
        /// </summary>
        public double[] SuppIntakeRate = new double[2];

        /// <summary>
        /// Create a stock container
        /// </summary>
        public StockContainer()
        {
            for (int i = 0; i < 2; i++)
                this.PastIntakeRate[i] = new GrazType.GrazingOutputs();
        }
    }

    /// <summary>
    /// StockList is primarily a list of AnimalGroups. Each animal group has a     
    /// "current paddock" (function getInPadd() ) and a "group tag" (function getTag()      
    /// associated with it. The correspondences between these and the animal         
    /// groups must be maintained.                                                   
    /// -                                                                               
    /// In addition, the class maintains two other lists:                            
    /// FPaddockInfo  holds paddock-specific information.  Animal groups are        
    ///                 related to the members of FPaddockInfo by the FPaddockNos     
    ///                 array.                                                        
    /// FSwardInfo    holds the herbage availabilities and amounts removed from     
    ///                 each sward (i.e. all components which respond to the          
    ///                 call for "sward2stock").  The animal groups never refer to    
    ///                 this information directly; instead, the TStockList.Dynamics   
    ///                 method (1) aggregates the availability in each sward into     
    ///                 a paddock-level total, and (2) once the grazing logic has     
    ///                 been executed it also allocates the amounts removed between   
    ///                 the various swards.  Swards are allocated to paddocks on      
    ///                 the basis of their FQDN's.                                    
    /// -                                                                              
    ///  N.B. The use of a fixed-length array for priorities and paddock numbers      
    ///       limits the number of animal groups that can be stored in this           
    ///       implementation.                                                         
    ///  N.B. The At property is 1-offset.  In many of the management methods, an     
    ///       index of 0 denotes "do to all groups".                                  
    /// </summary>
    [Serializable]
    public class StockList
    {
        /// <summary>
        /// The parent stock model.
        /// </summary>
        private Stock parentStockModel = null;

        /// <summary>
        /// False flag
        /// </summary>
        private const int FALSE = 0;

        /// <summary>
        /// True flag
        /// </summary>
        private const int TRUE = 1;
        
        /// <summary>
        /// Conversion factor for months to days
        /// </summary>
        private const double MONTH2DAY = 365.25 / 12;

        /// <summary>
        /// Converts animal mass into "dse"s for trampling purposes                     
        /// </summary>
        private const double WEIGHT2DSE = 0.02;

        /// <summary>
        /// [AnimalType] Limits to breed SRW's                 
        /// </summary>
        private double[] MINSRW = { 30.0, 300.0 };

        /// <summary>
        /// [AnimalType] Limits to breed SRW's                 
        /// </summary>          
        private double[] MAXSRW = { 120.0, 1000.0 };

        /// <summary>
        /// Base parameters
        /// </summary>
        private AnimalParamSet baseParams = null;

        /// <summary>
        /// Set of genotype parameters
        /// </summary>
        private AnimalParamSet[] genotypeParams = new AnimalParamSet[0];

        /// <summary>
        /// stock[0] is kept for use as temporary storage         
        /// </summary>
        private StockContainer[] stock = new StockContainer[0]; 
        
        /// <summary>
        /// The paddock list
        /// </summary>
        private PaddockList paddockList;

        /// <summary>
        /// The list of enterprises to manage
        /// </summary>
        private EnterpriseList enterpriseList;

        /// <summary>
        /// The list of grazing periods
        /// </summary>
        private GrazingList grazingList;

        /// <summary>
        /// List of forage providers/components
        /// </summary>
        private ForageProviders forageProviders;

        /// <summary>
        /// Gets or sets the start of the simulation
        /// </summary>
        public int StartRun { get; set; }

        /// <summary>
        /// Gets the base parameter set for this instance
        /// specified by the ParamFile
        /// </summary>
        public AnimalParamSet BaseParams
        {
            get { return baseParams; }
        }
        /// <summary>
        /// Gets the list of paddocks
        /// </summary>
        public PaddockList Paddocks
        {
            get { return this.paddockList; }
        }

        /// <summary>
        /// Gets the enterprise list
        /// </summary>
        public EnterpriseList Enterprises
        {
            get { return this.enterpriseList; }
        }

        /// <summary>
        /// Gets the grazing periods
        /// </summary>
        public GrazingList GrazingPeriods
        {
            get { return this.grazingList; }
        }

        /// <summary>
        /// Gets all the forage providers
        /// </summary>
        public ForageProviders ForagesAll
        {
            get { return this.forageProviders; }
        }

        /// <summary>
        /// Sets the animals weather
        /// </summary>
        public AnimalWeather Weather
        {
            set
            {
                this.SetWeather(value);
            }
        }

        /// <summary>
        /// Makes a copy of TAnimalParamsGlb and modifies it according to sConstFile     
        /// </summary>
        /// <param name="constFileName">The name of the parameter file</param>
        /// <returns>The animal parameter set</returns>
        public static AnimalParamSet MakeParamSet(string constFileName)
        {
            AnimalParamSet result = new AnimalParamSet((AnimalParamSet)null);
            GlobalAnimalParams animalParams = new GlobalAnimalParams();
            result.CopyAll(animalParams.AnimalParamsGlb());
            if (constFileName != string.Empty)
                GlobalParameterFactory.ParamXMLFactory().ReadFromFile(constFileName, result, true);
            result.CurrLocale = GrazLocale.DefaultLocale();

            return result;
        }

        /// <summary>
        /// posIdx is 1-offset; so is stock
        /// </summary>
        /// <param name="posIdx">The index in the stock list</param>
        /// <returns>Return the animal group</returns>
        private AnimalGroup GetAt(int posIdx)
        {
            return this.stock[posIdx].Animals;
        }

        /// <summary>
        /// Set the animal group at the index position
        /// </summary>
        /// <param name="posIdx">Index in the stock list</param>
        /// <param name="animalGroup">The animal group value</param>
        private void SetAt(int posIdx, AnimalGroup animalGroup)
        {
            if ((posIdx == this.Count() + 1) && (animalGroup != null))
                this.Add(animalGroup, this.Paddocks.ByIndex(0), 0, 0);
            else
                this.stock[posIdx].Animals = animalGroup;
        }

        /// <summary>
        /// posIdx is 1-offset; so is stock                                              
        /// </summary>
        /// <param name="posIdx">Index in the stock list</param>
        /// <returns>The paddock</returns>
        private PaddockInfo GetPaddInfo(int posIdx)
        {
            return this.stock[posIdx].PaddOccupied;
        }

        /// <summary>
        /// posIdx is 1-offset; so is stock                                              
        /// </summary>
        /// <param name="posIdx">Index in the stock list</param>
        /// <returns>Get the paddock occupied</returns>
        public string GetInPadd(int posIdx)
        {
            if ((posIdx >= 1) && (posIdx <= this.Count()))
                return this.stock[posIdx].PaddOccupied.Name;
            else
                return string.Empty;
        }

        /// <summary>
        /// posIdx is 1-offset; so is stock
        /// </summary>
        /// <param name="posIdx">Index in stock list</param>
        /// <param name="value">Paddock name</param>
        public void SetInPadd(int posIdx, string value)
        {
            PaddockInfo paddock;

            paddock = this.Paddocks.ByName(value);
            if (paddock == null)
                throw new Exception("Stock: attempt to place animals in non-existent paddock: " + value);
            else
                this.stock[posIdx].PaddOccupied = paddock;
        }

        /// <summary>
        /// posIdx is 1-offset; so is stock                                              
        /// </summary>
        /// <param name="posIdx">Index in stock list</param>
        /// <returns>The priority value</returns>
        public int GetPriority(int posIdx)
        {
            if ((posIdx >= 1) && (posIdx <= this.Count()))
                return this.stock[posIdx].Priority;
            else
                return 0;
        }

        /// <summary>
        /// posIdx is 1-offset; so is stock
        /// </summary>
        /// <param name="posIdx">Index in stock list</param>
        /// <param name="value">Priority value</param>
        public void SetPriority(int posIdx, int value)
        {
            if ((posIdx >= 1) && (posIdx <= this.Count()))
                this.stock[posIdx].Priority = value;
        }

        /// <summary>
        /// Set the weather data for the animal group
        /// </summary>
        /// <param name="theEnv">The weather data</param>
        private void SetWeather(AnimalWeather theEnv)
        {
            int i;

            for (i = 1; i <= this.Count(); i++)
            {
                this.At(i).Weather = theEnv;
                if (this.At(i).Young != null)
                    this.At(i).Young.Weather = theEnv;
            }
        }

        /// <summary>
        /// These values are paddock-specific and are stored in the FPaddocks list.        
        /// </summary>
        /// <param name="paddIdx">The paddock index</param>
        /// <param name="value">Water logging value</param>
        private void SetWaterLog(int paddIdx, double value)
        {
            PaddockInfo paddInfo;

            paddInfo = this.paddockList.ByID(paddIdx);
            if (paddInfo != null)
                paddInfo.Waterlog = value;
        }
        
        /// <summary>
        /// Combine sufficiently-similar groups of animals and delete empty ones         
        /// </summary>
        private void Merge()
        {
            int idx, jdx;
            AnimalGroup animalGroup;

            // Remove empty groups                   
            for (idx = 1; idx <= this.Count(); idx++)                                                     
            {
                if ((this.At(idx) != null) && (this.At(idx).NoAnimals == 0))
                {
                    this.SetAt(idx, null);
                }
            }

            // Merge similar groups
            for (idx = 1; idx <= this.Count() - 1; idx++)                                                                     
            {
                for (jdx = idx + 1; jdx <= this.Count(); jdx++)
                {
                    if ((this.At(idx) != null) && (this.At(jdx) != null)
                       && this.At(idx).Similar(this.At(jdx))
                       && (this.GetPaddInfo(idx) == this.GetPaddInfo(jdx))
                       && (this.GetTag(idx) == this.GetTag(jdx))
                       && (this.GetPriority(idx) == this.GetPriority(jdx)))
                    {
                        animalGroup = this.At(jdx);
                        this.SetAt(jdx, null);
                        this.At(idx).Merge(ref animalGroup);
                    }
                }
            }

            // Pack the lists and priority array.      
            for (idx = this.Count(); idx >= 1; idx--)                                              
            {
                if (this.At(idx) == null)
                    this.Delete(idx);
            }
        }

        /// <summary>
        /// Records state information prior to the grazing and nutrition calculations      
        /// so that it can be restored if there is an RDP insufficiency.                 
        /// </summary>
        /// <param name="posIdx">Index in stock list</param>
        private void StoreInitialState(int posIdx)
        {
            this.At(posIdx).storeStateInfo(ref this.stock[posIdx].InitState[0]);
            if (this.At(posIdx).Young != null)
                this.At(posIdx).Young.storeStateInfo(ref this.stock[posIdx].InitState[1]);
        }

        /// <summary>
        /// Restores state information about animal groups if there is an RDP            
        /// insufficiency. Also alters the intake limit.                                 
        /// * Assumes that stock[*].fRDPFactor[] has ben populated - see the            
        ///   computeNutritiion() method.                                                
        /// </summary>
        /// <param name="posIdx">Index in stock list</param>
        private void RevertInitialState(int posIdx)
        {
            this.At(posIdx).revertStateInfo(this.stock[posIdx].InitState[0]);
            this.At(posIdx).PotIntake = this.At(posIdx).PotIntake * this.stock[posIdx].RDPFactor[0];

            if (this.At(posIdx).Young != null)
            {
                this.At(posIdx).Young.revertStateInfo(this.stock[posIdx].InitState[1]);
                this.At(posIdx).Young.PotIntake = this.At(posIdx).Young.PotIntake * this.stock[posIdx].RDPFactor[1];
            }
        }

        /// <summary>
        /// 1. Sets the livestock inputs (other than forage and supplement amounts) for    
        ///    animal groups occupying the paddock denoted by aPaddock.                  
        /// 2. Sets up the amounts of herbage available to each animal group from each   
        ///    forage (for animal groups and forages in the paddock denoted by aPaddock).  
        /// </summary>
        /// <param name="posIdx">Index in stock list</param>
        private void SetInitialStockInputs(int posIdx)
        {
            AnimalGroup group;
            PaddockInfo paddock;
            int jdx;

            group = this.At(posIdx);
            paddock = this.GetPaddInfo(posIdx);

            group.PaddSteep = paddock.Steepness;
            group.WaterLogging = paddock.Waterlog;
            group.RationFed.Assign(paddock.SuppInPadd);                              // fTotalAmount will be overridden       

            // ensure young are fed
            if (group.Young != null)
            {
                group.Young.PaddSteep = paddock.Steepness;
                group.Young.WaterLogging = paddock.Waterlog;
                group.Young.RationFed.Assign(paddock.SuppInPadd);
            }

            Array.Resize(ref this.stock[posIdx].InitForageInputs, paddock.Forages.Count());
            Array.Resize(ref this.stock[posIdx].StepForageInputs, paddock.Forages.Count());
            for (jdx = 0; jdx <= paddock.Forages.Count() - 1; jdx++)
            {
                if (this.stock[posIdx].StepForageInputs[jdx] == null)
                    this.stock[posIdx].StepForageInputs[jdx] = new GrazType.GrazingInputs();

                this.stock[posIdx].InitForageInputs[jdx] = paddock.Forages.ByIndex(jdx).AvailForage();
                this.stock[posIdx].StepForageInputs[jdx].CopyFrom(this.stock[posIdx].InitForageInputs[jdx]);
            }
        }

        /// <summary>
        /// Caluculate ration availability
        /// </summary>
        /// <param name="posIdx">Index in stock list</param>
        public void ComputeStepAvailability(int posIdx)
        {
            PaddockInfo paddock;
            AnimalGroup group;
            double propn;
            int jdx;

            paddock = this.GetPaddInfo(posIdx);
            group = this.At(posIdx);

            this.stock[posIdx].PaddockInputs = new GrazType.GrazingInputs();
            for (jdx = 0; jdx <= paddock.Forages.Count() - 1; jdx++)
                GrazType.addGrazingInputs(jdx + 1, this.stock[posIdx].StepForageInputs[jdx], ref this.stock[posIdx].PaddockInputs);

            group.Herbage.CopyFrom(this.stock[posIdx].PaddockInputs);
            group.RationFed.Assign(paddock.SuppInPadd);                               // fTotalAmount will be overridden       

            if (paddock.SummedPotIntake > 0.0)
                propn = group.PotIntake / paddock.SummedPotIntake;                  // This is the proportion of the total   
            else                                                                        // supplement that one animal gets     
                propn = 0.0;

            group.RationFed.TotalAmount = propn * StdMath.DIM(paddock.SuppInPadd.TotalAmount, paddock.SuppRemovalKG);
            if (group.Young != null)
            {
                group.Young.Herbage.CopyFrom(this.stock[posIdx].PaddockInputs);
                group.Young.RationFed.Assign(paddock.SuppInPadd);

                if (paddock.SummedPotIntake > 0.0)
                    propn = group.Young.PotIntake / paddock.SummedPotIntake;
                else
                    propn = 0.0;
                group.Young.RationFed.TotalAmount = propn * StdMath.DIM(paddock.SuppInPadd.TotalAmount, paddock.SuppRemovalKG);
            }
        }

        /// <summary>
        /// Limits the length of a grazing sub-step so that no more than MAX_CONSUMPTION 
        /// of the herbage is consumed.                                                  
        /// </summary>
        /// <param name="paddock">The paddock</param>
        /// <returns>The step length</returns>
        private double ComputeStepLength(PaddockInfo paddock)
        {
            double result;
            const double MAX_CONSUMPTION = 0.20;

            int posn;
            double[] herbageRI = new double[GrazType.DigClassNo + 1];
            double[,] seedRI = new double[GrazType.MaxPlantSpp + 1, 3];
            double suppRelIntake = 0.0;
            double removalRate;
            double removalTime;
            int classIdx;

            posn = 1;                                                                  // Find the first animal group occupying 
            while ((posn <= this.Count()) && (this.GetPaddInfo(posn) != paddock))          // this paddock                         
                posn++;

            if ((posn > this.Count()) || (paddock.Area <= 0.0))
                result = 1.0;
            else
            {
                this.At(posn).CalculateRelIntake(this.At(posn), 1.0, false, 1.0, ref herbageRI, ref seedRI, ref suppRelIntake);

                removalTime = 9999.9;
                for (classIdx = 1; classIdx <= GrazType.DigClassNo; classIdx++)
                {
                    if (this.stock[posn].PaddockInputs.Herbage[classIdx].Biomass > 0.0)
                    {
                        removalRate = paddock.SummedPotIntake * herbageRI[classIdx] / paddock.Area;
                        if (removalRate > 0.0)
                            removalTime = Math.Min(removalTime, this.stock[posn].PaddockInputs.Herbage[classIdx].Biomass / removalRate);
                    }
                }
                result = Math.Max(0.01, Math.Min(1.0, MAX_CONSUMPTION * removalTime));
            }

            return result;
        }

        /// <summary>
        /// Calculate the intake limit
        /// </summary>
        /// <param name="group">Animal group</param>
        public void ComputeIntakeLimit(AnimalGroup group)
        {
            group.Calc_IntakeLimit();
            if (group.Young != null)
                group.Young.Calc_IntakeLimit();
        }

        /// <summary>
        /// Calculate the grazing
        /// </summary>
        /// <param name="posIdx">Position in stock list</param>
        /// <param name="startTime">Start time</param>
        /// <param name="deltaTime">Time adjustment</param>
        /// <param name="feedSuppFirst">Feed supplement first</param>
        private void ComputeGrazing(int posIdx, double startTime, double deltaTime, bool feedSuppFirst)
        {
            this.At(posIdx).Grazing(deltaTime, (startTime == 0.0), feedSuppFirst, ref this.stock[posIdx].PastIntakeRate[0], ref this.stock[posIdx].SuppIntakeRate[0]);
            if (this.At(posIdx).Young != null)
                this.At(posIdx).Young.Grazing(deltaTime, (startTime == 0.0), false, ref this.stock[posIdx].PastIntakeRate[1], ref this.stock[posIdx].SuppIntakeRate[1]);
        }

        /// <summary>
        /// Compute removal
        /// </summary>
        /// <param name="paddock">The paddock</param>
        /// <param name="deltaTime">Time adjustment</param>
        private void ComputeRemoval(PaddockInfo paddock, double deltaTime)
        {
            AnimalGroup group;
            ForageInfo forage;
            double propn;
            int posn;
            int forageIdx;
            int classIdx;
            int ripeIdx;

            if (paddock.Area > 0.0)
            {
                for (posn = 1; posn <= this.Count(); posn++)
                {
                    if (this.GetPaddInfo(posn) == paddock)
                    {
                        group = this.At(posn);

                        for (forageIdx = 0; forageIdx <= paddock.Forages.Count() - 1; forageIdx++)
                        {
                            forage = paddock.Forages.ByIndex(forageIdx);

                            for (classIdx = 1; classIdx <= GrazType.DigClassNo; classIdx++)
                            {
                                if (this.stock[posn].PaddockInputs.Herbage[classIdx].Biomass > 0.0)
                                {
                                    propn = this.stock[posn].StepForageInputs[forageIdx].Herbage[classIdx].Biomass / this.stock[posn].PaddockInputs.Herbage[classIdx].Biomass;
                                    forage.RemovalKG.Herbage[classIdx] = forage.RemovalKG.Herbage[classIdx] + (propn * deltaTime * group.NoAnimals * this.stock[posn].PastIntakeRate[0].Herbage[classIdx]);
                                    if (group.Young != null)
                                        forage.RemovalKG.Herbage[classIdx] = forage.RemovalKG.Herbage[classIdx] + (propn * deltaTime * group.Young.NoAnimals * this.stock[posn].PastIntakeRate[1].Herbage[classIdx]);
                                }
                            }

                            for (ripeIdx = GrazType.UNRIPE; ripeIdx <= GrazType.RIPE; ripeIdx++)
                                forage.RemovalKG.Seed[1, ripeIdx] = forage.RemovalKG.Seed[1, ripeIdx] + (deltaTime * group.NoAnimals * this.stock[posn].PastIntakeRate[0].Seed[forageIdx + 1, ripeIdx]);
                            if (group.Young != null)
                                for (ripeIdx = GrazType.UNRIPE; ripeIdx <= GrazType.RIPE; ripeIdx++)
                                    forage.RemovalKG.Seed[1, ripeIdx] = forage.RemovalKG.Seed[1, ripeIdx] + (deltaTime * group.Young.NoAnimals * this.stock[posn].PastIntakeRate[1].Seed[forageIdx + 1, ripeIdx]);
                        } // _ loop over forages within paddock _

                        paddock.SuppRemovalKG = paddock.SuppRemovalKG + (deltaTime * group.NoAnimals * this.stock[posn].SuppIntakeRate[0]);
                        if (group.Young != null)
                            paddock.SuppRemovalKG = paddock.SuppRemovalKG + (deltaTime * group.Young.NoAnimals * this.stock[posn].SuppIntakeRate[1]);
                    } // _ loop over animal groups within paddock _
                }

                for (posn = 1; posn <= this.Count(); posn++)
                {
                    if (this.GetPaddInfo(posn) == paddock)
                    {
                        for (forageIdx = 0; forageIdx <= paddock.Forages.Count() - 1; forageIdx++)
                        {
                            forage = paddock.Forages.ByIndex(forageIdx);

                            for (classIdx = 1; classIdx <= GrazType.DigClassNo; classIdx++)
                                this.stock[posn].StepForageInputs[forageIdx].Herbage[classIdx].Biomass = StdMath.DIM(this.stock[posn].InitForageInputs[forageIdx].Herbage[classIdx].Biomass, forage.RemovalKG.Herbage[classIdx] / paddock.Area);

                            for (ripeIdx = GrazType.UNRIPE; ripeIdx <= GrazType.RIPE; ripeIdx++)
                                this.stock[posn].StepForageInputs[forageIdx].Seeds[forageIdx + 1, ripeIdx].Biomass = StdMath.DIM(this.stock[posn].InitForageInputs[forageIdx].Seeds[forageIdx + 1, ripeIdx].Biomass, forage.RemovalKG.Seed[1, ripeIdx] / paddock.Area);
                        } // _ loop over forages within paddock _
                    }
                }
            } // _ if aPaddock.fArea > 0.0 _
        }

        /// <summary>
        /// Compute the nutrition
        /// </summary>
        /// <param name="posIdx">Index in stock list</param>
        /// <param name="availRDP">The rumen degradable protein value</param>
        private void ComputeNutrition(int posIdx, ref double availRDP)
        {
            this.At(posIdx).Nutrition();
            this.stock[posIdx].RDPFactor[0] = this.At(posIdx).RDP_IntakeFactor();
            availRDP = Math.Min(availRDP, this.stock[posIdx].RDPFactor[0]);
            if (this.At(posIdx).Young != null)
            {
                this.At(posIdx).Young.Nutrition();
                this.stock[posIdx].RDPFactor[1] = this.At(posIdx).Young.RDP_IntakeFactor();
                availRDP = Math.Min(availRDP, this.stock[posIdx].RDPFactor[1]);
            }
        }

        /// <summary>
        /// Complete the animal growth
        /// </summary>
        /// <param name="posIdx">Index in the stock list</param>
        private void CompleteGrowth(int posIdx)
        {
            this.At(posIdx).completeGrowth(this.stock[posIdx].RDPFactor[0]);
            if (this.At(posIdx).Young != null)
                this.At(posIdx).Young.completeGrowth(this.stock[posIdx].RDPFactor[1]);
        }

        /// <summary>
        /// Get the paddock rank value
        /// </summary>
        /// <param name="paddock">The paddock</param>
        /// <param name="animalGroup">The animal group</param>
        /// <returns>The paddock rank</returns>
        private double GetPaddockRank(PaddockInfo paddock, AnimalGroup animalGroup)
        {
            double result;
            GrazType.GrazingInputs forageInputs;
            GrazType.GrazingInputs paddockInputs;
            double[] herbageRI = new double[GrazType.DigClassNo + 1];
            double[,] seedRI = new double[GrazType.MaxPlantSpp + 1, GrazType.RIPE + 1];
            double dummy = 0.0;
            int jdx;
            int classIdx;

            animalGroup.PaddSteep = paddock.Steepness;
            animalGroup.WaterLogging = paddock.Waterlog;
            animalGroup.RationFed.Assign(paddock.SuppInPadd);
            animalGroup.RationFed.TotalAmount = 0.0;                                        // No supplementary feed here            

            paddockInputs = new GrazType.GrazingInputs();
            for (jdx = 0; jdx <= paddock.Forages.Count() - 1; jdx++)
            {
                forageInputs = paddock.Forages.ByIndex(jdx).AvailForage();
                GrazType.addGrazingInputs(jdx + 1, forageInputs, ref paddockInputs);
            }
            animalGroup.Herbage = paddockInputs;

            animalGroup.CalculateRelIntake(animalGroup, 1.0, false, 1.0, ref herbageRI, ref seedRI, ref dummy);

            // Function result is DMDI/pot. intake   
            result = 0.0;
            for (classIdx = 1; classIdx <= GrazType.DigClassNo; classIdx++)                                             
                result = result + (herbageRI[classIdx] * GrazType.ClassDig[classIdx]);
            return result;
        }
        
        // management events described by the livestock dialog

        /// <summary>
        /// Do daily tasks
        /// </summary>
        /// <param name="currentDay">Todays date</param>
        /// <param name="curEnt">Current enterprise</param>
        protected void ManageDailyTasks(int currentDay, EnterpriseInfo curEnt)
        {
        }
 
        /// <summary>
        /// Process the reproduction logic specified by the dialog.
        /// Mating, Castrating, Weaning.
        /// </summary>
        /// <param name="currentDay">The current day</param>
        /// <param name="curEnt">Current enterprise</param>
        protected void ManageReproduction(int currentDay, EnterpriseInfo curEnt)
        {
            int g;
            int t;
            int tagNo;
            int groups;
            int birthDOY;
            int gestation;
            bool found;

            // Mating day
            if (curEnt.MateDay == currentDay)
            {
                // only mate the groups in this enterprise
                g = 1;
                groups = this.Count();
                while (g <= groups)                                                 
                {
                    // for each tag that needs to be mated
                    for (t = 1; t <= curEnt.MateTagCount; t++)                      
                    {
                        tagNo = curEnt.GetMateTag(t);

                        // if mate this group and this group belongs to this ent
                        if ((tagNo == this.GetTag(g)) && (curEnt.ContainsTag(this.GetTag(g))))    
                        {
                            if (this.At(g).AgeDays >= (365 * curEnt.MateYears))
                            {
                                this.Join(g, curEnt.MateWith, 42);
                                this.SetTag(g, curEnt.JoinedTag);                        // retag the ewes that are mated into a ewe tag group
                            }
                        }
                    }
                    g++;
                }
            }

            // Castrate day
            if (curEnt.Castrate)
            {
                if (curEnt.IsCattle)
                    gestation = EnterpriseInfo.COWGESTATION;
                else
                    gestation = EnterpriseInfo.EWEGESTATION;
                birthDOY = StdDate.DateShift(curEnt.MateDay, gestation, 0, 0);

                // if 30 days after birth (??)
                if (StdDate.DateShift(birthDOY, 30, 0, 0) == currentDay)     
                {
                    g = 1;
                    groups = this.Count();
                    while (g <= groups)                             
                    {
                        // if this group belongs to this ent
                        if (curEnt.ContainsTag(this.GetTag(g)))          
                        {
                            this.Castrate(g, this.At(g).NoAnimals); // castrate all male young in the group
                        }
                        g++;
                    }
                }
            }

            // Weaning day
            if (curEnt.WeanDay == currentDay)
            {
                g = 1;
                groups = this.Count();                              // store the group count because it changes
                while (g <= groups)                         
                {
                    // if this group belongs to this ent
                    if (curEnt.ContainsTag(this.GetTag(g)))              
                    {
                        this.Wean(g, this.At(g).NoAnimals, true, true);  // wean all young in the group
                        if (curEnt.IsCattle)
                            this.DryOff(g, this.At(g).NoAnimals);   // ## may be possible to include option in user interface
                                                                    // retag the mothers into dry ewes tag group
                        this.SetTag(g, curEnt.DryTag);
                    }
                    g++;
                }

                // go through all the new groups and determine the new weaner tag for them
                for (g = groups + 1; g <= this.Count(); g++)
                {
                    found = false;
                    t = 1;
                    while (!found && (t <= curEnt.MateTagCount))
                    {
                        if (this.GetTag(g) == curEnt.GetMateTag(t))
                            found = true;                           // this tag belongs to a mated group
                        t++;
                    }
                    if (found)
                    {
                        // retag the weaners
                        if (this.At(g).MaleNo > 0)
                        {
                            this.SetTag(g, curEnt.WeanerMTag);
                        }

                        // the new group will be retagged M/F
                        if (this.At(g).FemaleNo > 0)
                        {
                            this.SetTag(g, curEnt.WeanerFTag);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// There can be a number of grazing periods. Each of these can include the
        /// movement of any number of tag groups to any paddocks. There are two types
        /// of grazing period, Fixed and Flexible.
        /// </summary>
        /// <param name="currentDate">The current date</param>
        /// <param name="currentDay">The current day</param>
        /// <param name="curEnt">The enterprise</param>
        protected void ManageGrazing(int currentDate, int currentDay, EnterpriseInfo curEnt)
        {
            int p;
            int paddock, atag;
            int tagNo;
            int paddockIter, tagIter;
            int stockedIdx;
            List<string> exclPaddocks;
            bool found;
            int index;

            // for each grazing period
            for (p = 1; p <= this.GrazingPeriods.Count(); p++)   
            {
                // if this period applies - within dates or within wrapped dates
                if (this.TodayIsInPeriod(currentDay, this.GrazingPeriods.GetStartDay(p), this.GrazingPeriods.GetFinishDay(p)))
                {
                    // if fixed period
                    if ((this.GrazingPeriods.GetPeriodType(p).ToLower()) == EnterpriseInfo.PERIOD_TEXT[EnterpriseInfo.FIXEDPERIOD].ToLower()) 
                    {
                        // move the tag groups to their paddocks
                        // (they may already be there, although it is possible they may not be due to starting part way through the period)
                        // for each paddock in this grazing period
                        for (paddockIter = 1; paddockIter <= this.GrazingPeriods.GetFixedPaddCount(p); paddockIter++)                
                        {
                            paddock = this.GrazingPeriods.GetFixedPadd(p, paddockIter);                                             // test this paddock index

                            // for each tag group that is planned for this paddock
                            for (tagIter = 1; tagIter <= this.GrazingPeriods.GetFixedPaddTagCount(p, paddockIter); tagIter++)        
                            {
                                atag = this.GrazingPeriods.GetFixedPaddTag(p, paddockIter, tagIter);
                                if (curEnt.ContainsTag(atag))
                                {
                                    stockedIdx = this.PaddockIndexStockedByTagNo(atag);
                                    if (paddock != stockedIdx)
                                        this.MoveTagToPaddock(atag, paddock);
                                }
                            }
                        }
                    }
                    else if (this.GrazingPeriods.GetPeriodType(p).ToLower() == EnterpriseInfo.PERIOD_TEXT[EnterpriseInfo.FLEXIBLEPERIOD].ToLower())     
                    {
                        // Flexible grazing
                        // X day intervals from the start of the period - is this the check day or day one?
                        if ((this.GrazingPeriods.GetMoveCheck(p) > 0) && (StdDate.Interval(this.GrazingPeriods.GetStartDay(p), currentDay) % this.GrazingPeriods.GetMoveCheck(p) == 0)) 
                        {
                            // else if drafting for this period then
                            if (this.GrazingPeriods.GetCriteria(p) == this.CRITERIA[DRAFT_MOVE])                                      
                            {
                                // get the list of excluded paddocks
                                exclPaddocks = new List<string>();

                                // for each tag in the GrazingPeriod
                                for (tagIter = 1; tagIter <= this.GrazingPeriods.GetTagCount(p); tagIter++)      
                                {
                                    for (index = 1; index <= this.Paddocks.Count() - 1; index++)
                                    {
                                        found = false;
                                        paddockIter = 1;

                                        // for each paddock in the GrazingPeriod
                                        while (paddockIter <= this.GrazingPeriods.GetTagPaddocks(p, tagIter))    
                                        {
                                            paddock = this.GrazingPeriods.GetPaddock(p, tagIter, paddockIter);
                                            if (paddock == index)
                                                found = true;
                                            paddockIter++;
                                        }
                                        if (!found)
                                            exclPaddocks.Add(this.Paddocks.ByIndex(index).Name);           // add to the exclude list
                                    }
                                    tagNo = this.GrazingPeriods.GetTag(p, tagIter);
                                    this.Draft(tagNo, exclPaddocks); // now do the draft only for this tagno
                                } // next tag
                            }
                        }
                    }
                }
            } // next period
        }

        /// <summary>
        /// For a given day of year, obtains the ages (in years, rounded down) of   
        /// the youngest and oldest animals in a flock/herd from the policy for     
        /// additions to and sales from it.                                         
        /// </summary>
        /// <param name="enterDOY">Day of year for entry to the flock/herd</param>
        /// <param name="enterDays">Age in days at entry</param>
        /// <param name="sale_yrs">The age at sale</param>
        /// <param name="sale_day">The day of sale</param>
        /// <param name="todaysDate">Today's date</param>
        /// <param name="youngYrs">Youngest age</param>
        /// <param name="oldYrs">Oldest age</param>
        protected void GetAgeRange(int enterDOY, int enterDays, int sale_yrs, int sale_day, int todaysDate, ref int youngYrs, ref int oldYrs)
        {
            int ageAtSale;                                                  // Age of animals at sale (in days)         
            int timeSinceEntry;                                             // Time since last entry of animals (days)  
            int timeSinceSale;                                              // Time since last sale                     

            ageAtSale = (366 * sale_yrs) + ((enterDays + this.DaysFromDOY(enterDOY, sale_day)) % 366);

            // If todaysDate is the same day-of-year as enterDOY, then the entry hasn't happened yet        
            timeSinceEntry = this.DaysFromDOY(enterDOY, todaysDate);       
            if (timeSinceEntry == 0)                                       
                timeSinceEntry = 366;                                                         

            timeSinceSale = this.DaysFromDOY(sale_day, todaysDate);         // Ditto for the sale day-of-year           
            if (timeSinceSale == 0)
                timeSinceSale = 366;

            youngYrs = (enterDays + timeSinceEntry) / 366;
            oldYrs = ((ageAtSale + timeSinceSale) / 366) - 1;                 // Oldest animals left were AgeAtSale-366   
                                                                            // days old on the last Sales.DOY         
        }

        /// <summary>
        /// Find the index of the paddock that this tag group is currently grazing
        /// </summary>
        /// <param name="tagNo">The tag number</param>
        /// <returns>The paddock index</returns>
        protected int PaddockIndexStockedByTagNo(int tagNo)
        {
            int i;
            int posIdx;
            bool found;

            int result = -1;
            found = false;
            for (posIdx = 1; posIdx <= this.Count(); posIdx++)
            {
                if (this.stock[posIdx].Tag == tagNo)
                {
                    i = 1;
                    while (!found && (i < this.Paddocks.Count()))
                    {
                        if (this.Paddocks.ByIndex(i).Name == this.stock[posIdx].PaddOccupied.Name)
                        {
                            result = i;
                            found = true;
                        }
                        i++;
                    }
                }
            }  // next animal index

            return result;
        }

        /// <summary>
        /// Move a tagged group of animals to a paddock by index.
        /// </summary>
        /// <param name="tagNo">The tag number</param>
        /// <param name="paddockIdx">The paddock index</param>
        protected void MoveTagToPaddock(int tagNo, int paddockIdx)
        {
            int g, groups;

            g = 1;
            groups = this.Count();
            while (g <= groups)                                             
            {
                // if this group belongs to this enterprise
                if (tagNo == this.GetTag(g))                                     
                {
                    this.SetInPadd(g, this.Paddocks.ByIndex(paddockIdx).Name);  // move them to this paddock
                }
                g++;
            }
        }

        /// <summary>
        /// Check date to see if it is in this range - handles 1 Jan wrapping.
        /// </summary>
        /// <param name="currentDay">The date to test</param>
        /// <param name="periodstart">Start of the period</param>
        /// <param name="periodfinish">End of the period</param>
        /// <returns>True if the date is in the period</returns>
        public bool TodayIsInPeriod(int currentDay, int periodstart, int periodfinish)
        {
            bool result = false;
            if (((periodstart <= currentDay) && (periodfinish >= currentDay))
              || ((periodstart > periodfinish) && ((periodfinish >= currentDay) || (periodstart <= currentDay))))
                result = true;
            return result;
        }

        /// <summary>
        /// Create a TStockList
        /// </summary>
        /// <param name="stockModel">The parent stock model.</param>
        public StockList(Stock stockModel)
        {
            this.parentStockModel = stockModel;
            this.StartRun = 0;
            Array.Resize(ref this.stock, 1);                                          // Set aside temporary storage           
            this.paddockList = new PaddockList();
            this.paddockList.Add(-1, string.Empty);                                      // The "null" paddock is added here      
            //  FForages  := TForageList.Create( TRUE );
            this.forageProviders = new ForageProviders();
            this.enterpriseList = new EnterpriseList();
            this.grazingList = new GrazingList();
        }

        /// <summary>
        /// Get the genotype count
        /// </summary>
        /// <returns>The number of genotypes</returns>
        public int GenotypeCount()
        {
            return this.genotypeParams.Length;
        }

        /// <summary>
        /// Get the genotype at the index
        /// </summary>
        /// <param name="idx">Genotype index</param>
        /// <returns>The genotype</returns>
        public AnimalParamSet GetGenotype(int idx)
        {
            return this.genotypeParams[idx];
        }

        /// <summary>
        /// Locate a genotype in FGenotypes. If this fails, try searching for it in the  
        /// main parameter set and adding it to FGenotypes.                            
        /// </summary>
        /// <param name="genoName">The genotype name</param>
        /// <returns>The genotype</returns>
        public AnimalParamSet GetGenotype(string genoName)
        {
            int idx;
            AnimalParamSet srcParamSet;

            AnimalParamSet result = null;
            if ((genoName == string.Empty) && (this.genotypeParams.Length >= 1))                           // Null string is a special case         
                result = this.genotypeParams[0];
            else
            {
                idx = 0;
                while ((idx < this.genotypeParams.Length) && (genoName.ToLower() != this.genotypeParams[idx].Name.ToLower()))
                    idx++;

                if (idx < this.genotypeParams.Length)
                    result = this.genotypeParams[idx];
                else
                {
                    srcParamSet = parentStockModel.Genotypes.Get(genoName).Parameters;
                    srcParamSet.EnglishName = genoName;
                    srcParamSet.DeriveParams();
                    //srcParamSet.Initialise();

                    if (srcParamSet != null)
                    {
                        result = new AnimalParamSet(null, srcParamSet);
                        idx = this.genotypeParams.Length;
                        Array.Resize(ref this.genotypeParams, idx + 1);
                        this.genotypeParams[idx] = result;
                    }
                }
            }

            if (result == null)
                throw new Exception("Genotype name \"" + genoName + "\" not recognised");

            return result;
        }

        /// <summary>
        /// Add a group of animals to the list                                           
        /// Returns the group index of the group that was added. 0->n                    
        /// </summary>
        /// <param name="animalGroup">Animal group</param>
        /// <param name="paddInfo">The paddock information</param>
        /// <param name="tagNo">Tag value</param>
        /// <param name="priority">Priority number</param>
        /// <returns>The index of the new group in the stock array</returns>
        public int Add(AnimalGroup animalGroup, PaddockInfo paddInfo, int tagNo, int priority)
        {
            int idx;

            animalGroup.Calc_IntakeLimit();

            idx = this.stock.Length;
            Array.Resize(ref this.stock, idx + 1);
            this.stock[idx] = new StockContainer();
            this.stock[idx].Animals = animalGroup.Copy();
            this.stock[idx].PaddOccupied = paddInfo;
            this.stock[idx].Tag = tagNo;
            this.stock[idx].Priority = priority;

            this.SetInitialStockInputs(idx);
            return idx;
        }

        /// <summary>
        /// Returns the group index of the group that was added. 0->n                    
        /// </summary>
        /// <param name="animalInits">The animal data</param>
        /// <returns>The index of the new animal group</returns>
        public int Add(AnimalInits animalInits)
        {
            AnimalGroup newGroup;
            PaddockInfo paddock;

            newGroup = new AnimalGroup(
                                        this.GetGenotype(animalInits.Genotype),
                                        animalInits.Sex,
                                        animalInits.Number,
                                        animalInits.AgeDays,
                                        animalInits.Weight,
                                        animalInits.FleeceWt,
                                        parentStockModel.randFactory);
            if (this.IsGiven(animalInits.MaxPrevWt))
                newGroup.MaxPrevWeight = animalInits.MaxPrevWt;
            if (this.IsGiven(animalInits.FibreDiam))
                newGroup.FibreDiam = animalInits.FibreDiam;

            if (animalInits.MatedTo != string.Empty)
                newGroup.MatedTo = this.GetGenotype(animalInits.MatedTo);
            if ((newGroup.ReproState == GrazType.ReproType.Empty) && (animalInits.Pregnant > 0))
            {
                newGroup.Pregnancy = animalInits.Pregnant;
                if (animalInits.NumFoetuses > 0)
                    newGroup.NoFoetuses = animalInits.NumFoetuses;
            }
            if (((newGroup.ReproState == GrazType.ReproType.Empty) || (newGroup.ReproState == GrazType.ReproType.EarlyPreg) || (newGroup.ReproState == GrazType.ReproType.LatePreg)) && (animalInits.Lactating > 0))
            {
                newGroup.Lactation = animalInits.Lactating;
                if (animalInits.NumSuckling > 0)
                    newGroup.NoOffspring = animalInits.NumSuckling;
                else if ((newGroup.Animal == GrazType.AnimalType.Cattle) && (animalInits.NumSuckling == 0))
                {
                    newGroup.Young = null;
                }
                if (this.IsGiven(animalInits.BirthCS))
                    newGroup.BirthCondition = AnimalParamSet.CondScore2Condition(animalInits.BirthCS, AnimalParamSet.Cond_System.csSYSTEM1_5);
            }

            if (newGroup.Young != null)
            {
                if (this.IsGiven(animalInits.YoungWt))
                    newGroup.Young.LiveWeight = animalInits.YoungWt;
                if (this.IsGiven(animalInits.YoungGFW))
                    newGroup.Young.FleeceCutWeight = animalInits.YoungGFW;
            }

            paddock = this.paddockList.ByName(animalInits.Paddock.ToLower());
            if (paddock == null)
                paddock = this.paddockList.ByIndex(0);

            return this.Add(newGroup, paddock, animalInits.Tag, animalInits.Priority);
        }

        ///// <summary>Add a group of animals to the list.</summary>
        ///// <param name="newGroup">New animal group.</param>
        ///// <returns>The index of the new group in the stock array. 0 based.</returns>
        //public int Add(AnimalGroup newGroup)
        //{
        //    newGroup.InitialiseFromParameters();
        //    var paddock = this.paddockList.ByName(newGroup.PaddockName.ToLower());
        //    if (paddock == null)
        //        paddock = this.paddockList.ByIndex(0);

        //    return this.Add(newGroup, paddock, newGroup.Tag, newGroup.Priority);
        //}

        /// <summary>
        ///  * N.B. posn is 1-offset; stock list is effectively also a 1-offset array        
        /// </summary>
        /// <param name="posn">In all methods, posn is 1-offset</param>
        public void Delete(int posn)
        {
            int count;
            int idx;

            count = this.Count();
            if ((posn >= 1) && (posn <= count))
            {
                this.stock[posn].Animals = null;
                this.stock[posn].InitForageInputs = null;
                this.stock[posn].StepForageInputs = null;

                for (idx = posn + 1; idx <= count; idx++)
                    this.stock[idx - 1] = this.stock[idx];
                Array.Resize(ref this.stock, count);                                               // Leave stock[0] as temporary storage  
            }
        }

        /// <summary>
        /// Clear the list
        /// </summary>
        public void Clear()
        {
            while (this.Count() > 0)
                this.Delete(this.Count());
        }

        /// <summary>
        /// Remove empty groups                   
        /// </summary>
        public void Pack()
        {
            int idx;

            for (idx = 1; idx <= this.Count(); idx++)
            {
                if ((this.At(idx) != null) && (this.At(idx).NoAnimals == 0))
                {
                    this.SetAt(idx, null);
                }
            }

            for (idx = this.Count(); idx >= 1; idx++)
                if (this.At(idx) == null)
                    this.Delete(idx);
        }

        /// <summary>
        /// Only groups 1 to Length()-1 are counted                                    
        /// </summary>
        /// <returns>The number of items in the stock list</returns>
        public int Count()
        {
            return this.stock.Length - 1;
        }

        /// <summary>
        /// Get the animal group at the position
        /// </summary>
        /// <param name="posn">The position in the list</param>
        /// <returns>The animal group at the index position</returns>
        public AnimalGroup At(int posn)
        {
            return this.GetAt(posn);
        }

        /// <summary>
        /// 
        /// </summary>
        public List<AnimalGroup> Animals {  get { return Animals; } }

        /// <summary>
        /// posIdx is 1-offset; so is stock                                              
        /// </summary>
        /// <param name="posIdx">The position in the stock list</param>
        /// <returns>The tag number</returns>
        public int GetTag(int posIdx)
        {
            if ((posIdx >= 1) && (posIdx <= this.Count()))
                return this.stock[posIdx].Tag;
            else
                return 0;
        }

        /// <summary>
        /// Set the tag value
        /// </summary>
        /// <param name="posIdx">The position in the stock list</param>
        /// <param name="value">Tag value</param>
        public void SetTag(int posIdx, int value)
        {
            if ((posIdx >= 1) && (posIdx <= this.Count()))
                this.stock[posIdx].Tag = value;
        }

        /// <summary>
        /// Get the highest tag number
        /// </summary>
        /// <returns>The highest tag value in the list</returns>
        public int HighestTag()
        {
            int idx;

            int result = 0;
            for (idx = 1; idx <= this.Count(); idx++)
                result = Math.Max(result, this.GetTag(idx));
            return result;
        }

        /// <summary>
        /// Place the supplement in the paddock
        /// </summary>
        /// <param name="paddName">Paddock name</param>
        /// <param name="suppKG">The amount of supplement</param>
        /// <param name="supplement">The supplement to use</param>
        /// <param name="feedSuppFirst">Feed supplement first</param>
        public void PlaceSuppInPadd(string paddName, double suppKG, FoodSupplement supplement, bool feedSuppFirst)
        {
            PaddockInfo thePadd;

            thePadd = this.Paddocks.ByName(paddName);
            if (thePadd == null)
                throw new Exception("Stock: attempt to feed supplement into non-existent paddock");
            else
                thePadd.FeedSupplement(suppKG, supplement, feedSuppFirst);
        }

        // Model execution routines ................................................

        /// <summary>
        /// Initiate the time step for the paddocks
        /// </summary>
        public void BeginTimeStep()
        {
            this.Paddocks.BeginTimeStep();
        }

        /// <summary>
        /// Advance the list by one time step.  All the input properties should be set first                                                                        
        /// </summary>
        public void Dynamics()
        {
            const double EPS = 1.0E-6;

            double totPotIntake;
            AnimalList newGroups;
            PaddockInfo thePaddock;
            double timeValue;
            double delta;
            double RDP;
            int paddIdx, idx, n;
            int iterator;
            bool done;

            for (paddIdx = 0; paddIdx <= this.Paddocks.Count() - 1; paddIdx++)
            {
                thePaddock = this.Paddocks.ByIndex(paddIdx);
                thePaddock.ComputeTotals();
            }

            for (idx = 1; idx <= this.Count(); idx++)
                this.SetInitialStockInputs(idx);

            // Aging, birth, deaths etc. Animal groups may appear in the NewGroups list as a result of processes such as lamb deaths
            n = this.Count();
            for (idx = 1; idx <= n; idx++)                                                  
            {                                                                               
                newGroups = null;                                                            
                this.At(idx).Age(1, ref newGroups);

                // Ensure the new young have climate data                             
                if (this.At(idx).Young != null)                                             
                    this.At(idx).Young.Weather = this.At(idx).Weather;
                this.Add(newGroups, this.GetPaddInfo(idx), this.GetTag(idx), this.GetPriority(idx));       // The new groups are added back onto    
                newGroups = null;                                                           // the main list                       
            }

            this.Merge();                                                                        // Merge any similar animal groups       

            // Now run the grazing and nutrition models. This process is quite involved...                         
            for (idx = 1; idx <= this.Count(); idx++)                                       
            {
                this.StoreInitialState(idx);                                                     
                this.ComputeIntakeLimit(this.At(idx));
                this.At(idx).Reset_Grazing();
            }

            // Compute the total potential intake (used to distribute supplement between groups of animals)         
            for (paddIdx = 0; paddIdx <= this.Paddocks.Count() - 1; paddIdx++)              
            {                                                                               
                thePaddock = this.Paddocks.ByIndex(paddIdx);                                  
                totPotIntake = 0.0;

                for (idx = 1; idx <= this.Count(); idx++)
                    if (this.GetPaddInfo(idx) == thePaddock)
                    {
                        totPotIntake = totPotIntake + (this.At(idx).NoAnimals * this.At(idx).PotIntake);
                        if (this.At(idx).Young != null)
                            totPotIntake = totPotIntake + (this.At(idx).Young.NoAnimals * this.At(idx).Young.PotIntake);
                    }
                thePaddock.SummedPotIntake = totPotIntake;
            }

            // We loop over paddocks and then over animal groups within a paddock so that we can take account of herbage 
            for (paddIdx = 0; paddIdx <= this.Paddocks.Count() - 1; paddIdx++)                       
            {                                                                               
                thePaddock = this.Paddocks.ByIndex(paddIdx);
                                                   
                // removal & its effect on intake      
                iterator = 1;                                                                  // This loop handles RDP insufficiency   
                done = false;
                while (!done)
                {
                    timeValue = 0.0;                                                            // Variable-length substeps for grazing  

                    while (timeValue < 1.0 - EPS)
                    {
                        for (idx = 1; idx <= this.Count(); idx++)
                            if (this.GetPaddInfo(idx) == thePaddock)
                                this.ComputeStepAvailability(idx);

                        delta = Math.Min(this.ComputeStepLength(thePaddock), 1.0 - timeValue);

                        // Compute rate of grazing for this substep                             
                        for (idx = 1; idx <= this.Count(); idx++)                           
                            if (this.GetPaddInfo(idx) == thePaddock)                             
                                this.ComputeGrazing(idx, timeValue, delta, thePaddock.FeedSuppFirst);

                        this.ComputeRemoval(thePaddock, delta);

                        timeValue = timeValue + delta;
                    } // _ grazing sub-steps loop _

                    // Nutrition submodel here...            
                    RDP = 1.0;
                    for (idx = 1; idx <= this.Count(); idx++)                               
                        if (this.GetPaddInfo(idx) == thePaddock)
                            this.ComputeNutrition(idx, ref RDP);

                    // Maximum of 2 iterations in the RDP loop
                    if (iterator == 2)                                                                                         
                        done = true;                                                       
                    else
                    {
                        done = (RDP == 1.0);                                              // Is there an animal group in this paddock with an RDP insufficiency?  
                        if (!done)                                                         
                        {
                            thePaddock.ZeroRemoval();

                            // If so, we have to revert the state of the animal group ready for the second iteration.
                            for (idx = 1; idx <= this.Count(); idx++)                       
                                if (this.GetPaddInfo(idx) == thePaddock)
                                    this.RevertInitialState(idx);                                                   
                        }
                    }

                    iterator++;
                } // _ RDP loop _
            } // _ loop over paddocks _

            for (idx = 1; idx <= this.Count(); idx++)
                this.CompleteGrowth(idx);
        }

        // Outputs to other models .................................................

        /// <summary>
        /// Get the mass for the area
        /// </summary>
        /// <param name="paddID">Paddock id</param>
        /// <param name="provider">The forage provider object</param>
        /// <param name="units">The units</param>
        /// <returns>The mass</returns>
        public double ReturnMassPerArea(int paddID, ForageProvider provider, string units)
        {
            double result;
            PaddockInfo thePadd;
            double massKGHA;
            int idx;

            if (provider != null)
                thePadd = provider.OwningPaddock;
            else
                thePadd = this.paddockList.ByID(paddID);

            massKGHA = 0.0;
            if (thePadd != null)
            {
                for (idx = 1; idx <= this.Count(); idx++)
                    if (this.GetPaddInfo(idx) == thePadd)
                    {
                        massKGHA = massKGHA + (this.At(idx).NoAnimals * this.At(idx).LiveWeight);
                        if (this.At(idx).Young != null)
                            massKGHA = massKGHA + (this.At(idx).Young.NoAnimals * this.At(idx).Young.LiveWeight);
                    }
                massKGHA = massKGHA / thePadd.Area;
            }

            if (units == "kg/ha")
                result = massKGHA;
            else if (units == "kg/m^2")
                result = massKGHA * 0.0001;
            else if (units == "dse/ha")
                result = massKGHA * WEIGHT2DSE;
            else if (units == "g/m^2")
                result = massKGHA * 0.1;
            else
                throw new Exception("Stock: Unit (" + units + ") not recognised");

            return result;
        }

        // function    returnRemoval(     iForageID : Integer; sUnit : string   ) : TGrazingOutputs;

        /// <summary>
        /// Calculate the weighted mean
        /// </summary>
        /// <param name="dY1">First Y value</param>
        /// <param name="dY2">Second Y value</param>
        /// <param name="dX1">First X value</param>
        /// <param name="dX2">Second X value</param>
        /// <returns>The weighted mean</returns>
        private double WeightedMean(double dY1, double dY2, double dX1, double dX2)
        {
            if (dX1 + dX2 > 0.0)
                return ((dX1 * dY1) + (dX2 * dY2)) / (dX1 + dX2);
            else
                return 0;
        }

        /// <summary>
        /// Used by returnExcretion()
        /// </summary>
        /// <param name="destExcretion">Output excretion data</param>
        /// <param name="srcExcretion">The excretion data</param>
        private void AddExcretions(ref ExcretionInfo destExcretion, ExcretionInfo srcExcretion)
        {
            if (srcExcretion.Defaecations > 0.0)
            {
                destExcretion.DefaecationVolume = this.WeightedMean(
                                                                    destExcretion.DefaecationVolume, 
                                                                    srcExcretion.DefaecationVolume,
                                                                    destExcretion.Defaecations, 
                                                                    srcExcretion.Defaecations);
                destExcretion.DefaecationArea = this.WeightedMean( 
                                                                    destExcretion.DefaecationArea, 
                                                                    srcExcretion.DefaecationArea,
                                                                    destExcretion.Defaecations, 
                                                                    srcExcretion.Defaecations);
                destExcretion.DefaecationEccentricity = this.WeightedMean(
                                                                            destExcretion.DefaecationEccentricity, 
                                                                            srcExcretion.DefaecationEccentricity,
                                                                            destExcretion.Defaecations, 
                                                                            srcExcretion.Defaecations);
                destExcretion.FaecalNO3Propn = this.WeightedMean(
                                                                    destExcretion.FaecalNO3Propn, 
                                                                    srcExcretion.FaecalNO3Propn,
                                                                    destExcretion.InOrgFaeces.Nu[(int)GrazType.TOMElement.n], 
                                                                    srcExcretion.InOrgFaeces.Nu[(int)GrazType.TOMElement.n]);
                destExcretion.Defaecations = destExcretion.Defaecations + srcExcretion.Defaecations;

                destExcretion.OrgFaeces = this.AddDMPool(destExcretion.OrgFaeces, srcExcretion.OrgFaeces);
                destExcretion.InOrgFaeces = this.AddDMPool(destExcretion.InOrgFaeces, srcExcretion.InOrgFaeces);
            }

            if (srcExcretion.Urinations > 0.0)
            {
                destExcretion.UrinationVolume = this.WeightedMean(
                                                                    destExcretion.UrinationVolume, 
                                                                    srcExcretion.UrinationVolume,
                                                                    destExcretion.Urinations, 
                                                                    srcExcretion.Urinations);
                destExcretion.UrinationArea = this.WeightedMean(
                                                                    destExcretion.UrinationArea, 
                                                                    srcExcretion.UrinationArea,
                                                                    destExcretion.Urinations, 
                                                                    srcExcretion.Urinations);
                destExcretion.dUrinationEccentricity = this.WeightedMean(
                                                                            destExcretion.dUrinationEccentricity, 
                                                                            srcExcretion.dUrinationEccentricity,
                                                                            destExcretion.Urinations, 
                                                                            srcExcretion.Urinations);
                destExcretion.Urinations = destExcretion.Urinations + srcExcretion.Urinations;

                destExcretion.Urine = this.AddDMPool(destExcretion.Urine, srcExcretion.Urine);
            }
        }

        /// <summary>
        /// Parameters:                                                               
        /// OrgFaeces    kg/ha  Excretion of organic matter in faeces                 
        /// InorgFaeces  kg/ha  Excretion of inorganic nutrients in faeces            
        /// Urine        kg/ha  Excretion of nutrients in urine                       
        /// -                                                                          
        /// Note:  TAnimalGroup.OrgFaeces returns the OM faecal excretion in kg, and  
        ///        is the total of mothers and young where appropriate; similarly for   
        ///        TAnimalGroup.InorgFaeces and TAnimalGroup.Urine.                   
        ///        TAnimalGroup.FaecalAA and TAnimalGroup.UrineAAN return weighted    
        ///        averages over mothers and young where appropriate. As a result we  
        ///        don't need to concern ourselves with unweaned young in this        
        ///        particular calculation except when computing PatchFract.           
        /// </summary>
        /// <param name="paddID">Paddock ID</param>
        /// <param name="excretion">The excretion info</param>
        public void ReturnExcretion(int paddID, out ExcretionInfo excretion)
        {
            PaddockInfo thePadd;
            double area;
            int idx;

            thePadd = this.paddockList.ByID(paddID);

            if (thePadd != null)
                area = thePadd.Area;
            else if (this.paddockList.Count() == 0)
                area = 1.0;
            else
            {
                area = 0.0;
                for (idx = 0; idx <= this.paddockList.Count() - 1; idx++)
                    area = area + this.paddockList.ByIndex(idx).Area;
            }

            excretion = new ExcretionInfo();
            for (idx = 1; idx <= this.Count(); idx++)
            {
                if ((thePadd == null) || (this.GetPaddInfo(idx) == thePadd))
                {
                    this.AddExcretions(ref excretion, this.At(idx).Excretion);
                    if (this.At(idx).Young != null)
                        this.AddExcretions(ref excretion, this.At(idx).Young.Excretion);
                }
            }

            // Convert values in kg to kg/ha
            excretion.OrgFaeces = this.MultiplyDMPool(excretion.OrgFaeces, 1.0 / area);
            excretion.InOrgFaeces = this.MultiplyDMPool(excretion.InOrgFaeces, 1.0 / area);
            excretion.Urine = this.MultiplyDMPool(excretion.Urine, 1.0 / area);
        }

        /// <summary>
        /// Return the reproductive status of the group as a string.  These strings   
        /// are compatible with the ParseRepro routine.                               
        /// </summary>
        /// <param name="idx">Index of the group</param>
        /// <param name="useYoung">For the young</param>
        /// <returns>The reproduction status string</returns>
        public string SexString(int idx, bool useYoung)
        {
            string[,] maleNames = { { "wether", "ram" }, { "steer", "bull" } };   // [AnimalType,Castrated..Male] of String =

            string result;
            AnimalGroup theGroup;

            if (useYoung)
                theGroup = this.At(idx).Young;
            else
                theGroup = this.At(idx);
            if (theGroup == null)
                result = string.Empty;
            else
            {
                if ((theGroup.ReproState == GrazType.ReproType.Male) || (theGroup.ReproState == GrazType.ReproType.Castrated))
                    result = maleNames[(int)theGroup.Animal, (int)theGroup.ReproState];
                else if (theGroup.Animal == GrazType.AnimalType.Sheep)
                    result = "ewe";
                else if (theGroup.AgeDays < 2 * 365)
                    result = "heifer";
                else
                    result = "cow";
            }
            return result;
        }

        /// <summary>
        /// GrowthCurve calculates MaxNormalWt (see below) for an animal with the   
        /// default birth weight.                                                   
        /// </summary>
        /// <param name="srw">Standard reference weight</param>
        /// <param name="bw">Birth weight</param>
        /// <param name="ageDays">Age in days</param>
        /// <param name="parameters">Breed parameter set</param>
        /// <returns>The maximum normal weight kg</returns>
        private double MaxNormWtFunc(double srw, double bw, int ageDays, AnimalParamSet parameters)
        {
            double growthRate;

            growthRate = parameters.GrowthC[1] / Math.Pow(srw, parameters.GrowthC[2]);
            return srw - (srw - bw) * Math.Exp(-growthRate * ageDays);
        }

        /// <summary>
        /// Calculate the growth from the standard growth curve
        /// </summary>
        /// <param name="ageDays">Age in days</param>
        /// <param name="reprodStatus">Reproductive status</param>
        /// <param name="parameters">Animal parameter set</param>
        /// <returns>The normal weight kg</returns>
        public double GrowthCurve(int ageDays, GrazType.ReproType reprodStatus, AnimalParamSet parameters)
        {
            double stdRefWt;

            stdRefWt = parameters.BreedSRW;
            if ((reprodStatus == GrazType.ReproType.Male) || (reprodStatus == GrazType.ReproType.Castrated))
                stdRefWt = stdRefWt * parameters.SRWScalars[(int)reprodStatus];
            return this.MaxNormWtFunc(stdRefWt, parameters.StdBirthWt(1), ageDays, parameters);
        }

        /// <summary>
        /// Get the reproduction rate
        /// </summary>
        /// <param name="cohortsInfo">The animal cohorts</param>
        /// <param name="mainGenotype">The genotype parameters</param>
        /// <param name="ageInfo">The age information</param>
        /// <param name="latitude">Latitiude value</param>
        /// <param name="mateDOY">Mating day of year</param>
        /// <param name="condition">Animal condition</param>
        /// <param name="chill">Chill index</param>
        /// <returns>The reproduction rate</returns>
        private double GetReproRate(CohortsInfo cohortsInfo, AnimalParamSet mainGenotype, AgeInfo[] ageInfo, double latitude, int mateDOY, double condition, double chill)
        {
            double result = 0.0;
            double[] pregRate = new double[4];
            int cohortIdx;
            int n;

            for (cohortIdx = cohortsInfo.MinYears; cohortIdx <= cohortsInfo.MaxYears; cohortIdx++)
            {
                pregRate = this.GetOffspringRates(mainGenotype, latitude, mateDOY, ageInfo[cohortIdx].AgeAtMating, ageInfo[cohortIdx].SizeAtMating, condition, chill);
                for (n = 1; n <= 3; n++)
                    result = result + (ageInfo[cohortIdx].Propn * pregRate[n]);
            }

            return result;
        }

        /// <summary>
        /// The age information
        /// </summary>
        internal class AgeInfo
        {
            /// <summary>
            /// Proportion
            /// </summary>
            public double Propn;

            /// <summary>
            /// Proportion pregnant
            /// </summary>
            public double[] PropnPreg = new double[4];

            /// <summary>
            /// Proportion lactating
            /// </summary>
            public double[] PropnLact = new double[4];

            /// <summary>
            /// The animal numbers preg and lactating
            /// </summary>
            public int[,] Numbers = new int[4, 4];

            /// <summary>
            /// Gets or sets the age of animal
            /// </summary>
            public int AgeDays { get; set; }

            /// <summary>
            /// Gets or sets the normal base weight
            /// </summary>
            public double NormalBaseWt { get; set; }

            /// <summary>
            /// Gets or sets the animals base weight
            /// </summary>
            public double BaseWeight { get; set; }

            /// <summary>
            /// Gets or sets the fleece weight in kg
            /// </summary>
            public double FleeceWt { get; set; }

            /// <summary>
            /// Gets or sets the age at mating in days
            /// </summary>
            public int AgeAtMating { get; set; }

            /// <summary>
            /// Gets or sets the size at mating in kg
            /// </summary>
            public double SizeAtMating { get; set; }
        }

        // Management events .......................................................

        /// <summary>
        /// Add animal cohorts
        /// </summary>
        /// <param name="cohortsInfo">The animal cohort</param>
        /// <param name="dayOfYear">Day of the year</param>
        /// <param name="latitude">The latitude</param>
        /// <param name="newGroups">List of new animal groups</param>
        public void AddCohorts(CohortsInfo cohortsInfo, int dayOfYear, double latitude, List<int> newGroups)
        {
            AnimalParamSet mainGenotype;
            AgeInfo[] ageInfoList;

            AnimalInits animalInits;
            int numCohorts;
            double survival;
            int daysSinceShearing;
            double meanNormalWt;
            double meanFleeceWt;
            double baseWtScalar;
            double fleeceWtScalar;
            int totalAnimals;
            int mateDOY;
            double lowCondition;
            double lowFoetuses;
            double highCondition;
            double highFoetuses;
            double condition;
            double trialFoetuses;
            double[] pregRate = new double[4];     // TConceptionArray;
            int[] shiftNumber = new int[4];
            bool lactationDone;
            double lowChill;
            double lowOffspring;
            double highChill;
            double highOffspring;
            double chillIndex;
            double trialOffspring;
            double[] lactRate = new double[4];
            int cohortIdx;
            int preg;
            int lact;
            int groupIndex;

            if (cohortsInfo.Number > 0)
            {
                mainGenotype = this.GetGenotype(cohortsInfo.Genotype);

                ageInfoList = new AgeInfo[cohortsInfo.MaxYears + 1];
                for (int i = 0; i < cohortsInfo.MaxYears + 1; i++)
                    ageInfoList[i] = new AgeInfo();
                numCohorts = cohortsInfo.MaxYears - cohortsInfo.MinYears + 1;
                survival = 1.0 - mainGenotype.AnnualDeaths(false);

                if (mainGenotype.Animal == GrazType.AnimalType.Cattle)
                    daysSinceShearing = 0;
                else if (this.IsGiven(cohortsInfo.MeanGFW) && (cohortsInfo.FleeceDays == 0))
                    daysSinceShearing = Convert.ToInt32(Math.Truncate(365.25 * cohortsInfo.MeanGFW / mainGenotype.PotentialGFW), CultureInfo.InvariantCulture);
                else
                    daysSinceShearing = cohortsInfo.FleeceDays;

                for (cohortIdx = cohortsInfo.MinYears; cohortIdx <= cohortsInfo.MaxYears; cohortIdx++)
                {
                    // Proportion of all stock in this age cohort
                    if (survival >= 1.0)
                        ageInfoList[cohortIdx].Propn = 1.0 / numCohorts;
                    else
                        ageInfoList[cohortIdx].Propn = (1.0 - survival) * Math.Pow(survival, cohortIdx - cohortsInfo.MinYears)
                                                        / (1.0 - Math.Pow(survival, numCohorts));
                    ageInfoList[cohortIdx].AgeDays = Convert.ToInt32(Math.Truncate(365.25 * cohortIdx) + cohortsInfo.AgeOffsetDays, CultureInfo.InvariantCulture);

                    // Normal weight for age
                    ageInfoList[cohortIdx].NormalBaseWt = this.GrowthCurve(ageInfoList[cohortIdx].AgeDays, cohortsInfo.ReproClass, mainGenotype);

                    // Estimate a default fleece weight based on time since shearing
                    ageInfoList[cohortIdx].FleeceWt = AnimalParamSet.fDefaultFleece(
                                                                                    mainGenotype,
                                                                                    ageInfoList[cohortIdx].AgeDays,
                                                                                    cohortsInfo.ReproClass,
                                                                                    daysSinceShearing);
                }

                // Re-scale the fleece-free and fleece weights
                meanNormalWt = 0.0;
                meanFleeceWt = 0.0;
                for (cohortIdx = cohortsInfo.MinYears; cohortIdx <= cohortsInfo.MaxYears; cohortIdx++)
                {
                    meanNormalWt = meanNormalWt + (ageInfoList[cohortIdx].Propn * ageInfoList[cohortIdx].NormalBaseWt);
                    meanFleeceWt = meanFleeceWt + (ageInfoList[cohortIdx].Propn * ageInfoList[cohortIdx].FleeceWt);
                }

                if ((cohortsInfo.MeanGFW > 0.0) && (meanFleeceWt > 0.0))
                    fleeceWtScalar = cohortsInfo.MeanGFW / meanFleeceWt;
                else
                    fleeceWtScalar = 1.0;

                if (!this.IsGiven(cohortsInfo.MeanGFW))
                    cohortsInfo.MeanGFW = meanFleeceWt;
                if (this.IsGiven(cohortsInfo.MeanLiveWt))
                    baseWtScalar = (cohortsInfo.MeanLiveWt - cohortsInfo.MeanGFW) / meanNormalWt;
                else if (this.IsGiven(cohortsInfo.CondScore))
                    baseWtScalar = AnimalParamSet.CondScore2Condition(cohortsInfo.CondScore);
                else
                    baseWtScalar = 1.0;

                for (cohortIdx = cohortsInfo.MinYears; cohortIdx <= cohortsInfo.MaxYears; cohortIdx++)
                {
                    ageInfoList[cohortIdx].BaseWeight = ageInfoList[cohortIdx].NormalBaseWt * baseWtScalar;
                    ageInfoList[cohortIdx].FleeceWt = ageInfoList[cohortIdx].FleeceWt * fleeceWtScalar;
                }

                // Numbers in each age cohort
                totalAnimals = 0;
                for (cohortIdx = cohortsInfo.MinYears; cohortIdx <= cohortsInfo.MaxYears; cohortIdx++)
                {
                    ageInfoList[cohortIdx].Numbers[0, 0] = Convert.ToInt32(Math.Truncate(ageInfoList[cohortIdx].Propn * cohortsInfo.Number), CultureInfo.InvariantCulture);
                    totalAnimals += ageInfoList[cohortIdx].Numbers[0, 0];
                }
                for (cohortIdx = cohortsInfo.MinYears; cohortIdx <= cohortsInfo.MaxYears; cohortIdx++)
                    if (totalAnimals < cohortsInfo.Number)
                    {
                        ageInfoList[cohortIdx].Numbers[0, 0]++;
                        totalAnimals++;
                    }

                // Pregnancy and lactation
                if ((cohortsInfo.ReproClass == GrazType.ReproType.Empty) || (cohortsInfo.ReproClass == GrazType.ReproType.EarlyPreg) || (cohortsInfo.ReproClass == GrazType.ReproType.LatePreg))
                {
                    // Numbers with each number of foetuses
                    if ((cohortsInfo.DaysPreg > 0) && (cohortsInfo.Foetuses > 0.0))
                    {
                        mateDOY = 1 + ((dayOfYear - cohortsInfo.DaysPreg + 364) % 365);
                        for (cohortIdx = cohortsInfo.MinYears; cohortIdx <= cohortsInfo.MaxYears; cohortIdx++)
                        {
                            ageInfoList[cohortIdx].AgeAtMating = ageInfoList[cohortIdx].AgeDays - cohortsInfo.DaysPreg;
                            ageInfoList[cohortIdx].SizeAtMating = this.GrowthCurve(
                                                                                    ageInfoList[cohortIdx].AgeAtMating,
                                                                                    cohortsInfo.ReproClass,
                                                                                    mainGenotype) / mainGenotype.fSexStdRefWt(cohortsInfo.ReproClass);
                        }

                        // binary search for the body condition at mating that yields the desired pregnancy rate
                        lowCondition = 0.60;
                        lowFoetuses = this.GetReproRate(cohortsInfo, mainGenotype, ageInfoList, latitude, mateDOY, lowCondition, 0);
                        highCondition = 1.40;
                        highFoetuses = this.GetReproRate(cohortsInfo, mainGenotype, ageInfoList, latitude, mateDOY, highCondition, 0);

                        if (lowFoetuses > cohortsInfo.Foetuses)
                            condition = lowCondition;
                        else if (highFoetuses < cohortsInfo.Foetuses)
                            condition = highCondition;
                        else
                        {
                            do
                            {
                                condition = 0.5 * (lowCondition + highCondition);
                                trialFoetuses = this.GetReproRate(cohortsInfo, mainGenotype, ageInfoList, latitude, mateDOY, condition, 0);

                                if (trialFoetuses < cohortsInfo.Foetuses)
                                    lowCondition = condition;
                                else
                                    highCondition = condition;
                            }
                            while (Math.Abs(trialFoetuses - cohortsInfo.Foetuses) >= 1.0E-5); // until (Abs(fTrialFoetuses-CohortsInfo.fFoetuses) < 1.0E-5);
                        }

                        // Compute final pregnancy rates and numbers
                        for (cohortIdx = cohortsInfo.MinYears; cohortIdx <= cohortsInfo.MaxYears; cohortIdx++)
                        {
                            pregRate = this.GetOffspringRates(
                                                        mainGenotype, 
                                                        latitude, 
                                                        mateDOY,
                                                        ageInfoList[cohortIdx].AgeAtMating,
                                                        ageInfoList[cohortIdx].SizeAtMating,
                                                        condition);
                            for (preg = 1; preg <= 3; preg++)
                                shiftNumber[preg] = Convert.ToInt32(Math.Round(pregRate[preg] * ageInfoList[cohortIdx].Numbers[0, 0]), CultureInfo.InvariantCulture);
                            for (preg = 1; preg <= 3; preg++)
                            {
                                ageInfoList[cohortIdx].Numbers[preg, 0] += shiftNumber[preg];
                                ageInfoList[cohortIdx].Numbers[0, 0] -= shiftNumber[preg];
                            }
                        }
                    } // if (iDaysPreg > 0) and (fFoetuses > 0.0)  

                    // Numbers with each number of suckling young
                    // Different logic for sheep and cattle:
                    // - for sheep, first assume average body condition at conception and vary
                    // the chill index. If that doesn't work, fix the chill index & vary the
                    // body condition
                    // - for cattle, fix the chill index & vary the body condition
                    if ((cohortsInfo.DaysLact > 0) && (cohortsInfo.Offspring > 0.0))
                    {
                        lactationDone = false;
                        condition = 1.0;
                        chillIndex = 0;
                        mateDOY = 1 + ((dayOfYear - cohortsInfo.DaysLact - mainGenotype.Gestation + 729) % 365);
                        for (cohortIdx = cohortsInfo.MinYears; cohortIdx <= cohortsInfo.MaxYears; cohortIdx++)
                        {
                            ageInfoList[cohortIdx].AgeAtMating = ageInfoList[cohortIdx].AgeDays - cohortsInfo.DaysLact - mainGenotype.Gestation;
                            ageInfoList[cohortIdx].SizeAtMating = this.GrowthCurve(
                                                                                ageInfoList[cohortIdx].AgeAtMating,
                                                                                cohortsInfo.ReproClass,
                                                                                mainGenotype) / mainGenotype.fSexStdRefWt(cohortsInfo.ReproClass);
                        }

                        if (mainGenotype.Animal == GrazType.AnimalType.Sheep)
                        {
                            // binary search for the chill index at birth that yields the desired proportion of lambs
                            lowChill = 500.0;
                            lowOffspring = this.GetReproRate(cohortsInfo, mainGenotype, ageInfoList, latitude, mateDOY, 1.0, lowChill);
                            highChill = 2500.0;
                            highOffspring = this.GetReproRate(cohortsInfo, mainGenotype, ageInfoList, latitude, mateDOY, 1.0, highChill);

                            // this is a monotonically decreasing function...
                            if ((highOffspring < cohortsInfo.Offspring) && (lowOffspring > cohortsInfo.Offspring))
                            {
                                do
                                {
                                    chillIndex = 0.5 * (lowChill + highChill);
                                    trialOffspring = this.GetReproRate(cohortsInfo, mainGenotype, ageInfoList, latitude, mateDOY, 1.0, chillIndex);

                                    if (trialOffspring > cohortsInfo.Offspring)
                                        lowChill = chillIndex;
                                    else
                                        highChill = chillIndex;
                                }
                                while (Math.Abs(trialOffspring - cohortsInfo.Offspring) >= 1.0E-5); // until (Abs(fTrialOffspring-CohortsInfo.fOffspring) < 1.0E-5);

                                lactationDone = true;
                            }
                        } // fitting lactation rate to a chill index

                        if (!lactationDone)
                        {
                            chillIndex = 800.0;

                            // binary search for the body condition at mating that yields the desired proportion of lambs or calves
                            lowCondition = 0.60;
                            lowOffspring = this.GetReproRate(cohortsInfo, mainGenotype, ageInfoList, latitude, mateDOY, lowCondition, chillIndex);
                            highCondition = 1.40;
                            highOffspring = this.GetReproRate(cohortsInfo, mainGenotype, ageInfoList, latitude, mateDOY, highCondition, chillIndex);

                            if (lowOffspring > cohortsInfo.Offspring)
                                condition = lowCondition;
                            else if (highOffspring < cohortsInfo.Offspring)
                                condition = highCondition;
                            else
                            {
                                do
                                {
                                    condition = 0.5 * (lowCondition + highCondition);
                                    trialOffspring = this.GetReproRate(cohortsInfo, mainGenotype, ageInfoList, latitude, mateDOY, condition, chillIndex);

                                    if (trialOffspring < cohortsInfo.Offspring)
                                        lowCondition = condition;
                                    else
                                        highCondition = condition;
                                }
                                while (Math.Abs(trialOffspring - cohortsInfo.Offspring) >= 1.0E-5); // until (Abs(fTrialOffspring-CohortsInfo.fOffspring) < 1.0E-5);
                            }
                        } // fitting lactation rate to a condition 

                        // Compute final offspring rates and numbers
                        for (cohortIdx = cohortsInfo.MinYears; cohortIdx <= cohortsInfo.MaxYears; cohortIdx++)
                        {
                            lactRate = this.GetOffspringRates(
                                                            mainGenotype, 
                                                            latitude, 
                                                            mateDOY,
                                                            ageInfoList[cohortIdx].AgeAtMating,
                                                            ageInfoList[cohortIdx].SizeAtMating,
                                                            condition, 
                                                            chillIndex);
                            for (preg = 0; preg <= 3; preg++)
                            {
                                for (lact = 1; lact <= 3; lact++)
                                    shiftNumber[lact] = Convert.ToInt32(Math.Round(lactRate[lact] * ageInfoList[cohortIdx].Numbers[preg, 0]), CultureInfo.InvariantCulture);
                                for (lact = 1; lact <= 3; lact++)
                                {
                                    ageInfoList[cohortIdx].Numbers[preg, lact] += shiftNumber[lact];
                                    ageInfoList[cohortIdx].Numbers[preg, 0] -= shiftNumber[lact];
                                }
                            }
                        }
                    } // _ lactating animals _
                } // _ female animals 

                // Construct the animal groups from the numbers and cohort-specific information
                animalInits.Genotype = cohortsInfo.Genotype;
                animalInits.MatedTo = cohortsInfo.MatedTo;
                animalInits.Sex = cohortsInfo.ReproClass;
                animalInits.BirthCS = StdMath.DMISSING;
                animalInits.Paddock = string.Empty;
                animalInits.Tag = 0;
                animalInits.Priority = 0;

                for (cohortIdx = cohortsInfo.MinYears; cohortIdx <= cohortsInfo.MaxYears; cohortIdx++)
                {
                    for (preg = 0; preg <= 3; preg++)
                    {
                        for (lact = 0; lact <= 3; lact++)
                        {
                            if (ageInfoList[cohortIdx].Numbers[preg, lact] > 0)
                            {
                                animalInits.Number = ageInfoList[cohortIdx].Numbers[preg, lact];
                                animalInits.AgeDays = ageInfoList[cohortIdx].AgeDays;
                                animalInits.Weight = ageInfoList[cohortIdx].BaseWeight + ageInfoList[cohortIdx].FleeceWt;
                                animalInits.MaxPrevWt = StdMath.DMISSING; // compute from cond_score
                                animalInits.FleeceWt = ageInfoList[cohortIdx].FleeceWt;
                                animalInits.FibreDiam = AnimalParamSet.fDefaultMicron(
                                                                                     mainGenotype,
                                                                                     animalInits.AgeDays,
                                                                                     animalInits.Sex,
                                                                                     daysSinceShearing,
                                                                                     animalInits.FleeceWt);
                                if (preg > 0)
                                {
                                    animalInits.Pregnant = cohortsInfo.DaysPreg;
                                    animalInits.NumFoetuses = preg;
                                }
                                else
                                {
                                    animalInits.Pregnant = 0;
                                    animalInits.NumFoetuses = 0;
                                }

                                if ((lact > 0)
                                   || ((mainGenotype.Animal == GrazType.AnimalType.Cattle) && (cohortsInfo.DaysLact > 0) && (cohortsInfo.Offspring == 0.0)))
                                {
                                    animalInits.Lactating = cohortsInfo.DaysLact;
                                    animalInits.NumSuckling = lact;
                                    animalInits.YoungGFW = cohortsInfo.LambGFW;
                                    animalInits.YoungWt = cohortsInfo.OffspringWt;
                                }
                                else
                                {
                                    animalInits.Lactating = 0;
                                    animalInits.NumSuckling = 0;
                                    animalInits.YoungGFW = 0.0;
                                    animalInits.YoungWt = 0.0;
                                }

                                groupIndex = this.Add(animalInits);
                                if (newGroups != null)
                                {
                                    newGroups.Add(groupIndex);
                                }
                            }
                        }
                    }
                }
            } // if CohortsInfo.iNumber > 0 
        }

        /// <summary>
        /// Executes a "buy" event
        /// </summary>
        /// <param name="animalInfo">The animal details</param>
        /// <returns>The index of the new group</returns>
        protected int Buy(PurchaseInfo animalInfo)
        {
            AnimalParamSet agenotype;
            AnimalGroup newGroup;
            double bodyCondition;
            double liveWeight;
            double lowBaseWeight = 0.0;
            double highBaseWeight = 0.0;
            AnimalList weanList;
            int paddNo;

            int result = 0;

            if (animalInfo.Number > 0)
            {
                agenotype = this.GetGenotype(animalInfo.Genotype);

                if (animalInfo.LiveWt > 0.0)
                    liveWeight = animalInfo.LiveWt;
                else
                {
                    liveWeight = this.GrowthCurve(animalInfo.AgeDays, animalInfo.Repro, agenotype);
                    if (animalInfo.CondScore > 0.0)
                        liveWeight = liveWeight * AnimalParamSet.CondScore2Condition(animalInfo.CondScore);
                    if (agenotype.Animal == GrazType.AnimalType.Sheep)
                        liveWeight = liveWeight + animalInfo.GFW;
                }

                // Construct a new group of animals.     
                newGroup = new AnimalGroup(
                                            agenotype,
                                            animalInfo.Repro,                         // Repro should be Empty, Castrated or 
                                            animalInfo.Number,                        // Male; pregnancy is handled with the 
                                            animalInfo.AgeDays,                       // Preg  field.                        
                                            liveWeight,
                                            animalInfo.GFW,
                                            parentStockModel.randFactory);

                // Adjust the condition score if it has been given
                if ((animalInfo.CondScore > 0.0) && (animalInfo.LiveWt > 0.0))        
                {                                                                                                
                    bodyCondition = AnimalParamSet.CondScore2Condition(animalInfo.CondScore);
                    newGroup.WeightRangeForCond(
                                                animalInfo.Repro, 
                                                animalInfo.AgeDays,
                                                bodyCondition, 
                                                newGroup.Genotype,
                                                ref lowBaseWeight, 
                                                ref highBaseWeight);

                    if ((newGroup.BaseWeight >= lowBaseWeight) && (newGroup.BaseWeight <= highBaseWeight))
                        newGroup.setConditionAtWeight(bodyCondition);
                    else
                    {
                        newGroup = null;
                        throw new Exception("Purchased animals with condition score "
                                                + animalInfo.CondScore.ToString() + "\n"
                                                + " must have a base weight in the range "
                                                + lowBaseWeight.ToString()
                                                + "-" + highBaseWeight.ToString() + " kg");
                    }
                }

                if (newGroup.ReproState == GrazType.ReproType.Empty)
                {
                    // Use TAnimalGroup's property interface to set up pregnancy and lactation.  
                    if (animalInfo.MatedTo != string.Empty)                                      
                        newGroup.MatedTo = this.GetGenotype(animalInfo.MatedTo);            
                    newGroup.Pregnancy = animalInfo.Preg;
                    newGroup.Lactation = animalInfo.Lact;

                    // NYoung denotes the number of *suckling* young in lactating cows, which isn't quite the same as the YoungNo property                    
                    if ((newGroup.Animal == GrazType.AnimalType.Cattle)
                       && (animalInfo.Lact > 0) && (animalInfo.NYoung == 0))            
                    {                                                                   
                        weanList = null;                                                
                        newGroup.Wean(true, true, ref weanList, ref weanList);          
                        weanList = null; 
                    }
                    else if (animalInfo.NYoung > 0)
                    {
                        // if the animals are pregnant then they need feotuses
                        if (newGroup.Pregnancy > 0)
                        {
                            if ((animalInfo.Lact > 0) && (newGroup.Animal == GrazType.AnimalType.Cattle))
                            {
                                newGroup.NoOffspring = 1;
                                newGroup.NoFoetuses = Math.Min(2, Math.Max(0, animalInfo.NYoung - 1));
                            }
                            else
                            {
                                newGroup.NoFoetuses = Math.Min(3, animalInfo.NYoung);                 // recalculates livewt
                            }
                        }
                        else
                            newGroup.NoOffspring = animalInfo.NYoung;
                    }

                    // Lamb/calf weights and lamb fleece weights are optional.               
                    if (newGroup.Young != null)                                              
                    {                                                                   
                        if (this.IsGiven(animalInfo.YoungWt))
                            newGroup.Young.LiveWeight = animalInfo.YoungWt;
                        if (this.IsGiven(animalInfo.YoungGFW))
                            newGroup.Young.FleeceCutWeight = animalInfo.YoungGFW;
                    }
                } // if (ReproState = Empty) 

                paddNo = 0;                                                                          // Newly bought animals have tag # zero and go in the first named paddock.  
                while ((paddNo < this.Paddocks.Count()) && (this.Paddocks.ByIndex(paddNo).Name == string.Empty))   
                    paddNo++;
                if (paddNo >= this.Paddocks.Count())
                    paddNo = 0;
                result = this.Add(newGroup, this.Paddocks.ByIndex(paddNo), 0, 0);
            } // if AnimalInfo.Number > 0 
            return result;
        }

        /// <summary>
        /// If groupIdx=0, work through all groups, removing animals until Number        
        /// animals (not including unweaned lambs/calves) have been removed.  If         
        /// GroupIdx>0, then remove the lesser of Number animals and all animals in      
        /// the group                                                                    
        /// </summary>
        /// <param name="groupIdx">The animal group index</param>
        /// <param name="number">The number to sell</param>
        public void Sell(int groupIdx, int number)
        {
            int numToSell;
            int idx;

            idx = 1;

            // A negative number is construed as zero
            while ((idx <= this.Count()) && (number > 0))                                   
            {
                // Does this call apply to group I?      
                if (((groupIdx == 0) || (groupIdx == idx)) && (this.At(idx) != null))       
                {
                    numToSell = Math.Min(number, this.At(idx).NoAnimals);
                    this.At(idx).NoAnimals = this.At(idx).NoAnimals - numToSell;
                    if (groupIdx == 0)
                        number = number - numToSell;
                    else
                        number = 0;
                }
                idx++;
            }
        }

        /// <summary>
        /// Sell the animals that have this tag. Sells firstly from the group with the
        /// smallest index.
        /// </summary>
        /// <param name="tagNo">The tag number</param>
        /// <param name="number">Number to sell</param>
        public void SellTag(int tagNo, int number)
        {
            int numToSell;
            int idx;
            int remainToSell;

            remainToSell = number;                                                          // count down the numbers for sale in a group
            idx = 1;

            // A negative number is construed as zero
            while ((idx <= this.Count()) && (remainToSell > 0))                             
            {
                // Does this call apply to group I? 
                if ((tagNo == this.GetTag(idx)) && (this.At(idx) != null))                            
                {
                    numToSell = Math.Min(remainToSell, this.At(idx).NoAnimals);             // only sell what is possible from this group
                    this.At(idx).NoAnimals = this.At(idx).NoAnimals - numToSell;
                    remainToSell = remainToSell - numToSell;
                }
                idx++;
            }
        }

        /// <summary>
        /// If groupIdx=0, shear all groups; otherwise shear the nominated group.        
        /// Unweaned lambs are not shorn.                                                
        /// </summary>
        /// <param name="groupIdx">The animal group index</param>
        /// <param name="adults">Do adults</param>
        /// <param name="lambs">Do lambs</param>
        public void Shear(int groupIdx, bool adults, bool lambs)
        {
            double dummy = 0;
            int idx;

            for (idx = 1; idx <= this.Count(); idx++)
            {
                if (((groupIdx == 0) || (groupIdx == idx)) && (this.At(idx) != null))
                {
                    if (adults)
                        this.At(idx).Shear(ref dummy);
                    if (lambs && (this.At(idx).Young != null))
                        this.At(idx).Young.Shear(ref dummy);
                }
            }
        }

        /// <summary>
        /// If groupIdx=0, commence joining of all groups; otherwise commence joining    
        /// of the nominated group.                                                      
        /// </summary>
        /// <param name="groupIdx">The animal group index</param>
        /// <param name="mateTo">Mate to these animals</param>
        /// <param name="mateDays">Mating period</param>
        public void Join(int groupIdx, string mateTo, int mateDays)
        {
            int idx;

            for (idx = 1; idx <= this.Count(); idx++)
                if (((groupIdx == 0) || (groupIdx == idx)) && (this.At(idx) != null))
                    this.At(idx).Join(this.GetGenotype(mateTo), mateDays);
        }

        /// <summary>
        /// The castration routine is complicated somewhat by the fact that the          
        /// parameter refers to the number of male lambs or calves to castrate.          
        /// When this number is less than the number of male lambs or calves in a        
        /// group, the excess must be split off.                                         
        /// </summary>
        /// <param name="groupIdx">The animal grou index</param>
        /// <param name="number">Number of animals</param>
        public void Castrate(int groupIdx, int number)
        {
            int numToCastrate;
            int idx, n;

            n = this.Count();                                                                   // Store the initial list size so that groups which are split off aren't processed twice
            for (idx = 1; idx <= n; idx++)                                                                                         
            {
                if (((groupIdx == 0) || (groupIdx == idx)) && (this.At(idx) != null))               
                {
                    if ((this.At(idx).Young != null) && (this.At(idx).Young.MaleNo > 0) && (number > 0))
                    {
                        numToCastrate = Math.Min(number, this.At(idx).Young.MaleNo);
                        if (numToCastrate < this.At(idx).Young.MaleNo)
                            this.Split(idx, Convert.ToInt32(Math.Round((double)number / numToCastrate * this.At(idx).NoAnimals), CultureInfo.InvariantCulture));  // TODO: check this conversion
                        this.At(idx).Young.Castrate();
                        number = number - numToCastrate;
                    }
                }
            }
        }

        /// <summary>
        /// See the notes to the Castrate method; but weaning is even further         
        /// complicated because males and/or females may be weaned.                   
        /// </summary>
        /// <param name="groupIdx">The animal group index</param>
        /// <param name="number">The number of animals</param>
        /// <param name="weanFemales">Wean the females</param>
        /// <param name="weanMales">Wean the males</param>
        public void Wean(int groupIdx, int number, bool weanFemales, bool weanMales)
        {
            int numToWean;
            int mothersToWean;
            AnimalList newGroups;
            int idx, n;

            number = Math.Max(number, 0);

            // Only iterate through groups present at the start of the routine            
            n = this.Count();                                                               
            for (idx = 1; idx <= n; idx++)                                                  
            {
                // Group Idx, or all groups if 0         
                if (((groupIdx == 0) || (groupIdx == idx)) && (this.At(idx) != null))       
                {
                    if (this.At(idx).Young != null)
                    {
                        // Establish the number of lambs/calves to wean from this group of mothers  
                        if (weanMales && weanFemales)                                       
                            numToWean = Math.Min(number, this.At(idx).Young.NoAnimals);     
                        else if (weanMales)
                            numToWean = Math.Min(number, this.At(idx).Young.MaleNo);
                        else if (weanFemales)
                            numToWean = Math.Min(number, this.At(idx).Young.FemaleNo);
                        else
                            numToWean = 0;

                        if (numToWean > 0)
                        {
                            if (numToWean == number)
                            {
                                // If there are more lambs/calves present than are to be weaned, split the excess off                       
                                if (weanMales && weanFemales)                                                               
                                    mothersToWean = Convert.ToInt32(Math.Round((double)numToWean / this.At(idx).NoOffspring), CultureInfo.InvariantCulture);
                                else                                                                                        
                                    mothersToWean = Convert.ToInt32(Math.Round(numToWean / (this.At(idx).NoOffspring / 2.0)), CultureInfo.InvariantCulture);
                                if (mothersToWean < this.At(idx).NoAnimals)
                                    this.Split(idx, mothersToWean);
                            }
                            newGroups = null;                                                           // Carry out the weaning process. N.B.   
                            this.At(idx).Wean(weanFemales, weanMales, ref newGroups, ref newGroups);    // the weaners appear in the same      
                            this.Add(newGroups, this.GetPaddInfo(idx), this.GetTag(idx), this.GetPriority(idx));  // paddock as their mothers and with   
                            newGroups = null;                                                           // the same tag and priority value     
                        }

                        number = number - numToWean;
                    } // _ if (Young <> NIL) 
                }
            }
        }

        /// <summary>
        /// If groupIdx=0, end lactation of all groups; otherwise end lactation of    
        /// of the nominated group.                                                   
        /// </summary>
        /// <param name="groupIdx">Group index</param>
        /// <param name="number">Number of animals</param>
        public void DryOff(int groupIdx, int number)
        {
            int numToDryOff;
            int idx, n;

            number = Math.Max(number, 0);

            // Only iterate through groups present at the start of the routine   
            n = this.Count();                                                     
            for (idx = 1; idx <= n; idx++)                                                          
            {
                // Group I, or all groups if I=0
                if (((groupIdx == 0) || (groupIdx == idx)) && (this.At(idx) != null) && (this.At(idx).Lactation > 0))
                {
                    numToDryOff = Math.Min(number, this.At(idx).FemaleNo);
                    if (numToDryOff > 0)
                    {
                        if (numToDryOff < this.At(idx).FemaleNo)
                            this.Split(idx, numToDryOff);
                        this.At(idx).DryOff();
                    }
                    number = number - numToDryOff;
                }
            }
        }

        /// <summary>
        /// Break an animal group up in various ways; by number, by age, by weight    
        /// or by sex of lambs/calves.  The new group(s) have the same priority and   
        /// paddock as the original.  SplitWeight assumes a distribution of weights   
        /// around the group average.                                                 
        /// </summary>
        /// <param name="groupIdx">The animal group index</param>
        /// <param name="numToKeep">Number to keep</param>
        public void Split(int groupIdx, int numToKeep)
        {
            AnimalGroup srcGroup;
            int numToSplit;

            srcGroup = this.GetAt(groupIdx);
            if (srcGroup != null)
            {
                numToSplit = Math.Max(0, srcGroup.NoAnimals - Math.Max(numToKeep, 0));
                if (numToSplit > 0)
                    this.Add(srcGroup.Split(numToSplit, false, srcGroup.NODIFF, srcGroup.NODIFF), this.GetPaddInfo(groupIdx), this.GetTag(groupIdx), this.GetPriority(groupIdx));
            }
        }

        /// <summary>
        /// Split the group by age
        /// </summary>
        /// <param name="groupIdx">The animal group index</param>
        /// <param name="ageDays">Age in days</param>
        public void SplitAge(int groupIdx, int ageDays)
        {
            AnimalGroup srcGroup;
            int numMales = 0;
            int numFemales = 0;

            srcGroup = this.GetAt(groupIdx);
            if (srcGroup != null)
            {
                srcGroup.GetOlder(ageDays, ref numMales, ref numFemales);
                if (numMales + numFemales > 0)
                    this.Add(srcGroup.Split(numMales + numFemales, true, srcGroup.NODIFF, srcGroup.NODIFF), this.GetPaddInfo(groupIdx), this.GetTag(groupIdx), this.GetPriority(groupIdx));
            }
        }

        /// <summary>
        /// Split the group by weight
        /// </summary>
        /// <param name="groupIdx">The animal group index</param>
        /// <param name="splitWt">The weight</param>
        public void SplitWeight(int groupIdx, double splitWt)
        {
            double varRatio = 0.10;                                                 // Coefficient of variation of LW (0-1)       
            int NOSTEPS = 20;

            AnimalGroup srcGroup;
            double splitSD;                                                         // Position of the threshold on the live wt   
                                                                                    //   distribution of the group, in S.D. units 
            double removePropn;                                                     // Proportion of animals lighter than SplitWt 
            int numToRemove;                                                        // Number to transfer to TempAnimals          
            int numAnimals;
            double liveWt;
            DifferenceRecord diffs;
            double rightSD;
            double stepWidth;
            double prevCum;
            double currCum;
            double removeLW;
            double diffRatio;
            int idx;

            srcGroup = this.GetAt(groupIdx);
            if (srcGroup != null)
            {
                numAnimals = srcGroup.NoAnimals;
                liveWt = srcGroup.LiveWeight;
                splitSD = (splitWt - liveWt) / (varRatio * liveWt);                 // NB SplitWt is a live weight value     

                removePropn = StdMath.CumNormal(splitSD);                           // Normal distribution of weights assumed
                numToRemove = Convert.ToInt32(Math.Round(numAnimals * removePropn), CultureInfo.InvariantCulture);

                if (numToRemove > 0)
                {
                    diffs = new DifferenceRecord() { StdRefWt = srcGroup.NODIFF.StdRefWt, BaseWeight = srcGroup.NODIFF.BaseWeight, FleeceWt = srcGroup.NODIFF.FleeceWt };            
                    if (numToRemove < numAnimals)
                    {
                        // This computation gives us the mean live weight of animals which are    
                        // lighter than the weight threshold. We are integrating over a truncated 
                        // normal distribution, using the differences between successive      
                        // evaluations of the CumNormal function                            
                        rightSD = -5.0;                                             
                        stepWidth = (splitSD - rightSD) / NOSTEPS;                  
                        removeLW = 0.0;                                             
                        prevCum = 0.0;                                              
                        for (idx = 1; idx <= NOSTEPS; idx++)                        
                        {                                                           
                            rightSD = rightSD + stepWidth;                          
                            currCum = StdMath.CumNormal(rightSD);
                            removeLW = removeLW + ((currCum - prevCum) * liveWt * (1.0 + varRatio * (rightSD - 0.5 * stepWidth)));
                            prevCum = currCum;
                        }
                        removeLW = removeLW / removePropn;

                        diffRatio = numAnimals / (numAnimals - numToRemove) * (removeLW / liveWt - 1.0);
                        diffs.BaseWeight = diffRatio * srcGroup.BaseWeight;
                        diffs.StdRefWt = diffRatio * srcGroup.StdReferenceWt;               // Weight diffs within a group are       
                        diffs.FleeceWt = diffRatio * srcGroup.FleeceCutWeight;              // assumed genetic!                    
                    }                       

                    this.Add(
                         srcGroup.Split(numToRemove, false, diffs, srcGroup.NODIFF),     // Now we have computed Diffs, we split  
                         this.GetPaddInfo(groupIdx), 
                         this.GetTag(groupIdx), 
                         this.GetPriority(groupIdx));   // up the animals                      
                } 
            }
        }

        /// <summary>
        /// Split off the young
        /// </summary>
        /// <param name="groupIdx">The animal group index</param>
        public void SplitYoung(int groupIdx)
        {
            AnimalGroup srcGroup;
            AnimalList newGroups;

            srcGroup = this.GetAt(groupIdx);
            if (srcGroup != null)
            {
                newGroups = null;
                srcGroup.SplitYoung(ref newGroups);
                this.Add(newGroups, this.GetPaddInfo(groupIdx), this.GetTag(groupIdx), this.GetPriority(groupIdx));
                newGroups = null;
            }
        }

        /// <summary>
        /// Sorting is done using the one-offset stock array                            
        /// </summary>
        public void Sort()
        {
            int idx, jdx;

            for (idx = 1; idx <= this.Count() - 1; idx++)
            {
                for (jdx = idx + 1; jdx <= this.Count(); jdx++)
                {
                    if (this.stock[idx].Tag > this.stock[jdx].Tag)
                    {
                        this.stock[0] = this.stock[idx];                                            // stock[0] is temporary storage        
                        this.stock[idx] = this.stock[jdx];
                        this.stock[jdx] = this.stock[0];
                    }
                }
            }
        }

        /// <summary>
        /// Perform a drafting operation
        /// </summary>
        /// <param name="closedList">List of closed paddocks</param>
        public void Draft(List<string> closedList)
        {
            double[] paddockRank;
            bool[] available;
            AnimalGroup tempAnimals;
            int prevPadd;
            int bestPadd;
            double bestRank;
            int prevPriority;
            int bestPriority;
            int paddIdx, idx;

            if ((this.Count() > 0) && (this.Paddocks.Count() > 0))
            {
                paddockRank = new double[this.Paddocks.Count()];
                available = new bool[this.Paddocks.Count()];

                // Only draft into pasture paddocks     
                for (paddIdx = 0; paddIdx <= this.Paddocks.Count() - 1; paddIdx++)                       
                    available[paddIdx] = this.Paddocks.ByIndex(paddIdx).Forages.Count() > 0;

                // Paddocks occupied by groups that are not to be drafted                   
                for (idx = 1; idx <= this.Count(); idx++)                                           
                {
                    if (this.GetPriority(idx) <= 0)                                                      
                    {
                        paddIdx = this.Paddocks.IndexOf(this.GetInPadd(idx));
                        if (paddIdx >= 0)
                            available[paddIdx] = false;
                    }
                }

                // Paddocks closed by the manager        
                for (idx = 0; idx <= closedList.Count() - 1; idx++)                                 
                {
                    paddIdx = this.Paddocks.IndexOf(closedList[idx]);
                    if (paddIdx >= 0)
                        available[paddIdx] = false;
                }

                // Rank order for open, unoccupied paddocks                            
                tempAnimals = this.At(1).Copy();
                for (paddIdx = 0; paddIdx <= this.Paddocks.Count() - 1; paddIdx++)                 
                {
                    if (available[paddIdx])
                        paddockRank[paddIdx] = this.GetPaddockRank(this.Paddocks.ByIndex(paddIdx), tempAnimals);
                    else
                        paddockRank[paddIdx] = 0.0;
                }
                tempAnimals = null;

                prevPadd = 0;                                                                       // Fallback paddock if none available    
                while ((prevPadd < this.Paddocks.Count() - 1) && (this.Paddocks.ByIndex(prevPadd).Name == string.Empty))
                    prevPadd++;

                prevPriority = 0;
                do
                {
                    bestPadd = -1;                                                                  // Locate the best available paddock     
                    bestRank = -1.0;
                    for (paddIdx = 0; paddIdx <= this.Paddocks.Count() - 1; paddIdx++)
                    {
                        if (available[paddIdx] && (paddockRank[paddIdx] > bestRank))
                        {
                            bestPadd = paddIdx;
                            bestRank = paddockRank[paddIdx];
                        }
                    }

                    // No unoccupied paddocks - use the lowest-ranked unoccupied paddock    
                    if (bestPadd == -1)                                                             
                        bestPadd = prevPadd;                                                        

                    bestPriority = int.MaxValue;                                                  // Locate the next-smallest priority score 
                    for (idx = 1; idx <= this.Count(); idx++)
                    {
                        if ((this.GetPriority(idx) < bestPriority) && (this.GetPriority(idx) > prevPriority))
                            bestPriority = this.GetPriority(idx);
                    }

                    // Move animals with that priority score 
                    for (idx = 1; idx <= this.Count(); idx++)                                       
                    {
                        if (this.GetPriority(idx) == bestPriority)
                            this.SetInPadd(idx, this.Paddocks.ByIndex(bestPadd).Name);
                    }
                    available[bestPadd] = false;

                    prevPadd = bestPadd;
                    prevPriority = bestPriority;
                }
                while (bestPriority != int.MaxValue);
            }
        }

        /// <summary>
        /// Perform a drafting operation
        /// </summary>
        /// <param name="tagNo">The tag number</param>
        /// <param name="closedPaddocks">List of closed paddocks</param>
        public void Draft(int tagNo, List<string> closedPaddocks)
        {
            double[] paddockRank;
            bool[] available;
            AnimalGroup tempAnimals;
            int prevPadd;
            int bestPadd;
            double bestRank;
            int prevPriority;
            int bestPriority;
            int paddIdx, idx;

            if ((this.Count() > 0) && (this.Paddocks.Count() > 0))
            {
                paddockRank = new double[this.Paddocks.Count()];
                available = new bool[this.Paddocks.Count()];

                // Only draft into pasture paddocks      
                for (paddIdx = 0; paddIdx <= this.Paddocks.Count() - 1; paddIdx++)                
                    available[paddIdx] = (this.Paddocks.ByIndex(paddIdx).Forages.Count() > 0);

                // Paddocks occupied by groups that are not to be drafted                   
                for (idx = 1; idx <= this.Count(); idx++)                                   
                {
                    if (this.GetPriority(idx) <= 0)                                              
                    {
                        paddIdx = this.Paddocks.IndexOf(this.GetInPadd(idx));
                        if (paddIdx >= 0)
                            available[paddIdx] = false;
                    }
                }

                // Paddocks closed by the manager       
                for (idx = 0; idx <= closedPaddocks.Count() - 1; idx++)                             
                {
                    paddIdx = this.Paddocks.IndexOf(closedPaddocks[idx]);
                    if (paddIdx >= 0)
                        available[paddIdx] = false;
                }

                tempAnimals = this.At(1).Copy();

                // Rank order for open, unoccupied
                for (paddIdx = 0; paddIdx <= this.Paddocks.Count() - 1; paddIdx++)                            
                {
                    if (available[paddIdx])                                                  // paddocks                            
                        paddockRank[paddIdx] = this.GetPaddockRank(this.Paddocks.ByIndex(paddIdx), tempAnimals);
                    else
                        paddockRank[paddIdx] = 0.0;
                }
                tempAnimals = null;

                prevPadd = 0;                                                              // Fallback paddock if none available    
                while ((prevPadd < this.Paddocks.Count() - 1) && (this.Paddocks.ByIndex(prevPadd).Name == string.Empty))
                    prevPadd++;

                prevPriority = 0;
                do
                {
                    bestPadd = -1;                                                         // Locate the best available paddock     
                    bestRank = -1.0;
                    for (paddIdx = 0; paddIdx <= this.Paddocks.Count() - 1; paddIdx++)
                    {
                        if (available[paddIdx] && (paddockRank[paddIdx] > bestRank))
                        {
                            bestPadd = paddIdx;
                            bestRank = paddockRank[paddIdx];
                        }
                    }
                    if (bestPadd == -1)                                                    // No unoccupied paddocks - use the      
                        bestPadd = prevPadd;                                              // lowest-ranked unoccupied paddock    

                    bestPriority = Int32.MaxValue;                                         // Locate the next-smallest priority score 
                    for (idx = 1; idx <= this.Count(); idx++)
                    {
                        if ((this.GetPriority(idx) < bestPriority) && (this.GetPriority(idx) > prevPriority))
                            bestPriority = this.GetPriority(idx);
                    }

                    // Move animals with that priority score 
                    for (idx = 1; idx <= this.Count(); idx++)                               
                    {
                        if ((this.GetTag(idx) == tagNo) && (this.GetPriority(idx) == bestPriority))
                            this.SetInPadd(idx, this.Paddocks.ByIndex(bestPadd).Name);
                    }

                    available[bestPadd] = false;

                    prevPadd = bestPadd;
                    prevPriority = bestPriority;
                }
                while (bestPriority != Int32.MaxValue);
            }
        }

        // ==============================================================================
        // Execute user's internally defined tasks for this day
        // ==============================================================================

        /// <summary>
        /// Setup the stock groups using the internal criteria the user has defined
        /// for this component.
        /// </summary>
        /// <param name="currentDay">Todays date</param>
        /// <param name="latitude">The Latitude</param>
        public void ManageInternalInit(int currentDay, double latitude)
        {
            int i;
            EnterpriseInfo curEnt;

            // for each enterprise
            for (i = 0; i <= this.Enterprises.Count - 1; i++)   
            {
                curEnt = this.Enterprises.byIndex(i);
 
                if (curEnt.ManageGrazing)
                    this.ManageGrazing(currentDay, currentDay, curEnt);
            }
        }

        /// <summary>
        /// Follow the management events described by the user for this stock component.
        /// </summary>
        /// <param name="currentDate">Todays date</param>
        public void ManageInternalTasks(int currentDate)
        {
            int i;
            EnterpriseInfo curEnt;
            int currentDay;

            currentDay = StdDate.DateVal(StdDate.DayOf(currentDate), StdDate.MonthOf(currentDate), 0);

            // for each enterprise
            for (i = 0; i <= this.Enterprises.Count - 1; i++)    
            {
                curEnt = this.Enterprises.byIndex(i);
                this.ManageDailyTasks(currentDay, curEnt);       // correct order?

                if (curEnt.ManageGrazing)
                    this.ManageGrazing(currentDate, currentDay, curEnt);

                if (curEnt.ManageReproduction)
                    this.ManageReproduction(currentDay, curEnt);
            } // next enterprise
        }

        // Paddock rank order ......................................................

        /// <summary>
        /// Rank the paddocks
        /// </summary>
        /// <param name="paddockList">List of paddocks returned</param>
        public void RankPaddocks(List<string> paddockList)
        {
            double[] paddockRank = new double[this.Paddocks.Count()];
            AnimalGroup tempAnimals;
            int bestPadd;
            double bestRank;
            int paddIdx, idx;

            if (this.Count() > 0)
                tempAnimals = this.At(1).Copy();
            else
                tempAnimals = new AnimalGroup(this.GetGenotype("Medium Merino"), GrazType.ReproType.Empty, 1, 365 * 4, 50.0, 0.0, parentStockModel.randFactory);
            for (paddIdx = 0; paddIdx <= this.Paddocks.Count() - 1; paddIdx++)
                paddockRank[paddIdx] = this.GetPaddockRank(this.Paddocks.ByIndex(paddIdx), tempAnimals);

            paddockList.Clear();
            for (idx = 0; idx <= this.Paddocks.Count() - 1; idx++)
            {
                bestRank = -1.0;
                bestPadd = -1;
                for (paddIdx = 0; paddIdx <= this.Paddocks.Count() - 1; paddIdx++)
                {
                    if (paddockRank[paddIdx] > bestRank)
                    {
                        bestPadd = paddIdx;
                        bestRank = paddockRank[paddIdx];
                    }
                }
                paddockList.Add(this.Paddocks.ByIndex(bestPadd).Name);
                paddockRank[bestPadd] = -999.9;
            }
        }

        /// <summary>
        /// The reproduction record
        /// </summary>
        private struct ReproRecord
        {
            /// <summary>
            /// The name
            /// </summary>
            public string Name;

            /// <summary>
            /// The reproduction record
            /// </summary>
            public GrazType.ReproType Repro;

            /// <summary>
            /// The ReproRecord constructor
            /// </summary>
            /// <param name="name">Name of the reproduction</param>
            /// <param name="repro">Reproduction type</param>
            public ReproRecord(string name, GrazType.ReproType repro)
            {
                this.Name = name;
                this.Repro = repro;
            }
        }

        /// <summary>
        /// Converts a ReproductiveType to a ReproType. 
        /// </summary>
        /// <param name="reproType">The keyword to match</param>
        /// <param name="repro">The reproduction record</param>
        /// <returns>True if the keyword is found</returns>
        private bool ParseRepro(ReproductiveType reproType, ref GrazType.ReproType repro)
        {
            switch (reproType)
            {
                case ReproductiveType.Female:
                    repro = GrazType.ReproType.Empty;
                    return true;
                case ReproductiveType.Male:
                    repro = GrazType.ReproType.Male;
                    return true;
                case ReproductiveType.Castrate:
                    repro = GrazType.ReproType.Castrated;
                    return true;
            }
            return false;
        }
                
        /// <summary>
        /// These functions return the number of days from the first date to the  
        /// second.  PosInterval assumes that its arguments are days-of-year, i.e.    
        /// YearOf(DOY1)=YearOf(DOY2)=0, while DaysFromDOY treats its first argument  
        /// as though it is a day-of-year and computes the number of days from that   
        /// day-of-year to the second date.                                           
        /// </summary>
        /// <param name="dayOfYear1">Start day</param>
        /// <param name="dayOfYear2">End day</param>
        /// <returns>The interval in days</returns>
        private int PosInterval(int dayOfYear1, int dayOfYear2)
        {
            int result = StdDate.Interval(dayOfYear1, dayOfYear2);
            if (result < 0)
                result = 366 + result;
            return result;
        }

        /// <summary>
        /// Get the days difference
        /// </summary>
        /// <param name="dayOfYear">Start date</param>
        /// <param name="theDate">The end date</param>
        /// <returns>The difference</returns>
        private int DaysFromDOY(int dayOfYear, int theDate)
        {
            int DOY_MASK = 0xFFFF;
            int result;
            if (StdDate.YearOf(theDate) == 0)
                result = this.PosInterval(dayOfYear & DOY_MASK, theDate);
            else
            {
                dayOfYear = StdDate.DateVal(StdDate.DayOf(dayOfYear), StdDate.MonthOf(dayOfYear), StdDate.YearOf(theDate));
                if (dayOfYear > theDate)
                    dayOfYear = StdDate.DateShift(dayOfYear, 0, 0, -1);
                result = StdDate.Interval(dayOfYear, theDate);
            }
            return result;
        }

        /// <summary>
        /// Tests for a non-MISSING, non-zero value                                      
        /// </summary>
        /// <param name="x">The test value</param>
        /// <returns>True if this is not a missing value</returns>
        public bool IsGiven(double x)
        {
            return ((x != 0.0) && (Math.Abs(x - StdMath.DMISSING) > Math.Abs(0.0001 * StdMath.DMISSING)));
        }

        /// <summary>
        /// Calculate the days from the day of year in a non leap year
        /// </summary>
        /// <param name="dayOfYear">Start day</param>
        /// <param name="otherDay">End day</param>
        /// <returns>The days in the interval</returns>
        public int DaysFromDOY365(int dayOfYear, int otherDay)
        {
            int theDOY;
            int result;

            if (dayOfYear == 0)
                result = 0;
            else
            {
                theDOY = StdDate.DateShift(StdDate.DateVal(31, 12, StdDate.YearOf(otherDay) - 1), dayOfYear % 365, 0, 0);
                if ((StdDate.YearOf(otherDay) > 0) && (theDOY > otherDay))
                    theDOY = StdDate.DateShift(theDOY, 0, 0, -1);
                result = StdDate.Interval(theDOY, otherDay);
            }
            return result;
        }

        // checking paddock for grazing move
        private const int MAX_CRITERIA = 1;
        private const int DRAFT_MOVE = 0;
        private string[] CRITERIA = new string[MAX_CRITERIA] { "draft" };   // used in radiogroup on dialog

        /// <summary>
        /// Utility routines for manipulating the DM_Pool type.  AddDMPool adds the   
        /// contents of two pools together
        /// </summary>
        /// <param name="pool1">DM pool 1</param>
        /// <param name="pool2">DM pool 2</param>
        /// <returns>The combined pool</returns>
        protected GrazType.DM_Pool AddDMPool(GrazType.DM_Pool pool1, GrazType.DM_Pool pool2)
        {
            int n = (int)GrazType.TOMElement.n;
            int p = (int)GrazType.TOMElement.p;
            int s = (int)GrazType.TOMElement.s;
            GrazType.DM_Pool result = new GrazType.DM_Pool();
            result.DM = pool1.DM + pool2.DM;
            result.Nu[n] = pool1.Nu[n] + pool2.Nu[n];
            result.Nu[p] = pool1.Nu[p] + pool2.Nu[p];
            result.Nu[s] = pool1.Nu[s] + pool2.Nu[s];
            result.AshAlk = pool1.AshAlk + pool2.AshAlk;

            return result;
        }

        /// <summary>
        /// MultiplyDMPool scales the contents of a pool                                                                 
        /// </summary>
        /// <param name="srcPool">The dm pool to scale</param>
        /// <param name="scale">The scale</param>
        /// <returns>The scaled pool</returns>
        protected GrazType.DM_Pool MultiplyDMPool(GrazType.DM_Pool srcPool, double scale)
        {
            int n = (int)GrazType.TOMElement.n;
            int p = (int)GrazType.TOMElement.p;
            int s = (int)GrazType.TOMElement.s;
            GrazType.DM_Pool result = new GrazType.DM_Pool();
            result.DM = srcPool.DM * scale;
            result.Nu[n] = srcPool.Nu[n] * scale;
            result.Nu[p] = srcPool.Nu[p] * scale;
            result.Nu[s] = srcPool.Nu[s] * scale;
            result.AshAlk = srcPool.AshAlk * scale;

            return result;
        }

        /// <summary>
        /// Get the young offspring rates
        /// </summary>
        /// <param name="parameters">The animal parameters</param>
        /// <param name="latitude">The latitude</param>
        /// <param name="mateDOY">Mating day of year</param>
        /// <param name="ageDays">Age in days</param>
        /// <param name="matingSize">Mating size</param>
        /// <param name="condition">Animal condition</param>
        /// <param name="chillIndex">The chill index</param>
        /// <returns>Offspring rates</returns>
        private double[] GetOffspringRates(AnimalParamSet parameters, double latitude, int mateDOY, int ageDays, double matingSize, double condition, double chillIndex = 0.0)
        {
            const double NO_CYCLES = 2.5;
            const double STD_LATITUDE = -35.0;             // Latitude (in degrees) for which the DayLengthConst[] parameters are set    
            double[] result;
            double[] conceptions = new double[4];
            double emptyPropn;
            double dayLengthFactor;
            double propn;
            double exposureOdds;
            double deathRate;
            int n;

            dayLengthFactor = (1.0 - Math.Sin(GrazEnv.DAY2RAD * (mateDOY + 10)))
                         * Math.Sin(GrazEnv.DEG2RAD * latitude) / Math.Sin(GrazEnv.DEG2RAD * STD_LATITUDE);
            for (n = 1; n <= parameters.MaxYoung; n++)
            {
                if ((ageDays > parameters.Puberty[0]) && (parameters.ConceiveSigs[n][0] < 5.0))     // Puberty[false]
                    propn = StdMath.DIM(1.0, parameters.DayLengthConst[n] * dayLengthFactor)
                              * StdMath.SIG(matingSize * condition, parameters.ConceiveSigs[n]);
                else
                    propn = 0.0;

                if (n == 1)
                    conceptions[n] = propn;
                else
                {
                    conceptions[n] = propn * conceptions[n - 1];
                    conceptions[n - 1] = conceptions[n - 1] - conceptions[n];
                }
            }

            emptyPropn = 1.0;
            for (n = 1; n <= parameters.MaxYoung; n++)
                emptyPropn = emptyPropn - conceptions[n];

            result = new double[4];
            if (emptyPropn < 1.0)
                for (n = 1; n <= parameters.MaxYoung; n++)
                    result[n] = conceptions[n] * (1.0 - Math.Pow(emptyPropn, NO_CYCLES)) / (1.0 - emptyPropn);

            if ((chillIndex > 0) && (parameters.Animal == GrazType.AnimalType.Sheep))
            {
                for (n = 1; n <= parameters.MaxYoung; n++)
                {
                    exposureOdds = parameters.ExposureConsts[0] - (parameters.ExposureConsts[1] * condition) + (parameters.ExposureConsts[2] * chillIndex);
                    if (n > 1)
                        exposureOdds = exposureOdds + parameters.ExposureConsts[3];
                    deathRate = Math.Exp(exposureOdds) / (1.0 + Math.Exp(exposureOdds));

                    result[n] = (1.0 - deathRate) * result[n];
                }
            }
            return result;
        }

        /// <summary>
        /// Add( TAnimalList, TPaddockInfo, integer, integer )                           
        /// Private variant. Adds all members of a TAnimalList back into the stock list  
        /// </summary>
        /// <param name="animalList">The source animal list</param>
        /// <param name="paddInfo">The paddock info</param>
        /// <param name="tagNo">The tag number</param>
        /// <param name="priority">Priority value</param>
        public void Add(AnimalList animalList, PaddockInfo paddInfo, int tagNo, int priority)
        {
            int idx;

            if (animalList != null)
                for (idx = 0; idx <= animalList.Count - 1; idx++)
                {
                    this.Add(animalList.At(idx), paddInfo, tagNo, priority);
                    animalList.SetAt(idx, null);                           // Detach the animal group from the TAnimalList                              
                }
        }
        
        /// <summary>
        /// The main stock management function that handles a number of events.
        /// </summary>
        /// <param name="model">The stock model</param>
        /// <param name="stockEvent">The event parameters</param>
        /// <param name="dateToday">Today's date</param>
        /// <param name="latitude">The latitiude</param>
        public void DoStockManagement(StockList model, IStockEvent stockEvent, int dateToday = 0, double latitude = -35.0)
        {
            CohortsInfo cohort = new CohortsInfo();
            PurchaseInfo purchaseInfo = new PurchaseInfo();
            List<string> closedPaddocks;
            string strParam;
            int param1;
            int param3;
            double value;
            int tagNo;
            int numGroups;

            if (stockEvent != null)
            {
                // add_animals
                if (stockEvent.GetType() == typeof(StockAdd))          
                {
                    StockAdd stockInfo = (StockAdd)stockEvent;
                    cohort.Genotype = stockInfo.Genotype;
                    cohort.Number = Math.Max(0, stockInfo.Number);
                    if (!this.ParseRepro(stockInfo.Sex, ref cohort.ReproClass))
                        throw new Exception("Event ADD does not support sex='" + stockInfo.Sex + "'");
                    if (dateToday > 0)
                        cohort.AgeOffsetDays = this.DaysFromDOY365(stockInfo.BirthDay, dateToday);
                    else
                        cohort.AgeOffsetDays = 0;
                    cohort.MinYears = stockInfo.MinYears;
                    cohort.MaxYears = stockInfo.MaxYears;
                    cohort.MeanLiveWt = stockInfo.MeanWeight;
                    cohort.CondScore = stockInfo.CondScore;
                    cohort.MeanGFW = stockInfo.MeanFleeceWt;
                    if (dateToday > 0)
                        cohort.FleeceDays = this.DaysFromDOY365(stockInfo.ShearDay, dateToday);
                    else
                        cohort.FleeceDays = 0;
                    cohort.MatedTo = stockInfo.MatedTo;
                    cohort.DaysPreg = stockInfo.Pregnant;
                    cohort.Foetuses = stockInfo.Foetuses;
                    cohort.DaysLact = stockInfo.Lactating;
                    cohort.Offspring = stockInfo.Offspring;
                    cohort.OffspringWt = stockInfo.YoungWt;
                    cohort.OffspringCS = stockInfo.YoungCondScore;
                    cohort.LambGFW = stockInfo.YoungFleeceWt;

                    if (cohort.Number > 0)
                        model.AddCohorts(cohort, 1 + this.DaysFromDOY365(1, dateToday), latitude, null);
                }
                else if (stockEvent.GetType() == typeof(StockBuy))
                {
                    StockBuy stockInfo = (StockBuy)stockEvent;
                    purchaseInfo.Genotype = stockInfo.Genotype;
                    purchaseInfo.Number = Math.Max(0, stockInfo.Number);
                    if (!this.ParseRepro(stockInfo.Sex, ref purchaseInfo.Repro))
                        throw new Exception("Event BUY does not support sex='" + stockInfo.Sex + "'");
                    purchaseInfo.AgeDays = Convert.ToInt32(Math.Round(MONTH2DAY * stockInfo.Age), CultureInfo.InvariantCulture);  // Age in months
                    purchaseInfo.LiveWt = stockInfo.Weight;
                    purchaseInfo.GFW = stockInfo.FleeceWt;
                    purchaseInfo.CondScore = stockInfo.CondScore;
                    purchaseInfo.MatedTo = stockInfo.MatedTo;
                    purchaseInfo.Preg = stockInfo.Pregnant;
                    purchaseInfo.Lact = stockInfo.Lactating;
                    purchaseInfo.NYoung = stockInfo.NumYoung;
                    if ((purchaseInfo.Preg > 0) || (purchaseInfo.Lact > 0))
                        purchaseInfo.NYoung = Math.Max(1, purchaseInfo.NYoung);
                    purchaseInfo.YoungWt = stockInfo.YoungWt;
                    if ((purchaseInfo.Lact == 0) || (purchaseInfo.YoungWt == 0.0))                              // Can't use MISSING as default owing    
                        purchaseInfo.YoungWt = StdMath.DMISSING;                                                // to double-to-single conversion      
                    purchaseInfo.YoungGFW = stockInfo.YoungFleeceWt;
                    tagNo = stockInfo.UseTag;

                    if (purchaseInfo.Number > 0)
                    {
                        model.Buy(purchaseInfo);
                        if (tagNo > 0)
                            model.SetTag(model.Count(), tagNo);
                    }
                } 
                else if (stockEvent.GetType() == typeof(StockSell))
                {
                    // sell a number from a group of animals
                    StockSell stockInfo = (StockSell)stockEvent;
                    model.Sell(stockInfo.Group, stockInfo.Number);
                }
                else if (stockEvent.GetType() == typeof(StockSellTag))
                {
                    // sell a number of animals tagged with a specific tag 
                    StockSellTag stockInfo = (StockSellTag)stockEvent;
                    model.SellTag(stockInfo.Tag, stockInfo.Number);
                }
                else if (stockEvent.GetType() == typeof(StockShear))
                {
                    StockShear stockInfo = (StockShear)stockEvent;
                    strParam = stockInfo.SubGroup.ToLower();
                    model.Shear(stockInfo.Group, ((strParam == "adults") || (strParam == "both") || (strParam == string.Empty)), ((strParam == "lambs") || (strParam == "both")));
                }
                else if (stockEvent.GetType() == typeof(StockMove))
                {
                    StockMove stockInfo = (StockMove)stockEvent;
                    param1 = stockInfo.Group;
                    if ((param1 >= 1) && (param1 <= model.Count()))
                        model.SetInPadd(param1, stockInfo.Paddock);
                    else
                        throw new Exception("Invalid group number in MOVE event");
                }
                else if (stockEvent.GetType() == typeof(StockJoin))
                {
                    StockJoin stockInfo = (StockJoin)stockEvent;
                    model.Join(stockInfo.Group, stockInfo.MateTo, stockInfo.MateDays);
                }
                else if (stockEvent.GetType() == typeof(StockCastrate))
                {
                    StockCastrate stockInfo = (StockCastrate)stockEvent;
                    model.Castrate(stockInfo.Group, stockInfo.Number);
                }
                else if (stockEvent.GetType() == typeof(StockWean))
                {
                    StockWean stockInfo = (StockWean)stockEvent;
                    param1 = stockInfo.Group;
                    strParam = stockInfo.Sex.ToLower();
                    param3 = stockInfo.Number;

                    if (strParam == "males")
                        model.Wean(param1, param3, false, true);
                    else if (strParam == "females")
                        model.Wean(param1, param3, true, false);
                    else if ((strParam == "all") || (strParam == "both") || (strParam == string.Empty))
                        model.Wean(param1, param3, true, true);
                    else
                        throw new Exception("Invalid offspring type \"" + strParam + "\" in WEAN event");
                }
                else if (stockEvent.GetType() == typeof(StockDryoff))
                {
                    StockDryoff stockInfo = (StockDryoff)stockEvent;
                    model.DryOff(stockInfo.Group, stockInfo.Number);
                }
                else if (stockEvent.GetType() == typeof(StockSplitAll))
                {
                    // split off the requested animals from all groups
                    StockSplitAll stockInfo = (StockSplitAll)stockEvent;
                    numGroups = model.Count(); // get pre-split count of groups
                    for (param1 = 1; param1 <= numGroups; param1++)
                    {
                        int groups = model.Count();
                        strParam = stockInfo.Type.ToLower();
                        value = stockInfo.Value;
                        tagNo = stockInfo.OtherTag;

                        if (strParam == "age")
                            model.SplitAge(param1, Convert.ToInt32(Math.Round(value), CultureInfo.InvariantCulture));
                        else if (strParam == "weight")
                            model.SplitWeight(param1, value);
                        else if (strParam == "young")
                            model.SplitYoung(param1);
                        else if (strParam == "number")
                            model.Split(param1, Convert.ToInt32(Math.Round(value), CultureInfo.InvariantCulture));
                        else
                            throw new Exception("Stock: invalid keyword (" + strParam + ") in \"split\" event");
                        if ((tagNo > 0) && (model.Count() > groups))     // if a tag for any new group is given
                        {
                            for (int g = groups + 1; g <= model.Count(); g++)
                                model.SetTag(g, tagNo);
                        }
                    }
                }
                else if (stockEvent.GetType() == typeof(StockSplit))
                {
                    // split off the requested animals from one group
                    StockSplit stockInfo = (StockSplit)stockEvent;
                    numGroups = model.Count(); // get pre-split count of groups
                    param1 = stockInfo.Group;
                    strParam = stockInfo.Type.ToLower();
                    value = stockInfo.Value;
                    tagNo = stockInfo.OtherTag;

                    if ((param1 < 1) && (param1 > model.Count()))
                        throw new Exception("Invalid group number in SPLIT event");
                    else if (strParam == "age")
                        model.SplitAge(param1, Convert.ToInt32(Math.Round(value)));
                    else if (strParam == "weight")
                        model.SplitWeight(param1, value);
                    else if (strParam == "young")
                        model.SplitYoung(param1);
                    else if (strParam == "number")
                        model.Split(param1, Convert.ToInt32(Math.Round(value), CultureInfo.InvariantCulture));
                    else
                        throw new Exception("Stock: invalid keyword (" + strParam + ") in \"split\" event");
                    if ((tagNo > 0) && (model.Count() > numGroups))     // if a tag for the new group is given
                    {
                        for (int g = numGroups + 1; g <= model.Count(); g++)
                            model.SetTag(g, tagNo);
                    }
                }
                else if (stockEvent.GetType() == typeof(StockTag))
                {
                    StockTag stockInfo = (StockTag)stockEvent;
                    param1 = stockInfo.Group;
                    if ((param1 >= 1) && (param1 <= model.Count()))
                        model.SetTag(param1, stockInfo.Value);
                    else
                        throw new Exception("Invalid group number in TAG event");
                }
                else if (stockEvent.GetType() == typeof(StockSort))
                {
                    model.Sort();
                }
                else if (stockEvent.GetType() == typeof(StockPrioritise))
                {
                    StockPrioritise stockInfo = (StockPrioritise)stockEvent;
                    param1 = stockInfo.Group;
                    if ((param1 >= 1) && (param1 <= model.Count()))
                        model.SetPriority(param1, stockInfo.Value);
                    else
                        throw new Exception("Invalid group number in PRIORITISE event");
                }
                else if (stockEvent.GetType() == typeof(StockDraft))
                {
                    StockDraft stockInfo = (StockDraft)stockEvent;
                    closedPaddocks = new List<string>(stockInfo.Closed);
                    
                    model.Draft(closedPaddocks);
                }
                else
                    throw new Exception("Event not recognised in STOCK");
            }
        }
    }
}
