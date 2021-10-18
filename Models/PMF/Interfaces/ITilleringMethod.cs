using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.PMF.Interfaces
{
    /// <summary>
    /// Interface for managing tillering.
    /// Tillers are stored in Culms in the Leaf organ where the first Culm is the main stem and the remaining culms are the tillers.
    /// </summary>
    public interface ITilleringMethod : IModel
    {
        /// <summary> Update number of leaves for all culms </summary>
        void UpdateLeafNumber();

        /// <summary> 
        /// Update potential number of tillers for all culms as well as the current number of active tillers.
        /// </summary>
        void UpdateTillerNumber();

        /// <summary> Calculate the potential leaf area before inputs are updated</summary>
        double CalculatePotentialLeafArea();
        
        /// <summary> Calculate the actual leaf area once inputs are known</summary>
        double CalculateActualLeafArea();
    }
}
