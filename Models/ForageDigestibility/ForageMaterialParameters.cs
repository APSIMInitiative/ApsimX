using System;
using Models.Core;
using Models.Functions;
using Newtonsoft.Json;

namespace Models.ForageDigestibility
{
    /// <summary>Encapsulates parameters for a forage material (e.g. leaf.live, leaf.dead, stem.live  etc).</summary>
    [Serializable]
    public class ForageMaterialParameters
    {
        private IFunction digestibilityFunction;

        /// <summary>Constructor.</summary>
        public ForageMaterialParameters()
        {
        }

        /// <summary>
        /// Constuctor
        /// </summary>
        /// <param name="parentModel">Parent model.</param>
        /// <param name="name">Name.</param>
        /// <param name="live">Is live material?</param>
        /// <param name="digestibility">Digestibility.</param>
        /// <param name="fractionConsumable">Fraction consumable.</param>
        /// <param name="minimum">Minimum mass to maintain (kg/ha).</param>
        public ForageMaterialParameters(IModel parentModel, string name, bool live, string digestibility, double fractionConsumable, double minimum)
        {
            Name = name;
            IsLive = live;
            UseDigestibilityFromModel = string.Equals(digestibility, "Internal", StringComparison.InvariantCultureIgnoreCase);
            DigestibilityString = digestibility;
            FractionConsumable = fractionConsumable;
            MinimumAmount = minimum;
            Initialise(parentModel);
        }

        /// <summary>Name of material.</summary>
        public string Name { get; set; }

        /// <summary>Is it live material?</summary>
        public bool IsLive { get; set; }

        /// <summary>Digestibility of material (0-1). Can be value or expresison.</summary>
        public string DigestibilityString { get; set; }

        /// <summary>Fraction of material that is consumable.</summary>
        public double FractionConsumable { get; set; }

        /// <summary>Minimum amount (mass) of material that is consumable (kg/ha).</summary>
        public double MinimumAmount { get; set; }

        /// <summary>Digestibility of material (0-1).</summary>
        [JsonIgnore]
        public double Digestibility => digestibilityFunction.Value();

        /// <summary>Use digestibility from the mode?</summary>
        public bool UseDigestibilityFromModel { get; set; }

        /// <summary>Initialise the instance.</summary>
        /// <param name="parentModel">Parent model.</param>
        public void Initialise(IModel parentModel)
        {
            if (!UseDigestibilityFromModel)
            {
                if (double.TryParse(DigestibilityString, out double doubleValue))
                    digestibilityFunction = new Constant() { FixedValue = doubleValue };
                else
                    digestibilityFunction = new ExpressionFunction() { Parent = parentModel, Expression = DigestibilityString };
            }
        }
    }
}