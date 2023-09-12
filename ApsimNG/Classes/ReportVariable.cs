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
        /// <summary> Code to be used as reporting variable.</summary>
        public string Code { get; set; }

        // Model name from within Code property.
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

        public ReportVariable(string variableName, string variableCode)
        {
            Description = variableName;
            Code = variableCode;
        }


    }
}
