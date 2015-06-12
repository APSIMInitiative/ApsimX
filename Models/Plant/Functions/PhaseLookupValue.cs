using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.PMF.Phen;

namespace Models.PMF.Functions
{
    /// <summary>
    /// Returns the value of it child function to the PhaseLookup parent function if current phenology is between Start and end stages specified.
    /// </summary>
    [Serializable]
    [Description("Returns the value of it child function to the PhaseLookup parent function if current phenology is between Start and end stages specified.")]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PhaseLookupValuePresenter")]
    public class PhaseLookupValue : Model, IFunction
    {
        /// <summary>The phenology</summary>
        [Link]
        Phenology Phenology = null;

        /// <summary>The child functions</summary>
        private List<IModel> ChildFunctions;

        /// <summary>The start</summary>
        [Description("Start")]
        public string Start { get; set; }

        /// <summary>The end</summary>
        [Description("End")]
        public string End { get; set; }

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        /// <exception cref="System.Exception">
        /// Phase start name not set: + Name
        /// or
        /// Phase end name not set: + Name
        /// </exception>
        public double Value
        {
            get
            {
                if (ChildFunctions == null)
                    ChildFunctions = Apsim.Children(this, typeof(IFunction));

                if (Start == "")
                    throw new Exception("Phase start name not set:" + Name);
                if (End == "")
                    throw new Exception("Phase end name not set:" + Name);

                if (Phenology.Between(Start, End) && ChildFunctions.Count > 0)
                {
                    IFunction Lookup = ChildFunctions[0] as IFunction;
                    return Lookup.Value;
                }
                else
                    return 0.0;
            }
        }

        /// <summary>Gets a value indicating whether [in phase].</summary>
        /// <value><c>true</c> if [in phase]; otherwise, <c>false</c>.</value>
        public bool InPhase
        {
            get
            {
                return Phenology.Between(Start, End);
            }
        }
    }

}