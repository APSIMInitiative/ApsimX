using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Models.Core;
using Models.ForageDigestibility;
using Models.Interfaces;
using StdUnits;

namespace Models.GrazPlan
{

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
        /// Conversion factor for months to days
        /// </summary>
        private const double MONTH2DAY = 365.25 / 12;

        /// <summary>
        /// Converts animal mass into "dse"s for trampling purposes
        /// </summary>
        private const double WEIGHT2DSE = 0.02;

        /// <summary>The parent stock model.</summary>
        private readonly Stock parentStockModel = null;

        /// <summary>The clock model.</summary>
        private readonly IClock clock;

        /// <summary>The weather model.</summary>
        private readonly IWeather weather;

        /// <summary>
        /// stock[0] is kept for use as temporary storage
        /// </summary>
        private AnimalGroup[] stock = new AnimalGroup[0];

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="stockModel">The parent stock model.</param>
        /// <param name="clockModel">The clock model.</param>
        /// <param name="weatherModel">The weather model.</param>
        /// <param name="paddocksInSimulation">The paddocks in the simulation.</param>
        public StockList(Stock stockModel, IClock clockModel, IWeather weatherModel, List<Zone> paddocksInSimulation)
        {
            parentStockModel = stockModel;
            ForagesAll = new ForageProviders();
            Enterprises = new List<EnterpriseInfo>();
            clock = clockModel;
            weather = weatherModel;

            Array.Resize(ref this.stock, 1);                                          // Set aside temporary storage
            Paddocks = new List<PaddockInfo>();

            Paddocks.Add(new PaddockInfo());

            // get the paddock areas from the simulation
            foreach (var zone in paddocksInSimulation)
            {
                var newPadd = new PaddockInfo(zone) { zone = zone };
                Paddocks.Add(newPadd);

                // find all the child crop, pasture components that have removable biomass
                var forages = stockModel.FindInScope<Forages>();
                if (forages == null)
                    throw new Exception("No forages component found in simulation.");

                foreach (var forage in forages.ModelsWithDigestibleBiomass.Where(m => m.Zone == zone))
                    ForagesAll.AddProvider(newPadd, zone.Name, zone.Name + "." + forage.Name, 0, 0, forage);
            }
        }

        /// <summary>Gets an enumeration of all animal groups.</summary>
        public IList<AnimalGroup> Animals { get { return stock; } }

        /// <summary>
        /// Gets the list of paddocks
        /// </summary>
        public List<PaddockInfo> Paddocks { get; }

        /// <summary>
        /// Gets the enterprise list
        /// </summary>
        public List<EnterpriseInfo> Enterprises { get; }

        /// <summary>
        /// Gets all the forage providers
        /// </summary>
        public ForageProviders ForagesAll { get; }

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
                if ((stock[idx] != null) && (stock[idx].NoAnimals == 0))
                {
                    stock[idx] = null;
                }
            }

            // Merge similar groups
            for (idx = 1; idx <= this.Count() - 1; idx++)
            {
                for (jdx = idx + 1; jdx <= this.Count(); jdx++)
                {
                    if ((stock[idx] != null) && (stock[jdx] != null)
                       && stock[idx].Similar(stock[jdx])
                       && (stock[idx].PaddOccupied == stock[jdx].PaddOccupied)
                       && (stock[idx].Tag == stock[jdx].Tag))
                    {
                        animalGroup = stock[jdx];
                        stock[jdx] = null;
                        stock[idx].Merge(ref animalGroup);
                    }
                }
            }

            // Pack the lists and priority array.
            for (idx = this.Count(); idx >= 1; idx--)
            {
                if (stock[idx] == null)
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
            stock[posIdx].StoreStateInfo(ref this.stock[posIdx].InitState[0]);
            if (stock[posIdx].Young != null)
                stock[posIdx].Young.StoreStateInfo(ref this.stock[posIdx].InitState[1]);
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
            stock[posIdx].RevertStateInfo(this.stock[posIdx].InitState[0]);
            stock[posIdx].PotIntake = stock[posIdx].PotIntake * this.stock[posIdx].RDPFactor[0];

            if (stock[posIdx].Young != null)
            {
                stock[posIdx].Young.RevertStateInfo(this.stock[posIdx].InitState[1]);
                stock[posIdx].Young.PotIntake = stock[posIdx].Young.PotIntake * this.stock[posIdx].RDPFactor[1];
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

            group = stock[posIdx];
            paddock = stock[posIdx].PaddOccupied;

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

            paddock = stock[posIdx].PaddOccupied;
            group = stock[posIdx];

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
            while ((posn <= this.Count()) && (stock[posn].PaddOccupied != paddock))    // this paddock
                posn++;

            if ((posn > this.Count()) || (paddock.Area <= 0.0))
                result = 1.0;
            else
            {
                stock[posn].CalculateRelIntake(stock[posn], 1.0, false, 1.0, ref herbageRI, ref seedRI, ref suppRelIntake);

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
            group.CalculateIntakeLimit();
            if (group.Young != null)
                group.Young.CalculateIntakeLimit();
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
            stock[posIdx].Grazing(deltaTime, (startTime == 0.0), feedSuppFirst, ref this.stock[posIdx].PastIntakeRate[0], ref this.stock[posIdx].SuppIntakeRate[0]);
            if (stock[posIdx].Young != null)
                stock[posIdx].Young.Grazing(deltaTime, (startTime == 0.0), false, ref this.stock[posIdx].PastIntakeRate[1], ref this.stock[posIdx].SuppIntakeRate[1]);
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
                    if (stock[posn].PaddOccupied == paddock)
                    {
                        group = stock[posn];

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
                    if (stock[posn].PaddOccupied == paddock)
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
            stock[posIdx].Nutrition();
            this.stock[posIdx].RDPFactor[0] = stock[posIdx].RDPIntakeFactor();
            availRDP = Math.Min(availRDP, this.stock[posIdx].RDPFactor[0]);
            if (stock[posIdx].Young != null)
            {
                stock[posIdx].Young.Nutrition();
                this.stock[posIdx].RDPFactor[1] = stock[posIdx].Young.RDPIntakeFactor();
                availRDP = Math.Min(availRDP, this.stock[posIdx].RDPFactor[1]);
            }
        }

        /// <summary>
        /// Complete the animal growth
        /// </summary>
        /// <param name="posIdx">Index in the stock list</param>
        private void CompleteGrowth(int posIdx)
        {
            stock[posIdx].CompleteGrowth(this.stock[posIdx].RDPFactor[0]);
            if (stock[posIdx].Young != null)
                stock[posIdx].Young.CompleteGrowth(this.stock[posIdx].RDPFactor[1]);
        }

        /// <summary>
        /// Add a group of animals to the list
        /// Returns the group index of the group that was added. 0->n
        /// </summary>
        /// <param name="animalGroup">Animal group</param>
        /// <param name="paddInfo">The paddock information</param>
        /// <param name="tagNo">Tag value</param>
        /// <returns>The index of the new group in the stock array</returns>
        public int Add(AnimalGroup animalGroup, PaddockInfo paddInfo, int tagNo)
        {
            int idx;

            animalGroup.CalculateIntakeLimit();

            idx = this.stock.Length;
            Array.Resize(ref this.stock, idx + 1);
            this.stock[idx] = animalGroup.Copy();
            this.stock[idx].PaddOccupied = paddInfo;
            this.stock[idx].Tag = tagNo;

            this.SetInitialStockInputs(idx);
            return idx;
        }

        /// <summary>
        /// Returns the group index of the group that was added. 0->n
        /// </summary>
        /// <param name="animalInits">The animal data</param>
        /// <returns>The index of the new animal group</returns>
        public int Add(Animals animalInits)
        {
            AnimalGroup newGroup;
            PaddockInfo paddock;

            newGroup = new AnimalGroup(parentStockModel.Genotypes.Get(animalInits.Genotype),
                                       animalInits.Sex,
                                       animalInits.Number,
                                       animalInits.AgeDays,
                                       animalInits.Weight,
                                       animalInits.FleeceWt,
                                       parentStockModel.randFactory,
                                       clock, weather, this);
            if (this.IsGiven(animalInits.MaxPrevWt))
                newGroup.MaxPrevWeight = animalInits.MaxPrevWt;
            if (this.IsGiven(animalInits.FibreDiam))
                newGroup.FibreDiam = animalInits.FibreDiam;

            if (animalInits.MatedTo != string.Empty)
                newGroup.MatedTo = parentStockModel.Genotypes.Get(animalInits.MatedTo);
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
                    newGroup.BirthCondition = StockUtilities.CondScore2Condition(animalInits.BirthCS, StockUtilities.Cond_System.csSYSTEM1_5);
            }

            if (newGroup.Young != null)
            {
                if (this.IsGiven(animalInits.YoungWt))
                    newGroup.Young.LiveWeight = animalInits.YoungWt;
                if (this.IsGiven(animalInits.YoungGFW))
                    newGroup.Young.FleeceCutWeight = animalInits.YoungGFW;
            }

            paddock = this.Paddocks.Find(p => p.Name.Equals(animalInits.Paddock, StringComparison.InvariantCultureIgnoreCase));
            if (paddock == null)
                paddock = this.Paddocks[0];

            return this.Add(newGroup, paddock, animalInits.Tag);
        }

        /// <summary>Adds animals.</summary>
        /// <param name="stockInfo">The info about each animal.</param>
        public void Add(StockAdd stockInfo)
        {
            var cohort = new CohortsInfo
            {
                Genotype = stockInfo.Genotype,
                Number = Math.Max(0, stockInfo.Number),
                AgeOffsetDays = this.DaysFromDOY365Simple(stockInfo.BirthDay, clock.Today.DayOfYear),
                MinYears = stockInfo.MinYears,
                MaxYears = stockInfo.MaxYears,
                MeanLiveWt = stockInfo.MeanWeight,
                CondScore = stockInfo.CondScore,
                MeanGFW = stockInfo.MeanFleeceWt,
                FleeceDays = this.DaysFromDOY365Simple(stockInfo.ShearDay, clock.Today.DayOfYear),
                MatedTo = stockInfo.MatedTo,
                DaysPreg = stockInfo.Pregnant,
                Foetuses = stockInfo.Foetuses,
                DaysLact = stockInfo.Lactating,
                Offspring = stockInfo.Offspring,
                OffspringWt = stockInfo.YoungWt,
                OffspringCS = stockInfo.YoungCondScore,
                LambGFW = stockInfo.YoungFleeceWt
            };
            if (!this.ParseRepro(stockInfo.Sex, ref cohort.ReproClass))
                throw new Exception("Event ADD does not support sex='" + stockInfo.Sex + "'");

            if (cohort.Number > 0)
                AddCohorts(cohort, clock.Today.DayOfYear, weather.Latitude, null);
        }

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
                this.stock[posn] = null;

                for (idx = posn + 1; idx <= count; idx++)
                    this.stock[idx - 1] = this.stock[idx];
                Array.Resize(ref this.stock, count);                                               // Leave stock[0] as temporary storage
            }
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
        /// Get the highest tag number
        /// </summary>
        /// <returns>The highest tag value in the list</returns>
        public int HighestTag()
        {
            int idx;

            int result = 0;
            for (idx = 1; idx <= this.Count(); idx++)
                result = Math.Max(result, stock[idx].Tag);
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

            thePadd = this.Paddocks.Find(p => p.Name.Equals(paddName, StringComparison.InvariantCultureIgnoreCase));
            if (thePadd == null)
                throw new Exception("Stock: attempt to feed supplement into non-existent paddock");
            else
                thePadd.FeedSupplement(suppKG, supplement, feedSuppFirst);
        }

        /// <summary>
        /// Advance the list by one time step.  All the input properties should be set first
        /// </summary>
        public void Dynamics()
        {
            const double EPS = 1.0E-6;

            double totPotIntake;
            List<AnimalGroup> newGroups;
            PaddockInfo thePaddock;
            double timeValue;
            double delta;
            double RDP;
            int paddIdx, idx, n;
            int iterator;
            bool done;

            for (paddIdx = 0; paddIdx <= this.Paddocks.Count() - 1; paddIdx++)
            {
                thePaddock = this.Paddocks[paddIdx];
                thePaddock.ComputeTotals();
            }

            for (idx = 1; idx <= this.Count(); idx++)
                this.SetInitialStockInputs(idx);

            // Aging, birth, deaths etc. Animal groups may appear in the NewGroups list as a result of processes such as lamb deaths
            n = this.Count();
            for (idx = 1; idx <= n; idx++)
            {
                newGroups = null;
                stock[idx].Age(1, ref newGroups);

                // Ensure the new young have climate data
                this.Add(newGroups, stock[idx].PaddOccupied, stock[idx].Tag);       // The new groups are added back onto
                newGroups = null;                                                           // the main list
            }

            this.Merge();                                                                        // Merge any similar animal groups

            // Now run the grazing and nutrition models. This process is quite involved...
            for (idx = 1; idx <= this.Count(); idx++)
            {
                this.StoreInitialState(idx);
                this.ComputeIntakeLimit(stock[idx]);
                stock[idx].ResetGrazing();
            }

            // Compute the total potential intake (used to distribute supplement between groups of animals)
            for (paddIdx = 0; paddIdx <= this.Paddocks.Count() - 1; paddIdx++)
            {
                thePaddock = this.Paddocks[paddIdx];
                totPotIntake = 0.0;

                for (idx = 1; idx <= this.Count(); idx++)
                    if (stock[idx].PaddOccupied == thePaddock)
                    {
                        totPotIntake = totPotIntake + (stock[idx].NoAnimals * stock[idx].PotIntake);
                        if (stock[idx].Young != null)
                            totPotIntake = totPotIntake + (stock[idx].Young.NoAnimals * stock[idx].Young.PotIntake);
                    }
                thePaddock.SummedPotIntake = totPotIntake;
            }

            // We loop over paddocks and then over animal groups within a paddock so that we can take account of herbage
            for (paddIdx = 0; paddIdx <= this.Paddocks.Count() - 1; paddIdx++)
            {
                thePaddock = this.Paddocks[paddIdx];

                // removal & its effect on intake
                iterator = 1;                                                                  // This loop handles RDP insufficiency
                done = false;
                while (!done)
                {
                    timeValue = 0.0;                                                            // Variable-length substeps for grazing

                    while (timeValue < 1.0 - EPS)
                    {
                        for (idx = 1; idx <= this.Count(); idx++)
                            if (stock[idx].PaddOccupied == thePaddock)
                                this.ComputeStepAvailability(idx);

                        delta = Math.Min(this.ComputeStepLength(thePaddock), 1.0 - timeValue);

                        // Compute rate of grazing for this substep
                        for (idx = 1; idx <= this.Count(); idx++)
                            if (stock[idx].PaddOccupied == thePaddock)
                                this.ComputeGrazing(idx, timeValue, delta, thePaddock.FeedSuppFirst);

                        this.ComputeRemoval(thePaddock, delta);

                        timeValue = timeValue + delta;
                    } // _ grazing sub-steps loop _

                    // Nutrition submodel here...
                    RDP = 1.0;
                    for (idx = 1; idx <= this.Count(); idx++)
                        if (stock[idx].PaddOccupied == thePaddock)
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
                                if (stock[idx].PaddOccupied == thePaddock)
                                    this.RevertInitialState(idx);
                        }
                    }

                    iterator++;
                } // _ RDP loop _
            } // _ loop over paddocks _

            for (idx = 1; idx <= this.Count(); idx++)
                this.CompleteGrowth(idx);
        }

        /// <summary>
        /// Get the mass of animals per ha
        /// </summary>
        /// <param name="paddockName">Name of the zone/paddock</param>
        /// <param name="units">units per area</param>
        /// <returns></returns>
        public double ReturnMassPerArea(string paddockName, string units)
        {
            double result = 0;

            if (paddockName != string.Empty)
            {
                PaddockInfo thePadd = this.Paddocks.Find(p => p.Name.Equals(paddockName, StringComparison.InvariantCultureIgnoreCase));
                result = ReturnMassPerArea(thePadd, null, units);
            }

            return result;
        }

        /// <summary>
        /// Get the mass for the area
        /// </summary>
        /// <param name="thePadd">Paddock</param>
        /// <param name="provider">The forage provider object</param>
        /// <param name="units">The mass or dse per area units</param>
        /// <returns>The mass</returns>
        public double ReturnMassPerArea(PaddockInfo thePadd, ForageProvider provider, string units)
        {
            double result = 0;

            if (provider != null)
                thePadd = provider.OwningPaddock;

            double massKGHA = 0.0;
            if (thePadd != null)
            {
                for (int idx = 1; idx <= this.Count(); idx++)
                {
                    if (stock[idx].PaddOccupied == thePadd)
                    {
                        massKGHA = massKGHA + (stock[idx].NoAnimals * stock[idx].LiveWeight);
                        if (stock[idx].Young != null)
                            massKGHA = massKGHA + (stock[idx].Young.NoAnimals * stock[idx].Young.LiveWeight);
                    }
                }
                massKGHA = massKGHA / thePadd.Area;


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
            }

            return result;
        }

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
        /// <param name="thePadd">Paddock</param>
        /// <param name="excretion">The excretion info</param>
        public void ReturnExcretion(PaddockInfo thePadd, out ExcretionInfo excretion)
        {
            double area;
            int idx;

            if (thePadd != null)
                area = thePadd.Area;
            else if (this.Paddocks.Count() == 0)
                area = 1.0;
            else
            {
                area = 0.0;
                for (idx = 0; idx <= this.Paddocks.Count() - 1; idx++)
                    area = area + this.Paddocks[idx].Area;
            }

            excretion = new ExcretionInfo();
            for (idx = 1; idx <= this.Count(); idx++)
            {
                if ((thePadd == null) || (stock[idx].PaddOccupied == thePadd))
                {
                    this.AddExcretions(ref excretion, stock[idx].Excretion);
                    if (stock[idx].Young != null)
                        this.AddExcretions(ref excretion, stock[idx].Young.Excretion);
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
                theGroup = stock[idx].Young;
            else
                theGroup = stock[idx];
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
        private double MaxNormWtFunc(double srw, double bw, int ageDays, Genotype parameters)
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
        public double GrowthCurve(int ageDays, GrazType.ReproType reprodStatus, Genotype parameters)
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
        private double GetReproRate(CohortsInfo cohortsInfo, Genotype mainGenotype, AgeInfo[] ageInfo, double latitude, int mateDOY, double condition, double chill)
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
        /// Add animal cohorts
        /// </summary>
        /// <param name="cohortsInfo">The animal cohort</param>
        /// <param name="dayOfYear">Day of the year</param>
        /// <param name="latitude">The latitude</param>
        /// <param name="newGroups">List of new animal groups</param>
        public void AddCohorts(CohortsInfo cohortsInfo, int dayOfYear, double latitude, List<int> newGroups)
        {
            Genotype mainGenotype;
            AgeInfo[] ageInfoList;

            var animalInits = new Animals();
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
                mainGenotype = parentStockModel.Genotypes.Get(cohortsInfo.Genotype);

                ageInfoList = new AgeInfo[cohortsInfo.MaxYears + 1];
                for (int i = 0; i < cohortsInfo.MaxYears + 1; i++)
                    ageInfoList[i] = new AgeInfo();
                numCohorts = cohortsInfo.MaxYears - cohortsInfo.MinYears + 1;
                survival = 1.0 - mainGenotype.AnnualDeaths(false);

                if (mainGenotype.Animal == GrazType.AnimalType.Cattle)
                    daysSinceShearing = 0;
                else if (this.IsGiven(cohortsInfo.MeanGFW) && (cohortsInfo.FleeceDays == 0))
                    daysSinceShearing = Convert.ToInt32(Math.Truncate(365.25 * cohortsInfo.MeanGFW / mainGenotype.PotFleeceWt), CultureInfo.InvariantCulture);
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
                    ageInfoList[cohortIdx].FleeceWt = StockUtilities.DefaultFleece(
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
                    baseWtScalar = StockUtilities.CondScore2Condition(cohortsInfo.CondScore);
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
                                                                                    mainGenotype) / mainGenotype.SexStdRefWt(cohortsInfo.ReproClass);
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
                                                                                mainGenotype) / mainGenotype.SexStdRefWt(cohortsInfo.ReproClass);
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
                                animalInits.FibreDiam = StockUtilities.DefaultMicron(
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
            Genotype agenotype;
            AnimalGroup newGroup;
            double bodyCondition;
            double liveWeight;
            double lowBaseWeight = 0.0;
            double highBaseWeight = 0.0;
            List<AnimalGroup> weanList;
            int paddNo;

            int result = 0;

            if (animalInfo.Number > 0)
            {
                agenotype = parentStockModel.Genotypes.Get(animalInfo.Genotype);

                if (animalInfo.LiveWt > 0.0)
                    liveWeight = animalInfo.LiveWt;
                else
                {
                    liveWeight = this.GrowthCurve(animalInfo.AgeDays, animalInfo.Repro, agenotype);
                    if (animalInfo.CondScore > 0.0)
                        liveWeight = liveWeight * StockUtilities.CondScore2Condition(animalInfo.CondScore);
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
                                            parentStockModel.randFactory,
                                            clock, weather, this);

                // Adjust the condition score if it has been given
                if ((animalInfo.CondScore > 0.0) && (animalInfo.LiveWt > 0.0))
                {
                    bodyCondition = StockUtilities.CondScore2Condition(animalInfo.CondScore);
                    newGroup.WeightRangeForCond(
                                                animalInfo.Repro,
                                                animalInfo.AgeDays,
                                                bodyCondition,
                                                newGroup.Genotype,
                                                ref lowBaseWeight,
                                                ref highBaseWeight);

                    if ((newGroup.BaseWeight >= lowBaseWeight) && (newGroup.BaseWeight <= highBaseWeight))
                        newGroup.SetConditionAtWeight(bodyCondition);
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

                // ensure lactating sheep have young. Cattle can be purchased lactating with no calf
                if ((animalInfo.Lact > 0) && (newGroup.Animal == GrazType.AnimalType.Sheep))
                    animalInfo.NYoung = Math.Max(1, animalInfo.NYoung);

                // females will be empty to this point
                if (newGroup.ReproState == GrazType.ReproType.Empty)
                {
                    // Use TAnimalGroup's property interface to set up pregnancy and lactation.
                    if (animalInfo.MatedTo != string.Empty)
                        newGroup.MatedTo = parentStockModel.Genotypes.Get(animalInfo.MatedTo);
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
                                newGroup.NoFoetuses = Math.Min(2, Math.Max(0, animalInfo.NYoung - 1));  // recalculates livewt and sets ReproState
                            }
                            else
                            {
                                newGroup.NoFoetuses = Math.Min(3, animalInfo.NYoung);                   // recalculates livewt and sets ReproState
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
                while ((paddNo < this.Paddocks.Count()) && (this.Paddocks[paddNo].Name == string.Empty))
                    paddNo++;
                if (paddNo >= this.Paddocks.Count())
                    paddNo = 0;
                result = this.Add(newGroup, this.Paddocks[paddNo], 0);
            } // if AnimalInfo.Number > 0
            return result;
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
            number = Math.Max(number, 0);

            // Only iterate through groups present at the start of the routine
            int n = Count();
            for (int idx = 1; idx <= n; idx++)
            {
                // Group Idx, or all groups if 0
                if (((groupIdx == 0) || (groupIdx == idx)) && (stock[idx] != null))
                    number = number - Wean(stock[idx], number, weanFemales, weanMales);
            }
        }

        /// <summary>
        /// Wean animals in an animal group.
        /// </summary>
        /// <param name="group">The group to wean animals in.</param>
        /// <param name="number">The number of animals to wean.</param>
        /// <param name="weanFemales">Wean females?</param>
        /// <param name="weanMales">Wean males?</param>
        /// <returns>The number of animals weaned.</returns>
        public int Wean(AnimalGroup group, int number, bool weanFemales, bool weanMales)
        {
            if (group.Young != null)
            {
                // Establish the number of lambs/calves to wean from this group of mothers
                int numToWean = 0;
                if (weanMales && weanFemales)
                    numToWean = Math.Min(number, group.Young.NoAnimals);
                else if (weanMales)
                    numToWean = Math.Min(number, group.Young.MaleNo);
                else if (weanFemales)
                    numToWean = Math.Min(number, group.Young.FemaleNo);

                if (numToWean > 0)
                {
                    if (numToWean == number)
                    {
                        // If there are more lambs/calves present than are to be weaned, split the excess off
                        int mothersToWean;
                        if (weanMales && weanFemales)
                            mothersToWean = Convert.ToInt32(Math.Round((double)numToWean / group.NoOffspring), CultureInfo.InvariantCulture);
                        else
                            mothersToWean = Convert.ToInt32(Math.Round(numToWean / (group.NoOffspring / 2.0)), CultureInfo.InvariantCulture);
                        if (mothersToWean < group.NoAnimals)
                            this.Split(group, mothersToWean);
                    }

                    // Carry out the weaning process. N.B. the weaners appear in the same
                    // paddock as their mothers and with the same tag
                    List<AnimalGroup> newGroups = null;
                    group.Wean(weanFemales, weanMales, ref newGroups, ref newGroups);
                    Add(newGroups, group.PaddOccupied, group.Tag);
                }

                return numToWean;
            }
            return 0;
        }

        /// <summary>
        /// Ends lactation in cows that have already had their calves weaned.
        /// The event has no effect on other animals.
        /// </summary>
        /// <param name="groups">Groups</param>
        /// <param name="number">Number of animals</param>
        public void DryOff(IEnumerable<AnimalGroup> groups, int number)
        {
            number = Math.Max(number, 0);

            // Only iterate through groups present at the start of the routine
            foreach (var group in groups)
            {
                if (group.Lactation > 0)
                {
                    int numToDryOff = Math.Min(number, group.FemaleNo);
                    if (numToDryOff > 0)
                    {
                        if (numToDryOff < group.FemaleNo)
                            this.Split(group, numToDryOff);
                        group.DryOff();
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
        public int Split(int groupIdx, int numToKeep)
        {
            return Split(stock[groupIdx], numToKeep);
        }

        /// <summary>
        /// Break an animal group up in various ways; by number, by age, by weight
        /// or by sex of lambs/calves.  The new group(s) have the same priority and
        /// paddock as the original.  SplitWeight assumes a distribution of weights
        /// around the group average.
        /// </summary>
        /// <param name="group">The animal group to split.</param>
        /// <param name="numToKeep">Number of animals to keep.</param>
        public int Split(AnimalGroup group, int numToKeep)
        {
            int numToSplit = 0;
            if (group != null)
            {
                numToSplit = Math.Max(0, group.NoAnimals - Math.Max(numToKeep, 0));
                if (numToSplit > 0)
                    this.Add(group.Split(numToSplit, false, group.NODIFF, group.NODIFF), group.PaddOccupied, group.Tag);
            }

            return numToSplit;
        }

        /// <summary>
        /// Split the groups by age
        /// </summary>
        /// <param name="ageDays">Age in days</param>
        /// <param name="groups">The animal groups to split.</param>
        /// <return>The new groups.</return>
        public IEnumerable<AnimalGroup> SplitByAge(int ageDays, IEnumerable<AnimalGroup> groups)
        {
            var newGroups = new List<AnimalGroup>();
            foreach (var group in groups)
            {
                int numMales = 0;
                int numFemales = 0;

                group.GetOlder(ageDays, ref numMales, ref numFemales);
                if (numMales + numFemales > 0)
                {
                    var newGroup = group.Split(numMales + numFemales, true, group.NODIFF, group.NODIFF);
                    Add(newGroup, group.PaddOccupied, group.Tag);
                    newGroups.Add(stock.Last()); // Can't add newGroup because it is cloned inside of Add method.
                }
            }
            return newGroups;
        }

        /// <summary>
        /// Split the group by weight
        /// </summary>
        /// <param name="splitWt">The weight (kg)</param>
        /// <param name="groups">The animal groups to split.</param>
        /// <returns>The new groups that were created.</returns>
        public IEnumerable<AnimalGroup> SplitByWeight(double splitWt, IEnumerable<AnimalGroup> groups)
        {
            const double varRatio = 0.10;      // Coefficient of variation of LW (0-1)
            const int NOSTEPS = 20;

            var newGroups = new List<AnimalGroup>();
            foreach (var srcGroup in groups)
            {
                int numAnimals = srcGroup.NoAnimals;
                double liveWt = srcGroup.LiveWeight;

                // Calculate the position of the threshold on the live wt
                // distribution of the group, in S.D. units .
                // NB SplitWt is a live weight value.
                double splitSD = (splitWt - liveWt) / (varRatio * liveWt);

                // Calculate proportion of animals lighter than SplitWt .
                // NB Normal distribution of weights assumed.
                double removePropn = StdMath.CumNormal(splitSD);

                // Calculate number to transfer to TempAnimals
                int numToRemove = Convert.ToInt32(Math.Round(numAnimals * removePropn), CultureInfo.InvariantCulture);

                if (numToRemove > 0)
                {
                    DifferenceRecord diffs = new DifferenceRecord() { StdRefWt = srcGroup.NODIFF.StdRefWt, BaseWeight = srcGroup.NODIFF.BaseWeight, FleeceWt = srcGroup.NODIFF.FleeceWt };
                    if (numToRemove < numAnimals)
                    {
                        // This computation gives us the mean live weight of animals which are
                        // lighter than the weight threshold. We are integrating over a truncated
                        // normal distribution, using the differences between successive
                        // evaluations of the CumNormal function
                        double rightSD = -5.0;
                        double stepWidth = (splitSD - rightSD) / NOSTEPS;
                        double removeLW = 0.0;
                        double prevCum = 0.0;
                        for (int idx = 1; idx <= NOSTEPS; idx++)
                        {
                            rightSD = rightSD + stepWidth;
                            double currCum = StdMath.CumNormal(rightSD);
                            removeLW = removeLW + ((currCum - prevCum) * liveWt * (1.0 + varRatio * (rightSD - 0.5 * stepWidth)));
                            prevCum = currCum;
                        }
                        removeLW = removeLW / removePropn;

                        double diffRatio = numAnimals / (numAnimals - numToRemove) * (removeLW / liveWt - 1.0);
                        diffs.BaseWeight = diffRatio * srcGroup.BaseWeight;
                        diffs.StdRefWt = diffRatio * srcGroup.standardReferenceWeight; // Weight diffs within a group are
                        diffs.FleeceWt = diffRatio * srcGroup.FleeceCutWeight;         // assumed genetic!
                    }

                    // Now we have computed Diffs, we split up the animals.
                    var newGroup = srcGroup.Split(numToRemove, false, diffs, srcGroup.NODIFF);
                    Add(newGroup, srcGroup.PaddOccupied, srcGroup.Tag);
                    newGroups.Add(stock.Last()); // Can't add newGroup because it is cloned inside of Add method.
                }
            }

            return newGroups;
        }

        /// <summary>
        /// Split off the young
        /// </summary>
        /// <param name="groups">The animal groups to split.</param>
        /// <returns>The newly created animal groups.</returns>
        public IEnumerable<AnimalGroup> SplitByYoung(IEnumerable<AnimalGroup> groups)
        {
            var allNewGroups = new List<AnimalGroup>();
            foreach (var srcGroup in groups)
            {
                if (srcGroup != null)
                {
                    int numGroupsBeforeAdd = stock.Length;
                    var newGroups = srcGroup.SplitYoung();
                    Add(newGroups, srcGroup.PaddOccupied, srcGroup.Tag);
                    for (int i = numGroupsBeforeAdd; i < stock.Length; i++)
                        allNewGroups.Add(stock[i]);
                }
            }
            return allNewGroups;
        }

        /// <summary>
        /// Sorting is done using the one-offset stock array
        /// </summary>
        public void Sort()
        {
            Array.Sort(stock, new TagComparer());
        }

        /// <summary>A stock tag comparer.</summary>
        private class TagComparer : IComparer<AnimalGroup>
        {
            public int Compare(AnimalGroup x, AnimalGroup y)
            {
                if (x == null)
                    return 0;
                else if (y == null)
                    return 1;
                else
                    return x.Tag.CompareTo(y.Tag);
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
        /// <param name="firstDOY">Start day</param>
        /// <param name="secondDOY">End day</param>
        /// <returns>The days in the interval</returns>
        public int DaysFromDOY365Simple(int firstDOY, int secondDOY)
        {
            if (firstDOY == 0 || secondDOY == 0)
                return 0;
            else if (firstDOY > secondDOY)
                return 365 - firstDOY + secondDOY;
            else
                return secondDOY - firstDOY;
        }

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
        private double[] GetOffspringRates(Genotype parameters, double latitude, int mateDOY, int ageDays, double matingSize, double condition, double chillIndex = 0.0)
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
        public void Add(List<AnimalGroup> animalList, PaddockInfo paddInfo, int tagNo)
        {
            int idx;

            if (animalList != null)
                for (idx = 0; idx <= animalList.Count - 1; idx++)
                {
                    this.Add(animalList[idx], paddInfo, tagNo);
                    animalList[idx] = null;                           // Detach the animal group from the TAnimalList
                }
        }

        /// <summary>
        /// Buy animals.
        /// </summary>
        /// <param name="stockInfo">Information about the animals.</param>
        public void Buy(StockBuy stockInfo)
        {
            var purchaseInfo = new PurchaseInfo
            {
                Genotype = stockInfo.Genotype,
                Number = Math.Max(0, stockInfo.Number),
                AgeDays = Convert.ToInt32(Math.Round(MONTH2DAY * stockInfo.Age), CultureInfo.InvariantCulture),  // Age in months
                LiveWt = stockInfo.Weight,
                GFW = stockInfo.FleeceWt,
                CondScore = stockInfo.CondScore,
                MatedTo = stockInfo.MatedTo,
                Preg = stockInfo.Pregnant,
                Lact = stockInfo.Lactating,
                NYoung = stockInfo.NumYoung,
                YoungWt = stockInfo.YoungWt,
                YoungGFW = stockInfo.YoungFleeceWt
            };
            if (!this.ParseRepro(stockInfo.Sex, ref purchaseInfo.Repro))
                throw new Exception("Event BUY does not support sex='" + stockInfo.Sex + "'");
            if (purchaseInfo.Preg > 0)
                purchaseInfo.NYoung = Math.Max(1, purchaseInfo.NYoung);
            if ((purchaseInfo.Lact == 0) || (purchaseInfo.YoungWt == 0.0))                              // Can't use MISSING as default owing
                purchaseInfo.YoungWt = StdMath.DMISSING;                                                // to double-to-single conversion

            if (purchaseInfo.Number > 0)
            {
                Buy(purchaseInfo);
                if (stockInfo.UseTag > 0)
                    Animals[Count()].Tag = stockInfo.UseTag;
            }
        }
    }
}