using System;

namespace APSIM.Client
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                var client = new ApsimClient();
                client.Run();
                return 0;
            }
            catch (Exception err)
            {
                Console.Error.WriteLine(err.ToString());
                return 1;
            }
        }
    }
}
