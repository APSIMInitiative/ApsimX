using System;
using Models.Report;
using Models.Core;

namespace UnitTests
{
    internal class MockEvents : IEvent
    {
        public MockEvents()
        {
        }

        public void Subscribe(string eventName, EventHandler handler)
        {
        }

        public void Unsubscribe(string eventName, EventHandler handler)
        {
        }
    }
}