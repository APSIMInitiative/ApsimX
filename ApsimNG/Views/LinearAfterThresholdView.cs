using System;
using Gtk;
using UserInterface.Extensions;
using UserInterface.Interfaces;

namespace UserInterface.Views
{
    /// <summary>
    /// This view displays a property UI above a graph, with a splitter in between.
    /// </summary>
    public class LinearAfterThresholdView : ViewBase
    {
        private PropertyView properties;
        private GraphView graph;
        private Paned panel;

        public IPropertyView Properties
        {
            get
            {
                return properties;
            }
        }

        public IGraphView Graph
        {
            get
            {
                return graph;
            }
        }

        /// <summary>
        /// Creates a LinearAfterThresholdView instance.
        /// </summary>
        /// <param name="owner">Owner view.</param>
        public LinearAfterThresholdView(ViewBase owner) : base(owner)
        {
            properties = new PropertyView(this);
            graph = new GraphView(this);
#if NETFRAMEWORK
            panel = new VPaned();
#else
            panel = new Paned(Orientation.Vertical);
#endif
            panel.Pack1(properties.MainWidget, false, false);
            panel.Pack2(graph.MainWidget, true, false);
            panel.Destroyed += OnDestroyed;
            mainWidget = panel;
            mainWidget.ShowAll();
        }

        private void OnDestroyed(object sender, EventArgs e)
        {
            try
            {
                panel.Cleanup();
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }
    }
}
