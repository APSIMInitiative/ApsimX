using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Core;
using Models.Core;

namespace Models.Functions
{
    /// <summary>
    /// Bounds the child function between lower and upper bounds
    /// </summary>
    [Serializable]
    [Description("Bounds the child function between lower and upper bounds")]
    public class BoundFunction : Model, IFunction, IStructureDependency
    {
        /// <summary>Structure instance supplied by APSIM.core.</summary>
        [field: NonSerialized]
        public IStructure Structure { private get; set; }

        /// <summary>The child functions</summary>
        private IEnumerable<IFunction> ChildFunctions;

        [Link(Type = LinkType.Child, ByName = true)]
        IFunction Lower = null;

        [Link(Type = LinkType.Child, ByName = true)]
        IFunction Upper = null;

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            if (ChildFunctions == null)
                ChildFunctions = Structure.FindChildren<IFunction>().ToList();
            foreach (IFunction child in ChildFunctions)
                if (child != Lower && child != Upper)
                    return Math.Max(Math.Min(Upper.Value(arrayIndex), child.Value(arrayIndex)), Lower.Value(arrayIndex));
            throw new Exception("Cannot find function value to apply in bound");
        }
    }
}
