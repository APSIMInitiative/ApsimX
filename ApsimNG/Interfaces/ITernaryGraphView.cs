using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApsimNG.Interfaces
{
    interface ITernaryGraphView
    {
        /// <summary>
        /// The value of one of the variables to be shown.
        /// </summary>
        double X { get; set; }

        /// <summary>
        /// The value of one of the variables to be shown.
        /// </summary>
        double Y { get; set; }

        /// <summary>
        /// The value of one of the variables to be shown.
        /// </summary>
        double Z { get; set; }

        /// <summary>
        /// X, Y, and Z must add to this value.
        /// </summary>
        double Total { get; set; }

        /// <summary>
        /// Show the graph.
        /// </summary>
        void Show();

        /// <summary>
        /// Perform cleanup.
        /// </summary>
        void Detach();
    }
}
