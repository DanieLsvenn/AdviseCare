namespace AdviseCare.Application.Contracts.Requests.ErasureRequest
{
    public class SearchErasureRequestRequest
    {
        public Guid? ConsentId { get; set; }
        public Guid? ExecutedBy { get; set; }
        public Guid? StudentId { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
