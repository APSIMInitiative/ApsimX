using Models.DCAPST.Interfaces;

namespace Models.DCAPST
{
    /// <summary>
    /// 
    /// </summary>
    public class PathwayParameters : IPathwayParameters
    {
        /// <summary>
        /// 
        /// </summary>
        public double IntercellularToAirCO2Ratio { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double FractionOfCyclicElectronFlow { get; set; }        
        
        /// <summary>
        /// 
        /// </summary>
        public double RespirationSLNRatio { get; set; }        
        
        /// <summary>
        /// 
        /// </summary>
        public double MaxRubiscoActivitySLNRatio { get; set; }        
        
        /// <summary>
        /// 
        /// </summary>
        public double MaxElectronTransportSLNRatio { get; set; }        
        
        /// <summary>
        /// 
        /// </summary>
        public double MaxPEPcActivitySLNRatio { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double MesophyllCO2ConductanceSLNRatio { get; set; }
        
        /// <summary>
        /// 
        /// </summary>
        public double MesophyllElectronTransportFraction { get; set; }
        
        /// <summary>
        /// 
        /// </summary>
        public double ATPProductionElectronTransportFactor { get; set; }
       
        /// <summary>
        /// 
        /// </summary>
        public double ExtraATPCost { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public TemperatureResponseValues RubiscoCarboxylation { get; set; }
        
        /// <summary>
        /// 
        /// </summary>
        public TemperatureResponseValues RubiscoOxygenation { get; set; }
        
        /// <summary>
        /// 
        /// </summary>
        public TemperatureResponseValues RubiscoCarboxylationToOxygenation { get; set; }
        
        /// <summary>
        /// 
        /// </summary>
        public TemperatureResponseValues RubiscoActivity { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public TemperatureResponseValues PEPc { get; set; }
        
        /// <summary>
        /// 
        /// </summary>
        public TemperatureResponseValues PEPcActivity { get; set; }
        
        /// <summary>
        /// 
        /// </summary>
        public TemperatureResponseValues Respiration { get; set; }

        
        /// <summary>
        /// 
        /// </summary>
        public LeafTemperatureParameters ElectronTransportRateParams { get; set; }
        
        /// <summary>
        /// 
        /// </summary>
        public LeafTemperatureParameters MesophyllCO2ConductanceParams { get; set; }

        
        /// <summary>
        /// 
        /// </summary>
        public double SpectralCorrectionFactor { get; set; }
        
        /// <summary>
        /// 
        /// </summary>
        public double PS2ActivityFraction { get; set; }
        
        /// <summary>
        /// 
        /// </summary>
        public double PEPRegeneration { get; set; }
        
        /// <summary>
        /// 
        /// </summary>
        public double BundleSheathConductance { get; set; }       
    }

    /// <summary>
    /// 
    /// </summary>
    public struct TemperatureResponseValues
    {
        /// <summary>
        /// The value of the temperature response factor for a given parameter
        /// </summary>
        public double Factor;

        /// <summary>
        /// The value of the temperature response factor at 25 degrees
        /// </summary>
        public double At25;
    }
}
