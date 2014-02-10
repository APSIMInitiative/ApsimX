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
        [XmlIgnore]
        public List<Factor> factors { get { return ModelsMatching<Factor>(); } }
    }
}
