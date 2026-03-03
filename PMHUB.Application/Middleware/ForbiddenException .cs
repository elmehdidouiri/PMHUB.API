namespace PMHUB.Application.Exceptions
{
    public class ForbiddenException : Exception
    {
        public ForbiddenException()
            : base("Vous n'avez pas la permission d'effectuer cette action.") { }

        public ForbiddenException(string message)
            : base(message) { }
    }
}
