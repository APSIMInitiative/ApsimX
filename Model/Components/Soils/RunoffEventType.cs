using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Model.Components
{
    public class RunoffEventType
    {
        public float runoff;
    }

    public delegate void RunoffEventDelegate(RunoffEventType Runoff);
}
