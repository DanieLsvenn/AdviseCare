namespace AdviseCare.Application.Interfaces.Security
{
    // Envelope encryption: every data key is stored wrapped by the master key, and is only unwrapped in memory to encrypt or decrypt one value
    public interface IDataEncryptor
    {
        // Creates a new random data key and returns it wrapped by the master key (base64), ready for ATTRIBUTEKEYS.wrappedKey
        string CreateWrappedKey();

        // Encrypts a value with a wrapped data key; associatedData ties the result to its owner, so it cannot be moved onto another row
        string Encrypt(
            string wrappedKey,
            string plaintext,
            string associatedData);

        // Decrypts a value; throws CryptographicException when the key, the associated data or the value does not match
        string Decrypt(
            string wrappedKey,
            string payload,
            string associatedData);
    }
}
