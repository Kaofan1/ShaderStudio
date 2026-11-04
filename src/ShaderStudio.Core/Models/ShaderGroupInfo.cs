namespace ShaderStudio.Core.Models;

/// <summary>One entry of the <c>groups</c> array: a set of shader hashes replaced by the addon.</summary>
public sealed class ShaderGroupInfo
{
    public required string Key { get; init; }

    public string Label { get; init; } = string.Empty;

    public bool IsDefault { get; init; }

    public bool Hidden { get; init; }

    public bool HdrGroup { get; init; }

    public int HashCount { get; init; }
}
