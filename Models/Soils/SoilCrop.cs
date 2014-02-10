using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Models.Core;

namespace Models.Soils
{
    [Serializable]
    public class SoilCrop : Model
    {
        [XmlIgnore]
        public Soil Soil { get; set; }

        [Units("mm/mm")]
        public double[] LL { get; set; }

        [DisplayTotal]
        [DisplayFormat("N1")]
        [Units("mm")]
        public double[] PAWC
        {
            get
            {
                return Utility.Math.Multiply(Soil.CalcPAWC(Soil.Thickness, LL, Soil.DUL, XF), Soil.Thickness);
            }
        }

        [DisplayFormat("N2")]
        [Units("mm/mm")]
        public double[] KL { get; set; }
        
        [DisplayFormat("N1")]
        [Units("mm/mm")]
        public double[] XF { get; set; }

        public string[] LLMetadata { get; set; }
        public string[] KLMetadata { get; set; }
        public string[] XFMetadata { get; set; }

    }
}