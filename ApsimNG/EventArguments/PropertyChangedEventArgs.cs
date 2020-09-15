namespace UserInterface.EventArguments
{
    using System;

    /// <summary>
    /// Event arguments for a PropertyChanged event.
    /// </summary>
    public class PropertyChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Name of the property which has been changed.
        /// </summary>
        public string PropertyName { get; private set; }

        /// <summary>
        /// New value of the property.
        /// </summary>
        public string NewValue { get; set; }
        
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">Name of the property which has been changed.</param>
        /// <param name="value">New value of the property.</param>
        public PropertyChangedEventArgs(string name, string value)
        {
            PropertyName = name;
            NewValue = value;
        }
    }
}