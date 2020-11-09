using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApsimNG.EventArguments
{
    public delegate Task AsyncEventHandler(object sender, EventArgs args);
}
