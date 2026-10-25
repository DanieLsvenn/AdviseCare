namespace AdviseCare.Application.Contracts.Requests.AttributeKey
{
    public class SearchAttributeKeyRequest
    {
        public Guid? DataAttributeId { get; set; }
        public Guid? StudentId { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
