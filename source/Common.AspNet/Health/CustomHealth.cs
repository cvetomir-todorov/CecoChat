using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Common.AspNet.Health;

public static class CustomHealth
{
    public static Task Writer(string serviceName, HttpContext context, HealthReport report)
    {
        CustomHealthReport customReport = new()
        {
            ServiceName = serviceName,
            ServiceVersion = GetVersion(),
            Runtime = RuntimeInformation.FrameworkDescription,
            Status = report.Status,
            Duration = report.TotalDuration,
            Dependencies = GetDependencies(report)
        };

        return context.Response.WriteAsJsonAsync(customReport, SerializerOptions);
    }

    private static string? GetVersion()
    {
        string? version = null;

        Assembly? entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly != null)
        {
            Version? assemblyVersion = entryAssembly.GetName().Version;
            if (assemblyVersion != null)
            {
                version = $"{assemblyVersion.Major}.{assemblyVersion.Minor}.{assemblyVersion.Build}";
            }
        }

        return version;
    }

    private static CustomHealthDependencyReport[] GetDependencies(HealthReport report)
    {
        CustomHealthDependencyReport[] dependencies = Array.Empty<CustomHealthDependencyReport>();

        if (report.Entries.Count > 0)
        {
            dependencies = new CustomHealthDependencyReport[report.Entries.Count];

            int index = 0;
            foreach (KeyValuePair<string, HealthReportEntry> entry in report.Entries)
            {
                dependencies[index] = new CustomHealthDependencyReport
                {
                    Name = entry.Key,
                    Status = entry.Value.Status,
                    Duration = entry.Value.Duration
                };
                index++;
            }
        }

        return dependencies;
    }

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };
}
