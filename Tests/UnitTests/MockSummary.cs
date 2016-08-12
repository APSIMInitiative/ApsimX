using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnitTests
{
    [Serializable]
    class MockSummary : ISummary
    {
        public static List<string> messages = new List<string>();

        public void WriteMessage(IModel model, string message)
        {
            throw new NotImplementedException();
        }

        public void WriteMessage(object model, string message)
        {
            messages.Add(message);
        }

        public void WriteWarning(IModel model, string message)
        {
            throw new NotImplementedException();
        }

        public void WriteWarning(object model, string message)
        {
            messages.Add("WARNING: " + message);
        }
    }
}
