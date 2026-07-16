using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using APSIM.Core;
using APSIM.Numerics;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;

namespace Models
{
    /// <summary>
    /// GenericFruitTree-specific microclimate entry point.
    /// Inherits from MicroClimate so existing type-based links continue to resolve.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Simulation))]
    [ValidParent(ParentType = typeof(Zone))]
    public class PerennialMicroClimate : MicroClimate
    {
        [Link]
        private IClock clock = null;

        [Link]
        private IWeather weather = null;

        [Link]
        private ISoilWater soilWater = null;

        [Link]
        private ICalculateEo eoCalculator = null;

        private const double sunSetAngle = 0.0;
        private const double sunAngleNetPositiveRadiation = 15;

        private List<PerennialMicroClimateZone> perennialZones = new();

        private double dayLengthEvap;
        private double dayLengthLight;

        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            if (ReferenceHeight < 1 || ReferenceHeight > 10)
                throw new Exception($"Error in microclimate: reference height must be between 1 and 10. Actual value is {ReferenceHeight}");

            perennialZones = new List<PerennialMicroClimateZone>();
            foreach (Zone newZone in Node.FindChildren<Zone>(recurse: true, relativeTo: Parent as INodeModel))
                perennialZones.Add(new PerennialMicroClimateZone(clock, newZone, Node, MinimumHeightDiffForNewLayer));

            if (perennialZones.Count == 0)
                perennialZones.Add(new PerennialMicroClimateZone(clock, this.Parent as Zone, Node, MinimumHeightDiffForNewLayer));

            SyncBaseZoneCache();
        }

        [EventSubscribe("DoEnergyArbitration")]
        private void DoEnergyArbitration(object sender, EventArgs e)
        {
            perennialZones.ForEach(zone => zone.DailyInitialise(weather));

            dayLengthLight = MathUtilities.DayLength(clock.Today.DayOfYear, sunSetAngle, weather.Latitude);
            dayLengthEvap = MathUtilities.DayLength(clock.Today.DayOfYear, sunAngleNetPositiveRadiation, weather.Latitude);
            dayLengthEvap = Math.Max(dayLengthEvap, (dayLengthLight * 2.0 / 3.0));

            if (perennialZones.Count == 2 && perennialZones[0].Zone is Zones.RectangularZone && perennialZones[1].Zone is Zones.RectangularZone)
            {
                perennialZones[0].DoCanopyCompartments();
                perennialZones[1].DoCanopyCompartments();
                CalculateStripZoneShortWaveRadiation();
            }
            else
            {
                foreach (PerennialMicroClimateZone zoneMC in perennialZones)
                {
                    zoneMC.DoCanopyCompartments();
                    CalculateLayeredShortWaveRadiation(zoneMC, weather.Radn);
                }
            }

            foreach (var zoneMC in perennialZones)
            {
                zoneMC.CalculateEnergyTerms(soilWater.Salb);
                zoneMC.CalculateLongWaveRadiation(dayLengthLight, dayLengthEvap);
                zoneMC.CalculateSoilHeatRadiation(SoilHeatFluxFraction);
                zoneMC.CalculateGc(dayLengthEvap);
                zoneMC.CalculateGa(ReferenceHeight);
                zoneMC.CalculateInterception(a_interception, b_interception, c_interception, d_interception);
                zoneMC.CalculatePM(dayLengthEvap, NightInterceptionFraction);
                zoneMC.CalculateOmega();
                zoneMC.SetCanopyEnergyTerms();
                zoneMC.SoilWater.Eo = eoCalculator.Calculate(zoneMC);
            }
        }

        private void SyncBaseZoneCache()
        {
            FieldInfo field = typeof(MicroClimate).GetField("microClimatesZones", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
                field.SetValue(this, perennialZones.Cast<MicroClimateZone>().ToList());
        }

        ///<summary> Calculate the short wave radiation balance for strip crop system</summary>
        private void CalculateStripZoneShortWaveRadiation()
        {
            PerennialMicroClimateZone tallest;
            PerennialMicroClimateZone shortest;
            if (perennialZones[0].DeltaZ.Sum() > perennialZones[1].DeltaZ.Sum())
            {
                tallest = perennialZones[0];
                shortest = perennialZones[1];
            }
            else
            {
                tallest = perennialZones[1];
                shortest = perennialZones[0];
            }

            bool tallestIsTree = false;
            bool tallestIsVine = false;
            double wt = (tallest.Zone as Zones.RectangularZone).Width * 1000;
            foreach (MicroClimateCanopy c in tallest.Canopies)
            {
                if (c.Canopy.CanopyType == "STRUM")
                    tallestIsTree = true;

                if ((c.Canopy.Height - c.Canopy.Depth) > 0 && c.Canopy.Width <= wt)
                    tallestIsVine = true;
            }

            if (tallestIsTree)
                DoTreeRowCropShortWaveRadiation(ref tallest, ref shortest);
            else if (tallestIsVine)
                DoVineStripShortWaveRadiation(ref tallest, ref shortest);
            else
                DoStripCropShortWaveRadiation(ref tallest, ref shortest);
        }

        /// <summary>
        /// Tree rows with no vertical overlap and possible horizontal overlap.
        /// </summary>
        private void DoTreeRowCropShortWaveRadiation(ref PerennialMicroClimateZone treeZone, ref PerennialMicroClimateZone alleyZone)
        {
            if (treeZone.DeltaZ.Sum() > 0 || alleyZone.DeltaZ.Sum() > 0)
            {
                double ht = treeZone.DeltaZ.Sum();
                double wt = (treeZone.Zone as Zones.RectangularZone).Width;
                double wa = (alleyZone.Zone as Zones.RectangularZone).Width;
                double cdt = 0;
                double cwt = 0;
                foreach (MicroClimateCanopy c in treeZone.Canopies)
                    if (c.Canopy.Depth < c.Canopy.Height)
                    {
                        if (cdt > 0.0)
                            throw new Exception("Can't have two tree canopies");
                        cdt = c.Canopy.Depth / 1000;
                        cwt = Math.Min(c.Canopy.Width / 1000, (wt + wa));
                    }

                double cbht = ht - cdt;
                double ha = alleyZone.DeltaZ.Sum();
                if ((ha > cbht) & (treeZone.DeltaZ.Length > 1))
                    throw new Exception("Height of the alley canopy must not exceed the base height of the tree canopy");

                double waOl = Math.Min(cwt - wt, wa);
                double waOp = wa - waOl;
                double ft = cwt / (wt + wa);
                double fa = waOp / (wt + wa);
                double lait = treeZone.LAItotsum.Sum();
                double laia = alleyZone.LAItotsum.Sum();
                double kt = treeZone.layerKtot[treeZone.layerKtot.Length - 1];
                double ka = 0;
                if (alleyZone.layerKtot.Length != 0)
                    ka = alleyZone.layerKtot[0];

                double laithomo = ft * lait;
                double ftbla = (Math.Sqrt(Math.Pow(cdt, 2) + Math.Pow(cwt, 2)) - cdt) / cwt;
                double fabla = (Math.Sqrt(Math.Pow(cdt, 2) + Math.Pow(waOp, 2)) - cdt) / waOp;
                if (waOp == 0)
                    fabla = 0;

                double tt = ft * (ftbla * Math.Exp(-kt * lait)
                          + ft * (1 - ftbla) * Math.Exp(-kt * laithomo))
                          + fa * ft * (1 - fabla) * Math.Exp(-kt * laithomo);
                double ta = fa * (fabla + fa * (1 - fabla) * Math.Exp(-kt * laithomo))
                          + ft * fa * ((1 - ftbla) * Math.Exp(-kt * laithomo));
                double it = 1 - tt - ta;
                double st = tt * wt / cwt;
                double iaOl = tt * waOl / cwt * (1 - Math.Exp(-ka * laia));
                double iaOp = ta * (1 - Math.Exp(-ka * laia));
                double ia = iaOl + iaOp;
                double saOl = tt * waOl / cwt * (Math.Exp(-ka * laia));
                double saOp = ta * (Math.Exp(-ka * laia));
                double sa = saOl + saOp;
                double balance = it + st + ia + sa;

                if (Math.Abs(1 - balance) > 0.001)
                    throw new Exception("Energy Balance not maintained in strip crop light interception model");

                ft = wt / (wt + wa);
                fa = wa / (wt + wa);

                double rint = 0;
                double rin = weather.Radn * it / ft;
                for (int i = treeZone.numLayers - 1; i >= 0; i--)
                {
                    if (double.IsNaN(rint))
                        throw new Exception("Bad Radiation Value in Light partitioning");
                    rint = rin;
                    for (int j = 0; j <= treeZone.Canopies.Count - 1; j++)
                        treeZone.Canopies[j].Rs[i] = rint * MathUtilities.Divide(treeZone.Canopies[j].Ftot[i] * treeZone.Canopies[j].Ktot, treeZone.layerKtot[i], 0.0);
                    rin -= rint;
                }
                treeZone.SurfaceRs = weather.Radn * st / ft;

                rint = 0;
                rin = weather.Radn * ia / fa;
                for (int i = alleyZone.numLayers - 1; i >= 0; i--)
                {
                    if (double.IsNaN(rint))
                        throw new Exception("Bad Radiation Value in Light partitioning");
                    rint = rin;
                    for (int j = 0; j <= alleyZone.Canopies.Count - 1; j++)
                        alleyZone.Canopies[j].Rs[i] = rint * MathUtilities.Divide(alleyZone.Canopies[j].Ftot[i] * alleyZone.Canopies[j].Ktot, alleyZone.layerKtot[i], 0.0);
                    rin -= rint;
                }
                alleyZone.SurfaceRs = weather.Radn * sa / fa;
            }
            else
            {
                treeZone.SurfaceRs = weather.Radn;
                CalculateLayeredShortWaveRadiation(alleyZone, weather.Radn);
            }
        }

        /// <summary>
        /// Strip crops with vertical overlap and no horizontal overlap.
        /// </summary>
        private void DoStripCropShortWaveRadiation(ref PerennialMicroClimateZone tallest, ref PerennialMicroClimateZone shortest)
        {
            if (tallest.DeltaZ.Sum() > 0)
            {
                double ht = tallest.DeltaZ.Sum();
                double hs = shortest.DeltaZ.Sum();
                double wt = (tallest.Zone as Zones.RectangularZone).Width;
                double ws = (shortest.Zone as Zones.RectangularZone).Width;
                double ft = wt / (wt + ws);
                double fs = ws / (wt + ws);
                double lait = tallest.LAItotsum.Sum();
                double lais = shortest.LAItotsum.Sum();
                double kt = 0;
                if (tallest.Canopies.Count > 0)
                    kt = tallest.Canopies[0].Ktot;
                double ks = 0;
                if (shortest.Canopies.Count > 0)
                    ks = shortest.Canopies[0].Ktot;

                double httop = ht - hs;
                double laittop = httop / ht * lait;
                double laitbot = lait - laittop;
                double laittophomo = ft * laittop;
                double ftblack = (Math.Sqrt(Math.Pow(httop, 2) + Math.Pow(wt, 2)) - httop) / wt;
                double fsblack = (Math.Sqrt(Math.Pow(httop, 2) + Math.Pow(ws, 2)) - httop) / ws;
                double tt = ft * (ftblack * Math.Exp(-kt * laittop)
                          + ft * (1 - ftblack) * Math.Exp(-kt * laittophomo))
                          + fs * ft * (1 - fsblack) * Math.Exp(-kt * laittophomo);
                double ts = fs * (fsblack + fs * (1 - fsblack) * Math.Exp(-kt * laittophomo))
                          + ft * fs * ((1 - ftblack) * Math.Exp(-kt * laittophomo));
                double intttop = 1 - tt - ts;
                double inttbot = tt * (1 - Math.Exp(-kt * laitbot));
                double soilt = tt * Math.Exp(-kt * laitbot);
                double ints = ts * (1 - Math.Exp(-ks * lais));
                double soils = ts * Math.Exp(-ks * lais);
                double balance = intttop + inttbot + soilt + ints + soils;
                if (Math.Abs(1 - balance) > 0.001)
                    throw new Exception("Energy Balance not maintained in strip crop light interception model");

                if (tallest.Canopies.Count > 0)
                    tallest.Canopies[0].Rs[0] = weather.Radn * (intttop + inttbot) / ft;
                tallest.SurfaceRs = weather.Radn * soilt / ft;

                if (shortest.Canopies.Count > 0 && shortest.Canopies[0].Rs != null)
                    if (shortest.Canopies[0].Rs.Length > 0)
                        shortest.Canopies[0].Rs[0] = weather.Radn * ints / fs;
                shortest.SurfaceRs = weather.Radn * soils / fs;
            }
            else
            {
                tallest.SurfaceRs = weather.Radn;
                shortest.SurfaceRs = weather.Radn;
            }
        }

        /// <summary>
        /// Vine strips with no vertical overlap and no horizontal overlap.
        /// </summary>
        private void DoVineStripShortWaveRadiation(ref PerennialMicroClimateZone vine, ref PerennialMicroClimateZone alley)
        {
            if (vine.DeltaZ.Sum() > 0)
            {
                double ht = vine.DeltaZ.Sum();
                double cdt = vine.Canopies[0].Canopy.Depth / 1000;
                double ha = alley.DeltaZ.Sum();
                double wt = (vine.Zone as Zones.RectangularZone).Width;
                double wa = (alley.Zone as Zones.RectangularZone).Width;
                double cwt = vine.Canopies[0].Canopy.Width / 1000;

                double waOp = wa + wt - cwt;
                double ft = cwt / (wt + wa);
                double fs = waOp / (wt + wa);

                double lait = vine.LAItotsum.Sum() * wt / cwt;
                double lais = alley.LAItotsum.Sum() * wa / waOp;

                double kt = 0;
                if (vine.Canopies.Count > 0 & lait > 0)
                    kt = vine.Canopies[0].Ktot;
                double ka = 0;
                if (alley.Canopies.Count > 0 & lais > 0)
                    ka = alley.Canopies[0].Ktot;

                double httop = ht - ha;
                double laithomo = ft * lait;

                double fhomo = 1 - Math.Exp(-kt * laithomo);
                double fcompr = (1 - Math.Exp(-kt * lait)) * cwt / waOp;

                double ipblackt = (Math.Sqrt(Math.Pow(cdt, 2) + Math.Pow(waOp, 2)) - cdt) / waOp;
                double irblackt = (Math.Sqrt(Math.Pow(cdt, 2) + Math.Pow(cwt, 2)) - cdt) / cwt;

                double spt = ipblackt + (1 - ipblackt) * Math.Exp(-kt * laithomo);
                double srt = irblackt * Math.Exp(-kt * lait) + (1 - irblackt) * Math.Exp(-kt * laithomo);
                double w = 0;
                if (vine.Canopies.Count > 0 & lait > 0 & kt > 0)
                    w = (spt - srt) / (1 - Math.Exp(-kt * lait));

                double ftop = fhomo * (1 - w) + fcompr * w;

                double ipblackb = (Math.Sqrt(Math.Pow(httop, 2) + Math.Pow(waOp, 2)) - httop) / waOp;
                double irblackb = (Math.Sqrt(Math.Pow(httop, 2) + Math.Pow(cwt, 2)) - httop) / cwt;

                double spb = ipblackb + (1 - ipblackb) * Math.Exp(-kt * laithomo);
                double srb = irblackb * Math.Exp(-kt * lait) + (1 - irblackb) * Math.Exp(-kt * laithomo);

                double soilt = srb * ft;
                double intttop = ftop;
                double ints = spb * (1 - Math.Exp(-ka * lais)) * fs;
                double soils = spb * Math.Exp(-ka * lais) * fs;

                ft = wt / (wt + wa);
                fs = wa / (wt + wa);

                double rint = 0;
                double rin = weather.Radn * intttop / ft;
                for (int i = vine.numLayers - 1; i >= 0; i--)
                {
                    if (double.IsNaN(rint))
                        throw new Exception("Bad Radiation Value in Light partitioning");
                    rint = rin;
                    for (int j = 0; j <= vine.Canopies.Count - 1; j++)
                        vine.Canopies[j].Rs[i] = rint * MathUtilities.Divide(vine.Canopies[j].Ftot[i] * vine.Canopies[j].Ktot, vine.layerKtot[i], 0.0);
                    rin -= rint;
                }
                vine.SurfaceRs = weather.Radn * soilt / ft;

                rint = 0;
                rin = weather.Radn * ints / fs;
                for (int i = alley.numLayers - 1; i >= 0; i--)
                {
                    if (double.IsNaN(rint))
                        throw new Exception("Bad Radiation Value in Light partitioning");
                    rint = rin;
                    for (int j = 0; j <= alley.Canopies.Count - 1; j++)
                        alley.Canopies[j].Rs[i] = rint * MathUtilities.Divide(alley.Canopies[j].Ftot[i] * alley.Canopies[j].Ktot, alley.layerKtot[i], 0.0);
                    rin -= rint;
                }
                alley.SurfaceRs = weather.Radn * soils / fs;
            }
            else
            {
                vine.SurfaceRs = weather.Radn;
                alley.SurfaceRs = weather.Radn;
            }
        }

        /// <summary>
        /// Calculates interception of short wave by canopy compartments.
        /// </summary>
        private void CalculateLayeredShortWaveRadiation(PerennialMicroClimateZone zoneMC, double rin)
        {
            double rint = 0;
            for (int i = zoneMC.numLayers - 1; i >= 0; i--)
            {
                if (double.IsNaN(rint))
                    throw new Exception("Bad Radiation Value in Light partitioning");

                rint = rin * (1.0 - Math.Exp(-zoneMC.layerKtot[i] * zoneMC.LAItotsum[i]));
                for (int j = 0; j <= zoneMC.Canopies.Count - 1; j++)
                    zoneMC.Canopies[j].Rs[i] = rint * MathUtilities.Divide(zoneMC.Canopies[j].Ftot[i] * zoneMC.Canopies[j].Ktot, zoneMC.layerKtot[i], 0.0);
                rin -= rint;
            }
            zoneMC.SurfaceRs = rin;
        }
    }
}
