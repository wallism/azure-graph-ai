using CloudGraphAI.AI.GoogleVertexAI;
using NUnit.Framework;

namespace CloudGraphAI.Tests;

[TestFixture]
public class GoogleVertexAIGlobalEndpointHandlerTests
{
    [Test]
    public void RewriteGlobalEndpoint_RewritesConnectorGlobalHost()
    {
        var uri = new Uri("https://global-aiplatform.googleapis.com/v1/projects/demo/locations/global/publishers/google/models/gemini-3-flash-preview:streamGenerateContent");

        var rewritten = GoogleVertexAIGlobalEndpointHandler.RewriteGlobalEndpoint(uri);

        Assert.That(rewritten.Host, Is.EqualTo("aiplatform.googleapis.com"));
        Assert.That(rewritten.AbsolutePath, Is.EqualTo(uri.AbsolutePath));
    }

    [Test]
    public void RewriteGlobalEndpoint_LeavesRegionalHost()
    {
        var uri = new Uri("https://us-central1-aiplatform.googleapis.com/v1/projects/demo/locations/us-central1/publishers/google/models/gemini-2.5-flash:streamGenerateContent");

        var rewritten = GoogleVertexAIGlobalEndpointHandler.RewriteGlobalEndpoint(uri);

        Assert.That(rewritten, Is.EqualTo(uri));
    }
}
