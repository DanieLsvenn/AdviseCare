namespace AdviseCare.Application.DTOs
{
    // One plain value to encrypt for a student's data attribute (Member C's indicator import)
    public class PlainAttributeValue
    {
        public Guid StudentId { get; set; } = Guid.Empty;
        public Guid DataAttributeId { get; set; } = Guid.Empty;
        public string Value { get; set; } = string.Empty;
    }

    // One stored value to decrypt (STUDENTINDICATORS.indicatorValue, format v1:{attributeKeyId}:{payload})
    public class EncryptedAttributeValue
    {
        public Guid StudentId { get; set; } = Guid.Empty;
        public Guid DataAttributeId { get; set; } = Guid.Empty;
        public string CipherText { get; set; } = string.Empty;
    }

    // Result of a decryption: the value, or IsErased = true when its key was destroyed by an erasure
    public class DecryptedAttributeValue
    {
        public Guid StudentId { get; set; } = Guid.Empty;
        public Guid DataAttributeId { get; set; } = Guid.Empty;
        public string? Value { get; set; }
        public bool IsErased { get; set; } = false;
    }
}
