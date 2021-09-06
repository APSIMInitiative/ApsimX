using APSIM.Shared.Utilities;
using Models.Functions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Models.GrazPlan
{
    /// <summary>Encapsulates parameters for a forage model (e.g. wheat).</summary>
    [Serializable]
    public class ForageParameters
    {
        /// <summary>Constructor.</summary>
        public ForageParameters() {  }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="modelName">Name of model.</param>
        public ForageParameters(string modelName)
        {
            Name = modelName;
        }

        /// <summary>Name of forage (e.g. wheat).</summary>
        public string Name { get; set; }

        /// <summary>Parameters for a forage.</summary>
        public List<ForageMaterialParameter> Material { get; set; } = new List<ForageMaterialParameter>();

        /// <summary>Return true if forage has grazable material.</summary>
        public bool HasGrazableMaterial => Material.FirstOrDefault(m => m.FractionConsumableLive > 0 || m.FractionConsumableDead > 0) != null;

        /// <summary>
        /// Add a material to this forage parameter instance.
        /// </summary>
        /// <param name="dataRow">The data row to read from.</param>
        public void AddMaterial(DataRow dataRow)
        {
            Material.Add(new ForageMaterialParameter(dataRow));
        }

        /// <summary>Initialise the forage parameters.</summary>
        internal void Initialise()
        {
            foreach (var material in Material)
            {
                material.Initialise();
            }
        }

        /// <summary>Encapsulates parameters for a forage material (e.g. leaf, stem etc).</summary>
        [Serializable]
        public class ForageMaterialParameter
        {
            private IFunction digestibilityLiveFunction;
            private IFunction digestibilityDeadFunction;

            /// <summary>Constructor.</summary>
            public ForageMaterialParameter()
            {
            }

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="dataRow">The data row to read from.</param>
            public ForageMaterialParameter(DataRow dataRow)
            {
                Name = dataRow[1].ToString();
                DigestibilityLiveString = dataRow[2].ToString();
                DigestibilityDeadString = dataRow[3].ToString();
                FractionConsumableLive = Convert.ToDouble(dataRow[4].ToString());
                FractionConsumableDead = Convert.ToDouble(dataRow[5].ToString());

                if (double.TryParse(DigestibilityLiveString, out double doubleValue))
                    digestibilityLiveFunction = new Constant() { FixedValue = doubleValue };
                else
                    digestibilityLiveFunction = new ExpressionFunction() { Expression = DigestibilityLiveString };
                if (double.TryParse(DigestibilityDeadString, out doubleValue))
                    digestibilityDeadFunction = new Constant() { FixedValue = doubleValue };
                else
                    digestibilityDeadFunction = new ExpressionFunction() { Expression = DigestibilityDeadString };
            }

            /// <summary>Digestibility of live material (0-1). Can be value or expresison.</summary>
            public string Name { get; set; }

            /// <summary>Digestibility of live material (0-1). Can be value or expresison.</summary>
            public string DigestibilityLiveString { get; set; }

            /// <summary>Digestibility of dead material (0-1). Can be value or expresison.</summary>
            public string DigestibilityDeadString { get; set; }

            /// <summary>Fraction of live material that is consumable.</summary>
            public double FractionConsumableLive { get; set; }

            /// <summary>Fraction of dead material that is consumable.</summary>
            public double FractionConsumableDead { get; set; }

            /// <summary>Digestibility of live material (0-1).</summary>
            public double DigestibilityLive => digestibilityLiveFunction.Value();

            /// <summary>Digestibility of dead material (0-1).</summary>
            public double DigestibilityDead => digestibilityDeadFunction.Value();


            /// <summary>Initialise.</summary>
            public void Initialise()
            {
                if (MathUtilities.IsNumerical(DigestibilityLiveString))
                    digestibilityLiveFunction = new Constant() { FixedValue = Convert.ToDouble(DigestibilityLiveString) };
                else
                    digestibilityLiveFunction = new ExpressionFunction() { Expression = DigestibilityLiveString };

                if (MathUtilities.IsNumerical(DigestibilityDeadString))
                    digestibilityDeadFunction = new Constant() { FixedValue = Convert.ToDouble(DigestibilityDeadString) };
                else
                    digestibilityDeadFunction = new ExpressionFunction() { Expression = DigestibilityDeadString };
            }
        }
    }
}