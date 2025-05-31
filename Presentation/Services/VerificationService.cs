using Azure;
using Azure.Communication.Email;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Presentation.Models;
using System.Diagnostics;

namespace Presentation.Services;

public interface IVerificationService
{
    void SaveVerificationCode(SaveVerificationCodeRequest request);
    Task<VerficationServiceResult> SendVerficationCodeAsync(SendVerificationCodeRequest request);
    VerficationServiceResult VerifyVerificationCode(VerifyVerificationCodeRequest request);
}

public class VerificationService(IConfiguration configuration, EmailClient emailClient, IMemoryCache cache) : IVerificationService
{
    
    private readonly IConfiguration _configuration = configuration;
    private readonly EmailClient _emailClient = emailClient;
    private readonly IMemoryCache _cache = cache;
    private static readonly Random _random = new();


    public async Task<VerficationServiceResult> SendVerficationCodeAsync(SendVerificationCodeRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email))
                return new VerficationServiceResult { Succeeded = false, Error = "Recipient email address is required." };

            var verficationCode = _random.Next(100000, 999999).ToString();
            // Send email
            var subject = $"Your verification code is: {verficationCode}";
            var plainTextontent = @$"
                Hello {request.Email},
                To verify your email address, please use the following code:
               {verficationCode}. 
                It is valid for 5 minutes.";

            var htmlContent = @$"<p>Hello {request.Email},</p>
                <p>To verify your email address, please use the following code:</p>
                <h2>{verficationCode}</h2>
                <p>It is valid for 5 minutes.</p>";

            var emailMessage = new EmailMessage(
                senderAddress: _configuration["ACS:SenderAddress"],
                recipients: new EmailRecipients([new(request.Email)]),
                content: new EmailContent(subject)
                {
                    PlainText = plainTextontent,
                    Html = htmlContent
                });
            var emailSendOperation = await _emailClient.SendAsync(WaitUntil.Started, emailMessage);
            SaveVerificationCode(new SaveVerificationCodeRequest
            {
                Email = request.Email,
                Code = verficationCode,
                ValidFor = TimeSpan.FromMinutes(2)
            });
            return new VerficationServiceResult { Succeeded = true, Error = $"Verification email sent successfully." };
        }

        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            return new VerficationServiceResult { Succeeded = false, Error = $"Failed to send verification email." };
        }

    }

    public void SaveVerificationCode(SaveVerificationCodeRequest request)
    {
        // Save the verification code in cache with a 5-minute expiration
        _cache.Set(request.Email.ToLowerInvariant(), request.Code, request.ValidFor);
    }

    public VerficationServiceResult VerifyVerificationCode(VerifyVerificationCodeRequest request)
    {
        var key = request.Email.ToLowerInvariant();

        if (_cache.TryGetValue(key, out string? storedCode))
        {
            if (storedCode == request.Code)
            {
                _cache.Remove(key);
                return new VerficationServiceResult{Succeeded = true,Message = "Verification successfully."};

            }  
        }

        return new VerficationServiceResult { Succeeded = false, Error = "Invalid or expired verification code." };
    }
}

