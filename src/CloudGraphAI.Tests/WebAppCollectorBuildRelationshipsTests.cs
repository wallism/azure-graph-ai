using System.Net;
using System.Text;
using Agile.API.Clients.CallHandling;
using CloudGraphAI.Azure.Api;
using CloudGraphAI.Azure.Configuration;
using CloudGraphAI.Azure.Environments;
using CloudGraphAI.Azure.Import;
using CloudGraphAI.Azure.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NSubstitute;
using NUnit.Framework;

namespace CloudGraphAI.Tests;

[TestFixture]
public class WebAppCollectorBuildRelationshipsTests
{
    private IAzureResourceCollector _collector = null!;
    private AzureImportContext _context = null!;
    private IAzureRestApi _azureApi = null!;

    [SetUp]
    public void SetUp()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureGraph:KeyVaultReferenceSettingNames:0"] = "keyVaultPrefix",
                ["AzureGraph:KeyVaultReferenceSettingNames:1"] = "keyVaultBaseUrl",
            })
            .Build();

        _azureApi = Substitute.For<IAzureRestApi>();
        _collector = new WebAppCollector(
            _azureApi,
            config,
            Substitute.For<ILogger<WebAppCollector>>());

        var environmentResolver = Substitute.For<IResourceEnvironmentResolver>();
        _context = new AzureImportContext(["sub1"], environmentResolver);
    }

    [Test]
    public async Task CollectAsync_LoadsSiteConfigAsLinkedGraphNode()
    {
        const string webAppId = "/subscriptions/sub1/resourceGroups/rg1/providers/Microsoft.Web/sites/my-app";
        const string siteConfigId = $"{webAppId}/config/web";
        var webApp = new WebApp
        {
            Id = webAppId,
            Name = "my-app",
            Type = "Microsoft.Web/sites",
            Location = "australiaeast",
            Properties = new WebAppProperties()
        };

        _azureApi.GetWebAppsAsync("sub1", Arg.Any<CancellationToken>())
            .Returns(SuccessAsync(new AzureResourceListResult<WebApp> { Value = [webApp] }));
        _azureApi.GetWebAppSiteConfigAsync("sub1", Arg.Any<WebApp>(), Arg.Any<CancellationToken>())
            .Returns(SuccessAsync(new WebAppConfig
            {
                Value =
                [
                    new SiteConfigWrapper
                    {
                        Id = siteConfigId,
                        Name = "my-app/web",
                        Type = "Microsoft.Web/sites/config",
                        Location = "australiaeast",
                        Properties = new SiteConfig
                        {
                            MinTlsVersion = "1.2",
                            ScmMinTlsVersion = "1.2",
                            FtpsState = "FtpsOnly",
                            AlwaysOn = true
                        }
                    }
                ]
            }));
        _azureApi.GetWebAppSettingsAsync("sub1", Arg.Any<WebApp>(), Arg.Any<CancellationToken>())
            .Returns(SuccessAsync(new AppSettings()));
        _azureApi.GetWebAppConnectionStringsAsync("sub1", Arg.Any<WebApp>(), Arg.Any<CancellationToken>())
            .Returns(SuccessAsync(new ConnectionStrings()));

        await _collector.CollectAsync(_context);

        var result = _context.GetNodes<WebApp>().Single();
        var siteConfig = _context.GetNodes<WebAppSiteConfig>().Single();

        Assert.That(result.SiteConfigs, Is.EqualTo(new[] { siteConfigId }));
        Assert.That(siteConfig.Id, Is.EqualTo(siteConfigId));
        Assert.That(siteConfig.Properties?.MinTlsVersion, Is.EqualTo("1.2"));
        Assert.That(siteConfig.Properties?.ScmMinTlsVersion, Is.EqualTo("1.2"));
        Assert.That(siteConfig.ResourceGroups, Is.EqualTo(new[] { "/subscriptions/sub1/resourceGroups/rg1" }));
        Assert.That(siteConfig.Subscriptions, Is.EqualTo(new[] { "/subscriptions/sub1" }));
    }

    [Test]
    public async Task BuildRelationshipsAsync_ViaInterface_PopulatesKeyVaults()
    {
        var vault = new KeyVault
        {
            Id = "/subscriptions/sub1/resourceGroups/rg1/providers/Microsoft.KeyVault/vaults/my-vault",
            Name = "my-vault",
            Properties = new KeyVaultProperties { VaultUri = "https://my-vault.vault.azure.net/" }
        };

        var webApp = new WebApp
        {
            Id = "/subscriptions/sub1/resourceGroups/rg1/providers/Microsoft.Web/sites/my-app",
            Name = "my-app"
        };
        webApp.KeyVaultReferenceCandidates.Add("https://my-vault.vault.azure.net/");

        _context.AddNodes(new[] { vault });
        _context.AddNodes(new[] { webApp });

        // Call through the interface — this is the exact dispatch that was broken
        await _collector.BuildRelationshipsAsync(_context);

        var result = _context.GetNodes<WebApp>().First();
        Assert.That(result.KeyVaults, Has.Count.EqualTo(1));
        Assert.That(result.KeyVaults[0], Is.EqualTo(vault.Id));
    }

    [Test]
    public async Task BuildRelationshipsAsync_ViaInterface_PopulatesServerFarms()
    {
        var serverFarmId = "/subscriptions/sub1/resourceGroups/rg1/providers/Microsoft.Web/serverfarms/my-plan";
        var webApp = new WebApp
        {
            Id = "/subscriptions/sub1/resourceGroups/rg1/providers/Microsoft.Web/sites/my-app",
            Name = "my-app",
            Properties = new WebAppProperties { ServerFarmId = serverFarmId }
        };

        _context.AddNodes(new[] { webApp });

        await _collector.BuildRelationshipsAsync(_context);

        var result = _context.GetNodes<WebApp>().First();
        Assert.That(result.ServerFarms, Has.Count.EqualTo(1));
        Assert.That(result.ServerFarms[0], Is.EqualTo(serverFarmId));
    }

    [Test]
    public async Task BuildRelationshipsAsync_ViaInterface_PopulatesSubnets()
    {
        var subnetId = "/subscriptions/sub1/resourceGroups/rg1/providers/Microsoft.Network/virtualNetworks/vnet1/subnets/subnet1";
        var webApp = new WebApp
        {
            Id = "/subscriptions/sub1/resourceGroups/rg1/providers/Microsoft.Web/sites/my-app",
            Name = "my-app",
            Properties = new WebAppProperties { VirtualNetworkSubnetId = subnetId }
        };

        _context.AddNodes(new[] { webApp });

        await _collector.BuildRelationshipsAsync(_context);

        var result = _context.GetNodes<WebApp>().First();
        Assert.That(result.DeployedInSubnets, Has.Count.EqualTo(1));
        Assert.That(result.DeployedInSubnets[0], Is.EqualTo(subnetId));
    }

    [Test]
    public async Task BuildRelationshipsAsync_ViaInterface_MatchesMultipleVaults()
    {
        var vault1 = new KeyVault
        {
            Id = "/subscriptions/sub1/resourceGroups/rg1/providers/Microsoft.KeyVault/vaults/vault-one",
            Name = "vault-one",
            Properties = new KeyVaultProperties { VaultUri = "https://vault-one.vault.azure.net/" }
        };
        var vault2 = new KeyVault
        {
            Id = "/subscriptions/sub1/resourceGroups/rg1/providers/Microsoft.KeyVault/vaults/vault-two",
            Name = "vault-two",
            Properties = new KeyVaultProperties { VaultUri = "https://vault-two.vault.azure.net/" }
        };

        var webApp = new WebApp
        {
            Id = "/subscriptions/sub1/resourceGroups/rg1/providers/Microsoft.Web/sites/my-app",
            Name = "my-app"
        };
        webApp.KeyVaultReferenceCandidates.Add("https://vault-one.vault.azure.net/");
        webApp.KeyVaultReferenceCandidates.Add("https://vault-two.vault.azure.net/");

        _context.AddNodes(new[] { vault1, vault2 });
        _context.AddNodes(new[] { webApp });

        await _collector.BuildRelationshipsAsync(_context);

        var result = _context.GetNodes<WebApp>().First();
        Assert.That(result.KeyVaults, Has.Count.EqualTo(2));
        Assert.That(result.KeyVaults, Does.Contain(vault1.Id));
        Assert.That(result.KeyVaults, Does.Contain(vault2.Id));
    }

    [Test]
    public async Task BuildRelationshipsAsync_ViaInterface_NoCandidates_LeavesKeyVaultsEmpty()
    {
        var vault = new KeyVault
        {
            Id = "/subscriptions/sub1/resourceGroups/rg1/providers/Microsoft.KeyVault/vaults/my-vault",
            Name = "my-vault",
            Properties = new KeyVaultProperties { VaultUri = "https://my-vault.vault.azure.net/" }
        };

        var webApp = new WebApp
        {
            Id = "/subscriptions/sub1/resourceGroups/rg1/providers/Microsoft.Web/sites/my-app",
            Name = "my-app"
        };
        // No candidates added

        _context.AddNodes(new[] { vault });
        _context.AddNodes(new[] { webApp });

        await _collector.BuildRelationshipsAsync(_context);

        var result = _context.GetNodes<WebApp>().First();
        Assert.That(result.KeyVaults, Is.Empty);
    }

    private static async Task<CallResult<T>> SuccessAsync<T>(T value)
        where T : class
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://management.azure.test/");
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonConvert.SerializeObject(value), Encoding.UTF8, "application/json")
        };

        return await CallResult<T>.Wrap(request, response, 1);
    }
}
