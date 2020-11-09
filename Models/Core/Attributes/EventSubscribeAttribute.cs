namespace Models.Core
{
    using System;

    /// <summary>
    /// Specifies that the related method should be called whenever an event
    /// is invoked that has the specified name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class EventSubscribeAttribute : System.Attribute
    {
        /// <summary>
        /// The event name being subscribed to.
        /// </summary>
        private string name;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventSubscribeAttribute" /> class.
        /// </summary>
        /// <param name="name">Name of the event being subscribed to</param>
        public EventSubscribeAttribute(string name)
        {
            this.name = name;
        }

        /// <summary>
        /// Gets the name of the event.
        /// </summary>
        /// <returns>The name of the event being subscribed to</returns>
        public override string ToString()
        {
            return this.name;
        }
    } 
}
