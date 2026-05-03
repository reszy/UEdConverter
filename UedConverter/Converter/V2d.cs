namespace UedConverter.Converter;

public class V2d
{
    public double X { get; protected set; }
    public double Y { get; protected set; }

    public V2d()
    {
        X = .0;
        Y = .0;
    }

    public V2d(double x, double y)
    {
        X = x;
        Y = y;
    }

    public override string ToString()
    {
        return $"X=\"{X}\", Y=\"{Y}\"";
    }


    public static V2d operator +(V2d left, V2d right)
    {
        return new V2d(
            left.X + right.X,
            left.Y + right.Y
            );
    }


    public static V2d operator +(V2d left, UVi right)
    {
        return new V2d(
            left.X + right.U,
            left.Y + right.V
            );
    }


    public static V2d operator -(V2d left, V2d right)
    {
        return new V2d(
            left.X - right.X,
            left.Y - right.Y
            );
    }


    public double Dot(V2d v)
    {
        return X * v.X + Y * v.Y;
    }


    public double Magnitude()
    {
        return Math.Sqrt(X * X + Y * Y);
    }
}
