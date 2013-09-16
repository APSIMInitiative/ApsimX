using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Model.Components
{
    public class NitrogenChangedType
    {
        public string Sender;
        public double[] DeltaUrea;
        public double[] DeltaNH4;
        public double[] DeltaNO3;
    }

    public delegate void NitrogenChangedDelegate(NitrogenChangedType Nitrogen);
}
