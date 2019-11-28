namespace APSIM.Shared.APSoil
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;

    /// <summary>The soil class encapsulates a soil characterisation and 0 or more soil samples.</summary>
    [Serializable]
    public class Soil
    {
        /// <summary>Gets or sets the name.</summary>
        [XmlAttribute("name")]
        public string Name { get; set; }

        /// <summary>Gets or sets the record number.</summary>
        public int RecordNumber { get; set; }

        /// <summary>Gets or sets the asc order.</summary>
        public string ASCOrder { get; set; }

        /// <summary>Gets or sets the asc sub order.</summary>
        public string ASCSubOrder { get; set; }

        /// <summary>Gets or sets the type of the soil.</summary>
        public string SoilType { get; set; }

        /// <summary>Gets or sets the name of the local.</summary>
        public string LocalName { get; set; }

        /// <summary>Gets or sets the site.</summary>
        public string Site { get; set; }

        /// <summary>Gets or sets the nearest town.</summary>
        public string NearestTown { get; set; }

        /// <summary>Gets or sets the region.</summary>
        public string Region { get; set; }

        /// <summary>Gets or sets the state.</summary>
        public string State { get; set; }

        /// <summary>Gets or sets the country.</summary>
        public string Country { get; set; }

        /// <summary>Gets or sets the natural vegetation.</summary>
        public string NaturalVegetation { get; set; }

        /// <summary>Gets or sets the apsoil number.</summary>
        public string ApsoilNumber { get; set; }

        /// <summary>Gets or sets the latitude.</summary>
        public double Latitude { get; set; }

        /// <summary>Gets or sets the longitude.</summary>
        public double Longitude { get; set; }

        /// <summary>Gets or sets the location accuracy.</summary>
        public string LocationAccuracy { get; set; }

        /// <summary>Gets or sets the year of sampling.</summary>
        public int YearOfSampling { get; set; }

        /// <summary>Gets or sets the data source.</summary>
        public string DataSource { get; set; }

        /// <summary>Gets or sets the comments.</summary>
        public string Comments { get; set; }

        /// <summary>Gets or sets the water.</summary>
        public Water Water { get; set; }

        /// <summary>Gets or sets the soil water.</summary>
        public SoilWater SoilWater { get; set; }

        /// <summary>Gets or sets the soil organic matter.</summary>
        public SoilOrganicMatter SoilOrganicMatter { get; set; }

        /// <summary>Gets or sets the analysis.</summary>
        public Analysis Analysis { get; set; }

        /// <summary>Gets or sets the samples.</summary>
        [XmlElement("Sample")]
        public List<Sample> Samples { get; set; }
    }
}
