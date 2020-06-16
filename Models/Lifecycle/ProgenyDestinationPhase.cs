namespace Models.LifeCycle
{
    using Models.Core;
    using Models.Functions;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;

    /// <summary>
    /// This class specifies which life cycle and which phase the progeny of the parent 
    /// LifeCyclePhase will be added to.  More that one ReproductionDestinationPhase can 
    /// be added and the ProportionOfProgeny property determines what proportion of the
    /// total progeny are added to this ReproductionDestinationPhase
    /// </summary>

    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(LifeCyclePhase))]
    public class ProgenyDestinationPhase : Model
    {
        /// <summary> Specifies the destination LifeCycle that progeney from this LifeCyclePhaseReproduction will be created in</summary>
        [Description("Select Life cycle that progeny will be added to")]
        [Display(Type = DisplayType.LifeCycleName)]
        public string NameOfLifeCycleForProgeny { get; set; }

        /// <summary> Specifies the destination LifeCyclePhase that progeney from this LifeCyclePhaseReproduction will be created in</summary>
        [Description("Select Life cycle phase that progeny will be added to")]
        [Display(Type = DisplayType.LifePhaseName)]
        public string NameOfPhaseForProgeny { get; set; }

        /// <summary> Specifies the proportion of the parent phases progeny that are added to the destination phase</summary>
        [Link(IsOptional = true, Type = LinkType.Child, ByName = true)]
        private IFunction proportionOfProgeny = null;

        /// <summary> Returns the proportion of the parent phases progeny that are added to the destination phase</summary>
        public double ProportionOfProgeny
        {
            get
            {
                if (proportionOfProgeny != null)
                    return proportionOfProgeny.Value();
                else return 1.0;
            }
        }

        /// <summary> the number of projeny added to the destination by this phase</summary>
        public double ProgenyToDestination { get; set; }
    }
}
