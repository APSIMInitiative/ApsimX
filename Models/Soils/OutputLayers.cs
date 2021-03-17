namespace Models.Soils
{
    using Models.Core;
    using Models.Interfaces;
    using Models.Soils.Nutrients;
    using Models.Soils.Standardiser;
    using System;
    using Newtonsoft.Json;

    /// <summary>
    /// This class takes soil variables simulated at each of the modelled soil layers and maps them onto a new specified layering.
    /// The outputs can be used for producing summaries and rearrange outputs to facilitate comparison with observed data.
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Soil))]
    [ViewName("UserInterface.Views.ProfileView")]
    [PresenterName("UserInterface.Presenters.ProfilePresenter")]
    public class OutputLayers : Model
    {
        /// <summary>Access the soil physical properties.</summary>
        [Link] 
        private IPhysical soilPhysicalProperties = null;
        
        /// <summary>Access the soil water model.</summary>
        [Link]
        private ISoilWater waterBalanceModel = null;

        [Link]
        private INutrient nutrientBalanceModel = null;


        /// <summary>Gets or sets the thickness of each layer.</summary>
        [Description("Depth (mm)")]
        public double[] Thickness { get; set; }

        ///<summary>Gets the soil bulk density of each mapped layer.</summary>
        [JsonIgnore]
        [Units("g/cm3")]
        public double[] BD
        {
            get { return Layers.MapConcentration(soilPhysicalProperties.BD, waterBalanceModel.Thickness, Thickness, double.NaN); }
        }

        ///<summary>Gets the current soil water content of each mapped layer</summary>
        [JsonIgnore]
        [Units("mm/mm")]
        public double[] SW
        {
            get { return Layers.MapConcentration(waterBalanceModel.SW, waterBalanceModel.Thickness, Thickness, double.NaN); }
        }

        ///<summary>Gets the current soil water amount of each mapped layer.</summary>
        [JsonIgnore]
        [Units("mm")]
        public double[] SWmm
        {
            get { return Layers.MapMass(waterBalanceModel.SWmm, waterBalanceModel.Thickness, Thickness); }
        }

        ///<summary>Gets the plant available water amount of each mapped layer.</summary>
        [JsonIgnore]
        [Units("mm/mm")]
        public double[] PAW
        {
            get { return Layers.MapConcentration(waterBalanceModel.PAW, waterBalanceModel.Thickness, Thickness, double.NaN); }
        }

        ///<summary>Gets the plant available water amount of each mapped layer.</summary>
        [JsonIgnore]
        [Units("mm")]
        public double[] PAWmm
        {
            get { return Layers.MapMass(waterBalanceModel.PAWmm, waterBalanceModel.Thickness, Thickness); }
        }

        ///<summary>Gets the soil water content at the lower limit of each mapped layer</summary>
        [JsonIgnore]
        [Units("mm/mm")]
        public double[] LL15
        {
            get { return Layers.MapConcentration(soilPhysicalProperties.LL15, waterBalanceModel.Thickness, Thickness, double.NaN); }
        }

        ///<summary>Gets the soil water amount at the lower limit of each mapped layer.</summary>
        [JsonIgnore]
        [Units("mm")]
        public double[] LL15mm
        {
            get { return Layers.MapMass(soilPhysicalProperties.LL15mm, waterBalanceModel.Thickness, Thickness); }
        }

        ///<summary>Gets the soil water content at the upper limit of each mapped layer</summary>
        [JsonIgnore]
        [Units("mm/mm")]
        public double[] DUL
        {
            get { return Layers.MapConcentration(soilPhysicalProperties.DUL, waterBalanceModel.Thickness, Thickness, double.NaN); }
        }

        ///<summary>Gets the soil water amount at the upper limit of each mapped layer.</summary>
        [JsonIgnore]
        [Units("mm")]
        public double[] DULmm
        {
            get { return Layers.MapMass(soilPhysicalProperties.DULmm, waterBalanceModel.Thickness, Thickness); }
        }

        ///<summary>Gets the soil water content at saturation of each mapped layer</summary>
        [JsonIgnore]
        [Units("mm/mm")]
        public double[] SAT
        {
            get { return Layers.MapConcentration(soilPhysicalProperties.SAT, waterBalanceModel.Thickness, Thickness, double.NaN); }
        }

        ///<summary>Gets the soil water amount at saturation of each mapped layer.</summary>
        [JsonIgnore]
        [Units("mm")]
        public double[] SATmm
        {
            get { return Layers.MapMass(soilPhysicalProperties.SATmm, waterBalanceModel.Thickness, Thickness); }
        }

        ///<summary>Gets the soil urea N content of each mapped layer.</summary>
        [JsonIgnore]
        [Units("kg/ha")]
        public double[] Urea
        {
            get { return Layers.MapMass(nutrientBalanceModel.Urea.kgha, waterBalanceModel.Thickness, Thickness); }
        }

        ///<summary>Gets the soil urea N concentration of each mapped layer.</summary>
        [JsonIgnore]
        [Units("ppm")]
        public double[] Ureappm
        {
            get { return Layers.MapConcentration(nutrientBalanceModel.Urea.kgha, waterBalanceModel.Thickness, Thickness, double.NaN); }
        }

        ///<summary>Gets the soil ammonium N content of each mapped layer.</summary>
        [JsonIgnore]
        [Units("kg/ha")]
        public double[] NH4
        {
            get { return Layers.MapMass(nutrientBalanceModel.NH4.kgha, waterBalanceModel.Thickness, Thickness); }
        }

        ///<summary>Gets the soil ammonium N concentration of each mapped layer.</summary>
        [JsonIgnore]
        [Units("ppm")]
        public double[] NH4ppm
        {
            get { return Layers.MapConcentration(nutrientBalanceModel.NH4.kgha, waterBalanceModel.Thickness, Thickness, double.NaN); }
        }

        ///<summary>Gets the soil nitrate N content of each mapped layer.</summary>
        [JsonIgnore]
        [Units("kg/ha")]
        public double[] NO3
        {
            get { return Layers.MapMass(nutrientBalanceModel.NO3.kgha, waterBalanceModel.Thickness, Thickness); }
        }

        ///<summary>Gets the soil nitrate N concentration of each mapped layer.</summary>
        [JsonIgnore]
        [Units("ppm")]
        public double[] NO3ppm
        {
            get { return Layers.MapConcentration(nutrientBalanceModel.NO3.kgha, waterBalanceModel.Thickness, Thickness, double.NaN); }
        }

        ///<summary>Gets the soil mineral N content of each mapped layer.</summary>
        [JsonIgnore]
        [Units("kg/ha")]
        public double[] MineralN
        {
            get { return Layers.MapMass(nutrientBalanceModel.MineralN, waterBalanceModel.Thickness, Thickness); }
        }

        ///<summary>Gets the soil organic N content of each mapped layer.</summary>
        [JsonIgnore]
        [Units("kg/ha")]
        public double[] OrganicN
        {
            get { return Layers.MapMass(nutrientBalanceModel.Organic.N, waterBalanceModel.Thickness, Thickness); }
        }

        ///<summary>Gets the soil fresh organic matter N content of each mapped layer.</summary>
        [JsonIgnore]
        [Units("kg/ha")]
        public double[] FOMN
        {
            get { return Layers.MapMass(nutrientBalanceModel.FOM.N, waterBalanceModel.Thickness, Thickness); }
        }

        ///<summary>Gets the soil microbial N content of each mapped layer.</summary>
        [JsonIgnore]
        [Units("kg/ha")]
        public double[] MicrobialN
        {
            get { return Layers.MapMass(nutrientBalanceModel.Microbial.N, waterBalanceModel.Thickness, Thickness); }
        }

        ///<summary>Gets the soil total humic N content of each mapped layer.</summary>
        [JsonIgnore]
        [Units("kg/ha")]
        public double[] HumicN
        {
            get { return Layers.MapMass(nutrientBalanceModel.Humic.N, waterBalanceModel.Thickness, Thickness); }
        }

        ///<summary>Gets the soil inert humic N content of each mapped layer.</summary>
        [JsonIgnore]
        [Units("kg/ha")]
        public double[] InertN
        {
            get { return Layers.MapMass(nutrientBalanceModel.Inert.N, waterBalanceModel.Thickness, Thickness); }
        }

        ///<summary>Gets the soil fresh organic matter C content of each mapped layer.</summary>
        [JsonIgnore]
        [Units("kg/ha")]
        public double[] FOMC
        {
            get { return Layers.MapMass(nutrientBalanceModel.FOM.C, waterBalanceModel.Thickness, Thickness); }
        }

        ///<summary>Gets the soil microbial C content of each mapped layer.</summary>
        [JsonIgnore]
        [Units("kg/ha")]
        public double[] MicrobialC
        {
            get { return Layers.MapMass(nutrientBalanceModel.Microbial.C, waterBalanceModel.Thickness, Thickness); }
        }

        ///<summary>Gets the soil total humic C content of each mapped layer.</summary>
        [JsonIgnore]
        [Units("kg/ha")]
        public double[] HumicC
        {
            get { return Layers.MapMass(nutrientBalanceModel.Humic.C, waterBalanceModel.Thickness, Thickness); }
        }

        ///<summary>Gets the soil inert humic C content of each mapped layer.</summary>
        [JsonIgnore]
        [Units("kg/ha")]
        public double[] InertC
        {
            get { return Layers.MapMass(nutrientBalanceModel.Inert.C, waterBalanceModel.Thickness, Thickness); }
        }

        ///<summary>Gets the soil organic carbon content of each mapped layer.</summary>
        [JsonIgnore]
        [Units("%")]
        public double[] OrganicC
        {
            get { return Layers.MapMass(nutrientBalanceModel.Organic.C, waterBalanceModel.Thickness, Thickness); }
        }

        ///<summary>Gets the soil organic carbon concentration of each mapped layer.</summary>
        [JsonIgnore]
        [Units("%")]
        public double[] OC
        {
            get
            {
                double[] modelOC = new double[waterBalanceModel.Thickness.Length];
                for (int layer = 0; layer < waterBalanceModel.Thickness.Length; ++layer)
                {
                    modelOC[layer] = (nutrientBalanceModel.Humic.C[layer] + nutrientBalanceModel.Microbial.C[layer])
                                   / (soilPhysicalProperties.BD[layer]*waterBalanceModel.Thickness[layer]) / 100.0;
                }

                return Layers.MapConcentration(modelOC, waterBalanceModel.Thickness, Thickness, double.NaN);
            }
        }
    }
}
