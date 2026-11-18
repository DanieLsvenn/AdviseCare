namespace AdviseCare.Application.Contracts.Responses
{
    // Body of every error answer (400/401/403/404/409/500) written by the exception handler; shown in Swagger for each route (Q-123)
    public class ErrorResponse
    {
        public string Message { get; set; } = string.Empty;
    }
}
