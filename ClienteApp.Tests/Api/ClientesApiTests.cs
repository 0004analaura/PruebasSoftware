
using System.Net;
using System.Net.Http.Json;
using ClienteApp.Core.Models;
using ClienteApp.Core.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ClienteApp.Tests.Api;

public class ClientesApiTests
{
    [Fact]
    public async Task Registrar_ClienteValido_DebeRetornar201()
    {
        // Arrange
        using var factory = new ClienteApiFactory();
        using var client = factory.CreateClient();

        var solicitud = new
        {
            Nombre = "Ana López",
            Correo = $"ana_{Guid.NewGuid():N}@example.com"
        };

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/Clientes",
            solicitud);

        // Assert
        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var cliente = await response.Content
            .ReadFromJsonAsync<ClienteRespuesta>();

        Assert.NotNull(cliente);
        Assert.Equal(solicitud.Nombre, cliente.Nombre);
        Assert.Equal(solicitud.Correo, cliente.Correo);
    }

    [Fact]
    public async Task Registrar_NombreVacio_DebeRetornar400()
    {
        // Arrange
        using var factory = new ClienteApiFactory();
        using var client = factory.CreateClient();

        var solicitud = new
        {
            Nombre = "",
            Correo = $"correo_{Guid.NewGuid():N}@example.com"
        };

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/Clientes",
            solicitud);

        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task GetCliente_ClienteExistente_Devuelve200OK()
    {
        // Arrange: iniciar la API (crea la tabla) y guardar un cliente
        // conocido directamente en la base SQLite aislada.
        using var factory = new ClienteApiFactory();
        using var client = factory.CreateClient();

        var repository = new ClienteRepository(factory.ConnectionString);
        var clienteConocido = new Cliente
        {
            Nombre = "Ana López",
            Correo = "ana@example.com"
        };
        repository.Agregar(clienteConocido);

        // Act
        var response = await client.GetAsync(
            $"/api/Clientes/{clienteConocido.Id}");

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var cliente = await response.Content
            .ReadFromJsonAsync<ClienteRespuesta>();

        Assert.NotNull(cliente);
        Assert.Equal(clienteConocido.Id, cliente.Id);
        Assert.Equal("Ana López", cliente.Nombre);
        Assert.Equal("ana@example.com", cliente.Correo);
    }

    [Fact]
    public async Task GetCliente_ClienteInexistente_Devuelve404NotFound()
    {
        // Arrange: base SQLite aislada y vacía.
        using var factory = new ClienteApiFactory();
        using var client = factory.CreateClient();

        const int idInexistente = 999;

        // Act
        var response = await client.GetAsync(
            $"/api/Clientes/{idInexistente}");

        // Assert
        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    private sealed record ClienteRespuesta(
        int Id,
        string Nombre,
        string Correo);
}

public sealed class ClienteApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(
        Path.GetTempPath(),
        $"clientes_api_test_{Guid.NewGuid():N}.db");

    public string ConnectionString =>
        $"Data Source={_dbPath};Pooling=False";

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        // Program lee la cadena de conexión antes de Build(), por lo que
        // ConfigureAppConfiguration llega tarde; UseSetting se aplica antes.
        builder.UseSetting(
            "ConnectionStrings:ClientesDb",
            ConnectionString);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing && File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }
}
