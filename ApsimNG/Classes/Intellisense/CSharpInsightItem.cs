using System.IO;
using System.Text;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.Xml;

namespace UserInterface.Intellisense
{
    sealed class ParameterHighlightingOutputFormatter : TextWriterTokenWriter
    {
        private StringBuilder b;

        private int highlightedParameterIndex;

        private int parameterIndex;

        private int parameterStartOffset;

        private int parameterLength;

        public ParameterHighlightingOutputFormatter(StringBuilder b, int highlightedParameterIndex)
            : base(new StringWriter(b))
        {
            this.b = b;
            this.highlightedParameterIndex = highlightedParameterIndex;
        }

        public override void StartNode(AstNode node)
        {
            if (parameterIndex == highlightedParameterIndex && node is ParameterDeclaration)
            {
                parameterStartOffset = b.Length;
            }
            base.StartNode(node);
        }

        public override void EndNode(AstNode node)
        {
            base.EndNode(node);
            if (node is ParameterDeclaration)
            {
                if (parameterIndex == highlightedParameterIndex)
                    parameterLength = b.Length - parameterStartOffset;
                parameterIndex++;
            }
        }
    }

    public sealed class CSharpInsightItem
    {
        private int highlightedParameterIndex = -1;

        private string documentation;

        public CSharpInsightItem(IParameterizedMember method)
        {
            this.Method = method;
        }

        public IParameterizedMember Method { get; private set; }

        public object Header
        {
            get
            {
                return null;
            }
        }

        public void HighlightParameter(int parameterIndex)
        {
            if (highlightedParameterIndex == parameterIndex)
                return;
            this.highlightedParameterIndex = parameterIndex;
            if (Header != null)
                GenerateHeader();
        }

        public object Content
        {
            get { return Documentation; }
        }
        
        public string Documentation
        {
            get
            {
                if (documentation == null)
                {
                    if (Method.Documentation == null)
                        documentation = "";
                    else
                        documentation = EntityCompletionData.XmlDocumentationToText(Method.Documentation);
                }
                return documentation;
            }
        }

        /// <summary>
        /// Not sure what this method is supposed to do, but it seems to be tied to AvalonEdit in some way.
        /// There are one or two references to this method in varioud places, but I've never observed it
        /// it actually being called. I'll leave it in for now, but it should probably be removed at some 
        /// point - DH May 2018.
        /// </summary>
        private void GenerateHeader()
        {
            throw new System.NotImplementedException();
            /*
            CSharpAmbience ambience = new CSharpAmbience();
            ambience.ConversionFlags = ConversionFlags.StandardConversionFlags;
            var stringBuilder = new StringBuilder();
            TokenWriter formatter = new ParameterHighlightingOutputFormatter(stringBuilder, highlightedParameterIndex);
            ambience.ConvertSymbol(Method, formatter, FormattingOptionsFactory.CreateSharpDevelop());
            var documentation = XmlDocumentationElement.Get(Method);
            ambience.ConversionFlags = ConversionFlags.ShowTypeParameterList;

            var inlineBuilder = new HighlightedInlineBuilder(stringBuilder.ToString());
            Header.Inlines.Clear();
            Header.Inlines.AddRange(inlineBuilder.CreateRuns());
            */
        }
    }
}
