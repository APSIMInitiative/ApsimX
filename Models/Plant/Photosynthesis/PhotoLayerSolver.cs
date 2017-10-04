
namespace Models.PMF.Photosynthesis
{
    /// <summary></summary>
    public abstract class PhotoLayerSolver { 
   
        PhotosynthesisModel _PM;
        int _layer;

        //--------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        /// <param name="PM"></param>
        /// <param name="layer"></param>
        public PhotoLayerSolver( PhotosynthesisModel PM, int layer)
        {
            _PM = PM;
            _layer = layer;
        }
        /// <summary></summary>
        public abstract double calcPhotosynthesis(PhotosynthesisModel PM, SunlitShadedCanopy s, int layer, double _Cc);
        /// <summary></summary>
        public abstract double calcAc(double Cc, LeafCanopy canopy, SunlitShadedCanopy s, int layer);
        /// <summary></summary>
        public abstract double calcAj(double Cc, LeafCanopy canopy, SunlitShadedCanopy s, int layer);
    }
}
