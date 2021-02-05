using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;

namespace Models.CLEM
{
    /// <summary>
    /// This provides the ability to track a value based on an associated relationship of change in value provided by Y for a given X
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Relationship))]
    [Description("Tracks a bound running value based on a relationship where Y represents the change in this value for a given X")]
    [Version(1, 0, 1, "This component replaces the depreciated RelationshipTracker and is placed below a Relationship")]
    [HelpUri(@"Content/Features/Relationships/RelationshipRunningValue.htm")]
    public class RelationshipRunningValue: CLEMModel, IValidatableObject
    {
        /// <summary>
        /// Current value
        /// </summary>
        [JsonIgnore]
        public double Value { get; set; }

        /// <summary>
        /// Initial value of Running value that can be modified by this relationship Modify() during the simulation
        /// </summary>
        [Description("Initial running value")]
        [Required]
        public double StartingValue { get; set; }

        /// <summary>
        /// Minimum value possible
        /// </summary>
        [Description("Minimum running value possible")]
        [Required]
        public double Minimum { get; set; }

        /// <summary>
        /// Maximum value possible
        /// </summary>
        [Description("Maximum running value possible")]
        [Required, GreaterThan("Minimum", ErrorMessage = "Maximum value must be greater than minimum value")]
        public double Maximum { get; set; }

        /// <summary>
        /// Modify the current value by Y calculated from x
        /// </summary>
        /// <param name="x">x value</param>
        public void Modify(double x)
        {
            Value += (this.Parent as Relationship).SolveY(x);
            Value = Math.Min(Value, Maximum);
            Value = Math.Max(Value, Minimum);
        }

        /// <summary>
        /// Calculate new value using Y calculated from x
        /// </summary>
        /// <param name="x">x value</param>
        public void Calculate(double x)
        {
            Value = (this.Parent as Relationship).SolveY(x);
            Value = Math.Min(Value, Maximum);
            Value = Math.Max(Value, Minimum);
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            Value = StartingValue;
        }

        #region validation

        /// <summary>
        /// Validate this object
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            List<ValidationResult> results = new List<ValidationResult>();
            if (Maximum <= Minimum)
            {
                string[] memberNames = new string[] { "Maximum" };
                results.Add(new ValidationResult("The maximum running value must be greater than the Minimum value", memberNames));
            }
            return results;
        }

        #endregion

        #region descriptive summary

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("\r\n<div class=\"activityentry\">");
                htmlWriter.Write($"A running value starting at <span class=\"setvalue\">{StartingValue}</span>");
                htmlWriter.Write($" and ranging between <span class=\"setvalue\">{Minimum}</span> and ");
                if (Maximum <= Minimum)
                {
                    htmlWriter.Write("<span class=\"errorlink\">Invalid</span>");
                }
                else
                {
                    htmlWriter.Write($"<span class=\"setvalue\">{Maximum}</span>");
                }
                htmlWriter.Write("</div>");
                return htmlWriter.ToString(); 
            }
        } 
        #endregion
    }
}
