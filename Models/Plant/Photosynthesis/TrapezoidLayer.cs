
namespace Models.PMF.Photosynthesis
{
    /// <summary></summary>
    public class TrapezoidLayer
    {
        /// <summary></summary>
        public TrapezoidLayer() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nLayers"></param>
        /// <param name="final"></param>
        /// <param name="intermediate"></param>
        /// <param name="LAIs"></param>
        public static void integrate(int nLayers, double[] final, double[] intermediate, double[] LAIs)
        {
            if (nLayers == 1)
            {
                final[0] = intermediate[0];
            }
            else
            {
                for (int i = 0; i < nLayers; i++)
                {
                    final[i] = (intermediate[i - 1] + intermediate[i]) * LAIs[i] / 2;
                }
            }
        }
    }
}
