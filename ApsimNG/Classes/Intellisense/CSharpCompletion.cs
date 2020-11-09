using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.Completion;
using ICSharpCode.NRefactory.Editor;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.Documentation;
using System.IO;

namespace UserInterface.Intellisense
{
    /// <summary>
    /// Generates completion options for a document.
    /// </summary>
    public class CSharpCompletion
    {
        /// <summary>
        /// The content we are examining for completion options.
        /// </summary>
        private IProjectContent projectContent;

        /// <summary>
        /// List of assemblies which in which we will search for completion options.
        /// </summary>
        private static IUnresolvedAssembly[] unresolvedAssemblies;

        /// <summary>
        /// Controls access to the initialisation of this class.
        /// </summary>
        private static object initMutex = new object();

        /// <summary>
        /// Default construtor. If null is passed in, the default assemblies list will be used.
        /// </summary>
        /// <param name="assemblies"></param>
        public CSharpCompletion(IReadOnlyList<Assembly> assemblies = null)
        {
            Init();
            projectContent = new CSharpProjectContent();
            if (assemblies != null)
            {
                unresolvedAssemblies = GetAssemblies(assemblies.ToList());
            }
            
            projectContent = projectContent.AddAssemblyReferences((IEnumerable<IUnresolvedAssembly>)unresolvedAssemblies);
        }

        /// <summary>
        /// Initialises the completion object by loading required assemblies.
        /// </summary>
        public static void Init()
        {
            lock (initMutex)
            {
                if (unresolvedAssemblies == null)
                    unresolvedAssemblies = GetAssemblies(); // This takes a long time
            }

        }

        /// <summary>
        /// Alternative constructor. Currently unused, but could come in handy.
        /// </summary>
        /// <param name="scriptProvider"></param>
        /// <param name="assemblies"></param>
        public CSharpCompletion(ICSharpScriptProvider scriptProvider, IReadOnlyList<Assembly> assemblies = null) : this(assemblies)
        {
            ScriptProvider = scriptProvider;
        }

        /// <summary>
        /// Default script provider.
        /// </summary>
        public ICSharpScriptProvider ScriptProvider { get; set; }

        /// <summary>
        /// Gets the completion options at a given offset in a document.
        /// </summary>
        /// <param name="document">Document containing the source code.</param>
        /// <param name="offset">Offset of the cursor in the document.</param>
        /// <returns>The code completion result.</returns>
        public CodeCompletionResult GetCompletions(IDocument document, int offset)
        {
            return GetCompletions(document, offset, false);
        }

        /// <summary>
        /// Gets the completion options at a given offset in a document.
        /// </summary>
        /// <param name="document">Document containing the source code.</param>
        /// <param name="offset">Offset of the cursor in the document.</param>
        /// <param name="controlSpace">True if the user pressed control-space, false otherwise.</param>
        /// <returns>The code completion result.</returns>
        public CodeCompletionResult GetCompletions(IDocument document, int offset, bool controlSpace)
        {
            //get the using statements from the script provider
            string usings = null;
            string variables = null;
            string @namespace = null;
            if (ScriptProvider != null)
            {
                usings = ScriptProvider.GetUsing();
                variables = ScriptProvider.GetVars();
                @namespace = ScriptProvider.GetNamespace();
            }
            return GetCompletions(document, offset, controlSpace, usings, variables, @namespace);
        }

        /// <summary>
        /// Gets the completion options at a given offset in a document.
        /// </summary>
        /// <param name="document">Document containing the source code.</param>
        /// <param name="offset">Offset of the cursor in the document.</param>
        /// <param name="controlSpace">True if the user pressed control-space, false otherwise.</param>
        /// <param name="usings">Using statements.</param>
        /// <param name="variables">Variables.</param>
        /// <param name="namespace">Namespace (of the script/document?).</param>
        /// <returns>The code completion result.</returns>
        public CodeCompletionResult GetCompletions(IDocument document, int offset, bool controlSpace, string usings, string variables, string @namespace)
        {
            var result = new CodeCompletionResult();

            if (String.IsNullOrEmpty(document.FileName))
                return result;

            var completionContext = new CSharpCompletionContext(document, offset, projectContent, usings, variables, @namespace);

            var completionFactory = new CSharpCompletionDataFactory(completionContext.TypeResolveContextAtCaret, completionContext);
            var cce = new CSharpCompletionEngine(
                completionContext.Document,
                completionContext.CompletionContextProvider,
                completionFactory,
                completionContext.ProjectContent,
                completionContext.TypeResolveContextAtCaret
                );

            cce.EolMarker = Environment.NewLine;
            cce.FormattingPolicy = FormattingOptionsFactory.CreateSharpDevelop();


            var completionChar = completionContext.Document.GetCharAt(completionContext.Offset - 1);
            int startPos, triggerWordLength;
            IEnumerable<ICSharpCode.NRefactory.Completion.ICompletionData> completionData;
            if (controlSpace)
            {
                if (!cce.TryGetCompletionWord(completionContext.Offset, out startPos, out triggerWordLength))
                {
                    startPos = completionContext.Offset;
                    triggerWordLength = 0;
                }
                completionData = cce.GetCompletionData(startPos, true);
                //this outputs tons of available entities
                //if (triggerWordLength == 0)
                //    completionData = completionData.Concat(cce.GetImportCompletionData(startPos));
            }
            else
            {
                startPos = completionContext.Offset;

                if (char.IsLetterOrDigit(completionChar) || completionChar == '_')
                {
                    if (startPos > 1 && char.IsLetterOrDigit(completionContext.Document.GetCharAt(startPos - 2)))
                        return result;
                    completionData = cce.GetCompletionData(startPos, false);
                    startPos--;
                    triggerWordLength = 1;
                }
                else
                {
                    completionData = cce.GetCompletionData(startPos, false);
                    triggerWordLength = 0;
                }
            }

            result.TriggerWordLength = triggerWordLength;
            result.TriggerWord = completionContext.Document.GetText(completionContext.Offset - triggerWordLength, triggerWordLength);

            //cast to UserInterface.Intellisense completion data and add to results
            foreach (var completion in completionData)
            {
                var cshellCompletionData = completion as CompletionData;
                if (cshellCompletionData != null)
                {
                    cshellCompletionData.TriggerWord = result.TriggerWord;
                    cshellCompletionData.TriggerWordLength = result.TriggerWordLength;
                    result.CompletionData.Add(cshellCompletionData);
                }
            }

            //method completions
            if (!controlSpace)
            {
                // Method Insight
                var pce = new CSharpParameterCompletionEngine(
                    completionContext.Document,
                    completionContext.CompletionContextProvider,
                    completionFactory,
                    completionContext.ProjectContent,
                    completionContext.TypeResolveContextAtCaret
                );

                var parameterDataProvider = pce.GetParameterDataProvider(completionContext.Offset, completionChar);
                result.OverloadProvider = parameterDataProvider as IOverloadProvider;
            }

            return result;
        }

        public CodeCompletionResult GetMethodCompletion(IDocument document, int offset, bool controlSpace)
        {
            var result = new CodeCompletionResult();

            if (String.IsNullOrEmpty(document.FileName))
                return result;

            var completionContext = new CSharpCompletionContext(document, offset, projectContent, null, null, null);

            var completionFactory = new CSharpCompletionDataFactory(completionContext.TypeResolveContextAtCaret, completionContext);
            var cce = new CSharpCompletionEngine(
                completionContext.Document,
                completionContext.CompletionContextProvider,
                completionFactory,
                completionContext.ProjectContent,
                completionContext.TypeResolveContextAtCaret
                );

            cce.EolMarker = Environment.NewLine;
            cce.FormattingPolicy = FormattingOptionsFactory.CreateSharpDevelop();
            var completionChar = completionContext.Document.GetCharAt(completionContext.Offset - 1);
            var pce = new CSharpParameterCompletionEngine(
                    completionContext.Document,
                    completionContext.CompletionContextProvider,
                    completionFactory,
                    completionContext.ProjectContent,
                    completionContext.TypeResolveContextAtCaret
                );

            var parameterDataProvider = pce.GetParameterDataProvider(completionContext.Offset, completionChar);
            result.OverloadProvider = parameterDataProvider as IOverloadProvider;
            return result;
        }

        /// <summary>
        /// Gets the XML documentation associated with a dll file.
        /// </summary>
        /// <param name="dllPath">Path to the binary file.</param>
        /// <returns>XML documentation.</returns>
        private static XmlDocumentationProvider GetXmlDocumentation(string dllPath)
        {
            if (string.IsNullOrEmpty(dllPath))
                return null;

            var xmlFileName = Path.GetFileNameWithoutExtension(dllPath) + ".xml";
            var localPath = Path.Combine(Path.GetDirectoryName(dllPath), xmlFileName);
            if (File.Exists(localPath))
                return new XmlDocumentationProvider(localPath);

            //if it's a .NET framework assembly it's in one of following folders
            var netPath = Path.Combine(@"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0", xmlFileName);
            if (File.Exists(netPath))
                return new XmlDocumentationProvider(netPath);

            return null;
        }

        /// <summary>
        /// Loads the assemblies needed to generate completion options.
        /// </summary>
        /// <param name="assemblies">List of assemblies. If nothing is passed in, a default list will be used.</param>
        /// <returns>List of assemblies.</returns>
        /// <remarks>This is an expensive operation.</remarks>
        private static IUnresolvedAssembly[] GetAssemblies(List<Assembly> assemblies = null)
        {
            // List of assemblies frequently used in manager scripts. 
            // These assemblies get used by the CSharpCompletion object to look for intellisense options.
            // Would be better to dynamically generate this list based on the user's script. The disadvantage of doing it that way
            // is that loading these assemblies into the CSharpCompletion object is quite slow. 
            if (assemblies == null)
                assemblies = new List<Assembly>
                {
                    typeof(object).Assembly, // mscorlib
		            typeof(Uri).Assembly, // System.dll
		            typeof(System.Linq.Enumerable).Assembly, // System.Core.dll
                    typeof(System.Xml.XmlDocument).Assembly, // System.Xml.dll
                    typeof(System.Drawing.Bitmap).Assembly, // System.Drawing.dll
		            typeof(IProjectContent).Assembly,
                    typeof(Models.Core.IModel).Assembly, // Models.exe
                    typeof(APSIM.Shared.Utilities.StringUtilities).Assembly, // APSIM.Shared.dll
                    typeof(MathNet.Numerics.Combinatorics).Assembly, // MathNet.Numerics,
                    typeof(System.Data.DataTable).Assembly, // System.Data.dll,
                    typeof(System.Drawing.Color).Assembly // System.Data.dll,
                };
            assemblies = assemblies.Where(v => !v.IsDynamic).ToList();

            IUnresolvedAssembly[] assemblyList = new IUnresolvedAssembly[assemblies.Count];
            for (int i = 0; i < assemblies.Count; i++)
            {
                var loader = new CecilLoader();
                loader.DocumentationProvider = GetXmlDocumentation(assemblies[i].Location);
                assemblyList[i] = loader.LoadAssemblyFile(assemblies[i].Location);
            }
            return assemblyList;
        }
    }
}
