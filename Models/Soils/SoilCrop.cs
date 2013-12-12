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
        [Units("mm/mm")]
        public double[] LL { get; set; }

        [DisplayTotal]
        [DisplayFormat("N1")]
        [Units("mm")]
        public double[] PAWC
        {
            get
            {
                Soil parentSoil = Parent.Parent as Soil;
                if (parentSoil == null)
                    return null;
                return Utility.Math.Multiply(Soil.CalcPAWC(parentSoil.Thickness, LL, parentSoil.DUL, XF), parentSoil.Thickness);
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