namespace Models.Soils
{
    using Models.Core;
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// A class for storing NO3 and NH4 values and converting them
    /// from ppm to kgha
    /// </summary>
    [Serializable]
    public class NitrogenValue
    {
        /// <summary>The related soil.</summary>
        private Soil soil;

        /// <summary>The raw values.</summary>
        [JsonProperty]
        private double[] Values { get; set; }

        /// <summary>Are the values stored as PPM?</summary>
        [JsonProperty]
        public bool StoredAsPPM { get; private set; }

        /// <summary>Gets or sets the values as parts per million.</summary>
        [JsonIgnore]
        [Display(Format = "N2", ShowTotal = true)]
        [Units("ppm")]
        public double[] PPM
        {
            get
            {
                if (StoredAsPPM)
                    return Values;
                else
                {
                    // Convert Kg/Ha to PPM.
                    var values = (double[])Values.Clone();
                    for (int i = 0; i < values.Length; i++)
                        if (!Double.IsNaN(values[i]))
                            values[i] = values[i] * 100 / (soil.BD[i] * soil.Thickness[i]);
                    return values;
                }
            }
            set
            {
                Values = value;
                StoredAsPPM = true;
            }
        }

        /// <summary>Gets or sets the values as kg/ha.</summary>
        [JsonIgnore]
        [Display(Format = "N1", ShowTotal = true)]
        [Units("kg/ha")]
        public double[] KgHa
        {
            get
            {
                if (StoredAsPPM)
                {
                    // Convert PPM to Kg/Ha
                    var values = (double[])Values.Clone();
                    for (int i = 0; i < values.Length; i++)
                        if (!double.IsNaN(values[i]))
                            values[i] = values[i] / 100 * (soil.BD[i] * soil.Thickness[i]);
                    return values;
                }
                else
                    return Values;
            }
            set
            {
                Values = value;
                StoredAsPPM = false;
            }
        }

        /// <summary>Called when this object is created.</summary>
        /// <param name="relatedSoil">The related soil the helper belongs to.</param>
        public void OnCreated(Soil relatedSoil)
        {
            soil = relatedSoil;
        }
    }
}