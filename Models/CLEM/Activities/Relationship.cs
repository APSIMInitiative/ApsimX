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
    /// This determines a relationship
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(PastureActivityManage))]
    [ValidParent(ParentType = typeof(RuminantActivityTrade))]
    [Description("This model component specifies a relationship to be used by supplying a series of x and y values.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"content/features/Relationships/Relationship.htm")]
    public class Relationship: Model, IValidatableObject
    {
        /// <summary>
        /// Current value
        /// </summary>
        [XmlIgnore]
        public double Value { get; set; }

        /// <summary>
        /// Starting value
        /// </summary>
        [Description("Value at start of simulation")]
        [Required]
        public double StartingValue { get; set; }

        /// <summary>
        /// Minimum value possible
        /// </summary>
        [Description("Minimum value possible")]
        [Required]
        public double Minimum { get; set; }

        /// <summary>
        /// Maximum value possible
        /// </summary>
        [Description("Maximum value possible")]
        [Required, GreaterThan("Minimum", ErrorMessage = "Maximum value must be greater than minimum value")]
        public double Maximum { get; set; }

        /// <summary>
        /// X values of relationship
        /// </summary>
        [Description("X values of relationship")]
        [Required]
        public double[] XValues { get; set; }

        /// <summary>
        /// Y values of relationship
        /// </summary>
        [Description("Y values of relationship")]
        [Required]
        public double[] YValues { get; set; }

        /// <summary>
        /// Solve equation for y given x
        /// </summary>
        /// <param name="xValue">x value to solve y</param>
        /// <param name="linearInterpolation">Use linear interpolation between the nearest point before and after x</param>
        /// <returns>y value for given x</returns>
        public double SolveY(double xValue, bool linearInterpolation)
        {
            if (xValue <= XValues[0])
            {
                return YValues[0];
            }

            if (xValue >= XValues[XValues.Length-1])
            {
                return YValues[YValues.Length - 1];
            }

            int k = 0;
            for (int i = 0; i < XValues.Length; i++)
            {
                if (xValue <= XValues[i + 1])
                {
                    k = i;
                    break;
                }
            }

            if(linearInterpolation)
            {
                return YValues[k] + (YValues[k + 1] - YValues[k]) / (XValues[k + 1] - XValues[k]) * (xValue - YValues[k]);
            }
            else
            {
                return YValues[k + 1];
            }
        }

        /// <summary>
        /// Modify the current value by Y calculated from x
        /// </summary>
        /// <param name="x">x value</param>
        public void Modify(double x)
        {
            Value += SolveY(x, true);
            Value = Math.Min(Value, Maximum);
            Value = Math.Max(Value, Minimum);
        }

        /// <summary>
        /// Calculate new value using Y calculated from x
        /// </summary>
        /// <param name="x">x value</param>
        public void Calculate(double x)
        {
            Value = SolveY(x, true);
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
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (XValues == null)
            {
                string[] memberNames = new string[] { "XValues" };
                results.Add(new ValidationResult("X values are required for relationship", memberNames));
            }
            if (YValues == null)
            {
                string[] memberNames = new string[] { "YValues" };
                results.Add(new ValidationResult("Y values are required for relationship", memberNames));
            }
            if (XValues.Length != YValues.Length)
            {
                string[] memberNames = new string[] { "XValues and YValues" };
                results.Add(new ValidationResult("The same number of X and Y values are required for relationship", memberNames));
            }
            if (XValues.Length == 0)
            {
                string[] memberNames = new string[] { "XValues" };
                results.Add(new ValidationResult("No data points were provided for relationship", memberNames));
            }
            if (XValues.Length < 2)
            {
                string[] memberNames = new string[] { "XValues" };
                results.Add(new ValidationResult("At least two data points are required for relationship", memberNames));
            }
            return results;
        }
    }
}
