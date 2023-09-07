using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Data;
using MessagePack;
using APSIM.ZMQServer.IO;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Core.ApsimFile;
using static Models.Core.Overrides;
using Models.Core.Run;

/// <summary>
/// Encapsulate an apsim simulation & runner
/// Runs apsim in another thread
/// </summary>

namespace APSIM.ZMQServer
{
    public class ApsimEncapsulator
    {
        private Simulations sims;

        private Runner runner;

        private ServerJobRunner jobRunner;

        private List<Exception> errors = null;

        private Thread workerThread = null;

        public ApsimEncapsulator(GlobalServerOptions options)
        {
            sims = FileFormat.ReadFromFile<Simulations>(options.File, e => throw e, false).NewModel as Simulations;
            sims.FindChild<Models.Storage.DataStore>().UseInMemoryDB = true;
            runner = new Runner(sims, numberOfProcessors: (int)options.WorkerCpuCount);
            jobRunner = new ServerJobRunner(this);
            runner.Use(jobRunner);
            runState = runStateT.idling;
        }

        public runStateT runState;

        public enum runStateT
        {
            idling, running, waiting
        }

        public void aboutToStart(string s)
        {
#if true
            //Console.WriteLine("About to start " + s);
            var sim = sims.FindChild<Simulation>(s);

            // fixme hook up a semaphore here
            //foreach (var x in sim.FindAllByPath("[Semaphore].Script.Semaphore"))
            var s2 = sim.FindAllByPath("[Synchroniser].Script.Identifier");
            Console.WriteLine("n=" + s2.Count().ToString());
            foreach (var s3 in s2)
            {
                object s4 = s3.Value;
                //var t4 = s4.GetType();

                //if (t4.GetMethod("OnPause") != null)
                //{
                    //t4.GetMethod("OnPause") = OnPause = x => Console.WriteLine(x);
                //}
            }
#endif
        }
        public bool HasMethod(object objectToCheck, string methodName)
        {
            var type = objectToCheck.GetType();
            return type.GetMethod(methodName) != null;
        }
        public event Action<string> onPaused;
        public event Action<string> onRunFinished;
        public event Action<string> onRunStart;

        /// <summary>
        // set the values immediately 
        // Syntax: [Manager].Script.CultivarName = Blah
        /// </summary>
        // fixme - this ignores any undos, it makes a permanent change to the simulation
        public void setVariable(string[] nvPairs)
        {
            // Overrides.Apply(sims, Overrides.ParseStrings(nvPairs)); << only does 1st occurence
            for (var i = 0; i < nvPairs.Length; i++)
            {
                var name = nvPairs[0].Substring(0, nvPairs[0].IndexOf("=")).Trim(' ');
                var newValue = nvPairs[0].Substring(nvPairs[0].IndexOf("=") + 1).Trim(' ');
                foreach (var v in sims.FindAllByPath(name))
                {
                    v.Value = newValue;
                    //Console.WriteLine(v.Name + " =>> " + v.Value.ToString());
                }
            }
        }

        ///
        // get a variable from the model
        // looks like [Manager].Script.TestVariable
        // result as byte array
        /// 
        public byte[] getVariableFromModel(string variablePath)
        {
            var v = sims.FindAllByPath(variablePath).ToArray().Select(x => x.Value);
            byte[] bytes = MessagePackSerializer.Serialize(v);
            return (bytes);
        }

        ///
        // get a variable from the datastore 
        // looks like <tablename>.<variablename>
        // result as byte array
        /// 
        public byte[] getVariableFromDS(string variablePath)
        {
            var vp = variablePath.IndexOf(".");
            if (vp < 0)
                throw new Exception($"get V {variablePath} should be a dotted table/column pair.");
            string tableName = variablePath.Substring(0, vp);
            string fieldName = variablePath.Substring(vp + 1);
            var storage = sims?.FindChild<Models.Storage.IDataStore>();

            if (!storage.Reader.TableNames.Contains(tableName))
                throw new Exception($"Table {tableName} does not exist in the database.");

            DataTable Result = storage.Reader.GetData(tableName, fieldName);

            if (Result == null)
                throw new Exception($"Unable to read table {tableName} from datastore (cause unknown - but the table appears to exist)");

            if (Result.Columns[fieldName] == null)
                throw new Exception($"Column {fieldName} does not exist in table {tableName}");

            var data = Result.AsEnumerable().Select(r => r[fieldName]).ToArray();
            byte[] bytes = MessagePackSerializer.Serialize(data);
            return (bytes);
        }

        /// <summary>
        // Run the loaded simulation with specified changes. Return immediately
        /// </summary>
        public void Run(string[] changes)
        {
            runState = runStateT.running;
            errors = null;
            if (changes != null)
                jobRunner.Replacements = Overrides.ParseStrings(changes);
            else
                jobRunner.Replacements = Enumerable.Empty<Override>();

            Action onWorkerExit = () =>
            {
                runState = runStateT.idling;
                onRunFinished?.Invoke("idling");
                workerThread = null;
            };
            workerThread = new Thread(
              new ThreadStart(() =>
              {
                  var timer = Stopwatch.StartNew();
                  try
                  {
                      errors = runner.Run();
                  }
                  finally
                  {
                      timer.Stop();
                      Console.WriteLine($"Job took {timer.ElapsedMilliseconds}ms");
                      if (errors != null && errors.Count > 0)
                      {
                          Console.WriteLine("Errors:\n");
                          foreach (var e in errors) { Console.WriteLine(e.ToString()); }
                      }
                      onWorkerExit();
                  }
              }));

            workerThread.Start();
            onRunStart?.Invoke("running");
        }
        /// Wait until the simulation terminates (thread exit), or until all 
        /// simulations paused
        public void WaitForStateChange()
        {
#if false
            while (true)
            {
                workerThread?.Join(250);
                if (workerThread == null)
                    break;
                // this is wrong, need to syhchronise between threads, not poll them
                var s2 = sims.FindAllByPath("[Semaphore].Script.semaphore");
                foreach (var x in s2)
                {
                    Console.WriteLine("x=", x.ToString());
                }
            }
#else
            workerThread?.Join();
#endif
        }
        public void Proceed()
        {

        }
        public List<Exception> getErrors()
        {
            if (runState == runStateT.idling)
            {
                return (errors);
            }
            return (null);
        }

        /// <summary>
        /// Cleanup the simulations, disconnect events, etc.
        /// </summary>
        public void Close()
        {
            sims?.FindChild<Models.Storage.IDataStore>()?.Close();
        }
    }
}
