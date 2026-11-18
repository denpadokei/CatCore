using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using CatCore.Models.Credentials;
using CatCore.Services.Credentials;
using CatCore.Services.Interfaces;
using Serilog;

namespace CatCore.Services
{
	internal abstract class KittenCredentialsProvider<T> where T : class, ICredentials, IEquatable<T>, new()
	{
		private readonly SemaphoreSlim _locker = new(1, 1);

		private readonly ILogger _logger;
		private readonly IKittenPathProvider _pathProvider;
		private readonly ICredentialsProtector _credentialsProtector;

		private readonly JsonSerializerOptions _jsonSerializerOptions;

		private readonly string _credentialsFilePath;

		private string? _credentialsFileName;
		private string CredentialsFilename => _credentialsFileName ??= $"{nameof(CatCore)}Credentials - {ServiceType}.json";

		protected abstract string ServiceType { get; }

		protected T Credentials { get; private set; } = null!;

		public event Action? OnCredentialsChanged;

		protected KittenCredentialsProvider(ILogger logger, IKittenPathProvider pathProvider, ICredentialsProtector credentialsProtector)
		{
			_logger = logger;
			_pathProvider = pathProvider;
			_credentialsProtector = credentialsProtector;

			_jsonSerializerOptions = new JsonSerializerOptions { WriteIndented = true };

			_credentialsFilePath = Path.Combine(pathProvider.DataPath, CredentialsFilename);

			if (!_credentialsProtector.EncryptsAtRest)
			{
				_logger.Warning(
					"Credentials for service {ServiceType} are stored without encryption because no platform-native protection is available on this platform; "
					+ "consider restricting access to the data directory to your own user account", ServiceType);
			}

			// Initializing internally
			Load();
		}

		private bool Store()
		{
			try
			{
				_locker.Wait();

				_logger.Information("Storing credentials for service {ServiceType}", ServiceType);

				if (!Directory.Exists(_pathProvider.DataPath))
				{
					Directory.CreateDirectory(_pathProvider.DataPath);
				}

				WriteToDiskNoSafeGuards(Credentials);

				try
				{
					OnCredentialsChanged?.Invoke();
				}
				catch (Exception e)
				{
					// A failing subscriber must not make a successful disk write look like a
					// persistence failure.
					_logger.Error(e, "A subscriber of OnCredentialsChanged for service {ServiceType} threw", ServiceType);
				}

				return true;
			}
			catch (Exception e)
			{
				_logger.Error(e, "An error occurred while trying to store the config for service {ServiceType}", ServiceType);
				return false;
			}
			finally
			{
				_locker.Release();
			}
		}

		/// <summary>
		/// Updates the in-memory credentials and persists them when they changed. Returns
		/// <c>false</c> when persistence failed, so callers can avoid treating a refresh as
		/// successful when the rotated token pair only exists in memory.
		/// </summary>
		protected bool UpdateCredentials(T credentials)
		{
			var shouldStore = !credentials.Equals(Credentials);
			Credentials = credentials;

			return !shouldStore || Store();
		}

		private void Load()
		{
			try
			{
				_locker.Wait();

				_logger.Information("Loading credentials for service {ServiceType}", ServiceType);

				if (!Directory.Exists(_pathProvider.DataPath))
				{
					Directory.CreateDirectory(_pathProvider.DataPath);
				}

				if (!File.Exists(_credentialsFilePath))
				{
					WriteToDiskNoSafeGuards(Credentials = new T());
					return;
				}

				var fileBytes = File.ReadAllBytes(_credentialsFilePath);

				switch (_credentialsProtector.TryUnprotect(fileBytes, out var jsonBytes))
				{
					case CredentialsUnprotectStatus.Success:
					{
						if (jsonBytes != null && TryDeserializeCredentials(jsonBytes, out var credentials))
						{
							Credentials = credentials!;
							return;
						}

						// The container was unprotected, but its payload is no longer valid JSON. There are no
						// recoverable credentials left in it, so replace it with a fresh protected empty file.
						WriteToDiskNoSafeGuards(Credentials = new T());
						return;
					}

					case CredentialsUnprotectStatus.ForeignOrCorruptContainer:
					{
						_logger.Error(
							"The stored credentials for service {ServiceType} could not be unprotected, most likely because they were written under a different Windows account "
							+ "or on a different machine; the file was left untouched and a new login will be required", ServiceType);
						Credentials = new T();
						return;
					}

					case CredentialsUnprotectStatus.UnknownContainerVersion:
					{
						_logger.Error(
							"The stored credentials for service {ServiceType} were written by a newer version of CatCore and cannot be read by this version; "
							+ "the file was left untouched and a new login will be required", ServiceType);
						Credentials = new T();
						return;
					}

					default: // CredentialsUnprotectStatus.NotCredentialsContainer
					{
						if (TryDeserializeCredentials(fileBytes, out var legacyCredentials))
						{
							// A legacy plaintext JSON file from before protected storage. The user must not have
							// to re-login, so migrate it in place before anything else can overwrite it.
							Credentials = legacyCredentials!;
							MigrateLegacyPlaintextFile(fileBytes);
							return;
						}

						// A file that is neither a container nor valid JSON is corrupt; there are no recoverable
						// credentials in it, so replace it with a fresh protected empty file, like before.
						WriteToDiskNoSafeGuards(Credentials = new T());
						return;
					}
				}
			}
			catch (Exception e)
			{
				_logger.Error(e, "An error occurred while trying to load the config for service {ServiceType}", ServiceType);

				// Deliberately do not write anything to disk here: the on-disk file may be perfectly fine (for
				// example while another process is atomically replacing it), and wiping it would destroy
				// whatever is stored in it. Start the session with empty credentials instead.
				Credentials = new T();
			}
			finally
			{
				_locker.Release();
			}
		}

		/// <summary>
		/// Migrates a legacy plaintext credentials file to the protected container format in place. The exact
		/// original bytes are preserved inside the container and an unprotect roundtrip is verified before the
		/// plaintext file is replaced; on any failure the plaintext file is left untouched and the migration is
		/// retried on the next start.
		/// </summary>
		private void MigrateLegacyPlaintextFile(byte[] legacyFileBytes)
		{
			if (!_credentialsProtector.EncryptsAtRest)
			{
				// Nothing to migrate to: the plaintext protector intentionally keeps the legacy format.
				return;
			}

			if (_credentialsProtector.TryProtect(legacyFileBytes, out var protectedBytes) != CredentialsProtectStatus.Success || protectedBytes == null)
			{
				_logger.Error(
					"Failed to protect the legacy plaintext credentials for service {ServiceType}; the plaintext file was left untouched and the migration will be retried on the next start",
					ServiceType);
				return;
			}

			if (_credentialsProtector.TryUnprotect(protectedBytes, out var roundtripBytes) != CredentialsUnprotectStatus.Success
				|| roundtripBytes == null
				|| !BytesEqual(roundtripBytes, legacyFileBytes))
			{
				_logger.Error(
					"The protected container for the legacy credentials of service {ServiceType} could not be verified; the plaintext file was left untouched and the migration will be retried on the next start",
					ServiceType);
				return;
			}

			var temporaryFilePath = $"{_credentialsFilePath}.tmp-{Guid.NewGuid():N}";
			try
			{
				File.WriteAllBytes(temporaryFilePath, protectedBytes);
				File.Replace(temporaryFilePath, _credentialsFilePath, null);
				_logger.Information("Migrated the stored credentials for service {ServiceType} from the legacy plaintext format to the protected container format", ServiceType);
			}
			catch (Exception e)
			{
				_logger.Error(e, "Failed to replace the legacy plaintext credentials file for service {ServiceType}; it was left untouched and the migration will be retried on the next start", ServiceType);
				TryDeleteFileBestEffort(temporaryFilePath);
			}
		}

		private bool TryDeserializeCredentials(byte[] jsonBytes, out T? credentials)
		{
			try
			{
				credentials = JsonSerializer.Deserialize<T>(jsonBytes, _jsonSerializerOptions);
				return credentials != null;
			}
			catch (JsonException)
			{
				credentials = null;
				return false;
			}
		}

		private void WriteToDiskNoSafeGuards(T credentials)
		{
			var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(credentials, _jsonSerializerOptions);
			if (_credentialsProtector.TryProtect(jsonBytes, out var protectedBytes) != CredentialsProtectStatus.Success || protectedBytes == null)
			{
				throw new InvalidOperationException($"Failed to protect the credentials for service {ServiceType} before writing them to disk");
			}

			using var fileStream = File.Open(_credentialsFilePath, FileMode.Create, FileAccess.Write);
			fileStream.Write(protectedBytes, 0, protectedBytes.Length);
			fileStream.Flush(true);
		}

		private static bool BytesEqual(byte[] left, byte[] right)
		{
			if (left.Length != right.Length)
			{
				return false;
			}

			for (var i = 0; i < left.Length; i++)
			{
				if (left[i] != right[i])
				{
					return false;
				}
			}

			return true;
		}

		private void TryDeleteFileBestEffort(string filePath)
		{
			try
			{
				if (File.Exists(filePath))
				{
					File.Delete(filePath);
				}
			}
			catch (Exception e)
			{
				_logger.Debug(e, "Failed to clean up the temporary credentials file {TemporaryFilePath}", filePath);
			}
		}
	}
}
