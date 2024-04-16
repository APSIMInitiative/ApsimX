using System;

namespace Models.Core
{
    /// <summary>
    /// An interface for publishing / subscribing to events.
    /// </summary>
    public interface IEvent
    {
        /// <summary>
        /// Subscribe to an event. Will throw if namePath doesn't point to a event publisher.
        /// </summary>
        /// <param name="eventName">The name of the event to subscribe to</param>
        /// <param name="handler">The event handler</param>
        void Subscribe(string eventName, EventHandler handler);

        /// <summary>
        /// Unsubscribe an event. Throws if not found.
        /// </summary>
        /// <param name="eventName">The name of the event to subscribe to</param>
        /// <param name="handler">The event handler</param>
        void Unsubscribe(string eventName, EventHandler handler);

        /// <summary>Connect all events in the specified simulation.</summary>
        void ConnectEvents();

        /// <summary>Disconnect and reconnect specified event in the specified simulation
        /// This ensures correct tree-based model order after models are added during runtime</summary>
        /// <param name="publisherName">Name of publishing model</param>
        /// <param name="eventName">Name of event</param>
        void ReconnectEvents(string publisherName = null, string eventName = null);

        /// <summary>Connect all events in the specified simulation.</summary>
        void DisconnectEvents();

    }
}