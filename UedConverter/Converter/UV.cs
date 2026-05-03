using System;
using System.Collections.Generic;
using System.Text;

namespace UedConverter.Converter;

public class UVi
{
    public int U { get; protected set; }
    public int V { get; protected set; }

    public UVi()
    {
        U = 0;
        V = 0;
    }

    public UVi(int u, int v)
    {
        U = u;
        V = v;
    }

    public override string ToString()
    {
        return $"U=\"{U}\", V=\"{V}\"";
    }
}
