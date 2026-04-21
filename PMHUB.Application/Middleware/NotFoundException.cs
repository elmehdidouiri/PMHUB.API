namespace PMHUB.Application.Exceptions
{
    public class NotFoundException : Exception
    {
        public NotFoundException(string name, object key)
            : base($"'{name}' with identifier '{key}' was not found.") { }

        public NotFoundException(string message)
            : base(message) { }
    }
}
