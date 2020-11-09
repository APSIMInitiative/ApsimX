namespace UnitTests
{
    using Models.Core;
    using Models.Core.Run;
    using Models.Storage;
    using System;

    [Serializable]
    class MockPostSimulationTool : Model, IPostSimulationTool
    {
        static public bool wasRun = false;
        bool doThrow;
        public MockPostSimulationTool(bool doThrow)
        {
            this.doThrow = doThrow;
        }
        public void Run()
        {
            if (doThrow)
                throw new Exception("Intentional exception");
            else
                wasRun = true;
        }
    }
}
