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

        public string FileName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public string Name => throw new NotImplementedException();

        public string FullNameAndPath => throw new NotImplementedException();

        public void AddChild(INodeModel childModel)
        {
            throw new NotImplementedException();
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

        public T FindChild<T>(string name = null, bool recurse = false, INodeModel relativeTo = null)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<T> FindChildren<T>(string name = null, bool recurse = false, INodeModel relativeTo = null)
        {
            throw new NotImplementedException();
        }

        public T FindParent<T>(string name = null, bool recurse = false, INodeModel relativeTo = null)
        {
            throw new NotImplementedException();
        }

        public T FindSibling<T>(string name = null, INodeModel relativeTo = null)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<T> FindSiblings<T>(string name = null, INodeModel relativeTo = null)
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

        public void InsertChild(int index, INodeModel childModel)
        {
            throw new NotImplementedException();
        }

        public void RemoveChild(INodeModel childModel)
        {
            throw new NotImplementedException();
        }

        public void Rename(string name)
        {
            throw new NotImplementedException();
        }

        public void ReplaceChild(INodeModel oldModel, INodeModel newModel)
        {
            throw new NotImplementedException();
        }

        public void Set(string namePath, object value, INodeModel relativeTo = null)
        {
            throw new NotImplementedException();
        }
    }
}