using System;
using System.Collections.Generic;
using System.Data;
using Models.Core;
using Models.Interfaces;
using Models.PMF.Interfaces;
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
    [ViewName("UserInterface.Views.DualGridView")]
    [PresenterName("UserInterface.Presenters.TablePresenter")]

    public class Forages : Model, IModelAsTable
    {
        private List<ModelWithDigestibleBiomass> forageModels = null;

        /// <summary>Forage parameters for all models and all organs.</summary>
        public List<ForageMaterialParameters> Parameters { get; set; }

        /// <summary>Gets or sets the table of values.</summary>
        [JsonIgnore]
        public List<DataTable> Tables
        {
            get
            {
                return new List<DataTable>() { GetParametersAsGrid() };
            }
            set
            {
                SetParametersFromGrid(value[0]);
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