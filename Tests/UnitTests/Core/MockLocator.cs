using System;
using Models.Core;
using Models;
using System.Collections.Generic;

namespace UnitTests.Core
{
    internal class MockLocator : ILocator
    {
        public Dictionary<string, IVariable> Values = new Dictionary<string, IVariable>();

        public MockLocator()
        {
        }

        public IModel Get(Type typeToMatch)
        {
            throw new NotImplementedException();
        }

        public object Get(string namePath)
        {
            if (Values.ContainsKey(namePath))
                return Values[namePath].Value;
            return null;
        }

        public IVariable GetObject(string namePath)
        {
            if (Values.ContainsKey(namePath))
                return Values[namePath];
            return null;
        }
    }
}