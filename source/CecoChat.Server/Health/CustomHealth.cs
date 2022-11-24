using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CecoChat.Server.Health;

public static class CustomHealth
{
    public static Task Writer(string serviceName, HttpContext context, HealthReport report)
    {
        CustomHealthReport customReport = new()
        {
            ServiceName = serviceName,
            Runtime = RuntimeInformation.FrameworkDescription,
            Status = report.Status,
            Duration = report.TotalDuration
        };

        SetVersion(customReport);
        SetDependencies(report, customReport);

        return context.Response.WriteAsJsonAsync(customReport, SerializerOptions);
    }

    private static void SetVersion(CustomHealthReport customReport)
    {
        Assembly? entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly != null)
        {
            Version? version = entryAssembly.GetName().Version;
            if (version != null)
            {
                customReport.ServiceVersion = $"{version.Major}.{version.Minor}.{version.Build}";
            }
        }
    }

    private static void SetDependencies(HealthReport report, CustomHealthReport customReport)
    {
        if (report.Entries.Count > 0)
        {
            customReport.Dependencies = new CustomHealthDependencyReport[report.Entries.Count];

            int index = 0;
            foreach (KeyValuePair<string, HealthReportEntry> entry in report.Entries)
            {
                customReport.Dependencies[index] = new CustomHealthDependencyReport
                {
                    Name = entry.Key,
                    Status = entry.Value.Status,
                    Duration = entry.Value.Duration
                };
                index++;
            }
        }
    }

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };
}
