using System.Xml.Serialization;
using Models.Core;
using System.Collections.Generic;
using System;
using System.IO;
using System.Collections;

namespace Models.Graph
{
    [ViewName("UserInterface.Views.GraphView")]
    [PresenterName("UserInterface.Presenters.GraphPresenter")]
    [Serializable]
    public class Graph : Model
    {
        [NonSerialized] private DataStore _DataStore = null;

        public string Title {get; set;}

        public string Footer { get; set; }

        [XmlElement("Axis")]
        public List<Axis> Axes { get; set; }

        [XmlElement("Series")]
        public List<Series> Series { get; set; }

        public enum LegendPositionType { TopLeft, TopRight, BottomLeft, BottomRight };
        public LegendPositionType LegendPosition { get; set; }

        /// <summary>
        /// Destructor. Get rid of DataStore
        /// </summary>
        ~Graph()
        {
            if (_DataStore != null)
                _DataStore.Disconnect();
            _DataStore = null;
        }

        public IEnumerable GetValidFieldNames(GraphValues graphValues)
        {
            return graphValues.ValidFieldNames(this);
        }


        
        /// <summary>
        /// Return an instance of the datastore. Creates it if it doesn't exist.
        /// </summary>
        public DataStore DataStore
        {
            get
            {
                if (_DataStore == null)
                    _DataStore = new DataStore(this);
                return _DataStore;
            }
        }
    }
}
