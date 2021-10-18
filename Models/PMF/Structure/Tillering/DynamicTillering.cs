using DocumentFormat.OpenXml.Bibliography;
using Models.Core;
using Models.PMF.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.PMF.Struct
{
    /// <summary>
    /// This is a tillering method to control the number of tillers and leaf area
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Tillering))]
    public class DynamicTillering : Model, ITilleringMethod
    {
        /// <summary> Update number of leaves for all culms </summary>
        public void UpdateLeafNumber() {  }

        /// <summary> 
        /// Update potential number of tillers for all culms as well as the current number of active tillers.
        /// </summary>
        public void UpdateTillerNumber() { }

        /// <summary> Calculate the potential leaf area before inputs are updated</summary>
        public double CalculatePotentialLeafArea() { return 0.0; }

        /// <summary> Calculate the actual leaf area once inputs are known</summary>
        public double CalculateActualLeafArea() { return 0.0; }

    }

}
