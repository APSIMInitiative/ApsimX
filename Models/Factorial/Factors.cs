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
    public class Factors : Model
    {
        [XmlIgnore]
        public Model[] factors { get { return Children.MatchingMultiple(typeof(Factor)); } }
    }
}
