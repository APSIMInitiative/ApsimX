
namespace Models.PMF.Photosynthesis
{
    /// <summary></summary>
    public class ModelVar : ModelPar
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="desc"></param>
        /// <param name="sym"></param>
        /// <param name="subscript"></param>
        /// <param name="units"></param>
        /// <param name="scope"></param>
        /// <param name="untsTip"></param>
        /// <param name="C4Only"></param>
        public ModelVar(string id, string desc, string sym, string subscript, string units, string scope = "", string untsTip = "", bool C4Only = false) :
            base(id, desc, sym, subscript, units, scope, untsTip, C4Only)
        {
        }
    }
}