using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.PMF.Photosynthesis
{

    // Interface declaration.
/// <summary>
/// 
/// </summary>
    public interface IApsimC4PhotoLink
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="DOY"></param>
        /// <param name="latitude"></param>
        /// <param name="maxT"></param>
        /// <param name="minT"></param>
        /// <param name="radn"></param>
        /// <param name="lai"></param>
        /// <param name="SLN"></param>
        /// <param name="soilWaterAvail"></param>
        /// <param name="B"></param>
        /// <param name="RootShootRatio"></param>
        /// <param name="LeafAngle"></param>
        /// <param name="SLNRatioTop"></param>
        /// <param name="psiVc"></param>
        /// <param name="psiJ"></param>
        /// <param name="psiRd"></param>
        /// <param name="psiVp"></param>
        /// <param name="psiFactor"></param>
        /// <param name="Ca"></param>
        /// <param name="gbs"></param>
        /// <param name="gm25"></param>
        /// <param name="Vpr"></param>
        /// <returns></returns>
        double[] calc(int DOY, double latitude, double maxT, double minT, double radn, double lai, double SLN, double soilWaterAvail,
            double B, double RootShootRatio, double LeafAngle, double SLNRatioTop, double psiVc, double psiJ, double psiRd, double psiVp, 
            double psiFactor, double Ca, double gbs, double gm25, double Vpr);
    };

    // Interface implementation.
    /// <summary></summary>
    public class ApsimC4PhotoLink : IApsimC4PhotoLink
    {
        /// <summary>
        /// 
        /// </summary>
        public void calc() { } //deliberately empty 
        /// <summary>
        /// 
        /// </summary>
        /// <param name="DOY"></param>
        /// <param name="latitude"></param>
        /// <param name="maxT"></param>
        /// <param name="minT"></param>
        /// <param name="radn"></param>
        /// <param name="lai"></param>
        /// <param name="SLN"></param>
        /// <param name="soilWaterAvail"></param>
        /// <param name="B"></param>
        /// <param name="RootShootRatio"></param>
        /// <param name="LeafAngle"></param>
        /// <param name="SLNRatioTop"></param>
        /// <param name="psiVc"></param>
        /// <param name="psiJ"></param>
        /// <param name="psiRd"></param>
        /// <param name="psiVp"></param>
        /// <param name="psiFactor"></param>
        /// <param name="Ca"></param>
        /// <param name="gbs"></param>
        /// <param name="gm25"></param>
        /// <param name="Vpr"></param>
        /// <returns></returns>
        public double[] calc(int DOY, double latitude, double maxT, double minT, double radn, double lai, double SLN, double soilWaterAvail,
            double B, double RootShootRatio, double LeafAngle, double SLNRatioTop, double psiVc, double psiJ, double psiRd, double psiVp, 
            double psiFactor, double Ca, double gbs, double gm25, double Vpr) //0 = simple conductance
        {

            Models.PMF.Photosynthesis.PhotosynthesisModelC4 PM = new Models.PMF.Photosynthesis.PhotosynthesisModelC4();
            PM.initialised = false;
            PM.photoPathway = Models.PMF.Photosynthesis.PhotosynthesisModel.PhotoPathway.C4;

            PM.conductanceModel = Models.PMF.Photosynthesis.PhotosynthesisModel.ConductanceModel.SIMPLE;
            PM.electronTransportModel = Models.PMF.Photosynthesis.PhotosynthesisModel.ElectronTransportModel.EMPIRICAL;

            PM.canopy.nLayers = 1;

            PM.envModel.latitudeD = latitude;
            PM.envModel.DOY = DOY;
            PM.envModel.maxT = maxT;
            PM.envModel.minT = minT;
            PM.envModel.radn = radn;  // Check that this changes ratio
            PM.envModel.ATM = 1.013;

            PM.canopy.LAI = lai;
            PM.canopy.leafAngle = LeafAngle;
            PM.canopy.leafWidth = 0.05;
            PM.canopy.u0 = 1;
            PM.canopy.ku = 0.5;

            PM.canopy.CPath.SLNAv = SLN;
            PM.canopy.CPath.SLNRatioTop = SLNRatioTop;
            PM.canopy.CPath.structuralN = 14;

            PM.canopy.CPath.psiVc = psiVc * psiFactor;
            PM.canopy.CPath.psiJ = psiJ * psiFactor;
            PM.canopy.CPath.psiRd = psiRd * psiFactor;
            PM.canopy.CPath.psiVp = psiVp * psiFactor;

            PM.canopy.rcp = 1200;
            PM.canopy.g = 0.066;
            PM.canopy.sigma = 5.668E-08;
            PM.canopy.lambda = 2447000;

            PM.canopy.θ = 0.7;
            PM.canopy.f = 0.15;
            PM.canopy.oxygenPartialPressure = 210000;
            PM.canopy.Ca = Ca;

            PM.canopy.gbs_CO2 = 0.003;
            PM.canopy.alpha = 0.1;
            PM.canopy.x = 0.4;
            
            PM.canopy.diffuseExtCoeff = 0.8;
            PM.canopy.leafScatteringCoeff = 0.2;
            PM.canopy.diffuseReflectionCoeff = 0.057;

            PM.canopy.diffuseExtCoeffNIR = 0.8;
            PM.canopy.leafScatteringCoeffNIR = 0.8;
            PM.canopy.diffuseReflectionCoeffNIR = 0.389;

            PM.canopy.CPath.Kc_P25 = 1210;
            PM.canopy.CPath.Kc_c = 25.899;
            PM.canopy.CPath.Kc_b = 7721.915;
            PM.canopy.CPath.Ko_P25 = 292000;
            PM.canopy.CPath.Ko_c = 4.236;
            PM.canopy.CPath.Ko_b = 1262.93;
            PM.canopy.CPath.VcMax_VoMax_P25 = 5.401;
            PM.canopy.CPath.VcMax_VoMax_c = 9.126;
            PM.canopy.CPath.VcMax_VoMax_b = 2719.478;
            PM.canopy.CPath.VcMax_c = 31.467;
            PM.canopy.CPath.VcMax_b = 9381.766;
            PM.canopy.CPath.Rd_c = 0;
            PM.canopy.CPath.Rd_b = 0;
            PM.canopy.CPath.Kp_P25 = 139;
            PM.canopy.CPath.Kp_c = 14.644;
            PM.canopy.CPath.Kp_b = 4366.129;
            PM.canopy.CPath.VpMax_c = 38.244;
            PM.canopy.CPath.VpMax_b = 11402.45;

            PM.canopy.CPath.JMax_TOpt = 32.633;
            PM.canopy.CPath.JMax_Omega = 15.27;
            PM.canopy.CPath.gm_TOpt = 34.309;
            PM.canopy.CPath.gm_Omega = 20.791;

            PM.canopy.gbs_CO2 = gbs;
            PM.canopy.CPath.gm_P25 = gm25;
            PM.canopy.Vpr_l = Vpr;

            PM.envModel.initilised = true;
            PM.envModel.run();

            PM.initialised = true;

            List<double> sunlitWaterDemands = new List<double>();
            List<double> shadedWaterDemands = new List<double>();
            List<double> hourlyWaterDemandsmm = new List<double>();
            List<double> hourlyWaterSuppliesmm = new List<double>();
            List<double> sunlitAssimilations = new List<double>();
            List<double> shadedAssimilations = new List<double>();
            List<double> interceptedRadn = new List<double>();

            for (int time = 6; time <= 18; time++)
            {
                //This run is to get potential water use

                if (time > PM.envModel.sunrise && time < PM.envModel.sunset)
                {  
                    PM.run(time, soilWaterAvail);
                    sunlitWaterDemands.Add(Math.Min(Math.Min(PM.sunlitAC1.Elambda_[0], PM.sunlitAC2.Elambda_[0]), PM.sunlitAJ.Elambda_[0]));
                    shadedWaterDemands.Add(Math.Min(Math.Min(PM.shadedAC1.Elambda_[0], PM.shadedAC2.Elambda_[0]), PM.shadedAJ.Elambda_[0]));

                    sunlitWaterDemands[sunlitWaterDemands.Count - 1] = Math.Max(sunlitWaterDemands.Last(), 0);
                    shadedWaterDemands[shadedWaterDemands.Count - 1] = Math.Max(shadedWaterDemands.Last(), 0);

                    hourlyWaterDemandsmm.Add((sunlitWaterDemands.Last() + shadedWaterDemands.Last()) / PM.canopy.lambda * 1000 * 0.001 * 3600);
                    hourlyWaterSuppliesmm.Add(hourlyWaterDemandsmm.Last());
                }
                else
                {
                    sunlitWaterDemands.Add(0);
                    shadedWaterDemands.Add(0);
                    hourlyWaterDemandsmm.Add(0);
                    hourlyWaterSuppliesmm.Add(0);
                }

                sunlitAssimilations.Add(0);
                shadedAssimilations.Add(0);
            }

            double maxHourlyT = hourlyWaterSuppliesmm.Max();

            while (hourlyWaterSuppliesmm.Sum() > soilWaterAvail)
            {
                maxHourlyT *= 0.99;
                for (int i = 0; i < hourlyWaterSuppliesmm.Count; i++)
                {
                    if (hourlyWaterSuppliesmm[i] > maxHourlyT)
                    {
                        hourlyWaterSuppliesmm[i] = maxHourlyT;
                    }
                }
            }


            sunlitAssimilations.Clear();
            shadedAssimilations.Clear();


            //Now that we have our hourly supplies we can calculate again
            for (int time = 6; time <= 18; time++)
            {
                double TSupply = hourlyWaterSuppliesmm[time - 6];
                double sunlitWaterDemand = sunlitWaterDemands[time - 6];
                double shadedWaterDemand = shadedWaterDemands[time - 6];

                double totalWaterDemand = sunlitWaterDemand + shadedWaterDemand;

                if (time > PM.envModel.sunrise && time < PM.envModel.sunset)
                {
                    PM.run(time, soilWaterAvail, hourlyWaterSuppliesmm[time - 6], sunlitWaterDemand / totalWaterDemand, shadedWaterDemand / totalWaterDemand);
                    sunlitAssimilations.Add(Math.Min(Math.Min(PM.sunlitAC1.A[0], PM.sunlitAC2.A[0]), PM.sunlitAJ.A[0]));
                    shadedAssimilations.Add(Math.Min(Math.Min(PM.shadedAC1.A[0], PM.shadedAC2.A[0]), PM.shadedAJ.A[0]));


                    sunlitAssimilations[sunlitAssimilations.Count - 1] = Math.Max(sunlitAssimilations.Last(), 0);
                    shadedAssimilations[shadedAssimilations.Count - 1] = Math.Max(shadedAssimilations.Last(), 0);

                    double propIntRadn = PM.canopy.propnInterceptedRadns.Sum();
                    interceptedRadn.Add(PM.envModel.totalIncidentRadiation * propIntRadn * 3600);
                    interceptedRadn[interceptedRadn.Count - 1] = Math.Max(interceptedRadn.Last(), 0);
                }
                else
                {
                    sunlitAssimilations.Add(0);
                    shadedAssimilations.Add(0);
                    interceptedRadn.Add(0);
                }
            }
            double[] results = new double[4];

            results[0] = (sunlitAssimilations.Sum() + shadedAssimilations.Sum()) * 3600 / 1000000 * 44 * B * 100 / ((1 + RootShootRatio) * 100);
            results[1] = hourlyWaterDemandsmm.Sum();
            results[2] = hourlyWaterSuppliesmm.Sum();
            results[3] = interceptedRadn.Sum();

            return results;
        }
    }
}
