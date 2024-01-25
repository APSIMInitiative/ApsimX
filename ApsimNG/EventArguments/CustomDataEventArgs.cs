using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApsimNG.EventArguments
{
    /// <summary>
    /// Generic event arguments class which allows for simple passing of user
    /// data to the callback.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CustomDataEventArgs<T> : EventArgs
    {
        public T Data { get; private set; }

        public CustomDataEventArgs(T data)
        {
            Data = data;
        }
    }
}
