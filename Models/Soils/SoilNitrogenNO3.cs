namespace Models.Soils
{
    using Interfaces;
    using System;

    /// <summary>This class encapsulates a SoilNitrogen model NO3 solute.</summary>
    [Serializable]
    public class SoilNitrogenNO3 : ISolute
    {
        SoilNitrogen parent = null;

        /// <summary>Name of solute.</summary>
        public string Name { get { return "NO3"; } }

        /// <summary>Solute amount (kg/ha)</summary>
        public double[] kgha
        {
            get
            {
                return parent.NO3;
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
        public SoilNitrogenNO3(SoilNitrogen nitrogen)
        {
            parent = nitrogen;
        }

        /// <summary>Setter for kgha.</summary>
        /// <param name="callingModelType">Type of calling model.</param>
        /// <param name="value">New values.</param>
        public void SetKgHa(SoluteManager.SoluteSetterType callingModelType, double[] value)
        {
            parent.SetNO3(callingModelType, value);
        }

        /// <summary>Setter for kgha delta.</summary>
        /// <param name="callingModelType">Type of calling model</param>
        /// <param name="delta">New delta values</param>
        public void SetKgHaDelta(SoluteManager.SoluteSetterType callingModelType, double[] delta)
        {
            parent.SetNO3Delta(callingModelType, delta);
        }
    }
}
