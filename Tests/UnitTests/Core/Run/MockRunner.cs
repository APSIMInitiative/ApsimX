using System;
using System.Collections.Generic;
using APSIM.Core;

namespace UnitTests.Core.Run;

public class MockRunner : IRunner
{
    public Action<Exception> ErrorHandler { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public double Progress => throw new NotImplementedException();

    public string Status => throw new NotImplementedException();

    public event EventHandler<IRunner.AllJobsCompletedArgs> AllSimulationsCompleted;

    public bool RunCalled { get; set;  } = false;

    public INodeModel RelativeTo { get; set;  }

    public List<Exception> Run()
    {
        RunCalled = true;
        return null;
    }

    public void Run(INodeModel relativeTo)
    {
        RunCalled = true;
        RelativeTo = relativeTo;
    }

    public void Stop()
    {
        throw new NotImplementedException();
    }
}