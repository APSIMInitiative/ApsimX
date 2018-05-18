using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem;

namespace UserInterface.Intellisense
{
    /// <summary>
    /// Item for 'override' completion.
    /// </summary>
    public class OverrideCompletionData : EntityCompletionData
    {
        /// <summary>
        /// The offset in the document at which the declaration of this member begins.
        /// </summary>
        private readonly int declarationBegin;

        /// <summary>
        /// Context in the document at the caret.
        /// </summary>
        private readonly CSharpTypeResolveContext contextAtCaret;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="declarationBegin">The offset in the document at which the declaration of this member begins.</param>
        /// <param name="m">Member.</param>
        /// <param name="contextAtCaret">Context in the document at the caret.</param>
        public OverrideCompletionData(int declarationBegin, IMember m, CSharpTypeResolveContext contextAtCaret) : base(m)
        {
            this.declarationBegin = declarationBegin;
            this.contextAtCaret = contextAtCaret;
            var ambience = new CSharpAmbience
            {
                ConversionFlags =
                    ConversionFlags.ShowTypeParameterList | ConversionFlags.ShowParameterList |
                    ConversionFlags.ShowParameterNames
            };
            this.CompletionText = ambience.ConvertSymbol(m);
        }
    }
}
