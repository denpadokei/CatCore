using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using CatCore.Models.Credentials;
using CatCore.Services.Interfaces;
using Serilog;

namespace CatCore.Services
{
	internal abstract class KittenCredentialsProvider<T> where T : class, ICredentials, IEquatable<T>, new()
	{
		private readonly SemaphoreSlim _locker = new(1, 1);

		private readonly ILogger _logger;
		private readonly IKittenPathProvider _pathProvider;

		private readonly JsonSerializerOptions _jsonSerializerOptions;

		private readonly string _credentialsFilePath;

		private string? _credentialsFileName;
		private string CredentialsFilename => _credentialsFileName ??= $"{nameof(CatCore)}Credentials - {ServiceType}.json";

		protected abstract string ServiceType { get; }

		protected T Credentials { get; private set; } = null!;

		public event Action? OnCredentialsChanged;

		protected KittenCredentialsProvider(ILogger logger, IKittenPathProvider pathProvider)
		{
			_logger = logger;
			_pathProvider = pathProvider;

			_jsonSerializerOptions = new JsonSerializerOptions { WriteIndented = true };

			_credentialsFilePath = Path.Combine(pathProvider.DataPath, CredentialsFilename);

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

				using var fileStream = File.OpenRead(_credentialsFilePath);
				Credentials = JsonSerializer.Deserialize<T>(fileStream, _jsonSerializerOptions) ?? new T();
			}
			catch (Exception e)
			{
				_logger.Error(e, "An error occurred while trying to load the config for service {ServiceType}", ServiceType);
				WriteToDiskNoSafeGuards(Credentials = new T());
			}
			finally
			{
				_locker.Release();
			}
		}

		private void WriteToDiskNoSafeGuards(T credentials)
		{
			using var fileStream = File.Open(_credentialsFilePath, FileMode.Create, FileAccess.Write);
			JsonSerializer.Serialize(fileStream, credentials, _jsonSerializerOptions);
			fileStream.Flush(true);
		}
	}
}