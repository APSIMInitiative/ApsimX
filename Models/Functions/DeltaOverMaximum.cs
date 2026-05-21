using System;
using APSIM.Core;
using Models.Core;
using Models.PMF.Phen;

namespace Models.Functions
{
    /// <summary>
    /// This function returns change (delta) of a value from yesterday to today, over the maximum that delta that has been recorded.
    /// When the value is requested, if that day's delta exceed the maximum, it will update the maximum to the new calculated delta.
    /// Today and yesterday values are calculated at the start of day to prevent event ordering issues.
    /// If Maximum is 0, will return 0 instead.
    /// 
    /// First used in the Cotton model.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Description("")]
    public class DeltaOverMaximum : Model, IFunction
    {
        /// <summary></summary>
        [Link(Type = LinkType.Child)]
        private IFunction child = null;

        private double _yesterday = 0;
        private double _today = 0;

        /// <summary>
        /// 
        /// </summary>
        public double _delta {get; set;} = 0;

        /// <summary>
        /// 
        /// </summary>
        public double _maximum  {get; set;} = 0;

        private bool _initialisedToday = true;

        /// <summary>The stage to start calculating</summary>
        [Description("The stage to start calculating")]
        [Display(Type = DisplayType.CropStageName)]
        public string StageToStartCalculating { get; set; }

        /// <summary>
        /// Invoked at the start of each day.
        /// </summary>
        [EventSubscribe("EndOfDay")]
        private void OnEndOfDay(object sender, EventArgs args)
        {
            _yesterday = _today;
            _today = child.Value();
            _delta = _today - _yesterday;
            if (_delta > _maximum)
                _maximum = _delta;

            if (_initialisedToday)
            {
                _initialisedToday = false;
                _maximum = 0;
            }
        }

        /// <summary>Called when [phase changed].</summary>
        /// <param name="phaseChange">The phase change.</param>
        /// <param name="sender">Sender plant.</param>
        [EventSubscribe("PhaseChanged")]
        private void OnPhaseChanged(object sender, PhaseChangedType phaseChange)
        {
            if (phaseChange.StageName == StageToStartCalculating)
                _initialisedToday = true;
        }

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            if (_initialisedToday || _maximum == 0)
                return 1;
            else
                return _delta / _maximum;
        }
    }
}
