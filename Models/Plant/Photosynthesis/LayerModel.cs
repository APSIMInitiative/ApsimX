
namespace Models.PMF.Phenology
{

    public class LayerModel
    {
        NaturalSpline spline;
        public LayerModel(double[] x, double[] y)
        {
            spline = new NaturalSpline(x, y);
        }

        //---------------------------------------------------------------------------
        public double getValue(double value)
        {
            return spline.getValue(value);
        }
    }
}
