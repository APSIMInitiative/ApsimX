using System;

namespace UserInterface.EventArguments
{
    /// <summary>
    /// An EventArgs for passing back a value alongside the event
    /// </summary>
    public class EventArgsValue : EventArgs
    {
        /// <summary>
        /// Value to return with event args
        /// </summary>
        public int Value {get; set;}

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="value">Value to return</param>
        public EventArgsValue(int value)
        {
            Value = value;
        }
    } 
}
