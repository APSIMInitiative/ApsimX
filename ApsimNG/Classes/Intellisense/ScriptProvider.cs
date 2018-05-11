namespace UserInterface.Intellisense
{
    public class ScriptProvider : ICSharpScriptProvider
    {
        public string GetUsing()
        {
            return "" +
                "using System; " +
                "using System.Collections.Generic; " +
                "using System.Linq; " +
                "using System.Text; " +
                "using Models; ";
        }

        public string GetVars()
        {
            return "int age = 25;";
        }

        public string GetNamespace() => null;
    }
}
