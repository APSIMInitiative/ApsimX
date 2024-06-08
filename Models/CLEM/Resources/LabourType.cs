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
        [Link]
        private readonly CLEMEvents events = new CLEMEvents();

        private DateTime birthDate;

        /// <summary>
        /// A list of attributes added to this individual
        /// </summary>
        [JsonIgnore]
        public IndividualAttributeList Attributes { get; set; } = new IndividualAttributeList();

        /// <summary>
        /// Unit type
        /// </summary>
        [JsonIgnore]
        public string Units { get { return "Days available"; } }

        /// <summary>
        /// Initial age of individuals.
        /// </summary>
        [Description("Initial Age")]
        [Core.Display(SubstituteSubPropertyName = "Parts")]
        [Units("years, months, days")]
        public AgeSpecifier InitialAge { get; set; } = new int[] { 18, 0, 0 };

        /// <summary>
        /// Male or Female
        /// </summary>
        [Description("Sex")]
        [Required]
        [FilterByProperty]
        public Sex Sex { get; set; }

        /// <summary>
        /// Name of the labour compoonent
        /// </summary>
        [FilterByProperty]
        public string NameOfLabour { get { return Name; } }

        /// <summary>
        /// Age in years.
        /// </summary>
        [JsonIgnore]
        [FilterByProperty]
        public double AgeInYears 
        { 
            get 
            { 
                if(AllowAgeing)
                    return (events.TimeStepStart - birthDate).TotalDays/365.0; 
                return InitialAge.InDays / 365.0;
            } 
        }

        /// <summary>
        /// Allow ageing of this individual
        /// </summary>
        [JsonIgnore]
        public bool AllowAgeing { get; private set; } = false;

        private double? adultEquivalent = null;

        /// <summary>
        /// Adult equivalent.
        /// </summary>
        [JsonIgnore]
        [FilterByProperty]
        public double AdultEquivalent{ get { return adultEquivalent?? 1; } }

        /// <summary>
        /// Method for controlling model to set the Adult Equivalent measurement for this individual
        /// </summary>
        /// <param name="aeRelationship">Relationship model providing the AE relationship</param>
        public void SetAdultEquivalent(Relationship aeRelationship)
        {
            if (aeRelationship is not null)
                adultEquivalent = aeRelationship.SolveY(AgeInYears);
        }

        /// <summary>
        /// Adult equivalents of all individuals.
        /// </summary>
        [JsonIgnore]
        [FilterByProperty]
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
        public bool Hired { get; set; }

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
            SetDefaults();
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

        /// <inheritdoc/>
        [EventSubscribe("OnInitialiseResources")]
        private void OnInitialiseResources(object sender, EventArgs e)
        {
            birthDate = events.Clock.StartDate.AddDays(-1 * InitialAge.InDays);
            AllowAgeing = (!Hired && (Parent as Labour).AllowAgeing);
        }

        /// <summary>
        /// Get value of this individual
        /// </summary>
        /// <returns>value</returns>
        public double PayRate(bool reportWarningIfNoPrice = false)
        {
            return (Parent as Labour).PayRate(this, reportWarningIfNoPrice);
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
                throw new Exception(String.Format("ResourceAmount object of type {0} does not support Add method in {1}", resourceAmount.GetType().ToString(), this.Name));

            double amountAdded = (double)resourceAmount;

            if (amountAdded > 0)
            {
                AvailableDays += amountAdded;
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

            if (Individuals > 1)
                throw new NotImplementedException("Cannot currently use labour transactions while using cohort-based style labour");

            double amountRemoved = request.Required;
            // avoid taking too much
            amountRemoved = Math.Min(AvailableDays, amountRemoved);
            AvailableDays -= amountRemoved;
            request.Provided = amountRemoved;

            ReportTransaction(TransactionType.Loss, amountRemoved, request.ActivityModel, request.RelatesToResource, request.Category, this);
        }

        /// <summary>
        /// Set amount of animal food available
        /// </summary>
        /// <param name="newValue">New value to set food store to</param>
        public new void Set(double newValue)
        {
            AvailableDays = newValue;
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
                return AvailableDays;
            }
        }

        #endregion

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using StringWriter htmlWriter = new();
            if (!FormatForParentControl)
            {
                htmlWriter.Write("<div class=\"activityentry\">");
                if (Individuals == 0)
                    htmlWriter.Write("No individuals are provided for this labour type");
                else
                {
                    if (Individuals > 1)
                        htmlWriter.Write($"<span class=\"setvalue\">{Individuals}</span> x ");
                    htmlWriter.Write($"<span class=\"setvalue\">{InitialAge}</span> year old ");
                    htmlWriter.Write($"<span class=\"setvalue\">{Sex}</span>");
                    if (Hired)
                        htmlWriter.Write(" as hired labour");
                }
                htmlWriter.Write("</div>");

                if (Individuals > 1)
                    htmlWriter.Write($"<div class=\"warningbanner\">You will be unable to identify these individuals with <span class=\"setvalue\">Name</div> but need to use the Attribute with tag <span class=\"setvalue\">Group</span> and value <span class=\"setvalue\">{Name}</span></div>");
            }
            return htmlWriter.ToString();
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