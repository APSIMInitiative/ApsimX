using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Model.Core;

namespace Model.Components.Soils
{
    public class Phosphorus
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [Description("Root C:P ratio")]
        public double RootCP { get; set; }
        [Description("Rate disolved rock")]
        public double RateDissolRock { get; set; }
        [Description("Rate loss available")]
        public double RateLossAvail { get; set; }
        [Description("Sorption coefficient")]
        public double SorptionCoeff { get; set; }

        public double[] Thickness { get; set; }
        public double[] LabileP { get; set; }
        public double[] Sorption { get; set; }
        public double[] BandedP { get; set; }
        public double[] RockP { get; set; }
    }


}
