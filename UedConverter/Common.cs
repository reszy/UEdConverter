using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UedConverter
{
    public static class Common
    {

        public static string GetSizeWithUnits(double value)
        {
            string unit = "B";
            if (value > 1024)
            {
                unit = "kB";
                value /= 1024;
                if (value > 1024)
                {
                    unit = "MB";
                    value /= 1024;
                    if (value > 1024)
                    {
                        unit = "GB";
                        value /= 1024;
                        if (value > 1024)
                        {
                            unit = "TB";
                            value /= 1024;
                        }
                    }
                }
            }
            else
            {
                return $"{value} {unit}";
            }
            return $"{value:F2} {unit}";
        }
    }
}
