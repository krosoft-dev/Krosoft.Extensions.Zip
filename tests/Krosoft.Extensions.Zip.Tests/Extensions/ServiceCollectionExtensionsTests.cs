using Krosoft.Extensions.Testing;
using Krosoft.Extensions.Zip.Extensions;
using Krosoft.Extensions.Zip.Interfaces;
using Krosoft.Extensions.Zip.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Krosoft.Extensions.Zip.Tests.Extensions;

[TestClass]
public class ServiceCollectionExtensionsTests : BaseTest
{
    [TestMethod]
    public void AddZip_AppeleeDeuxFois_EnregistreDeuxFoisLeService()
    {
        var services = new ServiceCollection();

        services.AddZip();
        services.AddZip();

        var descripteurs = services.Where(x => x.ServiceType == typeof(IZipService)).ToList();
        Check.That(descripteurs).HasSize(2);
    }

    [TestMethod]
    public void AddZip_ChaqueResolutionRetourneUneNouvelleInstance()
    {
        var serviceProvider = CreateServiceCollection(services => services.AddZip());

        var premiereInstance = serviceProvider.GetRequiredService<IZipService>();
        var secondeInstance = serviceProvider.GetRequiredService<IZipService>();

        Check.That(premiereInstance).IsNotNull();
        Check.That(secondeInstance).IsNotNull();
        Check.That(premiereInstance).Not.IsSameReferenceAs(secondeInstance);
    }

    [TestMethod]
    public void AddZip_EnregistreZipServiceEnTransient()
    {
        var services = new ServiceCollection();

        services.AddZip();

        var descripteur = services.Single(x => x.ServiceType == typeof(IZipService));
        Check.That(descripteur.ImplementationType).IsEqualTo(typeof(ZipService));
        Check.That(descripteur.Lifetime).IsEqualTo(ServiceLifetime.Transient);
    }

    [TestMethod]
    public void AddZip_RetourneLaCollectionDeServices()
    {
        var services = new ServiceCollection();

        var resultat = services.AddZip();

        Check.That(resultat).IsSameReferenceAs(services);
    }

    [TestMethod]
    public void AddZip_ZipServiceEstResolvableDepuisLeConteneur()
    {
        var serviceProvider = CreateServiceCollection(services => services.AddZip());

        var zipService = serviceProvider.GetRequiredService<IZipService>();

        Check.That(zipService).IsNotNull();
        Check.That(zipService).IsInstanceOf<ZipService>();
    }

    [TestMethod]
    public void AddZip_ZipServiceNestPasEnregistreSansAppel()
    {
        var serviceProvider = CreateServiceCollection();

        var zipService = serviceProvider.GetService<IZipService>();

        Check.That(zipService).IsNull();
    }
}
