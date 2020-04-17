using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Activities
{
    /// <summary>Ruminant feed activity</summary>
    /// <summary>This activity provides food to people from the whole available human food store based on defined nutritional targets</summary>
    /// <version>1.0</version>
    /// <updates>1.0 First implementation of this activity using IAT/NABSA processes</updates>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("This activity performs human feeding based on nutritional quality and desired daily targets. It also includes sales of excess food.")]
    [Version(1, 0, 2, "This version implements the latest approach as outlines in the help system. Suitable for Ethiopian food security project")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Labour/LabourActivityFeedToTargets.htm")]
    public class LabourActivityFeedToTargets: CLEMActivityBase, IValidatableObject
    {
        private Labour people = null;
        private HumanFoodStore food = null;
        private FinanceType bankAccount;

        [Link]
        Clock Clock = null;

        /// <summary>
        /// Feed hired labour as well as household
        /// </summary>
        [Description("Include hired labour")]
        public bool IncludeHiredLabour { get; set; }

        /// <summary>
        /// Daily intake limit
        /// </summary>
        [Description("Daily intake limit")]
        [Units("kg/AE/day")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Daily intake limit required"), GreaterThanValue(0)]
        public double DailyIntakeLimit { get; set; }

        /// <summary>
        /// Daily intake from sources other than modelled in Human Food Store
        /// </summary>
        [Description("Intake from sources not modelled")]
        [Units("kg/AE/day")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Intake from sources not modelled required"), GreaterThanEqualValue(0)]
        public double DailyIntakeOtherSources { get; set; }

        /// <summary>
        /// Undertake managed sales to reserve level
        /// </summary>
        [Description("Sell excess")]
        public bool SellExcess { get; set; }

        /// <summary>
        /// Bank account to use
        /// </summary>
        [Description("Bank account to use")]
        [Models.Core.Display(Type = DisplayType.CLEMResourceName, CLEMResourceNameResourceGroups = new Type[] { typeof(Finance) }, CLEMExtraEntries = new string[] { "Not provided" })]
        public string AccountName { get; set; }

        /// <summary>
        /// Storage reserves to maintain before sales
        /// </summary>
        [Description("Months storage for reserves")]
        [Units("months")]
        [GreaterThanEqualValue(0)]
        public int MonthsStorage { get; set; }

        /// <summary>
        /// Name of market if present.
        /// </summary>
        public Market Market { get; private set; }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            people = Resources.Labour();
            food = Resources.HumanFoodStore();
            bankAccount = Resources.GetResourceItem(this, AccountName, OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.Ignore) as FinanceType;

            Market = FindMarket();
        }

        /// <summary>
        /// Validate component
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            // if finances and not account provided throw error
            if(SellExcess && Resources.GetResourceGroupByType(typeof(Finance)) != null)
            {
                if(bankAccount is null)
                {
                    string[] memberNames = new string[] { "AccountName" };
                    results.Add(new ValidationResult($"A valid bank account must be supplied as sales of excess food is enabled and [r=Finance] resources are available.", memberNames));
                }
            }

            Market market = Apsim.Children(Apsim.Parent(this, typeof(Simulation)), typeof(Market)).FirstOrDefault() as Market;
            if(market != null & bankAccount is null)
            {
                string[] memberNames = new string[] { "AccountName" };
                results.Add(new ValidationResult($"A valid bank account must be supplied for purchases of food from the market used by [a="+this.Name+"].", memberNames));
            }

            // check that at least one target has been provided. 
            if (Apsim.Children(this, typeof(LabourActivityFeedTarget)).Count() == 0)
            {
                string[] memberNames = new string[] { "LabourActivityFeedToTargets" };
                results.Add(new ValidationResult(String.Format("At least one [LabourActivityFeedTarget] component is required below the feed activity [{0}]", this.Name), memberNames));
            }

            // check purchases
            if(Apsim.Children(this, typeof(LabourActivityFeedTargetPurchase)).Cast<LabourActivityFeedTargetPurchase>().Sum(a => a.TargetProportion) != 1)
            {
                string[] memberNames = new string[] { "LabourActivityFeedToTargetPurchases" };
                results.Add(new ValidationResult(String.Format("The sum of all [LabourActivityFeedTargetPurchase] proportions should be 1 for the targeted feed activity [{0}]", this.Name), memberNames));
            }

            return results;
        }

        /// <summary>
        /// Method to determine resources required for this activity in the current month
        /// </summary>
        /// <returns>List of required resource requests</returns>
        public override List<ResourceRequest> GetResourcesNeededForActivity()
        {
            if (people is null | food is null)
            {
                return null;
            }

            List<LabourType> peopleList = people.Items.Where(a => IncludeHiredLabour || a.Hired == false).ToList();
            peopleList.Select(a => a.FeedToTargetIntake == 0);

            // determine AEs to be fed
            double aE = peopleList.Sum(a => a.AdultEquivalent);

            if(aE <= 0)
            {
                return null;
            }

            int daysInMonth = DateTime.DaysInMonth(Clock.Today.Year, Clock.Today.Month);

            // determine feed limits (max kg per AE per day * AEs * days)
            double intakeLimit = DailyIntakeLimit * aE * daysInMonth;

            // remove previous consumption
            double otherIntake = this.DailyIntakeOtherSources * aE * daysInMonth;
            otherIntake += peopleList.Sum(a => a.GetAmountConsumed());

            List<LabourActivityFeedTarget> labourActivityFeedTargets = Apsim.Children(this, typeof(LabourActivityFeedTarget)).Cast<LabourActivityFeedTarget>().ToList();

            // determine targets
            foreach (LabourActivityFeedTarget target in labourActivityFeedTargets)
            {
                // calculate target
                target.Target = target.TargetValue * aE * daysInMonth;

                // set initial level based on off store inputs
                target.CurrentAchieved = target.OtherSourcesValue * aE * daysInMonth;

                // calculate current level from previous intake this month (LabourActivityFeed)
                target.CurrentAchieved += people.GetDietaryValue(target.Metric, IncludeHiredLabour, true) * aE * daysInMonth;

                // add sources outside of this activity to peoples' diets
                if (target.OtherSourcesValue > 0)
                {
                    foreach (var person in peopleList)
                    {
                        LabourDietComponent outsideEat = new LabourDietComponent();
                        outsideEat.AddOtherSource(target.Metric, target.OtherSourcesValue * person.AdultEquivalent * daysInMonth);
                        person.AddIntake(outsideEat);
                    }
                }
            }

            // get max months before spoiling of all food stored (will be zero for non perishable food)
            int maxFoodAge = Apsim.Children(food, typeof(HumanFoodStoreType)).Cast<HumanFoodStoreType>().Max(a => a.Pools.Select(b => a.UseByAge - b.Age).DefaultIfEmpty(0).Max());

            // create list of all food parcels
            List<HumanFoodParcel> foodParcels = new List<HumanFoodParcel>();

            foreach (HumanFoodStoreType foodStore in Apsim.Children(food, typeof(HumanFoodStoreType)).Cast<HumanFoodStoreType>().ToList())
            {
                foreach (HumanFoodStorePool pool in foodStore.Pools)
                {
                    foodParcels.Add(new HumanFoodParcel()
                    {
                        FoodStore = foodStore,
                        Pool = pool,
                        Expires = ((foodStore.UseByAge == 0) ? maxFoodAge+1: foodStore.UseByAge - pool.Age)
                    });
                }
            }

            foodParcels = foodParcels.OrderBy(a => a.Expires).ToList();

            // if a market exists add the available market produce to the list below that ordered above.
            // order market food by price ascending
            // this will include market available food in the decisions.
            // will need to purchase this food before taking it if cost associated.
            // We can check if the parent of the human food store used is a market and charge accordingly.

            // for each market
            List<HumanFoodParcel> marketFoodParcels = new List<HumanFoodParcel>();
            foreach (Market market in Apsim.Children(Apsim.Parent(this, typeof(Simulation)), typeof(Market)).Cast<Market>().ToList())
            {
                ResourcesHolder resources = Apsim.Child(market, typeof(ResourcesHolder)) as ResourcesHolder;
                if (resources != null)
                {
                    HumanFoodStore food = resources.HumanFoodStore();
                    if (food != null)
                    {
                        foreach (HumanFoodStoreType foodStore in Apsim.Children(food, typeof(HumanFoodStoreType)).Cast<HumanFoodStoreType>().ToList())
                        {
                            foreach (HumanFoodStorePool pool in foodStore.Pools)
                            {
                                marketFoodParcels.Add(new HumanFoodParcel()
                                {
                                    FoodStore = foodStore,
                                    Pool = pool,
                                    Expires = ((foodStore.UseByAge == 0) ? maxFoodAge + 1 : foodStore.UseByAge - pool.Age)
                                });
                            }
                        }
                    }
                }
            }
            foodParcels.AddRange(marketFoodParcels.OrderBy(a => a.FoodStore.Price(PurchaseOrSalePricingStyleType.Purchase).PricePerPacket));

            double fundsAvailable = double.PositiveInfinity;
            if (bankAccount != null)
            {
                fundsAvailable = bankAccount.FundsAvailable;
            }

            int parcelIndex = 0;
            double intake = otherIntake;
            // start eating food from list from that about to expire first
            while(parcelIndex < foodParcels.Count)
            {
                foodParcels[parcelIndex].Proportion = 0;
                if (intake < intakeLimit & (labourActivityFeedTargets.Where(a => !a.TargetMet).Count() > 0 | foodParcels[parcelIndex].Expires == 0))
                {
                    // still able to eat and target not met or food about to expire this timestep
                    // reduce by amout that can be eaten
                    double propCanBeEaten = Math.Min(1, (intakeLimit - intake) / (foodParcels[parcelIndex].FoodStore.EdibleProportion * foodParcels[parcelIndex].Pool.Amount));
                    // reduce to target limits
                    double propToTarget = 1;
                    if (foodParcels[parcelIndex].Expires != 0)
                    {
                        // if the food is not going to spoil
                        // then adjust what can be eaten up to target otherwise allow over target consumption to avoid waste

                        LabourActivityFeedTarget targetUnfilled = labourActivityFeedTargets.Where(a => !a.TargetMet).FirstOrDefault();
                        if (targetUnfilled != null)
                        {
                            // calculate reduction to metric target
                            double metricneeded = Math.Max(0, targetUnfilled.Target - targetUnfilled.CurrentAchieved);
                            double amountneeded = metricneeded / foodParcels[parcelIndex].FoodStore.ConversionFactor(targetUnfilled.Metric);

                            propToTarget = Math.Min(1, amountneeded / (foodParcels[parcelIndex].FoodStore.EdibleProportion * foodParcels[parcelIndex].Pool.Amount));
                        }
                    }

                    foodParcels[parcelIndex].Proportion = Math.Min(propCanBeEaten, propToTarget);

                    // work out if there will be a cost limitation, only if a price structure exists for the resource
                    double propToPrice = 1;
                    if (foodParcels[parcelIndex].FoodStore.PricingExists(PurchaseOrSalePricingStyleType.Purchase))
                    {
                        ResourcePricing price = foodParcels[parcelIndex].FoodStore.Price(PurchaseOrSalePricingStyleType.Purchase);
                        double cost = (foodParcels[parcelIndex].Pool.Amount * foodParcels[parcelIndex].Proportion) / price.PacketSize * price.PricePerPacket;
                        if (cost > 0)
                        {
                            propToPrice = Math.Min(1, fundsAvailable / cost);
                            // remove cost from running check tally
                            fundsAvailable = Math.Max(0, fundsAvailable - (cost * propToPrice));

                            // real finance transactions will happen in the do activity as stuff is allocated
                            // there should not be shortfall as all the checks and reductions have happened here
                        }
                    }
                    foodParcels[parcelIndex].Proportion *= propToPrice;

                    // update intake
                    double newIntake = (foodParcels[parcelIndex].FoodStore.EdibleProportion * foodParcels[parcelIndex].Pool.Amount * foodParcels[parcelIndex].Proportion);
                    intake += newIntake;
                    // update metrics
                    foreach (LabourActivityFeedTarget target in labourActivityFeedTargets)
                    {
                        target.CurrentAchieved += newIntake * foodParcels[parcelIndex].FoodStore.ConversionFactor(target.Metric);
                    }
                }
                else if (intake >= intakeLimit && labourActivityFeedTargets.Where(a => !a.TargetMet).Count() > 1)
                {
                    // full but could still reach target with some substitution
                    // but can substitute to remove a previous target

                    // does the current parcel have better target values than any previous non age 0 pool of a different food type

                }
                else
                {
                    break;
                }
                parcelIndex++;
            }

            // fill resource requests
            List<ResourceRequest> requests = new List<ResourceRequest>();
            foreach (var item in foodParcels.GroupBy(a => a.FoodStore))
            {
                double amount = item.Sum(a => a.Pool.Amount * a.Proportion);
                if (amount > 0)
                {
                    double financeLimit = 1;
                    // if obtained from the market make financial transaction before taking
                    ResourcePricing price = item.Key.Price(PurchaseOrSalePricingStyleType.Sale);
                    if (bankAccount != null && item.Key.Parent.Parent.Parent == Market && price.PricePerPacket > 0)
                    {
                        // if shortfall reduce purchase
                        ResourceRequest marketRequest = new ResourceRequest
                        {
                            ActivityModel = this,
                            Required = amount / price.PacketSize * price.PricePerPacket,
                            AllowTransmutation = false,
                            Reason = "Food purchase",
                            MarketTransactionMultiplier = 1
                        };
                        bankAccount.Remove(marketRequest);
                    }

                    requests.Add(new ResourceRequest()
                    {
                        Resource = item.Key,
                        ResourceType = typeof(HumanFoodStore),
                        AllowTransmutation = false,
                        Required = amount * financeLimit,
                        ResourceTypeName = item.Key.Name,
                        ActivityModel = this,
                        Reason = "Consumption"
                    });
                }
            }

            // if still hungry and funds available, try buy food in excess of what stores (private or market) offered using transmutation if present.
            // This will force the market or private sources to purchase more food to meet demand if transmutation available.
            // if no market is present it will look to transmutating from its own stores if possible.
            // this means that other than a purchase from market (above) this activity doesn't need to worry about financial tranactions.
            if (intake < intakeLimit && (labourActivityFeedTargets.Where(a => !a.TargetMet).Count() > 0) && fundsAvailable > 0)
            {
                ResourcesHolder resourcesHolder = Resources;
                // if market is present point to market to find the resource
                if (Market != null)
                {
                    resourcesHolder = Apsim.Child(Market, typeof(ResourcesHolder)) as ResourcesHolder;
                }

                // don't worry about money anymore. The over request will be handled by the transmutation.
                // move through specified purchase list
                foreach (LabourActivityFeedTargetPurchase purchase in Apsim.Children(this, typeof(LabourActivityFeedTargetPurchase)).Cast<LabourActivityFeedTargetPurchase>().ToList())
                {
                    HumanFoodStoreType foodtype = resourcesHolder.GetResourceItem(this, purchase.FoodStoreName, OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.Ignore) as HumanFoodStoreType;
                    if (foodtype != null && (foodtype.TransmutationDefined & intake < intakeLimit))
                    {
                        LabourActivityFeedTarget targetUnfilled = labourActivityFeedTargets.Where(a => !a.TargetMet).FirstOrDefault();
                        if (targetUnfilled != null)
                        {
                            // calculate reduction to metric target
                            double metricneeded = Math.Max(0, (targetUnfilled.Target - targetUnfilled.CurrentAchieved));
                            double amountneeded = metricneeded / foodtype.ConversionFactor(targetUnfilled.Metric);

                            if(intake + amountneeded > intakeLimit)
                            {
                                amountneeded = intakeLimit - intake;
                            }
                            double amountfood = amountneeded / foodtype.EdibleProportion;

                            // update intake
                            intake += amountfood;

                            // find in requests or create a new one
                            ResourceRequest foodRequestFound = requests.Find(a => a.Resource == foodtype) as ResourceRequest;
                            if (foodRequestFound is null)
                            {
                                requests.Add(new ResourceRequest()
                                {
                                    Resource = foodtype,
                                    ResourceType = typeof(HumanFoodStore),
                                    AllowTransmutation = true,
                                    Required = amountfood,
                                    ResourceTypeName = purchase.FoodStoreName.Split('.')[1],
                                    ActivityModel = this,
                                    Reason = "Consumption"
                                });
                            }
                            else
                            {
                                foodRequestFound.Required += amountneeded;
                                foodRequestFound.AllowTransmutation = true;
                            }
                        }
                    }
                }
                // NOTE: proportions of purchased food are not modified if the sum does not add up to 1 or some of the food types are not available. 
            }

            return requests;
        }

        /// <summary>
        /// Determines how much labour is required from this activity based on the requirement provided
        /// </summary>
        /// <param name="requirement">The details of how labour are to be provided</param>
        /// <returns></returns>
        public override double GetDaysLabourRequired(LabourRequirement requirement)
        {
            List<LabourType> group = Resources.Labour().Items.Where(a => a.Hired != true).ToList();
            int head = 0;
            double adultEquivalents = 0;
            foreach (Model child in Apsim.Children(this, typeof(LabourFeedGroup)))
            {
                var subgroup = group.Filter(child).ToList();
                head += subgroup.Count();
                adultEquivalents += subgroup.Sum(a => a.AdultEquivalent);
            }

            double daysNeeded = 0;
            double numberUnits = 0;
            switch (requirement.UnitType)
            {
                case LabourUnitType.Fixed:
                    daysNeeded = requirement.LabourPerUnit;
                    break;
                case LabourUnitType.perHead:
                    numberUnits = head / requirement.UnitSize;
                    if (requirement.WholeUnitBlocks)
                    {
                        numberUnits = Math.Ceiling(numberUnits);
                    }

                    daysNeeded = numberUnits * requirement.LabourPerUnit;
                    break;
                case LabourUnitType.perAE:
                    numberUnits = adultEquivalents / requirement.UnitSize;
                    if (requirement.WholeUnitBlocks)
                    {
                        numberUnits = Math.Ceiling(numberUnits);
                    }

                    daysNeeded = numberUnits * requirement.LabourPerUnit;
                    break;
                default:
                    throw new Exception(String.Format("LabourUnitType {0} is not supported for {1} in {2}", requirement.UnitType, requirement.Name, this.Name));
            }
            return daysNeeded;
        }

        /// <summary>
        /// The method allows the activity to adjust resources requested based on shortfalls (e.g. labour) before they are taken from the pools
        /// </summary>
        public override void AdjustResourcesNeededForActivity()
        {
            if(LabourLimitProportion < 1)
            {
                foreach (ResourceRequest item in ResourceRequestList)
                {
                    if(item.ResourceType != typeof(LabourType))
                    {
                        item.Provided *= LabourLimitProportion;
                    }
                }
            }

            return;
        }

        /// <summary>
        /// Method used to perform activity if it can occur as soon as resources are available.
        /// </summary>
        public override void DoActivity()
        {
            // add all provided requests to the individuals intake pools.

            List<LabourType> group = Resources.Labour().Items.Where(a => IncludeHiredLabour | a.Hired != true).ToList();
            double aE = group.Sum(a => a.AdultEquivalent);
            Status = ActivityStatus.NotNeeded;
            if (group != null && group.Count > 0)
            {
                var requests = ResourceRequestList.Where(a => a.ResourceType == typeof(HumanFoodStore));
                if (requests.Count() > 0)
                {
                    foreach (ResourceRequest request in requests)
                    {
                        if (request.Provided > 0)
                        {
                            // add to individual intake
                            foreach (LabourType labour in group)
                            {
                                labour.AddIntake(new LabourDietComponent()
                                {
                                    AmountConsumed = request.Provided * (labour.AdultEquivalent / aE),
                                    FoodStore = request.Resource as HumanFoodStoreType
                                });
                            }
                        }
                    }
                }
                List<LabourActivityFeedTarget> labourActivityFeedTargets = Apsim.Children(this, typeof(LabourActivityFeedTarget)).Cast<LabourActivityFeedTarget>().ToList();
                if (labourActivityFeedTargets.Where(a => !a.TargetMet).Count() > 0)
                {
                    this.Status = ActivityStatus.Partial;
                }
                else
                {
                    this.Status = ActivityStatus.Success;
                }
            }
            
        }

        /// <summary>An event handler to allow us to sell excess resources.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMAnimalSell")]
        private void OnCLEMAnimalSell(object sender, EventArgs e)
        {
            // Sell excess above store reserve level calculated from AE and daily target of first feed target
            // Performed here so all activities have access to human food stores before being sold.

            if (SellExcess && TimingOK)
            {
                // only uses the first target metric as measure
                double[] stored = new double[MonthsStorage + 1];
                double[] target = new double[MonthsStorage + 1];
                int[] daysInMonth = new int[MonthsStorage + 1];

                // determine AEs to be fed - NOTE does not account ofr aging in reserve calcualtion
                double aE = people.Items.Where(a => IncludeHiredLabour || a.Hired == false).Sum(a => a.AdultEquivalent);

                LabourActivityFeedTarget feedTarget = Apsim.Children(this, typeof(LabourActivityFeedTarget)).FirstOrDefault() as LabourActivityFeedTarget;

                for (int i = 1; i <= MonthsStorage; i++)
                {
                    DateTime month = Clock.Today.AddMonths(i);
                    daysInMonth[i] = DateTime.DaysInMonth(month.Year, month.Month);
                    target[i] = daysInMonth[i] * aE * feedTarget.TargetValue;
                }

                foreach (HumanFoodStoreType foodStore in Apsim.Children(food, typeof(HumanFoodStoreType)).Cast<HumanFoodStoreType>().ToList())
                {
                    double amountStored = 0;
                    double amountAvailable = foodStore.Pools.Sum(a => a.Amount);

                    if (amountAvailable > 0)
                    {
                        foreach (HumanFoodStorePool pool in foodStore.Pools.OrderBy(a => ((foodStore.UseByAge == 0) ? MonthsStorage : a.Age)))
                        {
                            if (foodStore.UseByAge != 0 && pool.Age == foodStore.UseByAge)
                            {
                                // don't sell food expiring this month as spoiled
                                amountStored += pool.Amount;
                            }
                            else
                            {
                                int currentMonth = ((foodStore.UseByAge == 0) ? MonthsStorage : foodStore.UseByAge - pool.Age + 1);
                                double poolRemaining = pool.Amount;
                                while (currentMonth > 0)
                                {
                                    if (stored[currentMonth] < target[currentMonth])
                                    {
                                        // place amount in store
                                        double amountNeeded = target[currentMonth] - stored[currentMonth];
                                        double towardTarget = pool.Amount * foodStore.EdibleProportion * foodStore.ConversionFactor(feedTarget.Metric);
                                        double amountSupplied = Math.Min(towardTarget, amountNeeded);
                                        double proportionProvided = amountSupplied / towardTarget;

                                        amountStored += pool.Amount * proportionProvided;
                                        poolRemaining -= pool.Amount * proportionProvided;
                                        stored[currentMonth] += amountSupplied;

                                        if (poolRemaining <= 0)
                                        {
                                            break;
                                        }
                                    }
                                    currentMonth--;
                                }
                            }
                        }

                        double amountSold = amountAvailable - amountStored;
                        if (amountSold > 0)
                        {
                            ResourcePricing priceToUse = new ResourcePricing()
                            {
                                PacketSize = 1
                            }; 
                            if(foodStore.PricingExists(PurchaseOrSalePricingStyleType.Purchase))
                            {
                                priceToUse = foodStore.Price(PurchaseOrSalePricingStyleType.Purchase);
                            }

                            double units = amountSold / priceToUse.PacketSize;
                            if (priceToUse.UseWholePackets)
                            {
                                units = Math.Truncate(units);
                            }
                            // remove resource
                            ResourceRequest purchaseRequest = new ResourceRequest
                            {
                                ActivityModel = this,
                                Required = units * priceToUse.PacketSize,
                                AllowTransmutation = false,
                                Reason = "Sell excess",
                                MarketTransactionMultiplier = 1
                            };
                            foodStore.Remove(purchaseRequest);

                            // transfer money earned
                            if (bankAccount != null)
                            {
                                bankAccount.Add(units * priceToUse.PricePerPacket, this, $"Sales {foodStore.Name}");
                            }
                        } 
                    }
                }
            }
        }

        /// <summary>
        /// Method to determine resources required for initialisation of this activity
        /// </summary>
        /// <returns></returns>
        public override List<ResourceRequest> GetResourcesNeededForinitialisation()
        {
            return null;
        }

        /// <summary>
        /// Resource shortfall event handler
        /// </summary>
        public override event EventHandler ResourceShortfallOccurred;

        /// <summary>
        /// Shortfall occurred 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnShortfallOccurred(EventArgs e)
        {
            ResourceShortfallOccurred?.Invoke(this, e);
        }

        /// <summary>
        /// Resource shortfall occured event handler
        /// </summary>
        public override event EventHandler ActivityPerformed;

        /// <summary>
        /// Shortfall occurred 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnActivityPerformed(EventArgs e)
        {
            ActivityPerformed?.Invoke(this, e);
        }

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = "";
            html += "<div class=\"activityentry\">";
            html += "Each Adult Equivalent is able to consume ";
            if(DailyIntakeLimit > 0)
            {
                html += "<span class=\"setvalue\">";
                html += DailyIntakeLimit.ToString("#,##0.##");
            }
            else
            {
                html += "<span class=\"errorlink\">NOT SET";
            }
            html += "</span> kg per day";
            if(DailyIntakeOtherSources > 0)
            {
                html += "with <span class=\"setvalue\">";
                html += DailyIntakeOtherSources.ToString("#,##0.##");
                html += "</span> provided from non-modelled sources";
            }
            html += "</div>";
            html += "<div class=\"activityentry\">";
            html += "Hired labour <span class=\"setvalue\">" + ((IncludeHiredLabour) ? "is" : "is not") + "</span> included";
            html += "</div>";


            Market marketPlace = FindMarket();
            if (marketPlace != null)
            {
                html += "<div class=\"activityentry\">";
                html += "Food with be bought and sold through the market <span class=\"setvalue\">"+marketPlace.Name+"</span>";
                html += "</div>";
            }

            return html;
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryInnerClosingTags(bool formatForParentControl)
        {
            string html = "";
            html += "\n</div>";
            return html;
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryInnerOpeningTags(bool formatForParentControl)
        {
            string html = "";
            html += "\n<div class=\"croprotationborder\">";
            html += "<div class=\"croprotationlabel\">The following targets and purchases will be used:</div>";

            if (Apsim.Children(this, typeof(LabourActivityFeedTarget)).Count() == 0)
            {
                html += "\n<div class=\"errorbanner clearfix\">";
                html += "<div class=\"filtererror\">No Feed To Target component provided</div>";
                html += "</div>";
            }

            if (Apsim.Children(this, typeof(LabourActivityFeedTargetPurchase)).Count() == 0)
            {
                html += "\n<div class=\"errorbanner clearfix\">";
                html += "<div class=\"filtererror\">No food items will be purchased above what is currently available</div>";
                html += "</div>";
            }

            return html;
        }
    }
}
