using Models.Core;
using System;
using System.Collections.Generic;

namespace UnitTests
{
    [Serializable]
    class MockSummary : Model,ISummary
    {
        public List<string> messages = new List<string>();

        public void WriteMessage(IModel model, string message)
        {
            messages.Add(message);
        }

        public void WriteMessage(object model, string message)
        {
            messages.Add(message);
        }

        public void WriteWarning(IModel model, string message)
        {
            messages.Add("WARNING: " + message);
        }

        public void WriteWarning(object model, string message)
        {
            messages.Add("WARNING: " + message);
        }
        public void WriteError(IModel model, string message)
        {
            messages.Add("ERROR: " + message);
        }

        public void WriteMessage(IModel model, string message, MessageType messageType)
        {
            switch (messageType)
            {
                case MessageType.Error:
                    WriteError(model, message);
                    break;
                case MessageType.Warning:
                    WriteWarning(model, message);
                    break;
                case MessageType.Information:
                    WriteMessage(model, message);
                    break;
                default:
                    messages.Add(message);
                    break;
            }
        }

        public void WriteMessagesToDataStore()
        {
            return;
        }
    }
}
