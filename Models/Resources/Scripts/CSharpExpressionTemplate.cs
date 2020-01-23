using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using APSIM.Shared.Utilities;
using Models.Functions;
using Models;
using Models.Core;

namespace Models
{
    [Serializable]
	public class Script : Model, IFunction
	{
        [Link] Clock Clock;

        /// <summary>Gets the value of the function.</summary>
        public double Value(int arrayIndex = -1)
        {
            return 123456;
        }
    }
}