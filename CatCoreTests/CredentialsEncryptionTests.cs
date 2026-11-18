using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CatCore.Models.Credentials;
using CatCore.Services;
using CatCore.Services.Credentials;
using CatCore.Services.Interfaces;
using FluentAssertions;
using Serilog;
using Xunit;

namespace CatCoreTests
{
	/// <summary>
	/// Tests for the at-rest protection of the credentials file: migration of legacy plaintext files to the
	/// DPAPI-protected container format, preservation of files that cannot be unprotected, and persistence
	/// failure handling. Tests that require DPAPI disable themselves on non-Windows platforms.
	/// </summary>
	public class CredentialsEncryptionTests : IDisposable
	{
		private static readonly byte[] Magic = DpapiCredentialsProtector.Magic;

		private readonly string _dataPath;

		public CredentialsEncryptionTests()
		{
			_dataPath = Path.Combine(Path.GetTempPath(), "catcore-cred-tests-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(_dataPath);
		}

		private string CredentialsFilePath => Path.Combine(_dataPath, "CatCoreCredentials - Test.json");

		private static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

		private TestCredentialsProvider CreateProvider(ICredentialsProtector protector)
		{
			return new TestCredentialsProvider(new LoggerConfiguration().CreateLogger(), new TestKittenPathProvider(_dataPath), protector);
		}

		private static byte[] SerializeTestCredentials(TestCredentials credentials)
		{
			return JsonSerializer.SerializeToUtf8Bytes(credentials, new JsonSerializerOptions { WriteIndented = true });
		}

		[Fact]
		public void LegacyPlaintextFileIsMigratedToProtectedContainer()
		{
			if (!IsWindows)
			{
				return; // DPAPI is only available on Windows; the CI test job for CatCore runs on Windows.
			}

			var legacyBytes = SerializeTestCredentials(new TestCredentials("dummy-access-token-1", "dummy-refresh-token-1"));
			File.WriteAllBytes(CredentialsFilePath, legacyBytes);

			var provider = CreateProvider(new DpapiCredentialsProtector());

			// The tokens were already loaded before migration: no re-login is required.
			provider.Current.AccessToken.Should().Be("dummy-access-token-1");
			provider.Current.RefreshToken.Should().Be("dummy-refresh-token-1");

			// The file was replaced by a CatCore container, not plaintext JSON.
			var storedBytes = File.ReadAllBytes(CredentialsFilePath);
			storedBytes.Length.Should().BeGreaterThanOrEqualTo(Magic.Length + 1);
			Encoding.ASCII.GetString(storedBytes, 0, Magic.Length).Should().Be("CCRED");
			storedBytes[Magic.Length].Should().Be(DpapiCredentialsProtector.ContainerVersion);

			// No trace of the tokens may remain in the file itself.
			Encoding.ASCII.GetString(storedBytes).Should().NotContain("dummy-access-token-1").And.NotContain("dummy-refresh-token-1");

			// No leftover temporary migration files.
			Directory.GetFiles(_dataPath, "*.tmp-*").Should().BeEmpty();
		}

		[Fact]
		public void ProtectedContainerRoundTripsThroughStoreAndLoad()
		{
			if (!IsWindows)
			{
				return; // DPAPI is only available on Windows; the CI test job for CatCore runs on Windows.
			}

			var provider = CreateProvider(new DpapiCredentialsProtector());
			provider.TryUpdate(new TestCredentials("dummy-access-token-2", "dummy-refresh-token-2")).Should().BeTrue();

			// A second provider instance reads the same file through real DPAPI.
			var reloaded = CreateProvider(new DpapiCredentialsProtector());
			reloaded.Current.AccessToken.Should().Be("dummy-access-token-2");
			reloaded.Current.RefreshToken.Should().Be("dummy-refresh-token-2");
		}

		[Fact]
		public void ForeignContainerCannotBeReadAndFileIsPreserved()
		{
			if (!IsWindows)
			{
				return; // DPAPI is only available on Windows; the CI test job for CatCore runs on Windows.
			}

			// A container with a valid header but a payload that DPAPI cannot decrypt. This simulates a file
			// that was written under a different Windows account or on a different machine.
			var foreignBytes = new byte[Magic.Length + 1 + 32];
			Encoding.ASCII.GetBytes("CCRED").CopyTo(foreignBytes, 0);
			foreignBytes[Magic.Length] = DpapiCredentialsProtector.ContainerVersion;
			for (var i = 0; i < 32; i++)
			{
				foreignBytes[Magic.Length + 1 + i] = (byte)(i * 7 + 13);
			}

			File.WriteAllBytes(CredentialsFilePath, foreignBytes);

			var provider = CreateProvider(new DpapiCredentialsProtector());

			// The session starts without credentials...
			provider.Current.AccessToken.Should().BeNull();
			provider.Current.RefreshToken.Should().BeNull();

			// ...but the unreadable file must be preserved untouched.
			File.ReadAllBytes(CredentialsFilePath).Should().Equal(foreignBytes);
		}

		[Fact]
		public void UnknownContainerVersionCannotBeReadAndFileIsPreserved()
		{
			if (!IsWindows)
			{
				return; // DPAPI is only available on Windows; the CI test job for CatCore runs on Windows.
			}

			var futureBytes = new byte[Magic.Length + 1 + 8];
			Encoding.ASCII.GetBytes("CCRED").CopyTo(futureBytes, 0);
			futureBytes[Magic.Length] = 99; // A container version this build does not know.
			for (var i = 0; i < 8; i++)
			{
				futureBytes[Magic.Length + 1 + i] = (byte)(i + 1);
			}

			File.WriteAllBytes(CredentialsFilePath, futureBytes);

			var provider = CreateProvider(new DpapiCredentialsProtector());

			provider.Current.AccessToken.Should().BeNull();

			// A file from a future version must be preserved untouched as well.
			File.ReadAllBytes(CredentialsFilePath).Should().Equal(futureBytes);
		}

		[Fact]
		public void CorruptLegacyFileIsReplacedByProtectedEmptyFile()
		{
			if (!IsWindows)
			{
				return; // DPAPI is only available on Windows; the CI test job for CatCore runs on Windows.
			}

			File.WriteAllText(CredentialsFilePath, "this is not json at all");

			var provider = CreateProvider(new DpapiCredentialsProtector());

			provider.Current.AccessToken.Should().BeNull();

			// The corrupt file is replaced by a valid protected empty file.
			var storedBytes = File.ReadAllBytes(CredentialsFilePath);
			storedBytes.Length.Should().BeGreaterThanOrEqualTo(Magic.Length + 1);
			Encoding.ASCII.GetString(storedBytes, 0, Magic.Length).Should().Be("CCRED");
			storedBytes[Magic.Length].Should().Be(DpapiCredentialsProtector.ContainerVersion);
		}

		[Fact]
		public void FreshInstallWritesProtectedEmptyFile()
		{
			if (!IsWindows)
			{
				return; // DPAPI is only available on Windows; the CI test job for CatCore runs on Windows.
			}

			CreateProvider(new DpapiCredentialsProtector());

			// Even the empty placeholder file must never be plaintext.
			var storedBytes = File.ReadAllBytes(CredentialsFilePath);
			storedBytes.Length.Should().BeGreaterThanOrEqualTo(Magic.Length + 1);
			Encoding.ASCII.GetString(storedBytes, 0, Magic.Length).Should().Be("CCRED");
			storedBytes[Magic.Length].Should().Be(DpapiCredentialsProtector.ContainerVersion);
		}

		[Fact]
		public void ProtectFailureKeepsPreviousFileIntact()
		{
			// Platform-independent: a protector stub that refuses to protect anything.
			var legacyBytes = SerializeTestCredentials(new TestCredentials("dummy-access-token-3", "dummy-refresh-token-3"));
			File.WriteAllBytes(CredentialsFilePath, legacyBytes);

			var provider = CreateProvider(new FailingCredentialsProtector());

			// Migration could not protect the legacy file: the plaintext stays on disk for a retry next start...
			File.ReadAllBytes(CredentialsFilePath).Should().Equal(legacyBytes);

			// ...but the legacy content was still loaded into memory, so no re-login is required this session.
			provider.Current.AccessToken.Should().Be("dummy-access-token-3");

			// A failed store reports failure without touching the previous file.
			provider.TryUpdate(new TestCredentials("dummy-access-token-4", "dummy-refresh-token-4")).Should().BeFalse();
			File.ReadAllBytes(CredentialsFilePath).Should().Equal(legacyBytes);
		}

		[Fact]
		public void PlaintextProtectorKeepsLegacyFormatAndBehaviorUnchanged()
		{
			// Simulates the non-Windows self-hosting path: no encryption, legacy JSON format preserved.
			var legacyBytes = SerializeTestCredentials(new TestCredentials("dummy-access-token-5", "dummy-refresh-token-5"));
			File.WriteAllBytes(CredentialsFilePath, legacyBytes);

			var provider = CreateProvider(new PlaintextCredentialsProtector());

			provider.Current.AccessToken.Should().Be("dummy-access-token-5");

			// No migration happened; the file is still plaintext JSON.
			File.ReadAllBytes(CredentialsFilePath).Should().Equal(legacyBytes);

			provider.TryUpdate(new TestCredentials("dummy-access-token-6", "dummy-refresh-token-6")).Should().BeTrue();

			var updated = JsonSerializer.Deserialize<TestCredentials>(File.ReadAllBytes(CredentialsFilePath));
			updated!.AccessToken.Should().Be("dummy-access-token-6");
			updated.RefreshToken.Should().Be("dummy-refresh-token-6");
		}

		public void Dispose()
		{
			try
			{
				if (Directory.Exists(_dataPath))
				{
					Directory.Delete(_dataPath, recursive: true);
				}
			}
			catch (IOException)
			{
				// Best effort cleanup only.
			}
		}

		private sealed class FailingCredentialsProtector : ICredentialsProtector
		{
			public bool EncryptsAtRest => true;

			public CredentialsProtectStatus TryProtect(byte[] plaintext, out byte[]? protectedBytes)
			{
				protectedBytes = null;
				return CredentialsProtectStatus.Failure;
			}

			public CredentialsUnprotectStatus TryUnprotect(byte[] protectedBytes, out byte[]? plaintext)
			{
				plaintext = null;
				return CredentialsUnprotectStatus.NotCredentialsContainer;
			}
		}

		private sealed class TestKittenPathProvider : IKittenPathProvider
		{
			public string DataPath { get; }

			public TestKittenPathProvider(string dataPath)
			{
				DataPath = dataPath;
			}
		}
	}

	internal sealed class TestCredentials : ICredentials, IEquatable<TestCredentials>
	{
		public string? AccessToken { get; }

		public string? RefreshToken { get; }

		public TestCredentials()
		{
		}

		[JsonConstructor]
		public TestCredentials(string? accessToken, string? refreshToken)
		{
			AccessToken = accessToken;
			RefreshToken = refreshToken;
		}

		public bool Equals(TestCredentials? other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return AccessToken == other.AccessToken && RefreshToken == other.RefreshToken;
		}

		public override bool Equals(object? obj)
		{
			return ReferenceEquals(this, obj) || obj is TestCredentials other && Equals(other);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = AccessToken != null ? AccessToken.GetHashCode() : 0;
				hashCode = (hashCode * 397) ^ (RefreshToken != null ? RefreshToken.GetHashCode() : 0);
				return hashCode;
			}
		}
	}

	internal sealed class TestCredentialsProvider : KittenCredentialsProvider<TestCredentials>
	{
		public TestCredentialsProvider(ILogger logger, IKittenPathProvider pathProvider, ICredentialsProtector protector)
			: base(logger, pathProvider, protector)
		{
		}

		protected override string ServiceType => "Test";

		public TestCredentials Current => Credentials;

		public bool TryUpdate(TestCredentials credentials) => UpdateCredentials(credentials);
	}
}
