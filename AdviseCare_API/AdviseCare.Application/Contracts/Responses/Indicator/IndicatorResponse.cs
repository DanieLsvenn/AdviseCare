namespace AdviseCare.Application.Contracts.Responses.Indicator
{
    public class GetAllIndicatorResponse
    {
        public Guid IndicatorId { get; set; } = Guid.Empty;
        public Guid DataAttributeId { get; set; } = Guid.Empty;
        public string IndicatorName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DataType { get; set; } = "numeric";
        public string Status { get; set; } = "active";
        public string AttributeName { get; set; } = string.Empty;
        public string AttributeDescription { get; set; } = string.Empty;
    }

    public class GetAllIndicatorWithPaginationResponse
    {
        public Guid IndicatorId { get; set; } = Guid.Empty;
        public Guid DataAttributeId { get; set; } = Guid.Empty;
        public string IndicatorName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DataType { get; set; } = "numeric";
        public string Status { get; set; } = "active";
        public string AttributeName { get; set; } = string.Empty;
        public string AttributeDescription { get; set; } = string.Empty;
    }

    public class GetIndicatorByIdResponse
    {
        public Guid IndicatorId { get; set; } = Guid.Empty;
        public Guid DataAttributeId { get; set; } = Guid.Empty;
        public string IndicatorName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DataType { get; set; } = "numeric";
        public string Status { get; set; } = "active";
        public string AttributeName { get; set; } = string.Empty;
        public string AttributeDescription { get; set; } = string.Empty;
    }

    public class SearchIndicatorResponse
    {
        public Guid IndicatorId { get; set; } = Guid.Empty;
        public Guid DataAttributeId { get; set; } = Guid.Empty;
        public string IndicatorName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DataType { get; set; } = "numeric";
        public string Status { get; set; } = "active";
        public string AttributeName { get; set; } = string.Empty;
        public string AttributeDescription { get; set; } = string.Empty;
    }

    public class CreateIndicatorResponse
    {
        public Guid IndicatorId { get; set; } = Guid.Empty;
        public Guid DataAttributeId { get; set; } = Guid.Empty;
        public string IndicatorName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DataType { get; set; } = "numeric";
        public string Status { get; set; } = "active";
        public string AttributeName { get; set; } = string.Empty;
        public string AttributeDescription { get; set; } = string.Empty;
    }

    public class UpdateIndicatorResponse
    {
        public Guid IndicatorId { get; set; } = Guid.Empty;
        public Guid DataAttributeId { get; set; } = Guid.Empty;
        public string IndicatorName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DataType { get; set; } = "numeric";
        public string Status { get; set; } = "active";
        public string AttributeName { get; set; } = string.Empty;
        public string AttributeDescription { get; set; } = string.Empty;
    }

    public class ArchiveIndicatorResponse
    {
        public Guid IndicatorId { get; set; } = Guid.Empty;
        public Guid DataAttributeId { get; set; } = Guid.Empty;
        public string Status { get; set; } = "archived";
        public string Message { get; set; } = string.Empty;
    }
}
