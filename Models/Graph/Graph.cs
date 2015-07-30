// -----------------------------------------------------------------------
// <copyright file="Graph.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.Graph
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Xml.Serialization;
    using Models.Core;

    /// <summary>
    /// Represents a graph
    /// </summary>
    [ViewName("UserInterface.Views.GraphView")]
    [PresenterName("UserInterface.Presenters.GraphPresenter")]
    [Serializable]
    public class Graph : Model, AutoDocumentation.ITag
    {
        /// <summary>
        /// The data store to use when retrieving data
        /// </summary>
        [NonSerialized] 
        private DataStore dataStore = null;

        /// <summary>
        /// Finalizes an instance of the <see cref="Graph" /> class./>
        /// </summary>
        ~Graph()
        {
            if (this.dataStore != null)
                this.dataStore.Disconnect();
            this.dataStore = null;
        }

        /// <summary>
        /// An enumeration for the position of the legend
        /// </summary>
        public enum LegendPositionType
        {
            /// <summary>
            /// Top left corner of the graph
            /// </summary>
            TopLeft,

            /// <summary>
            /// Top right corner of the graph
            /// </summary>
            TopRight,

            /// <summary>
            /// Bottom left corner of the graph
            /// </summary>
            BottomLeft,

            /// <summary>
            /// Bottom right corner of the graph
            /// </summary>
            BottomRight
        }

        /// <summary>
        /// Gets or sets the caption at the bottom of the graph
        /// </summary>
        public string Caption { get; set; }

        /// <summary>
        /// Gets or sets a list of all axes
        /// </summary>
        [XmlElement("Axis")]
        public List<Axis> Axes { get; set; }

        /// <summary>
        /// Gets or sets a list of all series
        /// </summary>
        [XmlIgnore]
        public List<IModel> Series { get { return Apsim.Children(this, typeof(Series)); } }

        /// <summary>
        /// Gets or sets the location of the legend
        /// </summary>
        public LegendPositionType LegendPosition { get; set; }

        /// <summary>
        /// Gets or sets a list of raw grpah series that should be disabled.
        /// </summary>
        public List<string> DisabledSeries { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the graph should be included in the auto-doc documentation.
        /// </summary>
        public bool IncludeInDocumentation { get; set; }

        /// <summary>Gets the definitions to graph.</summary>
        /// <returns>A list of series definitions.</returns>
        public List<SeriesDefinition> GetDefinitionsToGraph()
        {
            EnsureAllAxesExist();

            List<SeriesDefinition> definitions = new List<SeriesDefinition>();
            foreach (IGraphable series in Apsim.Children(this, typeof(IGraphable)))
                series.GetSeriesToPutOnGraph(definitions);

            return definitions;
        }

        /// <summary>Gets the annotations to graph.</summary>
        /// <returns>A list of series annotations.</returns>
        public List<Annotation> GetAnnotationsToGraph()
        {
            List<Annotation> annotations = new List<Annotation>();
            foreach (IGraphable series in Apsim.Children(this, typeof(IGraphable)))
                series.GetAnnotationsToPutOnGraph(annotations);

            return annotations;
        }

        /// <summary>
        /// Writes documentation for this function by adding to the list of documentation tags.
        /// </summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public override void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            if (IncludeInDocumentation)
                tags.Add(this);
        }

        /// <summary>
        /// Ensure that we have all necessary axis objects.
        /// </summary>
        private void EnsureAllAxesExist()
        {
            // Get a list of all axis types that are referenced by the series.
            List<Models.Graph.Axis.AxisType> allAxisTypes = new List<Models.Graph.Axis.AxisType>();
            foreach (Series series in Series)
            {
                allAxisTypes.Add(series.XAxis);
                allAxisTypes.Add(series.YAxis);
            }

            // Go through all graph axis objects. For each, check to see if it is still needed and
            // if so copy to our list.
            List<Axis> allAxes = new List<Axis>();
            bool unNeededAxisFound = false;
            foreach (Axis axis in Axes)
            {
                if (allAxisTypes.Contains(axis.Type))
                    allAxes.Add(axis);
                else
                    unNeededAxisFound = true;
            }

            // Go through all series and make sure an axis object is present in our AllAxes list. If
            // not then go create an axis object.
            bool axisWasAdded = false;
            foreach (Series S in Series)
            {
                Axis foundAxis = allAxes.Find(a => a.Type == S.XAxis);
                if (foundAxis == null)
                {
                    allAxes.Add(new Axis() { Type = S.XAxis });
                    axisWasAdded = true;
                }

                foundAxis = allAxes.Find(a => a.Type == S.YAxis);
                if (foundAxis == null)
                {
                    allAxes.Add(new Axis() { Type = S.YAxis });
                    axisWasAdded = true;
                }
            }

            if (unNeededAxisFound || axisWasAdded)
                Axes = allAxes;
        }

    }
}
