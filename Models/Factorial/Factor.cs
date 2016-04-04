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
    [ValidParent(ParentType = typeof(Factors))]
    [ValidParent(ParentType = typeof(Factor))]
    public class Factor : Model
    {
        /// <summary>A simple type for combining a path with multiple values.</summary>
        public class PathValuesPair
        {
            /// <summary>A path</summary>
            public string path;

            /// <summary>A value.</summary>
            public object value;
        }

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


            List<List<PathValuesPair>> allValues = new List<List<PathValuesPair>>();
            List<PathValuesPair> fixedValues = new List<PathValuesPair>();

            foreach (string specification in Specifications)
            {
                if (specification.Contains(" to ") &&
                    specification.Contains(" step "))
                    allValues.Add(ParseRangeSpecification(specification));
                else
                {
                    List<PathValuesPair> localValues;
                    if (specification.Contains('='))
                        localValues = ParseSimpleSpecification(specification);
                    else
                        localValues = ParseModelReplacementSpecification(specification);

                    if (localValues.Count == 1)
                        fixedValues.Add(localValues[0]);
                    else if (localValues.Count > 1)
                        allValues.Add(localValues);
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


            if (allValues.Count == 0)
            {
                PathValuesPairToFactorValue(factorValues, fixedValues, null);
            }

            List<List<PathValuesPair>> allCombinations = MathUtilities.AllCombinationsOf<PathValuesPair>(allValues.ToArray());

            if (allCombinations != null)
            {
                foreach (List<PathValuesPair> combination in allCombinations)
                    PathValuesPairToFactorValue(factorValues, fixedValues, combination);
            }

            return factorValues;
        }

        private void PathValuesPairToFactorValue(List<FactorValue> factorValues, List<PathValuesPair> fixedValues, List<PathValuesPair> combination)
        {
            List<string> pathsForFactor = new List<string>();
            List<object> valuesForFactor = new List<object>();

            // Add in fixed path/values.
            foreach (PathValuesPair fixedPathValue in fixedValues)
            {
                pathsForFactor.Add(fixedPathValue.path);
                valuesForFactor.Add(fixedPathValue.value);
            }

            // Add in rest.
            string factorName = Name;
            foreach (PathValuesPair pathValue in combination)
            {
                pathsForFactor.Add(pathValue.path);
                valuesForFactor.Add(pathValue.value);

                if (pathValue.value is IModel)
                    factorName += (pathValue.value as IModel).Name;
                else
                    factorName += pathValue.value.ToString();
            }

            factorValues.Add(new FactorValue(this, factorName, pathsForFactor, valuesForFactor));
        }

        /// <summary>
        /// Convert a simple specification into factor values.
        /// </summary>
        /// <param name="specification">The specification to examine</param>
        private List<PathValuesPair> ParseSimpleSpecification(string specification)
        {
            List<PathValuesPair> pairs = new List<PathValuesPair>();

            // Can be multiple values on specification line, separated by commas. Return a separate
            // factor value for each value.

            string path = specification;
            string value = StringUtilities.SplitOffAfterDelimiter(ref path, "=");

            if (value == null)
                throw new ApsimXException(this, "Cannot find any values on the specification line: " + Specifications[0]);
            string[] valueStrings = value.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            foreach (string stringValue in valueStrings)
                pairs.Add(new PathValuesPair() { path = path.Trim(), value = stringValue.Trim() });
            return pairs;
        }

        /// <summary>
        /// Convert a range specification into factor values.
        /// </summary>
        /// <param name="specification">The specification to examine</param>
        private List<PathValuesPair> ParseRangeSpecification(string specification)
        {
            List<PathValuesPair> pairs = new List<PathValuesPair>();

            // Format of a range:
            //    value1 to value2 step increment.
            string path = specification;
            string rangeString = StringUtilities.SplitOffAfterDelimiter(ref path, "=");
            string[] rangeBits = rangeString.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            double from = Convert.ToDouble(rangeBits[0], CultureInfo.InvariantCulture);
            double to = Convert.ToDouble(rangeBits[2], CultureInfo.InvariantCulture);
            double step = Convert.ToDouble(rangeBits[4], CultureInfo.InvariantCulture);

            for (double value = from; value <= to; value += step)
                pairs.Add(new PathValuesPair() { path = path.Trim(), value = value.ToString() });
            return pairs;
        }

        /// <summary>
        /// Convert a range specification into factor values.
        /// </summary>
        /// <param name="specification">The specification to examine</param>
        private List<PathValuesPair> ParseModelReplacementSpecification(string specification)
        {
            List<PathValuesPair> pairs = new List<PathValuesPair>();

            // Must be a model replacement.
            // Need to find a child value of the correct type.

            Experiment experiment = Apsim.Parent(this, typeof(Experiment)) as Experiment;
            if (experiment != null)
            {
                IModel modelToReplace = Apsim.Get(experiment.BaseSimulation, specification) as IModel;
                if (modelToReplace == null)
                    throw new ApsimXException(this, "Cannot find model: " + specification);

                foreach (IModel newModel in Apsim.Children(this, modelToReplace.GetType()))
                    pairs.Add(new PathValuesPair() { path = specification, value = newModel });
            }
            return pairs;
        }
    }
}
