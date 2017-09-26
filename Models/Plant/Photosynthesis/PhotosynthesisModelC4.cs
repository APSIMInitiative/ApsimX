using System;
using System.Collections.Generic;
using System.IO;
using C4MethodExtensions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.PMF.Phenology
{
    public class PhotosynthesisModelC4 : PhotosynthesisModel
    {

        public SunlitCanopy sunlitAC2;
        public ShadedCanopy shadedAC2;


        public PhotosynthesisModelC4() : base() { }

        //---------------------------------------------------------------------------
        public override void run(bool sendNotification, double swAvail = 0, double maxHourlyT = -1, double sunlitFraction = 0, double shadedFraction = 0)
        {
            if (!initialised)
            {
                return;
            }

            if (sendNotification && notifyStart != null)
            {
                notifyStart(false);
            }

            envModel.run(this.time);
            canopy.calcCanopyStructure(this.envModel.sunAngle.rad);


            sunlitAC1 = new SunlitCanopy(canopy.nLayers, SSType.AC1);
            sunlitAC2 = new SunlitCanopy(canopy.nLayers, SSType.AC2);
            sunlitAJ = new SunlitCanopy(canopy.nLayers, SSType.AJ);
            shadedAC1 = new ShadedCanopy(canopy.nLayers, SSType.AC1);
            shadedAC2 = new ShadedCanopy(canopy.nLayers, SSType.AC2);
            shadedAJ = new ShadedCanopy(canopy.nLayers, SSType.AJ);

            sunlitAC1.calcLAI(this.canopy, shadedAC1);
            sunlitAC2.calcLAI(this.canopy, shadedAC2);
            sunlitAJ.calcLAI(this.canopy, shadedAJ);
            shadedAC1.calcLAI(this.canopy, sunlitAC1);
            shadedAC2.calcLAI(this.canopy, sunlitAC2);
            shadedAJ.calcLAI(this.canopy, sunlitAJ);

            canopy.run(this, envModel);

            sunlitAC1.run(canopy.nLayers, this, shadedAC1);
            sunlitAC2.run(canopy.nLayers, this, shadedAC2);
            sunlitAJ.run(canopy.nLayers, this, shadedAJ);
            shadedAC1.run(canopy.nLayers, this, sunlitAC1);
            shadedAC2.run(canopy.nLayers, this, sunlitAC2);
            shadedAJ.run(canopy.nLayers, this, sunlitAJ);

            TranspirationMode mode = TranspirationMode.unlimited;

            if (maxHourlyT != -1)
            {
                mode = TranspirationMode.limited;
            }

            bool useAirTemp = false;

            double defaultCm = 160;

            List<bool> results = new List<bool>();

            results.Add(sunlitAC1.calcPhotosynthesis(this, useAirTemp, 0, envModel.getTemp(time), defaultCm, mode, maxHourlyT, sunlitFraction));
            results.Add(sunlitAC2.calcPhotosynthesis(this, useAirTemp, 0, envModel.getTemp(time), defaultCm, mode, maxHourlyT, sunlitFraction));
            results.Add(sunlitAJ.calcPhotosynthesis(this, useAirTemp, 0, envModel.getTemp(time), defaultCm, mode, maxHourlyT, sunlitFraction));
            results.Add(shadedAC1.calcPhotosynthesis(this, useAirTemp, 0, envModel.getTemp(time), defaultCm, mode, maxHourlyT, shadedFraction));
            results.Add(shadedAC2.calcPhotosynthesis(this, useAirTemp, 0, envModel.getTemp(time), defaultCm, mode, maxHourlyT, shadedFraction));
            results.Add(shadedAJ.calcPhotosynthesis(this, useAirTemp, 0, envModel.getTemp(time), defaultCm, mode, maxHourlyT, shadedFraction));

            count = 1;

            bool caughtError = false;

            while (results.Contains(false) && !caughtError)
            {
                results.Clear();
                results.Add(sunlitAC1.calcPhotosynthesis(this, useAirTemp, 0, sunlitAC1.leafTemp[0], sunlitAC1.Cm[0], mode, maxHourlyT, sunlitFraction));
                results.Add(sunlitAC2.calcPhotosynthesis(this, useAirTemp, 0, sunlitAC2.leafTemp[0], sunlitAC2.Cm[0], mode, maxHourlyT, sunlitFraction));
                results.Add(sunlitAJ.calcPhotosynthesis(this, useAirTemp, 0, sunlitAJ.leafTemp[0], sunlitAJ.Cm[0], mode, maxHourlyT, sunlitFraction));
                results.Add(shadedAC1.calcPhotosynthesis(this, useAirTemp, 0, shadedAC1.leafTemp[0], shadedAC1.Cm[0], mode, maxHourlyT, shadedFraction));
                results.Add(shadedAC2.calcPhotosynthesis(this, useAirTemp, 0, shadedAC2.leafTemp[0], shadedAC2.Cm[0], mode, maxHourlyT, shadedFraction));
                results.Add(shadedAJ.calcPhotosynthesis(this, useAirTemp, 0, shadedAJ.leafTemp[0], shadedAJ.Cm[0], mode, maxHourlyT, shadedFraction));

                if (double.IsNaN(sunlitAC1.Cm[0]) ||
                    double.IsNaN(sunlitAC2.Cm[0]) ||
                    double.IsNaN(sunlitAJ.Cm[0]) ||
                    double.IsNaN(shadedAC1.Cm[0]) ||
                    double.IsNaN(shadedAC2.Cm[0]) ||
                    double.IsNaN(shadedAJ.Cm[0]) ||
                    count == 30)
                {
                    sunlitAC1.Cm[0] = defaultCm;
                    sunlitAC2.Cm[0] = defaultCm;
                    sunlitAJ.Cm[0] = defaultCm;
                    shadedAC1.Cm[0] = defaultCm;
                    shadedAC2.Cm[0] = defaultCm;
                    shadedAJ.Cm[0] = defaultCm;

                    useAirTemp = true;
                }

                count++;

                if (count > 100 && !caughtError)
                {
                    StreamWriter sr = new StreamWriter("scenario.csv");

                    sr.WriteLine("Lat," + envModel.latitudeD);
                    sr.WriteLine("DOY," + envModel.DOY);
                    sr.WriteLine("Maxt," + envModel.maxT);
                    sr.WriteLine("AvailableWater," + swAvail);
                    sr.WriteLine("Mint," + envModel.minT);
                    sr.WriteLine("Radn," + envModel.radn);
                    sr.WriteLine("Ratio," + envModel.atmTransmissionRatio);
                    sr.WriteLine("LAI," + canopy.LAI);
                    sr.WriteLine("SLN," + canopy.CPath.SLNAv);

                    List<double> temps = new List<double>();

                    for (int i = 0; i <= 12; i++)
                    {
                        temps.Add(envModel.getTemp(i + 5));
                    }

                    sr.WriteLine("Temps," + String.Join(",", temps.ToArray()));

                    sr.Flush();

                    sr.Close();

                    caughtError = true;
                }
            }

            canopy.calcCanopyBiomassAccumulation(this);

            if (sendNotification && notifyFinish != null)
            {
                notifyFinish();
            }
        }
    }
}
