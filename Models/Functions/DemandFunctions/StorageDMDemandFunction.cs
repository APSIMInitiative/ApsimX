using System;
using System.Collections.Generic;
using Models.Core;
using Models.PMF.Interfaces;
using APSIM.Shared.Utilities;
using APSIM.Shared.Documentation;

namespace Models.Functions.DemandFunctions
{
    /// <summary>The partitioning of daily growth to storage biomass is based on a storage fraction.</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class StorageDMDemandFunction : Model, IFunction
    {
        /// <summary>The Storage Fraction</summary>
        [Description("StorageFraction")]
        [Link(Type = LinkType.Child, ByName = true)]
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
                        throw new Exception(Name + "cannot find parent organ to get Structural and Storage DM status");
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

        /// <summary>Document the model.</summary>
        public override IEnumerable<ITag> Document()
        {
            // Write description of this class from summary and remarks XML documentation.
            foreach (var tag in GetModelDescription())
                yield return tag;

            foreach (var tag in DocumentChildren<IModel>())
                yield return tag;
        }
    }
}
