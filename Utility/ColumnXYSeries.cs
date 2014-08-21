using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OxyPlot.Series;
using OxyPlot;

namespace Utility
{
    class ColumnXYSeries : RectangleBarSeries
    {

        public ColumnXYSeries()
        {
            ColumnWidth = 0.01;
        }

        /// <summary>
        /// Gets the width of the column (as fraction of width of axis.)
        /// </summary>
        public double ColumnWidth { get; set; }

        /// <summary>
        /// Updates the data.
        /// </summary>
        protected override void UpdateData()
        {
            MovePointsToItems();
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
        public override void Render(IRenderContext rc, PlotModel model)
        {
            if (this.Items.Count == 0)
            {
                return;
            }

            // Let the base class draw the rectanges.
            base.Render(rc, model);
        }

        /// <summary>
        /// Create a series of RectangleBarItem objects in Items based on Points.
        /// </summary>
        private void MovePointsToItems()
        {
            if (ItemsSource != null)
            {
                double HalfBarWidth = XDataRange() * ColumnWidth / 2.0;

                HalfBarWidth = 0.40; // slightly less that half of 1.0 cartesian coordinate
                Items.Clear();
                foreach (DataPoint P in ItemsSource)
                {
                    double x0 = P.X - HalfBarWidth;
                    double x1 = P.X + HalfBarWidth;
                    Items.Add(new RectangleBarItem(x0, 0.0, x1, P.Y));
                }
            }
        }

        /// <summary>
        /// Calculate the range of X data i.e. max - min.
        /// </summary>
        private double XDataRange()
        {
            double range = 0;

            double Minimum = double.MaxValue;
            double Maximum = double.MinValue;
            foreach (DataPoint P in ItemsSource)
            {
                Minimum = System.Math.Min(Minimum, P.X);
                Maximum = System.Math.Max(Maximum, P.X);
                range = Maximum - Minimum;
            }
            return range;
        }
    }
}
