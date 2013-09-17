using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.IO;
using Model.Core;
using System.Xml.Serialization;
using Model.Components;

namespace Model.Components.Graph
{
    public class Series : Model.Core.Model
    {
        public enum SeriesType { Line, Bar };
        public string Title { get; set; }
        public SeriesType Type { get; set; }
        public string SimulationName { get; set; }
        public string TableName { get; set; }
        public string X { get; set; }
        public string Y { get; set; }
    }
}
