#if AGE_API
using System;
using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

//	To enable: define compilation symbol AGE_API and call AgeVerificationApi.StartAsync(port) from your WPF app if needed.


namespace ImageAndMp4WebBuilder.AgeVerification
{
    // Optional, self-hosted minimal API for age verification scaffolding.
    // Enable by adding the conditional compilation symbol AGE_API to the project.
    // Example usage from WPF: await AgeVerificationApi.StartAsync(5055);
    public static class AgeVerificationApi
    {
        private static readonly ConcurrentDictionary<string, bool> _ageStatus = new();

        public static WebApplication Build(int port = 5055)
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                Args = Array.Empty<string>(),
                ApplicationName = typeof(AgeVerificationApi).Assembly.FullName,
                ContentRootPath = AppContext.BaseDirectory
            });

            builder.Services.AddRouting();
            builder.Services.AddCors(o =>
            {
                o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
            });

            var app = builder.Build();
            app.Urls.Add($"http://localhost:{port}");

            app.UseCors();

            // POST /vc/presentation-request -> returns request payload/QR data (scaffold)
            app.MapPost("/vc/presentation-request", (HttpRequest req) =>
            {
                var requestId = Guid.NewGuid().ToString("N");
                _ageStatus[requestId] = false;

                // In a real integration, create a Verified ID presentation request here
                // and return QR payload per Entra Verified ID docs.
                var payload = new PresentationRequest
                (
                    requestId,
                    $"https://example.com/qr/{requestId}",
                    $"http://localhost:{port}/vc/callback",
                    "ageOver18"
                );
                return Results.Json(payload);
            });

            // POST /vc/callback -> verifies result and sets ageVerified in your app session (scaffold)
            app.MapPost("/vc/callback", async (HttpRequest req, HttpResponse res) =>
            {
                var body = await req.ReadFromJsonAsync<CallbackPayload>();
                if (body is null || string.IsNullOrWhiteSpace(body.RequestId))
                    return Results.BadRequest(new { error = "invalid_payload" });

                // TODO: Validate signature/issuer and the VC claims from the provider.
                bool verified = body.Success && body.Claims?.AgeOver18 == true;
                _ageStatus[body.RequestId] = verified;

                // Set a cookie consumers can check from the same origin (demo only)
                if (verified)
                {
                    res.Cookies.Append("age_verified", "true", new CookieOptions
                    {
                        HttpOnly = false,
                        SameSite = SameSiteMode.Lax,
                        Secure = false, // set true behind HTTPS
                        Expires = DateTimeOffset.UtcNow.AddHours(12)
                    });
                }

                return Results.Json(new { ok = verified });
            });

            // Helper endpoint to check state by requestId (demo only)
            app.MapGet("/vc/status/{id}", (string id) =>
            {
                return Results.Json(new { id, verified = _ageStatus.TryGetValue(id, out var v) && v });
            });

            return app;
        }

        public static async Task StartAsync(int port = 5055, CancellationToken cancellationToken = default)
        {
            var app = Build(port);
            await app.RunAsync(cancellationToken);
        }

        public record PresentationRequest(
            [property: JsonPropertyName("requestId")] string RequestId,
            [property: JsonPropertyName("qrUrl")] string QrUrl,
            [property: JsonPropertyName("callback")] string Callback,
            [property: JsonPropertyName("requestedClaim")] string RequestedClaim
        );

        public record CallbackClaims(
            [property: JsonPropertyName("ageOver18")] bool AgeOver18
        );

        public record CallbackPayload(
            [property: JsonPropertyName("requestId")] string RequestId,
            [property: JsonPropertyName("success")] bool Success,
            [property: JsonPropertyName("claims")] CallbackClaims? Claims
        );
    }
}
#endif
