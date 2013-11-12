using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using System.ComponentModel;
using Models.Plant.Functions;


namespace Models.Plant.Phen
{
    abstract public class Phase
    {
        public string Start;

        public string End;

        [Link]
        public Component My;

        [Link]
        public Phenology Phenology = null;

        [Link(IsOptional = true)]
        public Function ThermalTime = null;  //FIXME this should be called something to represent rate of progress as it is sometimes used to represent other things that are not thermal time.

        [Link(IsOptional = true)]
        public Function Stress = null;

        protected double PropOfDayUnused = 0;
        protected double _TTForToday = 0;
        
        public double TTForToday
        {
            get
            {
                if (ThermalTime == null)
                    return 0;
                return ThermalTime.Value;
            }
        }
        protected double _TTinPhase = 0;
        
        public double TTinPhase { get { return _TTinPhase; } }

        public string Name { get { return this.Name; } }

        /// <summary>
        /// This function increments thermal time accumulated in each phase 
        /// and returns a non-zero value if the phase target is met today so
        /// the phenology class knows to progress to the next phase and how
        /// much tt to pass it on the first day.
        /// </summary>
        virtual public double DoTimeStep(double PropOfDayToUse)
        {
            // Calculate the TT for today and Accumulate.      
            _TTForToday = ThermalTime.Value * PropOfDayToUse;
            if (Stress != null)
            {
                _TTForToday *= Stress.Value;
            }
            _TTinPhase += _TTForToday;

            return PropOfDayUnused;
        }

        // Return proportion of TT unused
        virtual public double AddTT(double PropOfDayToUse)
        {
            _TTinPhase += ThermalTime.Value * PropOfDayToUse;
            return 0;
        }
        virtual public void Add(double dlt_tt) { _TTinPhase += dlt_tt; }
        abstract public double FractionComplete { get; }

        [EventSubscribe("Initialised")]
        private void OnInitialised(object sender, EventArgs e) { ResetPhase(); }
        public virtual void ResetPhase()
        {
            _TTinPhase = 0;
            PropOfDayUnused = 0;
        }


        internal virtual void WriteSummary()
        {
            Console.WriteLine("      " + Name);
        }
    }
}
