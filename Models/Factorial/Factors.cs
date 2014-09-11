using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using System.Xml.Serialization;

namespace Models.Factorial
{
    [Serializable]
    [ValidParent(typeof(Experiment))]
    public class Factors : Model
    {
        [XmlIgnore]
        public List<IModel> factors { get { return Apsim.Children(this, typeof(Factor)); } }
    }
}
