using System;
using Models.Core;
using Models.PMF.Interfaces;
using APSIM.Shared.Utilities;

namespace Models.Functions.DemandFunctions
{
    /// <summary>
    /// # [Name]
    /// Calculate partitioning of daily growth based upon allometric relationship
    /// </summary>
    [Serializable]
    [Description("This function calculated dry matter demand using plant allometry which is described using a simple power function (y=kX^p).")]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class StorageDMDemandFunction : Model, IFunction
    {
        /// <summary>The Storage Fraction</summary>
        [Description("StorageFraction")]
        [Link]
        private IFunction storageFraction = null;

        private IArbitration parentOrgan = null;

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            bool ParentOrganIdentified = false;
            IModel ParentClass = this.Parent;
            while (!ParentOrganIdentified)
            {
                if (ParentClass is IArbitration)
                {
                    parentOrgan = ParentClass as IArbitration;
                    ParentOrganIdentified = true;
                    if (ParentClass is IPlant)
                        throw new Exception(Name + "cannot find parent organ to get Structural and storage DM status");
                }
                ParentClass = ParentClass.Parent;
            }
        }

        /// <summary>Gets the value.</summary>
        public double Value(int arrayIndex = -1)
        {
            double structuralWt = parentOrgan.Live.StructuralWt + parentOrgan.DMDemand.Structural;
            double MaximumDM = MathUtilities.Divide(structuralWt,1-storageFraction.Value(), 0);
            double AlreadyAllocated = structuralWt + parentOrgan.Live.StorageWt;
            return MaximumDM - AlreadyAllocated;
        }

    }
}
