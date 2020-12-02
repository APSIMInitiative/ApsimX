namespace UserInterface.Presenters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.IO;
    using Views;
    using Interfaces;
    using Intellisense;
    using EventArguments;
    using Models.Core;
    using System.Globalization;
    using System.Drawing;
    using System.Reflection;
    using System.Text;
    using System.Xml;
    using APSIM.Shared.Utilities;
    using Models.Storage;
    using System.Threading;

#if NETCOREAPP
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Completion;
    using Microsoft.CodeAnalysis.Host.Mef;
    using Microsoft.CodeAnalysis.Text;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis.Recommendations;
#endif

#if NETFRAMEWORK
    using ICSharpCode.NRefactory.Editor;
    using ICSharpCode.NRefactory.CSharp;
    using ICSharpCode.NRefactory.TypeSystem;
#endif

    /// <summary>
    /// Responsible for handling intellisense operations.
    /// In order to use this class, <see cref="LoseFocus"/>, <see cref="ItemSelected"/>, and <see cref="ContextItemsNeeded"/> must be set.
    /// Instantiating this class can be somewhat expensive (~1-2 seconds).
    /// </summary>
    public class IntellisensePresenter
    {
        /// <summary>
        /// The view responsible for displaying the intellisense options on the screen.
        /// </summary>
        private IntellisenseView view;

        /// <summary>
        /// Small popup window which displays completion options (arguments) for a method.
        /// </summary>
        private IMethodCompletionView methodCompletionView;

        /// <summary>
        /// Fired when the we need to generate intellisense suggestions.
        /// </summary>
        private event EventHandler<NeedContextItemsArgs> OnContextItemsNeeded;

        /// <summary>
        /// The partially-finished word for which the user wants completion options. May be empty string.
        /// </summary>
        private string triggerWord = string.Empty;

        /// <summary>
        /// Stores the last used coordinates for the intellisense popup.
        /// </summary>
        private Point recentLocation;

        /// <summary>
        /// Fired when an item is selected in the intellisense window.
        /// </summary>
        private event EventHandler<IntellisenseItemSelectedArgs> OnItemSelected;
#if NETFRAMEWORK
        /// <summary>
        /// Responsible for generating the completion options.
        /// </summary>
        private CSharpCompletion completion = new CSharpCompletion();
        
        /// <summary>
        /// List of intellisense options.
        /// Probably doesn't need to be a class field, but I have plans to use it in the future. - DH May 2018
        /// </summary>
        private CodeCompletionResult completionResult;
#else
        private MefHostServices host;
        private AdhocWorkspace workspace;
        private ProjectInfo projectInfo;
        private Project project;
        private Document doc;
        private CompletionService completion;
#endif

        /// <summary>
        /// Speeds up initialisation of all future intellisense objects.
        /// Only needs to be called once, when the application starts.
        /// </summary>
        public static void Init()
        {
#if NETFRAMEWORK
            Thread initThread = new Thread(CSharpCompletion.Init);
            initThread.Start();
#endif
        }

#if NETCOREAPP
        /// <summary>
        /// Default constructor.
        /// </summary>
        public IntellisensePresenter()
        {
            host = MefHostServices.Create(GetManagerAssemblies());
            workspace = new AdhocWorkspace(host);
            projectInfo = ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Create(), "Manager Script", "ManagerScript", LanguageNames.CSharp);
            project = workspace.AddProject(projectInfo);
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
                typeof(System.Linq.Enumerable).Assembly, // System.Core.dll
                typeof(System.Xml.XmlDocument).Assembly, // System.Xml.dll
                typeof(System.Drawing.Bitmap).Assembly, // System.Drawing.dll
                typeof(Models.Core.IModel).Assembly, // Models.exe
                typeof(APSIM.Shared.Utilities.StringUtilities).Assembly, // APSIM.Shared.dll
                typeof(MathNet.Numerics.Combinatorics).Assembly, // MathNet.Numerics
                typeof(System.Data.DataTable).Assembly, // System.Data
                Assembly.Load("Microsoft.CodeAnalysis"),
                Assembly.Load("Microsoft.CodeAnalysis.CSharp"),
                Assembly.Load("Microsoft.CodeAnalysis.Features"),
                Assembly.Load("Microsoft.CodeAnalysis.CSharp.Features")
            };
            assemblies.AddRange(MefHostServices.DefaultAssemblies);
            return assemblies.Distinct().ToArray();
        }
#endif
        /// <summary>
        /// Constructor. Requires a reference to the view holding the text editor.
        /// </summary>
        /// <param name="textEditor">Reference to the view holding the text editor. Cannot be null.</param>
        public IntellisensePresenter(ViewBase textEditor)
        {
            if (textEditor == null)
                throw new ArgumentException("textEditor cannot be null.");

            view = new IntellisenseView(textEditor);
            methodCompletionView = new MethodCompletionView(textEditor);

            // The way that the ItemSelected event handler works is a little complicated. If the user has half-typed 
            // a word and needs completion options for it, we can't just insert the selected completion at the caret 
            // - half of the word will be duplicated. Instead, we need to intercept the event, add the trigger word 
            // to the event args, and call the event handler provided to us. The view is then responsible for 
            // inserting the completion option at the appropriate point and removing the half-finished word.
            view.ItemSelected += ContextItemSelected;
        }

        /// <summary>
        /// Fired when the intellisense window loses focus.
        /// </summary>
        public event EventHandler LoseFocus
        {
            add
            {
                view.LoseFocus += value;
            }
            remove
            {
                view.LoseFocus -= value;
            }
        }

        /// <summary>
        /// Fired when an item is selected in the intellisense window.
        /// </summary>
        public event EventHandler<IntellisenseItemSelectedArgs> ItemSelected
        {
            add
            {
                DetachHandlers(ref OnItemSelected);
                OnItemSelected += value;
            }
            remove
            {
                OnItemSelected -= value;
            }
        }

        /// <summary>
        /// Invoked when the editor needs context items (after user presses '.').
        /// </summary>
        public event EventHandler<NeedContextItemsArgs> ContextItemsNeeded
        {
            add
            {
                if (OnContextItemsNeeded == null)
                    OnContextItemsNeeded += value;
                else if (OnContextItemsNeeded != value)
                {
                    foreach (var d in OnContextItemsNeeded.GetInvocationList())
                    {
                        OnContextItemsNeeded -= (d as EventHandler<NeedContextItemsArgs>);
                    }
                    OnContextItemsNeeded += value;
                }
            }
            remove
            {
                OnContextItemsNeeded -= value;
            }
        }

        /// <summary>
        /// Returns true if the intellisense is visible. False otherwise.
        /// </summary>
        public bool Visible { get { return view.Visible; } }

        /// <summary>
        /// Detaches all handlers from an event.
        /// </summary>
        /// <typeparam name="T">Type of the event.</typeparam>
        /// <param name="handler">Event for which handlers should be removed. Maybe handler is not such a good name.</param>
        public void DetachHandlers<T>(ref EventHandler<T> handler)
        {
            if (handler == null)
                return;
            foreach (EventHandler<T> anonymousHandler in handler.GetInvocationList())
            {
                handler -= anonymousHandler;
            }
        }

        /// <summary>
        /// Gets a list of all events which are published by models in
        /// the current simulations tree.
        /// </summary>
        /// <param name="model"></param>
        public List<NeedContextItemsArgs.ContextItem> GetEvents(IModel model)
        {
            var events = new List<NeedContextItemsArgs.ContextItem>();

            IEnumerable<IModel> allModels = model.FindAncestor<Simulations>().FindAllDescendants();
            foreach (var publisher in Events.Publisher.FindAll(allModels))
            {
                string description = NeedContextItemsArgs.GetDescription(publisher.EventInfo);
                Type eventType = publisher.EventInfo.EventHandlerType;
                events.Add(NeedContextItemsArgs.ContextItem.NewEvent(publisher.Name, description, eventType));
            }

            return events;
        }

        /// <summary>
        /// Generates completion options for a report, property presenter, etc.
        /// Essentially this is the completion provider for 'apsim' completion values,
        /// and is not used in a manager script context.
        /// </summary>
        /// <param name="cellContents">Source code.</param>
        /// <param name="model">Completion options are generated in reference to this model.</param>
        /// <param name="methods">Get method completion options?</param>
        /// <param name="properties">Get property completion options?</param>
        /// <param name="publishedEvents">If true, published events will be returned.</param>
        /// <param name="subscribedEvents">If true, subscribed events will be returned.</param>
        /// <param name="offset">Offset of the cursor/caret in the source code.</param>
        /// <param name="controlSpace">True iff this intellisense request was generated by the user pressing control + space.</param>
        /// <returns>True if any completion options are found. False otherwise.</returns>
        public bool GenerateGridCompletions(string cellContents, int offset, IModel model, bool properties, bool methods, bool publishedEvents, bool subscribedEvents, bool controlSpace = false)
        {
            // TODO : Perhaps there should be a separate intellisense class for grid completions?
            string contentsToCursor = cellContents.Substring(0, offset);

            // Remove any potential trailing period.
            contentsToCursor = contentsToCursor.TrimEnd('.');

            // Ignore everything before the most recent comma.
            string objectName = contentsToCursor.Substring(contentsToCursor.LastIndexOf(',') + 1);

            // Set the trigger word for later use.
            triggerWord = controlSpace ? GetTriggerWord(objectName) : string.Empty;

            // Ignore everything before most recent model name in square brackets.
            // I'm assuming that model/node names cannot start with a number.
            string modelNamePattern = @"\[([A-Za-z]+[A-Za-z0-9]*)\]";
            var matches = System.Text.RegularExpressions.Regex.Matches(objectName, modelNamePattern);
            if (matches.Count > 0)
            {
                int modelNameIndex = objectName.LastIndexOf(matches[matches.Count - 1].Value);
                if (modelNameIndex >= 0)
                    objectName = objectName.Substring(modelNameIndex);
            }

            if (double.TryParse(objectName, out _))
                return false;

            List<NeedContextItemsArgs.ContextItem> results = NeedContextItemsArgs.ExamineModelForContextItemsV2(model as Model, objectName, properties, methods, publishedEvents, subscribedEvents);

            view.Populate(results);
            return results.Any();
        }

#if NETFRAMEWORK
        /// <summary>
        /// Generates completion options for a manager script.
        /// </summary>
        /// <param name="code">Source code.</param>
        /// <param name="offset">Offset of the cursor/caret in the source code.</param>
        /// <param name="controlSpace">True iff this intellisense request was generated by the user pressing control + space.</param>
        /// <returns>True if any completion options are found. False otherwise.</returns>
        public bool GenerateScriptCompletions(string code, int offset, bool controlSpace = false)
        {
            CSharpParser parser = new CSharpParser();
            SyntaxTree syntaxTree = parser.Parse(code);
            string fileName = Path.GetTempFileName();
            File.WriteAllText(fileName, code);
            syntaxTree.FileName = fileName;
            syntaxTree.Freeze();

            // Should probably take into account which namespaces the user is using and load the needed assemblies into the CSharpCompletion object
            // string usings = syntaxTree.Descendants.OfType<UsingDeclaration>().Select(x => x.ToString()).Aggregate((x, y) => x + /* Environment.NewLine + */ y);

            IDocument document = new ReadOnlyDocument(new StringTextSource(code), syntaxTree.FileName);
            completionResult = completion.GetCompletions(document, offset, controlSpace);

            // Set the trigger word for later use.
            triggerWord = controlSpace ? completionResult.TriggerWord : string.Empty;

            // If the user pressed control space, we assume they are trying to generate completions for a partially typed word.
            // In this situation we need to filter the results based on what they have already typed.
            // The exception is if the most recent character is a period. 
            // No idea why NRefactory can't do this for us.
            if (controlSpace && !string.IsNullOrEmpty(completionResult.TriggerWord) && code[offset - 1] != '.')
            {
                // Filter items.
                completionResult.CompletionData = completionResult.CompletionData.Where(item => GetMatchQuality(item.CompletionText, completionResult.TriggerWord) > 0).ToList();
            }
            List<ICompletionItem> completionList = completionResult.CompletionData.Select(x => x as CompletionData).Where(x => x != null).OrderBy(x => x.CompletionText).ToList<ICompletionItem>();
            view.Populate(completionList);
            if (controlSpace && !string.IsNullOrEmpty(completionResult.TriggerWord))
                view.SelectItem(completionList.IndexOf(completionList.OrderByDescending(x => GetMatchQuality(x.CompletionText, completionResult.TriggerWord)).FirstOrDefault()));

            File.Delete(fileName);
            return completionList.Any();
        }
#else
        /// <summary>
        /// Generates completion options for a manager script.
        /// </summary>
        /// <param name="code">Source code.</param>
        /// <param name="offset">Offset of the cursor/caret in the source code.</param>
        /// <param name="controlSpace">True iff this intellisense request was generated by the user pressing control + space.</param>
        /// <returns>True if any completion options are found. False otherwise.</returns>
        public async Task<List<NeedContextItemsArgs.ContextItem>> GenerateScriptCompletions(string code, int offset, bool controlSpace = false)
        {
            // Alternative approach using CompletionService:
            //
            UpdateDocument(code);
            string contents = (await doc.GetTextAsync()).ToString();
            CompletionList results = await completion.GetCompletionsAsync(doc, offset);

            if (results == null)
                return null;

            // Can't use await in a lambda...ugh
            List<NeedContextItemsArgs.ContextItem> result = new List<NeedContextItemsArgs.ContextItem>();
            foreach (CompletionItem item in results.Items)
                result.Add(await GetContextItem(item));
            return result;
        }

        private async Task<NeedContextItemsArgs.ContextItem> GetContextItem(CompletionItem c)
        {
            return new NeedContextItemsArgs.ContextItem()
            {
                Name = c.DisplayText,
                Descr = (await completion.GetDescriptionAsync(doc, c)).Text,
                IsChildModel = false,
                IsEvent = false,
                IsMethod = true,

            };
        }

        private void UpdateDocument(string code)
        {
            if (doc == null)
            {
                doc = workspace.AddDocument(project.Id, "ManagerScript.cs", SourceText.From(code));
                completion = CompletionService.GetService(doc);
            }

            Document document = doc.WithText(SourceText.From(code));
            if (!workspace.TryApplyChanges(document.Project.Solution))
                throw new Exception("Unable to apply changes to solution");
            doc = workspace.CurrentSolution.GetDocument(doc.Id);
        }
#endif
        /// <summary>
        /// Generates completion options for a series.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="offset"></param>
        /// <param name="tableName"></param>
        /// <param name="storage"></param>
        /// <returns></returns>
        public bool GenerateSeriesCompletions(string text, int offset, string tableName, IStorageReader storage)
        {
            triggerWord = text?.Substring(0, offset).Split(' ').Last().Replace("[", "").Replace("]", "");
            
            List<string> columnNames = storage.ColumnNames(tableName).ToList();
            List<NeedContextItemsArgs.ContextItem> intellisenseOptions = new List<NeedContextItemsArgs.ContextItem>();
            foreach (string columnName in columnNames)
            {
                if (string.IsNullOrEmpty(triggerWord) || string.IsNullOrEmpty(triggerWord.Replace("[", "").Replace("]", "")) || columnName.StartsWith(triggerWord.Replace("[", "").Replace("]", "")))
                intellisenseOptions.Add(new NeedContextItemsArgs.ContextItem()
                {
                    Name = columnName,
                    Units = string.Empty,
                    TypeName = string.Empty,
                    Descr = string.Empty,
                    ParamString = string.Empty
                });
            }
            if (intellisenseOptions.Any())
                view.Populate(intellisenseOptions);
            return intellisenseOptions.Any();
        }

        /// <summary>
        /// Shows completion information for a method call.
        /// </summary>
        /// <param name="relativeTo">Model to be used as a reference when searching for completion data.</param>
        /// <param name="code">Code for which we want to generate completion data.</param>
        /// <param name="offset">Offset of the cursor/caret in the code.</param>
        /// <param name="location">Location of the caret/cursor in the editor.</param>
        public void ShowScriptMethodCompletion(IModel relativeTo, string code, int offset, Point location)
        {
#if NETFRAMEWORK
            CSharpParser parser = new CSharpParser();
            SyntaxTree syntaxTree = parser.Parse(code);
            string fileName = Path.GetTempFileName();
            File.WriteAllText(fileName, code);
            syntaxTree.FileName = fileName;
            syntaxTree.Freeze();

            IDocument document = new ReadOnlyDocument(new StringTextSource(code), syntaxTree.FileName);
            CodeCompletionResult result = completion.GetMethodCompletion(document, offset, false);
            File.Delete(fileName);
            if (result.OverloadProvider != null)
            {
                if (result.OverloadProvider.Count < 1)
                    return;
                List<MethodCompletion> completions = new List<MethodCompletion>();
                foreach (IParameterizedMember method in result.OverloadProvider.Items.Select(x => x.Method))
                {
                    // Generate argument signatures - e.g. string foo, int bar
                    List<string> arguments = new List<string>();
                    foreach (var parameter in method.Parameters)
                    {
                        string parameterString = string.Format("{0} {1}", parameter.Type.Name, parameter.Name);
                        if (parameter.ConstantValue != null)
                            parameterString += string.Format(" = {0}", parameter.ConstantValue.ToString());
                        arguments.Add(parameterString);
                    }

                    MethodCompletion completion = new MethodCompletion()
                    {
                        Signature = string.Format("{0} {1}({2})", method.ReturnType.Name, method.Name, arguments.Any() ? arguments.Aggregate((x, y) => string.Format("{0}, {1}", x, y)) : string.Empty)
                    };

                    if (method.Documentation == null)
                    {
                        completion.Summary = string.Empty;
                        completion.ParameterDocumentation = string.Empty;
                    }
                    else
                    {
                        if (method.Documentation.Xml.Text.Contains("<summary>") && method.Documentation.Xml.Text.Contains("</summary>"))
                            completion.Summary = method.Documentation.Xml.Text.Substring(0, method.Documentation.Xml.Text.IndexOf("</summary")).Replace("<summary>", string.Empty).Trim(Environment.NewLine.ToCharArray()).Trim();
                        else
                            completion.Summary = string.Empty;

                        // NRefactory doesn't do anything more than read the xml documentation file.
                        // Therefore, we need to parse this XML to get the parameter summaries.
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(string.Format("<documentation>{0}</documentation>", method.Documentation.Xml.Text));
                        List<string> argumentSummariesList = new List<string>();
                        foreach (XmlElement parameter in XmlUtilities.ChildNodesRecursively(doc.FirstChild, "param"))
                            argumentSummariesList.Add(string.Format("{0}: {1}", parameter.GetAttribute("name"), parameter.InnerText));

                        if (argumentSummariesList.Any())
                            completion.ParameterDocumentation = argumentSummariesList.Aggregate((x, y) => x + Environment.NewLine + y);
                        else
                            completion.ParameterDocumentation = string.Empty;
                    }

                    completions.Add(completion);
                }

                methodCompletionView.Completions = completions;
                methodCompletionView.Location = location;
                methodCompletionView.Visible = true;
            }
#else
            throw new NotImplementedException();
#endif
        }

        /// <summary>
        /// Shows completion information for a method call.
        /// </summary>
        /// <param name="relativeTo">Model to be used as a reference when searching for completion data.</param>
        /// <param name="code">Code for which we want to generate completion data.</param>
        /// <param name="offset">Offset of the cursor/caret in the code.</param>
        /// <param name="location">Location of the cursor/caret in the editor.</param>
        public void ShowMethodCompletion(IModel relativeTo, string code, int offset, Point location)
        {
            string contentsToCursor = code.Substring(0, offset).TrimEnd('.');

            // Ignore everything before the most recent comma.
            contentsToCursor = contentsToCursor.Substring(contentsToCursor.LastIndexOf(',') + 1);

            string currentLine = contentsToCursor.Split(Environment.NewLine.ToCharArray()).Last().Trim();
            // Set the trigger word for later use.
            triggerWord = GetTriggerWord(currentLine);
            
            // Ignore everything before most recent model name in square brackets.
            // I'm assuming that model/node names cannot start with a number.
            string modelNamePattern = @"\[([A-Za-z]+[A-Za-z0-9]*)\]";
            string objectName = currentLine;
            var matches = System.Text.RegularExpressions.Regex.Matches(code, modelNamePattern);
            if (matches.Count > 0)
            {
                int modelNameIndex = currentLine.LastIndexOf(matches[matches.Count - 1].Value);
                if (modelNameIndex >= 0)
                {
                    currentLine = currentLine.Substring(modelNameIndex);
                    int lastPeriod = currentLine.LastIndexOf('.');
                    objectName = lastPeriod >= 0 ? currentLine.Substring(0, lastPeriod) : currentLine;
                }
            }
            string methodName = triggerWord.TrimEnd('(');
            MethodInfo method = NeedContextItemsArgs.GetMethodInfo(relativeTo as Model, methodName, objectName);

            if (method == null)
                return;
            MethodCompletion completion = new MethodCompletion();

            List<string> parameterStrings = new List<string>();
            StringBuilder parameterDocumentation = new StringBuilder();
            foreach (ParameterInfo parameter in method.GetParameters())
            {
                string parameterString = string.Format("{0} {1}", parameter.ParameterType.Name, parameter.Name);
                if (parameter.DefaultValue != DBNull.Value)
                    parameterString += string.Format(" = {0}", parameter.DefaultValue.ToString());
                parameterStrings.Add(parameterString);
                parameterDocumentation.AppendLine(string.Format("{0}: {1}", parameter.Name, NeedContextItemsArgs.GetDescription(method, parameter.Name)));
            }
            string parameters = parameterStrings.Aggregate((a, b) => string.Format("{0}, {1}", a, b));

            completion.Signature = string.Format("{0} {1}({2})", method.ReturnType.Name, method.Name, parameters);
            completion.Summary = NeedContextItemsArgs.GetDescription(method);
            completion.ParameterDocumentation = parameterDocumentation.ToString().Trim(Environment.NewLine.ToCharArray());

            methodCompletionView.Completions = new List<MethodCompletion>() { completion };
            methodCompletionView.Location = location;
            methodCompletionView.Visible = true;
        }

        /// <summary>
        /// Displays the intellisense popup at the given coordinates.
        /// </summary>
        /// <param name="x">x-coordinate at which the popup will be displayed.</param>
        /// <param name="y">y-coordinate at which the popup will be displayed.</param>
        /// <param name="lineHeight">Line height (in px?).</param>
        public void Show(int x, int y, int lineHeight = 17)
        {
            if (methodCompletionView.Visible)
                methodCompletionView.Visible = false;
            view.SmartShowAtCoordinates(x, y, lineHeight);
#if NETFRAMEWORK
            // TBI in netcore builds
            if (completionResult != null && completionResult.SuggestedCompletionDataItem != null)
            {
                int index = completionResult.CompletionData.IndexOf(completionResult.SuggestedCompletionDataItem);
                if (index >= 0)
                    view.SelectItem(index);
            }
#endif
            recentLocation = new Point(x, y);
        }

        /// <summary>
        /// Unsubscribes events, releases unmanaged resources, all that fun stuff.
        /// </summary>
        public void Cleanup()
        {
            view.ItemSelected -= ContextItemSelected;
            view?.Cleanup();
            methodCompletionView.Destroy();
            methodCompletionView.Visible = false;
        }

        /// <summary>
        /// Gets the trigger word - the partially completed word for which the user wants completion options.
        /// </summary>
        /// <param name="textBeforeCursor">Text before the cursor.</param>
        /// <returns></returns>
        private string GetTriggerWord(string textBeforeCursor)
        {
            if (!textBeforeCursor.Contains("."))
                return textBeforeCursor;

            // Return a substring starting just after the last period in the string.
            return textBeforeCursor.Substring(textBeforeCursor.LastIndexOf('.') + 1);
        }

        /// <summary>
        /// Determines how well an item matches the completion word.
        /// </summary>
        /// <param name="itemText">Name of a completion option.</param>
        /// <param name="query">The completion word.</param>
        /// <returns>
        /// Number representing the quality of the match. Higher numbers represent closer matches.
        /// 8 represents an exact match, -1 represents no match.
        /// </returns>
        private int GetMatchQuality(string itemText, string query)
        {
            if (itemText == null)
                throw new ArgumentNullException("itemText", "ICompletionData.Text returned null");

            // Qualities:
            //  	8 = full match case sensitive
            // 		7 = full match
            // 		6 = match start case sensitive
            //		5 = match start
            //		4 = match CamelCase when length of query is 1 or 2 characters
            // 		3 = match substring case sensitive
            //		2 = match substring
            //		1 = match CamelCase
            //		-1 = no match

            if (query == itemText)
                return 8;
            if (string.Equals(itemText, query, StringComparison.InvariantCultureIgnoreCase))
                return 7;

            if (itemText.StartsWith(query, StringComparison.InvariantCulture))
                return 6;
            if (itemText.StartsWith(query, StringComparison.InvariantCultureIgnoreCase))
                return 5;

            bool? camelCaseMatch = null;
            if (query.Length <= 2)
            {
                camelCaseMatch = CamelCaseMatch(itemText, query);
                if (camelCaseMatch == true) return 4;
            }
            
            if (itemText.IndexOf(query, StringComparison.InvariantCulture) >= 0)
                return 3;
            if (itemText.IndexOf(query, StringComparison.InvariantCultureIgnoreCase) >= 0)
                return 2;

            if (!camelCaseMatch.HasValue)
                camelCaseMatch = CamelCaseMatch(itemText, query);
            if (camelCaseMatch == true)
                return 1;

            return -1;
        }

        /// <summary>
        /// Checks if two strings match on the upper case letters.
        /// e.g. "CodeQualityAnalysis" matches "CQ".
        /// </summary>
        /// <param name="text">The camel case word.</param>
        /// <param name="query">The acronym/abbreviated word to test.</param>
        /// <returns>True if the upper case letters match, false otherwise.</returns>
        private bool CamelCaseMatch(string text, string query)
        {
            int i = 0;
            foreach (char upper in text.Where(c => char.IsUpper(c)))
            {
                if (i > query.Length - 1)
                    return true;    // return true here for CamelCase partial match ("CQ" matches "CodeQualityAnalysis")
                if (char.ToUpper(query[i], CultureInfo.InvariantCulture) != upper)
                    return false;
                i++;
            }
            if (i >= query.Length)
                return true;
            return false;
        }

        /// <summary>
        /// Invoked when the user selects a completion option.
        /// Removes the intellisense popup, and displays the method completion popup
        /// if the selected item is a method.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event Arguments.</param>
        private void ContextItemSelected(object sender, NeedContextItemsArgs.ContextItem args)
        {
            IntellisenseItemSelectedArgs itemSelectedArgs = new IntellisenseItemSelectedArgs()
            {
                TriggerWord = triggerWord,
                ItemSelected = args.Name + (args.IsMethod ? "(" : ""),
                IsMethod = args.IsMethod
            };
            OnItemSelected?.Invoke(this, itemSelectedArgs);
        }
    }
}
