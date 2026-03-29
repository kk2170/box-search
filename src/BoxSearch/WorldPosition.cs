using System.Globalization;

namespace BoxSearch;

internal readonly struct WorldPosition
{
    internal WorldPosition(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    internal float X { get; }

    internal float Y { get; }

    internal float Z { get; }

    public override string ToString()
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "{0:0.#}, {1:0.#}, {2:0.#}",
            X,
            Y,
            Z);
    }
}
