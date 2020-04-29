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
using System.Xml.Serialization;

namespace Models.CLEM.Activities
{
    /// <summary>
    /// Activity timer based on crop harvest
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ResourcePricing))]
    [Description("This activity timer is used to determine whether a resource level meets a set criteria.")]
    [HelpUri(@"Content/Features/Timers/ResourceLevel.htm")]
    [Version(1, 0, 1, "")]
    public class ActivityTimerResourceLevel: CLEMModel, IActivityTimer, IValidatableObject, IActivityPerformedNotifier
    {
        [Link]
        ResourcesHolder Resources = null;

        /// <summary>
        /// Name of resource to check
        /// </summary>
        [Description("Resource type")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Resource type is required")]
        [Models.Core.Display(Type = DisplayType.CLEMResourceName, CLEMResourceNameResourceGroups = new Type[] { typeof(AnimalFoodStore), typeof(Equipment), typeof(Finance), typeof(GrazeFoodStore), typeof(GreenhouseGases), typeof(HumanFoodStore), typeof(Labour), typeof(Land), typeof(OtherAnimals), typeof(ProductStore), typeof(WaterStore) })]
        public string ResourceTypeName { get; set; }

        /// <summary>
        /// Resource to check
        /// </summary>
        [XmlIgnore]
        public IResourceType ResourceTypeModel { get; set; }

        /// <summary>
        /// Operator to filter with
        /// </summary>
        [Description("Operator to use for filtering")]
        [Required]
        public FilterOperators Operator { get; set; }

        /// <summary>
        /// Amount
        /// </summary>
        [Description("Amount")]
        public double Amount { get; set; }

        /// <summary>
        /// Notify CLEM that this activity was performed.
        /// </summary>
        public event EventHandler ActivityPerformed;

        /// <summary>
        /// Constructor
        /// </summary>
        public ActivityTimerResourceLevel()
        {
            this.SetDefaults();
        }

        /// <summary>
        /// Validate model
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            return results;
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            ResourceTypeModel = Resources.GetResourceItem(this, ResourceTypeName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as IResourceType;
        }

        /// <summary>
        /// Method to determine whether the activity is due based on harvest details form parent.
        /// </summary>
        /// <returns>Whether the activity is due in the current month</returns>
        public bool ActivityDue
        {
            get
            {
                bool due = false;
                switch (Operator)
                {
                    case FilterOperators.Equal:
                        due = (ResourceTypeModel.Amount == Amount);
                        break;
                    case FilterOperators.NotEqual:
                        due = (ResourceTypeModel.Amount != Amount);
                        break;
                    case FilterOperators.LessThan:
                        due = (ResourceTypeModel.Amount < Amount);
                        break;
                    case FilterOperators.LessThanOrEqual:
                        due = (ResourceTypeModel.Amount <= Amount);
                        break;
                    case FilterOperators.GreaterThan:
                        due = (ResourceTypeModel.Amount > Amount);
                        break;
                    case FilterOperators.GreaterThanOrEqual:
                        due = (ResourceTypeModel.Amount >= Amount);
                        break;
                    default:
                        break;
                }

                if (due)
                {
                    // report activity performed.
                    ActivityPerformedEventArgs activitye = new ActivityPerformedEventArgs
                    {
                        Activity = new BlankActivity()
                        {
                            Status = ActivityStatus.Timer,
                            Name = this.Name
                        }
                    };
                    this.OnActivityPerformed(activitye);
                    activitye.Activity.SetGuID(this.UniqueID);
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Method to determine whether the activity is due based on a specified date
        /// </summary>
        /// <returns>Whether the activity is due based on the specified date</returns>
        public bool Check(DateTime dateToCheck)
        {
            return false;
        }

        /// <summary>
        /// Activity has occurred 
        /// </summary>
        /// <param name="e"></param>
        public virtual void OnActivityPerformed(EventArgs e)
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
            html += "\n<div class=\"filter\">";
            html += "Perform when ";
            if(ResourceTypeName is null || ResourceTypeName == "")
            {
                html += "<span class=\"errorlink\">RESOURCE NOT SET</span> ";
            }
            else
            {
                html += "<span class=\"resourcelink\">" + ResourceTypeName + "</span> ";
            }
            string str = "";
            switch (Operator)
            {
                case FilterOperators.Equal:
                    str += "equals";
                    break;
                case FilterOperators.NotEqual:
                    str += "does not equal";
                    break;
                case FilterOperators.LessThan:
                    str += "is less than";
                    break;
                case FilterOperators.LessThanOrEqual:
                    str += "is less than or equal to";
                    break;
                case FilterOperators.GreaterThan:
                    str += "is greater than";
                    break;
                case FilterOperators.GreaterThanOrEqual:
                    str += "is greater than or equal to";
                    break;
                default:
                    break;
            }
            html += str;
            if(Amount == 0)
            {
                html += " <span class=\"errorlink\">NOT SET</span>";
            }
            else
            {
                html += " <span class=\"setvalueextra\">";
                html += Amount.ToString();
                html += "</span>";
            }
            html += "</div>";
            if (!this.Enabled)
            {
                html += " - DISABLED!";
            }
            return html;
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryClosingTags(bool formatForParentControl)
        {
            return "</div>";
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryOpeningTags(bool formatForParentControl)
        {
            string html = "";
            html += "\n<div class=\"filterborder clearfix\" style=\"opacity: " + SummaryOpacity(formatForParentControl).ToString() + "\">";
            return html;
        }

    }
}
