using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;

namespace Models.PMF.Functions
{
    [Serializable]
    [Description("Returns the value of PreEventValue child function from Initialisation to SetEvent, PostEventValue from ReSetEvent and PreEventValue again from ReSetEvent to the next SetEvent")]
    public class OnEventFunction : Function
    {
        private double _Value = 0;

        public string SetEvent = "";
        public string ReSetEvent = "";


        [Link] Function PreEventValue = null;
        [Link] Function PostEventValue = null;

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

        public void OnReSetEvent()
        {
            _Value = PreEventValue.Value;
        }

        public void OnSetEvent()
        {
            _Value = PostEventValue.Value;
        }

        
        public override double Value
        {
            get
            {
                return _Value;
            }
        }

    }

}