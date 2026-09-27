namespace FeBuddy.Core.Infrastructure.Configuration.Models;

/// <summary>
/// A settings file that cannot be imported: missing, unreadable, not an FE-Buddy settings file, or
/// from a newer FE-Buddy. The message is written for the user and can be shown as it is.
/// </summary>
public sealed class UserConfigTransferException : Exception
{
	/// <summary>Creates the exception with no message.</summary>
	public UserConfigTransferException()
	{
	}

	/// <summary>Creates the exception.</summary>
	/// <param name="message">What went wrong, for the user.</param>
	public UserConfigTransferException(string message)
		: base(message)
	{
	}

	/// <summary>Creates the exception around the error that caused it.</summary>
	/// <param name="message">What went wrong, for the user.</param>
	/// <param name="innerException">The underlying error.</param>
	public UserConfigTransferException(string message, Exception innerException)
		: base(message, innerException)
	{
	}
}
