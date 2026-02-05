using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models.Core;
using APSIM.Shared.Utilities;
using Models.Functions;
using System.Data;
using System;
using Markdig;
using SkiaSharp;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Documentation class for DocXYPairs
    /// </summary>
    public class DocXYPairs : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocXYPairs" /> class.
        /// </summary>
        public DocXYPairs(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override List<ITag> Document(int none = 0)
        {
            XYPairs xyPairs = model as XYPairs;

            DataTable table = new DataTable(model.Name);
            string xName = "X";
            string yName = "Y";
            if (model.Parent != null)
            {
                yName = model.Parent.Name;
                List<IModel> siblings = model.Parent.Children;
                foreach(IModel child in siblings)
                    if (child is VariableReference)
                        xName = (child as VariableReference).VariableName;
            }

            table.Columns.Add(xName, typeof(string));
            table.Columns.Add(yName, typeof(string));
            for (int i = 0; i < Math.Max(xyPairs.X.Length, xyPairs.Y.Length); i++)
            {
                DataRow row = table.NewRow();

                const double tolerance = 1e-9;
                const int minDecimalPlaces = 1; //minimum 1 decimal place
                const int maxDecimalPlaces = 6; //max 5 decimal places
                string xDigits = "F" + minDecimalPlaces.ToString();
                string yDigits = "F" + minDecimalPlaces.ToString();
                double xDeci = Math.Round(xyPairs.X[i], minDecimalPlaces);
                double yDeci = Math.Round(xyPairs.Y[i], minDecimalPlaces);
                for (int j = minDecimalPlaces + 1; j <= maxDecimalPlaces; j++)
                {
                    if (Math.Abs(Math.Round(xyPairs.X[i], j) - xDeci) > tolerance)
                    {
                        xDigits = "F" + j.ToString();
                        xDeci = Math.Round(xyPairs.X[i], j);
                    }
                    if (Math.Abs(Math.Round(xyPairs.Y[i], j) - yDeci) > tolerance)
                    {
                        yDigits = "F" + j.ToString();
                        yDeci = Math.Round(xyPairs.Y[i], j);
                    }
                }

                row[0] = i <= xyPairs.X.Length - 1 ? xyPairs.X[i].ToString(xDigits) : "";
                row[1] = i <= xyPairs.Y.Length - 1 ? xyPairs.Y[i].ToString(yDigits) : "";
                table.Rows.Add(row);
            }

            Table tableTag = new Table(table);
            string tableMarkdown = WebDocs.ConvertToMarkdown(new List<ITag> {tableTag}, "", xyPairs);

            SKImage graphImage = WebDocs.GetGraphImage(CreateGraph(), 600, 600);
            string imgMarkdown = WebDocs.GetMarkdownImageFromSKImage(graphImage);

            string markdown = imgMarkdown + "\n" + tableMarkdown;

            MarkdownPipeline pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
            string html = Markdown.ToHtml(markdown, pipeline);
            html = html.Replace("<p>", "");
            html = html.Replace("</p>", "");

            html = WebDocs.AddCSSClasses(html);
            html = html.Replace("<img ", "<img style=\"width:40%;float:left;padding:0px;padding-left:6%;padding-right:4%\" ");
            html = html.Replace("<table ", "<table style=\"width:40%;float:left;margin-top:60px\" ");

            html = $"<div class=\"docs-table-container\" style=\"margin-top:-50px\">\n{html}</div>\n";

            return new List<ITag> {new Paragraph(html)};
        }

        private Graph CreateGraph()
        {
            XYPairs xyPairs = model as XYPairs;
            var series = new APSIM.Shared.Graphing.Series[1];
            VariableReference XVariable = (VariableReference) xyPairs.Parent.Children.Find(m => m.Name == "XValue");
            string xName = XVariable.VariableName;
            string yName = xyPairs.Parent.Name;
            string parentName = "";

            if (model.Parent != null)
                parentName = model.Parent.Name;

            series[0] = new APSIM.Shared.Graphing.LineSeries(parentName,
                ColourUtilities.ChooseColour(4),
                false,
                xyPairs.X,
                xyPairs.Y,
                new APSIM.Shared.Graphing.Line(APSIM.Shared.Graphing.LineType.Solid, APSIM.Shared.Graphing.LineThickness.Normal),
                new APSIM.Shared.Graphing.Marker(APSIM.Shared.Graphing.MarkerType.None, APSIM.Shared.Graphing.MarkerSize.Normal, 1),
                xName,
                yName
            );
            var xAxis = new APSIM.Shared.Graphing.Axis(xName, APSIM.Shared.Graphing.AxisPosition.Bottom, false, false);
            var yAxis = new APSIM.Shared.Graphing.Axis(yName, APSIM.Shared.Graphing.AxisPosition.Left, false, false);
            var legend = new APSIM.Shared.Graphing.LegendConfiguration(APSIM.Shared.Graphing.LegendOrientation.Vertical, APSIM.Shared.Graphing.LegendPosition.TopLeft, true);
            return new Graph("", xyPairs.FullPath, series, xAxis, yAxis, legend);
        }
    }
}
