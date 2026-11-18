namespace CatCore.Services.Credentials
{
	/// <summary>
	/// No-op protector used on platforms without a native protection facility (Linux/macOS self-hosting) and in
	/// tests that need deterministic on-disk files. Credentials remain stored as plain JSON; the credentials
	/// provider logs a startup warning whenever this protector is active.
	/// </summary>
	internal sealed class PlaintextCredentialsProtector : ICredentialsProtector
	{
		/// <inheritdoc />
		public bool EncryptsAtRest => false;

		/// <inheritdoc />
		public CredentialsProtectStatus TryProtect(byte[] plaintext, out byte[]? protectedBytes)
		{
			protectedBytes = plaintext;
			return CredentialsProtectStatus.Success;
		}

		/// <inheritdoc />
		public CredentialsUnprotectStatus TryUnprotect(byte[] protectedBytes, out byte[]? plaintext)
		{
			// Plaintext files are never framed as a container; the caller treats them as a legacy plaintext
			// JSON payload and parses them directly.
			plaintext = null;
			return CredentialsUnprotectStatus.NotCredentialsContainer;
		}
	}
}
