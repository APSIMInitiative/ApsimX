using DocumentFormat.OpenXml.Presentation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Extras
{
    /// <summary>
    /// A class to track the mean of a series of added values.
    /// </summary>
    public class MeanTracker
    {
        private int n = 0;
        private double sum  = 0;

        /// <summary>
        /// The current mean of the added values.
        /// </summary>
        public double Mean { get; private set; } = 0;

        /// <summary>
        /// Default constructor for MeanTracker.
        /// </summary>
        public MeanTracker()
        {
                
        }

        /// <summary>
        /// Constructor for MeanTracker with an initial value.
        /// </summary>
        /// <param name="initialValue">Intial value</param>
        public MeanTracker(double initialValue)
        {
            AddValue(initialValue);
        }

        /// <summary>
        /// Adds a value to the tracker and updates the mean.
        /// </summary>
        /// <param name="value">Value to add</param>
        public void AddValue(double value)
        {
            n++;
            sum += value;
            Mean = sum / n;
        }
    }
}
