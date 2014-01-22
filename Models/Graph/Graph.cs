using System.Xml.Serialization;
using Models.Core;
using System.Collections.Generic;
using System;
using System.IO;

namespace Models.Graph
{
    [ViewName("UserInterface.Views.GraphView")]
    [PresenterName("UserInterface.Presenters.GraphPresenter")]
    [Serializable]
    public class Graph : Model
    {
        [NonSerialized] private DataStore _DataStore = null;
        [Link] private Simulation Simulation = null;

        public string Title {get; set;}

        [XmlElement("Axis")]
        public List<Axis> Axes { get; set; }

        [XmlElement("Series")]
        public List<Series> Series { get; set; }

        ~Graph()
        {
            if (_DataStore != null)
                _DataStore.Disconnect();
            _DataStore = null;
        }

        public DataStore DataStore
        {
            get
            {
                if (_DataStore == null)
                {
                    _DataStore = new DataStore();
                    _DataStore.Connect(Path.ChangeExtension(Simulation.FileName, ".db"));
                }
                return _DataStore;
            }
        }
    }
}
