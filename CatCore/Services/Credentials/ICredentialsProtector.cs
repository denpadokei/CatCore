namespace CatCore.Services.Credentials
{
	/// <summary>
	/// Result of an attempt to protect credentials before they are written to disk.
	/// </summary>
	internal enum CredentialsProtectStatus
	{
		/// <summary>
		/// The credentials were successfully protected and may be written to disk.
		/// </summary>
		Success,

		/// <summary>
		/// The credentials could not be protected. The caller must not write them to disk.
		/// </summary>
		Failure,
	}

	/// <summary>
	/// Result of an attempt to unprotect a stored credentials file.
	/// </summary>
	internal enum CredentialsUnprotectStatus
	{
		/// <summary>
		/// The bytes were recognized as a CatCore credentials container and were successfully unprotected.
		/// </summary>
		Success,

		/// <summary>
		/// The bytes are not a CatCore credentials container, for example a legacy plaintext JSON file written
		/// by an older version of CatCore.
		/// </summary>
		NotCredentialsContainer,

		/// <summary>
		/// The bytes look like a CatCore credentials container, but could not be unprotected. The container was
		/// most likely written under a different Windows account or on a different machine, or its payload is
		/// corrupted.
		/// </summary>
		ForeignOrCorruptContainer,

		/// <summary>
		/// The bytes are a CatCore credentials container written by a newer version of CatCore that uses a
		/// container version this build does not understand.
		/// </summary>
		UnknownContainerVersion,
	}

	/// <summary>
	/// Transforms credentials between their in-memory representation (a JSON payload) and their at-rest
	/// representation in the credentials file. Implementations own their own container framing, which is how a
	/// stored file can be told apart from a legacy plaintext JSON file.
	/// </summary>
	internal interface ICredentialsProtector
	{
		/// <summary>
		/// Indicates whether <see cref="TryProtect"/> actually protects the data at rest. <c>false</c> means the
		/// bytes are stored as-is, which the credentials provider surfaces as a startup warning.
		/// </summary>
		bool EncryptsAtRest { get; }

		/// <summary>
		/// Protects the given plaintext bytes so they can be written to disk.
		/// </summary>
		CredentialsProtectStatus TryProtect(byte[] plaintext, out byte[]? protectedBytes);

		/// <summary>
		/// Tries to unprotect the given at-rest bytes previously produced by <see cref="TryProtect"/>.
		/// </summary>
		CredentialsUnprotectStatus TryUnprotect(byte[] protectedBytes, out byte[]? plaintext);
	}
}
