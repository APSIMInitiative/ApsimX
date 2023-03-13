
using APSIM.Shared.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Recommendations;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserInterface.EventArguments;
using UserInterface.Intellisense.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using System.Reflection;

namespace UserInterface.Intellisense
{
    /// <summary>
    /// Code completion/intellisense service.
    /// </summary>
    internal class CodeCompletionService
    {
        /// <summary>
        /// Formatting options for parameter symbols' metadata.
        /// </summary>
        private static readonly SymbolDisplayParameterOptions parameterOptions = SymbolDisplayParameterOptions.IncludeDefaultValue
                                                                               | SymbolDisplayParameterOptions.IncludeName
                                                                               | SymbolDisplayParameterOptions.IncludeParamsRefOut
                                                                               | SymbolDisplayParameterOptions.IncludeType;

        /// <summary>
        /// Formatting options for generic type parameter symbols' metadata.
        /// </summary>
        private static readonly SymbolDisplayGenericsOptions genericOptions = SymbolDisplayGenericsOptions.IncludeTypeConstraints
                                                                            | SymbolDisplayGenericsOptions.IncludeTypeParameters
                                                                            | SymbolDisplayGenericsOptions.IncludeVariance;

        /// <summary>
        /// Formatting options for member symbols' metadata.
        /// </summary>
        private static readonly SymbolDisplayMemberOptions memberOptions = SymbolDisplayMemberOptions.IncludeContainingType
                                                                         | SymbolDisplayMemberOptions.IncludeConstantValue
                                                                         | SymbolDisplayMemberOptions.IncludeModifiers
                                                                         | SymbolDisplayMemberOptions.IncludeParameters
                                                                         | SymbolDisplayMemberOptions.IncludeRef
                                                                         | SymbolDisplayMemberOptions.IncludeType;

        /// <summary>
        /// Formatting options for local symbols' metadata.
        /// </summary>
        private static readonly SymbolDisplayLocalOptions localOptions = SymbolDisplayLocalOptions.IncludeConstantValue
                                                                       | SymbolDisplayLocalOptions.IncludeRef
                                                                       | SymbolDisplayLocalOptions.IncludeType;

        /// <summary>
        /// Formatting metadata for symbol "kind" metadata.
        /// </summary>
        private static readonly SymbolDisplayKindOptions kindOptions = SymbolDisplayKindOptions.IncludeNamespaceKeyword
                                                                     | SymbolDisplayKindOptions.IncludeTypeKeyword;

        /// <summary>
        /// Miscellaneous formatting options for symbol metadata.
        /// </summary>
        private static readonly SymbolDisplayMiscellaneousOptions miscellaneousOptions = SymbolDisplayMiscellaneousOptions.UseSpecialTypes
                                                                                       | SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers
                                                                                       | SymbolDisplayMiscellaneousOptions.UseAsterisksInMultiDimensionalArrays
                                                                                       | SymbolDisplayMiscellaneousOptions.AllowDefaultLiteral;

        /// <summary>
        /// Formatting options for metadata displayed in the "details" area of the GUI.
        /// </summary>
        private static readonly SymbolDisplayFormat format = new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
                                                                                     genericsOptions: genericOptions,
                                                                                     memberOptions: memberOptions,
                                                                                     delegateStyle: SymbolDisplayDelegateStyle.NameAndSignature,
                                                                                     parameterOptions: parameterOptions,
                                                                                     propertyStyle: SymbolDisplayPropertyStyle.ShowReadWriteDescriptor,
                                                                                     localOptions: localOptions,
                                                                                     kindOptions: kindOptions,
                                                                                     miscellaneousOptions: miscellaneousOptions);

        private MefHostServices host;
        private AdhocWorkspace workspace;
        private ProjectInfo projectInfo;
        private Project project;
        private Document document;
        private static CodeCompletionService instance;

        public static CodeCompletionService Instance
        {
            get
            {
                Init();
                return instance;
            }
        }
        private static object initLock = new object();

        public static void Init()
        {
            lock (initLock)
            {
                if (instance == null)
                    instance = new CodeCompletionService();
            }
        }

        static CodeCompletionService() => Init();

        private CodeCompletionService()
        {
            host = MefHostServices.Create(GetManagerAssemblies());
            workspace = new AdhocWorkspace(host);
            projectInfo = ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Create(), "Manager Script", "ManagerScript", LanguageNames.CSharp);
            project = workspace.AddProject(projectInfo);
            project = project.AddMetadataReferences(GetReferences());
        }

        public async Task<IEnumerable<NeedContextItemsArgs.ContextItem>> GetCompletionItemsAsync(string code, int offset)
        {
            UpdateDocument(code);

            Document[] documents = new Document[1] { document };
            string wordToComplete = GetPartialWord(code, offset);
            var completions = new List<NeedContextItemsArgs.ContextItem>();

            foreach (Document document in documents)
            {
                SourceText source = await document.GetTextAsync();
                CompletionService service = CompletionService.GetService(document);
                CompletionList completionList = await service.GetCompletionsAsync(document, offset);

                if (completionList != null)
                {
                    // get recommended symbols to match them up later with SymbolCompletionProvider
                    SemanticModel semanticModel = await document.GetSemanticModelAsync();
                    ISymbol[] recommendedSymbols = (await Recommender.GetRecommendedSymbolsAtPositionAsync(document, offset)).ToArray();

                    bool isSuggestionMode = completionList.SuggestionModeItem != null;
                    foreach (CompletionItem item in completionList.ItemsList)
                    {
                        string completionText = item.DisplayText;
                        bool preselect = item.Rules.MatchPriority == MatchPriority.Preselect;
                        if (completionText.IsValidCompletionFor(wordToComplete))
                        {
                            var symbols = await item.GetCompletionSymbolsAsync(recommendedSymbols, document);
                            if (symbols.Any())
                            {
                                foreach (ISymbol symbol in symbols)
                                {
                                    if (item.UseDisplayTextAsCompletionText())
                                    {
                                        completionText = item.DisplayText;
                                    }
                                    else if (item.TryGetInsertionText(out var insertionText))
                                    {
                                        completionText = insertionText;
                                    }
                                    else
                                    {
                                        completionText = symbol.Name;
                                    }

                                    if (symbol != null)
                                    {
                                        //if (request.WantSnippet)
                                        //{
                                        //    foreach (var completion in MakeSnippetedResponses(request, symbol, completionText, preselect, isSuggestionMode))
                                        //    {
                                        //        completions.Add(completion);
                                        //    }
                                        //}
                                        //else
                                        {
                                            //completions.Add(MakeAutoCompleteResponse(request, symbol, completionText, preselect, isSuggestionMode));
                                            completions.Add(new NeedContextItemsArgs.ContextItem()
                                            {
                                                Name = symbol.Name,
                                                Descr = symbol.ToDisplayString(format),
                                                IsMethod = symbol.Kind == SymbolKind.Method,
                                                IsProperty = symbol.Kind == SymbolKind.Property,
                                                IsEvent = symbol.Kind == SymbolKind.Event
                                            });

                                            if (symbol is IPropertySymbol property)
                                                completions.Last().IsWriteable = !property.IsReadOnly;
                                        }
                                    }
                                }

                                // if we had any symbols from the completion, we can continue, otherwise it means
                                // the completion didn't have an associated symbol so we'll add it manually
                                continue;
                            }

                            // for other completions, i.e. keywords, create a simple AutoCompleteResponse
                            // we'll just assume that the completion text is the same
                            // as the display text.
                            //var response = new AutoCompleteResponse()
                            //{
                            //    CompletionText = item.DisplayText,
                            //    DisplayText = item.DisplayText,
                            //    Snippet = item.DisplayText,
                            //    Kind = request.WantKind ? item.Tags.First() : null,
                            //    IsSuggestionMode = isSuggestionMode,
                            //    Preselect = preselect
                            //};

                            completions.Add(new NeedContextItemsArgs.ContextItem()
                            {
                                Name = item.DisplayText,
                                Descr = item.Tags.First(),
                            });
                        }
                    }
                }
            }

            return completions
                .OrderByDescending(c => c.Name.IsValidCompletionStartsWithExactCase(wordToComplete))
                .ThenByDescending(c => c.Name.IsValidCompletionStartsWithIgnoreCase(wordToComplete))
                .ThenByDescending(c => c.Name.IsCamelCaseMatch(wordToComplete))
                .ThenByDescending(c => c.Name.IsSubsequenceMatch(wordToComplete))
                .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase);
        }

        private void UpdateDocument(string code)
        {
            if (document == null)
                document = project.AddDocument("Script", SourceText.From(code));
            else
            {
                document = document.WithText(SourceText.From(code));
                if (!workspace.TryApplyChanges(workspace.CurrentSolution))
                    throw new Exception("Unable to update document for code completion service");
            }
        }

        private static string GetPartialWord(string code, int offset)
        {
            if (string.IsNullOrEmpty(code) || offset == 0)
                return string.Empty;

            int index = offset;
            while (index >= 1)
            {
                var ch = code[index - 1];
                if (ch != '_' && !char.IsLetterOrDigit(ch))
                {
                    break;
                }

                index--;
            }

            return code.Substring(index, offset - index);
        }

        private MetadataReference[] GetReferences()
        {
            return GetManagerAssemblies().Select(a => MetadataReferences.CreateFromFile(a.Location)).ToArray();
        }

        private Assembly[] GetManagerAssemblies()
        {
            var assemblies = new List<Assembly>()
            {
                typeof(object).Assembly, // mscorlib
                typeof(Uri).Assembly, // System.dll
                typeof(Enumerable).Assembly, // System.Core.dll
                typeof(System.Xml.XmlDocument).Assembly, // System.Xml.dll
                typeof(Models.Core.IModel).Assembly, // Models.exe
                typeof(APSIM.Shared.Utilities.StringUtilities).Assembly, // APSIM.Shared.dll
                typeof(MathNet.Numerics.Combinatorics).Assembly, // MathNet.Numerics
                typeof(System.Data.DataTable).Assembly, // System.Data
                Assembly.Load("Microsoft.CodeAnalysis"),
                Assembly.Load("Microsoft.CodeAnalysis.Features"),
                Assembly.Load("Microsoft.CodeAnalysis.Workspaces"),
                Assembly.Load("Microsoft.CodeAnalysis.CSharp"),
                Assembly.Load("Microsoft.CodeAnalysis.CSharp.Features"),
                Assembly.Load("Microsoft.CodeAnalysis.CSharp.Workspaces")
            };
            return assemblies.Distinct().ToArray();
        }
    }
}
