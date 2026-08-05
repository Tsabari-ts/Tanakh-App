using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Tanakh.Domain;
using Tanakh.Domain.Entities;
using Tanakh.Domain.Validation;
using Tanakh.Infrastructure.Data;
using Tanakh.Infrastructure.Options;

namespace Tanakh.Infrastructure.Services
{
    public class Sms4FreeSmsSender : ISmsSender
    {
        // SMS4FREE status codes (see Backend/README.md for the full table):
        //   >0  sent (recipient count)     0  general error (transient)
        //   -1  bad key/user/pass          -2  bad sender        -6 sender not verified
        //   -3  no recipients found        -5  message rejected
        //   -4  balance too low
        // -1/-2/-6 are account/config problems, not this message's fault -
        // retrying the same request won't help until the config is fixed,
        // but we still let the normal retry/backoff run (in case someone
        // fixes it before the next attempt) rather than building a separate
        // "halt the whole dispatcher" path.
        private static readonly int[] PermanentFailureCodes = { -3, -5 };
        private const int LowBalanceCode = -4;

        private readonly HttpClient httpClient;
        private readonly SmsOptions options;
        private readonly ILogger<Sms4FreeSmsSender> logger;
        private readonly AppDbContext dbContext;

        public Sms4FreeSmsSender(
            HttpClient httpClient, IOptions<SmsOptions> options, ILogger<Sms4FreeSmsSender> logger, AppDbContext dbContext)
        {
            this.httpClient = httpClient;
            this.options = options.Value;
            this.logger = logger;
            this.dbContext = dbContext;
        }

        public async Task<SmsSendResult> SendAsync(
            string phoneNumberE164, string message, SmsMessageType type, CancellationToken cancellationToken = default)
        {
            string maskedPhone = IsraeliMobilePhoneValidator.MaskForLogging(phoneNumberE164);

            if (options.DryRun)
            {
                logger.LogInformation(
                    "SMS dry-run: would send {Length}-char message to {Phone}.",
                    message.Length, maskedPhone);
                SmsSendResult dryRunResult = new(Success: true, IsPermanentFailure: false, IsLowBalance: false, StatusCode: 1, RawResponse: "dry-run");
                await LogAsync(phoneNumberE164, message, type, dryRunResult, cancellationToken);
                return dryRunResult;
            }

            Sms4FreeRequest request = new(options.Key, options.User, options.Pass, options.Sender, phoneNumberE164, message);

            try
            {
                using HttpResponseMessage response = await httpClient.PostAsJsonAsync(options.ApiUrl, request, cancellationToken);
                string rawBody = await response.Content.ReadAsStringAsync(cancellationToken);

                Sms4FreeResponse? parsed = System.Text.Json.JsonSerializer.Deserialize<Sms4FreeResponse>(rawBody);
                int statusCode = parsed?.Status ?? 0;

                bool success = statusCode > 0;
                bool isPermanent = Array.IndexOf(PermanentFailureCodes, statusCode) >= 0;
                bool isLowBalance = statusCode == LowBalanceCode;

                if (!success)
                {
                    // -1/-2/-6 (bad credentials/sender/verification) and -4
                    // (balance) are account-level problems a human needs to
                    // act on - not this particular message's fault. 0/-3/-5
                    // are per-message noise, expected occasionally.
                    LogLevel severity = statusCode is -1 or -2 or -4 or -6 ? LogLevel.Critical : LogLevel.Warning;
                    logger.Log(severity, "SMS send to {Phone} failed with SMS4FREE status {Status}: {Message}",
                        maskedPhone, statusCode, parsed?.Message);
                }

                SmsSendResult result = new(success, isPermanent, isLowBalance, statusCode, rawBody);
                await LogAsync(phoneNumberE164, message, type, result, cancellationToken);
                return result;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                logger.LogWarning(ex, "SMS send to {Phone} failed at the transport level (timeout/network).", maskedPhone);
                SmsSendResult transportFailureResult = new(Success: false, IsPermanentFailure: false, IsLowBalance: false, StatusCode: 0, RawResponse: ex.Message);
                await LogAsync(phoneNumberE164, message, type, transportFailureResult, cancellationToken);
                return transportFailureResult;
            }
        }

        private async Task LogAsync(
            string phoneNumberE164, string message, SmsMessageType type, SmsSendResult result, CancellationToken cancellationToken)
        {
            try
            {
                await dbContext.SmsLogs.AddAsync(new SmsLog
                {
                    Id = Guid.CreateVersion7(),
                    ToPhoneNumber = phoneNumberE164,
                    Type = type,
                    Message = message,
                    Success = result.Success,
                    ProviderResponse = result.RawResponse,
                    ProviderStatusCode = result.StatusCode,
                }, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                // Logging the send must never be why the send itself
                // "fails" from the caller's perspective - the real SMS
                // outcome (result) has already been determined above.
                logger.LogError(ex, "Failed to write sms_log row for a {Type} send.", type);
            }
        }

        private record Sms4FreeRequest(
            [property: JsonPropertyName("key")] string Key,
            [property: JsonPropertyName("user")] string User,
            [property: JsonPropertyName("pass")] string Pass,
            [property: JsonPropertyName("sender")] string Sender,
            [property: JsonPropertyName("recipient")] string Recipient,
            [property: JsonPropertyName("msg")] string Msg);

        private record Sms4FreeResponse(
            [property: JsonPropertyName("status")] int Status,
            [property: JsonPropertyName("message")] string? Message);
    }
}
