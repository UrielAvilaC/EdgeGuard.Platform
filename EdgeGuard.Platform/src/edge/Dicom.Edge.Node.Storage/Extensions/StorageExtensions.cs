using Dicom.Edge.Abstractions.Storage;
using Dicom.Edge.Node.Storage.Diagnostics;
using Dicom.Edge.Node.Storage.FileSystem;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;

namespace Dicom.Edge.Node.Storage.Extensions;

/// <summary>
/// Registers all Edge Node storage services:
/// <list type="bullet">
///   <item><see cref="IStorageProvider"/> → <see cref="LocalStorageProvider"/> (singleton).</item>
///   <item><see cref="DicomFileWriter"/> for atomic DICOM file writes (singleton).</item>
///   <item>OpenTelemetry tracing source for storage operations.</item>
/// </list>
/// </summary>
public static class StorageExtensions
{
    public static IServiceCollection AddEdgeStorage(this IServiceCollection services)
    {
        // Core services
        services.AddSingleton<IStorageProvider, LocalStorageProvider>();
        services.AddSingleton<DicomFileWriter>();

        // OpenTelemetry tracing
        services.ConfigureOpenTelemetryTracerProvider(builder =>
            builder.AddSource(StorageActivitySource.SourceName));

        return services;
    }
}
