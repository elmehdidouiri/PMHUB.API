namespace PMHUB.Application.Exceptions

{
    public class ValidationException : Exception
    {
        public IDictionary<string, string[]> Errors { get; }

        public ValidationException()
            : base("Une ou plusieurs erreurs de validation se sont produites.")
        {
            Errors = new Dictionary<string, string[]>();
        }

        public ValidationException(IDictionary<string, string[]> errors)
            : base("Une ou plusieurs erreurs de validation se sont produites.")
        {
            Errors = errors;
        }

        public ValidationException(string field, string error)
            : base("Une ou plusieurs erreurs de validation se sont produites.")
        {
            Errors = new Dictionary<string, string[]>
            {
                { field, new[] { error } }
            };
        }
    }
}
