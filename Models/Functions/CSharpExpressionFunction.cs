using System;
using APSIM.Shared.Documentation;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Models.Core;
using APSIM.Shared.Utilities;

namespace Models.Functions
{

    /// <summary>
    /// A c# expression is evaluated.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class CSharpExpressionFunction : Model, IFunction
    {
        [NonSerialized]
        [Link]
        private ScriptCompiler scriptCompiler = null;

        /// <summary>An instance of an IFunction that is our compiled expression.</summary>
        private IFunction expressionFunction;

        /// <summary>The expression.</summary>
        [Description("C# expression")]
        public string Expression { get; set; }

        /// <summary>the script compiler.</summary>
        /// <param name="compiler"></param>
        public void SetCompiler(ScriptCompiler compiler)
        {
            scriptCompiler = compiler;
        }

        /// <summary>Gets the value of the expression.</summary>
        public double Value(int arrayIndex = -1)
        {
            return expressionFunction.Value(arrayIndex);
        }

        /// <summary>
        /// At design time the [Link] above will be null. In that case search for a 
        /// Simulations object and get its compiler.
        /// 
        /// </summary>
        private ScriptCompiler Compiler()
        {
            if (scriptCompiler == null)
            {
                var simulations = FindAncestor<Simulations>();
                if (simulations == null)
                    throw new Exception("Cannot find a script compiler in manager.");
                scriptCompiler = simulations.ScriptCompiler;
            }
            return scriptCompiler;
        }

        /// <summary>
        /// Document the model.
        /// </summary>
        public override IEnumerable<ITag> Document()
        {
            yield return new Paragraph($"{Name} = {Expression?.Replace(".Value()", "")}");
        }

        /// <summary>
        /// Compile the expression and return the compiled function.
        /// </summary>
        public void CompileExpression()
        {
            // From a list of visible models in scope, create [Link] lines e.g.
            //    [Link] Clock Clock;
            //    [Link] Weather Weather;
            // and namespace lines e.g.
            //    using Models.Clock;
            //    using Models;
            var models = Parent.FindAllInScope().ToList().Where(model => !model.IsHidden && 
                                                              model.GetType() != typeof(Graph) &&
                                                              model.GetType() != typeof(Series) &&
                                                              model.GetType().Name != "StorageViaSockets");
            var linkList = new List<string>();
            var namespaceList = new SortedSet<string>();
            foreach (var model in models)
            {
                if (Expression.Contains(model.Name))
                {
                    linkList.Add($"        [Link(ByName=true)] {model.GetType().Name} {model.Name};");
                    namespaceList.Add("using " + model.GetType().Namespace + ";");
                }
            }
            var namespaces = StringUtilities.BuildString(namespaceList.Distinct(), Environment.NewLine);
            var links = StringUtilities.BuildString(linkList.Distinct(), Environment.NewLine);

            // Get template c# script.
            var template = ReflectionUtilities.GetResourceAsString("Models.Resources.Scripts.CSharpExpressionTemplate.cs");

            // Replace the "using Models;" namespace place holder with the namesspaces above.
            template = template.Replace("using Models;", namespaces);

            template = template.Replace("class Script", $"class {Name}Script");

            // Replace the link place holder in the template with links created above.
            template = template.Replace("        [Link] Clock Clock = null;", links.ToString());

            // Replace the expression place holder in the template with the real expression.
            template = template.Replace("return Clock.FractionComplete;", "return " + Expression + ";");

            // Create a new manager that will compile the expression.
            var result = Compiler().Compile(template, this);
            if (result.ErrorMessages == null)
            {
                if (expressionFunction == null || result.WasCompiled)
                {
                    expressionFunction = result.Instance as IFunction;
                    (expressionFunction as IModel).Parent = this;

                    // Resolve links
                    var linkResolver = new Links();
                    linkResolver.Resolve(expressionFunction as IModel, true);
                }
            }
            else
            {
                var st = $"Cannot compile expression: {Expression}{Environment.NewLine}" +
                         $"{result.ErrorMessages}{Environment.NewLine}" +
                         $"Generated code: {Environment.NewLine}{template}";
                throw new Exception(st);
            }
        }

        /// <summary>
        /// Handler method for the start of simulation event.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            CompileExpression();

        }

    }
}


