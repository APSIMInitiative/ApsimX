using System;
using System.Collections.Generic;
using System.Data;
using Models.Core;
using Models.Interfaces;
using Models.PMF.Interfaces;
using Models.Utilities;
using Newtonsoft.Json;

namespace Models.ForageDigestibility
{
    /// <summary>
    /// Encapsulates a collection of forage parameters and a collection of forage models (e.g. wheat).
    /// The user interface calls the Tables property to get a table representation of all
    /// forage parameters.
    /// The stock model calls methods of this class to discover what forages are consumable by animals.
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Zone))]
    [ValidParent(ParentType = typeof(Simulation))]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.GridMultiPresenter")]

    public class Forages : Model, IGridModel
    {
        private List<ModelWithDigestibleBiomass> forageModels = null;

        /// <summary>Forage parameters for all models and all organs.</summary>
        public List<ForageMaterialParameters> Parameters { get; set; }

        /// <summary>Gets or sets the table of values.</summary>
        [JsonIgnore]
        public List<GridTable> Tables
        {
            get
            {
                List<GridTableColumn> columns = new List<GridTableColumn>();

                columns.Add(new GridTableColumn("Name", new VariableProperty(this, GetType().GetProperty("Parameters"))));
                columns.Add(new GridTableColumn("IsLive", new VariableProperty(this, GetType().GetProperty("Parameters"))));
                columns.Add(new GridTableColumn("DigestibilityString", new VariableProperty(this, GetType().GetProperty("Parameters"))));
                columns.Add(new GridTableColumn("FractionConsumable", new VariableProperty(this, GetType().GetProperty("Parameters"))));
                columns.Add(new GridTableColumn("MinimumAmount", new VariableProperty(this, GetType().GetProperty("Parameters"))));

                List<GridTable> tables = new List<GridTable>();
                tables.Add(new GridTable("", columns, this));

                if (Parameters == null)
                    GetParametersAsGrid();

                return tables;
            }
        }

        /// <summary>Return a collection of models that have digestible biomasses.</summary>
        public IEnumerable<ModelWithDigestibleBiomass> ModelsWithDigestibleBiomass
        {
            get
            {
                if (forageModels == null)
                {
                    // Need to initialise the parameters. When they are deserialised from file
                    // they haven't been initialised.
                    if (Parameters == null)
                        SetParametersFromGrid(GetParametersAsGrid()); // Setup with defaults.

                    foreach (var param in Parameters)
                        param.Initialise(this);

                    forageModels = new List<ModelWithDigestibleBiomass>();
                    foreach (var forage in FindAllInScope<IHasDamageableBiomass>())
                        forageModels.Add(new ModelWithDigestibleBiomass(forage, Parameters));
                }
                return forageModels;
            }
        }

        /// <summary>
        /// Combines the live and dead forages into a single row for display and renames columns
        /// </summary>
        public DataTable ConvertModelToDisplay(DataTable dt)
        {
            var data = new DataTable();
            data.Columns.Add("Name");
            data.Columns.Add("Live digestibility");
            data.Columns.Add("Dead digestibility");
            data.Columns.Add("Live fraction consumbable");
            data.Columns.Add("Dead fraction consumbable");
            data.Columns.Add("Live minimum biomass (kg/ha)");

            while(dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];
                DataRow row2 = null;

                //check if another row with the same name exists
                for (int i = 1; i < dt.Rows.Count && row2 == null; i++)
                    if (dt.Rows[i]["Name"].ToString() == row["Name"].ToString())
                        if (dt.Rows[i]["IsLive"] != row["IsLive"])
                            row2 = dt.Rows[i];

                //make row1 always the living row
                if (row2 != null && Convert.ToBoolean(row["IsLive"]) == false)
                {
                    DataRow temp = row;
                    row = row2;
                    row2 = temp;
                }

                if (row2 == null)
                    throw new Exception($"Cannot find the dead component for {row["Name"]}.");

                DataRow newRow = data.NewRow();
                newRow[0] = row["Name"];
                newRow[1] = row["DigestibilityString"];
                newRow[2] = row2["DigestibilityString"];
                newRow[3] = row["FractionConsumable"];
                newRow[4] = row2["FractionConsumable"];
                newRow[5] = row["MinimumAmount"];
                
                bool isEmpty = true;
                for (int i = 0; i < newRow.ItemArray.Length; i++)
                    if (newRow.ItemArray[i].ToString().Length > 0)
                        isEmpty = false;

                if (!isEmpty)
                    data.Rows.Add(newRow);

                dt.Rows.Remove(row);
                if (row2 != null)
                    dt.Rows.Remove(row2);
            }
            return data;
        }

        /// <summary>
        /// Breaks the lines into the live and dead parts and changes headers to match class
        /// </summary>
        public DataTable ConvertDisplayToModel(DataTable dt)
        {
            var data = new DataTable();
            data.Columns.Add("Name");
            data.Columns.Add("IsLive");
            data.Columns.Add("DigestibilityString");
            data.Columns.Add("FractionConsumable");
            data.Columns.Add("MinimumAmount");

            for(int i = 0; i < dt.Rows.Count; i++)
            {
                DataRow row = dt.Rows[i];

                DataRow newRowLive = data.NewRow();
                newRowLive["Name"] = row[0];
                newRowLive["IsLive"] = "True";
                newRowLive["DigestibilityString"] = row[1];
                newRowLive["FractionConsumable"] = row[3];
                newRowLive["MinimumAmount"] = row[5];

                DataRow newRowDead = data.NewRow();
                newRowDead["Name"] = row[0];
                newRowDead["IsLive"] = "False";
                newRowDead["DigestibilityString"] = row[2];
                newRowDead["FractionConsumable"] = row[4];
                newRowDead["MinimumAmount"] = 0;

                data.Rows.Add(newRowLive);
                data.Rows.Add(newRowDead);

            }

            return data;
        }

        /// <summary>Return a table of all parameters.</summary>
        private DataTable GetParametersAsGrid()
        {
            var data = new DataTable();
            data.Columns.Add("Name");
            data.Columns.Add("Live digestibility");
            data.Columns.Add("Dead digestibility");
            data.Columns.Add("Live fraction consumbable");
            data.Columns.Add("Dead fraction consumbable");
            data.Columns.Add("Live minimum biomass (kg/ha)");

            var materialNames = new List<string>();
            foreach (var forage in FindAllInScope<IHasDamageableBiomass>())
            {
                foreach (var material in forage.Material)
                {
                    if (!materialNames.Contains(material.Name))
                    {
                        DataRow row = GetForageParametersAsRow(data, material.Name);
                        data.Rows.Add(row);
                        materialNames.Add(material.Name);
                    }
                }
            }
            if (Parameters == null)
                SetParametersFromGrid(data);

            return data;
        }

        /// <summary>
        /// Get, as a DataRow, forage parameters for a model and organ.
        /// </summary>
        /// <param name="data">The DataTable the row is to belong to.</param>
        /// <param name="materialName">The name of the material.</param>
        /// <returns></returns>
        private DataRow GetForageParametersAsRow(DataTable data, string materialName)
        {
            var row = data.NewRow();
            row[0] = materialName;
            row[1] = 0.7;
            row[2] = 0.3;
            row[3] = 1;
            row[4] = 1;
            row[5] = 100;
            if (materialName.Contains("Root"))
            {
                row[1] = 0.0;
                row[2] = 0.0;
                row[3] = 0;
                row[4] = 0;
                row[5] = 0;
            }
            var live = Parameters?.Find(p => p.Name.Equals(materialName, StringComparison.InvariantCultureIgnoreCase)
                                             && p.IsLive);
            if (live != null)
            {
                row[1] = live.DigestibilityString;
                row[3] = live.FractionConsumable;
                row[5] = live.MinimumAmount;

                var dead = Parameters?.Find(p => p.Name.Equals(materialName, StringComparison.InvariantCultureIgnoreCase)
                                                 && !p.IsLive);
                if (dead != null)
                {
                    row[2] = dead.DigestibilityString;
                    row[4] = dead.FractionConsumable;
                }
            }

            return row;
        }

        /// <summary>
        /// Set the parameters property from a data table.
        /// </summary>
        /// <param name="data">The data table.</param>
        private void SetParametersFromGrid(DataTable data)
        {
            Parameters = new List<ForageMaterialParameters>();
            foreach (DataRow row in data.Rows)
            {
                var fullName = row[0].ToString();
                if (!string.IsNullOrEmpty(fullName)) // can be empty at bottom of grid because grid.CanGrow=true
                {
                    Parameters?.RemoveAll(p => p.Name.Equals(fullName, StringComparison.InvariantCultureIgnoreCase));
                    var live = new ForageMaterialParameters(this, fullName, live: true, row[1].ToString(), Convert.ToDouble(row[3]), Convert.ToDouble(row[5]));
                    Parameters.Add(live);

                    var dead = new ForageMaterialParameters(this, fullName, live: false, row[2].ToString(), Convert.ToDouble(row[4]), 0.0);
                    Parameters.Add(dead);
                }
            }
        }


    }
}