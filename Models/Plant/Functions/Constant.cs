using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Models.Core;
using System.Xml.Serialization;

namespace Models.PMF.Functions
{
    [Serializable]
    public class Constant : Function
    {
        [XmlElement("Value")]
        public double value { get; set; }
        
        public override double Value { get { return value; } }
    }
}