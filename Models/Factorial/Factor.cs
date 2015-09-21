// -----------------------------------------------------------------------
// <copyright file="Factor.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.Factorial
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Models.Core;
    using System.Xml.Serialization;
    using System.Xml;
    using System.Globalization;
    using APSIM.Shared.Utilities;

    /// <summary>
    /// A class representing a series of factor values.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.EditorView")]
    [PresenterName("UserInterface.Presenters.FactorPresenter")]
    [ValidParent(ParentModels = new Type[] { typeof(Factorial.Factors) })]
    public class Factor : Model
    {
        /// <summary>
        /// A list of factor specifications.
        /// </summary>
        public List<string> Specifications { get; set; }

        /// <summary>
        /// Return all possible factor values for this factor.
        /// </summary>
        public List<FactorValue> CreateValues()
        {
            List<FactorValue> factorValues = new List<FactorValue>();
            // Example specifications:
            //    simple specification:
            //      [SowingRule].Script.SowingDate = 2003-11-01                     
            //      [SowingRule].Script.SowingDate = 2003-11-01, 2003-12-20
            //    range specification:
            //      [FertiliserRule].Script.ApplicationAmount = 0 to 100 step 20
            //    model replacement specification:
            //      [FertiliserRule]
            //    compound specification has multiple other specifications that
            //    result in a single factor value being returned.

            List<string> paths = new List<string>();
            List<object> values = new List<object>();

            foreach (string specification in Specifications)
            {
                if (specification.Contains(" to ") &&
                    specification.Contains(" step "))
                {
                    // Range specification
                    ParseRangeSpecification(specification, paths, values);
                }
                else if (specification.Contains('='))
                {
                    // Simple specification
                    ParseSimpleSpecification(specification, paths, values);
                }
                else
                {
                    // Model replacement
                    ParseModelReplacementSpecification(specification, paths, values);
                }
            }

            // Look for child Factor models.
            foreach (Factor childFactor in Apsim.Children(this, typeof(Factor)))
            {
                foreach (FactorValue childFactorValue in childFactor.CreateValues())
                {
                    childFactorValue.Name = Name + childFactorValue.Name;
                    factorValues.Add(childFactorValue);
                }
            }

            // Do we have a compound specification (i.e. more than 1)?
            if (Specifications.Count > 1)
            {
                // A compound factor - return 1 factor value
                factorValues.Add(new FactorValue(Name, paths, values));
            }
            else
            {
                // Not a compound
                // Is there only a single path?
                if (paths.Count == 1)
                {
                    factorValues.Add(new FactorValue(Name, paths[0], values[0]));
                }
                else // if multiple paths, separate factor value for each path and value.
                {
                    for (int i = 0; i < paths.Count; i++)
                    {
                        string factorName;
                        if (values[i] is IModel && (values[i] as IModel).Parent.Parent is Factor)
                            factorName = Name;   // OLD - Need to remove.
                        else if (values[i] is IModel)
                            factorName = Name + (values[i] as IModel).Name;
                        else
                            factorName = Name + values[i].ToString();

                        factorValues.Add(new FactorValue(factorName, paths[i], values[i]));
                    }
                }
            }

            return factorValues;
        }

        /// <summary>
        /// Convert a simple specification into factor values.
        /// </summary>
        /// <param name="specification">The specification to examine</param>
        /// <param name="paths">The return list of factor paths</param>
        /// <param name="values">The return list of factor values</param>
        private void ParseSimpleSpecification(string specification, List<string> paths, List<object> values)
        {
            // Can be multiple values on specification line, separated by commas. Return a separate
            // factor value for each value.

            string path = specification;
            string value = StringUtilities.SplitOffAfterDelimiter(ref path, "=");

            if (value == null)
                throw new ApsimXException(this, "Cannot find any values on the specification line: " + Specifications[0]);
            string[] valueStrings = value.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            foreach (string stringValue in valueStrings)
            {
                paths.Add(path.Trim());
                values.Add(stringValue.Trim());
            }
        }

        /// <summary>
        /// Convert a range specification into factor values.
        /// </summary>
        /// <param name="specification">The specification to examine</param>
        /// <param name="paths">The return list of factor paths</param>
        /// <param name="values">The return list of factor values</param>
        private void ParseRangeSpecification(string specification, List<string> paths, List<object> values)
        {
            // Format of a range:
            //    value1 to value2 step increment.
            string path = specification;
            string rangeString = StringUtilities.SplitOffAfterDelimiter(ref path, "=");
            string[] rangeBits = rangeString.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            double from = Convert.ToDouble(rangeBits[0], CultureInfo.InvariantCulture);
            double to = Convert.ToDouble(rangeBits[2], CultureInfo.InvariantCulture);
            double step = Convert.ToDouble(rangeBits[4], CultureInfo.InvariantCulture);

            for (double value = from; value <= to; value += step)
            {
                paths.Add(path);
                values.Add(value.ToString());
            }
        }

        /// <summary>
        /// Convert a range specification into factor values.
        /// </summary>
        /// <param name="specification">The specification to examine</param>
        /// <param name="paths">The return list of factor paths</param>
        /// <param name="values">The return list of factor values</param>
        private void ParseModelReplacementSpecification(string specification, List<string> paths, List<object> values)
        {
            // Must be a model replacement.
            // Need to find a child value of the correct type.

            Experiment experiment = Apsim.Parent(this, typeof(Experiment)) as Experiment;
            if (experiment != null)
            {
                IModel modelToReplace = Apsim.Get(experiment.BaseSimulation, specification) as IModel;
                if (modelToReplace == null)
                    throw new ApsimXException(this, "Cannot find model: " + specification);

                foreach (IModel newModel in Apsim.Children(this, modelToReplace.GetType()))
                {
                    paths.Add(specification);
                    values.Add(newModel);
                }
            }
        }
    }
}
