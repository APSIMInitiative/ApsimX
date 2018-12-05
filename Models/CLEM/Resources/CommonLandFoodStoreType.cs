using Models.CLEM.Activities;
using Models.CLEM.Reporting;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// This provides a common land store as GrazeFoodStoreType or AnimalFoodStoreType
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(GrazeFoodStore))]
    [ValidParent(ParentType = typeof(AnimalFoodStore))]
    [Description("This resource represents a common land food store.")]
    [Version(1, 0, 1, "Adam Liedloff", "CSIRO", "Beta build")]
    [Version(1, 0, 2, "Adam Liedloff", "CSIRO", "Link to GrazeFoodStore implemented")]
    public class CommonLandFoodStoreType : CLEMResourceTypeBase, IResourceWithTransactionType, IValidatableObject, IResourceType
    {
        /// <summary>
        /// 
        /// </summary>
        [Link]
        public ResourcesHolder Resources = null;

        /// <summary>
        /// Coefficient to convert N% to DMD%
        /// </summary>
        [Description("Coefficient to convert N% to DMD%")]
        [Required]
        public double NToDMDCoefficient { get; set; }

        /// <summary>
        /// Intercept to convert N% to DMD%
        /// </summary>
        [Description("Intercept to convert N% to DMD%")]
        [Required]
        public double NToDMDIntercept { get; set; }

        /// <summary>
        /// Crude protein denominator to convert N% to DMD%
        /// </summary>
        [Description("Crude protein denominator to convert N% to DMD%")]
        [Required]
        public double NToDMDCrudeProteinDenominator { get; set; }

        /// <summary>
        /// Nitrogen of common land pasture (%)
        /// </summary>
        [Description("Nitrogen of common land pasture (%)")]
        [Required, Percentage]
        public double Nitrogen { get; set; }

        private double DMD { get; set; }

        /// <summary>
        /// Minimum Nitrogen %
        /// </summary>
        [Description("Minimum Nitrogen %")]
        [Required, Percentage]
        public double MinimumNitrogen { get; set; }

        /// <summary>
        /// Minimum Dry Matter Digestibility
        /// </summary>
        [Description("Minimum Dry Matter Digestibility")]
        [Required, Percentage]
        public double MinimumDMD { get; set; }

        /// <summary>
        /// Link to a AnimalFoodStore or GrazeFoodStore for pasture details
        /// </summary>
        [Description("AnimalFoodStore or GrazeFoodStore type for pasture details")]
        [Models.Core.Display(Type = DisplayTypeEnum.CLEMResourceName, CLEMResourceNameResourceGroups = new Type[] { typeof(GrazeFoodStore) }, CLEMExtraEntries = new string[] { "Not specified - general yards" })]
        public string PastureLink { get; set; }

        [NonSerialized]
        private object pasture = new object();

        /// <summary>
        /// Proportional reduction of N% from linked pasture
        /// </summary>
        [Description("Proportional reduction of N% from linked pasture")]
        [Required, Proportion]
        public double NitrogenReductionFromPasture { get; set; }

        /// <summary>
        /// Amount (kg)
        /// </summary>
        [XmlIgnore]
        public double Amount
        {
            get
            {
                // this is a virtual pool of forage outside the farm
                // it requires labour to create (cut and carry) it.
                return double.PositiveInfinity; // Pools.Sum(a => a.Amount);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if(Apsim.Children(this, typeof(Transmutation)).Count() > 0)
            {
                string[] memberNames = new string[] { "Transmutations" };
                results.Add(new ValidationResult("Transmutations are not available for the CommonLandFoodStoreType (" + this.Name + ")", memberNames));
            }

            pasture = new object();

            // check that either a AnimalFoodStoreType or a GrazeFoodStoreType can be found if link required.
            if (PastureLink!=null && !PastureLink.StartsWith("Not specified"))
            {
                // check animalFoodStoreType
                pasture = Resources.GetResourceItem(this, PastureLink, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop);
                if(pasture==null)
                {
                    string[] memberNames = new string[] { "Pasture link" };
                    results.Add(new ValidationResult("A link to an animal food store or graze food store type must be supplied to link to common land (" + this.Name + ")", memberNames));
                }
            }

            if (PastureLink != null && PastureLink.StartsWith("Not specified"))
            {
                // no link so need to ensure values are all supplied.
                List<string> missing = new List<string>();
                if (NToDMDCoefficient == 0)
                {
                    missing.Add("NToDMDCoefficient");
                }
                if (NToDMDIntercept == 0)
                {
                    missing.Add("NToDMDIntercept");
                }
                if (NToDMDCrudeProteinDenominator == 0)
                {
                    missing.Add("NToDMDCrudeProteinDenominator");
                }
                if (missing.Count() > 0)
                {
                    foreach (var item in missing)
                    {
                        string[] memberNames = new string[] { item };
                        results.Add(new ValidationResult("The common land [r=" + this.Name + "] requires [o=" + item + "] as it is not linked to an on-farm pasture", memberNames));
                    }
                }
            }
            return results;
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            // TODO: find and link pasture

            if(pasture==null)
            {
                DMD = Nitrogen * NToDMDCoefficient + NToDMDIntercept;
                DMD = Math.Max(MinimumDMD, DMD);
            }
        }

        /// <summary>
        /// Overrides the base class method to allow for clean up
        /// </summary>
        [EventSubscribe("Completed")]
        private void OnSimulationCompleted(object sender, EventArgs e)
        {
        }

        /// <summary>Clear data stores for utilisation at end of ecological indicators calculation month</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMAgeResources")]
        private void ONCLEMAgeResources(object sender, EventArgs e)
        {
        }

        /// <summary>Store amount of pasture available for everyone at the start of the step (kg per hectare)</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMPastureReady")]
        private void ONCLEMPastureReady(object sender, EventArgs e)
        {
            if (pasture != null)
            {
                // using linked pasture so get details from pasture for timestep 
                switch (pasture.GetType().ToString())
                {
                    case "AnimalFoodStoreType":
                        Nitrogen = (pasture as AnimalFoodStoreType).Nitrogen;
                        Nitrogen *= NitrogenReductionFromPasture;
                        Nitrogen = Math.Max(MinimumNitrogen, Nitrogen);
                        // calculate DMD from N% 
                        DMD = Nitrogen * NToDMDCoefficient + NToDMDIntercept;
                        DMD = Math.Max(MinimumDMD, DMD);
                        break;
                    case "GrazeFoodStoreType":
                        Nitrogen = (pasture as GrazeFoodStoreType).Nitrogen;
                        Nitrogen *= NitrogenReductionFromPasture;
                        Nitrogen = Math.Max(MinimumNitrogen, Nitrogen);
                        // calculate DMD from N% 
                        DMD = Nitrogen * NToDMDCoefficient + NToDMDIntercept;
                        DMD = Math.Max(MinimumDMD, DMD);
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// Graze food add method.
        /// This style is not supported in GrazeFoodStoreType
        /// </summary>
        /// <param name="ResourceAmount">Object to add. This object can be double or contain additional information (e.g. Nitrogen) of food being added</param>
        /// <param name="Activity">Name of activity adding resource</param>
        /// <param name="Reason">Name of individual adding resource</param>
        public new void Add(object ResourceAmount, CLEMModel Activity, string Reason)
        {
            // expecting a GrazeFoodStoreResource (PastureManage) or FoodResourcePacket (CropManage)
            if (!(ResourceAmount.GetType() == typeof(GrazeFoodStorePool) | ResourceAmount.GetType() != typeof(FoodResourcePacket)))
            {
                throw new Exception(String.Format("ResourceAmount object of type {0} is not supported in Add method in {1}", ResourceAmount.GetType().ToString(), this.Name));
            }

            GrazeFoodStorePool pool;
            if (ResourceAmount.GetType() == typeof(GrazeFoodStorePool))
            {
                pool = ResourceAmount as GrazeFoodStorePool;
            }
            else
            {
                pool = new GrazeFoodStorePool();
                FoodResourcePacket packet = ResourceAmount as FoodResourcePacket;
                pool.Set(packet.Amount);
                pool.Nitrogen = packet.PercentN;
                pool.DMD = packet.DMD;
            }

            if (pool.Amount > 0)
            {
                // need to check the follwoing code is no longer needed.

                // allow decaying or no pools currently available
                //if (PastureDecays | Pools.Count() == 0)
                //{
                //    Pools.Insert(0, pool);
                //}
                //else
                //{
                //    Pools[0].Add(pool);
                //}
                //// update biomass available
                //biomassAddedThisYear += pool.Amount;

                ResourceTransaction details = new ResourceTransaction();
                details.Debit = pool.Amount;
                details.Activity = Activity.Name;
                details.ActivityType = Activity.GetType().Name;
                details.Reason = Reason;
                details.ResourceType = this.Name;
                LastTransaction = details;
                TransactionEventArgs te = new TransactionEventArgs() { Transaction = details };
                OnTransactionOccurred(te);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="RemoveAmount"></param>
        /// <param name="ActivityName"></param>
        /// <param name="Reason"></param>
        public double Remove(double RemoveAmount, string ActivityName, string Reason)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Request"></param>
        public new void Remove(ResourceRequest Request)
        {
            // grazing or feeding from store treated the same way
            // grazing does not access pools by breed by gets all it needs of this quality common pasture
            // common pasture quality can be linked to a real pasture or foodstore and this has already been done.

            FoodResourcePacket additionalDetails = new FoodResourcePacket();
            additionalDetails.PercentN = this.Nitrogen;
            additionalDetails.DMD = this.DMD;
            additionalDetails.Amount = Request.Required;
            Request.AdditionalDetails = additionalDetails;

            // other non grazing activities requesting common land pasture
            Request.Provided = Request.Required;

            // report 
            ResourceTransaction details = new ResourceTransaction();
            details.ResourceType = this.Name;
            details.Credit = Request.Provided;
            details.Activity = Request.ActivityModel.Name;
            details.ActivityType = Request.ActivityModel.GetType().Name;
            details.Reason = Request.Reason;
            LastTransaction = details;
            TransactionEventArgs te = new TransactionEventArgs() { Transaction = details };
            OnTransactionOccurred(te);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="NewAmount"></param>
        public new void Set(double NewAmount)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Back account transaction occured
        /// </summary>
        public event EventHandler TransactionOccurred;

        /// <summary>
        /// Transcation occurred 
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnTransactionOccurred(EventArgs e)
        {
            if (TransactionOccurred != null)
                TransactionOccurred(this, e);
        }

        /// <summary>
        /// Last transaction received
        /// </summary>
        [XmlIgnore]
        public ResourceTransaction LastTransaction { get; set; }

        /// <summary>
        /// Ecological indicators have been calculated
        /// </summary>
        public event EventHandler EcologicalIndicatorsCalculated;

        /// <summary>
        /// Ecological indicators calculated 
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnEcologicalIndicatorsCalculated(EventArgs e)
        {
            if (EcologicalIndicatorsCalculated != null)
                EcologicalIndicatorsCalculated(this, e);
        }


        /// <summary>
        /// Ecological indicators of this pasture
        /// </summary>
        [XmlIgnore]
        public EcologicalIndicators CurrentEcologicalIndicators { get; set; }


        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="FormatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool FormatForParentControl)
        {
            string html = "";
            html += "<div class=\"activityentry\">";
            if (this.Parent.GetType() == typeof(AnimalFoodStore))
            {
                html += "This common land can be used by animal feed activities only";
            }
            else
            {
                html += "This common land can be used by grazing and cut and carry activities";
            }
            html += "</div>";
            if (PastureLink != null)
            {
                html += "<div class=\"activityentry\">";
                html += "The quality of this common land is based on <span class=\"resourcelink\">" + PastureLink + "</span> with <span class=\"setvalue\">" + (100 - this.NitrogenReductionFromPasture / 100).ToString("0.#") + "</span>% of the current Nitrogen percent";
                html += "</div>";
            }
            else
            {
                html += "<div class=\"activityentry\">";
                html += "The nitrogen quality of new pasture is <span class=\"setvalue\">" + this.Nitrogen.ToString("0.###") + "%</span> and can be reduced to <span class=\"setvalue\">" + this.MinimumNitrogen.ToString("0.#") + "%</span>";
                html += "</div>";
                html += "<div class=\"activityentry\">";
                html += "The minimum Dry Matter Digestaibility is <span class=\"setvalue\">" + this.MinimumDMD.ToString("0.###") + "%</span>";
                html += "</div>";
                html += "<div class=\"activityentry\">";
                html += "Dry matter digestibility will be calculated from the N%";
                html += "</div>";
            }
            return html;
        }

    }
}
