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
        string output = $"The {generator.DisplaySummaryValueSnippet(ModelTyped.Identifier)} relationship provides {generator.DisplaySummaryValueSnippet(ModelTyped.NameOfYVariable)} (y) for a given value of {generator.DisplaySummaryValueSnippet(ModelTyped.NameOfXVariable)} (x)";
        switch (ModelTyped.CalculationMethod)
        {
            case RelationshipCalculationMethod.UseSpecifiedValues:
                output += " using the largest value specified less than the given x";
                break;
            case RelationshipCalculationMethod.Interpolation:
                output += " using linear interpolation of points immediately below and above x";
                break;
            default:
                break;
        }
        generator.AddBlockWithText(output);

        // draw chart
        if (ModelTyped.XValues is null || ModelTyped.XValues.Length == 0)
        {
            generator.AddBlockWithText("No x values provided", classString: "infoBanner error");
        }
        else
        {
            if (ModelTyped.YValues is null || ModelTyped.XValues.Length != ModelTyped.YValues.Length)
            {
                generator.AddBlockWithText("Number of x values does not equal number of y values", classString: "infoBanner error");
            }
            else
            {
                using (generator.OpenBlock("childgroupborder"))
                {
                    generator.AddBlockWithText($"{generator.DisplaySummaryValueSnippet(ModelTyped.Identifier)} is defined by the following (x,y) points:", "childgrouplabel");

                    using StringWriter htmlWriter = new();
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
                        fontColor: '[GraphAxisLabelColour]',
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
                        fontColor: '[GraphAxisLabelColour]',
                        labelString: '" + ModelTyped.NameOfYVariable + @"'
                    }");
                    }
                    htmlWriter.Write(@"}],
                        }
                        }
                    });
                    </script>");
                    generator.AddBlockWithText(htmlWriter.ToString(), styleString: "width:400px;height:200px;");
                }
            }
        }
    }
}
