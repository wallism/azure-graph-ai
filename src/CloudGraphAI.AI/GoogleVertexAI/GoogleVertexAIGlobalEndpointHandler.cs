namespace CloudGraphAI.AI.GoogleVertexAI;

internal sealed class GoogleVertexAIGlobalEndpointHandler : DelegatingHandler
{
    private const string ConnectorGlobalHost = "global-aiplatform.googleapis.com";
    private const string VertexAIGlobalHost = "aiplatform.googleapis.com";

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.RequestUri is not null)
        {
            request.RequestUri = RewriteGlobalEndpoint(request.RequestUri);
        }

        return base.SendAsync(request, cancellationToken);
    }

    internal static Uri RewriteGlobalEndpoint(Uri uri)
    {
        if (!string.Equals(uri.Host, ConnectorGlobalHost, StringComparison.OrdinalIgnoreCase))
        {
            return uri;
        }

        var builder = new UriBuilder(uri)
        {
            Host = VertexAIGlobalHost
        };

        return builder.Uri;
    }
}
