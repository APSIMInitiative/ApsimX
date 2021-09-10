using Models.Core;
using Models.Interfaces;
using Models.PMF;
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
        /// <summary>Forage parameters.</summary>
        public List<ForageParameters> Parameters { get; set; }

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


        /// <summary>Return a collection of biomasses than can be damaged.</summary>
        public IEnumerable<IHasDigestibleBiomass> DamageableBiomasses
        {
            get
            {

            }
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

            var materialNames = new List<string>();
            foreach (var forage in FindAllInScope<IHasDamageableBiomass>())
            {
                foreach (var material in forage.Material)
                {
                    var fullName = $"{forage.Name}.{material.Name}";
                    if (!materialNames.Contains(fullName))
                    {
                        DataRow row = GetForageParametersAsRow(data, forage.Name, material.Name);
                        data.Rows.Add(row);
                        materialNames.Add(fullName);
                    }
                }
            }

            return data;
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

            var parameters = Parameters?.Find(p => p.Name == modelName) as ForageParameters;
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
            Parameters = new List<ForageParameters>();
            foreach (DataRow row in data.Rows)
            {
                var modelName = row[0].ToString();
                ForageParameters forage = Parameters.Find(p => p.Name == modelName);
                if (forage == null)
                {
                    forage = new ForageParameters(modelName);

                    Parameters.Add(forage);
                }
                forage.AddMaterial(row);
            }
        }

        /// <summary>An interface for a model that has digestible material.</summary>
        public interface IHasDigestibleBiomass
        {
            /// <summary>A list of digestible material that can be grazed.</summary>
            IEnumerable<DigestibleBiomass> Material { get; }

            /// <summary>
            /// Remove biomass from an organ.
            /// </summary>
            /// <param name="materialName">Name of organ.</param>
            /// <param name="biomassRemoveType">Name of event that triggered this biomass remove call.</param>
            /// <param name="biomassToRemove">Biomass to remove.</param>
            void RemoveBiomass(string materialName, string biomassRemoveType, OrganBiomassRemovalType biomassToRemove);
        }

        /// <summary>A class to hold a mass of digestible biomass.</summary>
        public class DigestibleBiomass
        {
            private readonly DamageableBiomass material;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="material">Biomass.</param>
            public DigestibleBiomass(DamageableBiomass material)
            {
                this.material = material;
            }

            /// <summary>Name of material.</summary>
            public string Name { get; }

            /// <summary>Biomass</summary>
            public Biomass Biomass => material.Biomass;

            /// <summary>Is biomass live.</summary>
            public bool IsLive => material.IsLive;

            /// <summary>Digestibility of material.</summary>
            public double Digestibility
            {
                get
                {
                }
            }
        }
    }
}