using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;

namespace Models.PMF.Functions
{
    [Description("Returns the value of PreEventValue child function from Initialisation to SetEvent, PostEventValue from ReSetEvent and PreEventValue again from ReSetEvent to the next SetEvent")]
    class OnEventFunction : Function
    {
        private double _Value = 0;

        public string SetEvent = "";
        public string ReSetEvent = "";


        public Function PreEventValue { get; set; }

        public Function PostEventValue { get; set; }

        [EventSubscribe("Initialised")]
        private void OnInitialised(object sender, EventArgs e)
        {
            //Fixme, this needs to be fixed to respond to change and reset events
            //MyPaddock.Subscribe(SetEvent, OnSetEvent);
            //MyPaddock.Subscribe(ReSetEvent, OnReSetEvent);
            _Value = PreEventValue.FunctionValue;
        }

        //[EventSubscribe("")]
        //public void OnInit()
        //{
        //    _Value = PreEventValue.Value;
        //}

        public void OnReSetEvent()
        {
            _Value = PreEventValue.FunctionValue;
        }

        public void OnSetEvent()
        {
            _Value = PostEventValue.FunctionValue;
        }

        
        public override double FunctionValue
        {
            get
            {
                return _Value;
            }
        }

    }

}