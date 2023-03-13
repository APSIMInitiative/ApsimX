using Models.CLEM.Activities;
using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Newtonsoft.Json;
using System.IO;
using System.Linq.Expressions;

namespace Models.CLEM.Timers
{
    /// <summary>
    /// Activity timer based on resource
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ResourcePricing))]
    [Description("This timer is based on whether a resource level meets a set criteria.")]
    [HelpUri(@"Content/Features/Timers/ResourceLevel.htm")]
    [Version(1, 0, 1, "")]
    public class ActivityTimerResourceLevel: CLEMModel, IActivityTimer, IActivityPerformedNotifier
    {
        [Link]
        private ResourcesHolder resources = null;

        [Link] Clock clock = null;

        double amountAtFirstCheck;
        DateTime checkDate = DateTime.Now;

        /// <summary>
        /// Name of resource to check
        /// </summary>
        [Description("Resource type")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Resource type is required")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new Type[] { typeof(AnimalFoodStore), typeof(Equipment), typeof(Finance), typeof(GrazeFoodStore), typeof(GreenhouseGases), typeof(HumanFoodStore), typeof(Labour), typeof(Land), typeof(OtherAnimals), typeof(ProductStore), typeof(WaterStore) } })]
        public string ResourceTypeName { get; set; }

        /// <summary>
        /// Resource to check
        /// </summary>
        [JsonIgnore]
        public IResourceType ResourceTypeModel { get; set; }

        /// <summary>
        /// Operator to filter with
        /// </summary>
        [Description("Operator to use for filtering")]
        [Required]
        [Core.Display(Type = DisplayType.DropDown, Values = nameof(GetOperators))]
        public ExpressionType Operator { get; set; }
        private object[] GetOperators() => new object[]
        {
            ExpressionType.Equal,
            ExpressionType.NotEqual,
            ExpressionType.LessThan,
            ExpressionType.LessThanOrEqual,
            ExpressionType.GreaterThan,
            ExpressionType.GreaterThanOrEqual
        };

        /// <summary>
        /// Amount
        /// </summary>
        [Description("Amount")]
        public double Amount { get; set; }

        ///<inheritdoc/>
        public string StatusMessage { get; set; }

        /// <summary>
        /// Notify CLEM that this activity was performed.
        /// </summary>
        public event EventHandler ActivityPerformed;

        /// <summary>
        /// Constructor
        /// </summary>
        public ActivityTimerResourceLevel()
        {
            ModelSummaryStyle = HTMLSummaryStyle.Filter;
            this.SetDefaults();
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            ResourceTypeModel = resources.FindResourceType<ResourceBaseWithTransactions, IResourceType>(this, ResourceTypeName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop);
        }

        /// <inheritdoc/>
        public bool ActivityDue
        {
            get
            {
                if (clock.Today != checkDate)
                {
                    amountAtFirstCheck = ResourceTypeModel.Amount;
                    checkDate = clock.Today;
                }

                bool due = false;
                switch (Operator)
                {
                    case ExpressionType.Equal:
                        due = (amountAtFirstCheck == Amount);
                        break;
                    case ExpressionType.NotEqual:
                        due = (amountAtFirstCheck != Amount);
                        break;
                    case ExpressionType.LessThan:
                        due = (amountAtFirstCheck < Amount);
                        break;
                    case ExpressionType.LessThanOrEqual:
                        due = (amountAtFirstCheck <= Amount);
                        break;
                    case ExpressionType.GreaterThan:
                        due = (amountAtFirstCheck > Amount);
                        break;
                    case ExpressionType.GreaterThanOrEqual:
                        due = (amountAtFirstCheck >= Amount);
                        break;
                    default:
                        break;
                }

                return due;
            }
        }

        /// <inheritdoc/>
        public bool Check(DateTime dateToCheck)
        {
            return false;
        }

        /// <inheritdoc/>
        public virtual void OnActivityPerformed(EventArgs e)
        {
            ActivityPerformed?.Invoke(this, e);
        }

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("\r\n<div class=\"filter\">");
                htmlWriter.Write($"Perform when {DisplaySummaryValueSnippet(ResourceTypeName, "Resource not set", HTMLSummaryStyle.Resource)} ");
                string str = "";
                switch (Operator)
                {
                    case ExpressionType.Equal:
                        str += "equals";
                        break;
                    case ExpressionType.NotEqual:
                        str += "does not equal";
                        break;
                    case ExpressionType.LessThan:
                        str += "is less than";
                        break;
                    case ExpressionType.LessThanOrEqual:
                        str += "is less than or equal to";
                        break;
                    case ExpressionType.GreaterThan:
                        str += "is greater than";
                        break;
                    case ExpressionType.GreaterThanOrEqual:
                        str += "is greater than or equal to";
                        break;
                    default:
                        break;
                }
                htmlWriter.Write(str);
                if (Amount == 0)
                    htmlWriter.Write(" <span class=\"errorlink\">NOT SET</span>");
                else
                {
                    htmlWriter.Write(" <span class=\"setvalueextra\">");
                    htmlWriter.Write(Amount.ToString());
                    htmlWriter.Write("</span>");
                }
                htmlWriter.Write("</div>");
                if (!this.Enabled & !FormatForParentControl)
                    htmlWriter.Write(" - DISABLED!");
                return htmlWriter.ToString(); 
            }
        }

        /// <inheritdoc/>
        public override string ModelSummaryClosingTags()
        {
            return "</div>";
        }

        /// <inheritdoc/>
        public override string ModelSummaryOpeningTags()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("<div class=\"filtername\">");
                if (!this.Name.Contains(this.GetType().Name.Split('.').Last()))
                    htmlWriter.Write(this.Name);
                htmlWriter.Write($"</div>");
                htmlWriter.Write("\r\n<div class=\"filterborder clearfix\" style=\"opacity: " + SummaryOpacity(FormatForParentControl).ToString() + "\">");
                return htmlWriter.ToString(); 
            }
        } 
        #endregion

    }
}
