using System;

namespace AppLogic.Helpers;

public enum RedactionMode
{
    Full,
    PreserveLength,
    HashSha256,
    First4Last4,
    BinaryLength
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class RedactAttribute : Attribute
{
    public RedactionMode Mode { get; }
    public RedactAttribute(RedactionMode mode = RedactionMode.Full) => Mode = mode;
}
