namespace Models
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Core.Interfaces;
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;

    /// <summary>
    /// The rotation manager model
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.RotBubbleChartView")]
    [PresenterName("UserInterface.Presenters.RotBubbleChartPresenter")]
    [ValidParent(ParentType = typeof(Simulation))]
    [ValidParent(ParentType = typeof(Zone))]
    [ValidParent(ParentType = typeof(Agroforestry.AgroforestrySystem))]
    public class RotBubbleChartView : Model, IOptionallySerialiseChildren
    {
        /// <summary>Allow children to be serialised?</summary>
        public bool DoSerialiseChildren { get { return false; } }

    }


}