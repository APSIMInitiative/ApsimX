using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;

namespace Models.PMF.Functions
{
    /// <summary>
    /// Returns the value of PreEventValue child function from Initialisation to SetEvent, PostEventValue from ReSetEvent and PreEventValue again from ReSetEvent to the next SetEvent
    /// </summary>
    [Serializable]
    [Description("Returns the value of PreEventValue child function from Initialisation to SetEvent, PostEventValue from ReSetEvent and PreEventValue again from ReSetEvent to the next SetEvent")]
    public class OnEventFunction : Function
    {
        /// <summary>The _ value</summary>
        private double _Value = 0;

        /// <summary>The set event</summary>
        public string SetEvent = "";
        /// <summary>The re set event</summary>
        public string ReSetEvent = "";


        /// <summary>The pre event value</summary>
        [Link] Function PreEventValue = null;
        /// <summary>The post event value</summary>
        [Link] Function PostEventValue = null;

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            //Fixme, this needs to be fixed to respond to change and reset events
            //MyPaddock.Subscribe(SetEvent, OnSetEvent);
            //MyPaddock.Subscribe(ReSetEvent, OnReSetEvent);
            _Value = PreEventValue.Value;
        }

        //[EventSubscribe("")]
        //public void OnInit()
        //{
        //    _Value = PreEventValue.Value;
        //}

        /// <summary>Called when [re set event].</summary>
        public void OnReSetEvent()
        {
            _Value = PreEventValue.Value;
        }

        /// <summary>Called when [set event].</summary>
        public void OnSetEvent()
        {
            _Value = PostEventValue.Value;
        }


        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public override double Value
        {
            get
            {
                return _Value;
            }
        }

    }

}