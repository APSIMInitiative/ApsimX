using System;
using System.Text;
using System.IO;

namespace Utility
{
    //=========================================================================
    /// <summary>
    /// This is the configuration settings object.
    /// Add fields here to store extra settings for the application.
    /// </summary>
    [Serializable()]
    public class SessionSettings
    {
        public int mainformleft = 0;
        public int mainformtop = 0;
        public int mainformwidth = 640;
        public int mainformheight = 480;
        public int windowstate = 2;         //default to maximised
    }
    //=========================================================================
    /// <summary>
    /// Handle the reading and writing of the configuration settings file
    /// </summary>
    public class ConfigManager
    {
        public String Version = "1.0";  //this could be obtained from elsewhere in the application
        public SessionSettings Session;
        public String ConfigFile;
        public ConfigManager()
        {
            //determine the path to the config file 
            String datafile = String.Format("CSIRO{0}APSIMX{0}{1}{0}apsimx.xml", Path.DirectorySeparatorChar, Version);
            //On Linux and Mac the path will be .config/
            ConfigFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), datafile);
            //deserialise the file
            if (File.Exists(ConfigFile))
            {
                System.Xml.Serialization.XmlSerializer xmlreader = new System.Xml.Serialization.XmlSerializer(typeof(SessionSettings));
                StreamReader filereader = new StreamReader(ConfigFile);
                Session = new SessionSettings();
                Session = (SessionSettings)xmlreader.Deserialize(filereader);
                filereader.Close();
            }
            else
                Session = new SessionSettings();
        }
        /// <summary>
        /// Store the configuration settings to file
        /// </summary>
        public void StoreConfig()
        {
            String ConfigPath = Path.GetDirectoryName(ConfigFile);
            if (!Directory.Exists(ConfigPath))
                Directory.CreateDirectory(ConfigPath);
            StreamWriter filewriter = new StreamWriter(ConfigFile);
            System.Xml.Serialization.XmlSerializer xmlwriter = new System.Xml.Serialization.XmlSerializer(typeof(SessionSettings));
            xmlwriter.Serialize(filewriter, Session);
            filewriter.Close();
        }
    }
}
