using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CoreUtility.Configuration
{

  // TODO: A conversion pattern with much higher security is necessary.

  public class SecureStringConverter : JsonConverter<SecureString?>
  {

    public byte[]? Entropy { get; set; }
    public DataProtectionScope DataProtectionScope { get; set; } = DataProtectionScope.LocalMachine;

    private const string EncryptionPropertyName = "IsEncrypted";
    private const string ValuePropertyName = "Value";

    public override SecureString? Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
    {
      bool isEncrypted = false;

      while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
      {
        string propertyName = Encoding.UTF8.GetString(reader.ValueSpan);
        reader.Read();

        switch (propertyName)
        {
          case EncryptionPropertyName:
            isEncrypted = reader.GetBoolean();
            break;

          case ValuePropertyName:
            SecureString result = new SecureString();

            if (isEncrypted)
            {
              char[] decoded = Encoding.Unicode.GetChars(ProtectedData.Unprotect(
                reader.GetBytesFromBase64(), Entropy, DataProtectionScope));

              for (int i = 0; i < decoded.Length; i++) result.AppendChar(decoded[i]);
            }
            else
            {
              string s = reader.GetString();
              for (int i = 0; i < s.Length; i++) result.AppendChar(s[i]);
            }

            result.MakeReadOnly();
            reader.Read();
            return result;
          default:
            Skip(ref reader);
            break;
        }
      }

      return null;
    }

    private static void Skip(ref Utf8JsonReader reader)
    {
      if (reader.TokenType == JsonTokenType.PropertyName) reader.Read();

      if (reader.TokenType == JsonTokenType.StartObject || reader.TokenType == JsonTokenType.StartArray)
      {
        int depth = reader.CurrentDepth;
        while (reader.Read() && depth <= reader.CurrentDepth) { }
      }
    }

    public override void Write(Utf8JsonWriter writer, SecureString? value, JsonSerializerOptions options)
    {
      if (value == null) throw new ArgumentNullException(nameof(value));
      IntPtr pointer = IntPtr.Zero;

      try
      {
        pointer = Marshal.SecureStringToGlobalAllocUnicode(value);

        writer.WriteStartObject();
        writer.WriteBoolean(EncryptionPropertyName, true);
        writer.WriteBase64String(ValuePropertyName, ProtectedData.Protect(Encoding.Unicode.GetBytes(Marshal.PtrToStringUni(pointer) ?? string.Empty),
          Entropy, DataProtectionScope));
        writer.WriteEndObject();
      }
      finally
      {
        Marshal.ZeroFreeGlobalAllocUnicode(pointer);
      }
    }

  }

}