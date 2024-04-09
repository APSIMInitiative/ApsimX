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

        // reference irrigation objects
        [Link(Type = LinkType.Path, Path = "[Field1].Irrigation")] Irrigation Irrigation1;
        [Link(Type = LinkType.Path, Path = "[Field2].Irrigation")] Irrigation Irrigation2;

        // class to store irrigation data
        [Serializable]
        public class IrrigationData
        {
            public int Index { get; set; }
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
                if (command == "resume")
                {
                    moreToDo = false;

                }
                else if (command == "do" && msg.FrameCount >= 2)
                {
                    onDoCommand(msg);
                    connection.SendFrame("ok");

                }
                else if (command == "set" && msg.FrameCount == 3)
                {
                    // set an apsim variable. 
                    //  arg 1 is the variable path (eg "[Nutrient].NO3.kgha"), 
                    //  arg 2 is the packed object                
                    string variableName = MessagePackSerializer.Deserialize<string>(msg[1].Buffer);

                    // See what type the apsim variable is
                    var myType = simulation.Get(variableName).GetType();
                    //Console.WriteLine("Got set {0}, my type is {1}", variableName, myType);

                    object value = MessagePackSerializer.Deserialize<object>(msg[2].Buffer);
                    //Console.WriteLine("Got set {0} of incoming type {1}", variableName, value.GetType());
                    if (value.GetType().IsArray != myType.IsArray)
                        throw new Exception("Array/scalar mismatch for " + variableName);

                    if (myType.IsArray)
                    {
                        Type myElementType = myType.GetElementType();
                        Object[] values = (value as object[]);
                        if (isNumeric(myElementType))
                        {
                            var myValues = Array.ConvertAll(values, (e) => (double)e);
                            simulation.Set(variableName, myValues);
                        }
                        else if (isInteger(myElementType))
                        {
                            var myValues = Array.ConvertAll(values, (e) => (int)e);
                            simulation.Set(variableName, myValues);
                        }
                        else if (isString(myElementType))
                        {
                            var myValues = Array.ConvertAll(values, (e) => (string)e);
                            simulation.Set(variableName, myValues);
                        }
                        else
                        {
                            throw new Exception("Don't know what to do setting a " + myElementType + " variable");
                        }

                        //Type myElementType = myType.GetElementType();
                        //var values = Array.ConvertAll((object[])value, (e) => Convert.ChangeType(e, myElementType)) as System.Array;
                        //Console.WriteLine("Doing set {0} of type {1} ({2}) = ", variableName, 
                        //                  values.GetType(), values.GetType().GetElementType(),  
                        //                  values.GetValue(0).ToString());
                        //simulation.Set(variableName, values);
                    }
                    else
                    {
                        simulation.Set(variableName, Convert.ChangeType(value, myType));
                    }
                    connection.SendFrame("ok");

                }
                else if (command == "get" && msg.FrameCount == 2)
                {
                    string variableName = MessagePackSerializer.Deserialize<string>(msg[1].Buffer);
                    object value = simulation.Get(variableName);
                    if (value is IFunction function)
                        value = function.Value();
                    else if (value != null && (value.GetType().IsArray || value.GetType().IsClass))
                    {
                        try
                        {
                            value = ReflectionUtilities.Clone(value);
                        }
                        catch (Exception err)
                        {
                            throw new Exception($"Cannot report variable \"{variableName}\": Variable is a non-reportable type: \"{value?.GetType()?.Name}\".", err);
                        }
                    }
                    //Console.WriteLine("Got get '{0}' of type '{1}'", variableName, value?.GetType()); 

                    byte[] bytes;
                    if (value != null)
                    {
                        bytes = MessagePackSerializer.Serialize(value);
                    }
                    else
                    {
                        Console.WriteLine("Sending NA");
                        bytes = MessagePackSerializer.Serialize("NA"); // fixme. Probably a better way to do this
                    }

                    connection.SendFrame(bytes);
                }
                else
                {
                    throw new Exception("Expected resume/get/set, not '" + command + "'");
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
                        irig_data.Index = MessagePackSerializer.Deserialize<int>(msg[i + 1].Buffer);
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
            /*
          if (apply_irrigation) {
            Irrigation.Apply(irrigation_amount, willRunoff: true);
            Console.WriteLine("Field 1: Irrigated {0} ", irrigation_amount, "mm");
            apply_irrigation = false;
          }
          */
            while (irrigationQueue.Count > 0)
            {
                IrrigationData data = irrigationQueue.Dequeue();
                if (data.Index == 1)
                {
                    Irrigation1.Apply(data.IrrigationAmount, willRunoff: true);
                }
                else if (data.Index == 2)
                {
                    Irrigation2.Apply(data.IrrigationAmount, willRunoff: true);
                }
                else
                {
                    Console.WriteLine($"Field {data.Index} not implemented");
                }
                // Process the data
                Console.WriteLine($"Field {data.Index} irrigated with {data.IrrigationAmount} mm");
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