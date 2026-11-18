using System.Runtime.InteropServices;

namespace CatCore.Services.Credentials
{
	/// <summary>
	/// Selects the default <see cref="ICredentialsProtector"/> for the current platform: DPAPI under the
	/// current user scope on Windows, a plaintext passthrough (accompanied by a startup warning from the
	/// credentials provider) everywhere else.
	/// </summary>
	internal static class CredentialsProtectorFactory
	{
		public static ICredentialsProtector CreateDefault()
		{
			return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
				? new DpapiCredentialsProtector()
				: new PlaintextCredentialsProtector();
		}
	}
}
