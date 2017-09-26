
namespace Models.PMF.Phenology
{
//    [System.AttributeUsage(System.AttributeTargets.Class |
//                       System.AttributeTargets.Struct)
//]
    public class ModelVar : ModelPar
    {

        public ModelVar(string id, string desc, string sym, string subscript, string units, string scope="", string untsTip="", bool C4Only = false):
            base (id, desc, sym, subscript, units, scope, untsTip, C4Only)
        {
        }
    }
}
