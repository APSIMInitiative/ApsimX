using CommandLine;
using Models.CLEM.Interfaces;
using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// This manages all ruminant parameters for a ruminant Type
    /// </summary>
    [Serializable]
    public class RuminantParameters
    {
        private readonly Dictionary<string, bool> modified = new();

        /// <summary>
        /// Link to the ruminant type details of the individual.
        /// </summary>
        public RuminantType Details { get; set; }

        /// <summary>
        /// Parameters for the Breed activity
        /// </summary>
        [JsonIgnore]
        public RuminantParametersBreeding Breeding { get; set; }

        /// <summary>
        /// General parameters defining the RuminantType
        /// </summary>
        [JsonIgnore]
        public RuminantParametersGeneral General { get; set; }

        /// <summary>
        /// Parameters for the Grazing activities
        /// </summary>
        [JsonIgnore]
        public RuminantParametersGrazing Grazing { get; set; }

        /// <summary>
        /// Parameters for the Grow activity
        /// </summary>
        [JsonIgnore]
        public RuminantParametersGrow Grow { get; set; }

        /// <summary>
        /// Parameters for the Grow24 activity
        /// </summary>
        [JsonIgnore]
        public RuminantParametersGrow24 Grow24 { get; set; }

        /// <summary>
        /// Intake parameters for the Grow24 activity
        /// </summary>
        [JsonIgnore]
        public RuminantParametersGrow24CI Grow24_CI { get; set; }

        /// <summary>
        /// Growth parameters for the Grow24 activity
        /// </summary>
        [JsonIgnore]
        public RuminantParametersGrow24CG Grow24_CG { get; set; }

        /// <summary>
        /// Death parameters for the Grow24 activity
        /// </summary>
        [JsonIgnore]
        public RuminantParametersGrow24CD Grow24_CD { get; set; }

        /// <summary>
        /// Pregnancy parameters for the Grow24 activity
        /// </summary>
        [JsonIgnore]
        public RuminantParametersGrow24CP Grow24_CP { get; set; }

        /// <summary>
        /// Metabolism parameters for the Grow24 activity
        /// </summary>
        [JsonIgnore]
        public RuminantParametersGrow24CM Grow24_CM { get; set; }

        /// <summary>
        /// Rumen digestability and efficiency parameters for the Grow24 activity
        /// </summary>
        [JsonIgnore]
        public RuminantParametersGrow24CACRD Grow24_CACRD { get; set; }

        /// <summary>
        /// Efficiency and lactation parameters for the Grow24 activity
        /// </summary>
        [JsonIgnore]
        public RuminantParametersGrow24CKCL Grow24_CKCL { get; set; }

        /// <summary>
        /// Parameters for the RuminantParametersMethaneCharmley activity
        /// </summary>
        [JsonIgnore]
        public RuminantParametersMethaneCharmley EntericMethaneCharmley { get; set; }

        /// <summary>
        /// Parameters for the Ruminant Lactation
        /// </summary>
        [JsonIgnore]
        public RuminantParametersLactation Lactation { get; set; }

        /// <summary>
        /// Parameters for the Ruminant Mortality based on original Grow
        /// </summary>
        [JsonIgnore]
        public RuminantParametersGrowMortality GrowMortality { get; set; }


        /// <summary>
        /// Initialise by finding available RuminantParameters
        /// </summary>
        /// <param name="ruminantType"></param>
        public void Initialise(RuminantType ruminantType)
        {
            Details = ruminantType;

            // create loop over all properties of this class and find corresponding item in tree
            foreach (var property in GetType().GetProperties())
            {
                if (property.PropertyType.GetInterfaces().Contains(typeof(ISubParameters)))
                {
                    var subParameterModel = ruminantType.FindAllDescendants().Where(a => a.GetType() == property.PropertyType).FirstOrDefault();
                    property.SetValue(this, subParameterModel);
                }
            }
        }

        /// <summary>
        /// Constructor for shallow reference based copy or full deep copy from parent details
        /// Non modifed parameter sets will be shared across the entire herd of individuals
        /// </summary>
        /// <param name="parent">A RuminantParameters object from parent.</param>
        /// <param name="createCopy">Perform a deep copy of structure as values provided.</param>
        public RuminantParameters(RuminantParameters parent, bool createCopy = false)
        {
            Details = parent.Details;

            // loop over all properties of this class and set to parent is not null
            foreach (var property in GetType().GetProperties().Where(a => a.CanWrite))
            {
                var parentValue = property.GetValue(parent);
                if (parentValue is not null)
                {
                    if(createCopy)
                    {
                        // Check if the property value is not null and implements ICloneable
                        if (parentValue is ICloneable cloneable)
                        {
                            var clonedValue = cloneable.Clone();
                            property.SetValue(this, clonedValue);
                        }
                    }
                    else
                    {
                        property.SetValue(this, parentValue);
                    }
                }
                modified.Add(property.Name, false);
            }
        }

        /// <summary>
        /// Update a property in Ruminant parameters and create deep copy to separate from shared parent values if needed.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="propertyInfo"></param>
        /// <param name="value"></param>
        public void Update(string key, PropertyInfo propertyInfo, object value)
        {
            // find local property with the same type as PropertyInfo
            var localPropertyInfo = GetType().GetProperties().Where(a => a.PropertyType == propertyInfo.DeclaringType).FirstOrDefault();
            var localProperty = localPropertyInfo?.GetValue(this);
            if (localPropertyInfo is not null && localProperty is not null)
            {
                if (!modified.GetValueOrDefault(localPropertyInfo.Name) && localProperty is ICloneable cloneable)
                {
                    var clonedValue = cloneable.Clone();
                    localPropertyInfo.SetValue(localProperty, clonedValue);
                    modified[localPropertyInfo.Name] = true;
                }
                else
                {
                    throw new Exception($"Unable to update property {key} in RuminantParameters. ");
                }
                propertyInfo.SetValue(localProperty, value);
            }
        }

        /// <summary>
        /// Find base mortality rate across possible locations
        /// </summary>
        public double FindBaseMortalityRate
        {
            get
            {
                return Grow?.MortalityBase ?? (Grow24_CD?.BasalMortalityRate_CD1 ?? 0 * 365);
            }
        }
    }
}
