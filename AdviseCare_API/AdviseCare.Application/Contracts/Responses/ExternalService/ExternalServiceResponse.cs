namespace AdviseCare.Application.Contracts.Responses.ExternalService
{
    public class GetAllExternalServiceResponse
    {
        public Guid ExternalServiceId { get; set; }
        public string ContactInfo { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public string ServiceType { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class GetAllExternalServiceWithPaginationResponse : GetAllExternalServiceResponse
    {
    }

    public class GetExternalServiceByIdResponse : GetAllExternalServiceResponse
    {
    }

    public class SearchExternalServiceResponse : GetAllExternalServiceResponse
    {
    }

    public class CreateExternalServiceResponse : GetAllExternalServiceResponse
    {
    }

    public class UpdateExternalServiceResponse : GetAllExternalServiceResponse
    {
    }
}
