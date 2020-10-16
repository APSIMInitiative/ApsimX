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
        public Guid ID { get; private set; }

        /// <summary>
        /// New value of the property.
        /// </summary>
        public object NewValue { get; set; }
        
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="id">ID of the property which has been changed.</param>
        /// <param name="value">New value of the property.</param>
        public PropertyChangedEventArgs(Guid id, object value)
        {
            ID = id;
            NewValue = value;
        }
    }
}