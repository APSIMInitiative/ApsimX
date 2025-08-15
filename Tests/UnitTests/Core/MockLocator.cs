using System;
using Models.Core;
using Models;
using System.Collections.Generic;
using APSIM.Core;

namespace UnitTests.Core
{
    internal class MockLocator : IStructure
    {
        public Dictionary<string, VariableComposite> Values = new();

        public MockLocator()
        {
        }

        public void ClearEntry(string path)
        {
            throw new NotImplementedException();
        }

        public void ClearLocator()
        {
            throw new NotImplementedException();
        }

        public T Find<T>(string name = null, INodeModel relativeTo = null)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<T> FindAll<T>(string name = null, INodeModel relativeTo = null)
        {
            throw new NotImplementedException();
        }

        public object Get(string namePath, LocatorFlags flags = LocatorFlags.None, INodeModel relativeTo = null)
        {
            if (Values.ContainsKey(namePath))
                return Values[namePath].Value;
            return null;
        }

        public VariableComposite GetObject(string namePath, LocatorFlags flags, INodeModel relativeTo = null)
        {
            if (Values.ContainsKey(namePath))
                return Values[namePath];
            return null;
        }

        public void Set(string namePath, object value, INodeModel relativeTo = null)
        {
            throw new NotImplementedException();
        }
    }
}