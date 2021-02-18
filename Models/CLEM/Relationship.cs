using Models.CLEM.Activities;
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
    /// This determines a relationship
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(RuminantActivityTrade))]
    [ValidParent(ParentType = typeof(PastureActivityManage))]
    [Description("This model component specifies a relationship to be used by supplying a series of x and y values.")]
    [Version(1, 0, 4, "Default 0,0 now applies")]
    [Version(1, 0, 3, "Graph of relationship displayed in Summary")]
    [Version(1, 0, 2, "Added RelationshipCalculationMethod to allow user to define fixed or linear solver")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Relationships/Relationship.htm")]
    public class Relationship : CLEMModel, IValidatableObject
    {
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
        [Description("Name of the x variable")]
        public string NameOfXVariable { get; set; }

        /// <summary>
        /// Name of the y variable
        /// </summary>
        [Description("Name of the y variable")]
        public string NameOfYVariable { get; set; }

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
            if (xValue <= XValues[0])
            {
                return YValues[0];
            }

            if (xValue >= XValues[XValues.Length - 1])
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

            if (CalculationMethod == RelationshipCalculationMethod.Interpolation)
            {
                return YValues[k] + (YValues[k + 1] - YValues[k]) * (xValue - XValues[k]) / (XValues[k + 1] - XValues[k]);
            }
            else
            {
                return YValues[k + 1];
            }
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
        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("\r\n<div class=\"activityentry\" style=\"width:400px;height:200px;\">");
                // draw chart

                if (XValues is null || XValues.Length == 0)
                {
                    htmlWriter.Write("<span class=\"errorlink\">No x values provided</span>");
                }
                else if (YValues is null || XValues.Length != YValues.Length)
                {
                    htmlWriter.Write("<span class=\"errorlink\">Number of x values does not equal number of y values</span>");
                }
                else
                {
                    htmlWriter.Write(@"
                <canvas id=""myChart_" + this.Name + @"""><p>Unable to display graph in browser</p></canvas>
                <script>
                var ctx = document.getElementById('myChart_" + this.Name + @"').getContext('2d');
                var myChart = new Chart(ctx, {
                    responsive:false,
                    maintainAspectRatio: true,
                    type: 'scatter',
                    data: {
                        datasets: [{
                            data: [");
                    string data = "";
                    for (int i = 0; i < XValues.Length; i++)
                    {
                        if (YValues.Length > i)
                        {
                            data += "{ x: " + XValues[i].ToString() + ", y: " + YValues[i] + "},";
                        }
                    }
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
                htmlWriter.Write("\r\n</div>");
                return htmlWriter.ToString(); 
            }
        }

        #endregion
    }
}
