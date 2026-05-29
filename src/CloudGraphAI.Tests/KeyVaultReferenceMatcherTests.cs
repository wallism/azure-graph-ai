using CloudGraphAI.Azure.Import;
using CloudGraphAI.Azure.Models;
using NUnit.Framework;

namespace CloudGraphAI.Tests;

[TestFixture]
public class KeyVaultReferenceMatcherTests
{
    private static KeyVault CreateVault(string id, string name, string? vaultUri = null)
        => new()
        {
            Id = id,
            Name = name,
            Properties = vaultUri is not null ? new KeyVaultProperties { VaultUri = vaultUri } : null
        };

    [Test]
    public void AddMatchingVaults_FullUrlWithTrailingSlash_MatchesByVaultUri()
    {
        var vault = CreateVault(
            "/subscriptions/sub1/resourceGroups/rg1/providers/Microsoft.KeyVault/vaults/trn-apac-tend-syd-keyvau",
            "trn-apac-tend-syd-keyvau",
            "https://trn-apac-tend-syd-keyvau.vault.azure.net/");

        var ids = new List<string>();
        KeyVaultReferenceMatcher.AddMatchingVaults(ids, [vault], ["https://trn-apac-tend-syd-keyvau.vault.azure.net/"]);

        Assert.That(ids, Has.Count.EqualTo(1));
        Assert.That(ids[0], Is.EqualTo(vault.Id));
    }

    [Test]
    public void AddMatchingVaults_FullUrlWithoutTrailingSlash_MatchesByVaultUri()
    {
        var vault = CreateVault(
            "/subscriptions/sub1/resourceGroups/rg1/providers/Microsoft.KeyVault/vaults/trn-apac-tend-syd-keyvau",
            "trn-apac-tend-syd-keyvau",
            "https://trn-apac-tend-syd-keyvau.vault.azure.net");

        var ids = new List<string>();
        KeyVaultReferenceMatcher.AddMatchingVaults(ids, [vault], ["https://trn-apac-tend-syd-keyvau.vault.azure.net"]);

        Assert.That(ids, Has.Count.EqualTo(1));
        Assert.That(ids[0], Is.EqualTo(vault.Id));
    }

    [Test]
    public void AddMatchingVaults_CandidateHasTrailingSlash_VaultUriDoesNot_StillMatches()
    {
        var vault = CreateVault(
            "/subscriptions/sub1/resourceGroups/rg1/providers/Microsoft.KeyVault/vaults/my-keyvault",
            "my-keyvault",
            "https://my-keyvault.vault.azure.net");

        var ids = new List<string>();
        KeyVaultReferenceMatcher.AddMatchingVaults(ids, [vault], ["https://my-keyvault.vault.azure.net/"]);

        Assert.That(ids, Has.Count.EqualTo(1));
        Assert.That(ids[0], Is.EqualTo(vault.Id));
    }

    [Test]
    public void AddMatchingVaults_MatchesByName()
    {
        var vault = CreateVault(
            "/subscriptions/sub1/resourceGroups/rg1/providers/Microsoft.KeyVault/vaults/my-keyvault",
            "my-keyvault",
            "https://my-keyvault.vault.azure.net/");

        var ids = new List<string>();
        KeyVaultReferenceMatcher.AddMatchingVaults(ids, [vault], ["my-keyvault"]);

        Assert.That(ids, Has.Count.EqualTo(1));
        Assert.That(ids[0], Is.EqualTo(vault.Id));
    }

    [Test]
    public void AddMatchingVaults_CaseInsensitive()
    {
        var vault = CreateVault(
            "/subscriptions/sub1/resourceGroups/rg1/providers/Microsoft.KeyVault/vaults/My-KeyVault",
            "My-KeyVault",
            "https://My-KeyVault.vault.azure.net/");

        var ids = new List<string>();
        KeyVaultReferenceMatcher.AddMatchingVaults(ids, [vault], ["https://my-keyvault.vault.azure.net/"]);

        Assert.That(ids, Has.Count.EqualTo(1));
    }

    [Test]
    public void AddMatchingVaults_ExtractsNameFromUrl_MatchesVaultByName()
    {
        var vault = CreateVault(
            "/subscriptions/sub1/resourceGroups/rg1/providers/Microsoft.KeyVault/vaults/trn-apac-tend-syd-keyvau",
            "trn-apac-tend-syd-keyvau",
            vaultUri: null);

        var ids = new List<string>();
        KeyVaultReferenceMatcher.AddMatchingVaults(ids, [vault], ["https://trn-apac-tend-syd-keyvau.vault.azure.net/"]);

        Assert.That(ids, Has.Count.EqualTo(1));
        Assert.That(ids[0], Is.EqualTo(vault.Id));
    }

    [Test]
    public void AddMatchingVaults_NoMatch_ReturnsEmpty()
    {
        var vault = CreateVault(
            "/subscriptions/sub1/resourceGroups/rg1/providers/Microsoft.KeyVault/vaults/other-vault",
            "other-vault",
            "https://other-vault.vault.azure.net/");

        var ids = new List<string>();
        KeyVaultReferenceMatcher.AddMatchingVaults(ids, [vault], ["https://my-keyvault.vault.azure.net/"]);

        Assert.That(ids, Is.Empty);
    }

    [Test]
    public void AddMatchingVaults_EmptyCandidates_NoMatch()
    {
        var vault = CreateVault(
            "/subscriptions/sub1/resourceGroups/rg1/providers/Microsoft.KeyVault/vaults/my-keyvault",
            "my-keyvault",
            "https://my-keyvault.vault.azure.net/");

        var ids = new List<string>();
        KeyVaultReferenceMatcher.AddMatchingVaults(ids, [vault], []);

        Assert.That(ids, Is.Empty);
    }

    [Test]
    public void AddMatchingVaults_DoesNotAddDuplicates()
    {
        var vault = CreateVault(
            "/subscriptions/sub1/resourceGroups/rg1/providers/Microsoft.KeyVault/vaults/my-keyvault",
            "my-keyvault",
            "https://my-keyvault.vault.azure.net/");

        var ids = new List<string> { vault.Id };
        KeyVaultReferenceMatcher.AddMatchingVaults(ids, [vault], ["https://my-keyvault.vault.azure.net/"]);

        Assert.That(ids, Has.Count.EqualTo(1));
    }

    [Test]
    public void AddMatchingVaults_MultipleCandidates_MatchesCorrectVault()
    {
        var vault1 = CreateVault(
            "/subscriptions/sub1/resourceGroups/rg1/providers/Microsoft.KeyVault/vaults/vault-a",
            "vault-a",
            "https://vault-a.vault.azure.net/");
        var vault2 = CreateVault(
            "/subscriptions/sub1/resourceGroups/rg1/providers/Microsoft.KeyVault/vaults/vault-b",
            "vault-b",
            "https://vault-b.vault.azure.net/");

        var ids = new List<string>();
        KeyVaultReferenceMatcher.AddMatchingVaults(ids, [vault1, vault2], ["https://vault-b.vault.azure.net/"]);

        Assert.That(ids, Has.Count.EqualTo(1));
        Assert.That(ids[0], Is.EqualTo(vault2.Id));
    }

    [Test]
    public void AddMatchingVaults_MultipleCandidates_MatchesMultipleVaults()
    {
        var vault1 = CreateVault(
            "/subscriptions/sub1/resourceGroups/rg1/providers/Microsoft.KeyVault/vaults/vault-a",
            "vault-a",
            "https://vault-a.vault.azure.net/");
        var vault2 = CreateVault(
            "/subscriptions/sub1/resourceGroups/rg1/providers/Microsoft.KeyVault/vaults/vault-b",
            "vault-b",
            "https://vault-b.vault.azure.net/");

        var ids = new List<string>();
        KeyVaultReferenceMatcher.AddMatchingVaults(ids, [vault1, vault2], ["vault-a", "vault-b"]);

        Assert.That(ids, Has.Count.EqualTo(2));
    }

    [Test]
    public void AddMatchingVaults_CandidateWithQuotes_StillMatches()
    {
        var vault = CreateVault(
            "/subscriptions/sub1/resourceGroups/rg1/providers/Microsoft.KeyVault/vaults/my-keyvault",
            "my-keyvault",
            "https://my-keyvault.vault.azure.net/");

        var ids = new List<string>();
        KeyVaultReferenceMatcher.AddMatchingVaults(ids, [vault], ["\"https://my-keyvault.vault.azure.net/\""]);

        Assert.That(ids, Has.Count.EqualTo(1));
    }

    [Test]
    public void GetKeyVaultName_ExtractsNameFromFullUrl()
    {
        var name = KeyVault.GetKeyVaultName("https://trn-apac-tend-syd-keyvau.vault.azure.net/");
        Assert.That(name, Is.EqualTo("trn-apac-tend-syd-keyvau"));
    }

    [Test]
    public void GetKeyVaultName_NullOrEmpty_ReturnsEmpty()
    {
        Assert.That(KeyVault.GetKeyVaultName(null), Is.EqualTo(string.Empty));
        Assert.That(KeyVault.GetKeyVaultName(""), Is.EqualTo(string.Empty));
        Assert.That(KeyVault.GetKeyVaultName("   "), Is.EqualTo(string.Empty));
    }

    [Test]
    public void GetKeyVaultName_NoTrailingSlash_ExtractsName()
    {
        var name = KeyVault.GetKeyVaultName("https://my-vault.vault.azure.net");
        Assert.That(name, Is.EqualTo("my-vault"));
    }
}
