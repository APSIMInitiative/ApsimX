using Models.CLEM.Interfaces;
using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// This provides a common land store as GrazeFoodStoreType or AnimalFoodStoreType
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(GrazeFoodStore))]
    [ValidParent(ParentType = typeof(AnimalFoodStore))]
    [Description("This resource represents the pasture on common land")]
    [Version(1, 0, 1, "Beta build")]
    [Version(1, 0, 2, "Link to GrazeFoodStore implemented")]
    [HelpUri(@"Content/Features/Resources/AnimalFoodStore/CommonLandStoreType.htm")]
    public class CommonLandFoodStoreType : CLEMResourceTypeBase, IResourceWithTransactionType, IValidatableObject, IResourceType
    {
        [Link]
        private ResourcesHolder resources = null;

        [NonSerialized]
        private object pasture = new object();

        /// <summary>
        /// Unit type
        /// </summary>
        [Description("Units (nominal)")]
        public string Units { get; set; }

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
        /// Nitrogen of common land pasture (%)
        /// </summary>
        [Description("Nitrogen of common land pasture (%)")]
        [Required, Percentage]
        public double Nitrogen { get; set; }

        private double dryMatterDigestibility { get; set; }

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
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { "Not specified - general yards", typeof(GrazeFoodStore) } })]
        [System.ComponentModel.DefaultValue("Not specified - general yards")]
        public string PastureLink { get; set; }

        /// <summary>
        /// Proportional reduction of N% from linked pasture
        /// </summary>
        [Description("Proportional reduction of N% from linked pasture")]
        [Required, Proportion]
        public double NitrogenReductionFromPasture { get; set; }

        /// <summary>
        /// Amount (kg)
        /// </summary>
        [JsonIgnore]
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
        /// Total value of resource
        /// </summary>
        public double? Value
        {
            get
            {
                return Price(PurchaseOrSalePricingStyleType.Sale)?.CalculateValue(Amount);
            }
        }


        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            // TODO: find and link pasture

            if (pasture == null)
            {
                dryMatterDigestibility = Nitrogen * NToDMDCoefficient + NToDMDIntercept;
                dryMatterDigestibility = Math.Max(MinimumDMD, dryMatterDigestibility);
            }
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
                        dryMatterDigestibility = Nitrogen * NToDMDCoefficient + NToDMDIntercept;
                        dryMatterDigestibility = Math.Max(MinimumDMD, dryMatterDigestibility);
                        break;
                    case "GrazeFoodStoreType":
                        Nitrogen = (pasture as GrazeFoodStoreType).Nitrogen;
                        Nitrogen *= NitrogenReductionFromPasture;
                        Nitrogen = Math.Max(MinimumNitrogen, Nitrogen);
                        // calculate DMD from N%
                        dryMatterDigestibility = Nitrogen * NToDMDCoefficient + NToDMDIntercept;
                        dryMatterDigestibility = Math.Max(MinimumDMD, dryMatterDigestibility);
                        break;
                    default:
                        break;
                }
            }
        }

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
            EcologicalIndicatorsCalculated?.Invoke(this, e);
            CurrentEcologicalIndicators.Reset();
        }

        /// <summary>
        /// Ecological indicators of this pasture
        /// </summary>
        [JsonIgnore]
        public EcologicalIndicators CurrentEcologicalIndicators { get; set; }


        #region validation

        /// <inheritdoc/>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (Structure.FindChildren<Transmutation>().Count() > 0)
            {
                string[] memberNames = new string[] { "Transmutations" };
                results.Add(new ValidationResult("Transmutations are not available for the CommonLandFoodStoreType (" + this.Name + ")", memberNames));
            }

            pasture = new object();

            // check that either a AnimalFoodStoreType or a GrazeFoodStoreType can be found if link required.
            if (PastureLink != null && !PastureLink.StartsWith("Not specified"))
            {
                // check animalFoodStoreType
                pasture = resources.FindResourceType<ResourceBaseWithTransactions, IResourceType>(this, PastureLink, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop);
                if (pasture == null)
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
                    missing.Add("NToDMDCoefficient");

                if (NToDMDIntercept == 0)
                    missing.Add("NToDMDIntercept");

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
        #endregion

        #region transactions

        /// <summary>
        /// Graze food add method.
        /// This style is not supported in GrazeFoodStoreType
        /// </summary>
        /// <param name="resourceAmount">Object to add. This object can be double or contain additional information (e.g. Nitrogen) of food being added</param>
        /// <param name="activity">Name of activity adding resource</param>
        /// <param name="relatesToResource"></param>
        /// <param name="category"></param>
        public new void Add(object resourceAmount, CLEMModel activity, string relatesToResource, string category)
        {
            // expecting a GrazeFoodStoreResource (PastureManage) or FoodResourcePacket (CropManage)
            if (!(resourceAmount.GetType() == typeof(GrazeFoodStorePool) || resourceAmount.GetType() != typeof(FoodResourcePacket)))
                throw new Exception(String.Format("ResourceAmount object of type {0} is not supported in Add method in {1}", resourceAmount.GetType().ToString(), this.Name));

            GrazeFoodStorePool pool;
            if (resourceAmount.GetType() == typeof(GrazeFoodStorePool))
                pool = resourceAmount as GrazeFoodStorePool;
            else
            {
                pool = new GrazeFoodStorePool();
                FoodResourcePacket packet = resourceAmount as FoodResourcePacket;
                pool.Set(packet.Amount);
                pool.Nitrogen = packet.PercentN;
                pool.DMD = packet.DMD;
            }

            if (pool.Amount > 0)
            {
                ReportTransaction(TransactionType.Gain, pool.Amount, activity, relatesToResource, category, this);
            }
        }

        /// <inheritdoc/>
        public double Remove(double removeAmount, string activityName, string reason)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public new void Remove(ResourceRequest request)
        {
            // grazing or feeding from store treated the same way
            // grazing does not access pools by breed by gets all it needs of this quality common pasture
            // common pasture quality can be linked to a real pasture or foodstore and this has already been done.

            FoodResourcePacket additionalDetails = new FoodResourcePacket
            {
                PercentN = this.Nitrogen,
                DMD = this.dryMatterDigestibility,
                Amount = request.Required
            };
            request.AdditionalDetails = additionalDetails;

            // other non grazing activities requesting common land pasture
            request.Provided = request.Required;

            ReportTransaction(TransactionType.Loss, request.Provided, request.ActivityModel, request.RelatesToResource, request.Category, this);
        }

        /// <inheritdoc/>
        public new void Set(double newAmount)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("<div class=\"activityentry\">");
                if (this.Parent.GetType() == typeof(AnimalFoodStore))
                    htmlWriter.Write("This common land can be used by animal feed activities only");
                else
                    htmlWriter.Write("This common land can be used by grazing and cut and carry activities");

                htmlWriter.Write("</div>");
                if (PastureLink != null)
                {
                    htmlWriter.Write("<div class=\"activityentry\">");
                    htmlWriter.Write("The quality of this common land is based on <span class=\"resourcelink\">" + PastureLink + "</span> with <span class=\"setvalue\">" + (100 - this.NitrogenReductionFromPasture / 100).ToString("0.#") + "</span>% of the current nitrogen percent");
                    htmlWriter.Write("</div>");
                }
                else
                {
                    htmlWriter.Write("<div class=\"activityentry\">");
                    htmlWriter.Write("The nitrogen quality of new pasture is <span class=\"setvalue\">" + this.Nitrogen.ToString("0.###") + "%</span> and can be reduced to <span class=\"setvalue\">" + this.MinimumNitrogen.ToString("0.#") + "%</span>");
                    htmlWriter.Write("</div>");
                    htmlWriter.Write("<div class=\"activityentry\">");
                    htmlWriter.Write("The minimum Dry Matter Digestaibility is <span class=\"setvalue\">" + this.MinimumDMD.ToString("0.###") + "%</span>");
                    htmlWriter.Write("</div>");
                    htmlWriter.Write("<div class=\"activityentry\">");
                    htmlWriter.Write("Dry matter digestibility will be calculated from the N%");
                    htmlWriter.Write("</div>");
                }
                return htmlWriter.ToString();
            }
        }

        #endregion
    }
}
