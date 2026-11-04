using System.Globalization;

namespace ShaderStudio.Core.Updates;

/// <summary>
/// Minimal "major.minor.patch" version used to compare the running build with a release tag.
/// Numeric only on purpose: tags such as <c>v1.2.3</c> or <c>1.2</c> must compare predictably.
/// </summary>
public readonly record struct ReleaseVersion(int Major, int Minor, int Patch) : IComparable<ReleaseVersion>
{
    public static ReleaseVersion Zero => new(0, 0, 0);

    public static ReleaseVersion Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Zero;
        }

        var text = value.Trim();
        if (text.StartsWith('v') || text.StartsWith('V'))
        {
            text = text[1..];
        }

        // Drop prerelease / build metadata: "1.2.3-beta.1+sha" -> "1.2.3"
        var cut = text.IndexOfAny(new[] { '-', '+' });
        if (cut > 0)
        {
            text = text[..cut];
        }

        var parts = text.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return new ReleaseVersion(
            ParsePart(parts, 0),
            ParsePart(parts, 1),
            ParsePart(parts, 2));
    }

    public int CompareTo(ReleaseVersion other)
    {
        var byMajor = Major.CompareTo(other.Major);
        if (byMajor != 0)
        {
            return byMajor;
        }

        var byMinor = Minor.CompareTo(other.Minor);
        return byMinor != 0 ? byMinor : Patch.CompareTo(other.Patch);
    }

    public static bool operator >(ReleaseVersion left, ReleaseVersion right) => left.CompareTo(right) > 0;

    public static bool operator <(ReleaseVersion left, ReleaseVersion right) => left.CompareTo(right) < 0;

    public static bool operator >=(ReleaseVersion left, ReleaseVersion right) => left.CompareTo(right) >= 0;

    public static bool operator <=(ReleaseVersion left, ReleaseVersion right) => left.CompareTo(right) <= 0;

    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Major}.{Minor}.{Patch}");

    private static int ParsePart(string[] parts, int index)
    {
        if (index >= parts.Length)
        {
            return 0;
        }

        return int.TryParse(parts[index], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? Math.Max(parsed, 0)
            : 0;
    }
}
