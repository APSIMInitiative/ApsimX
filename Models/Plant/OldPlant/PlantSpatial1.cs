using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using System.Xml.Serialization;

namespace Models.PMF.OldPlant
{
    /// <summary>
    /// Plant spatial model
    /// </summary>
    [Serializable]
    public class PlantSpatial1 : Model
    {
        /// <summary>The plant</summary>
        [Link]
        Plant15 Plant = null;

        /// <summary>The _ canopy width</summary>
        double _CanopyWidth = 0;

        /// <summary>Gets or sets the density.</summary>
        /// <value>The density.</value>
        [XmlIgnore]
        public double Density { get { return Plant.SowingData.Population; } set { Plant.SowingData.Population = value; } }
        /// <summary>Gets the skip row factor.</summary>
        /// <value>The skip row factor.</value>
        private double SkipRowFactor { get { return (2.0 + Plant.SowingData.SkipRow) / 2.0; } }
        /// <summary>Gets or sets the width of the canopy.</summary>
        /// <value>The width of the canopy.</value>
        [XmlIgnore]
        public double CanopyWidth { get { return _CanopyWidth; } set { _CanopyWidth = value; } }
        /// <summary>Gets the row spacing.</summary>
        /// <value>The row spacing.</value>
        public double RowSpacing 
        { 
            get 
            {
                if (Plant.SowingData != null)
                    return Plant.SowingData.RowSpacing;
                else
                    return 0;
            } 
        }
        /// <summary>Gets the canopy factor.</summary>
        /// <value>The canopy factor.</value>
        /// <exception cref="System.NotImplementedException"></exception>
        public double CanopyFactor
        {
            get
            {
                if (CanopyWidth > 0.0)
                    throw new NotImplementedException();
                else
                    return SkipRowFactor;
            }
        }

    }
}
