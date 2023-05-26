namespace Models
{
    /// <summary>
    /// A template script used by CSharpExpressionFunction.
    /// </summary>
    [Serializable]
    public class Script : Model, IBooleanFunction
    {
        [Link] IClock Clock = null;

        /// <summary>Gets the value of the function.</summary>
        public bool Value()
        {
            return Clock.FractionComplete;
        }
    }
}