using System;
using APSIM.Core;
using Models.Core;

namespace Models.Functions
{
    /// <summary>
    /// This function returns the daily delta for its child function
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Description("")]
    public class ValueApproachingMovingMaximumFunction : Model, IFunction
    {
        /// <summary></summary>
        [Link(Type = LinkType.Child)]
        private IFunction child = null;

        private double _yesterday = 0;
        private double _day_before_yesterday = 0;
        private double _maximum = 0;

        /// <summary>
        /// Invoked at the end of each day. Updates the stored value.
        /// </summary>
        [EventSubscribe("EndOfDay")]
        private void OnEndOfDay(object sender, EventArgs args)
        {
            _day_before_yesterday = _yesterday;
            _yesterday = child.Value();
        }

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            double delta = child.Value() - _day_before_yesterday;

            if (delta > _maximum)
                _maximum = delta;

            double deltaOverMax = delta / _maximum;

            return 1 - deltaOverMax;
        }
    }
}
