using System;
using APSIM.Core;
using Models.Core;

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
        private double _delta = 0;
        private double _maximum = 0;

        /// <summary>
        /// Invoked at the start of each day.
        /// </summary>
        [EventSubscribe("StartOfDay")]
        private void OnStartOfDay(object sender, EventArgs args)
        {
            _yesterday = _today;
            _today = child.Value();
            _delta = _today - _yesterday;
            if (_delta > _maximum)
                _maximum = _delta;
        }

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            if (_maximum == 0)
                return 0;
            else
                return _delta / _maximum;
        }
    }
}
