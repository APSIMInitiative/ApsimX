using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Models.CLEM.Resources;
using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations;

namespace Models.CLEM.Activities
{
    /// <summary>Ruminant grazing activity</summary>
    /// <summary>Specific version where pasture and breed is specified</summary>
    /// <summary>This activity determines how a ruminant breed will graze on a particular pasture (GrazeFoodSotreType)</summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("This activity performs grazing of a specified herd and pasture (paddock) in the simulation.")]
    class RuminantActivityGrazePastureBreed : CLEMRuminantActivityBase
    {
//        [Link]
//        public ResourcesHolder Resources = null;

        /// <summary>
        /// Number of hours grazed
        /// Based on 8 hour grazing days
        /// Could be modified to account for rain/heat walking to water etc.
        /// </summary>
        [Description("Number of hours grazed (based on 8 hr grazing day)")]
        [Required, Range(0, 8, ErrorMessage = "Value based on maximum 8 hour grazing day")]
        public double HoursGrazed { get; set; }

        /// <summary>
        /// Name of paddock or pasture to graze
        /// </summary>
        [Description("Name of GrazeFoodStoreType to graze")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Name of Graze Food Store required")]
        public string GrazeFoodStoreTypeName { get; set; }

        /// <summary>
        /// paddock or pasture to graze
        /// </summary>
        [XmlIgnore]
        public GrazeFoodStoreType GrazeFoodStoreModel { get; set; }

        /// <summary>
        /// Name of ruminant group to graze
        /// </summary>
        [Description("Name of ruminant type to graze")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Name of Ruminant Type required")]
        public string RuminantTypeName { get; set; }

        /// <summary>
        /// Ruminant group to graze
        /// </summary>
        [XmlIgnore]
        public RuminantType RuminantTypeModel { get; set; }

        /// <summary>
        /// The proportion of required graze that is available determined from parent activity arbitration
        /// </summary>
        public double GrazingCompetitionLimiter { get; set; }

        /// <summary>
        /// The biomass of pasture per hectare at start of allocation
        /// </summary>
        public double BiomassPerHectare { get; set; }

        /// <summary>
        /// Potential intake limiter based on pasture quality
        /// </summary>
        public double PotentialIntakePastureQualityLimiter { get; set; }

        /// <summary>
        /// Dry matter digestibility of pasture consumed (%)
        /// </summary>
        public double DMD { get; set; }

        /// <summary>
        /// Nitrogen of pasture consumed (%)
        /// </summary>
        public double N { get; set; }

        /// <summary>
        /// Proportion of intake that can be taken from each pool
        /// </summary>
        public List<GrazeBreedPoolLimit> PoolFeedLimits { get; set; }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            // This method will only fire if the user has added this activity to the UI
            // Otherwise all details will be provided from GrazeAll or GrazePaddock code [CLEMInitialiseActivity]

            this.InitialiseHerd(true, true);

            // if no settings have been provided from parent set limiter to 1.0. i.e. no limitation
            if (GrazingCompetitionLimiter == 0) GrazingCompetitionLimiter = 1.0;

            GrazeFoodStoreModel = Resources.GetResourceItem(this, typeof(GrazeFoodStore), GrazeFoodStoreTypeName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as GrazeFoodStoreType;

            RuminantTypeModel = Resources.GetResourceItem(this, typeof(RuminantHerd), RuminantTypeName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as RuminantType;
        }

        /// <summary>An event handler to allow us to clear requests at start of month.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("StartOfMonth")]
        private void OnStartOfMonth(object sender, EventArgs e)
        {
            ResourceRequestList = null;
        }

        public override List<ResourceRequest> GetResourcesNeededForActivity()
        {
            // check if resource request list has been calculated from a parent call
            if (ResourceRequestList == null)
            {
                ResourceRequestList = new List<ResourceRequest>();
                List<Ruminant> herd = this.CurrentHerd(false).Where(a => a.Location == this.GrazeFoodStoreModel.Name & a.Breed == this.RuminantTypeModel.Name).ToList();
                if (herd.Count() > 0)
                {
                    double amount = 0;
                    double indAmount = 0;
                    // get list of all Ruminants of specified breed in this paddock
                    foreach (Ruminant ind in herd)
                    {
                        // Reduce potential intake based on pasture quality for the proportion consumed calculated in GrazePasture.
                        // calculate intake from potential modified by pasture availability and hours grazed
                        indAmount = ind.PotentialIntake * PotentialIntakePastureQualityLimiter * (1 - Math.Exp(-ind.BreedParams.IntakeCoefficientBiomass * this.GrazeFoodStoreModel.TonnesPerHectareStartOfTimeStep * 1000)) * (HoursGrazed / 8);
                        // AL added reduce by amout already eaten to account for other feeding activities
                        indAmount -= ind.Intake;
                        amount += indAmount * GrazingCompetitionLimiter * 30.4;
                    }
                    if (amount > 0)
                    {
                        ResourceRequestList.Add(new ResourceRequest()
                        {
                            AllowTransmutation = true,
                            Required = amount,
                            ResourceType = typeof(GrazeFoodStore),
                            ResourceTypeName = this.GrazeFoodStoreModel.Name,
                            ActivityModel = this,
                            AdditionalDetails = this
                        }
                        );
                    }
                }
            }
            return ResourceRequestList;
        }

        public override void DoActivity()
        {
            //TODO: go through amount received and put it into the animals intake with quality measures.

            // the quality of mixed pasture eaten will be returned with ResourceRequest


            throw new NotImplementedException();
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
            if (ResourceShortfallOccurred != null)
                ResourceShortfallOccurred(this, e);
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
            if (ActivityPerformed != null)
                ActivityPerformed(this, e);
        }

    }

}
