using Microsoft.Extensions.DependencyInjection;
using TivuStream.Pie.Model;
using TivuStream.Pie.Model.Entities;
using TivuStream.Pie.Model.Enums;
using TivuStream.Pie.Storage;

namespace TivuStream.Pie.Api.Tests;

/// <summary>
/// Puts observations into the database the host reads from.
/// </summary>
/// <remarks>
/// The API is only a reader. What it says depends on what was stored, so the
/// tests store it through the same repositories the acquisition uses.
/// </remarks>
internal static class Seed
{
    private static readonly Guid DataSourceId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    internal static ObservationPeriod HoursAgo(int hours)
    {
        return ObservationPeriod.Containing(PieApplication.Now.AddHours(-hours));
    }

    internal static void Acquisition(
        PieApplication app,
        ObservationPeriod period,
        IReadOnlyList<Domain>? domains = null,
        IReadOnlyList<Device>? devices = null,
        IReadOnlyList<DomainActivity>? activities = null,
        bool offersActivity = true)
    {
        List<string> capabilities = ["Statistics", "Device", "Domain"];

        if (offersActivity)
        {
            capabilities.Add("DomainActivity");
        }

        app.Services.GetRequiredService<AcquisitionRepository>().Save(new StoredAcquisition
        {
            Period = period,
            ObservedAt = period.End.AddMinutes(-1),
            DataSource = new DataSource
            {
                Id = DataSourceId,
                Name = "dns.home.test",
                Provider = "Technitium DNS Server",
                Version = "14.3",
                Status = DataSourceStatus.Online,
                Capabilities = capabilities,
                LastUpdate = period.End.AddMinutes(-1),
            },
            Statistics = new Statistics
            {
                TotalQueries = 1000,
                BlockedQueries = 100,
                CachedQueries = 400,
                FailedQueries = 9,
                UniqueDomains = 2,
                UniqueDomainsQuality = MeasurementQuality.LowerBound,
                ActiveDevices = 3,
                EncryptedQueries = 16,
                DnssecEnabled = true,
            },
            Devices = devices ?? [],
            Domains = domains ?? [],
            DomainActivities = activities ?? [],
        });
    }

    internal static Domain Domain(string name, ObservationPeriod period, long occurrences)
    {
        return new Domain
        {
            Name = name,
            Category = ThreatCategory.Unknown,
            FirstSeen = period.Start,
            LastSeen = period.End,
            ObservationQuality = MeasurementQuality.PeriodBounded,
            Occurrences = occurrences,
        };
    }

    internal static Device Device(string address, DeviceIdentityBasis basis, ObservationPeriod period)
    {
        return new Device
        {
            DeviceId = Guid.NewGuid(),
            IpAddress = address,
            FirstSeen = period.Start,
            LastSeen = period.End,
            ObservationQuality = MeasurementQuality.PeriodBounded,
            IdentityBasis = basis,
            Status = DeviceStatus.Active,
        };
    }

    internal static DomainActivity Activity(string domain, ObservationPeriod period, long queries)
    {
        return new DomainActivity
        {
            DeviceId = Guid.NewGuid(),
            Domain = domain,
            QueryCount = queries,
            Blocked = false,
            Protocol = "Udp",
            FirstSeen = period.Start,
            LastSeen = period.End,
        };
    }

    internal static void Score(PieApplication app, ObservationPeriod period, Npss score)
    {
        app.Services.GetRequiredService<ScoreRepository>().Save(DataSourceId, period, score);
    }
}
