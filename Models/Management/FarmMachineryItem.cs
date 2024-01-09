using System;
using System.Collections.Generic;
using APSIM.Shared.Documentation;
using APSIM.Shared.Utilities;
using Models.Core;

// TODO
// work rates: each tractor should display a grid of work rates & fuel consumption for each implement combo
// capital costs?

namespace Models.Management
{
    /// <summary>
    /// A piece of machinery managed by the machinery component
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(FarmMachinery))]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class FarmMachineryItem : Model
    {
        /// <summary>Gets the type.</summary>
        [Description("The type of machine")]
        public MachineryType MachineryType { get; set; }

        /// <summary>Gets the optional units</summary>
        [Description("??grease, oil & maintenance costs, capital repayments??")]
        public string xyz { get; set; }
    }
    /// <summary>The type of machinery this is</summary>
    public enum MachineryType {
        /// <summary> A Tractor </summary>
        Tractor, 
        /// <summary> An implement </summary>
        Implement
    }
}