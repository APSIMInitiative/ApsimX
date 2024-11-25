using Models.CLEM.Activities;
using Models.CLEM.Interfaces;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using System.IO;
using APSIM.Shared.Utilities;
using System.Xml.Serialization;
using Models.CLEM.Resources;

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
        /// Constructor
        /// </summary>
        public Relationship()
        {
            this.SetDefaults();
        }

        /// <summary>
        /// Solve equation for y given x
        /// </summary>
        /// <param name="xValue">x value to solve y</param>
        /// <returns>y value for given x</returns>
        public double SolveY(double xValue)
        {
            if (MathUtilities.IsLessThanOrEqual(xValue, XValues[0]))
                return YValues[0];

            if (MathUtilities.IsGreaterThanOrEqual(xValue, XValues[XValues.Length - 1]))
                return YValues[YValues.Length - 1];

            int k = 0;
            for (int i = 0; i < XValues.Length; i++)
                if (MathUtilities.IsLessThanOrEqual(xValue, XValues[i + 1]))
                {
                    k = i;
                    break;
                }

            if (CalculationMethod == RelationshipCalculationMethod.Interpolation)
                return YValues[k] + (YValues[k + 1] - YValues[k]) * (xValue - XValues[k]) / (XValues[k + 1] - XValues[k]);
            else
                return YValues[k + 1];
        }

        #region validation
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
        #endregion

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("\r\n<div class=\"activityentry\" style=\"width:400px;height:200px;\">");
                // draw chart

                if (XValues is null || XValues.Length == 0)
                    htmlWriter.Write("<span class=\"errorlink\">No x values provided</span>");
                else
                {
                    if (YValues is null || XValues.Length != YValues.Length)
                        htmlWriter.Write("<span class=\"errorlink\">Number of x values does not equal number of y values</span>");
                    else
                    {
                        htmlWriter.Write(@"
                        <canvas id=""myChart_" + this.FullPath + @"""><p>Unable to display graph in browser</p></canvas>
                        <script>
                        var ctx = document.getElementById('myChart_" + this.FullPath + @"').getContext('2d');
                        var myChart = new Chart(ctx, {
                        responsive:false,
                        maintainAspectRatio: true,
                        type: 'scatter',
                        data: {
                            datasets: [{
                                data: [");
                        string data = "";
                        for (int i = 0; i < XValues.Length; i++)
                            if (YValues.Length > i)
                                data += "{ x: " + XValues[i].ToString() + ", y: " + YValues[i] + "},";

                        data = data.TrimEnd(',');
                        htmlWriter.Write(data);
                        htmlWriter.Write(@"],
                        pointBackgroundColor: '[GraphPointColour]',
                        pointBorderColor: '[GraphPointColour]',
                        borderColor: '[GraphLineColour]', 
                        pointRadius: 5,
                        pointHoverRadius: 5,
                        fill: false,
                        tension: 0,
                        showLine: true,
                        steppedLine: " + (CalculationMethod == RelationshipCalculationMethod.UseSpecifiedValues).ToString().ToLower() + @",
                        }]
                        },
                        options: {
                            legend: {
                                display: false
                            },
                            scales: {
                                xAxes: [{
                                    color: 'green',
                                    type: 'linear',
                                    position: 'bottom',
                                    ticks: {
                                      fontColor: '[GraphLabelColour]',
                                      fontSize: 13,
                                      padding: 3
                                    },
                                    gridLines: {
                                       color: '[GraphGridLineColour]',
                                       drawOnChartArea: true
                                    }");
                        if (this.NameOfXVariable != null && this.NameOfXVariable != "")
                        {
                            htmlWriter.Write(@", 
                            scaleLabel: {
                            display: true,
                            labelString: '" + this.NameOfXVariable + @"'
                            }");
                        }
                        htmlWriter.Write(@"}],
                        yAxes: [{
                            type: 'linear',
                            gridLines: {
                                zeroLineColor: '[GraphGridZeroLineColour]',
                                zeroLineWidth: 1,
                                zeroLineBorderDash: [3, 3],
                                color: '[GraphGridLineColour]',
                                drawOnChartArea: true
                            },
                            ticks: {
                                fontColor: '[GraphLabelColour]',
                                fontSize: 13,
                                padding: 3
                            }");
                        if (this.NameOfYVariable != null && this.NameOfYVariable != "")
                        {
                            htmlWriter.Write(@", scaleLabel: {
                            display: true,
                            labelString: '" + this.NameOfYVariable + @"'
                        }");
                        }
                        htmlWriter.Write(@"}],
                            }
                           }
                        });
                        </script>");
                    }
                }
                htmlWriter.Write("\r\n</div>");
                return htmlWriter.ToString(); 
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

        #endregion
    }
}
