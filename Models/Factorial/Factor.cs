﻿using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Shared.Utilities;
using Models.Core;

namespace Models.Factorial
{

    /// <summary>
    /// A class representing a treatment of an experiment (e.g. fertiliser).
    /// It produces a series of factor values.
    /// </summary>
    /// <remarks>
    /// Specification can be of the form:
    ///     [SowingRule].Script.SowingDate = 2003-11-01, 2003-12-20
    /// or
    ///     [FertiliserRule].Script.ApplicationAmount = 0 to 100 step 20
    /// or
    ///     [IrrigationSchedule]
    ///     to indicate a path to a model that will be replaced by the child
    ///     nodes.
    /// or
    ///     left null to indicate there are child FactorValues. 
    /// </remarks>
    [Serializable]
    [ViewName("UserInterface.Views.FactorView")]
    [PresenterName("UserInterface.Presenters.FactorPresenter")]
    [ValidParent(ParentType = typeof(Factors))]
    [ValidParent(ParentType = typeof(CompositeFactor))]
    [ValidParent(ParentType = typeof(Permutation))]
    public class Factor : Model, IReferenceExternalFiles
    {
        /// <summary>A specification for producing a series of factor values.</summary>
        public string Specification { get; set; }

        /// <summary>
        /// Return all possible factor values for this factor.
        /// </summary>
        public List<CompositeFactor> GetCompositeFactors()
        {
            try
            {
                var childCompositeFactors = FindAllChildren<CompositeFactor>().Where(f => f.Enabled);
                if (string.IsNullOrEmpty(Specification))
                {
                    // Return each child CompositeFactor
                    return childCompositeFactors.ToList();
                }
                else
                {
                    List<CompositeFactor> factorValues = new List<CompositeFactor>();

                    if (Specification.Contains(" to ") && Specification.Contains(" step "))
                    {
                        if (childCompositeFactors.Any())
                            throw new InvalidOperationException("Illegal factor configuration. Cannot use child composite factors with the factor specification '{Specification}'. Either delete the child composite factors or fix the factor specification text.");
                        factorValues.AddRange(RangeSpecificationToFactorValues(Specification));
                    }
                    else if (Specification.Contains('='))
                    {
                        if (childCompositeFactors.Any())
                            throw new InvalidProgramException($"Illegal factor configuration. Cannot use child composite factors with the factor specification '{Specification}'. Either delete the child composite factors or fix the factor specification text.");
                        factorValues.AddRange(SetSpecificationToFactorValues(Specification));
                    }
                    else
                        factorValues.AddRange(ModelReplacementToFactorValues(Specification));

                    return factorValues;
                }
            }
            catch (Exception error)
            {
                throw new InvalidOperationException($"Unable to parse factor {Name}", error);
            }
        }

        /// <summary>
        /// Convert a simple specification into factor values.
        /// </summary>
        /// <param name="specification">The specification to examine</param>
        private List<CompositeFactor> SetSpecificationToFactorValues(string specification)
        {
            List<CompositeFactor> returnValues = new List<CompositeFactor>();

            // Can be multiple values on specification line, separated by commas. Return a separate
            // factor value for each value.
            string path = specification;
            string value = StringUtilities.SplitOffAfterDelimiter(ref path, "=").Trim();

            if (value == null)
                throw new Exception("Cannot find any values on the specification line: " + specification);

            string[] values = value.Split(",".ToCharArray());
            foreach (var val in values)
            {
                var newFactor = new CompositeFactor(this, path.Trim(), val.Trim());
                newFactor.Children.AddRange(Children);
                returnValues.Add(newFactor);
            }

            return returnValues;
        }

        /// <summary>
        /// Convert a range specification into factor values.
        /// </summary>
        /// <param name="specification">The specification to examine</param>
        private List<CompositeFactor> RangeSpecificationToFactorValues(string specification)
        {
            try
            {
                List<CompositeFactor> values = new List<CompositeFactor>();

                // Format of a range:
                //    value1 to value2 step increment.
                string path = specification;
                string rangeString = StringUtilities.SplitOffAfterDelimiter(ref path, "=");
                string[] rangeBits = rangeString.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                double from = StringUtilities.ParseDouble(rangeBits[0]);
                double to = StringUtilities.ParseDouble(rangeBits[2]);
                double step = StringUtilities.ParseDouble(rangeBits[4]);

                if ((from < to && step < 0) ||
                    (from > to && step > 0))
                    throw new InvalidOperationException($"Unbounded factor specification: {specification}");

                for (double value = from; value <= to; value += step)
                {
                    var newFactor = new CompositeFactor(this, path.Trim(), value.ToString());
                    newFactor.Children.AddRange(Children);
                    values.Add(newFactor);
                }

                return values;
            }
            catch (Exception error)
            {
                throw new InvalidOperationException($"Invalid factor range specification: '{specification}'", error);
            }
        }

        /// <summary>
        /// Convert a range specification into factor values.
        /// </summary>
        /// <param name="specification">The specification to examine</param>
        private List<CompositeFactor> ModelReplacementToFactorValues(string specification)
        {
            List<CompositeFactor> values = new List<CompositeFactor>();

            // Must be a model replacement.
            // Need to find a child value of the correct type.

            Experiment experiment = FindAncestor<Experiment>();
            if (experiment != null)
            {
                var baseSimulation = experiment.FindChild<Simulation>();
                IModel modelToReplace = baseSimulation.FindByPath(specification)?.Value as IModel;
                if (modelToReplace == null)
                    throw new ApsimXException(this, "Cannot find model: " + specification);
                foreach (IModel newModel in Children.Where(c => c.Enabled))
                    values.Add(new CompositeFactor(this, specification, newModel));
            }
            return values;
        }

        /// <summary>Return paths to all files referenced by this model.</summary>
        public IEnumerable<string> GetReferencedFileNames()
        {
            return GetCompositeFactors().SelectMany(factor => factor.GetReferencedFileNames());
        }

        /// <summary>Remove all paths from referenced filenames.</summary>
        public void RemovePathsFromReferencedFileNames()
        {
            throw new NotImplementedException();
        }
    }
}
