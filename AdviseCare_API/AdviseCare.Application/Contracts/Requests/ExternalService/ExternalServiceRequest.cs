namespace AdviseCare.Application.Contracts.Requests.ExternalService
{
    public class SearchExternalServiceRequest
    {
        public string ServiceName { get; set; } = string.Empty;
        public string ServiceType { get; set; } = string.Empty;
        public bool? IsActive { get; set; }
    }
}
