using System;
using System.Linq;
using APSIM.Interop.Graphing;
using APSIM.Interop.Markdown.Renderers;
using APSIM.Interop.Utility;
using APSIM.Shared.Documentation;

namespace APSIM.Interop.Documentation.Renderers
{
    /// <summary>
    /// A class which can use a <see cref="PdfBuilder" /> to render a
    /// <see cref="GraphPage" /> to a PDF document.
    /// </summary>
    /// <typeparam name="T">The type of tag which this class can render.</typeparam>
    internal class GraphPageTagRenderer : TagRendererBase<GraphPage>
    {
        /// <summary>
        /// The graph exporter to be used.
        /// </summary>
        private IGraphExporter exporter;

        /// <summary>
        /// Create a GraphTagRenderer with the default image exporter.
        /// </summary>
        public GraphPageTagRenderer() : this(new GraphExporter()) { }

        /// <summary>
        /// Create a GraphTagRenderer with a custom image exporter.
        /// </summary>
        /// <param name="exporter">Graph exporter to be used.</param>
        public GraphPageTagRenderer(IGraphExporter exporter)
        {
            this.exporter = exporter;
        }

        /// <summary>
        /// Render the given graph page to the PDF document.
        /// </summary>
        /// <param name="GraphPage">Graph page to be rendered.</param>
        /// <param name="renderer">PDF renderer to use for rendering the tag.</param>
        protected override void Render(GraphPage page, PdfBuilder renderer)
        {
            renderer.GetPageSize(out double width, out double height);
            // Let image width = half page width.
            width /= 2;
            // 6 graphs per page - 2 columns of 3 rows.
            // Therefore each graph gets 1/3 total page height.
            height /= 3;

            renderer.StartNewParagraph();
            foreach (IGraph graph in page.Graphs)
            {
                OxyPlot.IPlotModel model = exporter.ToPlotModel(graph);
                if (model is OxyPlot.PlotModel plot)
                {
                    FixSizing(ref plot);

                    plot.Legends.Clear();

                }

                renderer.AppendImage(exporter.Export(model, width, height/*width * 2 / 3*/));
            }
            renderer.StartNewParagraph();
        }

        private void FixSizing(ref OxyPlot.PlotModel model)
        {
            foreach (var series in model.Series.OfType<OxyPlot.Series.LineSeries>())
                series.MarkerSize = 2;
            model.DefaultFontSize = 10;
            foreach (var axis in model.Axes)
                axis.FontSize = 10;
            model.TitleFontSize = 10;
        }
    }
}
