namespace AdviseCare.Application.Contracts.Responses.DataAttribute
{
    public class GetAllDataAttributeResponse
    {
        public Guid DataAttributeId { get; set; } = Guid.Empty;
        public string AttributeDescription { get; set; } = string.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string DataType { get; set; } = "numeric";
        public string Status { get; set; } = "active";
    }

    public class GetAllDataAttributeWithPaginationResponse
    {
        public Guid DataAttributeId { get; set; } = Guid.Empty;
        public string AttributeDescription { get; set; } = string.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string DataType { get; set; } = "numeric";
        public string Status { get; set; } = "active";
    }

    public class GetDataAttributeByIdResponse
    {
        public Guid DataAttributeId { get; set; } = Guid.Empty;
        public string AttributeDescription { get; set; } = string.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string DataType { get; set; } = "numeric";
        public string Status { get; set; } = "active";
    }

    public class SearchDataAttributeResponse
    {
        public Guid DataAttributeId { get; set; } = Guid.Empty;
        public string AttributeDescription { get; set; } = string.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string DataType { get; set; } = "numeric";
        public string Status { get; set; } = "active";
    }

    public class CreateDataAttributeResponse
    {
        public Guid DataAttributeId { get; set; } = Guid.Empty;
        public string AttributeDescription { get; set; } = string.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string DataType { get; set; } = "numeric";
        public string Status { get; set; } = "active";
    }

    public class UpdateDataAttributeResponse
    {
        public Guid DataAttributeId { get; set; } = Guid.Empty;
        public string AttributeDescription { get; set; } = string.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string DataType { get; set; } = "numeric";
        public string Status { get; set; } = "active";
    }
}
