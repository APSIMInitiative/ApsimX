using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using APSIM.Shared.Utilities;

namespace Models.Utilities
{
    /// <summary>
    /// This class accumulats values of variables
    /// </summary>
    [Serializable]
    public class Accumulator
    {
        private IModel parentModel;
        private string variableName;
        private int numberOfDays;
        private List<double> values = new List<double>();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="model"></param>
        /// <param name="variableName"></param>
        /// <param name="numberOfDays">Number of days to accumulate</param>
        public Accumulator(IModel model, string variableName, int numberOfDays)
        {
            this.parentModel = model;
            this.variableName = variableName;
            this.numberOfDays = numberOfDays;
        }

        /// <summary>
        /// Perform update
        /// </summary>
        public void Update()
        {
            if (values.Count > numberOfDays)
                values.RemoveAt(0);

            double value = (double) Apsim.Get(parentModel, variableName);

            values.Add(value);
        }

        /// <summary>
        /// Return the sum 
        /// </summary>
        public double Sum { get { return MathUtilities.Sum(values); } }
    }
}
