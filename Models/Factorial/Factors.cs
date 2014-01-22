using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using System.Xml.Serialization;

namespace Models.Factorial
{
    [Serializable]
    [AllowDropOn("Experiment")]
    public class Factors : ModelCollection
    {
        [XmlElement("Factor")]
        public List<Factor> factors { get; set; }
    }
}
