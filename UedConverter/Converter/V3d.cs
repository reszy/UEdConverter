using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UedConverter.Converter
{
    public class V3d
    {
        public double X { get; protected set; }
        public double Y { get; protected set; }
        public double Z { get; protected set; }

        public V3d()
        {
            X = .0;
            Y = .0;
            Z = .0;
        }

        public V3d(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static V3d Parse(string numbers, char separator)
        {
            var splited = numbers.Trim().Split(separator);
            double[] doubleNumbers = { .0, .0, .0 };
            if (splited.Length != 3) throw new ArgumentException("Cannot create 3D vector from " + splited.Length + " numbers");
            for (int i = 0; i < 3; i++)
            {
                doubleNumbers[i] = Double.Parse(splited[i], CultureInfo.InvariantCulture);
            }
            return new V3d(doubleNumbers[0], doubleNumbers[1], doubleNumbers[2]);
        }

        public override string ToString()
        {
            return "X=\"" + X + "\", Y=\"" + Y + "\", Z=\"" + Z + '"';
        }
    }
}
