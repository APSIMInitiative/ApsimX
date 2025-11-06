using System;
using System.Collections;
using System.Globalization;
using APSIM.Core;
using Models.Core;
using Models.Functions;
using Newtonsoft.Json;

namespace Models.PMF
{
    /// <summary>
    /// This class encapsulates an array of biomass objects
    /// </summary>
    [Serializable]
    public class ArrayBiomass : Model, IStructureDependency
    {
        /// <summary>Structure instance supplied by APSIM.core.</summary>
        [field: NonSerialized]
        public IStructure Structure { private get; set; }


        /// <summary>The propertys</summary>
        public string[] Propertys = null;

        /// <summary>The array size</summary>
        public string ArraySize = null;
        /// <summary>The array size number</summary>
        private int ArraySizeNumber = -1;

        /// <summary>Gets or sets the non structural n.</summary>
        /// <value>The non structural n.</value>
        /// <exception cref="System.Exception">Cannot set StorageN in ArrayBiomass</exception>
        [JsonIgnore]
        [Units("g/m^2")]
        public double[] StorageN
        {
            get
            {
                return AddValuesToList(".StorageN");
            }

            set { throw new Exception("Cannot set StorageN in ArrayBiomass"); }
        }

        /// <summary>Gets or sets the structural n.</summary>
        /// <value>The structural n.</value>
        /// <exception cref="System.Exception">Cannot set StructuralN in ArrayBiomass</exception>
        [JsonIgnore]
        [Units("g/m^2")]
        public double[] StructuralN
        {
            get
            {
                return AddValuesToList(".StructuralN");
            }

            set { throw new Exception("Cannot set StructuralN in ArrayBiomass"); }
        }

        /// <summary>Gets or sets the metabolic n.</summary>
        /// <value>The metabolic n.</value>
        /// <exception cref="System.Exception">Cannot set MetabolicN in ArrayBiomass</exception>
        [JsonIgnore]
        [Units("g/m^2")]
        public double[] MetabolicN
        {
            get
            {
                return AddValuesToList(".MetabolicN");
            }

            set { throw new Exception("Cannot set MetabolicN in ArrayBiomass"); }
        }

        /// <summary>Gets or sets the non structural wt.</summary>
        /// <value>The non structural wt.</value>
        /// <exception cref="System.Exception">Cannot set StorageWt in ArrayBiomass</exception>
        [JsonIgnore]
        [Units("g/m^2")]
        public double[] StorageWt
        {
            get
            {
                return AddValuesToList(".StorageWt");
            }
            set { throw new Exception("Cannot set StorageWt in ArrayBiomass"); }
        }

        /// <summary>Gets or sets the structural wt.</summary>
        /// <value>The structural wt.</value>
        /// <exception cref="System.Exception">Cannot set StructuralWt in ArrayBiomass</exception>
        [JsonIgnore]
        [Units("g/m^2")]
        public double[] StructuralWt
        {
            get
            {
                return AddValuesToList(".StructuralWt");
            }

            set { throw new Exception("Cannot set StructuralWt in ArrayBiomass"); }
        }

        /// <summary>Gets or sets the metabolic wt.</summary>
        /// <value>The metabolic wt.</value>
        /// <exception cref="System.Exception">Cannot set MetabolicWt in ArrayBiomass</exception>
        [JsonIgnore]
        [Units("g/m^2")]
        public double[] MetabolicWt
        {
            get
            {
                return AddValuesToList(".MetabolicWt");
            }

            set { throw new Exception("Cannot set MetabolicWt in ArrayBiomass"); }
        }

        /// <summary>Gets the n conc.</summary>
        /// <value>The n conc.</value>
        [JsonIgnore]
        [Units("g/g")]
        public double[] NConc
        {
            get
            {
                return AddValuesToList(".NConc");
            }
        }

        /// <summary>Gets the structural n conc.</summary>
        /// <value>The structural n conc.</value>
        [JsonIgnore]
        [Units("g/g")]
        public double[] StructuralNConc
        {
            get
            {
                return AddValuesToList(".StructuralNConc");
            }
        }

        /// <summary>Gets the non structural n conc.</summary>
        /// <value>The non structural n conc.</value>
        [JsonIgnore]
        [Units("g/g")]
        public double[] StorageNConc
        {
            get
            {
                return AddValuesToList(".StorageNConc");
            }
        }

        /// <summary>Gets the metabolic n conc.</summary>
        /// <value>The metabolic n conc.</value>
        [JsonIgnore]
        [Units("g/g")]
        public double[] MetabolicNConc
        {
            get
            {
                return AddValuesToList(".MetabolicNConc");
            }
        }

        /// <summary>Adds the values to list.</summary>
        /// <param name="SubPropertyName">Name of the sub property.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Cannot find:  + PropertyName +  in ArrayBiomass:  + this.Name</exception>
        private double[] AddValuesToList(string SubPropertyName)
        {
            if (ArraySizeNumber == -1)
                ArraySizeNumber = Convert.ToInt32(ExpressionFunction.Evaluate(ArraySize, this, Structure), CultureInfo.InvariantCulture);

            double[] Values = new double[ArraySizeNumber];
            int i = 0;
            foreach (string PropertyName in Propertys)
            {
                object Obj = Structure.GetObject(PropertyName + SubPropertyName)?.Value;
                if (Obj == null)
                    throw new Exception("Cannot find: " + PropertyName + " in ArrayBiomass: " + this.Name);

                if (Obj is IEnumerable)
                {
                    foreach (object Value in Obj as IEnumerable)
                    {
                        Values[i] = Convert.ToDouble(Value,
                                                     System.Globalization.CultureInfo.InvariantCulture);
                        i++;
                    }
                }
                else
                {
                    Values[i] = Convert.ToDouble(Obj,
                                                 System.Globalization.CultureInfo.InvariantCulture);
                    i++;
                }


            }
            return Values;
        }



    }
}