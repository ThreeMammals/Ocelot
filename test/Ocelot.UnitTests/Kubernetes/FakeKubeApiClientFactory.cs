using KubeClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ocelot.Provider.Kubernetes;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Ocelot.UnitTests.Kubernetes;

internal class FakeKubeApiClientFactory : KubeApiClientFactory
{
    public FakeKubeApiClientFactory(ILoggerFactory logger, IOptions<KubeClientOptions> options)
        : base(logger, options) { }

    public FakeKubeApiClientFactory(ILoggerFactory logger, IOptions<KubeClientOptions> options, string serviceAccountPath)
        : base(logger, options)
    {
        ServiceAccountPath = serviceAccountPath;
    }

    public FakeKubeApiClientFactory(string serviceAccountPath)
        : base(Mock.Of<ILoggerFactory>(), Mock.Of<IOptions<KubeClientOptions>>())
    {
        ServiceAccountPath = serviceAccountPath;
    }

    public string ActualServiceAccountPath => ServiceAccountPath;

    public KubeApiClient Actual { get; private set; }

    public override KubeApiClient Get(bool usePodServiceAccount)
    {
        return Actual = base.Get(usePodServiceAccount);
    }

    public static async Task CreateCertificate(string crtFile)
    {
        var certificate = CreateCertificate();
        byte[] certBytes = certificate.Export(X509ContentType.Cert);
        await File.WriteAllBytesAsync(crtFile, certBytes);
    }

    public static X509Certificate2 CreateCertificate()
    {
        // Generate a self-signed certificate
        using RSA rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=MyCertificate", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        // Add extensions to the certificate (optional)
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, false));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

        return request.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(1));
    }
}
