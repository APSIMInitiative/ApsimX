using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Net;
using System.Net.Sockets;
using System.IO;

/***************************************************
 * This project is an attempt to try to get an Apsim
 * run working across multiple machines.
 *          *** EXPERIMENTAL ***
 *            NOT FOR RELEASE
 *  AUTHOR: Justin Fainges
 ***************************************************/

//NB: this is a one hit wonder right now. Needs to be restarted after every run.
namespace Models.ApServer
{
    public class Master
    {
        static Utility.JobManager manager = null;

        public static void StartMaster()
        {
            try
            {
                TcpListener listener = new TcpListener(IPAddress.Any, 50000);
                listener.Start();
                using (TcpClient client = listener.AcceptTcpClient())
                using (NetworkStream ns = client.GetStream())
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    manager = (Utility.JobManager)formatter.Deserialize(ns);
                }
                listener.Stop();
                
            }
            catch (Exception) { }
            manager.Start(true);
        }
    }
}