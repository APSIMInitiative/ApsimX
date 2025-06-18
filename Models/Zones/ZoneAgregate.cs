using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Numerics;
using Models.Climate;
using Models.Core;
using Models.Interfaces;
using Models.PMF;
using NetTopologySuite.Algorithm;


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

        /*  /// <summary>Potential Evapotranspiration for plant canopy </summary>
        [Units("mm")]
         public ZoneAgregateVariable Eop { get; set; }/*/

        /// <summary> Incident radiation</summary>
        [Units("MJ/area")]
        public ZoneAgregateVariable Ro { get; set; }

        /*/// <summary>Potential Evapotranspiration calculated by soil </summary>
        [Units("l/area")]
        public ZoneAgregateVariable Eo { get; set; }

        /// <summary> Plant Transpiration </summary>
        [Units("l/area")]
        public ZoneAgregateVariable Et { get; set; }

        /// <summary> Soil evaporation </summary>
        [Units("l/area")]
        public ZoneAgregateVariable Es { get; set; }*/

        /// <summary>Irrigation averaged over all zones in simulation</summary>
        [Units("l/area")]
        public ZoneAgregateVariable Irrigation { get; set; }

        /// <summary>Irrigation averaged over all zones in simulation</summary>
        [Units("l/area")]
        public ZoneAgregateVariable AccumulatedIrrigation { get; set; }

        /// <summary>Nitrogen averaged over all zones in simulation</summary>
        [Units("kg/ha")]
        public ZoneAgregateVariable Nitrogen { get; set; }

        /// <summary>Nitrogen averaged over all zones in simulation</summary>
        [Units("kg/ha")]
        public ZoneAgregateVariable AccumulatedNitrogen{ get; set; }


        /// <summary> The amount of radiation intercepted by the green leaf on the tree canopy </summary>
        public double RiTreeGreen { get { return GetInterceptedRadiation(0, "Green"); } }
        /// <summary> The amount of radiation intercepted by the trunk and dead leaf of the tree </summary>
        public double RiTreeDead { get { return GetInterceptedRadiation(0, "Dead"); } }
        /// <summary> The amount of radiation intercepted by the green leaf in the understory </summary>
        public double RiUnderstoryGreen { get { return GetInterceptedRadiation(1, "Green"); } }
        /// <summary> The amount of radiation intercepted by dead material in the understory </summary>
        public double RiUnderstoryDead { get { return GetInterceptedRadiation(1, "Dead"); } }
        /// <summary>Radiation intercepted by green leaf over the area of the simulation (tree and understory)</summary>
        [Units("MJ/total zone area")]
        public double GreenAreaRadiationInterception { get { return RiTreeGreen + RiUnderstoryGreen; } }
        /// <summary>Radiation intercepted by green leaf over the area of the simulation (tree and understory)</summary>
        [Units("MJ/total zone area")]
        public double DeadAreaRadiationInterception { get { return RiTreeDead + RiUnderstoryDead; } }
        /// <summary> The proportion of radiation intercepted by the green leaf on the tree canopy </summary>
        public double FintTreeGreen { get { return RiTreeGreen / Ro.Total; } }
        /// <summary> The proportion of radiation intercepted by the trunk and dead leaf on the tree canopy </summary>
        public double FintTreeDead { get { return RiTreeDead / Ro.Total; } }
        /// <summary> The proportion of radiation intercepted by the green leaf of the understory </summary>
        public double FintUnderstoryGreen { get { return RiUnderstoryGreen / Ro.Total; } }
        /// <summary> The proportion of radiation intercepted by the dead leaf of the understory </summary>
        public double FintUnderstoryDead { get { return RiUnderstoryDead / Ro.Total; } }

        private double GetInterceptedRadiation(int zone, string type)
        {
            double intRadn = 0;
            foreach (ICanopy canopy in Zones[zone].Canopies)
            {
                if (canopy.LightProfile != null)
                {
                    for (int i = 0; i < canopy.LightProfile.Length; i++)
                    {
                        if (type == "Green")
                            intRadn += canopy.LightProfile[i].AmountOnGreen;
                        if (type == "Dead")
                            intRadn += canopy.LightProfile[i].AmountOnDead;
                    }
                }
            }
            return intRadn;
        }

        private double Total(string varName)
        {
            double variable = 0;
            foreach (Zone zone in Zones)
            {
                variable += (double)zone.Get(varName) * (double)zone.Area*10000;
            }
            return variable;
        }

        private double PerM2(string varName)
        {
            return Total(varName) / SimulationArea;
        }

        private double RowTotal(string varName)
        {
            return (double)Zones[0].Get(varName) * (double)Zones[0].Area*10000;
        }

        private double AlleyTotal(string varName)
        {
            return (double)Zones[1].Get(varName) * (double)Zones[1].Area * 10000;
        }

        ZoneAgregateVariable UpdateValues(string varName,double divisor = 1.0)
        {
            ZoneAgregateVariable rClass = new ZoneAgregateVariable(
            total:Total(varName),
            perM2:PerM2(varName),
            rowTotal:RowTotal(varName),
            alleyTotal:AlleyTotal(varName));
            if (divisor == 1.0)
                return rClass;
            else
                return rClass / divisor;
        }

        [EventSubscribe("DoReportCalculations")]
        private void onDoReportCalculations(object sender, EventArgs e)
        {
            /* Eop = UpdateValues("[Plant].Leaf.PotentialEP");
             Eo = UpdateValues("[ISoilWater].Eo");

             Et = UpdateValues("[Plant].Leaf.Transpiration");  
             Es = UpdateValues("[ISoilWater].Es");*/
            Ro = UpdateValues("[Plant].Leaf.Canopy.MetData.Radn");
            Irrigation = UpdateValues("[Irrigation].IrrigationApplied");
            Nitrogen = UpdateValues("[Fertiliser].NitrogenApplied",10); //divide N by 10 to make grams
            AccumulatedIrrigation = AccumulatedIrrigation + Irrigation;
            AccumulatedNitrogen = AccumulatedNitrogen + Nitrogen;
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
            string message = "For ZoneAgregate to work it requires a multizone simulation with the first zone named \"Row\" and the second zone named \"Alley\"";
            if (Zones[0].Name != "Row")
                throw new Exception(message);
            if (Zones[1].Name != "Alley")
                throw new Exception(message);

            AccumulatedIrrigation = new ZoneAgregateVariable(total: 0, perM2: 0, rowTotal: 0, alleyTotal: 0);
            AccumulatedNitrogen = new ZoneAgregateVariable(total: 0, perM2: 0, rowTotal: 0, alleyTotal: 0);
        }
    }

    /// <summary>Data structure to hold calculations for </summary>
    public class ZoneAgregateVariable: Model
    {
        /// <summary>The value for the variable per m2</summary>
        public double Total { get; set; }
        /// <summary>The value for the variable for the total area</summary>
        public double PerM2 { get; set; }
        /// <summary>The value for the variable from the row per m2</summary>
        public double RowTotal { get; set; }
        /// <summary>The value for the variable from the alley per m2</summary>
        public double AlleyTotal { get; set; }
        
        /// <summary>The constructor</summary>
        public ZoneAgregateVariable(double total,double perM2, double rowTotal, double alleyTotal)
        {
            Total = total;
            PerM2 = perM2;
            RowTotal = rowTotal;
            AlleyTotal = alleyTotal;
        }

        /// <summary>opperator to add two variable data classes</summary>
        public static ZoneAgregateVariable operator +(ZoneAgregateVariable a, ZoneAgregateVariable b)
        {
            return new ZoneAgregateVariable(
            a.Total + b.Total,
            a.PerM2 + b.PerM2,
            a.RowTotal + b.RowTotal,
            a.AlleyTotal + b.AlleyTotal);
        }

        /// <summary>opperator to add two variable data classes</summary>
        public static ZoneAgregateVariable operator /(ZoneAgregateVariable a, double b)
        {
            return new ZoneAgregateVariable(
            MathUtilities.Divide(a.Total,b,0),
            MathUtilities.Divide(a.PerM2,b,0),
            MathUtilities.Divide(a.RowTotal,b,0),
            MathUtilities.Divide(a.AlleyTotal,b,0));
        }
    }
}
