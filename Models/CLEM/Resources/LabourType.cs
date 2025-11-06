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
    /// This stores the initialisation parameters for a labour type (person) who can do labour 
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Labour))]
    [Description("This resource represents a labour type (i.e. individual or cohort)")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/Labour/LabourType.htm")]
    public class LabourType : CLEMResourceTypeBase, IResourceWithTransactionType, IResourceType, IFilterable, IAttributable
    {
        private double ageInMonths = 0;

        /// <summary>
        /// A list of attributes added to this individual
        /// </summary>
        [JsonIgnore]
        public IndividualAttributeList Attributes { get; set; } = new IndividualAttributeList();

        /// <summary>
        /// Unit type
        /// </summary>
        [JsonIgnore]
        public string Units { get { return "NA"; } }

        /// <summary>
        /// Age in years.
        /// </summary>
        [Description("Initial Age")]
        [Required, GreaterThanEqualValue(0)]
        public double InitialAge { get; set; }

        /// <summary>
        /// Male or Female
        /// </summary>
        [Description("Sex")]
        [Required]
        [FilterByProperty]
        public Sex Sex { get; set; }

        /// <summary>
        /// Age in years.
        /// </summary>
        [JsonIgnore]
        [FilterByProperty]
        public double Age { get { return Math.Floor(AgeInMonths / 12); } }

        /// <summary>
        /// Age in months.
        /// </summary>
        [JsonIgnore]
        [FilterByProperty]
        public double AgeInMonths
        {
            get
            {
                return ageInMonths;
            }
            set
            {
                if (ageInMonths != value)
                {
                    ageInMonths = value;
                    // update AE
                    adultEquivalent = (Parent as Labour).CalculateAE(value);
                }
            }
        }

        /// <summary>
        /// The name of the individuals which may include an index at end for LabourTypes initialised with more than one individual
        /// </summary>
        [JsonIgnore]
        [FilterByProperty]
        public string NameOfIndividual {  get { return Name; } }

        private double? adultEquivalent = null;

        /// <summary>
        /// Adult equivalent.
        /// </summary>
        [JsonIgnore]
        [FilterByProperty]
        public double AdultEquivalent
        {
            get
            {
                // if null then report warning that no AE relationship has been provided.
                if (adultEquivalent == null)
                {
                    CLEMModel parent = (Parent as CLEMModel);
                    string warning = "No Adult Equivalent (AE) relationship has been added to [r=" + this.Parent.Name + "]. All individuals assumed to be 1 AE.\r\nAdd a suitable relationship with the Identifier with [Adult equivalent] below the [r=Labour] resource group.";
                    if (!parent.Warnings.Exists(warning))
                    {
                        parent.Warnings.Add(warning);
                        parent.Summary.WriteMessage(this, warning, MessageType.Warning);
                    }
                }
                return adultEquivalent ?? 1;
            }
        }

        /// <summary>
        /// Adult equivalents of all individuals.
        /// </summary>
        [JsonIgnore]
        public double TotalAdultEquivalents
        {
            get
            {
                return (adultEquivalent ?? 1) * Convert.ToDouble(Individuals, System.Globalization.CultureInfo.InvariantCulture);
            }
        }


        /// <summary>
        /// Total value of resource
        /// </summary>
        public double? Value
        {
            get
            {
                return null;
            }
        }


        /// <summary>
        /// Monthly dietary components
        /// </summary>
        [JsonIgnore]
        public List<LabourDietComponent> DietaryComponentList { get; set; }

        /// <summary>
        /// A method to calculate the details of the current intake
        /// </summary>
        /// <param name="metric">the name of the metric to report</param>
        /// <returns></returns>
        public double GetDietDetails(string metric)
        {
            double value = 0;
            if (DietaryComponentList != null)
                foreach (LabourDietComponent dietComponent in DietaryComponentList)
                    value += dietComponent.GetTotal(metric);

            return value;
        }

        /// <summary>
        /// A method to calculate the details of the current intake
        /// </summary>
        /// <returns></returns>
        public double GetAmountConsumed()
        {
            if (DietaryComponentList is null)
                return 0;
            else
                return DietaryComponentList.Sum(a => a.AmountConsumed);
        }

        /// <summary>
        /// A method to calculate the details of the current intake
        /// </summary>
        /// <returns></returns>
        public double GetAmountConsumed(string foodTypeName)
        {
            if (DietaryComponentList is null)
                return 0;
            else
                return DietaryComponentList.Where(a => a.FoodStore?.Name == foodTypeName).Sum(a => a.AmountConsumed);
        }

        /// <summary>
        /// The amount of feed eaten during the feed to target activity processing.
        /// </summary>
        [JsonIgnore]
        public double FeedToTargetIntake { get; set; }

        /// <summary>
        /// Number of individuals
        /// </summary>
        [Description("Number of individuals")]
        [Required, GreaterThanEqualValue(0)]
        public decimal Individuals { get; set; }

        /// <summary>
        /// Hired labour switch
        /// </summary>
        [Description("Hired labour")]
        [FilterByProperty]
        public bool IsHired { get; set; }

        /// <summary>
        /// The unique id of the last activity request for this labour type
        /// </summary>
        [JsonIgnore]
        public Guid[] LastActivityRequestID { get; set; } = new Guid[2];

        /// <summary>
        /// The number of days provided to the current activity
        /// </summary>
        [JsonIgnore]
        public double[] LastActivityLabour { get; set; } = new double[2];

        /// <summary>
        /// Available Labour (in days) in the current month. 
        /// </summary>
        [JsonIgnore]
        [FilterByProperty]
        public double AvailableDays { get; private set; }

        /// <summary>
        /// Link to the current labour availability for this person
        /// </summary>
        [JsonIgnore]
        public ILabourSpecificationItem LabourAvailability { get; set; }

        /// <summary>
        /// A proportion (0-1) to limit available labour. This may be from financial shortfall for hired labour.
        /// </summary>
        [JsonIgnore]
        public double AvailabilityLimiter { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public LabourType()
        {
            this.SetDefaults();
        }

        /// <summary>
        /// Determines the amount of labour up to a max available for the specified Activity.
        /// </summary>
        /// <param name="activityID">Unique activity ID</param>
        /// <param name="maxLabourAllowed">Max labour allowed</param>
        /// <param name="takeMode">Logical specifiying whether this is a availability check or resource take request</param>
        /// <returns></returns>
        public double LabourCurrentlyAvailableForActivity(Guid activityID, double maxLabourAllowed, bool takeMode)
        {
            int checkTakeIndex = Convert.ToInt32(takeMode);
            return Math.Min(Amount, maxLabourAllowed - ((activityID != LastActivityRequestID[checkTakeIndex]) ? 0 : LastActivityLabour[checkTakeIndex]));
        }

        /// <summary>
        /// Reset the available days for a given month
        /// </summary>
        /// <param name="month"></param>
        public void SetAvailableDays(int month)
        {
            AvailableDays = 0;
            if (LabourAvailability != null)
                AvailableDays = Math.Min(30.4, LabourAvailability.GetAvailability(month - 1) * AvailabilityLimiter);
        }

        /// <summary>
        /// Get value of this individual
        /// </summary>
        /// <returns>value</returns>
        public double PayRate()
        {
            return (Parent as Labour).PayRate(this);
        }

        #region Transactions

        /// <summary>
        /// Add to labour store of this type
        /// </summary>
        /// <param name="resourceAmount">Object to add. This object can be double or contain additional information (e.g. Nitrogen) of food being added</param>
        /// <param name="activity">Name of activity adding resource</param>
        /// <param name="relatesToResource"></param>
        /// <param name="category"></param>
        public new void Add(object resourceAmount, CLEMModel activity, string relatesToResource, string category)
        {
            if (resourceAmount.GetType().ToString() != "System.Double")
                throw new Exception(String.Format("ResourceAmount object of type {0} is not supported Add method in {1}", resourceAmount.GetType().ToString(), this.Name));

            double amountAdded = (double)resourceAmount;

            if (amountAdded > 0)
            {
                this.AvailableDays += amountAdded;
                ReportTransaction(TransactionType.Gain, amountAdded, activity, relatesToResource, category, this);
            }
        }

        /// <summary>
        /// Add intake to the DietaryComponents list
        /// </summary>
        /// <param name="dietComponent"></param>
        public void AddIntake(LabourDietComponent dietComponent)
        {
            if (DietaryComponentList == null)
                DietaryComponentList = new List<LabourDietComponent>();

            LabourDietComponent alreadyEaten = DietaryComponentList.Where(a => a.FoodStore != null && a.FoodStore.Name == dietComponent.FoodStore.Name).FirstOrDefault();
            if (alreadyEaten != null)
                alreadyEaten.AmountConsumed += dietComponent.AmountConsumed;
            else
                DietaryComponentList.Add(dietComponent);
        }

        /// <summary>
        /// Remove from labour store
        /// </summary>
        /// <param name="request">Resource request class with details.</param>
        public new void Remove(ResourceRequest request)
        {
            if (request.Required == 0)
                return;

            if (this.Individuals > 1)
                throw new NotImplementedException("Cannot currently use labour transactions while using cohort-based style labour");

            double amountRemoved = request.Required;
            // avoid taking too much
            amountRemoved = Math.Min(this.AvailableDays, amountRemoved);
            this.AvailableDays -= amountRemoved;
            request.Provided = amountRemoved;

            ReportTransaction(TransactionType.Loss, amountRemoved, request.ActivityModel, request.RelatesToResource, request.Category, this);
        }

        /// <summary>
        /// Set amount of animal food available
        /// </summary>
        /// <param name="newValue">New value to set food store to</param>
        public new void Set(double newValue)
        {
            this.AvailableDays = newValue;
        }

        #endregion

        #region IResourceType

        /// <summary>
        /// Implemented Initialise method
        /// </summary>
        public void Initialise()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Current amount of labour required.
        /// </summary>
        public double Amount
        {
            get
            {
                return this.AvailableDays;
            }
        }

        #endregion

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                if (!FormatForParentControl)
                {
                    htmlWriter.Write("<div class=\"activityentry\">");
                    if (this.Individuals == 0)
                        htmlWriter.Write("No individuals are provided for this labour type");
                    else
                    {
                        if (this.Individuals > 1)
                            htmlWriter.Write($"<span class=\"setvalue\">{this.Individuals}</span> x ");
                        htmlWriter.Write($"<span class=\"setvalue\">{this.InitialAge}</span> year old ");
                        htmlWriter.Write($"<span class=\"setvalue\">{this.Sex}</span>");
                        if (IsHired)
                            htmlWriter.Write(" as hired labour");
                    }
                    htmlWriter.Write("</div>");

                    if (this.Individuals > 1)
                        htmlWriter.Write($"<div class=\"warningbanner\">You will be unable to identify these individuals with <span class=\"setvalue\">Name</div> but need to use the Attribute with tag <span class=\"setvalue\">Group</span> and value <span class=\"setvalue\">{Name}</span></div>");
                }
                return htmlWriter.ToString();
            }
        }

        /// <inheritdoc/>
        public override string ModelSummaryClosingTags()
        {
            if (FormatForParentControl)
                return "";
            else
                return base.ModelSummaryClosingTags();
        }

        /// <inheritdoc/>
        public override string ModelSummaryOpeningTags()
        {
            if (FormatForParentControl)
                return "";
            else
                return base.ModelSummaryOpeningTags();
        }

        #endregion
    }
}