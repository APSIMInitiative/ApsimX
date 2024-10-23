using System;
using System.Collections.Generic;
using Models.Core;
using Models.Functions;
using Models.PMF.Interfaces;

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
    [ViewName("UserInterface.Views.PropertyAndGridView")]
    [PresenterName("UserInterface.Presenters.PropertyAndGridPresenter")]

    public class Forages : Model
    {
        private List<ForageMaterialParameters> _parameters = null;
        private List<ModelWithDigestibleBiomass> forageModels = null;
        private Dictionary<string, ExpressionFunction> digestibilityFunctions = new();

        /// <summary>Forage parameters for all models and all organs.</summary>
        [Display]
        public List<ForageMaterialParameters> Parameters
        {
            get
            {
                if (_parameters == null)
                    CreateParametersUsingDefaults();
                return _parameters;
            }
            set
            {
                _parameters = value;
            }
        }

        /// <summary>Return a collection of models that have digestible biomasses.</summary>
        public IEnumerable<ModelWithDigestibleBiomass> ModelsWithDigestibleBiomass
        {
            get
            {
                if (forageModels == null)
                {
                    // Need to create default parameters if none have been deserialised from file.
                    if (_parameters == null)
                        CreateParametersUsingDefaults();

                    forageModels = new List<ModelWithDigestibleBiomass>();
                    foreach (var forage in FindAllInScope<IHasDamageableBiomass>())
                        forageModels.Add(new ModelWithDigestibleBiomass(this, forage, Parameters));
                }
                return forageModels;
            }
        }

        /// <summary>
        /// Get fraction consumable for biomass.
        /// </summary>
        /// <param name="damageableBiomass">Damageable material.</param>
        public double GetFractionConsumable(DamageableBiomass damageableBiomass)
        {
            var param = Parameters.Find(p => p.Name == damageableBiomass.Name) ?? throw new Exception($"Cannot find forage parameters for {damageableBiomass.Name}");
            if (damageableBiomass.IsLive)
                return param.LiveFractionConsumable;
            else
                return param.DeadFractionConsumable;
        }

         /// <summary>
        /// Get minimum consumable biomass for biomass.
        /// </summary>
        /// <param name="damageableBiomass">Damageable material.</param>
        public double GetMinimumConsumable(DamageableBiomass damageableBiomass)
        {
            var param = Parameters.Find(p => p.Name == damageableBiomass.Name) ?? throw new Exception($"Cannot find forage parameters for {damageableBiomass.Name}");
            if (damageableBiomass.IsLive)
                return param.LiveMinimumBiomass;
            else
                return param.DeadMinimumBiomass;
        }

        /// <summary>
        /// Get digestibility for biomass.
        /// </summary>
        /// <param name="damageableBiomass">Damageable material.</param>
        public double GetDigestibility(DamageableBiomass damageableBiomass)
        {
            var param = Parameters.Find(p => p.Name == damageableBiomass.Name) ?? throw new Exception($"Cannot find forage parameters for {damageableBiomass.Name}");

            string digestibilityString;
            if (damageableBiomass.IsLive)
                digestibilityString = param.LiveDigestibility;
            else
                digestibilityString = param.DeadDigestibility;

            if (digestibilityString == "FromModel")
            {
                if (damageableBiomass.DigestibilityFromModel == null)
                    throw new Exception($"You have chosen to use the digestibility from {damageableBiomass.Name} model but the model does not calculate digestibility");
                return (double)damageableBiomass.DigestibilityFromModel;
            }
            else if (double.TryParse(digestibilityString, out double digestibility))
                return digestibility;
            else
            {
                // assume expression function.
                if (!digestibilityFunctions.TryGetValue(digestibilityString, out ExpressionFunction expression))
                {
                    expression = new ExpressionFunction() { Parent = this, Expression = digestibilityString };
                    digestibilityFunctions.Add(digestibilityString, expression);
                }
                return expression.Value();
            }
        }

        /// <summary>Create parameters using default values.</summary>
        private void CreateParametersUsingDefaults()
        {
            var materialNames = new List<string>();
            foreach (var forage in FindAllInScope<IHasDamageableBiomass>())
            {
                foreach (var material in forage.Material)
                {
                    if (!materialNames.Contains(material.Name))
                    {
                        string liveDigestibility = "0.7";
                        string deadDigestibility = "0.3";
                        if (material.Name.StartsWith("AGP"))
                        {
                            liveDigestibility = "FromModel";
                            deadDigestibility = "FromModel";
                        }

                        if (_parameters == null)
                            _parameters = new();
                        Parameters.Add(new ForageMaterialParameters()
                        {
                            Name = material.Name,
                            LiveDigestibility = liveDigestibility,
                            DeadDigestibility = deadDigestibility,
                            LiveFractionConsumable = 1,
                            DeadFractionConsumable = 1,
                            LiveMinimumBiomass = 100
                        });
                        materialNames.Add(material.Name);
                    }
                }
            }
        }

        /// <summary>Encapsulates an amount of material removed from a plant.</summary>
        public class MaterialRemoved
        {
            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="wt">Mass of biomass removed (kg/ha).</param>
            /// <param name="n">Mass of nitrogen removed (kg/ha).</param>
            /// <param name="digestibility">Digestibility of material removed.</param>
            public MaterialRemoved(double wt, double n, double digestibility)
            {
                Wt = wt;
                N = n;
                Digestibility = digestibility;
            }

            /// <summary>Mass of biomass removed (kg/ha)</summary>
            public double Wt { get; }

            /// <summary>Mass of nitrogen removed (kg/ha)</summary>
            public double N { get; }

            /// <summary>Digestibility of material removed.</summary>
            public double Digestibility { get; }
        }
    }
}