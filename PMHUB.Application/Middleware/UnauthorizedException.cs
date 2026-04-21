namespace PMHUB.Application.Exceptions
{
    public class UnauthorizedException : Exception
    {
        public UnauthorizedException()
            : base("Authentication is required to access this resource.") { }

        public UnauthorizedException(string message)
            : base(message) { }
    }
}
