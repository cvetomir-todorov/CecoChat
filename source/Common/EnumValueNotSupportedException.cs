namespace Common;

/// <summary>
/// Thrown when an enum value is not supported.
/// E.g. in a switch statement a certain enum value may not be supported yet.
/// </summary>
public class EnumValueNotSupportedException : Exception
{
    public EnumValueNotSupportedException(Enum enumValue)
        : base($"{enumValue.GetType().FullName} value {enumValue} is not supported.")
    {
        if (!enumValue.GetType().IsEnum)
        {
            throw new ArgumentException($"Type {enumValue.GetType().FullName} should be enum.");
        }

        EnumType = enumValue.GetType();
        EnumValue = enumValue;
    }

    public Type EnumType { get; }

    public Enum EnumValue { get; }
}
