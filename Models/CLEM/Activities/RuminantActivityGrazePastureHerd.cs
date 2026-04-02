using APSIM.Numerics;
using Docker.DotNet.Models;
using DocumentFormat.OpenXml.Office.CustomUI;
using Models.CLEM.Groupings;
using Models.CLEM.Interfaces;
using Models.CLEM.Reporting;
using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using StdUnits;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.Design.Serialization;
using System.Linq;

namespace Models.CLEM.Activities
{
    /// <summary>Ruminant grazing activity</summary>
    /// <summary>Specific version where pasture and breed is specified</summary>
    /// <summary>This activity determines how a ruminant breed will graze on a particular pasture (GrazeFoodStoreType)</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("Performs grazing of a specified herd and pasture (paddock)")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Ruminant/RuminantGraze.htm")]
    [ModelAssociations(associatedModels: new Type[] { typeof(RuminantParametersGrazing) }, associationStyles: new ModelAssociationStyle[] { ModelAssociationStyle.DescendentOfRuminantType })]
    public class RuminantActivityGrazePastureHerd : CLEMRuminantActivityBase, IValidatableObject
    {
        [Link]
        private CLEMEvents events = null;
        [JsonIgnore]
        private DateTime lastResourceRequest = new();
        //private double totalPastureRequired = 0;
        //private double totalPastureDesired = 0;
        [JsonIgnore]
        private ResourceRequest pastureRequest = null;
        private double shortfallReportingCutoff = 0.01;
        private readonly bool isStandAloneModel = true;
        private bool usingGrowPF = false;
        private readonly string shortHerdName = "";
        IEnumerable<Ruminant> herdToFeed = null;

        /// <summary>
        /// Number of hours grazed
        /// Based on 8 hour grazing days
        /// Could be modified to account for rain/heat walking to water etc.
        /// </summary>
        [Description("Number of hours grazed (based on 8 hr grazing day)")]
        [Required, Range(0, 8, ErrorMessage = "Value based on maximum 8 hour grazing day"), GreaterThanValue(0)]
        public double HoursGrazed { get; set; }

        /// <summary>
        /// Paddock or pasture to graze
        /// </summary>
        [Description("GrazeFoodStore/pasture to graze")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Graze Food Store/pasture required")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { typeof(GrazeFoodStore) } })]
        public string GrazeFoodStoreTypeName { get; set; }

        /// <summary>
        /// paddock or pasture to graze
        /// </summary>
        [JsonIgnore]
        public IGrazeFoodStoreType GrazeFoodStoreModel { get; set; }

        /// <summary>
        /// Ruminant group to graze
        /// </summary>
        [Description("Ruminant type to graze")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Ruminant Type required")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { typeof(RuminantHerd) } })]
        public string RuminantTypeName { get; set; }

        /// <summary>
        /// Ruminant group to graze
        /// </summary>
        [JsonIgnore]
        public RuminantType RuminantTypeModel { get; set; }

        /// <summary>
        /// The proportion of required graze that is available determined from parent activity arbitration
        /// </summary>
        [JsonIgnore]
        public double GrazingCompetitionLimiter { get; set; } = 1.0;

        /// <summary>
        /// The biomass of pasture per hectare at start of allocation
        /// </summary>
        [JsonIgnore]
        public double BiomassPerHectare { get; set; }

        // ToDo: don't allow link unless using GrowPF. Therefore we don't need the limiter values.

        ///// <summary>
        ///// Potential intake limiter based on pasture quality
        ///// </summary>
        //[JsonIgnore]
        //public double PotentialIntakePastureQualityLimiter { get; set; }

        /// <summary>
        /// Potential intake limiter based on the biomass of available pasture
        /// </summary>
        [JsonIgnore]
        public double PotentialIntakePastureBiomassLimiter { get; set; }

        /// <summary>
        /// Potential intake limiter based on the proportion of 8 hours grazing allowed
        /// </summary>
        [JsonIgnore]
        public double PotentialIntakeGrazingTimeLimiter { get; set; }

        /// <summary>
        /// Potential intake limit including low biomass, pasture quality and time limited
        /// </summary>
        [JsonIgnore]
        public double PotentialIntakeLimit
        {
            get { return PotentialIntakePastureBiomassLimiter * PotentialIntakeGrazingTimeLimiter; } // PotentialIntakePastureQualityLimiter * 
        }

        /// <summary>
        /// A pasture packet used to track the quality of the mixed pasture pools eaten.
        /// </summary>
        [JsonIgnore]
        public FoodResourcePacket ConsumedPasturePoolsPacket { get; set; } = new();

        /// <summary>
        /// Proportion of intake that can be taken from each pool
        /// </summary>
        [JsonIgnore]
        public List<GrazeBreedPoolLimit> PoolFeedLimits { get; set; }

        /// <summary>
        /// The total biomass of pasture required by the herd (kg)
        /// </summary>
        public double PastureRequired { get; set; }

        ///// <summary>
        ///// The biomass of pasture desired by the herd (kg). Does not include biomass limiter.
        ///// </summary>
        //public double PastureDesired { get; set; }

        /// <summary>
        /// The daily biomass of pasture required by the herd (kg)
        /// </summary>
        public double DailyPastureRequired { get; set; }


        /// <summary>
        /// Default constructor
        /// </summary>
        public RuminantActivityGrazePastureHerd()
        {
        }

        /// <summary>
        /// Constructor using details from a GrazeAll activity
        /// </summary>
        public RuminantActivityGrazePastureHerd(RuminantActivityGrazePasture grazePasture, RuminantType herdType, string transactionCategory, Guid parentBasedUid)
        {
            shortHerdName = herdType.Name;
            RuminantTypeName = herdType.NameWithParent;
            GrazeFoodStoreTypeName = grazePasture.GrazeFoodStoreTypeName;
            HoursGrazed = grazePasture.HoursGrazed;
            Parent = grazePasture;
            Name = $"Graze_{grazePasture.GrazeFoodStoreModel.Name}_{herdType.Name}";
            OnPartialResourcesAvailableAction = grazePasture.OnPartialResourcesAvailableAction;
            TransactionCategory = transactionCategory;
            Status = ActivityStatus.NoTask;
            UniqueID = parentBasedUid;
            isStandAloneModel = false;
            Resources = grazePasture.Resources;
        }

        /// <summary>
        /// Add required children to this activity after the activity is fully created
        /// </summary>
        public void AddHerdLocationFilter()
        {
            // add ruminant activity filter group to ensure correct individuals are selected
            string location = GrazeFoodStoreModel.Name;
            if (location.Contains('.'))
            {
                location = location.Split('.')[1];
            }

            RuminantActivityGroup herdGroup = new()
            {
                Name = $"Filter_{location}_{shortHerdName}"
            };
            Structure.AddChild(herdGroup);

            herdGroup.Structure.AddChild(new FilterByProperty()
            {
                PropertyOfIndividual = "Location",
                Operator = System.Linq.Expressions.ExpressionType.Equal,
                Value = location,
                Parent = herdGroup,
                Name = "GrazeLocation"
            });
            herdGroup.Structure.AddChild(new FilterByProperty()
            {
                PropertyOfIndividual = "HerdName",
                Operator = System.Linq.Expressions.ExpressionType.Equal,
                Value = shortHerdName,
                Parent = herdGroup,
                Name = "GrazeHerd"
            });

            Links links = new();
            links.Resolve(herdGroup as IModel, true, recurse: false);
            // commencing event needed to wire up filter group
            var events = new Events(herdGroup);
            events.PublishToModelAndChildren("Commencing", new object[] { herdGroup, new EventArgs() });
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            GrazeFoodStoreModel = Resources.FindResourceType<GrazeFoodStore, IGrazeFoodStoreType>(this, GrazeFoodStoreTypeName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop);
            RuminantTypeModel = Resources.FindResourceType<RuminantHerd, RuminantType>(this, RuminantTypeName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop);

            usingGrowPF = Structure.Find<RuminantActivityGrowPF>()?.Enabled ?? false;

            shortfallReportingCutoff = Structure.Find<ReportResourceShortfalls>()?.PropPastureShortfallOfDesiredIntake ?? 0.02;

            HerdResource = Structure.Find<RuminantHerd>();

            AddHerdLocationFilter();

            InitialiseHerd(true, false);
        }

        /// <summary>An event handler to allow us to clear requests at start of month.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMStartOfTimeStep")]
        private void OnStartOfTimeStep(object sender, EventArgs e)
        {
            ResourceRequestList = null;
            PoolFeedLimits.Clear(); // = null;
            //TODO: add local hoursGrazed that is reset to user level each time step but can be reduced by other activities such as RuminantActivityMove, or ManageRuminants
            // this actually needs to be a property of each individual as we don't know the how management of an individual affects grazing time.
            // grazing competition should probably favour individuals with the lowest grazing time. maybe. 
            PotentialIntakeGrazingTimeLimiter = HoursGrazed / 8;

            PastureRequired = 0;
            DailyPastureRequired = 0;
            Status = ActivityStatus.NotNeeded;
            PotentialIntakePastureBiomassLimiter = 1;
        }

        /// <summary>
        /// Calculate the potential intake limiter based on pasture quality.
        /// </summary>
        /// <returns>Limiter as proportion</returns>
        public double CalculatePotentialIntakePastureQualityLimiter()
        {
            //// GrowPF (Dougherty 2025 and Frier 2012) will do the feed quality adjustment in greater detail (Intake.AdjustByFeedQuality)
            //if (usingGrowPF) return 1;

            //if (GrazeFoodStoreModel is GrazeFoodStoreType grazeFoodStore)
            //{
            //    // determine pasture quality from all pools (DMD) at start of grazing
            //    // ToDo: need to get this from IGrazeFoodStore where this interface is on GrazeFoodStoreType and GrazeFoodStoreAPSIMLink 
            //    double pastureDMD = grazeFoodStore.SwardDryMatterDigestibility;

            //    // Reduce potential intake based on pasture quality for the proportion consumed (zero legume).
            //    // TODO: check that this doesn't need to be performed for each breed based on how pasture taken
            //    // this will still occur when grazing on improved, irrigated or crops.
            //    // CLEM does not allow grazing on two pastures in the time step (i.e. month), whereas NABSA allowed irrigated pasture and supplemented with native for remainder needed.
            //    return 1 - Math.Max(0.0, grazeFoodStore.IntakeQualityCoefficient * (0.8 - grazeFoodStore.IntakeTropicalQualityCoefficient - pastureDMD / 100));
            //}
            return 1;
        }

        /// <summary>
        /// Method to allow another activity to request the activity determines its resources
        /// </summary>
        public List<ResourceRequest> RequestDetermineResources()
        {
            return RequestResourcesForTimestep();
        }

        /// <inheritdoc/>
        public override List<ResourceRequest> RequestResourcesForTimestep(double argument = 0)
        {
            // if this is the first time of this request. (a) called manually from a RuminantActivityGrazePasture, or (b) called from this activity OnGetResources event.
            if (lastResourceRequest == events.Clock.Today)
                return null;

            CalculateFeedRequirement();

            // Stand alone model is true unless dynamically created by parent graze paddock
            if (isStandAloneModel && GrazeFoodStoreModel is GrazeFoodStoreType)
            {
                SetupPoolsAndLimits(1.0);
            }
            //if (isStandAloneModel && !usingGrowPF)
            //{
            //    // TODO: this needs to be based on what is eaten, not to total pasture which may not represent the intake in many cases.
            //    PotentialIntakePastureQualityLimiter = CalculatePotentialIntakePastureQualityLimiter();
            //}


            if (GrazeFoodStoreModel is GrazeFoodStoreType)
            {
                ConsumedPasturePoolsPacket.Reset();
            }

            if (MathUtilities.IsPositive(PastureRequired))
            {
                GrazeFoodStoreModel.SetCurrentBiomass();
                pastureRequest = new ResourceRequest()
                {
                    AllowTransmutation = false,
                    Required = PastureRequired,
                    Resource = GrazeFoodStoreModel,
                    ResourceType = typeof(GrazeFoodStore),
                    ResourceTypeName = GrazeFoodStoreModel.Name,
                    ActivityModel = this,
                    AdditionalDetails = this,
                    Category = "Consumed",
                    RelatesToResource = PredictedHerdNameToDisplay
                };
                ResourceRequestList.Add(pastureRequest);
            }

            GrazeFoodStoreModel.CurrentGrazingRequest = pastureRequest;
            lastResourceRequest = events.Clock.Today;
            return ResourceRequestList;
        }

        /// <summary>
        /// Reset stores for start of time step
        /// </summary>
        public double CalculateFeedRequirement()
        {
            ResourceRequestList = new List<ResourceRequest>();
            pastureRequest = null;
            // as the grazing activity has added a dynamic filter group (location and herd name) we do not need to specifying the herd ot paddock here.
            herdToFeed = GetIndividuals<Ruminant>(GetRuminantHerdSelectionStyle.AllOnFarm);

            if (herdToFeed.Any() == false)
                return 0;

            // CLEM concept not included in AgPasture - reduce intake when pasture biomass becomes low to account for greater search time
            PotentialIntakePastureBiomassLimiter = 1 - Math.Round(Math.Exp(-herdToFeed.FirstOrDefault().Parameters.Grazing.IntakeCoefficientBiomass * GrazeFoodStoreModel.TonnesPerHectareStartOfTimeStep * 1000), 5);

            foreach (Ruminant ind in herdToFeed)
            {
                PastureRequired += Math.Min(ind.Intake.SolidsDaily.RequiredForTimeStep(events.Interval), ind.Intake.SolidsDaily.ExpectedForTimeStep(events.Interval) * PotentialIntakeLimit);
                DailyPastureRequired += Math.Min(ind.Intake.SolidsDaily.Required, ind.Intake.SolidsDaily.Expected * PotentialIntakeLimit);
            }
            return PastureRequired;
        }

        /// <inheritdoc/>
        protected override void AdjustResourcesForTimestep()
        {
            //if (pastureRequest != null && MathUtilities.IsLessThan(Math.Round(GrazingCompetitionLimiter,4), 1))
            //{
            //    // reduce the amount provided by the grazing competition limiter
            //    // accounts for reduction based on other herds of ruminants in the paddock adn insufficient feed available  
            //    ResourceRequestList.Where(a => a.Resource is IGrazeFoodStoreType).FirstOrDefault().Required *= GrazingCompetitionLimiter;
            //}

            if (GrazeFoodStoreModel is GrazeFoodStoreAPSIMLink apsimLink)
            {
                // call here to describe diet before feeding in PerformTasksForTimeStep
                apsimLink.SetDailyForageTakeFromGrazing(pastureRequest);
            }
        }

        /// <inheritdoc/>
        public override void PerformTasksForTimestep(double argument = 0)
        {
            // Go through amount received and put it into the animals intake with quality measures.
            // needs to be done here as the GrowPF activity will determine any intake quality reductions handled in OnAfterDietQualityDetermined

            if (MathUtilities.IsLessThanOrEqual(PastureRequired, 0.0))
                return;

            // shortfall already takes into account competition (AdjustResourcesForActivity) and availability
            //double shortfall = (pastureRequest?.Provided??0) / totalPastureRequired;

            foreach (Ruminant ind in herdToFeed)
            {
                double eaten = Math.Min(ind.Intake.SolidsDaily.Required, ind.Intake.SolidsDaily.Expected * PotentialIntakeLimit * GrazingCompetitionLimiter);

                if (GrazeFoodStoreModel is GrazeFoodStoreAPSIMLink grazePaddock)
                {
                    grazePaddock.TimeStepForageConsumed.Amount = eaten;
                    ind.Intake.AddFeed(grazePaddock.TimeStepForageConsumed);
                }
                else
                {
                    ConsumedPasturePoolsPacket.Amount = eaten;
                    ind.Intake.AddFeed(ConsumedPasturePoolsPacket);
                }

                // todo: set grazing energy requirement
                // todo: set movement energy requirement
            }
        }

        /// <summary>An event handler to allow final adjustment of intake based on diet quality and ensure this is accounted for in forage take.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMPostRuminantConsumption")]
        private void OnAfterDietQualityDetermined(object sender, EventArgs e)
        {
            if (pastureRequest is null)
                return;

            // calculate if any of the feed wasn't eaten due to intake quality reduction or insufficient rumen digestible protein provided.
            double notNeeded = 0;
            if (pastureRequest.Required > 0)
            {
                notNeeded = herdToFeed.Sum(a => a.Intake.SolidsDaily.Unneeded) / pastureRequest.Required;
            }

            if (notNeeded > 0)
                GrazeFoodStoreModel.ApplyDailyIntakeReduction(Math.Min(1.0, notNeeded));

            GrazeFoodStoreModel.ReportGrazingTransaction();

            Status = ActivityStatus.Success;

            if (MathUtilities.IsLessThan(GrazingCompetitionLimiter, 1) || MathUtilities.IsGreaterThan(PastureDesired - (pastureRequest?.Provided ?? 0), PastureDesired * shortfallReportingCutoff))
            {
                ResourceRequest shortfallRequest = pastureRequest;
                shortfallRequest.Required = PastureRequired;
                if (shortfallRequest is null)
                {
                    shortfallRequest = new ResourceRequest()
                    {
                        Available = pastureRequest?.Provided ?? 0, // display all that was given
                        Required = PastureRequired,
                        ResourceType = typeof(GrazeFoodStore),
                        ResourceTypeName = GrazeFoodStoreModel.Name
                    };
                }
                if (MathUtilities.IsLessThan(GrazingCompetitionLimiter, 1))
                {
                    Status = ((pastureRequest?.Provided ?? 0) == 0) ? ActivityStatus.Warning : ActivityStatus.Partial;
                    if (Status == ActivityStatus.Warning)
                    {
                        AddStatusMessage("No pasture");
                    }
                    // report desired to ignore the very low pasture biomass limiter that has been invoked with zero pasture.
                    shortfallRequest.Required = PastureRequired;
                    shortfallRequest.ShortfallStatus = "BelowRequired";
                }
                else
                {
                    if ((pastureRequest?.Provided ?? 0) == 0)
                    {
                        AddStatusMessage("No pasture");
                        Status = ActivityStatus.Warning;
                    }
                    shortfallRequest.Required = PastureDesired;
                    shortfallRequest.ShortfallStatus = "BelowDesired";
                }

                ActivitiesHolder.ReportActivityShortfall(new ResourceRequestEventArgs() { Request = shortfallRequest });

                // only allow the stop error if this is a shortfall in required not desired.
                if (MathUtilities.IsLessThan(Math.Round(GrazingCompetitionLimiter, 4), 1) && OnPartialResourcesAvailableAction == OnPartialResourcesAvailableActionTypes.ReportErrorAndStop)
                {
                    throw new ApsimXException(this, $"Insufficient pasture available for grazing in paddock ({GrazeFoodStoreModel.Name}) in {events.Clock.Today:dd\\yyyy}");
                }
            }
        }

        // Methods for GrazeFoodStoreType

        /// <summary>
        /// Method to set up pools from currently available graze pools and limit based upon green content herd limit parameters for CLEM Pasture approach
        /// </summary>
        /// <param name="limit">The competition limit defined from GrazePasture parent</param>
        public void CreateMixedPoolFeedItems(double limit)
        {
            if (GrazeFoodStoreModel is not GrazeFoodStoreType gfst)
                return;

            GrazingCompetitionLimiter = limit;
            // store kg/ha available for consumption calculation
            BiomassPerHectare = gfst.KilogramsPerHa;

            // calculate green proportion and green limit for species
            
            // if green prop + brown prop < intake required & strict feeding limits
            // increase green prop till shortfall met or green prop hits one

            // walk through all available pasture pools and add to groups by specified DMD range where 0 is all individual and 1 is all combined.
            // create a FoodResourcePacket for each group
            // add each pool to create the mix and store the pool in the food resource packet items.

            // we will feed animals down the groups
            // do we use proportion of fed from each group based on proportion of pasture (proportional feedeing)
            // or feed from top up to intake limit, leaving the poorest pasture till very end.

            // do this in paddock
            // at each pool - see if too much is taken - reduce based on breed proportion
            // walk down all pools..


        }


        /// <summary>
        /// Method to set up pools from currently available graze pools and limit based upon green content herd limit parameters for CLEM Pasture approach
        /// </summary>
        /// <param name="limit">The competition limit defined from GrazePasture parent</param>
        public void SetupPoolsAndLimits(double limit)
        {
            if (GrazeFoodStoreModel is not GrazeFoodStoreType gfst)
            {
                return;
            }

            GrazingCompetitionLimiter = limit;
            // store kg/ha available for consumption calculation
            BiomassPerHectare = gfst.Amount; //.KilogramsPerHa;

            // calculate breed feed limits
            if (PoolFeedLimits == null)
            {
                PoolFeedLimits = new();
            }
            else
            {
                PoolFeedLimits.Clear();
            }

            foreach (var pool in gfst.Pools)
            {
                PoolFeedLimits.Add(new GrazeBreedPoolLimit() { Limit = 1.0, Pool = pool });
            }

            // if Jan-March then use first three months otherwise use 2
            int greenage = (events.Clock.Today.Month <= 3) ? 2 : 1;

            double green = gfst.Pools.Where(a => (a.Age <= greenage)).Sum(b => b.Amount);
            double proportionGreen = green / gfst.Amount;

            // All values are now proportions.
            // Convert to percentage before calculation

            double greenLimit = (RuminantTypeModel.Parameters.Grazing.GreenDietMax * 100) * (1 - Math.Exp(-RuminantTypeModel.Parameters.Grazing.GreenDietCoefficient * ((proportionGreen * 100) - (RuminantTypeModel.Parameters.Grazing.GreenDietZero * 100))));
            greenLimit = Math.Max(0.0, greenLimit);
            if (MathUtilities.IsGreaterThan(proportionGreen, 0.9))
            {
                greenLimit = 100;
            }

            foreach (var pool in PoolFeedLimits.Where(a => a.Pool.Age <= greenage))
            {
                pool.Limit = greenLimit / 100.0;
            }

            // order feedPools by age so that diet is taken from youngest greenest first
            PoolFeedLimits = PoolFeedLimits.OrderBy(a => a.Pool.Age).ToList();
        }

        #region validation
        /// <inheritdoc/>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (GrazeFoodStoreTypeName.Contains("."))
            {
                ResourcesHolder resHolder = Structure.Find<ResourcesHolder>();
                if (resHolder is null || resHolder.FindResourceType<GrazeFoodStore, IGrazeFoodStoreType>(this, GrazeFoodStoreTypeName) is null)
                {
                    yield return new ValidationResult($"The location defined for grazing [r={GrazeFoodStoreTypeName}] in [a={Name}] is not found.{Environment.NewLine}Ensure [r=GrazeFoodStore] is present and the [GrazeFoodStoreType] is present", new string[] { "Location is not valid" });
                }
            }
        }
        #endregion
    }
}
