using PMHUB.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMHUB.Application.IServices
{
    public interface IEmailService
    {
        Task SendApprovalEmailAsync(string toEmail, string firstName);
        Task SendRejectionEmailAsync(string toEmail, string firstName);
        Task SendPasswordResetCodeAsync(string toEmail, string firstName, string code, int expiresInMinutes);
    }
}
