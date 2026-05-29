using CloudGraphAI.Azure.Configuration;
using CloudGraphAI.Azure.Import;
using CloudGraphAI.Azure.Models;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace CloudGraphAI.Tests;

[TestFixture]
public class DisplayNameFormatterTests
{
    private static DisplayNameFormatter CreateFormatter(DisplayNameOptions? options = null)
    {
        var opts = Options.Create(options ?? new DisplayNameOptions());
        return new DisplayNameFormatter(opts);
    }

    private static WebApp CreateNode(string id, string? name = null, string? environment = null)
    {
        var node = new WebApp { Id = id, Name = name };
        node.Environment = environment;
        return node;
    }

    [Test]
    public void Format_NullOrWhitespace_ReturnsAsIs()
    {
        var formatter = CreateFormatter();
        var node = CreateNode("/subscriptions/sub1/resourceGroups/rg1/providers/Microsoft.Web/sites/myapp");

        Assert.That(formatter.Format("", node), Is.EqualTo(""));
        Assert.That(formatter.Format("   ", node), Is.EqualTo("   "));
    }

    [Test]
    public void Format_NoOptionsConfigured_ReturnsRawName()
    {
        var formatter = CreateFormatter();
        var node = CreateNode("/sub/rg/providers/type/prod-myorg-webapp", "prod-myorg-webapp");

        var result = formatter.Format("prod-myorg-webapp", node);

        Assert.That(result, Is.EqualTo("prod-myorg-webapp"));
    }

    [Test]
    public void Format_StripEnvironmentPrefix_RemovesEnvironmentFromStart()
    {
        var formatter = CreateFormatter(new DisplayNameOptions { StripEnvironmentPrefix = true });
        var node = CreateNode("/sub/id", "prod-my-service", environment: "prod");

        var result = formatter.Format("prod-my-service", node);

        Assert.That(result, Is.EqualTo("my-service"));
    }

    [Test]
    public void Format_StripEnvironmentPrefix_CaseInsensitive()
    {
        var formatter = CreateFormatter(new DisplayNameOptions { StripEnvironmentPrefix = true });
        var node = CreateNode("/sub/id", "PROD-my-service", environment: "prod");

        var result = formatter.Format("PROD-my-service", node);

        Assert.That(result, Is.EqualTo("my-service"));
    }

    [Test]
    public void Format_StripEnvironmentPrefix_DoesNothingWhenDisabled()
    {
        var formatter = CreateFormatter(new DisplayNameOptions { StripEnvironmentPrefix = false });
        var node = CreateNode("/sub/id", "prod-my-service", environment: "prod");

        var result = formatter.Format("prod-my-service", node);

        Assert.That(result, Is.EqualTo("prod-my-service"));
    }

    [Test]
    public void Format_StripEnvironmentPrefix_NoEnvironmentResolved_LeavesNameAlone()
    {
        var formatter = CreateFormatter(new DisplayNameOptions { StripEnvironmentPrefix = true });
        var node = CreateNode("/sub/id", "prod-my-service", environment: null);

        var result = formatter.Format("prod-my-service", node);

        Assert.That(result, Is.EqualTo("prod-my-service"));
    }

    [Test]
    public void Format_StripEnvironmentPrefix_NameDoesNotStartWithEnv_LeavesAlone()
    {
        var formatter = CreateFormatter(new DisplayNameOptions { StripEnvironmentPrefix = true });
        var node = CreateNode("/sub/id", "my-prod-service", environment: "prod");

        var result = formatter.Format("my-prod-service", node);

        Assert.That(result, Is.EqualTo("my-prod-service"));
    }

    [Test]
    public void Format_PrefixesToRemove_StripsSinglePrefix()
    {
        var formatter = CreateFormatter(new DisplayNameOptions
        {
            PrefixesToRemove = ["ACME"]
        });
        var node = CreateNode("/sub/id", "ACME-webapp-01");

        var result = formatter.Format("ACME-webapp-01", node);

        Assert.That(result, Is.EqualTo("webapp-01"));
    }

    [Test]
    public void Format_PrefixesToRemove_StripsMultiplePrefixesInOrder()
    {
        var formatter = CreateFormatter(new DisplayNameOptions
        {
            PrefixesToRemove = ["ACME", "PROJ"]
        });
        var node = CreateNode("/sub/id", "ACME-PROJ-webapp");

        var result = formatter.Format("ACME-PROJ-webapp", node);

        Assert.That(result, Is.EqualTo("webapp"));
    }

    [Test]
    public void Format_PrefixesToRemove_HandlesDotSeparator()
    {
        var formatter = CreateFormatter(new DisplayNameOptions
        {
            PrefixesToRemove = ["myorg"]
        });
        var node = CreateNode("/sub/id", "myorg.webapp");

        var result = formatter.Format("myorg.webapp", node);

        Assert.That(result, Is.EqualTo("webapp"));
    }

    [Test]
    public void Format_PrefixesToRemove_HandlesUnderscoreSeparator()
    {
        var formatter = CreateFormatter(new DisplayNameOptions
        {
            PrefixesToRemove = ["myorg"]
        });
        var node = CreateNode("/sub/id", "myorg_webapp");

        var result = formatter.Format("myorg_webapp", node);

        Assert.That(result, Is.EqualTo("webapp"));
    }

    [Test]
    public void Format_PrefixesToRemove_NoSeparator_StillStrips()
    {
        var formatter = CreateFormatter(new DisplayNameOptions
        {
            PrefixesToRemove = ["dev"]
        });
        var node = CreateNode("/sub/id", "devwebapp");

        var result = formatter.Format("devwebapp", node);

        Assert.That(result, Is.EqualTo("webapp"));
    }

    [Test]
    public void Format_PrefixesToRemove_PrefixNotFound_LeavesAlone()
    {
        var formatter = CreateFormatter(new DisplayNameOptions
        {
            PrefixesToRemove = ["ACME"]
        });
        var node = CreateNode("/sub/id", "other-webapp");

        var result = formatter.Format("other-webapp", node);

        Assert.That(result, Is.EqualTo("other-webapp"));
    }

    [Test]
    public void Format_SubstringsToRemove_RemovesFromMiddle()
    {
        var formatter = CreateFormatter(new DisplayNameOptions
        {
            SubstringsToRemove = ["-eastus-"]
        });
        var node = CreateNode("/sub/id", "acme-eastus-webapp");

        var result = formatter.Format("acme-eastus-webapp", node);

        Assert.That(result, Is.EqualTo("acmewebapp"));
    }

    [Test]
    public void Format_SubstringsToRemove_CaseInsensitive()
    {
        var formatter = CreateFormatter(new DisplayNameOptions
        {
            SubstringsToRemove = ["EASTUS"]
        });
        var node = CreateNode("/sub/id", "acme-eastus-webapp");

        var result = formatter.Format("acme-eastus-webapp", node);

        Assert.That(result, Is.EqualTo("acme--webapp"));
    }

    [Test]
    public void Format_SubstringsToRemove_MultipleSubstrings()
    {
        var formatter = CreateFormatter(new DisplayNameOptions
        {
            SubstringsToRemove = ["acme-", "-asa-"]
        });
        var node = CreateNode("/sub/id", "acme-prod-asa-webapp");

        var result = formatter.Format("acme-prod-asa-webapp", node);

        Assert.That(result, Is.EqualTo("prodwebapp"));
    }

    [Test]
    public void Format_CombinedOptions_AppliesAllInOrder()
    {
        var formatter = CreateFormatter(new DisplayNameOptions
        {
            StripEnvironmentPrefix = true,
            PrefixesToRemove = ["ACME", "syd"],
            SubstringsToRemove = []
        });
        var node = CreateNode("/sub/id", "dev-ACME-syd-myapp", environment: "dev");

        var result = formatter.Format("dev-ACME-syd-myapp", node);

        Assert.That(result, Is.EqualTo("myapp"));
    }

    [Test]
    public void Format_CombinedOptions_SubstringIncludesSeparators()
    {
        var formatter = CreateFormatter(new DisplayNameOptions
        {
            StripEnvironmentPrefix = true,
            PrefixesToRemove = ["ACME"],
            SubstringsToRemove = ["syd-"]
        });
        var node = CreateNode("/sub/id", "dev-ACME-syd-myapp", environment: "dev");

        // After env strip: "ACME-syd-myapp" → after prefix strip: "syd-myapp" → after substring "syd-": "myapp"
        var result = formatter.Format("dev-ACME-syd-myapp", node);

        Assert.That(result, Is.EqualTo("myapp"));
    }

    [Test]
    public void Format_ResultWouldBeEmpty_ReturnsFallbackRawName()
    {
        var formatter = CreateFormatter(new DisplayNameOptions
        {
            PrefixesToRemove = ["entirename"]
        });
        var node = CreateNode("/sub/id", "entirename");

        // "entirename" starts with "entirename" but remainder is empty → returns original
        var result = formatter.Format("entirename", node);

        Assert.That(result, Is.EqualTo("entirename"));
    }

    [Test]
    public void Format_ResultBecomesOnlySeparators_ReturnsFallbackRawName()
    {
        var formatter = CreateFormatter(new DisplayNameOptions
        {
            SubstringsToRemove = ["webapp"]
        });
        var node = CreateNode("/sub/id", "-webapp-");

        var result = formatter.Format("-webapp-", node);

        Assert.That(result, Is.EqualTo("-webapp-"));
    }

    [Test]
    public void Format_TrimsLeadingAndTrailingSeparators()
    {
        var formatter = CreateFormatter(new DisplayNameOptions
        {
            PrefixesToRemove = ["org"]
        });
        var node = CreateNode("/sub/id", "org-myapp-");

        var result = formatter.Format("org-myapp-", node);

        Assert.That(result, Is.EqualTo("myapp"));
    }

    [Test]
    public void Format_EnvironmentAndPrefixes_StripsEnvironmentFirst()
    {
        var formatter = CreateFormatter(new DisplayNameOptions
        {
            StripEnvironmentPrefix = true,
            PrefixesToRemove = ["ACME"]
        });
        // Environment "staging" is prefix, then "ACME" is next prefix
        var node = CreateNode("/sub/id", "staging-ACME-service", environment: "staging");

        var result = formatter.Format("staging-ACME-service", node);

        Assert.That(result, Is.EqualTo("service"));
    }

    [Test]
    public void Format_NonEnvironmentAnnotatedNode_SkipsEnvironmentStripping()
    {
        var formatter = CreateFormatter(new DisplayNameOptions { StripEnvironmentPrefix = true });
        // Subscription is AzureGraphNode but NOT IEnvironmentAnnotatedResource-bearing in the same way
        var node = new Subscription
        {
            Id = "/subscriptions/sub1",
            Name = "prod-subscription",
            AzureSubscriptionId = "sub1"
        };

        var result = formatter.Format("prod-subscription", node);

        // Subscription doesn't implement IEnvironmentAnnotatedResource, so env stripping is skipped
        Assert.That(result, Is.EqualTo("prod-subscription"));
    }
}
