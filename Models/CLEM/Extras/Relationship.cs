using Models.CLEM.Activities;
using Models.CLEM.Interfaces;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using System.IO;
using System.Xml.Serialization;
using Models.CLEM.Resources;
using APSIM.Numerics;

namespace Models.CLEM
{
    /// <summary>
    /// This determines a relationship
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(RuminantActivityPurchase))]
    [ValidParent(ParentType = typeof(RuminantActivityPredictiveStockingENSO))]
    [ValidParent(ParentType = typeof(PastureActivityManage))]
    [ValidParent(ParentType = typeof(Labour))]
    [ValidParent(ParentType = typeof(OtherAnimalsType))]
    [Description("Specifies a relationship to be used by supplying a series of x and y values.")]
    [Version(1, 0, 4, "Default 0,0 now applies")]
    [Version(1, 0, 3, "Graph of relationship displayed in Summary")]
    [Version(1, 0, 2, "Added RelationshipCalculationMethod to allow user to define fixed or linear solver")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Relationships/Relationship.htm")]
    public class Relationship : CLEMModel, IValidatableObject, IActivityCompanionModel
    {
        /// <summary>
        /// An identifier for this Relationship based on parent requirements
        /// </summary>
        [Description("Relationship identifier")]
        [Core.Display(Type = DisplayType.DropDown, Values = "ParentSuppliedIdentifiers", VisibleCallback = "ParentSuppliedIdentifiersPresent")]
        public string Identifier { get; set; }

        /// <inheritdoc/>
        [XmlIgnore]
        public string Measure
        {
            get { return ""; }
            set {; }
        }

        /// <summary>
        /// X values of relationship
        /// </summary>
        [Description("X values of relationship")]
        [Required]
        [System.ComponentModel.DefaultValue(new double[] { 0 })]
        public double[] XValues { get; set; }

        /// <summary>
        /// Y values of relationship
        /// </summary>
        [Description("Y values of relationship")]
        [Required]
        [System.ComponentModel.DefaultValue(new double[] { 0 })]
        public double[] YValues { get; set; }

        /// <summary>
        /// Method to solving relationship
        /// </summary>
        [Description("Method for solving relationship")]
        [Required]
        public RelationshipCalculationMethod CalculationMethod { get; set; }

        /// <summary>
        /// Name of the x variable
        /// </summary>
        [Description("Label for x variable")]
        public string NameOfXVariable { get; set; }

        /// <summary>
        /// Name of the y variable
        /// </summary>
        [Description("Label for y variable")]
        public string NameOfYVariable { get; set; }

        /// <inheritdoc/>
        [JsonIgnore]
        public string TransactionCategory { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        /// <summary>
        /// Solve equation for y given x
        /// </summary>
        /// <param name="xValue">x value to solve y</param>
        /// <returns>y value for given x</returns>
        public double SolveY(double xValue)
        {
            if (MathUtilities.IsLessThanOrEqual(xValue, XValues[0]))
            {
                return YValues[0];
            }

            if (MathUtilities.IsGreaterThanOrEqual(xValue, XValues[XValues.Length - 1]))
            {
                return YValues[YValues.Length - 1];
            }

            int k = 0;
            for (int i = 0; i < XValues.Length; i++)
            {
                if (MathUtilities.IsLessThanOrEqual(xValue, XValues[i + 1]))
                {
                    k = i;
                    break;
                }
            }

            if (CalculationMethod == RelationshipCalculationMethod.Interpolation)
            {
                return YValues[k] + (YValues[k + 1] - YValues[k]) * (xValue - XValues[k]) / (XValues[k + 1] - XValues[k]);
            }
            else
            {
                return YValues[k + 1];
            }
        }

        /// <inheritdoc/>
        public void PrepareForTimestep()
        {
            return;
        }

        /// <inheritdoc/>
        public List<ResourceRequest> RequestResourcesForTimestep(double argument = 0)
        {
            return null;
        }

        /// <inheritdoc/>
        public void PerformTasksForTimestep(double argument = 0)
        {
            return;
        }

        #region validation
        /// <inheritdoc/>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (XValues == null)
            {
                string[] memberNames = new string[] { "XValues" };
                yield return new ValidationResult("X values are required for relationship", memberNames);
            }
            if (YValues == null)
            {
                string[] memberNames = new string[] { "YValues" };
                yield return new ValidationResult("Y values are required for relationship", memberNames);
            }
            if (XValues.Length != YValues.Length)
            {
                string[] memberNames = new string[] { "XValues and YValues" };
                yield return new ValidationResult("The same number of X and Y values are required for relationship", memberNames);
            }
            if (XValues.Length == 0)
            {
                string[] memberNames = new string[] { "XValues" };
                yield return new ValidationResult("No data points were provided for relationship", memberNames);
            }
            if (XValues.Length < 2)
            {
                string[] memberNames = new string[] { "XValues" };
                yield return new ValidationResult("At least two data points are required for relationship", memberNames);
            }
        }
        #endregion
    }
}
