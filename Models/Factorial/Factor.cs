namespace Models.Factorial
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

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
    public class Factor : Model
    {
        /// <summary>A specification for producing a series of factor values.</summary>
        public string Specification { get; set; }

        /// <summary>
        /// Return all possible factor values for this factor.
        /// </summary>
        public List<CompositeFactor> GetCompositeFactors()
        {
            var childCompositeFactors = Apsim.Children(this, typeof(CompositeFactor)).Where(f => f.Enabled).Cast<CompositeFactor>();
            if (string.IsNullOrEmpty(Specification))
            {
                // Return each child CompositeFactor
                return childCompositeFactors.ToList();
            }
            else
            {
                List<CompositeFactor> factorValues = new List<CompositeFactor>();

                if (Specification.Contains(" to ") &&
                    Specification.Contains(" step "))
                    factorValues.AddRange(RangeSpecificationToFactorValues(Specification));
                else if (Specification.Contains('='))
                    factorValues.AddRange(SetSpecificationToFactorValues(Specification));
                else
                    factorValues.AddRange(ModelReplacementToFactorValues(Specification));

                return factorValues;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="compositeFactor"></param>
        /// <returns></returns>
        public IEnumerable<CompositeFactor> ExpandFactor(CompositeFactor compositeFactor)
        {
            var childCompositeFactors = Apsim.Children(compositeFactor, typeof(CompositeFactor)).Cast<CompositeFactor>();
            if (childCompositeFactors.Count() > 0)
            {
                var newFactorValues = new List<CompositeFactor>();
                foreach (var child in childCompositeFactors)
                {
                    var newCompositeFactor = new CompositeFactor(this, compositeFactor.Paths, compositeFactor.Values)
                    {
                        Name = compositeFactor.Name + child.Name,
                        Specifications = child.Specifications
                    };
                    newCompositeFactor.Children.AddRange(child.Children);
                    newFactorValues.Add(newCompositeFactor);
                }
                return newFactorValues;
            }
            else
                return new CompositeFactor[] { compositeFactor };
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
            List<CompositeFactor> values = new List<CompositeFactor>();

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
                var newFactor = new CompositeFactor(this, path.Trim(), value.ToString());
                newFactor.Children.AddRange(Children);
                values.Add(newFactor);
            }

            return values;
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

            Experiment experiment = Apsim.Parent(this, typeof(Experiment)) as Experiment;
            if (experiment != null)
            {
                var baseSimulation = Apsim.Child(experiment, typeof(Simulation));
                IModel modelToReplace = Apsim.Get(baseSimulation, specification) as IModel;
                if (modelToReplace == null)
                    throw new ApsimXException(this, "Cannot find model: " + specification);
                foreach (IModel newModel in Children.Where(c => c.Enabled))
                    values.Add(new CompositeFactor(this, specification, newModel));
            }
            return values;
        }
    }
}
