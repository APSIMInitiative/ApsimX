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
    public class MigrantDestinationPhase : Model
    {
        /// <summary> Specifies the destination LifeCycle that migrants from this LifeCyclePhaseReproduction will be created in</summary>
        [Description("Select Life cycle that progeny will be added to")]
        [Display(Type = DisplayType.LifeCycleName)]
        public string NameOfLifeCycleForMigrants { get; set; }

        /// <summary> Specifies the destination LifeCyclePhase that migrants from this LifeCyclePhaseReproduction will be created in</summary>
        [Description("Select Life cycle phase that progeny will be added to")]
        [Display(Type = DisplayType.LifePhaseName)]
        public string NameOfPhaseForMigrants { get; set; }

        /// <summary> specifies the proportion of the parent phases migrants that are added to the destination phase</summary>
        [Link(IsOptional = true, Type = LinkType.Child, ByName = true)]
        private IFunction proportionOfMigrants = null;

        /// <summary> Returns the proportion of the parent phases migrants that are added to the destination phase</summary>
        public double ProportionOfMigrants
        {
            get
            {
                if (proportionOfMigrants != null)
                    return proportionOfMigrants.Value();
                else return 1.0;
            }
        }
    }
}
