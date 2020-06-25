using System;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.Completion;
using ICSharpCode.NRefactory.TypeSystem;

namespace UserInterface.Intellisense
{
    /// <summary>
    /// Represents completion data for a variable.
    /// </summary>
    public class VariableCompletionData : CompletionData, IVariableCompletionData
    {
        /// <summary>
        /// Public constructor.
        /// </summary>
        /// <param name="variable"></param>
        public VariableCompletionData(IVariable variable)
        {
            if (variable == null) throw new ArgumentNullException("variable");
            Variable = variable;

            IAmbience ambience = new CSharpAmbience();
            DisplayText = variable.Name;
            Description = ambience.ConvertSymbol(variable);
            CompletionText = Variable.Name;
            this.Image = CompletionImage.Field.BaseImage;
            Units = "";
            ReturnType = variable.Type.Name;
        }

        /// <summary>
        /// The variable.
        /// </summary>
        public IVariable Variable { get; private set; }
    }
}
