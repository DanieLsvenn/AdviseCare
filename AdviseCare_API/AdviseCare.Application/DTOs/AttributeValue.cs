namespace AdviseCare.Application.DTOs
{
    // One plain value to encrypt for a student's data attribute; RowId and FieldName say exactly where it will be stored
    public class PlainAttributeValue
    {
        public Guid StudentId { get; set; } = Guid.Empty;
        public Guid DataAttributeId { get; set; } = Guid.Empty;
        // STUDENTDATA.studentDataId or CORRECTIONREQUESTS.correctionRequestId of the row the value goes into
        public Guid RowId { get; set; } = Guid.Empty;
        // dataValue, currentValue or proposedValue
        public string FieldName { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    // The encrypted form of a PlainAttributeValue, in the same order: the key that encrypted it and the bytes to store
    public class EncryptedAttributeValue
    {
        public Guid AttributeKeyId { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public Guid DataAttributeId { get; set; } = Guid.Empty;
        public Guid RowId { get; set; } = Guid.Empty;
        public string FieldName { get; set; } = string.Empty;
        // Null for a value cleared by an erasure
        public byte[]? Payload { get; set; }
    }

    // Result of a decryption: the value, or IsErased = true when it was cleared by an erasure
    public class DecryptedAttributeValue
    {
        public Guid RowId { get; set; } = Guid.Empty;
        public string FieldName { get; set; } = string.Empty;
        public string? Value { get; set; }
        public bool IsErased { get; set; } = false;
    }
}
