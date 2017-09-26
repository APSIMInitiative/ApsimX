using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using C3MethodExtensions;

namespace Models.PMF.Phenology
{
    public class PhotosynthesisModelC3 : PhotosynthesisModel
    {
        public PhotosynthesisModelC3() : base() { }

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
            sunlitAJ = new SunlitCanopy(canopy.nLayers, SSType.AJ);
            shadedAC1 = new ShadedCanopy(canopy.nLayers, SSType.AC1);
            shadedAJ = new ShadedCanopy(canopy.nLayers, SSType.AJ);

            sunlitAC1.calcLAI(this.canopy, shadedAC1);
            sunlitAJ.calcLAI(this.canopy, shadedAJ);
            shadedAC1.calcLAI(this.canopy, sunlitAC1);
            shadedAJ.calcLAI(this.canopy, sunlitAJ);

            canopy.run(this, envModel);

            sunlitAC1.run(canopy.nLayers, this, shadedAC1);
            sunlitAJ.run(canopy.nLayers, this, shadedAJ);
            shadedAC1.run(canopy.nLayers, this, sunlitAC1);
            shadedAJ.run(canopy.nLayers, this, sunlitAJ);

            TranspirationMode mode = TranspirationMode.unlimited;

            if (maxHourlyT != -1)
            {
                mode = TranspirationMode.limited;
            }

            bool useAirTemp = false;

            List<bool> results = new List<bool>();

            results.Add(sunlitAC1.calcPhotosynthesis(this, useAirTemp, 0, envModel.getTemp(time), mode, maxHourlyT, sunlitFraction));
            results.Add(sunlitAJ.calcPhotosynthesis(this, useAirTemp, 0, envModel.getTemp(time), mode, maxHourlyT, sunlitFraction));
            results.Add(shadedAC1.calcPhotosynthesis(this, useAirTemp, 0, envModel.getTemp(time), mode, maxHourlyT, shadedFraction));
            results.Add(shadedAJ.calcPhotosynthesis(this, useAirTemp, 0, envModel.getTemp(time), mode, maxHourlyT, shadedFraction));

            count = 1;

            bool caughtError = false;

            while (results.Contains(false) && !caughtError)
            {
                results.Clear();
                results.Add(sunlitAC1.calcPhotosynthesis(this, useAirTemp, 0, sunlitAC1.leafTemp[0], mode, maxHourlyT, sunlitFraction));
                results.Add(sunlitAJ.calcPhotosynthesis(this, useAirTemp, 0, sunlitAJ.leafTemp[0], mode, maxHourlyT, sunlitFraction));
                results.Add(shadedAC1.calcPhotosynthesis(this, useAirTemp, 0, shadedAC1.leafTemp[0], mode, maxHourlyT, shadedFraction));
                results.Add(shadedAJ.calcPhotosynthesis(this, useAirTemp, 0, shadedAJ.leafTemp[0], mode, maxHourlyT, shadedFraction));

                count++;

                if (count > 100 && !caughtError)
                {
                    //writeScenario(swAvail);
                    caughtError = true;
                }
            }

            // canopy.calcCanopyBiomassAccumulation(this);

            if (sendNotification && notifyFinish != null)
            {
                notifyFinish();
            }
           // writeScenario(swAvail);
        }

        public void writeScenario(double swAvail)
        {
            StreamWriter sr = new StreamWriter("scenario.csv",true);

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
                temps.Add(envModel.getTemp(i + 6));
            }

            sr.WriteLine("Temps," + String.Join(",", temps.ToArray()));

            sr.Flush();

            sr.Close();
        }
    }
}
