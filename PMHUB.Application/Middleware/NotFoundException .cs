namespace PMHUB.Application.Exceptions
{
    public class NotFoundException : Exception
    {
        public NotFoundException(string name, object key)
            : base($"'{name}' avec l'identifiant '{key}' est introuvable.") { }

        public NotFoundException(string message)
            : base(message) { }
    }
}
