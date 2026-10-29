namespace AdviseCare.Application.Interfaces.Security
{
    // Envelope encryption: every data key is stored wrapped by a master key version, and is only unwrapped in memory to encrypt or decrypt one value.
    // KeyContext ties a wrapped key to its ATTRIBUTEKEYS row; AssociatedData ties an encrypted value to its key, student, attribute, row and field
    public interface IDataEncryptor
    {
        // The master key version new and rewrapped keys are wrapped with
        int CurrentMasterKeyVersion { get; }

        // Every master key version found in the configuration
        IReadOnlyCollection<int> MasterKeyVersions { get; }

        // Creates a new random data key and returns it wrapped under the current master key version (base64), ready for ATTRIBUTEKEYS.wrappedKey
        string CreateWrappedKey(string KeyContext);

        // Unwraps a data key with its old master key version and wraps it again under the current version; the data key itself does not change
        string RewrapKey(string WrappedKey, int MasterKeyVersion, string KeyContext);

        // Encrypts a value; the result is stored as VARBINARY (format version + nonce + tag + ciphertext)
        byte[] Encrypt(string WrappedKey, int MasterKeyVersion, string KeyContext, string Plaintext, string AssociatedData);

        // Decrypts a value; throws CryptographicException when the key, the associated data or the value does not match
        string Decrypt(string WrappedKey, int MasterKeyVersion, string KeyContext, byte[] Payload, string AssociatedData);
    }
}
