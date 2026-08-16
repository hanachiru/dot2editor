namespace Dot2Editor.Core.Parsing;

/// <summary>
///     Thrown when the input cannot be read as a .DotSettings document.
/// </summary>
public sealed class DotSettingsParseException : Exception
{
    /// <summary>Creates the exception with a message describing what was wrong with the input.</summary>
    /// <param name="message">A description suitable for showing to the user.</param>
    public DotSettingsParseException(string message) : base(message)
    {
    }

    /// <summary>Creates the exception from an underlying failure, such as an XML error.</summary>
    /// <param name="message">A description suitable for showing to the user.</param>
    /// <param name="innerException">The underlying failure.</param>
    public DotSettingsParseException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
