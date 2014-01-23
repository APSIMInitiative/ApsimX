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
                    // Find root component.
                    Model rootComponent = this;
                    while (rootComponent.Parent != null)
                        rootComponent = rootComponent.Parent;

                    if (rootComponent == null || !(rootComponent is Simulations))
                        throw new Exception("Cannot find root component");

                    Simulations simulations = rootComponent as Simulations;
                    _DataStore = new DataStore();
                    _DataStore.Connect(Path.ChangeExtension(simulations.FileName, ".db"));
                }
                return _DataStore;
            }
        }
    }
}
