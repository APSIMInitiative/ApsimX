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

        /// <summary>The values collected.</summary>
        public List<double> Values { get; set; } = new List<double>();

        /// <summary>
        /// Perform update
        /// </summary>
        public void Update()
        {
            if (Values.Count > 0 && Values.Count >= numberOfDays)
                Values.RemoveAt(0);

            try
            {
                double value = (double) parentModel.FindByPath(variableName)?.Value;
                Values.Add(value);
            }
            catch (Exception err)
            {
                throw new Exception($"Accumulator is unable to update variable '{variableName}'", err);
            }
        }

        /// <summary>
        /// Return the sum 
        /// </summary>
        public double Sum { get { return MathUtilities.Sum(Values); } }

        
    }
}
