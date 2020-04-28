namespace UserInterface.Presenters
{
    using Models.Functions;
    using Models;
    using System;
    using System.Collections.Generic;
    using Views;

    /// <summary>
    /// Presenter for the <see cref="LinearAfterThresholdFunction"/> class.
    /// </summary>
    public class LinearAfterThresholdPresenter : IPresenter
    {
        /// <summary>
        /// The function.
        /// </summary>
        private LinearAfterThresholdFunction function;

        /// <summary>
        /// The property presenter. Handles the display of the model's properties.
        /// </summary>
        private PropertyPresenter propertiesPresenter;

        /// <summary>
        /// The view which displays the properties and graph.
        /// </summary>
        private XYPairsView view;

        /// <summary>
        /// The explorer presenter controlling this presenter.
        /// </summary>
        private ExplorerPresenter presenter;

        /// <summary>
        /// Attaches the view to the model.
        /// </summary>
        /// <param name="model">The linear after threshold model.</param>
        /// <param name="viewObject">The view  - should be an XYPairsView.</param>
        /// <param name="parent">The explorer presenter for this tab.</param>
        public void Attach(object model, object viewObject, ExplorerPresenter parent)
        {
            function = model as LinearAfterThresholdFunction;
            if (function == null)
                throw new ArgumentException(string.Format("Attempted to display a model of type {0} using the LinearAfterThresholdPresenter.", model.GetType().ToString()));

            view = viewObject as XYPairsView;
            if (view == null)
                throw new ArgumentException(string.Format("Attempted to use a view of type {0} from the LinearAfterThresholdPresenter. View should be an XYPairsView.", viewObject.GetType().ToString()));

            presenter = parent;
            propertiesPresenter = new PropertyPresenter();
            propertiesPresenter.Attach(function, view.VariablesGrid, presenter);
            DrawGraph();
            view.VariablesGrid.CellsChanged += OnCellsChanged;
        }

        /// <summary>
        /// Detaches the view from the model.
        /// </summary>
        public void Detach()
        {
            view.VariablesGrid.CellsChanged -= OnCellsChanged;
            view.VariablesGrid.Dispose();
        }
        
        /// <summary>
        /// Draws the graph on the view.
        /// </summary>
        private void DrawGraph()
        {
            int xMin, xMax;
            if (function.XTrigger == 0)
            {
                xMin = -1;
                xMax = 1;
            }
            else
            {
                xMin = function.XTrigger < 0 ? (int)Math.Floor(function.XTrigger * 2) : 0;
                xMax = function.XTrigger < 0 ? 0 : (int)Math.Ceiling(function.XTrigger * 2);
            }

            List<double> x = new List<double>();
            List<double> y = new List<double>();
            double increment = (xMax - xMin) / 1000d;
            if (increment < 0)
                // Don't see how this could be possible, but it doesn't hurt to take precautions.
                increment *= -1;

            for (double xValue = xMin; xValue <= xMax; xValue += increment)
            {
                x.Add(xValue);
                y.Add(xValue <= function.XTrigger ? 0 : xValue * function.Slope);
            }

            view.Graph.Clear();
            view.Graph.DrawLineAndMarkers("", x, y, null, null, null, Axis.AxisType.Bottom, Axis.AxisType.Left, System.Drawing.Color.Blue, LineType.Solid, MarkerType.None, LineThicknessType.Normal, MarkerSizeType.Normal, 1, true);
            view.Graph.FormatAxis(Axis.AxisType.Bottom, "x", false, double.NaN, double.NaN, double.NaN, false);
            view.Graph.FormatAxis(Axis.AxisType.Left, "y", false, double.NaN, double.NaN, double.NaN, false);
            view.Graph.BackColor = OxyPlot.OxyColors.White;
            view.Graph.FontSize = 10;
            view.Graph.Refresh();
        }

        /// <summary>
        /// Invoked when any of the cells in the grid are modified.
        /// Redraws the graph to reflect these changes.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnCellsChanged(object sender, EventArgs args)
        {
            DrawGraph();
        }
    }
}
