
namespace Models.PMF.Photosynthesis
{

    /// <summary>
    /// 
    /// </summary>
    public class LayerModel
    {
        NaturalSpline spline;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public LayerModel(double[] x, double[] y)
        {
            spline = new NaturalSpline(x, y);
        }

        ///   
        public double getValue(double value)
        {
            return spline.getValue(value);
        }
    }
}
