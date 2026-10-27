using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using EvilBrains.EvilCase.Business.Files.Signatures;

namespace EvilBrains.EvilCase.Tests.Files.Signatures;

public class SignatureTrustTests
{
    private const string TimeStamping = "1.3.6.1.5.5.7.3.8";

    private const string ClientAuthentication = "1.3.6.1.5.5.7.3.2";

    private static readonly DateTimeOffset Within = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

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
                Assert.That(trust.ChainsAtIssuance(intermediate), Is.True, $"{intermediate.Subject} must chain to a bundled root");
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
                "D3D607A9FF24A19523B6DA9D2C649446F8788CB96D9FD130972E120C13677730", // I.CA Root CA/RSA
                "B8692148FF49C3799FA2347AE28BCC5289623512B67DC19170452ADE24BA51D5", // I.CA Root CA/ECC 12/2016
                "D279C01A12E8DD9A6230E459FAA447CEB336998477338C2EE4135C96737418EB", // I.CA Root CA/RSA 05/2022
                "3808CE3E961CA532682FFB8708B544E8F175AA065601A45902DF92128FC38532", // I.CA Root CA/ECC 05/2022
                "095DBC3E4E160664149682B044F5FCF3AC45040F1F14D0C0B91ED34577DA7277", // ACAeID3 - Root Certificate
                "AD016F958050E0E7E46FAE7DCC50197ED8E3FF0A4B262E5DDCDB3EDDDC7D6578", // PostSignum Root QCA 2, retired
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
                "DE2EA05839B82DCA5283A604C7D8DFF947F54695BBB4DAFB964840B1E611DA7E", // I.CA Qualified CA/RSA 07/2015
                "076ABC2269327EEF500A0C57527262BAC831F9D2DF4EF2D439E74CE17036AA3A", // I.CA Qualified 2 CA/RSA 02/2016
                "B7FAC27AA688E9736CB63BBB0C8AA40175F6A3EA6E2514D734311DCAC9B5A545", // I.CA Qualified 2 CA/ECC 06/2019
                "5F9147824201B2E23D8E128F99ADB9EC11C495796960FA0FAEF05F901A347C66", // I.CA EU Qualified CA2/RSA 06/2022
                "A7CDF7F580EA9016FD3AE07F642F5FB58A971224E6C4E92A498A33537B2034C8", // I.CA EU Qualified CA2/ECC 06/2022
                "4AB8BC6061743E0980BFD121370A88C511C4F29EB4BC089765886B64C791F768", // I.CA TSACA/RSA 05/2017
                "6AB6349834712B0D898C5700FE4E1A9C203C5B98FEF57187101689081997F27F", // I.CA TSACA/RSA 03/2022
                "52B27152BD36BCE43C76DD4F8E8068A39A2230EBCD21A354C27485D12FF6F9E1", // I.CA TSA CA/RSA 06/2022
                "B891934AC432D91FD40484A60A253F6FD041ECF0256ED52660A0C4200D1AE6CC", // ACAeID3.1 - Issuing Certificate
                "6D66FC4E8BF3BB3679FE047290ED45B69462DC17880D9996AC8AEC509C701D84", // ACAeID3.2 - Issuing Certificate
                "922B74719EEACCCEA9CF5014F7AFE53826E359CE26E210D473DFE1433AB6CCE9", // ACAeID3.3 - Issuing Certificate
                "3AE4F4DE5F3E2070A11845BDFE6DCA6E412BB7E4ED84FD4F1B7B496CADFF2CAC", // PostSignum Qualified CA 2, retired
                "D35E250CB02E27BB3FC52D1F1A0DFD88FA9813BE1B777320AAE912B54E3B1F02", // PostSignum Qualified CA 3, retired
                "270B33EFCDE50E45211CC4DBC21E92813EB9CBD6882B973373D671C33F8DDB07", // PostSignum Qualified CA 3-TSA, retired
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
                Assert.That(trust.ChainsAtIssuance(intermediate), Is.True, $"{intermediate.Subject} must chain to a bundled root");
        }
    }

    [Test]
    public void ACertificateChainsThroughABundledIntermediate()
    {
        using var pki = TestPki.Create();
        using var seal = pki.IssueSeal(Within.AddYears(-1), Within.AddYears(1));

        Assert.That(pki.Trust.Chains(seal, Within.UtcDateTime), Is.True);
    }

    [Test]
    public void AnIntermediateTheHostOffersIsNoTrustedCa()
    {
        using var pki = TestPki.Create();
        using var seal = pki.IssueSeal(Within.AddYears(-1), Within.AddYears(1));
        var rootOnly = new SignatureTrust([pki.Root], []);
        using var hostIntermediate = HostIntermediate.Add(pki.Intermediate);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(rootOnly.Chains(seal, Within.UtcDateTime), Is.False, "an intermediate from outside the bundle is no trusted CA");
            Assert.That(rootOnly.ChainsAtIssuance(seal), Is.False);
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ACertificateChainsThroughTheBundledIntermediateWhileTheHostOffersAnotherOfTheSameName(bool reissuedByTheBundledRoot)
    {
        using var pki = TestPki.Create();
        using var host = TestPki.Create("Host");
        using var seal = pki.IssueSeal(Within.AddYears(-1), Within.AddYears(1));
        using var hostIntermediate = HostIntermediate.Add(pki.ReissueIntermediate(reissuedByTheBundledRoot ? pki : host));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(pki.Trust.Chains(seal, Within.UtcDateTime), Is.True);
            Assert.That(pki.Trust.ChainsAtIssuance(seal), Is.True);
        }
    }

    [Test]
    public void ACertificateChainsOnlyWithinItsValidity()
    {
        using var pki = TestPki.Create();
        using var seal = pki.IssueSeal(Within.AddYears(-1), Within.AddYears(1));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(pki.Trust.Chains(seal, Within.AddYears(2).UtcDateTime), Is.False);
            Assert.That(pki.Trust.ChainsAtIssuance(seal), Is.True, "at issuance it was valid");
        }
    }

    [Test]
    public void ARequiredPolicyMustBeTheCertificates()
    {
        using var pki = TestPki.Create();
        using var seal = pki.IssueSeal(Within.AddYears(-1), Within.AddYears(1));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(pki.Trust.Chains(seal, Within.UtcDateTime, certificatePolicy: TestPki.SealPolicy), Is.True);
            Assert.That(pki.Trust.Chains(seal, Within.UtcDateTime, certificatePolicy: "1.2.3.4"), Is.False);
        }
    }

    [Test]
    public void ACertificateRestrictedToAnotherUsageDoesNotChainForIt()
    {
        using var pki = TestPki.Create();
        using var tsa = pki.IssueTsa();
        using var client = pki.IssueTsa(purpose: ClientAuthentication);
        using var seal = pki.IssueSeal(Within.AddYears(-1), Within.AddYears(1));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(pki.Trust.Chains(tsa, Within.UtcDateTime, applicationPolicy: TimeStamping), Is.True);
            Assert.That(pki.Trust.Chains(client, Within.UtcDateTime, applicationPolicy: TimeStamping), Is.False);
            Assert.That(pki.Trust.Chains(seal, Within.UtcDateTime, applicationPolicy: TimeStamping), Is.True, "a certificate without a usage restriction serves any");
            Assert.That(pki.Trust.Chains(client, Within.UtcDateTime), Is.True, "a critical usage restriction is understood");
        }
    }

    private static IEnumerable<string> Fingerprints(X509Certificate2Collection certificates)
    {
        return certificates.Select(static certificate => certificate.GetCertHashString(HashAlgorithmName.SHA256));
    }
}
