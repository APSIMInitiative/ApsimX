using System;
using Models.Core;
using Models;
using System.Collections.Generic;
using APSIM.Core;

namespace UnitTests.Core
{
    internal class MockLocator : ILocator
    {
        public Dictionary<string, IVariable> Values = new Dictionary<string, IVariable>();

        public MockLocator()
        {
        }

        public object Get(string namePath, LocatorFlags flags = LocatorFlags.None)
        {
            if (Values.ContainsKey(namePath))
                return Values[namePath].Value;
            return null;
        }

        public IVariable GetObject(string namePath, LocatorFlags flags)
        {
            if (Values.ContainsKey(namePath))
                return Values[namePath];
            return null;
        }

        public IVariable GetObjectProperties(string namePath, LocatorFlags flags = LocatorFlags.None)
        {
            throw new NotImplementedException();
        }
    }
}