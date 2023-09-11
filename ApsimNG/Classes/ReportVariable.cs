using System.Collections.Generic;
using System.Linq;

namespace ApsimNG.Classes
{
    /// <summary>
    /// Class used to store commonly used report model variables.
    /// </summary>
    public class ReportVariable
    {
        /// <summary> Name of report variable.</summary>
        public string Description { get; set; }
        /// <summary> Code to be used as reporting variable.</summary>
        public string Code { get; set; }

        public List<string> ModelNames
        {
            get
            {
                return Code.Split(new char[2] { '[', ']' }).ToList<string>();
            }
        }

        public ReportVariable() { }

        public ReportVariable(string variableName, string variableCode)
        {
            Description = variableName;
            Code = variableCode;
        }


    }
}
