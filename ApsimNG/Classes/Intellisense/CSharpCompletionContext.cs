using System;
using System.IO;
using System.Text.RegularExpressions;
using ICSharpCode.NRefactory.Editor;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.CSharp.Completion;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using ICSharpCode.NRefactory.CSharp;

namespace UserInterface.Intellisense
{
    /// <summary>
    /// If the code is just a script it it will contain no namespace, class or method structure and so the code completion will not work properly.
    /// This class surounds the code with the appropriate structure. 
    /// </summary>
    public sealed class CSharpCompletionContext
    {
        /// <summary>
        /// Original document containing source code.
        /// </summary>
        public readonly IDocument OriginalDocument;

        /// <summary>
        /// Original offset in the document.
        /// </summary>
        public readonly int OriginalOffset;

        /// <summary>
        /// Original using statements in the document.
        /// </summary>
        public readonly string OriginalUsings;

        /// <summary>
        /// Original variables declared in the document.
        /// </summary>
        public readonly string OriginalVariables;

        /// <summary>
        /// Original namespace of the document.
        /// </summary>
        public readonly string OriginalNamespace;

        /// <summary>
        /// Offset in the document.
        /// </summary>
        public readonly int Offset;

        /// <summary>
        /// Parsed/modified document containing the source code surrounded by the appropriate structures.
        /// If the was already a fully formatted file/script, this will be the same as <see cref="OriginalDocument"/>.
        /// </summary>
        public readonly IDocument Document;

        /// <summary>
        /// Compiled source code.
        /// </summary>
        public readonly ICompilation Compilation;

        /// <summary>
        /// Represents an assembly consisting of source code (parsed files).
        /// </summary>
        public readonly IProjectContent ProjectContent;

        /// <summary>
        /// Contains the main resolver logic.
        /// </summary>
        public readonly CSharpResolver Resolver;

        /// <summary>
        /// Context of the document at the caret location/offset.
        /// </summary>
        public readonly CSharpTypeResolveContext TypeResolveContextAtCaret;

        /// <summary>
        /// Context of the document at the caret location/offset.
        /// </summary>
        public readonly ICompletionContextProvider CompletionContextProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="CSharpCompletionContext"/> class.
        /// </summary>
        /// <param name="document">The document, make sure the FileName property is set on the document.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="projectContent">Content of the project.</param>
        /// <param name="usings">The usings.</param>
        /// <param name="variables">The variables</param>
        /// <param name="namespace">The namespace.</param>
        public CSharpCompletionContext(IDocument document, int offset, IProjectContent projectContent, string usings = null, string variables = null, string @namespace = null)
        {
            OriginalDocument = document;
            OriginalOffset = offset;
            OriginalUsings = usings;
            OriginalVariables = variables;
            OriginalNamespace = @namespace;

            //if the document is a c# script we have to soround the document with some code.
            Document = PrepareCompletionDocument(document, ref offset, usings, variables, @namespace);
            Offset = offset;

            var syntaxTree = new CSharpParser().Parse(Document, Document.FileName);
            syntaxTree.Freeze();
            var unresolvedFile = syntaxTree.ToTypeSystem();

            ProjectContent = projectContent.AddOrUpdateFiles(unresolvedFile);
            //note: it's important that the project content is used that is returned after adding the unresolved file
            Compilation = ProjectContent.CreateCompilation();

            var location = Document.GetLocation(Offset);
            Resolver = unresolvedFile.GetResolver(Compilation, location);
            TypeResolveContextAtCaret = unresolvedFile.GetTypeResolveContext(Compilation, location);
            CompletionContextProvider = new DefaultCompletionContextProvider(Document, unresolvedFile);
        }

        /// <summary>
        /// Matches any character that is not alphanumeric or an underscore.
        /// </summary>
        private static Regex replaceRegex = new Regex("[^a-zA-Z0-9_]");

        /// <summary>
        /// Prepares a document by surrounding code with the appropriate structures, if necessary.
        /// </summary>
        /// <param name="document">Document to prepare.</param>
        /// <param name="offset">Offset of the caret in the document</param>
        /// <param name="usings">Using statements.</param>
        /// <param name="variables">Variables used in the code.</param>
        /// <param name="namespace">Namespace of the script's code.</param>
        /// <returns></returns>
        private static IDocument PrepareCompletionDocument(IDocument document, ref int offset, string usings = null, string variables = null, string @namespace = null)
        {
            if (String.IsNullOrEmpty(document.FileName))
                return document;

            //if the code is just a script it it will contain no namestpace, class and method structure and so the code completion will not work properly
            // for it to work we have to suround the code with the appropriate code structure
            //we only process the file if its a .csx file
            var fileExtension = Path.GetExtension(document.FileName);
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(document.FileName);
            if (String.IsNullOrEmpty(fileExtension) || String.IsNullOrEmpty(fileNameWithoutExtension))
                return document;

            if (fileExtension.ToLower() == ".csx")
            {
                string classname = replaceRegex.Replace(fileNameWithoutExtension, "");
                classname = classname.TrimStart('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');

                string header = String.Empty;
                header += (usings ?? "") + Environment.NewLine;
                if (@namespace != null)
                {
                    header += "namespace " + @namespace + " {" + Environment.NewLine;
                }
                header += "public static class " + classname + " {" + Environment.NewLine;
                header += "public static void Main() {" + Environment.NewLine;
                header += (variables ?? "") + Environment.NewLine;

                string footer = "}" + Environment.NewLine + "}" + Environment.NewLine;
                if (@namespace != null)
                {
                    footer += "}" + Environment.NewLine;
                }

                string code = header + document.Text + Environment.NewLine + footer;

                offset += header.Length;

                return new ReadOnlyDocument(new StringTextSource(code), document.FileName);
            }
            return document;
        }
    }
}
