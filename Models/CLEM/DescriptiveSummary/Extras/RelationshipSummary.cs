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
                    // Start without any leading spaces so the first tag (canvas/script) begins at column 0.
                    // Use \t for indentation inside the script so that formatting uses tabs.
                    htmlWriter.Write("<canvas id=\"myChart_" + ModelTyped.FullPath + "\"><p>Unable to display graph in browser</p></canvas>");
                    htmlWriter.WriteLine();
                    htmlWriter.WriteLine("<script>");
                    htmlWriter.WriteLine("\tvar ctx = document.getElementById('myChart_" + ModelTyped.FullPath + "').getContext('2d');");
                    htmlWriter.WriteLine("\tvar myChart = new Chart(ctx, {");
                    htmlWriter.WriteLine("\t\tresponsive:false,");
                    htmlWriter.WriteLine("\t\tmaintainAspectRatio: true,");
                    htmlWriter.WriteLine("\t\ttype: 'scatter',");
                    htmlWriter.WriteLine("\t\tdata: {");
                    htmlWriter.WriteLine("\t\t\tdatasets: [{");
                    htmlWriter.WriteLine("\t\t\t\tdata: [");

                    // build the data points with tabs for indentation inside the script
                    var dataSb = new System.Text.StringBuilder();
                    for (int i = 0; i < ModelTyped.XValues.Length; i++)
                    {
                        if (ModelTyped.YValues.Length > i)
                        {
                            dataSb.Append("\t\t\t\t\t{ x: " + ModelTyped.XValues[i].ToString() + ", y: " + ModelTyped.YValues[i] + "},");
                            dataSb.AppendLine();
                        }
                    }

                    string data = dataSb.ToString().TrimEnd(',', '\r', '\n');
                    if (!string.IsNullOrEmpty(data))
                    {
                        htmlWriter.Write(data);
                        htmlWriter.WriteLine();
                    }

                    htmlWriter.WriteLine("\t\t\t\t],");
                    htmlWriter.WriteLine("\t\t\t\tpointBackgroundColor: '[GraphPointColour]',");
                    htmlWriter.WriteLine("\t\t\t\tpointBorderColor: '[GraphPointColour]',");
                    htmlWriter.WriteLine("\t\t\t\tborderColor: '[GraphLineColour]',");
                    htmlWriter.WriteLine("\t\t\t\tpointRadius: 5,");
                    htmlWriter.WriteLine("\t\t\t\tpointHoverRadius: 5,");
                    htmlWriter.WriteLine("\t\t\t\tfill: false,");
                    htmlWriter.WriteLine("\t\t\t\ttension: 0,");
                    htmlWriter.WriteLine("\t\t\t\tshowLine: true,");
                    htmlWriter.WriteLine("\t\t\t\tsteppedLine: " + (ModelTyped.CalculationMethod == RelationshipCalculationMethod.UseSpecifiedValues).ToString().ToLower() + ",");
                    htmlWriter.WriteLine("\t\t\t}]");
                    htmlWriter.WriteLine("\t\t},");
                    htmlWriter.WriteLine("\t\toptions: {");
                    htmlWriter.WriteLine("\t\t\tlegend: {");
                    htmlWriter.WriteLine("\t\t\t\tdisplay: false");
                    htmlWriter.WriteLine("\t\t\t},");
                    htmlWriter.WriteLine("\t\t\tscales: {");
                    htmlWriter.WriteLine("\t\t\t\txAxes: [{");
                    htmlWriter.WriteLine("\t\t\t\t\tcolor: 'green',");
                    htmlWriter.WriteLine("\t\t\t\t\ttype: 'linear',");
                    htmlWriter.WriteLine("\t\t\t\t\tposition: 'bottom',");
                    htmlWriter.WriteLine("\t\t\t\t\tticks: {");
                    htmlWriter.WriteLine("\t\t\t\t\t\tfontColor: '[GraphLabelColour]',");
                    htmlWriter.WriteLine("\t\t\t\t\t\tfontSize: 13,");
                    htmlWriter.WriteLine("\t\t\t\t\t\tpadding: 3");
                    htmlWriter.WriteLine("\t\t\t\t\t},");
                    htmlWriter.WriteLine("\t\t\t\t\tgridLines: {");
                    htmlWriter.WriteLine("\t\t\t\t\t\tcolor: '[GraphGridLineColour]',");
                    htmlWriter.WriteLine("\t\t\t\t\t\tdrawOnChartArea: true");
                    htmlWriter.WriteLine("\t\t\t\t\t}");

                    if (!string.IsNullOrEmpty(ModelTyped.NameOfXVariable))
                    {
                        htmlWriter.WriteLine("\t\t\t\t\t,");
                        htmlWriter.WriteLine("\t\t\t\t\tscaleLabel: {");
                        htmlWriter.WriteLine("\t\t\t\t\t\tdisplay: true,");
                        htmlWriter.WriteLine("\t\t\t\t\t\tfontColor: '[GraphAxisLabelColour]',");
                        htmlWriter.WriteLine("\t\t\t\t\t\tlabelString: '" + ModelTyped.NameOfXVariable + "'");
                        htmlWriter.WriteLine("\t\t\t\t\t}");
                    }

                    htmlWriter.WriteLine("\t\t\t\t}],");
                    htmlWriter.WriteLine("\t\t\t\tyAxes: [{");
                    htmlWriter.WriteLine("\t\t\t\t\ttype: 'linear',");
                    htmlWriter.WriteLine("\t\t\t\t\tgridLines: {");
                    htmlWriter.WriteLine("\t\t\t\t\t\tzeroLineColor: '[GraphGridZeroLineColour]',");
                    htmlWriter.WriteLine("\t\t\t\t\t\tzeroLineWidth: 1,");
                    htmlWriter.WriteLine("\t\t\t\t\t\tzeroLineBorderDash: [3, 3],");
                    htmlWriter.WriteLine("\t\t\t\t\t\tcolor: '[GraphGridLineColour]',");
                    htmlWriter.WriteLine("\t\t\t\t\t\tdrawOnChartArea: true");
                    htmlWriter.WriteLine("\t\t\t\t\t},");
                    htmlWriter.WriteLine("\t\t\t\t\tticks: {");
                    htmlWriter.WriteLine("\t\t\t\t\t\tfontColor: '[GraphLabelColour]',");
                    htmlWriter.WriteLine("\t\t\t\t\t\tfontSize: 13,");
                    htmlWriter.WriteLine("\t\t\t\t\t\tpadding: 3");
                    htmlWriter.WriteLine("\t\t\t\t\t}");

                    if (!string.IsNullOrEmpty(ModelTyped.NameOfYVariable))
                    {
                        htmlWriter.WriteLine("\t\t\t\t\t, scaleLabel: {");
                        htmlWriter.WriteLine("\t\t\t\t\t\tdisplay: true,");
                        htmlWriter.WriteLine("\t\t\t\t\t\tfontColor: '[GraphAxisLabelColour]',");
                        htmlWriter.WriteLine("\t\t\t\t\t\tlabelString: '" + ModelTyped.NameOfYVariable + "'");
                        htmlWriter.WriteLine("\t\t\t\t\t}");
                    }

                    htmlWriter.WriteLine("\t\t\t\t}],");
                    htmlWriter.WriteLine("\t\t\t}");
                    htmlWriter.WriteLine("\t\t}");
                    htmlWriter.WriteLine("\t});");
                    htmlWriter.WriteLine("</script>");

                    // Add 8 tabs to the start of each line before adding to generator, but keep the first non-empty line unindented
                    string html = htmlWriter.ToString();
                    string indent = new string('\t', 8);
                    string[] lines = html.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
                    var indentedBuilder = new System.Text.StringBuilder();
                    bool firstNonEmptyLine = true;
                    for (int li = 0; li < lines.Length; li++)
                    {
                        if (li > 0)
                        {
                            indentedBuilder.Append(Environment.NewLine);
                        }

                        if (firstNonEmptyLine && !string.IsNullOrWhiteSpace(lines[li]))
                        {
                            // Keep the first non-empty line without additional indent (so <canvas> / <script> start at column 0)
                            indentedBuilder.Append(lines[li]);
                            firstNonEmptyLine = false;
                        }
                        else
                        {
                            indentedBuilder.Append(indent);
                            indentedBuilder.Append(lines[li]);
                        }
                    }

                    generator.AddBlockWithText(indentedBuilder.ToString(), styleString: "width:400px;height:200px;");
                }
            }
        }
    }
}
