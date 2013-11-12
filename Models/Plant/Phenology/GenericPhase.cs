using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.Plant.Functions;

namespace Models.Plant.Phen
{
    public class GenericPhase : Phase
    {
        [Link(IsOptional = true)]
        public Function Target = null;

        /// <summary>
        /// This function increments thermal time accumulated in each phase 
        /// and returns a non-zero value if the phase target is met today so
        /// the phenology class knows to progress to the next phase and how
        /// much tt to pass it on the first day.
        /// </summary>
        public override double DoTimeStep(double PropOfDayToUse)
        {

            base.DoTimeStep(PropOfDayToUse);

            // Get the Target TT
            double Target = CalcTarget();


            if (_TTinPhase > Target)
            {
                double LeftOverValue = _TTinPhase - Target;
                if (_TTForToday > 0.0)
                {
                    double PropOfValueUnused = LeftOverValue / ThermalTime.Value;
                    PropOfDayUnused = PropOfValueUnused * PropOfDayToUse;
                }
                else
                    PropOfDayUnused = 1.0;
                _TTinPhase = Target;
            }

            return PropOfDayUnused;
        }

        /// <summary>
        /// Return the target to caller. Can be overridden by derived classes.
        /// </summary>
        protected virtual double CalcTarget()
        {
            if (Target == null)
                throw new Exception("Cannot find target for phase: " + Name);
            return Target.Value;
        }
        // Return proportion of TT unused
        public override double AddTT(double PropOfDayToUse)
        {
            _TTinPhase += ThermalTime.Value * PropOfDayToUse;
            double AmountUnusedTT = _TTinPhase - CalcTarget();
            if (AmountUnusedTT > 0)
                return AmountUnusedTT / ThermalTime.Value;
            return 0;
        }
        /// <summary>
        /// Return a fraction of phase complete.
        /// </summary>
        public override double FractionComplete
        {
            get
            {
                return _TTinPhase / CalcTarget();
            }
        }

        internal override void WriteSummary()
        {
            base.WriteSummary();
            if (Target != null)
                Console.WriteLine(string.Format("         Target                    = {0,8:F0} (dd)", Target.Value));
        }

    }

}
      
      
