using System.Net;
using System.Text;
using Azure.Core;

namespace CaldovaNetworkScheduling.Services.Playback;

/// <summary>Answers the app's Azure management status reads from the playback database.</summary>
public sealed class PlaybackArmHandler(PlaybackDatabase database) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri?.AbsolutePath.Contains("/providers/Microsoft.Insights/", StringComparison.OrdinalIgnoreCase) == true)
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        var json = """{"sku":{"name":"HS_S_Gen5_8","tier":"Hyperscale","capacity":8},"properties":{"minCapacity":1.0,"status":"""
            + "\"" + database.Status + "\"}}";
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        });
    }
}

public sealed class PlaybackCredential : TokenCredential
{
    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken) =>
        new("playback", DateTimeOffset.UtcNow.AddHours(1));

    public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken) =>
        ValueTask.FromResult(GetToken(requestContext, cancellationToken));
}
