using System;
using System.Collections.Generic;
using System.Linq;
using Models.Climate;
using Models.Core;
using Models.Interfaces;


namespace Models.Zones
{
    /// <summary>Agregates variables across zones</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Simulation))]
    [Description("This model agregates variablse across multiple zones for reporting")]
    public class ZoneAgregate : Model
    {
        /// <summary>
        /// Zones in simulation
        /// </summary>
        public List<Zone> Zones;

        /// <summary>
        /// The total area of the zones in the simulation
        /// </summary>
        [Units("m2")]
        public double SimulationArea
        {
            get
            {
                double area = 0;
                foreach (Zone zone in Zones)
                {
                    area += zone.Area;
                }
                return area * 10000; //convert area to m2 from ha;
            }
        }
    

        
        /// <summary>
        /// Potential evapotranspiration averaged over all zones in simulation
        /// </summary>
        [Units("mm")]
        public double EO 
        { 
            get
            {
                return AreaWeightedMean("[Soil].SoilWater.Eo");

            }
        }

        /// <summary>
        /// Transpiration averaged over all zones in simulation
        /// </summary>
        [Units("mm")]
        public double ET 
        { 
            get
            {
                return AreaWeightedMean("[Plant].Leaf.Transpiration");
            }
        }

        /// <summary>
        /// Soil evapotranspiration averaged over all zones in simulation
        /// </summary>
        [Units("mm")]
        public double ES
        {
            get
            {
                return AreaWeightedMean("[Soil].SoilWater.Es");
            }
        }
        
        /// <summary>
        /// The amount of radiation over the simulation area.  I.e sum of all zones 
        /// </summary>
        [Units("MJ/total zone area")]
        public double IncommingRadiation
        {
            get 
            {
                return (double)this.Parent.FindAllDescendants<Weather>().FirstOrDefault().Radn * SimulationArea;
            }
        }

        /// <summary>
        /// Radiation intercepted by green leaf over the area of the simulation
        /// </summary>
        [Units("MJ/total zone area")]
        public double GreenAreaRadiationInterception
        {
            get
            {
                double greenRadn = 0;
                foreach (Zone zone in Zones) 
                { 
                    foreach (ICanopy canopy in zone.Canopies)
                    {
                        double canopyRadn = 0;
                        if (canopy.LightProfile != null)
                        {
                            for (int i = 0; i < canopy.LightProfile.Length; i++)
                                canopyRadn += canopy.LightProfile[i].AmountOnGreen;
                        }
                        greenRadn += canopyRadn; //* zone.Area*10000;
                    }
                }
                return greenRadn;
            }
        }

        /// <summary>
        /// Radiation intercepted by dead material over the area of the simulation
        /// </summary>
        [Units("MJ/total zone area")]
        public double DeadMaterialRadiationInterception
        {
            get
            {
                double deadRadn = 0;
                foreach (Zone zone in Zones)
                {
                    double canopyRadn = 0;
                    foreach (ICanopy canopy in zone.Canopies)
                    {
                        if (canopy.LightProfile != null)
                        {
                            for (int i = 0; i < canopy.LightProfile.Length; i++)
                                canopyRadn += canopy.LightProfile[i].AmountOnDead;
                        }
                        deadRadn += canopyRadn * zone.Area * 10000;
                    }
                }
                return deadRadn;
            }
        }

        /// <summary>
        /// Proportion of incomming radiation intercepted by green leaf
        /// </summary>
        [Units("0-1")]
        public double FRadIntGreen
        {
            get
            {
                return GreenAreaRadiationInterception / IncommingRadiation;
            }
        }

        /// <summary>
        /// Proportion of incomming radiation intercepted by green leaf
        /// </summary>
        [Units("0-1")]
        public double FRadIntDead
        {
            get
            {
                return DeadMaterialRadiationInterception / IncommingRadiation;
            }
        }

        /// <summary>Called when simulation starts.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event data.</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            Zones = new List<Zone>();
            foreach (Zone newZone in this.Parent.FindAllDescendants<Zone>())
                Zones.Add(newZone);
        }

        private double AreaWeightedMean(string varName)
        {
            return AreaSum(varName) / SimulationArea;
        }

        private double AreaSum(string varName)
        {
            double variable = 0;
            foreach (Zone zone in Zones)
            {
                variable += (double)zone.Get(varName) * zone.Area * 10000; 

            }
            return variable;
        }

    }
}
