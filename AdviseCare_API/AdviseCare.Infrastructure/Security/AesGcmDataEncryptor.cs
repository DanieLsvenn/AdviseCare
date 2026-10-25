using AdviseCare.Application.Interfaces.Security;
using AdviseCare.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace AdviseCare.Infrastructure.Security
{
    // AES-256-GCM (built into .NET, no package). Every payload is base64(nonce 12 bytes | tag 16 bytes | ciphertext)
    public class AesGcmDataEncryptor : IDataEncryptor
    {
        private const int KeySize = 32;
        private const int NonceSize = 12;
        private const int TagSize = 16;
        // Associated data for wrapping, so a wrapped attribute key can only be unwrapped as an attribute key
        private static readonly byte[] KeyWrapContext = Encoding.UTF8.GetBytes("AdviseCare:attribute-key:v1");

        private readonly byte[] _masterKey;

        public AesGcmDataEncryptor(IOptions<EncryptionSettings> options)
        {
            if (!EncryptionSettings.IsValidMasterKey(options.Value.MasterKey))
                throw new InvalidOperationException("Encryption:MasterKey must be 32 random bytes in base64.");
            _masterKey = Convert.FromBase64String(options.Value.MasterKey);
        }

        public string CreateWrappedKey()
        {
            var dataKey = RandomNumberGenerator.GetBytes(KeySize);
            try
            {
                return Seal(_masterKey, dataKey, KeyWrapContext);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(dataKey);
            }
        }

        public string Encrypt(
            string wrappedKey,
            string plaintext,
            string associatedData)
        {
            var dataKey = Open(_masterKey, wrappedKey, KeyWrapContext);
            try
            {
                return Seal(dataKey, Encoding.UTF8.GetBytes(plaintext), Encoding.UTF8.GetBytes(associatedData));
            }
            finally
            {
                CryptographicOperations.ZeroMemory(dataKey);
            }
        }

        public string Decrypt(
            string wrappedKey,
            string payload,
            string associatedData)
        {
            var dataKey = Open(_masterKey, wrappedKey, KeyWrapContext);
            try
            {
                return Encoding.UTF8.GetString(Open(dataKey, payload, Encoding.UTF8.GetBytes(associatedData)));
            }
            finally
            {
                CryptographicOperations.ZeroMemory(dataKey);
            }
        }

        // Helper: encrypt with a fresh random nonce and return base64(nonce | tag | ciphertext)
        private static string Seal(byte[] key, byte[] plaintext, byte[] associatedData)
        {
            var output = new byte[NonceSize + TagSize + plaintext.Length];
            var nonce = output.AsSpan(0, NonceSize);
            var tag = output.AsSpan(NonceSize, TagSize);
            var ciphertext = output.AsSpan(NonceSize + TagSize);
            RandomNumberGenerator.Fill(nonce);

            using var aes = new AesGcm(key, TagSize);
            aes.Encrypt(nonce, plaintext, ciphertext, tag, associatedData);
            return Convert.ToBase64String(output);
        }

        // Helper: split base64(nonce | tag | ciphertext) and decrypt it; throws CryptographicException when anything was changed
        private static byte[] Open(byte[] key, string payload, byte[] associatedData)
        {
            byte[] input;
            try
            {
                input = Convert.FromBase64String(payload);
            }
            catch (FormatException exception)
            {
                throw new CryptographicException("The encrypted value is not valid base64.", exception);
            }
            if (input.Length < NonceSize + TagSize)
                throw new CryptographicException("The encrypted value is too short.");

            var plaintext = new byte[input.Length - NonceSize - TagSize];
            using var aes = new AesGcm(key, TagSize);
            aes.Decrypt(input.AsSpan(0, NonceSize), input.AsSpan(NonceSize + TagSize), input.AsSpan(NonceSize, TagSize), plaintext, associatedData);
            return plaintext;
        }
    }
}
