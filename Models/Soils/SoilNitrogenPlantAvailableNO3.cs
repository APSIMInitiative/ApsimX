namespace Models.Soils
{
    using Interfaces;
    using System;

    /// <summary>This class encapsulates a SoilNitrogen model 'PlantAvailableNO3' solute.</summary>
    [Serializable]
    public class SoilNitrogenPlantAvailableNO3 : ISolute
    {
        SoilNitrogen parent = null;

        /// <summary>Name of solute.</summary>
        public string Name { get { return "PlantAvailableNO3"; } }

        /// <summary>Solute amount (kg/ha)</summary>
        public double[] kgha
        {
            get
            {
                return parent.PlantAvailableNO3;
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
        public SoilNitrogenPlantAvailableNO3(SoilNitrogen nitrogen)
        {
            parent = nitrogen;
        }

        /// <summary>Setter for kgha.</summary>
        /// <param name="callingModelType">Type of calling model.</param>
        /// <param name="value">New values.</param>
        public void SetKgHa(SoluteManager.SoluteSetterType callingModelType, double[] value)
        {
            parent.SetPlantAvailableNO3(callingModelType, value);
        }
    }
}
