using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CoreUtility.Configuration
{

	public class ConfigurationDispatcher
	{

		public byte[]? Entropy
		{
			get => SecureStringConverter.Entropy;
			set => SecureStringConverter.Entropy = value;
		}

		public DataProtectionScope DataProtectionScope
		{
			get => SecureStringConverter.DataProtectionScope;
			set => SecureStringConverter.DataProtectionScope = value;
		}

		private readonly SecureStringConverter SecureStringConverter = new SecureStringConverter();
		private readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions() { WriteIndented = true };

		public ConfigurationDispatcher()
		{
			SerializerOptions = new JsonSerializerOptions() { WriteIndented = true };
			SerializerOptions.Converters.Add(SecureStringConverter);
		}

		public ConfigurationDispatcher AddConverter(JsonConverter jsonConverter)
		{
			SerializerOptions.Converters.Add(jsonConverter);
			return this;
		}

		public async Task<T> Load<T>(string filePath)
		{
			using FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
			return await Load<T>(stream);
		}

		public async Task<T> Load<T>(Uri uri)
		{
			using WebResponse response = await WebRequest.Create(uri).GetResponseAsync();
			using Stream stream = response.GetResponseStream();
			return await Load<T>(stream);
		}

		public async Task<T> Load<T>(Stream stream) => await JsonSerializer.DeserializeAsync<T>(stream, SerializerOptions);

		public async Task Save<T>(string filePath, T value)
		{
			if (!File.Exists(filePath)) File.Create(filePath).Close();
			using FileStream stream = new FileStream(filePath, FileMode.Truncate, FileAccess.Write);
			await Save(stream, value);
		}

		public async Task Save<T>(Uri uri, T value)
		{
			using Stream stream = await WebRequest.Create(uri).GetRequestStreamAsync();
			await Save(stream, value);
		}

		public async Task Save<T>(Stream stream, T value) => await JsonSerializer.SerializeAsync(stream, value, SerializerOptions);

	}

}