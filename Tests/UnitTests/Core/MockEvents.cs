using System;
using System.Collections.Generic;
using Models;
using Models.Core;

namespace UnitTests.Core
{
    internal class MockEvents : IEvent
    {
        public MockEvents()
        {
        }

        public void ConnectEvents()
        {
            throw new NotImplementedException();
        }

        public void ConnectEvents(List<IModel> models)
        {
            throw new NotImplementedException();
        }

        public void DisconnectEvents()
        {
            throw new NotImplementedException();
        }

        public void Subscribe(string eventName, EventHandler handler)
        {
        }

        public void Unsubscribe(string eventName, EventHandler handler)
        {
        }
    }
}