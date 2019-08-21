namespace APSIM.Shared.APSoil
{
    using System;
    using System.Xml.Serialization;

    /// <summary>A soil crop parameterisation    /// </summary>
    [Serializable]
    public class SoilCrop
    {
        /// <summary>Name of the crop.</summary>
        [XmlAttribute("name")]
        public string Name { get; set; }
        
        /// <summary>The thickness.</summary>
        public double[] Thickness { get; set; }
        
        /// <summary>The crop lower limit.</summary>
        public double[] LL { get; set; }

        /// <summary>The crop root extraction factor</summary>
        public double[] KL { get; set; }

        /// <summary>The crop exploration factor.</summary>
        public double[] XF { get; set; }

        /// <summary>The crop lower limit metadata.</summary>
        public string[] LLMetadata { get; set; }

        /// <summary>The crop root extraction metadata.</summary>
        public string[] KLMetadata { get; set; }

        /// <summary>The crop exploration metadata.</summary>
        public string[] XFMetadata { get; set; }
    }
}