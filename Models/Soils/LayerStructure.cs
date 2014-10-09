using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Xml;
using Models.Core;

namespace Models.Soils
{
    /// <summary>
    /// A model for holding layer structure information
    /// </summary>
    [Serializable]
    public class LayerStructure : Model
    {
        /// <summary>Gets or sets the thickness.</summary>
        /// <value>The thickness.</value>
        public double[] Thickness { get; set; }
    }
}
