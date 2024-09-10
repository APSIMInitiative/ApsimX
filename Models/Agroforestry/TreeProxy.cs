using System;
using System.Linq;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra.Double;
using Newtonsoft.Json;
using Models.Core;
using Models.Zones;
using Models.Soils;
using Models.Interfaces;
using Models.Soils.Arbitrator;
using APSIM.Shared.Utilities;
using System.Globalization;

namespace Models.Agroforestry
{
    /// <summary>
    /// A simple proxy for a full tree model is provided for use in agroforestry simulations.  It allows the user to directly specify the size and structural data for trees within the simulation rather than having to simulate complex tree development (e.g. tree canopy structure under specific pruning regimes).
    ///
    /// Several parameters are required of the user to specify the state of trees within the simulation.  These include:
    ///
    /// * Tree height (m)
    /// * Shade modifier with age (0-1)
    /// * Tree root radius (cm)
    /// * Shade at a range of distances from the trees (%)
    /// * Tree root length density at various depths and distances from the trees (cm/cm^3^)
    /// * Tree daily nitrogen demand (g/m2/day for tree zone area)
    ///
    /// The model calculates diffusive nutrient uptake using the equations of [DeWilligen1994] as formulated in the model WANULCAS [WANULCAS2011] and modified to better represent nutrient buffering [smethurst1997paste;smethurst1999phase;van1990defining].
    /// Water uptake is calculated using an adaptation of the approach of [Meinkeetal1993] where the extraction coefficient is assumed to be proportional to root length density [Peakeetal2013].  The user specifies a value of the uptake coefficient at a base root length density of 1 cm/cm^3^ and spatial water uptake is scales using this value and the user-input of tree root length density.
    ///
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.TreeProxyView")]
    [PresenterName("UserInterface.Presenters.TreeProxyPresenter")]
    [ValidParent(ParentType = typeof(Simulation))]
    [ValidParent(ParentType = typeof(Zone))]
    public class TreeProxy : Model, IUptake
    {
        private TreeProxySpatial _spatial;

        [Link]
        IWeather weather = null;
        [Link]
        IClock clock = null;

        /// <summary>
        /// Tree proxy spatial data.
        /// </summary>
        public TreeProxySpatial Spatial
        {
            get
            {
                if (_spatial == null)
                    _spatial = new();
                _spatial.TreeProxyInstance = this;
                return _spatial;
            }
            set
            {
                _spatial = value;
            }
        }

        /// <summary>
        /// Reference to the parent agroforestry system.
        /// </summary>
        public AgroforestrySystem AFsystem = null;

        /// <summary>
        /// Distance from zone in tree heights
        /// </summary>
        [JsonIgnore]
        [Units("TreeHeights")]
        public double H { get; set; }

        /// <summary>
        /// Height of the tree.
        /// </summary>
        [JsonIgnore]
        [Units("m")]
        public double heightToday { get { return GetHeightToday(); } }

        /// <summary>
        /// Leaf Area
        /// </summary>
        [JsonIgnore]
        [Units("m2")]
        public double ShadeModiferToday { get { return GetShadeModifierToday(); } }

        /// <summary>
        /// The trees water uptake per layer in a single zone
        /// </summary>
        [JsonIgnore]
        [Units("mm")]
        public double[] WaterUptake { get; set; }

        /// <summary>
        /// The trees N uptake per layer in a single zone
        /// </summary>
        [JsonIgnore]
        [Units("g/m2")]
        public double[] NUptake { get; set; }

        /// <summary>
        /// The trees water demand across all zones.
        /// </summary>
        [JsonIgnore]
        [Units("L")]
        public double SWDemand { get; set; }  // Tree water demand (L)

        /// <summary>The root radius.</summary>
        /// <value>The root radius.</value>
        [Summary]
        [Description("Root Radius (cm)")]
        [Units("cm")]
        public double RootRadius { get; set; }

        /// <summary>Number of Trees in the System</summary>
        /// <value>The number of trees</value>
        [Summary]
        [Description("Number of Trees in the System ")]
        public double NumberOfTrees { get; set; }

        /// <summary>Adsoption Cofficient for NO3</summary>
        /// <value>Adsoption Cofficient for NO3</value>
        [Summary]
        [Description("Adsoption Cofficient for NO3 (m3/g)")]
        [Units("m3/g")]
        public double Kd { get; set; }

        /// <summary>The uptake coefficient.</summary>
        /// <value>KL Value at RLD of 1 cm/cm3.</value>
        [Summary]
        [Description("Base KL (KL at RLD of 1) (/d/cm/cm3)")]
        [Units("/d/cm/cm3")]
        public double BaseKL { get; set; }

        /// <summary>Extinction Coefficient.</summary>
        /// <value>Light Extinction Coefficient.</value>
        [Summary]
        [Description("Extinction Coefficient (-)")]
        public double KValue { get; set; }

        /// <summary>
        /// Water stress factor.
        /// </summary>
        [JsonIgnore]
        [Units("0-1")]
        public double WaterStress { get; set; }

        /// <summary>
        /// N stress factor.
        /// </summary>
        [JsonIgnore]
        [Units("0-1")]
        public double NStress { get; set; }


        /// <summary>
        /// A list containing forestry information for each zone.
        /// </summary>
        [JsonIgnore]
        public IEnumerable<Zone> ZoneList;

        /// <summary>
        /// Return an array of shade values.
        /// </summary>
        [JsonIgnore]
        [Summary]
        [Units("%")]
        public double[] Shade { get { return shade.Values.ToArray(); } }

        /// <summary>
        /// Date list for tree heights over lime
        /// </summary>
        [Summary]
        [Display]
        public DateTime[] Dates { get; set; }

        /// <summary>
        /// Tree heights (mm)
        /// </summary>
        [Summary]
        [Units("mm")]
        public double[] Heights { get; set; }

        /// <summary>
        /// Tree heights (m)
        /// </summary>
        [JsonIgnore]
        [Summary]
        [Display(DisplayName = "Height")]
        [Units("m")]
        public double[] HeightsM
        {
            get
            {
                if (Heights != null)
                    return MathUtilities.Divide_Value(Heights, 1000);
                return null;
            }
            set
            {
                Heights = MathUtilities.Multiply_Value(value, 1000);
            }
        }

        /// <summary>
        /// Tree N demands
        /// </summary>
        [Summary]
        [Display(DisplayName = "NDemand")]
        [Units("g/m2")]
        public double[] NDemands { get; set; }

        /// <summary>
        /// Shade Modifiers
        /// </summary>
        [Summary]
        [Display]
        [Units(">=0")]
        public double[] ShadeModifiers { get; set; }

        private Dictionary<double, double> shade = new Dictionary<double, double>();
        private Dictionary<double, double[]> rld = new Dictionary<double, double[]>();
        private IEnumerable<Zone> forestryZones;
        private Zone treeZone;
        private ISoilWater treeZoneWater;



        /// <summary>
        /// Return the distance from the tree for a given zone. The tree is assumed to be in the first Zone.
        /// </summary>
        /// <param name="z">Zone</param>
        /// <returns>Distance from a static tree</returns>
        public double GetDistanceFromTrees(Zone z)
        {
            double D = 0;
            foreach (Zone zone in ZoneList)
            {
                if (zone is RectangularZone)
                {
                    if (zone == ZoneList.FirstOrDefault())
                        D += 0; // the tree is at distance 0
                    else
                        D += (zone as RectangularZone).Width;
                }
                else if (zone is CircularZone)
                    D += (zone as CircularZone).Width;
                else
                    throw new ApsimXException(this, "Cannot calculate distance for trees for zone of given type.");

                if (zone == z)
                    return D;
            }

            throw new ApsimXException(this, "Could not find zone called " + z.Name);
        }

        /// <summary>
        /// Return the width of the given zone.
        /// </summary>
        /// <param name="z">The width.</param>
        /// <returns></returns>
        private double GetZoneWidth(Zone z)
        {
            double D = 0;
            if (z is RectangularZone)
                D = (z as RectangularZone).Width;
            else if (z is CircularZone)
                D = (z as CircularZone).Width;
            else
                throw new ApsimXException(this, "Cannot calculate distance for trees for zone of given type.");
            return D;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="z"></param>
        /// <returns></returns>
        private double ZoneDistanceInTreeHeights(Zone z)
        {
            double treeHeight = GetHeightToday();
            double distFromTree = GetDistanceFromTrees(z);

            return distFromTree / treeHeight;
        }

        /// <summary>
        /// Return the %Shade for a given zone
        /// </summary>
        /// <param name="z"></param>
        public double GetShade(Zone z)
        {
            double distInTH = ZoneDistanceInTreeHeights(z);
            bool didInterp = false;
            return MathUtilities.LinearInterpReal(distInTH, shade.Keys.ToArray(), shade.Values.ToArray(), out didInterp) * GetShadeModifierToday();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="z"></param>
        /// <returns></returns>
        public double[] GetRLD(Zone z)
        {
            double distInTH = ZoneDistanceInTreeHeights(z);
            bool didInterp = false;
            DenseMatrix rldM = DenseMatrix.OfColumnArrays(rld.Values);
            double[] rldInterp = new double[rldM.RowCount];

            for (int i=0;i< rldM.RowCount;i++)
            {
                rldInterp[i] = MathUtilities.LinearInterpReal(distInTH, rld.Keys.ToArray(), rldM.Row(i).ToArray(), out didInterp);
            }

            return rldInterp;
        }

        /// <summary>
        /// Setup the tree properties so they can be mapped to a zone.
        /// </summary>
        private void SetupTreeProperties()
        {
            double[] shadeValues = Spatial.Shade;
            shade.Add(0, shadeValues[0]);
            shade.Add(0.5, shadeValues[1]);
            shade.Add(1, shadeValues[2]);
            shade.Add(1.5, shadeValues[3]);
            shade.Add(2, shadeValues[4]);
            shade.Add(2.5, shadeValues[5]);
            shade.Add(3, shadeValues[6]);
            shade.Add(4, shadeValues[7]);
            shade.Add(5, shadeValues[8]);
            shade.Add(6, shadeValues[9]);

            rld.Add(0, Spatial.Rld(0));
            rld.Add(0.5, Spatial.Rld(0.5));
            rld.Add(1, Spatial.Rld(1));
            rld.Add(1.5, Spatial.Rld(1.5));
            rld.Add(2, Spatial.Rld(2));
            rld.Add(2.5, Spatial.Rld(2.5));
            rld.Add(3, Spatial.Rld(3));
            rld.Add(4, Spatial.Rld(4));
            rld.Add(5, Spatial.Rld(5));
            rld.Add(6, Spatial.Rld(6));
        }

        private double GetHeightToday()
        {
            double[] OADates = new double[Dates.Count()];
            bool didInterp;

            for (int i = 0; i < Dates.Count(); i++)
                OADates[i] = Dates[i].ToOADate();
            return MathUtilities.LinearInterpReal(clock.Today.ToOADate(), OADates, Heights, out didInterp) / 1000;
        }
        private double GetNDemandToday()
        {
            double[] OADates = new double[Dates.Count()];
            bool didInterp;

            for (int i = 0; i < Dates.Count(); i++)
                OADates[i] = Dates[i].ToOADate();
            return MathUtilities.LinearInterpReal(clock.Today.ToOADate(), OADates, NDemands, out didInterp);
        }

        private double GetShadeModifierToday()
        {
            double[] OADates = new double[Dates.Count()];
            bool didInterp;

            for (int i = 0; i < Dates.Count(); i++)
                OADates[i] = Dates[i].ToOADate();
            return MathUtilities.LinearInterpReal(clock.Today.ToOADate(), OADates, ShadeModifiers, out didInterp);
        }

        /// <summary>
        /// Calculate the total intercepted radiation by the tree canopy (MJ)
        /// </summary>
        [Units("mm")]
        public double InterceptedRadiation
        {
            get
            {
                double IR = 0;
                foreach (Zone zone in ZoneList)
                    IR += zone.Area * weather.Radn;
                return IR;
            }
        }

        /// <summary>
        /// Calculate water use from each zone (mm)
        /// </summary>
        [Units("mm")]
        [JsonIgnore]
        public double[] TreeWaterUptake { get; private set; }

        /// <summary>
        /// Calculate water use on a per tree basis (L)
        /// </summary>
        [Units("L")]
        public double IndividualTreeWaterUptake {
            get
            {
                double TWU = 0;
                int i=0;

                foreach (Zone zone in ZoneList)
                {
                    TWU += TreeWaterUptake[i] * zone.Area * 10000.0/NumberOfTrees;
                    i++;
                }
                return TWU;
            }
        }

        /// <summary>
        /// Calculate water use on a per tree basis (L)
        /// </summary>
        [Units("L")]
        [JsonIgnore]
        public double IndividualTreeWaterDemand
        {
            get;
            private set;
        }

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            ZoneList = Parent.FindAllChildren<Zone>().ToList();
            SetupTreeProperties();

            //pre-fetch static information
            forestryZones = Parent.FindAllDescendants<Zone>().ToList();
            treeZone = ZoneList.FirstOrDefault();
            treeZoneWater = treeZone.FindInScope<ISoilWater>();

            TreeWaterUptake = new double[ZoneList.Count()];

        }

        /// <summary>
        /// Returns soil water uptake from each zone by the static tree model
        /// </summary>
        /// <param name="soilstate"></param>
        /// <returns></returns>
        public List<Soils.Arbitrator.ZoneWaterAndN> GetWaterUptakeEstimates(Soils.Arbitrator.SoilState soilstate)
        {
            double Etz = treeZoneWater.Eo; //Eo of Tree Zone

            SWDemand = 0;
            foreach (Zone ZI in ZoneList)
                SWDemand += Etz * (GetShade(ZI) / 100) * (ZI.Area * 10000);    // 100 converts from %, 10000 converts from ha to m2

            IndividualTreeWaterDemand = SWDemand / NumberOfTrees;

            List<ZoneWaterAndN> Uptakes = new List<ZoneWaterAndN>();
            double PotSWSupply = 0; // Total water supply (L)

            foreach (ZoneWaterAndN Z in soilstate.Zones)
            {
                foreach (Zone ZI in ZoneList)
                {
                    if (Z.Zone.Name == ZI.Name)
                    {
                        ZoneWaterAndN Uptake = new ZoneWaterAndN(ZI);
                        //Find the soil for this zone
                        Soils.Soil ThisSoil = null;
                        Soils.IPhysical soilPhysical = null;

                        foreach (Zone SearchZ in forestryZones)
                            if (SearchZ.Name == Z.Zone.Name)
                            {
                                ThisSoil = SearchZ.FindInScope<Soils.Soil>();
                                soilPhysical = ThisSoil.FindChild<Soils.IPhysical>();
                                break;
                            }

                        double[] SW = Z.Water;
                        Uptake.NO3N = new double[SW.Length];
                        Uptake.NH4N = new double[SW.Length];
                        Uptake.Water = new double[SW.Length];
                        double[] LL15mm = MathUtilities.Multiply(soilPhysical.LL15, soilPhysical.Thickness);
                        double[] RLD = GetRLD(ZI);

                        for (int i = 0; i <= SW.Length - 1; i++)
                        {
                            Uptake.Water[i] = Math.Max(SW[i] - LL15mm[i],0.0) * BaseKL*RLD[i];
                            PotSWSupply += Uptake.Water[i] * ZI.Area * 10000;
                        }
                        Uptakes.Add(Uptake);
                        break;
                    }
                }
            }
            // Now scale back uptakes if supply > demand
            double F = 0;  // Uptake scaling factor
            if (PotSWSupply > 0)
            {
                F = SWDemand / PotSWSupply;
                if (F > 1)
                    F = 1;
            }
            else
                F = 1;
            WaterStress = Math.Min(1, Math.Max(0, PotSWSupply / SWDemand));

            List<double> uptakeList = new List<double>();
            foreach (ZoneWaterAndN Z in Uptakes)
            {
                Z.Water = MathUtilities.Multiply_Value(Z.Water, F);
                uptakeList.Add(Z.TotalWater);
            }

            WaterUptake = uptakeList.ToArray();
            return Uptakes;


        }
        /// <summary>
        /// Returns soil Nitrogen uptake from each zone by the static tree model
        /// </summary>
        /// <param name="soilstate"></param>
        /// <returns></returns>
        public List<Soils.Arbitrator.ZoneWaterAndN> GetNitrogenUptakeEstimates(Soils.Arbitrator.SoilState soilstate)
        {
            Zone treeZone = ZoneList.FirstOrDefault() as Zone;

            List<ZoneWaterAndN> Uptakes = new List<ZoneWaterAndN>();
            double PotNO3Supply = 0; // Total N supply (kg)

            double NDemandkg = GetNDemandToday()*10 * treeZone.Area;

            foreach (ZoneWaterAndN Z in soilstate.Zones)
            {
                foreach (Zone ZI in ZoneList)
                {
                    if (Z.Zone.Name == ZI.Name)
                    {
                        ZoneWaterAndN Uptake = new ZoneWaterAndN(ZI);
                        //Find the soil for this zone
                        Soils.Soil ThisSoil = null;
                        Soils.IPhysical soilPhysical = null;

                        foreach (Zone SearchZ in forestryZones)
                            if (SearchZ.Name == Z.Zone.Name)
                            {
                                ThisSoil = SearchZ.FindInScope<Soils.Soil>();
                                soilPhysical = ThisSoil.FindChild<Soils.IPhysical>();
                                break;
                            }

                        double[] SW = Z.Water;

                        Uptake.NO3N = new double[SW.Length];
                        Uptake.NH4N = new double[SW.Length];
                        Uptake.Water = new double[SW.Length];
                        double[] LL15mm = MathUtilities.Multiply(soilPhysical.LL15, soilPhysical.Thickness);
                        double[] BD = soilPhysical.BD;
                        double[] RLD = GetRLD(ZI);

                        for (int i = 0; i <= SW.Length - 1; i++)
                        {
                            Uptake.NO3N[i] = PotentialNO3Uptake(soilPhysical.Thickness[i], Z.NO3N[i], Z.Water[i], RLD[i], RootRadius, BD[i], Kd);
                            Uptake.NO3N[i] *= 10; // convert from g/m2 to kg/ha
                            PotNO3Supply += Uptake.NO3N[i] * ZI.Area;
                        }
                        Uptakes.Add(Uptake);
                        break;
                    }
                }
            }
            // Now scale back uptakes if demand > supply
            double F = 0;  // Uptake scaling factor
            if (PotNO3Supply > 0)
            {
                F = NDemandkg / PotNO3Supply;
                if (F > 1)
                    F = 1;
            }
            else
                F = 1;

            foreach (ZoneWaterAndN Z in Uptakes)
                Z.NO3N = MathUtilities.Multiply_Value(Z.NO3N, F);

            return Uptakes;
        }
        double PotentialNO3Uptake(double thickness, double NO3N, double SWmm, double RLD, double RootRadius, double BD, double Kd)
        {

            double L = RLD / 100 * 1000000;   // Root Length Density (m/m3)
            double D0 = 0.05 /10000*24;       // Diffusion Coefficient (m2/d)
            double theta = SWmm / thickness;  // Volumetric soil water (m3/m3)
            double tau = 3.13*Math.Pow(theta,1.92);    //  Tortuosity (unitless)
            double H = thickness / 1000;  // Layer thickness (m)
            double R0 = RootRadius / 100;  // Root Radius (m)
            double Nconc = (NO3N / 10)/H;  // Concentration in solution (g/m3 soil)
            double BD_gm3 = BD * 1000000.0;

            //Potential Uptake (g/m2)
            double U = (Math.PI * L * D0 * tau * theta * H * Nconc)/((BD_gm3*Kd+theta)*(-3.0/8.0 + 1.0/2.0*Math.Log(1.0/(R0*Math.Pow(Math.PI*L,0.5)))));

            // Now check that U is less than NO3 in soil (which is in kg/ha)
            if (U > (NO3N/10))
                U = (NO3N/10);
            return U;
        }

        double PotentialSWUptake(double thickness, double RLD)
        {
            return 0;
        }
        /// <summary>
        ///  Accepts the actual soil water uptake from the soil arbitrator.
        /// </summary>
        /// <param name="info"></param>
        public void SetActualWaterUptake(List<Soils.Arbitrator.ZoneWaterAndN> info)
        {
            int i = 0;
            foreach (Zone SearchZ in forestryZones)
            {
                foreach (ZoneWaterAndN ZI in info)
                {
                    if (SearchZ.Name == ZI.Zone.Name)
                    {
                        var thisSoil = SearchZ.FindInScope<ISoilWater>();
                        thisSoil.RemoveWater(ZI.Water);
                        TreeWaterUptake[i] = MathUtilities.Sum(ZI.Water);
                        if (TreeWaterUptake[i] < 0)
                        { }
                        i++;
                    }
                }
            }

        }

        /// <summary>
        /// Accepts the actual soil Nitrogen uptake from the soil arbitrator.
        /// </summary>
        /// <param name="info"></param>
        public void SetActualNitrogenUptakes(List<Soils.Arbitrator.ZoneWaterAndN> info)
        {
            double NO3Supply = 0; // Total N supply (kg)
            List<double> uptakeList = new List<double>();
            foreach (ZoneWaterAndN ZI in info)
            {
                foreach (Zone SearchZ in forestryZones)
                {
                    if (SearchZ.Name == ZI.Zone.Name)
                    {
                        var NO3Solute = SearchZ.FindInScope("NO3") as ISolute;
                        double[] NewNO3 = new double[ZI.NO3N.Length];
                        for (int i = 0; i <= ZI.NO3N.Length - 1; i++)
                            NewNO3[i] = NO3Solute.kgha[i] - ZI.NO3N[i];
                        NO3Solute.SetKgHa(SoluteSetterType.Plant, NewNO3);
                        NO3Supply += NewNO3.Sum() * SearchZ.Area;
                        uptakeList.Add(ZI.TotalNO3N);
                    }
                }
            }
            double NDemandkg = GetNDemandToday() * 10 * treeZone.Area;
            NUptake = uptakeList.ToArray();
            NStress = Math.Min(1, Math.Max(0, NO3Supply / NDemandkg));
        }

    }

    /// <summary>
    /// A structure holding forestry information for a single zone.
    /// </summary>
    [Serializable]
    public struct ZoneInfo
    {
        /// <summary>
        /// The name of the zone.
        /// </summary>
        public Zone zone;

        /// <summary>
        /// Shade value.
        /// </summary>
        public double Shade;

        /// <summary>
        /// Root Length Density information for each soil layer in a zone.
        /// </summary>
        public double[] RLD;
    }
}

