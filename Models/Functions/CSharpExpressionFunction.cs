using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Core;
using APSIM.Shared.Utilities;
using Models.Core;

namespace Models.Functions
{
    /// <summary>
    /// A c# expression is evaluated.
    /// </summary>
    public class CSharpExpressionFunction
    {
        /// <summary>
        /// Compile the expression and return the compiled function.
        /// </summary>
        /// <param name="expression">The expression to compile.</param>
        /// <param name="relativeToNode">The node the expression is for.</param>
        /// <param name="compiler">An instance of the script compiler.</param>
        /// <param name="function">The returned function or null if not compilable.</param>
        /// <param name="errorMessages">The error messages from the compiler.</param>
        public static bool Compile<T>(string expression, Node relativeToNode, ScriptCompiler compiler,
                                      out T function, out string errorMessages)
        {
            var relativeTo = relativeToNode.Model as Model;
            if (compiler != null && relativeTo != null)
            {
                // From a list of visible models in scope, create [Link] lines e.g.
                //    [Link] IClock Clock;
                //    [Link] Weather Weather;
                // and namespace lines e.g.
                //    using Models.Clock;
                //    using Models;
                var models = relativeTo.FindAllInScope().ToList().Where(model => !model.IsHidden &&
                                                                        model.GetType() != typeof(Graph) &&
                                                                        model.GetType() != typeof(Series) &&
                                                                        model.GetType().Name != "StorageViaSockets");
                var linkList = new List<string>();
                var namespaceList = new SortedSet<string>();
                foreach (var model in models)
                {
                    if (expression.Contains(model.Name))
                    {
                        linkList.Add($"        [Link(ByName=true)] {model.GetType().Name} {model.Name};");
                        namespaceList.Add("using " + model.GetType().Namespace + ";");
                    }
                }
                var namespaces = StringUtilities.BuildString(namespaceList.Distinct(), Environment.NewLine);
                var links = StringUtilities.BuildString(linkList.Distinct(), Environment.NewLine);

                // Get template c# script.
                string template;
                if (typeof(T) == typeof(IBooleanFunction))
                    template = ReflectionUtilities.GetResourceAsString("Models.Resources.Scripts.CSharpBooleanExpressionTemplate.cs");
                else
                    template = ReflectionUtilities.GetResourceAsString("Models.Resources.Scripts.CSharpExpressionTemplate.cs");

                // Replace the "using Models;" namespace place holder with the namesspaces above.
                template = template.Replace("using Models;", namespaces);

                // Replace the link place holder in the template with links created above.
                template = template.Replace("        [Link] IClock Clock = null;", links.ToString());

                // Replace the expression place holder in the template with the real expression.
                template = template.Replace("return Clock.FractionComplete;", "return " + expression + ";");

                // Create a new manager that will compile the expression.
                var result = compiler.Compile(template, relativeToNode);
                if (result.ErrorMessages == null)
                {
                    errorMessages = null;
                    function = (T)result.Instance;

                    // Resolve links
                    var functionAsModel = function as IModel;
                    functionAsModel.Parent = relativeTo;
                    var linkResolver = new Links();
                    linkResolver.Resolve(functionAsModel, true);
                    return true;
                }
                else
                {
                    errorMessages = $"Cannot compile expression: {expression}{Environment.NewLine}" +
                                    $"{result.ErrorMessages}{Environment.NewLine}" +
                                    $"Generated code: {Environment.NewLine}{template}";
                    function = default;
                    return false;
                }
            }
            else
            {
                errorMessages = "Cannot find c# compiler";
                function = default;
                return false;
            }
        }
    }
}


