using CloudGraphAI.Azure.Api;
using CloudGraphAI.Azure.Configuration;
using CloudGraphAI.Azure.Environments;
using CloudGraphAI.Azure.Import;
using CloudGraphAI.Azure.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace CloudGraphAI.Tests;

[TestFixture]
public class WebAppCollectorBuildRelationshipsTests
{
    private IAzureResourceCollector _collector = null!;
    private AzureImportContext _context = null!;

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

        _collector = new WebAppCollector(
            Substitute.For<IAzureRestApi>(),
            config,
            Substitute.For<ILogger<WebAppCollector>>());

        var environmentResolver = Substitute.For<IResourceEnvironmentResolver>();
        _context = new AzureImportContext(["sub1"], environmentResolver);
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
}
