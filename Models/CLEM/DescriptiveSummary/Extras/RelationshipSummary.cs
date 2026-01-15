using DocumentFormat.OpenXml.Drawing.Charts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for the Relationship
/// </summary>
public class RelationshipSummary : DescriptiveSummaryProviderBase<Relationship>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        using StringWriter htmlWriter = new();
        // draw chart
        if (ModelTyped.XValues is null || ModelTyped.XValues.Length == 0)
        {
            htmlWriter.Write("<span class=\"errorlink\">No x values provided</span>");
        }
        else
        {
            if (ModelTyped.YValues is null || ModelTyped.XValues.Length != ModelTyped.YValues.Length)
            {
                htmlWriter.Write("<span class=\"errorlink\">Number of x values does not equal number of y values</span>");
            }
            else
            {
                htmlWriter.Write(@"
                        <canvas id=""myChart_" + ModelTyped.FullPath + @"""><p>Unable to display graph in browser</p></canvas>
                        <script>
                        var ctx = document.getElementById('myChart_" + ModelTyped.FullPath + @"').getContext('2d');
                        var myChart = new Chart(ctx, {
                        responsive:false,
                        maintainAspectRatio: true,
                        type: 'scatter',
                        data: {
                            datasets: [{
                                data: [");
                string data = "";
                for (int i = 0; i < ModelTyped.XValues.Length; i++)
                {
                    if (ModelTyped.YValues.Length > i)
                    {
                        data += "{ x: " + ModelTyped.XValues[i].ToString() + ", y: " + ModelTyped.YValues[i] + "},";
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
                        steppedLine: " + (ModelTyped.CalculationMethod == RelationshipCalculationMethod.UseSpecifiedValues).ToString().ToLower() + @",
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
                if (ModelTyped.NameOfXVariable != null && ModelTyped.NameOfXVariable != "")
                {
                    htmlWriter.Write(@", 
                            scaleLabel: {
                            display: true,
                            labelString: '" + ModelTyped.NameOfXVariable + @"'
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
                if (ModelTyped.NameOfYVariable != null && ModelTyped.NameOfYVariable != "")
                {
                    htmlWriter.Write(@", scaleLabel: {
                            display: true,
                            labelString: '" + ModelTyped.NameOfYVariable + @"'
                        }");
                }
                htmlWriter.Write(@"}],
                            }
                           }
                        });
                        </script>");
            }
        }
        generator.AddBlockWithText("activityentry", htmlWriter.ToString(), styleString: "width:400px;height:200px;");
    }
}
