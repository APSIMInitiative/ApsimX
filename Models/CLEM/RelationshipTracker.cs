using Models.CLEM.Activities;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Models.CLEM
{
    /// <summary>
    /// This determines a relationship
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(PastureActivityManage))]
    [Description("This model component specifies a relationship where the y value related to a change in running value as a function of x. This component tracks a value through time as modified by the specificed relationship.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"content/features/Relationships/RelationshipTracker.htm")]
    public class RelationshipTracker : Relationship, IValidatableObject
    {
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
            Value += SolveY(x);
            Value = Math.Min(Value, Maximum);
            Value = Math.Max(Value, Minimum);
        }

        /// <summary>
        /// Calculate new value using Y calculated from x
        /// </summary>
        /// <param name="x">x value</param>
        public void Calculate(double x)
        {
            Value = SolveY(x);
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

        /// <summary>
        /// Validate this object
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public new IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            results = base.Validate(validationContext).ToList();
            if (Maximum < Minimum)
            {
                string[] memberNames = new string[] { "Maximum" };
                results.Add(new ValidationResult("The maximum running value must be greater than the Minimum value", memberNames));
            }
            return results;
        }
    }
}
