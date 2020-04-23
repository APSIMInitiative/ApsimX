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
    [ValidParent(ParentType = typeof(RuminantActivityTrade))]
    [Description("This model component specifies a relationship to be used by supplying a series of x and y values.")]
    [Version(1, 0, 3, "Graph of relationship displayed in Summary")]
    [Version(1, 0, 2, "Added RelationshipCalculationMethod to allow user to define fixed or linear solver")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"content/features/Relationships/Relationship.htm")]
    public class Relationship : CLEMModel, IValidatableObject
    {
        /// <summary>
        /// Current value
        /// </summary>
        [XmlIgnore]
        public double Value { get; set; }

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

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
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

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = "";
            html += "\n<div class=\"activityentry\" style=\"width:400px;height:200px;\">";
            // draw chart

            if (XValues is null || XValues.Length == 0)
            {
                html += "<span class=\"errorlink\">No x values provided</span>";
            }
            else if (YValues is null || XValues.Length != YValues.Length)
            {
                html += "<span class=\"errorlink\">Number of x values does not equal number of y values</span>";
            }
            else
            {
                html += @"
                <canvas id=""myChart_" + this.Name + @"""><p>Unable to display graph in browser</p></canvas>
                <script>
                var ctx = document.getElementById('myChart_" + this.Name + @"').getContext('2d');
                var myChart = new Chart(ctx, {
                    responsive:false,
                    maintainAspectRatio: true,
                    type: 'scatter',
                    data: {
                        datasets: [{
                            data: [";
                string data = "";
                for (int i = 0; i < XValues.Length; i++)
                {
                    if (YValues.Length > i)
                    {
                        data += "{ x: " + XValues[i].ToString() + ", y: " + YValues[i] + "},";
                    }
                }
                data = data.TrimEnd(',');
                html += data;
                html += @"],
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
                                    }";
                if (this.NameOfXVariable != null && this.NameOfXVariable != "")
                {
                    html += @", 
                                      scaleLabel: {
                                       display: true,
                                       labelString: '" + this.NameOfXVariable + @"'
                                      }";
                }
                html += @"}],
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
                                    }";
                if (this.NameOfYVariable != null && this.NameOfYVariable != "")
                {
                    html += @", scaleLabel: {
                                      display: true,
                                      labelString: '" + this.NameOfYVariable + @"'
                                    }";
                }
                html += @"}],
                            }
                           }
                        });
                </script>";
            }
            html += "\n</div>";
            return html;
        }

    }
}
