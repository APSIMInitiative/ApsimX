using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnitTests
{
    [Serializable]
    class MockSummary : Model,ISummary
    {
        public static List<string> messages = new List<string>();

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
            messages.Add("WARNING: " + message);
        }
    }
}
