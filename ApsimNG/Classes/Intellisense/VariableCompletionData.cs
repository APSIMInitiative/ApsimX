using System;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.Completion;
using ICSharpCode.NRefactory.TypeSystem;

namespace UserInterface.Intellisense
{
    internal class VariableCompletionData : CompletionData, IVariableCompletionData
    {
        public VariableCompletionData(IVariable variable)
        {
            if (variable == null) throw new ArgumentNullException("variable");
            Variable = variable;

            IAmbience ambience = new CSharpAmbience();
            DisplayText = variable.Name;
            Description = ambience.ConvertVariable(variable);
            CompletionText = Variable.Name;
            this.Image = ICSharpCode.AvalonEdit.CodeCompletion.CompletionImage.Field.BaseImage;
        }

        public IVariable Variable { get; private set; }
    }
}
