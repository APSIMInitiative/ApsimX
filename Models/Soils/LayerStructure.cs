using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Xml;
using Models.Core;

namespace Models.Soils
{
    [Serializable]
    public class LayerStructure : Model
    {
        public double[] Thickness { get; set; }
    }
}
