using APSIM.Shared.Utilities;

namespace ApsimNG.Classes
{
    /// <summary>
    /// Class used to store commonly used report model variables.
    /// </summary>
    public class ReportVariable
    {
        // Value of model name within Code field.
        private string modelName;

        /// <summary> Name of report variable.</summary>
        public string Description { get; set; }

        /// <summary> Node the code references.</summary>
        public string Node { get; set; }
        /// <summary> Code to be used as reporting variable.</summary>
        public string Code { get; set; }

        /// <summary> The type that reporting variable returns eg. int, double, string, vector a.k.a '[]'. </summary>
        public string Type { get; set; }

        /// <summary> The units e.g. kg/m2, mm etc</summary>
        public string Units { get; set; }

        /// <summary> Model name from within Code property.</summary>
        public string ModelName
        {
            get
            {
                string codeString = Code;
                modelName = StringUtilities.SplitOffBracketedValue(ref codeString, '[', ']');
                return modelName;
            }
        }

        public ReportVariable() { }

        public ReportVariable(string variableName, string variableNode, string variableCode, string variableType, string variableUnits)
        {
            Description = variableName;
            Node = variableNode;
            Code = variableCode;
            Type = variableType;
            Units = variableUnits;
        }


    }
}
