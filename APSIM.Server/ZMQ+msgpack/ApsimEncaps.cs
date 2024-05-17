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
using NetMQ;
using NetMQ.Sockets;
using Models;

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

        private string Identifier { get; set; }

        private RequestSocket connection = null;

        public interface IClientMsg
        {
            
        }
        public ApsimEncapsulator(GlobalServerOptions options)
        {
            // read from file
            sims = FileFormat.ReadFromFile<Simulations>(options.File, e => throw e, false).NewModel as Simulations;
            sims.FindChild<Models.Storage.DataStore>().UseInMemoryDB = true;

            // open zmq connections
            Identifier = string.Format("tcp://{0}:{1}", options.IPAddress, options.Port);
            connection = new RequestSocket(Identifier);
            connection.SendFrame("connect");
            Console.WriteLine("Sent connect");
            var msg = connection.ReceiveFrameString();
            if (msg != "ok") { throw new Exception("Expected ok"); }
            
            // Add synchroniser model to tree
            Synchroniser synchroniser = new Synchroniser();
            Simulation sim_root = sims.FindChild<Simulation>();
            sim_root.Children.Add(synchroniser);
            // "init" synchroniser model by setting parent and calling
            // OnCreated(). It seems to just toggle a flag and check for
            // duplicate names
            synchroniser.Identifier = Identifier;
            synchroniser.Parent = sim_root;
            synchroniser.OnCreated();

            // Get the template field. Expects only a single field to be
            // defined in the file 
            Zone template_field = sim_root.FindChild<Zone>();
            // TODO check for null return

            // send string indicating we are in the setup phase
            // TODO(nubby): redefine interface to allow for more customizeable Field creation.
            connection.SendFrame("setup");
            var next_msg = connection.ReceiveMultipartMessage();
            // Awaits the command "energize" to start the simulation.
            string command = next_msg[0].ConvertToString();
            int fieldNum = 0;
            while (command != "energize")
            {
                switch (command)
                {
                    case "fields":
                        int num_fields = MessagePackSerializer.Deserialize<int>(next_msg[1].Buffer);
                        for (int i = 0; i < num_fields; i++)
                        { 
                            Zone clone = Apsim.Clone<Zone>(template_field);
                            clone.Name = $"Field{fieldNum}";
                            // add to simulation tree
                            sim_root.Children.Add(clone);
                            // register irrigator with synchroniser
                            Irrigation irrigation = clone.FindChild<Irrigation>();
                            synchroniser.IrrigationList.Add(irrigation);
                            fieldNum++;
                        }
                        break;
                    case "field":
                        // TODO(nubby):
                        //  3. translate K-V pairs into Field params.
                        Zone newField = Apsim.Clone<Zone>(template_field);
                        newField.Name = $"Field{fieldNum}";
                        Dictionary<string, dynamic> fieldConfigs = new Dictionary<string, dynamic>();
                        foreach (var arg in next_msg.Skip(1))
                        {
                            // TODO(nubby): Error handling.
                            string [] kv = MessagePackSerializer.Deserialize<dynamic>(arg.Buffer).Split(',');
                            try
                            {
                                fieldConfigs.Add(kv[0], kv[1]);
                            }
                            catch (ArgumentException)
                            {
                                Console.WriteLine(
                                    $"Key {kv[0]} already configured; please only configure each Field param once."
                                );
                            }
                        };
                        foreach (string key in fieldConfigs.Keys)
                        {
                            switch (key)
                            {
                                case "Name":
                                    newField.Name = fieldConfigs[key];
                                    break;
                                case "X":
                                    newField.X = Convert.ToDouble(fieldConfigs[key]);
                                    break;
                                case "Y":
                                    newField.Y = Convert.ToDouble(fieldConfigs[key]);
                                    break;
                                case "Z":
                                    newField.Z = Convert.ToDouble(fieldConfigs[key]);
                                    break;
                            }
                        }
                        // add to simulation tree
                        sim_root.Children.Add(newField);
                        // register irrigator with synchroniser
                        Irrigation irrigationNew = newField.FindChild<Irrigation>();
                        synchroniser.IrrigationList.Add(irrigationNew);
                        Console.WriteLine($"Added {newField.Name} to simulation.");
                        // Return the index of the newly-created Field to the Client.
                        byte[] bFieldNum = BitConverter.GetBytes(fieldNum);
                        if (BitConverter.IsLittleEndian)    // (for portability.)
                        {
                            Array.Reverse(bFieldNum);
                        }
                        connection.SendFrame(bFieldNum);
                        fieldNum++;
                        break;
                    case "energize":
                        Console.WriteLine("Setup complete; beginning simulation...");
                        connection.SendFrame("ready");
                        break;
                    default:
                        Console.WriteLine("Unknown setup command {0}", command);
                        break;
                }
                next_msg = connection.ReceiveMultipartMessage();
                command = next_msg[0].ConvertToString();
            }

            // close socket
            connection.Close();

            // disable template field
            template_field.Enabled = false;

            // configure runners
            runner = new Runner(sims, numberOfProcessors: (int)options.WorkerCpuCount);
            jobRunner = new ServerJobRunner(this);
            runner.Use(jobRunner);
        }

        public void aboutToStart(string s)
        {
#if false
            Console.WriteLine("About to start " + s);
            var sim = sims.FindChild<Simulation>(s);
#endif
        }
#if false
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
#endif

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
            errors = null;
            if (changes != null)
                jobRunner.Replacements = Overrides.ParseStrings(changes);
            else
                jobRunner.Replacements = Enumerable.Empty<Override>();

            Action onWorkerExit = () =>
            {
                workerThread = null;
            };
            workerThread = new Thread(
              new ThreadStart(() =>
              {
                  try
                  {
                      errors = runner.Run();
                  }
                  finally
                  {
                      if (errors != null && errors.Count > 0)
                      {
                          Console.WriteLine("Errors:\n");
                       
                          foreach (var e in errors) { Console.WriteLine(e.ToString()); }
                      }
                      onWorkerExit();
                  }
              }));

            workerThread.Start();
        }
        /// Wait until the simulation terminates (thread exit)
        public void WaitForStateChange()
        {
            workerThread?.Join();
        }
        public List<Exception> getErrors()
        {
            return (errors);
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
