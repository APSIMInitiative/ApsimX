using Models.Soils.Nutrients;
using System.Linq;
using System;
using Models.Core;
using Models.PMF;
using Models.Soils;
using Models.Surface;
using Models.Soils.Nutrients;
using Models.Utilities;
using Models.Functions;
using MessagePack;
using System.IO;
using System.Collections.Generic;

using NetMQ;
using NetMQ.Sockets;

using APSIM.Shared.Utilities;

namespace Models
{
    [Serializable]
    public class Synchroniser : Model
    {
        [Link] Clock clock;
        [Link] Simulation simulation;
        [Link] private Summary Summary;

        [NonSerialized]
        private RequestSocket connection;

        public string Identifier { get; set; }

        // public list of irrigation types
        public List<Irrigation> IrrigationList = new List<Irrigation>();

        // class to store irrigation data
        [Serializable]
        public class IrrigationData
        {
            public Irrigation irrigation { get; set; }
            public double IrrigationAmount { get; set; }
        }

        // Queue for storing irrigation data
        Queue<IrrigationData> irrigationQueue = new Queue<IrrigationData>();

        [EventSubscribe("StartOfSimulation")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            // open new socket
            connection = new RequestSocket(Identifier);

            Console.WriteLine("Simulation starting");
        }

        [EventSubscribe("EndOfDay")]
        private void OnEndOfDay(object sender, EventArgs e)
        {
            if (connection == null) { return; }
            bool moreToDo = true;
            connection.SendFrame("paused");
            while (moreToDo)
            {
                var msg = connection.ReceiveMultipartMessage();
                //Console.WriteLine("Got {0} parts, first = {1}", msg.FrameCount, msg[0].ConvertToString()); 
                if (msg.FrameCount <= 0) { continue; }

                var command = msg[0].ConvertToString();
                string simVarName;
                object simVarValue;
                switch (command)
                {
                    case "resume":
                        moreToDo = false;
                        break;
                    case "do":
                        if (msg.FrameCount >= 2)
                            {
                                onDoCommand(msg);
                                connection.SendFrame("ok");
                            }
                        break;
                    case "set":
                        // set an apsim variable. 
                        //  arg 1 is the variable path (eg "[Nutrient].NO3.kgha"), 
                        //  arg 2 is the packed object                
                        simVarName = MessagePackSerializer.Deserialize<string>(msg[1].Buffer);

                        // See what type the apsim variable is
                        var myType = simulation.Get(simVarName).GetType();
                        //Console.WriteLine("Got set {0}, my type is {1}", simVarName, myType);

                        simVarValue = MessagePackSerializer.Deserialize<object>(msg[2].Buffer);
                        //Console.WriteLine("Got set {0} of incoming type {1}", simVarName, simVarValue.GetType());
                        if (simVarValue.GetType().IsArray != myType.IsArray)
                        {
                            throw new Exception("Array/scalar mismatch for " + simVarName);
                        }
                        if (myType.IsArray)
                        {
                            Type myElementType = myType.GetElementType();
                            Object[] simVarValues = (simVarValue as object[]);
                            if (isNumeric(myElementType))
                            {
                                var myValues = Array.ConvertAll(simVarValues, (e) => (double)e);
                                simulation.Set(simVarName, myValues);
                            }
                            else if (isInteger(myElementType))
                            {
                                var myValues = Array.ConvertAll(simVarValues, (e) => (int)e);
                                simulation.Set(simVarName, myValues);
                            }
                            else if (isString(myElementType))
                            {
                                var myValues = Array.ConvertAll(simVarValues, (e) => (string)e);
                                simulation.Set(simVarName, myValues);
                            }
                            else
                            {
                                throw new Exception("Don't know what to do setting a " + myElementType + " variable");
                            }

                            //Type myElementType = myType.GetElementType();
                            //var simVarValues = Array.ConvertAll
                            //(
                            //  (object[])simVarValue, (e) => Convert.ChangeType(e, myElementType)
                            //) as System.Array;
                            //Console.WriteLine("Doing set {0} of type {1} ({2}) = ", simVarName, 
                            //                  simVarValues.GetType(), simVarValues.GetType().GetElementType(),  
                            //                  simVarValues.GetValue(0).ToString());
                            //simulation.Set(simVarName, simVarValues);
                        }
                        else
                        {
                            simulation.Set(simVarName, Convert.ChangeType(simVarValue, myType));
                        }
                        connection.SendFrame("ok");
                        break;
                    case "get":
                        if (msg.FrameCount == 2)
                        {
                            simVarName = MessagePackSerializer.Deserialize<string>(msg[1].Buffer);
                            simVarValue = simulation.Get(simVarName);
                            if (simVarValue is IFunction function)
                                simVarValue = function.Value();
                            else if (simVarValue != null && (simVarValue.GetType().IsArray || simVarValue.GetType().IsClass))
                            {
                                try
                                {
                                    simVarValue = ReflectionUtilities.Clone(simVarValue);
                                }
                                catch (Exception err)
                                {
                                    throw new Exception
                                        (
                                         $@"Cannot report variable {simVarName}:
                                            Variable is a non-reportable type:
                                            {simVarValue?.GetType()?.Name}.", err
                                        );
                                }
                            }
                            //Console.WriteLine("Got get '{0}' of type '{1}'", simVarName, simVarValue?.GetType()); 
                            byte[] bytes;
                            if (simVarValue != null)
                            {
                                bytes = MessagePackSerializer.Serialize(simVarValue);
                            }
                            else
                            {
                                Console.WriteLine("Sending NA");
                                bytes = MessagePackSerializer.Serialize("NA"); // fixme. Probably a better way to do this
                            }
                            connection.SendFrame(bytes);
                        }
                        break;
                    default:
                        throw new Exception("Expected resume/get/set, not '" + command + "'");
                        break;
                }
            }
        }

        // Handle the following commands:
        // applyIrrigation "amount" <double>
        // terminate

        public void onDoCommand(NetMQMessage msg)
        {
            // msg[0] is "do"
            // msg[1] specifies the command
            var cmd = MessagePackSerializer.Deserialize<string>(msg[1].Buffer);

            if (cmd == "applyIrrigation")
            {
                IrrigationData irig_data = new IrrigationData();

                for (int i = 2; i < msg.FrameCount; i += 2)
                {
                    var vname = MessagePackSerializer.Deserialize<string>(msg[i].Buffer);
                    // check for "amount" and decode
                    if (vname == "amount")
                    {
                        irig_data.IrrigationAmount = MessagePackSerializer.Deserialize<double>(msg[i + 1].Buffer);
                    }
                    // check for field index 
                    else if (vname == "field")
                    {
                        var irrigation_idx = MessagePackSerializer.Deserialize<int>(msg[i + 1].Buffer);
                        if (irrigation_idx >= IrrigationList.Count())
                        {
                            throw new Exception($"Field index is out of range. Idx: {irrigation_idx}");
                        }
                        irig_data.irrigation = IrrigationList[irrigation_idx];
                    }
                }

                // sanity check irrigation amount
                if (irig_data.IrrigationAmount > 0)
                {
                    irrigationQueue.Enqueue(irig_data);
                }
            }
            else if (cmd == "terminate")
            {
                clock.EndDate = clock.StartDate;
            }
        }

        [EventSubscribe("DoManagement")]
        private void OnDoManagement(object sender, EventArgs e)
        {
            // apply all irrigation
            while (irrigationQueue.Count > 0)
            {
                IrrigationData data = irrigationQueue.Dequeue();
                data.irrigation.Apply(data.IrrigationAmount, willRunoff: true);

                // Process the data
                Console.WriteLine($"Field {data.irrigation} irrigated with {data.IrrigationAmount} mm");
            }
        }

        [EventSubscribe("EndOfSimulation")]
        private void OnSimulationEnding(object sender, EventArgs e)
        {
            if (connection == null) { return; }

            connection.SendFrame("finished");
            var msg = connection.ReceiveFrameString();
            if (msg != "ok") { throw new Exception("Expected ok at end"); }
            connection.Close();

            Console.WriteLine("Simulation Ending");
        }
        static bool isNumeric(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
            }
            return false;
        }
        static bool isInteger(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return true;
            }
            return false;
        }
        static bool isString(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.String:
                    return true;
            }
            return false;
        }
    }
}
