using System.Xml.Serialization;
using Models.Core;
using System.Collections.Generic;
using System;

namespace Models.Graph
{
    [ViewName("UserInterface.Views.GraphView")]
    [PresenterName("UserInterface.Presenters.GraphPresenter")]
    public class Graph : Model
    {
        [Link] private DataStore _DataStore = null;

        public string Title {get; set;}

        [XmlElement("Axis")]
        public List<Axis> Axes { get; set; }

        [XmlElement("Series")]
        public List<Series> Series { get; set; }

        /// <summary>
        /// Return a list of visible datasets.
        /// </summary>
        public DataStore DataStore
        {
            get
            {
                return _DataStore;
            }
        }
    }
}
