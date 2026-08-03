using Krosoft.Extensions.Core.Helpers;
using Krosoft.Extensions.Testing;
using Krosoft.Extensions.Zip.Extensions;
using Krosoft.Extensions.Zip.Interfaces;
using Krosoft.Extensions.Zip.Tests.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Krosoft.Extensions.Zip.Tests.Services;

/// <summary>
/// Vérifie le contenu réel des archives produites par <see cref="IZipService" />.
/// </summary>
[TestClass]
public class ZipServiceContenuTests : BaseTest
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

    private string CreerFichier(string nom, string contenu)
    {
        var chemin = Path.Combine(_repertoireTravail, nom);
        Directory.CreateDirectory(Path.GetDirectoryName(chemin)!);
        File.WriteAllText(chemin, contenu);

        return chemin;
    }

    [TestMethod]
    public void ExtractZip_ArchiveProduite_RestitueLeContenuDesFichiers()
    {
        var streams = new Dictionary<string, Stream>
        {
            { "premier.txt", ZipTestHelper.CreerFlux("Contenu du premier fichier.") },
            { "second.txt", ZipTestHelper.CreerFlux("Contenu du second fichier.") }
        };

        var cheminArchive = Path.Combine(_repertoireTravail, "archive.zip");
        var cheminExtraction = Path.Combine(_repertoireTravail, "extraction");

        using (var zip = _zipService.Zip(streams))
        {
            FileHelper.Write(cheminArchive, zip);
        }

        _zipService.ExtractZip(cheminArchive, cheminExtraction);

        var fichiers = Directory.GetFiles(cheminExtraction).Select(Path.GetFileName).ToList();
        Check.That(fichiers).HasSize(2);
        Check.That(fichiers).Contains("premier.txt", "second.txt");
        Check.That(File.ReadAllText(Path.Combine(cheminExtraction, "premier.txt"))).IsEqualTo("Contenu du premier fichier.");
        Check.That(File.ReadAllText(Path.Combine(cheminExtraction, "second.txt"))).IsEqualTo("Contenu du second fichier.");
    }

    [TestMethod]
    public void ExtractZip_RepertoireCibleExistant_EstVideAvantExtraction()
    {
        var cheminExtraction = Path.Combine(_repertoireTravail, "extraction");
        Directory.CreateDirectory(cheminExtraction);
        File.WriteAllText(Path.Combine(cheminExtraction, "residu.txt"), "Fichier d'une extraction précédente.");

        _zipService.ExtractZip("Files/zip.zip", cheminExtraction);

        var fichiers = Directory.GetFiles(cheminExtraction).Select(Path.GetFileName).ToList();
        Check.That(fichiers).HasSize(3);
        Check.That(fichiers).Not.Contains("residu.txt");
    }

    [TestMethod]
    public void GetZipStream_CheminsDeFichiers_ArchiveContientLeContenuDesFichiers()
    {
        var premier = CreerFichier("premier.txt", "Contenu du premier fichier.");
        var second = CreerFichier("second.txt", "Contenu du second fichier.");

        using var zip = _zipService.GetZipStream(new[] { premier, second });

        var entrees = ZipTestHelper.LireEntrees(zip);
        Check.That(entrees).HasSize(2);
        Check.That(ZipTestHelper.NomsDe(entrees)).Contains("premier.txt", "second.txt");
        Check.That(ZipTestHelper.ContenuDe(entrees, "premier.txt")).IsEqualTo("Contenu du premier fichier.");
        Check.That(ZipTestHelper.ContenuDe(entrees, "second.txt")).IsEqualTo("Contenu du second fichier.");
    }

    [TestMethod]
    public void GetZipStream_CheminsDeFichiers_LibereLesFluxDesFichiersSources()
    {
        var chemin = CreerFichier("liberable.txt", "Contenu à libérer.");

        using var zip = _zipService.GetZipStream(new[] { chemin });

        Check.That(zip).IsNotNull();
        Check.ThatCode(() => File.Delete(chemin)).DoesNotThrow();
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
    public void Zip_FluxUnique_ArchiveContientLeFichierEtSonContenu()
    {
        using var stream = ZipTestHelper.CreerFlux("Contenu du rapport.");

        using var zip = _zipService.Zip(stream, "rapport.txt");

        var entrees = ZipTestHelper.LireEntrees(zip);
        Check.That(entrees).HasSize(1);
        Check.That(entrees.Single().Nom).IsEqualTo("rapport.txt");
        Check.That(entrees.Single().Contenu).IsEqualTo("Contenu du rapport.");
    }

    [TestMethod]
    public void Zip_NomDeFichierAvecCaracteresInvalides_EstAssaini()
    {
        using var stream = ZipTestHelper.CreerFlux("Contenu du rapport.");

        using var zip = _zipService.Zip(stream, "mon dossier/rapport final.txt");

        var entrees = ZipTestHelper.LireEntrees(zip);
        Check.That(entrees).HasSize(1);
        Check.That(entrees.Single().Nom).IsEqualTo("mondossier_rapportfinal.txt");
        Check.That(entrees.Single().Contenu).IsEqualTo("Contenu du rapport.");
    }

    [TestMethod]
    public void Zip_PlusieursFlux_ArchiveContientTousLesFichiers()
    {
        var streams = new Dictionary<string, Stream>
        {
            { "premier.txt", ZipTestHelper.CreerFlux("Contenu du premier fichier.") },
            { "second.txt", ZipTestHelper.CreerFlux("Contenu du second fichier.") },
            { "vide.txt", ZipTestHelper.CreerFlux(string.Empty) }
        };

        using var zip = _zipService.Zip(streams);

        var entrees = ZipTestHelper.LireEntrees(zip);
        Check.That(entrees).HasSize(3);
        Check.That(ZipTestHelper.NomsDe(entrees)).Contains("premier.txt", "second.txt", "vide.txt");
        Check.That(ZipTestHelper.ContenuDe(entrees, "premier.txt")).IsEqualTo("Contenu du premier fichier.");
        Check.That(ZipTestHelper.ContenuDe(entrees, "second.txt")).IsEqualTo("Contenu du second fichier.");
        Check.That(ZipTestHelper.ContenuDe(entrees, "vide.txt")).IsEmpty();
    }

    [TestMethod]
    public void Zip_SansFlux_ProduitUneArchiveVide()
    {
        using var zip = _zipService.Zip(new Dictionary<string, Stream>());

        var entrees = ZipTestHelper.LireEntrees(zip);
        Check.That(entrees).IsEmpty();
    }

    [TestMethod]
    public async Task ZipAsync_DesChemins_ArchiveContientLeContenuDesFichiers()
    {
        var dictionary = new Dictionary<string, string>
        {
            { "premier.txt", CreerFichier("source1.txt", "Contenu du premier fichier.") },
            { "second.txt", CreerFichier("source2.txt", "Contenu du second fichier.") }
        };

        using var zip = await _zipService.ZipAsync(dictionary, CancellationToken.None);

        var entrees = ZipTestHelper.LireEntrees(zip);
        Check.That(entrees).HasSize(2);
        Check.That(ZipTestHelper.NomsDe(entrees)).Contains("premier.txt", "second.txt");
        Check.That(ZipTestHelper.ContenuDe(entrees, "premier.txt")).IsEqualTo("Contenu du premier fichier.");
        Check.That(ZipTestHelper.ContenuDe(entrees, "second.txt")).IsEqualTo("Contenu du second fichier.");
    }

    [TestMethod]
    public async Task ZipAsync_DesChemins_FichierInexistantEstIgnore()
    {
        var dictionary = new Dictionary<string, string>
        {
            { "present.txt", CreerFichier("source1.txt", "Contenu présent.") },
            { "absent.txt", Path.Combine(_repertoireTravail, "introuvable.txt") }
        };

        using var zip = await _zipService.ZipAsync(dictionary, CancellationToken.None);

        var entrees = ZipTestHelper.LireEntrees(zip);
        Check.That(entrees).HasSize(1);
        Check.That(entrees.Single().Nom).IsEqualTo("present.txt");
        Check.That(entrees.Single().Contenu).IsEqualTo("Contenu présent.");
    }

    [TestMethod]
    public async Task ZipAsync_DesChemins_SansFichier_RetourneUnFluxVide()
    {
        IReadOnlyDictionary<string, string> dictionary = new Dictionary<string, string>();

        using var zip = await _zipService.ZipAsync(dictionary, CancellationToken.None);

        Check.That(zip).IsNotNull();
        Check.That(zip.Length).IsEqualTo(0);
    }

    [TestMethod]
    public async Task ZipAsync_DesFlux_ArchiveContientLeContenuDesFlux()
    {
        var dictionary = new Dictionary<string, Stream>
        {
            { "premier.txt", ZipTestHelper.CreerFlux("Contenu du premier fichier.") },
            { "second.txt", ZipTestHelper.CreerFlux("Contenu du second fichier.") }
        };

        using var zip = await _zipService.ZipAsync(dictionary, CancellationToken.None);

        var entrees = ZipTestHelper.LireEntrees(zip);
        Check.That(entrees).HasSize(2);
        Check.That(ZipTestHelper.NomsDe(entrees)).Contains("premier.txt", "second.txt");
        Check.That(ZipTestHelper.ContenuDe(entrees, "premier.txt")).IsEqualTo("Contenu du premier fichier.");
        Check.That(ZipTestHelper.ContenuDe(entrees, "second.txt")).IsEqualTo("Contenu du second fichier.");
    }

    [TestMethod]
    public async Task ZipAsync_DesFlux_SansFlux_RetourneUnFluxVide()
    {
        IReadOnlyDictionary<string, Stream> dictionary = new Dictionary<string, Stream>();

        using var zip = await _zipService.ZipAsync(dictionary, CancellationToken.None);

        Check.That(zip).IsNotNull();
        Check.That(zip.Length).IsEqualTo(0);
    }

    [TestMethod]
    public async Task ZipAsync_NomDArchiveAvecCaracteresInvalides_EstAssaini()
    {
        var dictionary = new Dictionary<string, Stream>
        {
            { "premier.txt", ZipTestHelper.CreerFlux("Contenu du premier fichier.") }
        };

        var zipFileStream = await _zipService.ZipAsync(dictionary, "mon archive finale.zip", CancellationToken.None);

        Check.That(zipFileStream.FileName).IsEqualTo("monarchivefinale.zip");
        Check.That(zipFileStream.ContentType).IsEqualTo("application/zip");

        var entrees = ZipTestHelper.LireEntrees(zipFileStream.Stream);
        Check.That(entrees).HasSize(1);
        Check.That(entrees.Single().Nom).IsEqualTo("premier.txt");
        Check.That(entrees.Single().Contenu).IsEqualTo("Contenu du premier fichier.");
    }
}
