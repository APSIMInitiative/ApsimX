using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace SWIMFrame
{
    class Program
    {
        static void Main(string[] args)
        {
            string output = string.Empty;
            //define test soils
            SoilParam[] soils = new SoilParam[2];
            soils[0] = new SoilParam(10, 103, 0.4, 2.0, -2.0, -10.0, 1.0 / 3.0, 1.0);
            soils[1] = new SoilParam(10, 109, 0.6, 0.2, -2.0, -40.0, 1.0 / 9.0, 1.0);

            string[] ftname = new string[2];
            int[] sidx;
            int i, j;
            int[] ndz;
            double dzmin;
            double[] x;
            double[,] dz = new double[2, 10]; //only for testing? if not will need to change hardcoded dimensions.
            bool Kgiven = true;
            SoilProps sp1, sp2;
            FluxTable ft1, ft2;

            //define soil profile
            x = new double[] {10,20,30,40,60,80,100,120,160,200}; //length = num soil layers
            sidx = new int[] { 103, 103, 103, 103, 109, 109, 109, 109, 109, 109 }; //soil ident of layers
            dzmin = 1.0; // smallest likely path length
            ndz = new int[] { 2, 4 }; // for the two soil types - gives six flux tables
            //can be done in loops, but clearer this way and will only be used for testing
            dz[0, 0] = 5;
            dz[0, 1] = 10;
            dz[1, 0] = 10;
            dz[1, 1] = 20;
            dz[1, 2] = 30;
            dz[1, 4] = 40;
            for (i = 0; i < 1; i++) //should be 2, using the first one for debugging.
            {
                BinaryWriter b = new BinaryWriter(File.OpenWrite("soil" + soils[i].sid + ".dat"));
                MVG.Params(soils[i].sid, soils[i].ths, soils[i].ks, soils[i].he, soils[i].hd, soils[i].p, soils[i].hg, soils[i].em, soils[i].en);
                soils[i].sp = Soil.gensptbl(dzmin, soils[i], Kgiven);
                b.Write(soils[i].sid);
                WriteProps(b, soils[i].sp);
                b.Close();
                for (j = 0; j < ndz[i]; j++)
                {
                    Fluxes.FluxTable(dz[i, j], soils[i].sp);
                    b = new BinaryWriter(File.OpenWrite("soil" + soils[i].sid + "dz" + dz[i, j] * 10));
                    b.Write(soils[i].sid);
                 //   WriteFluxes(b, Fluxes.ft);
                    b.Close();
                }
            }
            Fluxes.WriteDiags();

            //generate and write composite flux table for path with two soil types
            sp1 = ReadProps("soil103.dat");
            sp2 = ReadProps("soil109.dat");

            ft1 = ReadFluxes("soil103dz50.dat");
            ft2 = ReadFluxes("soil103dz100.dat");

            FluxTable ftwo = TwoFluxes.TwoTables(ft1, sp1, ft2, sp2); 
            BinaryWriter bw = new BinaryWriter(File.OpenWrite("soil0103dz0050_soil0109dz0100.dat"));
//            WriteFluxes(bw, ftwo);
        }

        private static FluxTable ReadFluxes(string file)
        {
            FluxTable ft;
            using (Stream stream = File.Open(file, FileMode.Open))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                ft = (FluxTable)formatter.Deserialize(stream);
            }
            return ft;
        }

        private static SoilProps ReadProps(string file)
        {
            SoilProps sp;
            using (Stream stream = File.Open(file, FileMode.Open))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                sp = (SoilProps)formatter.Deserialize(stream);
            }
            return sp;
        }

        private static void WriteFluxes(BinaryWriter b, FluxTable fluxTable)
        {
            byte[] bytes;
            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream stream = new MemoryStream())
            {
                formatter.Serialize(stream, fluxTable);
                bytes = stream.ToArray();
            }
            b.Write(bytes);
        }

        private static void WriteProps(BinaryWriter b, SoilProps soilProps)
        {
            byte[] bytes;
            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream stream = new MemoryStream())
            {
                formatter.Serialize(stream, soilProps);
                bytes = stream.ToArray();
            }
            b.Write(bytes);
        }
    }
}
