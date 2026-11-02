namespace AdviseCare.Application.Contracts.Responses.ExternalService
{
    public class GetAllExternalServiceResponse
    {
        public Guid ExternalServiceId { get; set; } = Guid.Empty;
        public string ContactInfo { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public string ServiceType { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    public class GetAllExternalServiceWithPaginationResponse
    {
        public Guid ExternalServiceId { get; set; } = Guid.Empty;
        public string ContactInfo { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public string ServiceType { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    public class GetExternalServiceByIdResponse
    {
        public Guid ExternalServiceId { get; set; } = Guid.Empty;
        public string ContactInfo { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public string ServiceType { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    public class SearchExternalServiceResponse
    {
        public Guid ExternalServiceId { get; set; } = Guid.Empty;
        public string ContactInfo { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public string ServiceType { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    public class CreateExternalServiceResponse
    {
        public Guid ExternalServiceId { get; set; } = Guid.Empty;
        public string ContactInfo { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public string ServiceType { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    public class UpdateExternalServiceResponse
    {
        public Guid ExternalServiceId { get; set; } = Guid.Empty;
        public string ContactInfo { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public string ServiceType { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}
