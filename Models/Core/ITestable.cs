namespace Models.Core
{
    /// <summary>
    /// An interface for something that is testable.
    /// </summary>
    public interface ITestable
    {
        /// <summary>Run tests. Should throw an exception if the test fails.</summary>
        void Test(bool accept, bool GUIrun);
    }
}
