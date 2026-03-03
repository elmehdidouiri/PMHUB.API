namespace PMHUB.Application.Exceptions
{
    public class ConflictException : Exception
    {
        public ConflictException(string message)
            : base(message) { }

        public ConflictException(string name, object key)
            : base($"'{name}' avec la valeur '{key}' existe déjà.") { }
    }
}
