using AdviseCare.Application.Interfaces.Security;
using AdviseCare.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace AdviseCare.Infrastructure.Security
{
    // AES-256-GCM (built into .NET, no package). A wrapped key is base64(nonce 12 | tag 16 | ciphertext); a value is (format 1 | nonce 12 | tag 16 | ciphertext)
    public class AesGcmDataEncryptor : IDataEncryptor
    {
        private const int KeySize = 32;
        private const int NonceSize = 12;
        private const int TagSize = 16;
        // First byte of every stored value, so a later format can be told apart
        private const byte ValueFormatVersion = 1;
        // HKDF info prefix: each attribute key row gets its own wrapping key derived from the master key and the row's ids
        private const string WrapContextPrefix = "AdviseCare:wrap:v1:";

        private readonly Dictionary<int, byte[]> _masterKeys;

        public AesGcmDataEncryptor(IOptions<EncryptionSettings> options)
        {
            if (!EncryptionSettings.IsValid(options.Value))
                throw new InvalidOperationException("Encryption:MasterKeys must hold numbered versions of 32 random bytes in base64, and Encryption:CurrentVersion must be one of them.");
            _masterKeys = options.Value.MasterKeys.ToDictionary(k => int.Parse(k.Key), k => Convert.FromBase64String(k.Value));
            CurrentMasterKeyVersion = options.Value.CurrentVersion;
        }

        public int CurrentMasterKeyVersion { get; }

        public IReadOnlyCollection<int> MasterKeyVersions => _masterKeys.Keys.ToList();

        public string CreateWrappedKey(string KeyContext)
        {
            var dataKey = RandomNumberGenerator.GetBytes(KeySize);
            try
            {
                return WrapKey(dataKey, CurrentMasterKeyVersion, KeyContext);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(dataKey);
            }
        }

        public string RewrapKey(string WrappedKey, int MasterKeyVersion, string KeyContext)
        {
            var dataKey = UnwrapKey(WrappedKey, MasterKeyVersion, KeyContext);
            try
            {
                return WrapKey(dataKey, CurrentMasterKeyVersion, KeyContext);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(dataKey);
            }
        }

        public byte[] Encrypt(string WrappedKey, int MasterKeyVersion, string KeyContext, string Plaintext, string AssociatedData)
        {
            var dataKey = UnwrapKey(WrappedKey, MasterKeyVersion, KeyContext);
            try
            {
                var sealedValue = Seal(dataKey, Encoding.UTF8.GetBytes(Plaintext), Encoding.UTF8.GetBytes(AssociatedData));
                var payload = new byte[sealedValue.Length + 1];
                payload[0] = ValueFormatVersion;
                sealedValue.CopyTo(payload, 1);
                return payload;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(dataKey);
            }
        }

        public string Decrypt(string WrappedKey, int MasterKeyVersion, string KeyContext, byte[] Payload, string AssociatedData)
        {
            if (Payload == null || Payload.Length < 1 + NonceSize + TagSize || Payload[0] != ValueFormatVersion)
                throw new CryptographicException("The encrypted value is not in a known format.");

            var dataKey = UnwrapKey(WrappedKey, MasterKeyVersion, KeyContext);
            try
            {
                return Encoding.UTF8.GetString(Open(dataKey, Payload.AsSpan(1), Encoding.UTF8.GetBytes(AssociatedData)));
            }
            finally
            {
                CryptographicOperations.ZeroMemory(dataKey);
            }
        }

        // Helper: wrap a data key with the wrapping key derived for this row, under one master key version
        private string WrapKey(byte[] dataKey, int masterKeyVersion, string keyContext)
        {
            var wrappingKey = DeriveWrappingKey(masterKeyVersion, keyContext);
            try
            {
                return Convert.ToBase64String(Seal(wrappingKey, dataKey, Encoding.UTF8.GetBytes(WrapContextPrefix + keyContext)));
            }
            finally
            {
                CryptographicOperations.ZeroMemory(wrappingKey);
            }
        }

        // Helper: unwrap a data key; a wrapped key copied onto another row fails here because the derived key and the associated data differ
        private byte[] UnwrapKey(string wrappedKey, int masterKeyVersion, string keyContext)
        {
            if (string.IsNullOrEmpty(wrappedKey))
                throw new CryptographicException("The attribute key has no wrapped key (it was destroyed).");

            byte[] input;
            try
            {
                input = Convert.FromBase64String(wrappedKey);
            }
            catch (FormatException exception)
            {
                throw new CryptographicException("The wrapped key is not valid base64.", exception);
            }

            var wrappingKey = DeriveWrappingKey(masterKeyVersion, keyContext);
            try
            {
                return Open(wrappingKey, input, Encoding.UTF8.GetBytes(WrapContextPrefix + keyContext));
            }
            finally
            {
                CryptographicOperations.ZeroMemory(wrappingKey);
            }
        }

        // Helper: HKDF-SHA256(master key of that version, info = row context) -> 32-byte wrapping key for one attribute key row
        private byte[] DeriveWrappingKey(int masterKeyVersion, string keyContext)
        {
            if (!_masterKeys.TryGetValue(masterKeyVersion, out var masterKey))
                throw new CryptographicException($"Master key version {masterKeyVersion} is not in the configuration.");
            return HKDF.DeriveKey(HashAlgorithmName.SHA256, masterKey, KeySize, info: Encoding.UTF8.GetBytes(WrapContextPrefix + keyContext));
        }

        // Helper: encrypt with a fresh random nonce and return (nonce | tag | ciphertext)
        private static byte[] Seal(byte[] key, byte[] plaintext, byte[] associatedData)
        {
            var output = new byte[NonceSize + TagSize + plaintext.Length];
            var nonce = output.AsSpan(0, NonceSize);
            var tag = output.AsSpan(NonceSize, TagSize);
            var ciphertext = output.AsSpan(NonceSize + TagSize);
            RandomNumberGenerator.Fill(nonce);

            using var aes = new AesGcm(key, TagSize);
            aes.Encrypt(nonce, plaintext, ciphertext, tag, associatedData);
            return output;
        }

        // Helper: split (nonce | tag | ciphertext) and decrypt it; throws CryptographicException when anything was changed
        private static byte[] Open(byte[] key, ReadOnlySpan<byte> input, byte[] associatedData)
        {
            if (input.Length < NonceSize + TagSize)
                throw new CryptographicException("The encrypted value is too short.");

            var plaintext = new byte[input.Length - NonceSize - TagSize];
            using var aes = new AesGcm(key, TagSize);
            aes.Decrypt(input.Slice(0, NonceSize), input.Slice(NonceSize + TagSize), input.Slice(NonceSize, TagSize), plaintext, associatedData);
            return plaintext;
        }
    }
}
