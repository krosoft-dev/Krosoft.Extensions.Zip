using Krosoft.Extensions.Core.Models.Exceptions;
using Krosoft.Extensions.Testing;
using Krosoft.Extensions.Zip.Extensions;
using Krosoft.Extensions.Zip.Interfaces;
using Krosoft.Extensions.Zip.Tests.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Krosoft.Extensions.Zip.Tests.Services;

/// <summary>
/// Vérifie le contrôle des arguments de <see cref="IZipService" />.
/// </summary>
[TestClass]
public class ZipServiceGuardTests : BaseTest
{
    private IZipService _zipService = null!;

    protected override void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddZip();
    }

    [TestMethod]
    [DataRow((string?)null)]
    [DataRow("")]
    [DataRow("   ")]
    public void ExtractZip_CheminArchiveNonRenseigne_Leve(string? zipPath)
    {
        Check.ThatCode(() => _zipService.ExtractZip(zipPath!, "extraction"))
             .Throws<KrosoftTechnicalException>()
             .WithMessage("La variable 'zipPath' est vide ou non renseignée.");
    }

    [TestMethod]
    [DataRow((string?)null)]
    [DataRow("")]
    [DataRow("   ")]
    public void ExtractZip_CheminExtractionNonRenseigne_Leve(string? extractPath)
    {
        Check.ThatCode(() => _zipService.ExtractZip("Files/zip.zip", extractPath!))
             .Throws<KrosoftTechnicalException>()
             .WithMessage("La variable 'extractPath' est vide ou non renseignée.");
    }

    [TestMethod]
    public void GetZipStream_CheminsNull_Leve()
    {
        Check.ThatCode(() => _zipService.GetZipStream(null!))
             .Throws<KrosoftTechnicalException>()
             .WithMessage("La variable 'filePaths' n'est pas renseignée.");
    }

    [TestInitialize]
    public void SetUp()
    {
        var serviceProvider = CreateServiceCollection();
        _zipService = serviceProvider.GetRequiredService<IZipService>();
    }

    [TestMethod]
    [DataRow((string?)null)]
    [DataRow("")]
    [DataRow("   ")]
    public void Zip_NomDeFichierNonRenseigne_Leve(string? fileName)
    {
        using var stream = ZipTestHelper.CreerFlux("contenu");

        Check.ThatCode(() => _zipService.Zip(stream, fileName!))
             .Throws<KrosoftTechnicalException>()
             .WithMessage("La variable 'fileName' est vide ou non renseignée.");
    }

    [TestMethod]
    public void Zip_StreamNull_Leve()
    {
        Check.ThatCode(() => _zipService.Zip(null!, "fichier.txt"))
             .Throws<KrosoftTechnicalException>()
             .WithMessage("La variable 'stream' n'est pas renseignée.");
    }

    [TestMethod]
    public void ZipAsync_DictionnaireDeCheminsNull_Leve()
    {
        Check.ThatCode(async () => await _zipService.ZipAsync((IReadOnlyDictionary<string, string>)null!, CancellationToken.None))
             .Throws<KrosoftTechnicalException>()
             .WithMessage("La variable 'dictionary' n'est pas renseignée.");
    }

    [TestMethod]
    public void ZipAsync_DictionnaireDeCheminsNullAvecNomDeFichier_Leve()
    {
        Check.ThatCode(async () => await _zipService.ZipAsync((IReadOnlyDictionary<string, string>)null!, "archive.zip", CancellationToken.None))
             .Throws<KrosoftTechnicalException>()
             .WithMessage("La variable 'dictionary' n'est pas renseignée.");
    }

    [TestMethod]
    public void ZipAsync_DictionnaireDeFluxNull_Leve()
    {
        Check.ThatCode(async () => await _zipService.ZipAsync((IReadOnlyDictionary<string, Stream>)null!, CancellationToken.None))
             .Throws<KrosoftTechnicalException>()
             .WithMessage("La variable 'dictionary' n'est pas renseignée.");
    }

    [TestMethod]
    public void ZipAsync_DictionnaireDeFluxNullAvecNomDeFichier_Leve()
    {
        Check.ThatCode(async () => await _zipService.ZipAsync((IReadOnlyDictionary<string, Stream>)null!, "archive.zip", CancellationToken.None))
             .Throws<KrosoftTechnicalException>()
             .WithMessage("La variable 'dictionary' n'est pas renseignée.");
    }

    [TestMethod]
    [DataRow((string?)null)]
    [DataRow("")]
    [DataRow("   ")]
    public void ZipAsync_NomDeFichierNonRenseignePourDesChemins_Leve(string? fileName)
    {
        IReadOnlyDictionary<string, string> dictionary = new Dictionary<string, string>();

        Check.ThatCode(async () => await _zipService.ZipAsync(dictionary, fileName!, CancellationToken.None))
             .Throws<KrosoftTechnicalException>()
             .WithMessage("La variable 'fileName' est vide ou non renseignée.");
    }

    [TestMethod]
    [DataRow((string?)null)]
    [DataRow("")]
    [DataRow("   ")]
    public void ZipAsync_NomDeFichierNonRenseignePourDesFlux_Leve(string? fileName)
    {
        IReadOnlyDictionary<string, Stream> dictionary = new Dictionary<string, Stream>();

        Check.ThatCode(async () => await _zipService.ZipAsync(dictionary, fileName!, CancellationToken.None))
             .Throws<KrosoftTechnicalException>()
             .WithMessage("La variable 'fileName' est vide ou non renseignée.");
    }
}
