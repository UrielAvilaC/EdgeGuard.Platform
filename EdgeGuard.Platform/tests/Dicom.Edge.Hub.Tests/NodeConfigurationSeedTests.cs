using Dicom.Edge.Abstractions.Persistence;
using Dicom.Edge.Common.Filters;
using Dicom.Edge.Common.Pagination;
using Dicom.Edge.Contracts.Configuration;
using Dicom.Edge.Hub.Application.NodeConfiguration;
using Dicom.Edge.Hub.Domain.Aggregates.NodeConfig;
using Dicom.Edge.Hub.Domain.Aggregates.Nodes;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Dicom.Edge.Hub.Tests;

/// <summary>
/// Cubre el sembrado del perfil de configuración de un nodo recién dado de alta.
///
/// <para>El AE title del nodo tiene una sola fuente de verdad, <c>dicom.ae_title</c>,
/// y el Hub la empuja al nodo en cada sync. Eso significa que el valor con el que
/// nace el perfil no es cosmético: es el que el nodo va a terminar obedeciendo. Si
/// nace con el default genérico "EDGE_NODE", el primer pull se lo empuja de vuelta
/// al nodo y le borra el AE con el que se registró.</para>
///
/// <para>La trampa está en el caso vacío. El override viene de lo que el nodo
/// reportó, y un nodo puede reportar un AE en blanco; sembrar eso dejaría la
/// pantalla de configuración vacía y el AE indefinido. El override tiene que ganar
/// solo cuando trae algo. Es una condición que compila igual en los dos sentidos.</para>
/// </summary>
public class NodeConfigurationSeedTests
{
    private const string ClaveAe = SharedNodeSettingKeys.Dicom.AeTitle;

    private static (NodeConfigurationService Servicio, RepositorioPerfilesFalso Repo) Construir()
    {
        var repo = new RepositorioPerfilesFalso();
        var servicio = new NodeConfigurationService(
            repo,
            new RepositorioNodosFalso(),
            new UnitOfWorkFalso(),
            NullLogger<NodeConfigurationService>.Instance);
        return (servicio, repo);
    }

    [Fact]
    public async Task El_override_reemplaza_el_default_generico()
    {
        var (servicio, repo) = Construir();

        await servicio.InitializeNodeDefaultsAsync(
            "nodo-1",
            new Dictionary<string, string> { [ClaveAe] = "CLINICA_01" });

        Assert.Equal("CLINICA_01", repo.Valor("nodo-1", ClaveAe));
    }

    [Fact]
    public async Task Sin_override_se_conserva_el_default_compartido()
    {
        var (servicio, repo) = Construir();

        await servicio.InitializeNodeDefaultsAsync("nodo-1");

        var esperado = SharedNodeSettingDefaults.All.First(d => d.Key == ClaveAe).DefaultValue;
        Assert.Equal(esperado, repo.Valor("nodo-1", ClaveAe));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Un_override_vacio_no_gana_sobre_el_default(string reportado)
    {
        var (servicio, repo) = Construir();

        await servicio.InitializeNodeDefaultsAsync(
            "nodo-1",
            new Dictionary<string, string> { [ClaveAe] = reportado });

        var esperado = SharedNodeSettingDefaults.All.First(d => d.Key == ClaveAe).DefaultValue;
        Assert.Equal(esperado, repo.Valor("nodo-1", ClaveAe));
    }

    [Fact]
    public async Task El_override_no_toca_las_demas_claves()
    {
        var (servicio, repo) = Construir();

        await servicio.InitializeNodeDefaultsAsync(
            "nodo-1",
            new Dictionary<string, string> { [ClaveAe] = "CLINICA_01" });

        foreach (var entrada in SharedNodeSettingDefaults.All.Where(d => d.Key != ClaveAe))
            Assert.Equal(entrada.DefaultValue, repo.Valor("nodo-1", entrada.Key));
    }

    [Fact]
    public async Task Es_idempotente_y_no_repisa_lo_ya_sembrado()
    {
        var (servicio, repo) = Construir();

        await servicio.InitializeNodeDefaultsAsync(
            "nodo-1", new Dictionary<string, string> { [ClaveAe] = "CLINICA_01" });
        await servicio.InitializeNodeDefaultsAsync(
            "nodo-1", new Dictionary<string, string> { [ClaveAe] = "OTRO_AE" });

        Assert.Equal("CLINICA_01", repo.Valor("nodo-1", ClaveAe));
        Assert.Equal(SharedNodeSettingDefaults.All.Count, repo.Perfiles.Count);
    }

    [Fact]
    public async Task GetSettingValue_no_siembra_defaults_al_consultar()
    {
        var (servicio, repo) = Construir();

        var valor = await servicio.GetSettingValueAsync("nodo-sin-perfiles", ClaveAe);

        Assert.Null(valor);
        Assert.Empty(repo.Perfiles);
    }

    // ── Dobles ────────────────────────────────────────────────────────────────

    private sealed class RepositorioPerfilesFalso : INodeConfigurationProfileRepository
    {
        public List<NodeConfigurationProfile> Perfiles { get; } = [];

        public string? Valor(string nodeId, string clave) => Perfiles
            .FirstOrDefault(p => p.NodeId == nodeId && p.SettingKey == clave)?.Value;

        public Task AddRangeAsync(IEnumerable<NodeConfigurationProfile> profiles, CancellationToken ct = default)
        {
            Perfiles.AddRange(profiles);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsForNodeAsync(string nodeId, CancellationToken ct = default) =>
            Task.FromResult(Perfiles.Any(p => p.NodeId == nodeId));

        public Task<NodeConfigurationProfile?> GetByNodeAndKeyAsync(string nodeId, string settingKey, CancellationToken ct = default) =>
            Task.FromResult(Perfiles.FirstOrDefault(p => p.NodeId == nodeId && p.SettingKey == settingKey));

        public Task<IReadOnlyList<NodeConfigurationProfile>> GetByNodeIdAsync(string nodeId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<NodeConfigurationProfile>>(
                Perfiles.Where(p => p.NodeId == nodeId).ToList());

        public Task<IReadOnlyList<NodeConfigurationProfile>> GetByNodeAndCategoryAsync(string nodeId, string category, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<NodeConfigurationProfile>>(
                Perfiles.Where(p => p.NodeId == nodeId && p.Category == category).ToList());
    }

    /// <summary>El servicio solo lo usa para leer el límite de almacenamiento, que estas pruebas no ejercen.</summary>
    private sealed class RepositorioNodosFalso : INodeRepository
    {
        public Task<Node?> GetByIdAsync(string id, CancellationToken ct = default) => Task.FromResult<Node?>(null);
        public Task<Node?> GetByNameAndIpAsync(string name, string ipAddress, CancellationToken ct = default) => Task.FromResult<Node?>(null);
        public Task<IReadOnlyList<Node>> GetAllAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Node>>([]);
        public Task<IReadOnlyList<Node>> GetActiveNodesAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Node>>([]);
        public Task<Node?> GetWithPacsAssignmentsAsync(string id, CancellationToken ct = default) => Task.FromResult<Node?>(null);
        public Task<PagedResult<Node>> GetPagedAsync(PaginationRequest pagination, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<PagedResult<Node>> GetFilteredPagedAsync(PaginationRequest pagination, NodeFilterCriteria filter, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Node>> GetNodesWithApiKeyAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Node>>([]);
        public Task<Node> AddAsync(Node node, CancellationToken ct = default) => Task.FromResult(node);
        public Task UpdateAsync(Node node, CancellationToken ct = default) => Task.CompletedTask;
        public Task<int> CountAsync(CancellationToken ct = default) => Task.FromResult(0);
    }

    private sealed class UnitOfWorkFalso : IUnitOfWork
    {
        public bool HasActiveTransaction => false;
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<bool> BeginTransactionAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task CommitTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RollbackTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> operation, CancellationToken cancellationToken = default) => operation();
        public void Dispose() { }
    }
}
