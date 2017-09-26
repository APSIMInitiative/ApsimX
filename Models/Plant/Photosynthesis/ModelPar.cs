
namespace Models.PMF.Phenology
{
//    [System.AttributeUsage(System.AttributeTargets.Class |
//                       System.AttributeTargets.Struct)
//]
    public class ModelPar : System.Attribute
    {
        public string symbol;
        public string description;
        public string units;
        public string subscript;
        public string scope;
        public string unitsTip;
        public string id;

        public bool C4Only;

        public object value;

        public ModelPar(string id, string _desc, string _sym, string _subscript, string _units, string _scope="", string _unitsTip="", bool _C4Only = false)
        {
            this.id = id;
            this.symbol = _sym;
            this.description = _desc;
            this.units = _units;
            this.subscript = _subscript;
            this.scope = _scope;
            this.unitsTip = _unitsTip;
            this.C4Only = _C4Only;
            value = null;
        }
    }
}
