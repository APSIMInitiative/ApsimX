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
using Models.Utilities;
using System.Data;

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
    public class TreeProxy : Model, IUptake, IGridModel
    {
        [Link]
        IWeather weather = null;
        [Link]
        IClock clock = null;

        /// <summary>
        /// Gets or sets the table data.
        /// Be careful when working with this property!
        /// The first list contains the column headers (e.g. 1 row of data).
        /// The subsequent lists all contain columns of data(?!).
        /// </summary>
        /// <value>The table.</value>
        public List<List<string>> Table { get; set; }

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
        public DateTime[] Dates { get; set; }

        /// <summary>
        /// Tree heights
        /// </summary>
        [Summary]
        [Units("mm")]
        public double[] Heights { get; set; }

        /// <summary>
        /// Tree N demands
        /// </summary>
        [Summary]
        [Units("g/m2")]
        public double[] NDemands { get; set; }

        /// <summary>
        /// Shade Modifiers
        /// </summary>
        [Summary]
        public double[] ShadeModifiers { get; set; }

        /// <summary>
        /// Tables
        /// </summary>
        public List<GridTable> Tables
        {
            get {
                List<GridTableColumn> columns = new List<GridTableColumn>();
                columns.Add(new GridTableColumn("Date", new VariableProperty(this, GetType().GetProperty("Dates"))));
                columns.Add(new GridTableColumn("Height", new VariableProperty(this, GetType().GetProperty("Heights"))));
                columns.Add(new GridTableColumn("NDemand", new VariableProperty(this, GetType().GetProperty("NDemands"))));
                columns.Add(new GridTableColumn("ShadeModifier", new VariableProperty(this, GetType().GetProperty("ShadeModifiers"))));
                GridTable grid1 = new GridTable("TreeProxyTemporal", columns, this);
                grid1.SetUnits(1, "m");
                grid1.SetUnits(3, "(>=0)");

                columns = new List<GridTableColumn>();
                GridTable grid2 = new GridTable("TreeProxySpatial", columns, this);

                List<GridTable> list = new List<GridTable>() { grid1, grid2 };
                return list;
            }
        }

        private Dictionary<double, double> shade = new Dictionary<double, double>();
        private Dictionary<double, double[]> rld = new Dictionary<double, double[]>();
        private IEnumerable<Zone> forestryZones;
        private Zone treeZone;
        private ISoilWater treeZoneWater;

        /// <summary>
        /// Combines the live and dead forages into a single row for display and renames columns
        /// </summary>
        public DataTable ConvertModelToDisplay(DataTable dt)
        {
            if (dt.TableName.Equals("TreeProxyTemporal"))
            {
                //convert height in mm to height in metres
                //first row is units, so skip
                for(int i = 1; i < dt.Rows.Count; i++)
                    if (!String.IsNullOrEmpty(dt.Rows[i]["Height"].ToString()))
                        dt.Rows[i]["Height"] = Convert.ToDouble(dt.Rows[i]["Height"]) / 1000;
                return dt;
            }
            //this is a special case, we need to manually handle the changes to Table
            else if (dt.TableName.Equals("TreeProxySpatial"))
            {
                var data = new DataTable();
                data.TableName = dt.TableName;
                data.Columns.Add("Parameter");
                data.Columns.Add("0");
                data.Columns.Add("0.5h");
                data.Columns.Add("1h");
                data.Columns.Add("1.5h");
                data.Columns.Add("2h");
                data.Columns.Add("2.5h");
                data.Columns.Add("3h");
                data.Columns.Add("4h");
                data.Columns.Add("5h");
                data.Columns.Add("6h");

                if (!(this.Parent is AgroforestrySystem))
                    throw new ApsimXException(this, "Error: TreeProxy must be a child of ForestrySystem.");

                IEnumerable<Zone> zones = this.Parent.FindAllDescendants<Zone>();

                // Get the first soil. For now we're assuming all soils have the same structure.
                var physical = zones.First().FindInScope<Physical>();

                if (this.Table.Count < 2)
                {
                    DataRow row = data.NewRow();
                    row["Parameter"] = "Shade (%)";
                    //fill in with 0s
                    for (int i = 1; i < row.ItemArray.Length; i++)
                        row[i] = 0;
                    data.Rows.Add(row);

                    //these are left empty
                    row = data.NewRow();
                    row["Parameter"] = "Root Length Density (cm/cm3)";
                    data.Rows.Add(row);

                    //these are left empty
                    row = data.NewRow();
                    row["Parameter"] = "Depth (cm)";
                    data.Rows.Add(row);

                    foreach (string s in SoilUtilities.ToDepthStringsCM(physical.Thickness))
                    {
                        row = data.NewRow();
                        row["Parameter"] = s;
                        //fill in with 0s
                        for (int i = 1; i < row.ItemArray.Length; i++)
                            row[i] = 0;
                        data.Rows.Add(row);
                    }
                }
                else
                {
                    for (int x = 0; x < this.Table[1].Count; x++)
                    {
                        //clean up any empty rows
                        bool empty = true;
                        DataRow row = data.NewRow();
                        for (int y = 1; y < this.Table.Count; y++)
                        {
                            if (this.Table[y][x] != null)
                                if (this.Table[y][x].ToString().Length > 0)
                                    empty = false;
                            row[y-1] = this.Table[y][x];
                        }
                        if (!empty)
                            data.Rows.Add(row);
                    }
                    
                    /*
                    // add Zones not in the table
                    IEnumerable<string> except = colNames.Except(forestryModel.Table[0]);
                    foreach (string s in except)
                        forestryModel.Table.Add(Enumerable.Range(1, forestryModel.Table[1].Count).Select(x => "0").ToList());

                    forestryModel.Table[0].AddRange(except);
                    for (int i = 2; i < forestryModel.Table.Count; i++)
                    {
                        // Set Depth and RLD rows to empty strings.
                        forestryModel.Table[i][2] = string.Empty;
                    }

                    // Remove Zones from table that don't exist in simulation.
                    except = forestryModel.Table[0].Except(colNames);
                    List<int> indexes = new List<int>();
                    foreach (string s in except.ToArray())
                        indexes.Add(forestryModel.Table[0].FindIndex(x => s == x));

                    indexes.Sort();
                    indexes.Reverse();

                    foreach (int i in indexes)
                    {
                        forestryModel.Table[0].RemoveAt(i);
                        forestryModel.Table.RemoveAt(i + 1);
                    }
                    */
                }
                return data;
            }
            else
            {
                return dt;
            }
        }

        /// <summary>
        /// Breaks the lines into the live and dead parts and changes headers to match class
        /// </summary>
        public DataTable ConvertDisplayToModel(DataTable dt)
        {
            if (dt.TableName.Equals("TreeProxyTemporal"))
            {
                //convert height in m to height in mm
                //first row is units, so skip
                for (int i = 1; i < dt.Rows.Count; i++)
                    if (!String.IsNullOrEmpty(dt.Rows[i]["Height"].ToString()))
                        dt.Rows[i]["Height"] = Convert.ToDouble(dt.Rows[i]["Height"]) * 1000;
                return dt;
            }
            else if (dt.TableName.Equals("TreeProxySpatial"))
            {
                //add all datas
                List<List<string>> newTable = new List<List<string>>();
                
                //removes blank line that gets added sometimes
                int startRow = 1;
                for (int x = 0; x < dt.Columns.Count; x++)
                    if (dt.Rows[0][x].ToString().Length > 0)
                        startRow = 0;

                for (int x = 0; x < dt.Columns.Count; x++)
                {
                    List<string> row = new List<string>();
                    for (int y = startRow; y < dt.Rows.Count; y++)
                    {
                        row.Add(dt.Rows[y][x].ToString());
                    }
                    newTable.Add(row);
                }

                //add headers - Yes, this is how it's supposed to be
                newTable.Insert(0, new List<string>());
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    newTable[0].Add(dt.Columns[i].ColumnName);
                }
                
                this.Table = newTable;

                //since we don't want the gridtable to edit anything in the model, we just return an empty datatable here.
                return new DataTable();
            }
            else
            {
                return dt;
            }
        }

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
            //These need to match the column names in the UI
            double[] THCutoffs = new double[] { 0, 0.5, 1, 1.5, 2, 2.5, 3, 4, 5, 6 };

            for (int i = 2; i < Table.Count; i++)
            {
                if (Table[i][0].Length == 0)
                    throw new Exception($"Cell at position [{Table[0][i-2]}, {Table[1][i]}] is empty");

                shade.Add(THCutoffs[i - 2], Convert.ToDouble(Table[i][0],
                                                             System.Globalization.CultureInfo.InvariantCulture));
                List<double> getRLDs = new List<double>();
                for (int j = 3; j < Table[1].Count; j++)
                    if (!string.IsNullOrEmpty(Table[i][j]))
                        getRLDs.Add(Convert.ToDouble(Table[i][j], System.Globalization.CultureInfo.InvariantCulture));
                rld.Add(THCutoffs[i - 2], getRLDs.ToArray());
            }
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

