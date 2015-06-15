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
    public class Graph : Model
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
        /// Gets or sets the title of the graph
        /// </summary>
        public string Title { get; set; }

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
        [XmlElement("Series")]
        public List<Series> Series { get; set; }

        /// <summary>
        /// Gets or sets the location of the legend
        /// </summary>
        public LegendPositionType LegendPosition { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether an overall regression line and stats for all series?
        /// </summary>
        public bool ShowRegressionLine { get; set; }

        /// <summary>
        /// Gets or sets a list of raw grpah series that should be disabled.
        /// </summary>
        public List<string> DisabledSeries { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the graph should be included in the auto-doc documentation.
        /// </summary>
        public bool IncludeInDocumentation { get; set; }

        /// <summary>
        /// Gets an instance of the data store. Creates it if it doesn't exist.
        /// </summary>
        public DataStore DataStore
        {
            get
            {
                if (this.dataStore == null)
                    this.dataStore = new DataStore(this);
                return this.dataStore;
            }
        }

        /// <summary>
        /// Get a list of valid field names
        /// </summary>
        /// <param name="graphValues">The graph values object</param>
        /// <returns>The list of field names that are valid</returns>
        public IEnumerable GetValidFieldNames(GraphValues graphValues)
        {
            return graphValues.ValidFieldNames(this);
        }
    }
}
