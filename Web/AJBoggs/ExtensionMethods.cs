using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AJBoggs
{
    public static class ExtensionMethods
    {
        public static bool IsCritical(this Exception exception) {
            if (exception == null) {
                return false;
            }
            if (exception is OutOfMemoryException || exception is StackOverflowException) {
                return true;
            }
            return false;
        }
    }
}