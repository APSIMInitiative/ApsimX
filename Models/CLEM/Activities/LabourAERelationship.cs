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
    /// This determines a relationship
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Labour))]
    [Description("This model component specifies the Adult equivalent relationship to be used for labour by supplying a series of x and y values.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"content/features/Relationships/LabourAERelationship.htm")]
    public class LabourAERelationship: Relationship, IValidatableObject
    {
        /// <summary>
        /// Starting value
        /// </summary>
        [XmlIgnore]
        public new double StartingValue { get; set; }

        /// <summary>
        /// Minimum value possible
        /// </summary>
        [XmlIgnore]
        public new double Minimum { get; set; }

        /// <summary>
        /// Maximum value possible
        /// </summary>
        [Description("Maximum value possible")]
        [Required, GreaterThan("Minimum", ErrorMessage = "Maximum value must be greater than minimum value")]
        public new double Maximum { get; set; }

        /// <summary>
        /// X values of relationship
        /// </summary>
        [Description("X values of relationship")]
        [Required]
        public new double[] XValues { get; set; }

        /// <summary>
        /// Y values of relationship
        /// </summary>
        [Description("Y values of relationship")]
        [Required]
        public new double[] YValues { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public LabourAERelationship()
        {
            Minimum = 0;
        }

        /// <summary>
        /// Validate this object
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public new IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
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
