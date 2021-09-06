using Models.Core;
using Models.Interfaces;
using Models.PMF.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Models.GrazPlan
{
    /// <summary>
    /// Encapsulates a collection of forage parameters and a collection of forage models (e.g. wheat).
    /// The user interface calls the Tables property to get a table representation of all
    /// forage parameters.
    /// The stock model calls methods of this class to discover what forages are consumable by animals.
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(GrazPlan.Stock))]
    [ViewName("UserInterface.Views.DualGridView")]
    [PresenterName("UserInterface.Presenters.TablePresenter")]

    public class Forages : Model, IModelAsTable
    {
        private List<Forage> allForages;

        /// <summary>Forage parameters.</summary>
        public List<ForageParameters> forageParameters = null;

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

        /// <summary>Return a collection of forages for a zone.</summary>
        public IEnumerable<Forage> GetForages(Zone zone)
        {
            if (allForages == null)
            {
                allForages = new List<Forage>();
                foreach (var organ in FindAllInScope<IOrganDamage>())
                {
                    string modelName = GetForageName(organ);
                    if (allForages.Find(f => f.Name == modelName) == null)
                    {
                        var parameters = forageParameters.FirstOrDefault(f => f.Name.Equals(modelName, StringComparison.InvariantCultureIgnoreCase));
                        if (parameters == null)
                            throw new Exception($"Cannot find grazing parameters for {organ.Name}");

                        parameters.Initialise();

                        if (parameters.HasGrazableMaterial)
                        {
                            var organAsModel = organ as IModel;
                            if (organAsModel.Parent is IPlantDamage plant)
                                allForages.Add(new Forage(plant as IModel, parameters));
                            else
                                allForages.Add(new Forage(organ as IModel, parameters));
                        }
                    }
                }
            }
            return allForages.Where(f => f.Zone == zone);
        }

        /// <summary>Return a table of all parameters.</summary>
        private DataTable GetParametersAsGrid()
        {
            var data = new DataTable();
            data.Columns.Add("Model");
            data.Columns.Add("OrganName");
            data.Columns.Add("Live digestibility");
            data.Columns.Add("Dead digestibility");
            data.Columns.Add("Live fraction consumbable");
            data.Columns.Add("Dead fraction consumbable");

            var organTypes = new List<string>();
            foreach (var organ in FindAllInScope<IOrganDamage>())
            {
                string modelName = GetForageName(organ);
                if (!organTypes.Contains(modelName+organ.Name))
                {
                    DataRow row = GetForageParametersAsRow(data, modelName, organ.Name);
                    data.Rows.Add(row);
                    organTypes.Add(modelName + organ.Name);
                }
            }

            return data;
        }

        /// <summary>
        /// Get name of forage from organ.
        /// </summary>
        /// <param name="organ">The organ</param>
        /// <returns></returns>
        private static string GetForageName(IOrganDamage organ)
        {
            string modelName;
            var organAsIModel = organ as IModel;
            if (organAsIModel.Parent is IPlantDamage)
                modelName = organAsIModel.Parent.Name;
            else
                modelName = organ.Name;
            return modelName;
        }

        /// <summary>
        /// Get, as a DataRow, forage parameters for a model and organ.
        /// </summary>
        /// <param name="data">The DataTable the row is to belong to.</param>
        /// <param name="modelName">The name of the model.</param>
        /// <param name="organName">The name of the organ.</param>
        /// <returns></returns>
        private DataRow GetForageParametersAsRow(DataTable data, string modelName, string organName)
        {
            var row = data.NewRow();
            row[0] = modelName;
            row[1] = organName;
            row[2] = 0.7;
            row[3] = 0.3;
            row[4] = 100;
            row[5] = 100;

            var parameters = forageParameters?.Find(p => p.Name == modelName) as ForageParameters;
            if (parameters != null)
            {
                var parametersForOrgan = parameters.Material?.FirstOrDefault(p => p.Name == organName);
                if (parametersForOrgan != null)
                {
                    row[2] = parametersForOrgan.DigestibilityLiveString;
                    row[3] = parametersForOrgan.DigestibilityDeadString;
                    row[4] = parametersForOrgan.FractionConsumableLive;
                    row[5] = parametersForOrgan.FractionConsumableDead;
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
            forageParameters = new List<ForageParameters>();
            foreach (DataRow row in data.Rows)
            {
                var modelName = row[0].ToString();
                ForageParameters forage = forageParameters.Find(p => p.Name == modelName);
                if (forage == null)
                {
                    forage = new ForageParameters(modelName);

                    forageParameters.Add(forage);
                }
                forage.AddMaterial(row);
            }
        }
    }
}