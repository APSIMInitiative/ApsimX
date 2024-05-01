using System;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Functions;
using Models.Soils;

namespace Models.LifeCycle
{
    /// <summary>
    /// For Pests/Diseases that reduce the water uptake of a plant model.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(LifeCyclePhase))]
    public class KLDamage : Model
    {
        [Link]
        private Soil Soil = null;

        /// <summary>Returns the potential damage that an individual can cause per day</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("0-1")]
        private IFunction reduction = null;

        /// <summary>Initial KL values, before modification.</summary>
        private double[] initialKL;

        /// <summary>The soil crop parameterisation to change KLs in.</summary>
        [Description("Select soil/crop parameterisation to change KLs in")]
        public SoilCrop SoilCrop { get; set; }


        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            if (SoilCrop == null)
                throw new Exception($"No soil/crop specified in {Name}");
            SoilCrop = Soil.FindDescendant<SoilCrop>(SoilCrop.Name);
            initialKL = SoilCrop.KL.Clone() as double[];
        }

        [EventSubscribe("DoPestDiseaseDamage")]
        private void DoPestDiseaseDamage(object sender, EventArgs e)
        {
            SoilCrop.KL = MathUtilities.Multiply_Value(initialKL, reduction.Value());
        }
    }
}
