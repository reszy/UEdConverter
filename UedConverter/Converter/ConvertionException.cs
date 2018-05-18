using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UedConverter.Converter
{
    public class ConvertionException : Exception
    {
        public ConvertionException(string message) : base(message) { }
    }
}
