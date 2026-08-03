using System.IO.Compression;
using System.Text;

namespace Krosoft.Extensions.Zip.Tests.Core;

/// <summary>
/// Outils de manipulation d'archives ZIP pour les tests.
/// </summary>
internal static class ZipTestHelper
{
    /// <summary>
    /// Crée un flux en mémoire à partir d'un contenu textuel.
    /// </summary>
    public static Stream CreerFlux(string contenu) => new MemoryStream(Encoding.UTF8.GetBytes(contenu));

    /// <summary>
    /// Lit toutes les entrées d'une archive, avec leur contenu décompressé.
    /// </summary>
    public static IReadOnlyList<EntreeZip> LireEntrees(Stream zipStream)
    {
        zipStream.Position = 0;

        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, true);

        var entrees = new List<EntreeZip>();
        foreach (var entry in archive.Entries)
        {
            using var entryStream = entry.Open();
            using var reader = new StreamReader(entryStream, Encoding.UTF8);
            entrees.Add(new EntreeZip(entry.FullName, reader.ReadToEnd()));
        }

        zipStream.Position = 0;

        return entrees;
    }

    /// <summary>
    /// Retourne le contenu de l'entrée portant le nom demandé.
    /// </summary>
    public static string ContenuDe(IReadOnlyList<EntreeZip> entrees, string nom) => entrees.Single(x => x.Nom == nom).Contenu;

    /// <summary>
    /// Retourne les noms des entrées de l'archive.
    /// </summary>
    public static IReadOnlyList<string> NomsDe(IReadOnlyList<EntreeZip> entrees) => entrees.Select(x => x.Nom).ToList();
}
