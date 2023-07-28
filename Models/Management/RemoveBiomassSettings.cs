
using Models.Core;
using Models.Functions;
using Models.PMF.Interfaces;
using Models.PMF.Phen;
using System;
using System.Collections.Generic;


namespace Models.Management
{
    /// <summary>
    /// Steps through each OrganBiomassRemoval child when Do() method is called and removes the specified fractions of biomass from each.  
    /// Organ names must match the name of an organ in the specified crop.  
    /// Biomass will only be removed from organs that are sepcified with an OrganBiomassRemoval child on this class.  
    /// Add Child of name "StageSet" to specify a phenology rewind when ever the Do() method is called
    /// </summary>
    [ValidParent(ParentType = typeof(Zone))]
    [ValidParent(ParentType = typeof(Folder))]
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class RemoveBiomassSettings : Model
    {
        /// <summary>
        /// List of possible biomass removal types
        /// </summary>
        public enum RemovalTypes
        {
            /// <summary>Bioimass is cut</summary>
            Cutting,
            /// <summary>Bioimass is grazed</summary>
            Grazing,
            /// <summary>Bioimass is harvested</summary>
            Harvesting,
            /// <summary>Bioimass is pruned</summary>
            Pruning
        }

        /// <summary>Name of the crop to remove biomass from</summary>
        [Separator("Add children of type 'OrganBiomassRemoval' with names matching organs of plant to have biomass removed.  " +
            "\nAdd child of type 'IFunction' and name 'SetStage' to have phenology reset.  " +
            "\nCall the Do() method on this class to inact removal and rewinds sepcified")]
        [Description("Crop to remove biomass from")]
        public IPlant Crop { get; set; }

        /// <summary>
        /// choose removal types
        /// </summary>
        [Description("Type of biomass removal.  This triggers events OnCutting, OnGrazing etc")]
        public RemovalTypes removaltp { get; set; }

        /// <summary>Stage to reset phenology to</summary>
        [Link(Type = LinkType.Child, ByName = true, IsOptional = true)]
        public IFunction SetStage { get; set; }

        /// <summary>List of organs for current crop</summary>
        [Link]
        protected List<IOrgan> Organs = new List<IOrgan>();

        [Link(Type = LinkType.Scoped, ByName = true)]
        private Phenology phenology = null;

        /// <summary>Cutting Event</summary>
        public event EventHandler<EventArgs> Cutting;

        /// <summary>Grazing Event</summary>
        public event EventHandler<EventArgs> Grazing;

        /// <summary>Pruning Event</summary>
        public event EventHandler<EventArgs> Pruning;

        /// <summary>Harvesting Event</summary>
        public event EventHandler<EventArgs> Harvesting;

        /// <summary>Method that applies specified removal fractions and rewind</summary>
        public void Do()
        {
            foreach (OrganBiomassRemoval os in this.FindAllChildren<OrganBiomassRemoval>())
            {
                double liveRemoved = os.liveToRemove != null ? os.liveToRemove.Value() : 0;
                double deadRemoved = os.deadToRemove != null ? os.deadToRemove.Value() : 0;
                double liveToResidues = os.liveToResidue != null ? os.liveToResidue.Value() : 0;
                double deadToResidues = os.deadToResidue != null ? os.deadToResidue.Value() : 0;

                foreach (IOrgan org in Organs)
                {
                    if (org.Name == os.Name)
                    {
                        (org as IHasDamageableBiomass).RemoveBiomass(liveToRemove: liveRemoved,
                                deadToRemove: deadRemoved,
                                liveToResidue: liveToResidues,
                                deadToResidue: deadToResidues);
                    }
                }
            }

            if (removaltp.ToString() == RemovalTypes.Cutting.ToString())
                Cutting?.Invoke(this, new EventArgs());
            if (removaltp.ToString() == RemovalTypes.Grazing.ToString())
                Grazing?.Invoke(this, new EventArgs());
            if (removaltp.ToString() == RemovalTypes.Pruning.ToString())
                Pruning?.Invoke(this, new EventArgs());
            if (removaltp.ToString() == RemovalTypes.Harvesting.ToString())
                Harvesting?.Invoke(this, new EventArgs());

            if (SetStage != null)
                phenology.SetToStage(SetStage.Value());
        }

        /// <summary>Constructor</summary>
        public RemoveBiomassSettings(){}
    }
}
