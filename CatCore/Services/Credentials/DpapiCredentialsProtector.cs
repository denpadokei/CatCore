using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace CatCore.Services.Credentials
{
	/// <summary>
	/// Protects credentials at rest with the Windows Data Protection API (DPAPI) under the current user scope.
	/// The key material is owned and managed entirely by Windows and is never stored anywhere by CatCore, which
	/// means the resulting container can only be unprotected by processes running under the same Windows
	/// account on the same machine. The factory never selects this implementation on non-Windows platforms.
	/// </summary>
	internal sealed class DpapiCredentialsProtector : ICredentialsProtector
	{
		/// <summary>
		/// Byte marker that identifies a CatCore credentials container. A stored file without this marker is
		/// treated as a legacy plaintext JSON file.
		/// </summary>
		internal static readonly byte[] Magic = Encoding.ASCII.GetBytes("CCRED");

		/// <summary>
		/// The container layout version introduced together with DPAPI-based protection. Bumping this allows a
		/// future container layout to be told apart from this one.
		/// </summary>
		internal const byte ContainerVersion = 1;

		/// <summary>
		/// Fixed purpose label mixed into the DPAPI entropy. This is not a secret; it only prevents other
		/// applications from unprotecting a CatCore container by calling DPAPI without this label.
		/// </summary>
		private static readonly byte[] Entropy = Encoding.ASCII.GetBytes("CatCore.Credentials.v1");

		private static readonly int HeaderLength = Magic.Length + 1;

		/// <inheritdoc />
		public bool EncryptsAtRest => true;

		/// <inheritdoc />
		public CredentialsProtectStatus TryProtect(byte[] plaintext, out byte[]? protectedBytes)
		{
			protectedBytes = null;
			try
			{
				var blob = ProtectedData.Protect(plaintext, Entropy, DataProtectionScope.CurrentUser);

				var container = new byte[HeaderLength + blob.Length];
				Buffer.BlockCopy(Magic, 0, container, 0, Magic.Length);
				container[Magic.Length] = ContainerVersion;
				Buffer.BlockCopy(blob, 0, container, HeaderLength, blob.Length);

				protectedBytes = container;
				return CredentialsProtectStatus.Success;
			}
			catch
			{
				// Any failure to protect must result in the credentials not being written to disk; callers add
				// the logging context.
				return CredentialsProtectStatus.Failure;
			}
		}

		/// <inheritdoc />
		public CredentialsUnprotectStatus TryUnprotect(byte[] protectedBytes, out byte[]? plaintext)
		{
			plaintext = null;
			if (protectedBytes.Length < HeaderLength || !BytesStartWith(protectedBytes, Magic))
			{
				return CredentialsUnprotectStatus.NotCredentialsContainer;
			}

			if (protectedBytes[Magic.Length] != ContainerVersion)
			{
				return CredentialsUnprotectStatus.UnknownContainerVersion;
			}

			try
			{
				var blob = new byte[protectedBytes.Length - HeaderLength];
				Buffer.BlockCopy(protectedBytes, HeaderLength, blob, 0, blob.Length);

				plaintext = ProtectedData.Unprotect(blob, Entropy, DataProtectionScope.CurrentUser);
				return CredentialsUnprotectStatus.Success;
			}
			catch
			{
				// Either the container was written under a different Windows account or on a different machine,
				// or its payload is corrupted. Either way it cannot be unprotected here.
				return CredentialsUnprotectStatus.ForeignOrCorruptContainer;
			}
		}

		private static bool BytesStartWith(byte[] bytes, byte[] prefix)
		{
			for (var i = 0; i < prefix.Length; i++)
			{
				if (bytes[i] != prefix[i])
				{
					return false;
				}
			}

			return true;
		}
	}
}
