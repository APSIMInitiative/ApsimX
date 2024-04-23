using System;

namespace Models.DCAPST.Canopy
{
    /// <summary>
    /// Models solar radiation on the canopy
    /// </summary>
    public class CanopyRadiation
    {
        /// <summary>
        /// Leaf level scattering coefficient for radiation
        /// </summary>
        /// <remarks>Different values for PAR/NIR</remarks>
        public double LeafScattering { get; set; }

        /// <summary>
        /// Diffuse extinction coefficient for radiation
        /// </summary>
        /// <remarks>Different values for PAR/NIR</remarks>
        public double DiffuseExtinction { get; set; }

        /// <summary>
        /// Diffuse reflection coefficient for radiation
        /// </summary>
        /// <remarks>Different values for PAR/NIR</remarks>
        public double DiffuseReflection { get; set; }

        /// <summary>
        /// Diffuse and scattered diffuse extinction coefficient
        /// </summary>
        public double DiffuseScatteredDiffuse => DiffuseExtinction * Math.Pow(1 - LeafScattering, 0.5);

        /// <summary>
        /// Direct extinction coefficient for radiation
        /// </summary>
        public double DirectExtinction { get; set; }

        /// <summary>
        /// Direct reflection coefficient for radiation
        /// </summary>
        public double DirectReflection => 1 - Math.Exp(-2 * HorizontalReflection * DirectExtinction / (1 + DirectExtinction));

        /// <summary>
        /// Direct and scattered direct coefficient for radiation
        /// </summary>
        public double DirectScatteredDirect => DirectExtinction * Math.Pow(1 - LeafScattering, 0.5);

        /// <summary>
        /// Horizontal reflection coefficient for radiation
        /// </summary>
        public double HorizontalReflection => (1 - Math.Pow(1 - LeafScattering, 0.5)) / (1 + Math.Pow(1 - LeafScattering, 0.5));

        /// <summary>
        /// The accumulated LAI of all layers up to the Nth layer
        /// </summary>
        private readonly double AccumLAI_1;

        /// <summary>
        /// The accumulated LAI of all layers up to the (N - 1)th layer
        /// </summary>
        private readonly double AccumLAI_0;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="layers"></param>
        /// <param name="lai"></param>
        public CanopyRadiation(int layers, double lai)
        {
            var layerLAI = lai / layers;

            AccumLAI_1 = layers * layerLAI;
            AccumLAI_0 = (layers - 1) * layerLAI;
        }

        /// <summary>
        /// Calculates the total radiation absorbed by the canopy
        /// </summary>
        /// <param name="direct">The direct solar radiation</param>
        /// <param name="diffuse">The diffuse solar radiation</param>
        public double CalcTotalRadiation(double direct, double diffuse)
        {
            var a = (1 - DirectReflection) * direct * CalcExp(DirectScatteredDirect);
            var b = (1 - DiffuseReflection) * diffuse * CalcExp(DiffuseScatteredDiffuse);

            return a + b;
        }

        /// <summary>
        /// Calculates the total radiation absorbed by the sunlit part of the canopy
        /// </summary>
        /// <param name="direct">The direct solar radiation</param>
        /// <param name="diffuse">The diffuse solar radiation</param>
        /// <returns></returns>
        public double CalcSunlitRadiation(double direct, double diffuse)
        {
            var dir = CalcSunlitDirect(direct);
            var dif = CalcSunlitDiffuse(diffuse);
            var sct = CalcSunlitScattered(direct);

            return dir + dif + sct;
        }

        /// <summary>
        /// Calculates the direct radiation absorbed by the sunlit canopy
        /// </summary>
        /// <param name="direct">The direct solar radiation</param>
        private double CalcSunlitDirect(double direct)
        {
            return (1 - LeafScattering) * direct * CalcExp(DirectExtinction);
        }

        /// <summary>
        /// Calculates the diffuse radiation absorbed by the sunlit canopy
        /// </summary>
        /// <param name="diffuse">The diffuse solar radiation</param>
        private double CalcSunlitDiffuse(double diffuse)
        {
            var DSD_DE = DiffuseScatteredDiffuse + DirectExtinction;
            var radiation = (1 - DiffuseReflection) * diffuse * CalcExp(DSD_DE) * (DiffuseScatteredDiffuse / DSD_DE);

            return radiation;
        }

        /// <summary>
        /// Calculates the scattered radiation absorbed by the sunlit canopy
        /// </summary>
        /// <param name="direct">The direct solar radiation</param>
        private double CalcSunlitScattered(double direct)
        {
            var DSD_DE = DirectScatteredDirect + DirectExtinction;
            if (DSD_DE == 0) return 0;

            var dir = (1.0 - DirectReflection) * CalcExp(DSD_DE) * (DirectScatteredDirect / DSD_DE); // Integral of direct     
            var dif = (1.0 - LeafScattering) * CalcExp(2 * DirectExtinction) / 2.0;    // Integral of diffuse

            var radiation = direct * (dir - dif);

            return radiation;
        }

        /// <summary>
        /// Calculates the LAI of the sunlit canopy
        /// </summary>
        public double CalculateSunlitLAI()
        {
            return CalcExp(DirectExtinction) / DirectExtinction;
        }

        /// <summary>
        /// Calculates the total intercepted radiation
        /// </summary>
        /// <returns></returns>
        public double CalcInterceptedRadiation()
        {
            return 1.0 - Math.Exp(-DirectExtinction * AccumLAI_1);
        }

        /// <summary>
        /// Models a function which finds the difference between two exponentials
        /// </summary>
        public double CalcExp(double x)
        {
            var a = Math.Exp(-x * AccumLAI_0);
            var b = Math.Exp(-x * AccumLAI_1);

            return a - b;
        }
    }
}
