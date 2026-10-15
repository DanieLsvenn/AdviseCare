namespace AdviseCare.Application.Exceptions
{
    public abstract class AppException : Exception { protected AppException(string m) : base(m) { } }
    public class NotFoundException : AppException { public NotFoundException(string m) : base(m) { } }
    public class ValidationException : AppException { public ValidationException(string m) : base(m) { } }
    public class ConflictException : AppException { public ConflictException(string m) : base(m) { } }
    public class ForbiddenException : AppException { public ForbiddenException(string m) : base(m) { } }
    public class UnauthorizedException : AppException { public UnauthorizedException(string m) : base(m) { } }
}
