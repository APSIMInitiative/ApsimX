using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Model.Core;
using System.Xml.Serialization;

namespace Model.Components
{
    public class Test
    {
        public enum TestType { FailOutput, AllPos, GreaterThan, LessThan, Between, Mean, Tolerance };

        public string SimulationName { get; set; }
        public string TableName { get; set; }
        public TestType Type { get; set; }
        public string ColumnNames { get; set; }
        public string Parameters { get; set; }

    }

    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.TestPresenter")]
    public class Tests : Model.Core.Model
    {
        [Link] private DataStore _DataStore = null;

        [XmlElement("Test")]
        public Test[] AllTests { get; set; }

        public DataStore DataStore { get { return _DataStore; } }
    }
}
