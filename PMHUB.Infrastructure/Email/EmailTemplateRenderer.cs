using PMHUB.Application.IServices;
using System.Net;
using System.Reflection;

namespace PMHUB.Infrastructure.Services
{
    public class EmailTemplateRenderer : IEmailTemplateRenderer
    {
        private const string LayoutTemplateName = "corporate-layout.html";
        private const string TeLogoUrl = "https://www.te.com/_TEincludes/ver/1696/v2/images/te-connectivity-logo.png";

        private readonly string _templatesFolder;

        public EmailTemplateRenderer()
        {
            _templatesFolder = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? AppContext.BaseDirectory,
                "Email",
                "Templates");
        }

        public async Task<string> RenderCorporateEmailAsync(
            string contentTemplateName,
            string title,
            string preheader,
            IReadOnlyDictionary<string, string?> values)
        {
            var content = await RenderTemplateAsync(contentTemplateName, values);

            return await RenderTemplateAsync(
                LayoutTemplateName,
                new Dictionary<string, string?>
                {
                    ["Title"] = title,
                    ["Preheader"] = preheader,
                    ["TeLogoUrl"] = TeLogoUrl,
                    ["Content"] = content
                },
                rawKeys: new HashSet<string>(StringComparer.Ordinal) { "Content" });
        }

        private async Task<string> RenderTemplateAsync(
            string fileName,
            IReadOnlyDictionary<string, string?> values,
            ISet<string>? rawKeys = null)
        {
            var templatePath = Path.Combine(_templatesFolder, fileName);
            if (!File.Exists(templatePath))
            {
                throw new FileNotFoundException($"Email template not found: {templatePath}");
            }

            var html = await File.ReadAllTextAsync(templatePath);
            foreach (var (key, value) in values)
            {
                var replacement = rawKeys?.Contains(key) == true
                    ? value ?? string.Empty
                    : WebUtility.HtmlEncode(value ?? string.Empty);

                html = html.Replace($"{{{{{key}}}}}", replacement, StringComparison.Ordinal);
            }

            return html;
        }
    }
}
