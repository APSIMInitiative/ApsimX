using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.PMF.Phen;

namespace Models.PMF.Functions
{
    /// <summary>
    /// Returns a value of 1 if phenology is between start and end phases and otherwise a value of 0.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class PhaseBasedSwitch : Model, IFunction
    {
        //Fixme.  This can be removed an phase lookup returnig a constant of 1 if in phase.

        /// <summary>The phenology</summary>
        [Link]
        Phenology Phenology = null;

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
                if (Start == "")
                    throw new Exception("Phase start name not set:" + Name);
                if (End == "")
                    throw new Exception("Phase end name not set:" + Name);

                if (Phenology.Between(Start, End))
                {
                    return 1.0;
                }
                else
                    return 0.0;
            }
        }
    }
}


