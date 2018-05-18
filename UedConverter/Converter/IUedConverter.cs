using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UedConverter.Converter
{
    interface IUedConverter
    {
        string[] Convert(string[] input);
    }
}
