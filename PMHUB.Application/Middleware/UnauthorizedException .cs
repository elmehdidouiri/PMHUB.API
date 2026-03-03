namespace PMHUB.Application.Exceptions
{
    public class UnauthorizedException : Exception
    {
        public UnauthorizedException()
            : base("Authentification requise pour accéder à cette ressource.") { }

        public UnauthorizedException(string message)
            : base(message) { }
    }
}
