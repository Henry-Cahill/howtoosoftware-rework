using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using HowToSoftware.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace HowToSoftware.Infrastructure.Services;

public class ActivityPubHttpClient(HttpClient httpClient, ILogger<ActivityPubHttpClient> logger) : IActivityPubHttpClient
{
    private const string ActivityPubMediaType = "application/activity+json";

    public async Task<string?> FetchActorAsync(string apId, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, apId);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(ActivityPubMediaType));

            using var response = await httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Failed to fetch actor {ApId}: {StatusCode}", apId, response.StatusCode);
                return null;
            }

            return await response.Content.ReadAsStringAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error fetching actor {ApId}", apId);
            return null;
        }
    }

    public async Task<bool> DeliverAsync(string inboxUrl, string activity, string actorKeyId, string privateKeyPem, CancellationToken ct = default)
    {
        try
        {
            var body = Encoding.UTF8.GetBytes(activity);
            var digest = Convert.ToBase64String(SHA256.HashData(body));

            var uri = new Uri(inboxUrl);
            var date = DateTime.UtcNow.ToString("R");
            var host = uri.Host;
            if (uri.Port is not 80 and not 443)
                host += $":{uri.Port}";

            var signatureString = $"(request-target): post {uri.AbsolutePath}\n" +
                                  $"host: {host}\n" +
                                  $"date: {date}\n" +
                                  $"digest: SHA-256={digest}";

            var signature = SignRsa(signatureString, privateKeyPem);

            var headerValue = $"keyId=\"{actorKeyId}\",algorithm=\"rsa-sha256\",headers=\"(request-target) host date digest\",signature=\"{signature}\"";
            using var request = new HttpRequestMessage(HttpMethod.Post, inboxUrl);
            request.Content = new ByteArrayContent(body);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue(ActivityPubMediaType);
            request.Headers.Add("Date", date);
            request.Headers.Add("Host", host);
            request.Headers.Add("Digest", $"SHA-256={digest}");
            request.Headers.Add("Signature", headerValue);

            using var response = await httpClient.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning("Delivery to {InboxUrl} failed: {StatusCode} {Body}", inboxUrl, response.StatusCode, responseBody);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error delivering to {InboxUrl}", inboxUrl);
            return false;
        }
    }

    private static string SignRsa(string data, string privateKeyPem)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(privateKeyPem);
        var bytes = Encoding.UTF8.GetBytes(data);
        var signed = rsa.SignData(bytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return Convert.ToBase64String(signed);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
