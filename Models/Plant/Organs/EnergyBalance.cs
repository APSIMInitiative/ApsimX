namespace Models.PMF.Organs
{
    using Models.Core;
    using Models.Functions;
    using Models.Interfaces;
    using Models.PMF.Interfaces;
    using System;
    using System.Xml.Serialization;

    /// <summary>
    /// This organ is simulated using a  organ type.  It provides the core functions of intercepting radiation
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(GenericOrgan))]
    public class EnergyBalance : Model, ICanopy, IHasWaterDemand
    {
        /// <summary>The plant</summary>
        [Link]
        private Plant Plant = null;

        /// <summary>The met data</summary>
        [Link]
        public IWeather MetData = null;

        /// <summary>The parent plant</summary>
        [Link]
        private Plant parentPlant = null;

        /// <summary>The parent organ</summary>
        [ParentLink]
        private GenericOrgan parentOrgan= null;
 
        /// <summary>The FRGR function</summary>
        [Link]
        IFunction FRGRFunction = null;  

        /// <summary>The effect of CO2 on stomatal conductance</summary>
        [Link]
        IFunction StomatalConductanceCO2Modifier = null;

        /// <summary>The cover function</summary>
        [Link(IsOptional = true)]
        IFunction CoverFunction = null;

        /// <summary>The lai function</summary>
        [Link(IsOptional = true)]
        IFunction GAIFunction = null;
   
        /// <summary>The extinction coefficient function</summary>
        [Link(IsOptional = true)]
        IFunction ExtinctionCoefficientFunction = null;
    
        /// <summary>The height function</summary>
        [Link]
        IFunction HeightFunction = null;
     
        /// <summary>TThe depth of canopy which organ resides in</summary>
        [Link]
        IFunction DepthFunction = null;
      
        /// <summary>The lai dead function</summary>
        [Link]
        IFunction GAIDeadFunction = null;



        #region Canopy interface

        /// <summary>Gets the canopy. Should return null if no canopy present.</summary>
        public string CanopyType { get { return Plant.CropType+ "_" + parentOrgan.Name; } }

        /// <summary>Albedo.</summary>
        [Description("Albedo")]
        public double Albedo { get; set; }

        /// <summary>Gets or sets the gsmax.</summary>
        [Description("Daily maximum stomatal conductance(m/s)")]
        public double Gsmax
        {
            get
            {
                return Gsmax350*FRGR * StomatalConductanceCO2Modifier.Value();
            }
        }

        /// <summary>Gets or sets the gsmax.</summary>
        [Description("Maximum stomatal conductance at CO2 concentration of 350 ppm (m/s)")]
        public double Gsmax350 { get; set; }

        /// <summary>Gets or sets the R50.</summary>
        [Description("R50")]
        public double R50 { get; set; }

        /// <summary>Gets the LAI</summary>
        [XmlIgnore]
        [Units("m^2/m^2")]
        public double LAI { get; set; }

        /// <summary>Gets the LAI live + dead (m^2/m^2)</summary>
        public double LAITotal { get { return LAI + LAIDead; } }

        /// <summary>Gets the cover green.</summary>
        [Units("0-1")]
        public double CoverGreen
        {
            get
            {
                if (Plant.IsAlive)
                {
                    double greenCover = 0.0;
                    if (CoverFunction == null)
                        greenCover = 1.0 - Math.Exp(-ExtinctionCoefficientFunction.Value() * LAI);
                    else
                        greenCover = CoverFunction.Value();
                    return Math.Min(Math.Max(greenCover, 0.0), 0.999999999); // limiting to within 10^-9, so MicroClimate doesn't complain
                }
                else
                    return 0.0;

            }
        }

        /// <summary>Gets the cover total.</summary>
        [Units("0-1")]
        public double CoverTotal
        {
            get { return 1.0 - (1 - CoverGreen) * (1 - CoverDead); }
        }

        /// <summary>Gets or sets the height.</summary>
        [XmlIgnore]
        [Units("mm")]
        public double Height { get; set; }
        /// <summary>Gets the depth.</summary>
        [Units("mm")]
        [XmlIgnore]
        public double Depth { get; set; }

        /// <summary>Gets the width of the canopy (mm).</summary>
        public double Width { get { return 0; } }


        /// <summary>Gets or sets the FRGR.</summary>
        [Units("mm")]
        [XmlIgnore]
        public double FRGR { get; set; }

        private double _PotentialEP = 0;
        /// <summary>Sets the potential evapotranspiration. Set by MICROCLIMATE.</summary>
        [Units("mm")]
        [XmlIgnore]
        public double PotentialEP
        {
            get { return _PotentialEP; }
            set
            {
                _PotentialEP = value;
            }
        }

        /// <summary>Sets the actual water demand.</summary>
        [Units("mm")]
        [XmlIgnore]
        public double WaterDemand { get; set; }

        /// <summary>Gets or sets the water allocation.</summary>
        [XmlIgnore]
        public double WaterAllocation { get; set; }

        /// <summary>Sets the light profile. Set by MICROCLIMATE.</summary>
        [XmlIgnore]
        public CanopyEnergyBalanceInterceptionlayerType[] LightProfile { get; set; }
        #endregion

  
        #region States and variables

        /// <summary>Gets or sets the k dead.</summary>
        public double KDead { get; set; }                  // Extinction Coefficient (Dead)
        /// <summary>Calculates the water demand.</summary>
        public double CalculateWaterDemand()
        {
          
                return WaterDemand;
        
        }
        /// <summary>Gets the transpiration.</summary>
        public double Transpiration { get { return WaterAllocation; } }

     
        

        /// <summary>Gets or sets the lai dead.</summary>
        public double LAIDead { get; set; }


        /// <summary>Gets the cover dead.</summary>
        public double CoverDead { get { return 1.0 - Math.Exp(-KDead * LAIDead); } }

        /// <summary>Gets the RAD int tot.</summary>
        [Units("MJ/m^2/day")]
        [Description("This is the intercepted radiation value that is passed to the RUE class to calculate DM supply")]
        public double RadIntTot
        {
            get
            {
                 double TotalRadn = 0;
                 if (LightProfile != null)
                     for (int i = 0; i < LightProfile.Length; i++)
                     TotalRadn += LightProfile[i].amount;
                 return TotalRadn;
            }
        }

         #endregion

  
        #region Component Process Functions

        /// <summary>Clears this instance.</summary>
        private void Clear()
        {
              Height = 0;
            Depth = 0;
            LAI = 0;
        }

         #endregion

        #region Top Level time step functions
        /// <summary>Event from sequencer telling us to do our potential growth.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoPotentialPlantGrowth")]
        private void OnDoPotentialPlantGrowth(object sender, EventArgs e)
        {
            // save current state
            if (parentPlant.IsEmerged)
             {

                FRGR = FRGRFunction.Value();
                if (CoverFunction == null && ExtinctionCoefficientFunction == null)
                    throw new Exception("\"CoverFunction\" or \"ExtinctionCoefficientFunction\" should be defined in " + this.Name);
                if (CoverFunction != null)
                    LAI = (Math.Log(1 - CoverGreen) / (ExtinctionCoefficientFunction.Value() * -1));
                if (GAIFunction != null)
                    LAI = GAIFunction.Value();

                Height = HeightFunction.Value();
                Depth = DepthFunction.Value();
                LAIDead = GAIDeadFunction.Value();
            }
        }

        #endregion
     
     
        /// <summary>Constructor</summary>
        public EnergyBalance()
        {
          }

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        protected void OnSimulationCommencing(object sender, EventArgs e)
        {
            Height = 0.0;
            LAI = 0.0;
            Depth = 0.0;
        }
 
        /// <summary>Called when crop is sowed</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="data">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantSowing")]
        protected void OnPlantSowing(object sender, SowPlant2Type data)
        {
            if (data.Plant == parentPlant)
                Clear();
        }

        /// <summary>Called when crop is ending</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantEnding")]
        protected void OnPlantEnding(object sender, EventArgs e)
        {
            Clear();
        }

     }
}