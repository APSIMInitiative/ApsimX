﻿using System;
using APSIM.Core;
using DocumentFormat.OpenXml.Office.CustomXsn;
using Models.Core;
using Models.Management;
using Newtonsoft.Json;

namespace Models.PMF.Phen
{
    /// <summary>
    /// When the specified start phase is reached, phenology is rewound to
    /// a specified phase.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Phenology))]
    public class GotoPhase : Model, IPhase, IScopeDependency
    {
        /// <summary>Scope supplied by APSIM.core.</summary>
        [field: NonSerialized]
        public IScope Scope { private get; set; }

        // 1. Links
        //----------------------------------------------------------------------------------------------------------------

        [Link]
        private Phenology phenology = null;


        //5. Public properties
        //-----------------------------------------------------------------------------------------------------------------
        /// <summary>The start</summary>
        [Description("Start")]
        public string Start { get; set; }

        /// <summary>The end stage name.</summary>
        public string End
        {
            get
            {
                if (phenology == null)
                    phenology = Scope.Find<Phenology>();
                return phenology.FindChild<IPhase>(PhaseNameToGoto)?.Start;
            }
        }
        /// <summary>Is the phase emerged from the
        /// ground?</summary>
        [Description("Is the phase emerged?")]
        public bool IsEmerged { get; set; } = true;

        /// <summary>The phase name to goto</summary>
        [Description("PhaseNameToGoto")]
        [Display(Type = DisplayType.CropPhaseName)]
        public string PhaseNameToGoto { get; set; }

        /// <summary>
        /// The type of biomass removal event
        /// </summary>
        [Description("Type of biomass removal.  This triggers events OnCutting, OnGrazing etc")]
        public BiomassRemovalType RemovalType
        {
            get { return _removalType; }
            set { _removalType = value; }
        }

        [JsonIgnore]
        private BiomassRemovalType _removalType { get; set; }

        /// <summary>Gets the fraction complete.</summary>
        [JsonIgnore]
        public double FractionComplete { get; }

        /// <summary>Cutting Event</summary>
        public event EventHandler<BiomassRemovalEventArgs> PhenologyDefoliate;

        //6. Public methods
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>Should not be called in this class</summary>
        public bool DoTimeStep(ref double PropOfDayToUse)
        {
            phenology.SetToStage((double)phenology.IndexFromPhaseName(PhaseNameToGoto) + 1);
            BiomassRemovalEventArgs breg = new BiomassRemovalEventArgs();
            breg.RemovalType = RemovalType;
            PhenologyDefoliate?.Invoke(this, breg);
            PropOfDayToUse = 1.0;
            return true;
        }

        /// <summary>Resets the phase.</summary>
        public virtual void ResetPhase() { }
    }
}
