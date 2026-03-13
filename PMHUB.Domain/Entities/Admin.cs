namespace PMHUB.Domain.Entities
{
    public class Admin : User
    {
        public DateTime? LastLoginAt { get; set; }
    }
}