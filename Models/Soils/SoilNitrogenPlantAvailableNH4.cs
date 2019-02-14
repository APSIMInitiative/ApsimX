namespace Models.Soils
{
    using Interfaces;
    using System;

    /// <summary>This class encapsulates a SoilNitrogen model NH4 solute.</summary>
    [Serializable]
    public class SoilNitrogenPlantAvailableNH4 : ISolute
    {
        SoilNitrogen parent = null;

        /// <summary>Name of solute.</summary>
        public string Name { get { return "PlantAvailableNH4"; } }

        /// <summary>Solute amount (kg/ha)</summary>
        public double[] kgha
        {
            get
            {
                return parent.PlantAvailableNH4;
            }
            set
            {
                SetKgHa(SoluteManager.SoluteSetterType.Plant, value);
            }
        }

        /// <summary>Solute amount (ppm)</summary>
        public double[] ppm { get { return parent.Soil.kgha2ppm(kgha); } }

        /// <summary>Constructor.</summary>
        /// <param name="nitrogen">Parent soil nitrogen model.</param>
        public SoilNitrogenPlantAvailableNH4(SoilNitrogen nitrogen)
        {
            parent = nitrogen;
        }

        /// <summary>Setter for kgha.</summary>
        /// <param name="callingModelType">Type of calling model.</param>
        /// <param name="value">New values.</param>
        public void SetKgHa(SoluteManager.SoluteSetterType callingModelType, double[] value)
        {
            parent.SetPlantAvailableNH4(callingModelType, value);
        }


        /// <summary>Setter for kgha delta.</summary>
        /// <param name="callingModelType">Type of calling model</param>
        /// <param name="delta">New delta values</param>
        public void SetKgHaDelta(SoluteManager.SoluteSetterType callingModelType, double[] delta)
        {
            throw new NotImplementedException("should not be trying to set plant available nh4");
        }
    }
}
