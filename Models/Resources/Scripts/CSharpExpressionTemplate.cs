namespace Models
{
    /// <summary>
    /// A template script used by CSharpExpressionFunction.
    /// </summary>
    [Serializable]
    public class Script : Model, IFunction
    {
        [Link] IClock Clock = null;

        /// <summary>Gets the value of the function.</summary>
        public double Value(int arrayIndex = -1)
        {
            return Clock.FractionComplete;
        }
    }
}