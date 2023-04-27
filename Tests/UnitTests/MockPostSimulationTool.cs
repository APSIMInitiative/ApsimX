using Models.Core;
using Models.Core.Run;
using System;

namespace UnitTests
{

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
