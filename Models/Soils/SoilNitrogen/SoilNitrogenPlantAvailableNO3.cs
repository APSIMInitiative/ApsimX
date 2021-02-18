namespace Models.Soils
{
    using Interfaces;
    using System;
    using Models.Core;
    using Newtonsoft.Json;
    using Models.Soils.Nutrients;
    using APSIM.Shared.Utilities;

    /// <summary>This class encapsulates a SoilNitrogen model 'PlantAvailableNO3' solute.</summary>
    [Serializable]
    [ValidParent(ParentType = typeof(SoilNitrogen))]
    public class SoilNitrogenPlantAvailableNO3 : Model, ISolute
    {
        [Link(Type = LinkType.Ancestor)]
        SoilNitrogen parent = null;

        /// <summary>Solute amount (kg/ha)</summary>
        [JsonIgnore]
        public double[] kgha
        {
            get
            {
                return parent.CalculatePlantAvailableNO3();
            }
            set
            {
                SetKgHa(SoluteSetterType.Plant, value);
            }
        }

        /// <summary>Solute amount (ppm)</summary>
        public double[] ppm { get { return SoilUtilities.kgha2ppm(parent.soilPhysical.Thickness, parent.soilPhysical.BD, kgha); } }

        /// <summary>Setter for kgha.</summary>
        /// <param name="callingModelType">Type of calling model.</param>
        /// <param name="value">New values.</param>
        public void SetKgHa(SoluteSetterType callingModelType, double[] value)
        {
            parent.SetPlantAvailableNO3(callingModelType, value);
        }

        /// <summary>Setter for kgha delta.</summary>
        /// <param name="callingModelType">Type of calling model</param>
        /// <param name="delta">New delta values</param>
        public void AddKgHaDelta(SoluteSetterType callingModelType, double[] delta)
        {
            throw new NotImplementedException("should not be trying to set plant available no3");
        }
    }
}
