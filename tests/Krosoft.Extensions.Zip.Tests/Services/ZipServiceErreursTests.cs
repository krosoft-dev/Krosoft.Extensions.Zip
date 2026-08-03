using Krosoft.Extensions.Testing;
using Krosoft.Extensions.Zip.Extensions;
using Krosoft.Extensions.Zip.Interfaces;
using Krosoft.Extensions.Zip.Tests.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Krosoft.Extensions.Zip.Tests.Services;

/// <summary>
/// Vérifie le comportement de <see cref="IZipService" /> face aux flux et archives invalides.
/// </summary>
[TestClass]
public class ZipServiceErreursTests : BaseTest
{
    private string _repertoireTravail = null!;
    private IZipService _zipService = null!;

    protected override void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddZip();
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_repertoireTravail))
        {
            Directory.Delete(_repertoireTravail, true);
        }
    }

    [TestMethod]
    public void ExtractZip_ArchiveCorrompue_Leve()
    {
        var cheminArchive = Path.Combine(_repertoireTravail, "corrompue.zip");
        File.WriteAllText(cheminArchive, "Ceci n'est pas une archive ZIP.");
        var cheminExtraction = Path.Combine(_repertoireTravail, "extraction");

        Check.ThatCode(() => _zipService.ExtractZip(cheminArchive, cheminExtraction))
             .Throws<InvalidDataException>();
    }

    [TestMethod]
    public void ExtractZip_ArchiveInexistante_Leve()
    {
        var cheminArchive = Path.Combine(_repertoireTravail, "introuvable.zip");
        var cheminExtraction = Path.Combine(_repertoireTravail, "extraction");

        Check.ThatCode(() => _zipService.ExtractZip(cheminArchive, cheminExtraction))
             .Throws<FileNotFoundException>();
    }

    [TestMethod]
    public void GetZipStream_FichierInexistant_Leve()
    {
        var chemin = Path.Combine(_repertoireTravail, "introuvable.txt");

        Check.ThatCode(() => _zipService.GetZipStream(new[] { chemin }))
             .Throws<FileNotFoundException>();
    }

    [TestMethod]
    public void GetZipStream_NomsDeFichiersEnDouble_Leve()
    {
        var premier = Path.Combine(_repertoireTravail, "dossier1", "doublon.txt");
        var second = Path.Combine(_repertoireTravail, "dossier2", "doublon.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(premier)!);
        Directory.CreateDirectory(Path.GetDirectoryName(second)!);
        File.WriteAllText(premier, "Contenu du premier fichier.");
        File.WriteAllText(second, "Contenu du second fichier.");

        Check.ThatCode(() => _zipService.GetZipStream(new[] { premier, second }))
             .Throws<ArgumentException>();
    }

    [TestInitialize]
    public void SetUp()
    {
        var serviceProvider = CreateServiceCollection();
        _zipService = serviceProvider.GetRequiredService<IZipService>();

        _repertoireTravail = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_repertoireTravail);
    }

    [TestMethod]
    public void Zip_FluxDejaLibere_Leve()
    {
        var stream = ZipTestHelper.CreerFlux("Contenu du rapport.");
        stream.Dispose();

        Check.ThatCode(() => _zipService.Zip(stream, "rapport.txt"))
             .Throws<ObjectDisposedException>();
    }

    [TestMethod]
    public void Zip_FluxNonLisible_Leve()
    {
        var stream = new Mock<Stream> { CallBase = true };
        stream.Setup(x => x.CanRead).Returns(false);
        stream.Setup(x => x.CanWrite).Returns(true);

        Check.ThatCode(() => _zipService.Zip(stream.Object, "rapport.txt"))
             .Throws<NotSupportedException>();
    }

    /// <summary>
    /// Construit un dictionnaire acceptant deux entrées homonymes, ce qu'un <see cref="Dictionary{TKey,TValue}" /> interdit.
    /// </summary>
    private static IDictionary<string, Stream> CreerFluxAvecNomsEnDouble()
    {
        var entrees = new List<KeyValuePair<string, Stream>>
        {
            new("doublon.txt", ZipTestHelper.CreerFlux("Contenu du premier fichier.")),
            new("doublon.txt", ZipTestHelper.CreerFlux("Contenu du second fichier."))
        };

        var streams = new Mock<IDictionary<string, Stream>>();
        streams.Setup(x => x.GetEnumerator()).Returns(() => entrees.GetEnumerator());

        return streams.Object;
    }

    [TestMethod]
    public void Zip_NomsDEntreesEnDouble_ProduitDeuxEntreesHomonymes()
    {
        using var zip = _zipService.Zip(CreerFluxAvecNomsEnDouble());

        var entreesArchive = ZipTestHelper.LireEntrees(zip);
        Check.That(entreesArchive).HasSize(2);
        Check.That(ZipTestHelper.NomsDe(entreesArchive)).ContainsExactly("doublon.txt", "doublon.txt");
        Check.That(entreesArchive[0].Contenu).IsEqualTo("Contenu du premier fichier.");
        Check.That(entreesArchive[1].Contenu).IsEqualTo("Contenu du second fichier.");
    }

    [TestMethod]
    public void Zip_NomsDEntreesEnDouble_ExtractionImpossible()
    {
        var cheminArchive = Path.Combine(_repertoireTravail, "doublons.zip");
        var cheminExtraction = Path.Combine(_repertoireTravail, "extraction");

        using (var zip = _zipService.Zip(CreerFluxAvecNomsEnDouble()))
        {
            using var fichier = File.Create(cheminArchive);
            zip.CopyTo(fichier);
        }

        Check.ThatCode(() => _zipService.ExtractZip(cheminArchive, cheminExtraction))
             .Throws<IOException>();
    }

    [TestMethod]
    public void ZipAsync_FluxDejaLibere_Leve()
    {
        var stream = ZipTestHelper.CreerFlux("Contenu du rapport.");
        stream.Dispose();

        var dictionary = new Dictionary<string, Stream> { { "rapport.txt", stream } };

        Check.ThatCode(async () => await _zipService.ZipAsync(dictionary, CancellationToken.None))
             .Throws<ObjectDisposedException>();
    }
}
