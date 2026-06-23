namespace PMHUB.Application.IServices
{
    public interface IEmailTemplateRenderer
    {
        Task<string> RenderCorporateEmailAsync(
            string contentTemplateName,
            string title,
            string preheader,
            IReadOnlyDictionary<string, string?> values);
    }
}
