namespace ApsimNG.Classes
{
    /// <summary>
    /// Class used to store commonly used report model variables.
    /// </summary>
    public class ReportVariable
    {
        /// <summary> Name of report variable.</summary>
        public string VariableName { get; set; }
        /// <summary> Code to be used as reporting variable.</summary>
        public string VariableCode { get; set; }

        public ReportVariable() { }

        public ReportVariable(string variableName, string variableCode)
        {
            VariableName = variableName;
            VariableCode = variableCode;
        }
    }
}
