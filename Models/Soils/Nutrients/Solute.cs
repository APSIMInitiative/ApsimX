

namespace Models.Soils.Nutrients
{
    using Core;
    using Interfaces;
    using System;
    using APSIM.Shared.Utilities;
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using APSIM.Shared.Documentation;
    using System.Data;
    using System.Linq;

    /// <summary>
    /// This class used for this nutrient encapsulates the nitrogen within a mineral N pool.
    /// Child functions provide information on flows of N from it to other mineral N pools,
    /// or losses from the system.
    /// </summary>
    [Serializable]
    [ViewName("ApsimNG.Resources.Glade.NewGridView.glade")]
    [PresenterName("UserInterface.Presenters.NewGridPresenter")]
    [ValidParent(ParentType = typeof(Chemical))]
    public class Solute : Model, ISolute, ITabularData
    {
        [Link]
        Soil soil = null;

        /// <summary>Access the soil physical properties.</summary>
        [Link] 
        private IPhysical soilPhysical = null;

        /// <summary>
        /// An enumeration for specifying soil water units
        /// </summary>
        public enum UnitsEnum
        {
            /// <summary>ppm</summary>
            [Description("ppm")]
            ppm,

            /// <summary>kgha</summary>
            [Description("kg/ha")]
            kgha
        }

        /// <summary>Default constructor.</summary>
        public Solute() { }

        /// <summary>Default constructor.</summary>
        public Solute(Soil soilModel, string soluteName, double[] value) 
        {
            soil = soilModel;
            kgha = value;
            Name = soluteName;
        }

        /// <summary>Depth strings. Wrapper around Thickness.</summary>
        [Description("Depth")]
        [Units("cm")]
        [JsonIgnore]
        public string[] Depth
        {
            get
            {
                return SoilUtilities.ToDepthStringsCM(Thickness);
            }
            set
            {
                Thickness = SoilUtilities.ToThicknessCM(value);
            }
        }

        /// <summary>Thickness</summary>
        [Summary]
        [Units("mm")]
        public double[] Thickness { get; set; }

        /// <summary>Nitrate NO3.</summary>
        [Description("Initial values")]
        [Summary]
        public double[] InitialValues { get; set; }

        /// <summary>Units of the Initial values.</summary>
        public UnitsEnum InitialValuesUnits { get; set; }

        /// <summary>Solute amount (kg/ha)</summary>
        [JsonIgnore]
        public double[] kgha { get; set; }

        /// <summary>Solute amount (ppm)</summary>
        public double[] ppm { get { return SoilUtilities.kgha2ppm(soilPhysical.Thickness, soilPhysical.BD, kgha); } }

        /// <summary>Performs the initial checks and setup</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            Reset();
        }

        /// <summary>
        /// Set solute to initialisation state
        /// </summary>
        public void Reset()
        {
            if (InitialValues == null)
                kgha = new double[Thickness.Length];
            else if (InitialValuesUnits == UnitsEnum.kgha)
                kgha = ReflectionUtilities.Clone(InitialValues) as double[];
            else
                kgha = SoilUtilities.ppm2kgha(Thickness, soilPhysical.BD, InitialValues);
        }
        /// <summary>Setter for kgha.</summary>
        /// <param name="callingModelType">Type of calling model.</param>
        /// <param name="value">New values.</param>
        public void SetKgHa(SoluteSetterType callingModelType, double[] value)
        {
            for (int i = 0; i < value.Length; i++)
                kgha[i] = value[i];
        }

        /// <summary>Setter for kgha delta.</summary>
        /// <param name="callingModelType">Type of calling model</param>
        /// <param name="delta">New delta values</param>
        public void AddKgHaDelta(SoluteSetterType callingModelType, double[] delta)
        {
            for (int i = 0; i < delta.Length; i++)
                kgha[i] += delta[i];
        }

        /// <summary>
        /// Document the model.
        /// </summary>
        public override IEnumerable<ITag> Document()
        {
            foreach (ITag tag in DocumentChildren<Memo>())
                yield return tag;

            foreach (ITag tag in GetModelDescription())
                yield return tag;
        }

        /// <summary>Tabular data. Called by GUI.</summary>
        public DataTable TabularData
        {
            get
            {
                return GetData();
            }
            set
            {
                SetData(value);
            }
        }

        /// <summary>Get tabular data. Called by GUI.</summary>
        private DataTable GetData()
        {
            var data = new DataTable(Name);
            data.Columns.Add("Depth");
            data.Columns.Add("Initial values");

            // Add units to row 1.
            var unitsRow = data.NewRow();
            unitsRow["Depth"] = "(mm)";
            unitsRow["Initial values"] = $"({InitialValuesUnits})";
            data.Rows.Add(unitsRow);

            var depthStrings = SoilUtilities.ToDepthStrings(Thickness);
            for (int i = 0; i < Thickness.Length; i++)
            {
                var row = data.NewRow();
                row["Depth"] = depthStrings[i];
                row["Initial values"] = InitialValues[i].ToString("F3");
                data.Rows.Add(row);
            }

            return data;
        }

        /// <summary>Setting tabular data. Called by GUI.</summary>
        /// <param name="data"></param>
        public void SetData(DataTable data)
        {
            var depthStrings = DataTableUtilities.GetColumnAsStrings(data, "Depth", 100, 1);
            var numLayers = depthStrings.ToList().FindIndex(value => value == null);
            if (numLayers == -1)
                numLayers = 100;

            Thickness = SoilUtilities.ToThickness(DataTableUtilities.GetColumnAsStrings(data, "Depth", numLayers, 1));
            InitialValues = DataTableUtilities.GetColumnAsDoubles(data, "Initial values", numLayers, 1);
            var units = data.Rows[0]["Initial values"].ToString();
            if (units == "(kgha)")
                InitialValuesUnits = Solute.UnitsEnum.kgha;
            else
                InitialValuesUnits = Solute.UnitsEnum.ppm;
        }

        /// <summary>
        /// Get possible units for a given column.
        /// </summary>
        /// <param name="columnIndex">The column index.</param>
        /// <returns></returns>
        public IEnumerable<string> GetUnits(int columnIndex)
        {
            if (columnIndex == 1)
                return (string[])Enum.GetNames(typeof(Solute.UnitsEnum));
            else
                return new string[0];
        }

        /// <summary>
        /// Set the units for a column.
        /// </summary>
        /// <param name="columnIndex"></param>
        /// <param name="units"></param>
        public void SetUnits(int columnIndex, string units)
        {
            if (columnIndex == 1)
                InitialValuesUnits = (Solute.UnitsEnum)Enum.Parse(typeof(Solute.UnitsEnum), units);
        }
    }
}
