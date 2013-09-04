using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;
using System.Collections;
using System.Xml.Serialization;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Utility
{
    public class Assert
    {
        /// <summary>
        /// Helper for Asserting that a function throws an exception of a particular type.
        /// </summary>
        public static void Throws<T>(Action func) where T : Exception
        {
            Exception exceptionOther = null;
            var exceptionThrown = false;
            try
            {
                func.Invoke();
            }
            catch (T)
            {
                exceptionThrown = true;
            }
            catch (Exception e)
            {
                exceptionOther = e;
            }

            if (!exceptionThrown)
            {
                if (exceptionOther != null)
                {
                    throw new AssertFailedException(
                        System.String.Format("An exception of type {0} was expected, but not thrown. Instead, an exception of type {1} was thrown.", typeof(T), exceptionOther.GetType()),
                        exceptionOther
                        );
                }

                throw new AssertFailedException(
                    System.String.Format("An exception of type {0} was expected, but no exception was thrown.", typeof(T))
                    );
            }
        }

    }
}

