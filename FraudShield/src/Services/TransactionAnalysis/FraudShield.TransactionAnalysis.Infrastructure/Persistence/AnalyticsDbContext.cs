using System.Text.Json;
using FraudShield.TransactionAnalysis.Domain.Common;
using FraudShield.TransactionAnalysis.Domain.Entities;
using FraudShield.TransactionAnalysis.Infrastructure.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FraudShield.TransactionAnalysis.Infrastructure.Persistence;

public class AnalyticsDbContext : DbContext
{
    private readonly IMediator _mediator;
    
    public DbSet<Analysis> Analyses { get; set; }
    public DbSet<RiskProfile> RiskProfiles { get; set; }
    public DbSet<MLModel> MLModels { get; set; }
    public DbSet<AnalysisRule> AnalysisRules { get; set; }
    public DbSet<AnalysisSession> AnalysisSessions { get; set; }

    public AnalyticsDbContext(
        DbContextOptions<AnalyticsDbContext> options,
        IMediator mediator) : base(options)
    {
        _mediator = mediator;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("uuid-ossp");
        modelBuilder.Ignore<DomainEvent>();
        modelBuilder.ConfigureRiskFactors();
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AnalyticsDbContext).Assembly);
        ConfigureValueConverters(modelBuilder);
    }
private void ConfigureValueConverters(ModelBuilder modelBuilder)
    {
        // String Dictionary için converter
        var stringDictionaryConverter = new ValueConverter<IDictionary<string, string>, string>(
            v => JsonSerializer.Serialize(v, new JsonSerializerOptions { WriteIndented = false }),
            v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) 
                 ?? new Dictionary<string, string>()
        );

        // String Dictionary için comparer
        var stringDictionaryComparer = new ValueComparer<IDictionary<string, string>>(
            (d1, d2) => d1.Count == d2.Count && !d1.Except(d2).Any(),
            d => d.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            d => new Dictionary<string, string>(d)
        );

        // Object Dictionary için converter (AnalysisMetrics için)
        var objectDictionaryConverter = new ValueConverter<IDictionary<string, object>, string>(
            v => JsonSerializer.Serialize(v, new JsonSerializerOptions { WriteIndented = false }),
            v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) 
                 ?? new Dictionary<string, object>()
        );

        // Object Dictionary için comparer
        var objectDictionaryComparer = new ValueComparer<IDictionary<string, object>>(
            (d1, d2) => d1.Count == d2.Count && !d1.Except(d2).Any(),
            d => d.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            d => new Dictionary<string, object>(d)
        );

        // Decimal Dictionary için converter
        var decimalDictionaryConverter = new ValueConverter<IDictionary<string, decimal>, string>(
            v => JsonSerializer.Serialize(v, new JsonSerializerOptions { WriteIndented = false }),
            v => JsonSerializer.Deserialize<Dictionary<string, decimal>>(v, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) 
                 ?? new Dictionary<string, decimal>()
        );

        // String List için converter
        var stringListConverter = new ValueConverter<IReadOnlyList<string>, string>(
            v => JsonSerializer.Serialize(v, new JsonSerializerOptions { WriteIndented = false }),
            v => JsonSerializer.Deserialize<List<string>>(v, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) 
                 ?? new List<string>()
        );

        // Converter'ları uygula
        modelBuilder.Entity<Analysis>(entity =>
        {
            entity.Property(e => e.Metadata)
                .HasConversion(stringDictionaryConverter)
                .Metadata.SetValueComparer(stringDictionaryComparer);

            // AnalysisMetrics için converter'lar
            entity.OwnsOne(e => e.Metrics, metrics =>
            {
                metrics.Property(m => m.FeatureImportance)
                    .HasConversion(decimalDictionaryConverter);

                metrics.Property(m => m.DetectedPatterns)
                    .HasConversion(stringListConverter);

                metrics.Property(m => m.AdditionalMetricsJson)
                    .HasColumnType("jsonb");
            });
        });

        modelBuilder.Entity<MLModel>(entity =>
        {
            entity.Property(e => e.Configuration)
                .HasConversion(stringDictionaryConverter)
                .Metadata.SetValueComparer(stringDictionaryComparer);
        });

        modelBuilder.Entity<AnalysisRule>(entity =>
        {
            entity.Property(e => e.Configuration)
                .HasConversion(stringDictionaryConverter)
                .Metadata.SetValueComparer(stringDictionaryComparer);
        });
    }

  

    private async Task PublishDomainEvents(CancellationToken cancellationToken)
    {
        var entities = ChangeTracker
            .Entries<AggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = entities
            .SelectMany(e => e.DomainEvents)
            .ToList();

        entities.ForEach(e => e.ClearDomainEvents());

        foreach (var domainEvent in domainEvents)
        {
            await _mediator.Publish(domainEvent, cancellationToken);
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Audit bilgilerini güncelle
        UpdateAuditFields();

        // Domain Events'leri topla
        var events = GetDomainEvents();

        // Domain Events'leri temizle
        ClearDomainEvents();

        // Değişiklikleri kaydet
        var result = await base.SaveChangesAsync(cancellationToken);

        // Domain Events'leri yayınla
        await PublishDomainEvents(events, cancellationToken);

        return result;
    }

    private void UpdateAuditFields()
    {
        var entries = ChangeTracker.Entries<Entity>();
        var now = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.LastModifiedAt = now;
            }
        }
    }

    private List<DomainEvent> GetDomainEvents()
    {
        var domainEntities = ChangeTracker.Entries<AggregateRoot>()
            .Where(x => x.Entity.DomainEvents.Any())
            .ToList();

        return domainEntities
            .SelectMany(x => x.Entity.DomainEvents)
            .ToList();
    }

    private void ClearDomainEvents()
    {
        var domainEntities = ChangeTracker.Entries<AggregateRoot>()
            .Where(x => x.Entity.DomainEvents.Any())
            .ToList();

        domainEntities.ForEach(e => e.Entity.ClearDomainEvents());
    }

    private async Task PublishDomainEvents(List<DomainEvent> events, CancellationToken cancellationToken)
    {
        foreach (var @event in events)
        {
            await _mediator.Publish(@event, cancellationToken);
        }
    }
}