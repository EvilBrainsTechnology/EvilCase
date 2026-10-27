using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using EvilBrains.EvilCase.Business.Files.Signatures;

namespace EvilBrains.EvilCase.Tests.Files.Signatures;

public class SignatureTrustTests
{
    [Test]
    public void TheBundledRootsAreThePostSignumQualifiedRoots()
    {
        var trust = SignatureTrust.LoadPostSignum();

        Assert.That(
            Fingerprints(trust.Roots),
            Is.EquivalentTo(
            [
                "AC7F7862E685C7A7D9826A58EA32D183D4893FCC8F8FD6D900C9769A987E77F0", // PostSignum Root QCA 4
                "2D41D6D54CF9C81B0725E963C351F192D46581C8989C96C565695F6C8FC6EA67", // PostSignum Root QCA ECC R1
                "AD016F958050E0E7E46FAE7DCC50197ED8E3FF0A4B262E5DDCDB3EDDDC7D6578", // PostSignum Root QCA 2, retired
            ]),
            "the bundled roots are exactly the ones published by PostSignum, the retired one included");
    }

    [Test]
    public void TheBundledIntermediatesAreThePostSignumQualifiedCas()
    {
        var trust = SignatureTrust.LoadPostSignum();

        Assert.That(
            Fingerprints(trust.Intermediates),
            Is.EquivalentTo(
            [
                "4779BA66C768FE7E5A5BB74A225FA8B69CD6942AF481355630E5AD78B34DFF2B", // PostSignum Qualified CA 4
                "BD0636ABC7F7501899C3FBD00AE071BB070B3E82ECAFF9A27F6E1FB1E7E2A2EA", // PostSignum Qualified CA 5
                "E4D6CDF6BD2DE1EECEFC0B754B9489C58C779FADBC6BE239CC478A0AE7A8A23B", // PostSignum Qualified ECC R1 CA1
                "EBF8D157ABEFCA4AD91B5A9B7B04CC2124242C1603AF2605CD9395D9C938EF3A", // PostSignum Qualified ECC R1 CA3
                "3AE4F4DE5F3E2070A11845BDFE6DCA6E412BB7E4ED84FD4F1B7B496CADFF2CAC", // PostSignum Qualified CA 2, retired
                "D35E250CB02E27BB3FC52D1F1A0DFD88FA9813BE1B777320AAE912B54E3B1F02", // PostSignum Qualified CA 3, retired
                "270B33EFCDE50E45211CC4DBC21E92813EB9CBD6882B973373D671C33F8DDB07", // PostSignum Qualified CA 3-TSA, retired
            ]),
            "the bundled intermediates are exactly the ones published by PostSignum, the retired ones included");
    }

    [Test]
    public void EveryBundledIntermediateChainsToABundledRoot()
    {
        var trust = SignatureTrust.LoadPostSignum();

        using (Assert.EnterMultipleScope())
        {
            foreach (var intermediate in trust.Intermediates)
            {
                using var chain = trust.NewChain();
                chain.ChainPolicy.VerificationTime = intermediate.NotBefore.AddDays(1);

                Assert.That(chain.Build(intermediate), Is.True, $"{intermediate.Subject} must chain to a bundled root");
            }
        }
    }

    [Test]
    public void TheQualifiedRootsAreTheBundledProviderRoots()
    {
        var trust = SignatureTrust.LoadQualified();

        Assert.That(
            Fingerprints(trust.Roots),
            Is.EquivalentTo(
            [
                "AC7F7862E685C7A7D9826A58EA32D183D4893FCC8F8FD6D900C9769A987E77F0", // PostSignum Root QCA 4
                "2D41D6D54CF9C81B0725E963C351F192D46581C8989C96C565695F6C8FC6EA67", // PostSignum Root QCA ECC R1
            ]),
            "the qualified roots are exactly the bundled roots of PostSignum, I.CA and eIdentity");
    }

    [Test]
    public void TheQualifiedIntermediatesAreTheBundledProviderCas()
    {
        var trust = SignatureTrust.LoadQualified();

        Assert.That(
            Fingerprints(trust.Intermediates),
            Is.EquivalentTo(
            [
                "4779BA66C768FE7E5A5BB74A225FA8B69CD6942AF481355630E5AD78B34DFF2B", // PostSignum Qualified CA 4
                "BD0636ABC7F7501899C3FBD00AE071BB070B3E82ECAFF9A27F6E1FB1E7E2A2EA", // PostSignum Qualified CA 5
                "E4D6CDF6BD2DE1EECEFC0B754B9489C58C779FADBC6BE239CC478A0AE7A8A23B", // PostSignum Qualified ECC R1 CA1
                "EBF8D157ABEFCA4AD91B5A9B7B04CC2124242C1603AF2605CD9395D9C938EF3A", // PostSignum Qualified ECC R1 CA3
            ]),
            "the qualified intermediates are exactly the bundled CAs of PostSignum, I.CA and eIdentity");
    }

    [Test]
    public void EveryQualifiedIntermediateChainsToABundledRoot()
    {
        var trust = SignatureTrust.LoadQualified();

        using (Assert.EnterMultipleScope())
        {
            foreach (var intermediate in trust.Intermediates)
            {
                using var chain = trust.NewChain();
                chain.ChainPolicy.VerificationTime = intermediate.NotBefore.AddDays(1);

                Assert.That(chain.Build(intermediate), Is.True, $"{intermediate.Subject} must chain to a bundled root");
            }
        }
    }

    private static IEnumerable<string> Fingerprints(X509Certificate2Collection certificates)
    {
        return certificates.Select(static certificate => certificate.GetCertHashString(HashAlgorithmName.SHA256));
    }
}
