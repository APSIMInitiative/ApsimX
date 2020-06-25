namespace APSIM.Shared.Utilities
{
    using System;
    using OxyPlot;
    using OxyPlot.Axes;
    using OxyPlot.Series;

    /// <summary>
    /// A column series for graphing that doesn't need a category axis.
    /// </summary>
    public class ColumnXYSeries : RectangleBarSeries
    {
        /// <summary>Updates the data.</summary>
        protected override void UpdateData()
        {
            this.MovePointsToItems();
        }

        /// <summary>
        /// Renders the Series on the specified rendering context.
        /// </summary>
        /// <param name="rc">
        /// The rendering context.
        /// </param>
        /// <param name="model">
        /// The model.
        /// </param>
        public override void Render(IRenderContext rc)
        {
            this.MovePointsToItems();

            // Let the base class draw the rectanges.
            base.Render(rc);
        }

        /// <summary>
        /// Create a series of RectangleBarItem objects in Items based on Points.
        /// </summary>
        private void MovePointsToItems()
        {
            if (this.ItemsSource != null && this.XAxis != null)
            {
                // double halfBarWidth = this.XAxis.ActualMajorStep * 0.4;
                double halfBarWidth = 0.4;
                if (this.XAxis.ActualStringFormat != null && this.XAxis.ActualStringFormat.Contains("yyyy"))
                {
                    DateTime d1 = DateTimeAxis.ToDateTime(this.XAxis.ActualMinimum);
                    DateTime d2 = DateTimeAxis.ToDateTime(this.XAxis.ActualMinimum + 1);
                    halfBarWidth = (d2 - d1).Days / 2.0;
                }

                this.Items.Clear();
                foreach (DataPoint p in this.ItemsSource)
                {
                    double x0 = p.X - halfBarWidth;
                    double x1 = p.X + halfBarWidth;
                    this.Items.Add(new RectangleBarItem(x0, 0.0, x1, p.Y));
                }
            }
        }
    }
}
