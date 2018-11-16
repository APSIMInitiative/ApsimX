namespace Models.Core
{
    /// <summary>
    /// An interface for a model which is a test.
    /// </summary>
    public interface ITest : IModel
    {
        /// <summary>
        /// Runs the test. Returns true iff successful.
        /// </summary>
        /// <returns>True iff successful.</returns>
        bool Run();
    }
}
