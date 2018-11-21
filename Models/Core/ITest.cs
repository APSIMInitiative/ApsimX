namespace Models.Core
{
    /// <summary>
    /// An interface for a model which is a test.
    /// </summary>
    public interface ITest : IModel
    {
        /// <summary>
        /// Runs the test. Throws an exception on failure.
        /// </summary>
        void Run();
    }
}
