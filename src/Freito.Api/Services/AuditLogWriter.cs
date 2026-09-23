using System.Text.Json;
using Freito.Domain.Entities;
using Freito.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Freito.Api.Services;

public sealed record PendingAuditChange(
    string Entity,
    string Action,
    int? EntityId,
    string? BeforeJson,
    object? After,
    Func<int>? ResolveEntityId = null)
{
    public static PendingAuditChange Created(string entity, object after, Func<int> id) =>
        new(entity, "Created", null, null, after, id);

    public static PendingAuditChange Updated(string entity, int id, string beforeJson, object after) =>
        new(entity, "Updated", id, beforeJson, after);

    public static PendingAuditChange Deleted(string entity, int id, string beforeJson) =>
        new(entity, "Deleted", id, beforeJson, null);
}

public sealed class AuditLogWriter(FreitoDbContext db)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static string Snapshot(object value) => JsonSerializer.Serialize(value, value.GetType(), JsonOptions);

    public async Task SaveAsync(int actorId, IReadOnlyCollection<PendingAuditChange> changes, CancellationToken cancellationToken)
    {
        IDbContextTransaction? transaction = null;
        if (db.Database.IsRelational() && db.Database.CurrentTransaction is null)
        {
            transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);

            foreach (var change in changes)
            {
                var entityId = change.ResolveEntityId?.Invoke() ?? change.EntityId;
                if (entityId is null or <= 0)
                {
                    throw new InvalidOperationException($"Audit change for {change.Entity} has no persisted entity ID.");
                }

                db.AuditLogs.Add(new AuditLog
                {
                    Entity = change.Entity,
                    EntityId = entityId.Value,
                    Action = change.Action,
                    ChangedByUserId = actorId,
                    ChangedAt = DateTime.UtcNow,
                    BeforeJson = change.BeforeJson,
                    AfterJson = change.After is null
                        ? null
                        : JsonSerializer.Serialize(change.After, change.After.GetType(), JsonOptions),
                });
            }

            await db.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }
        }
        catch
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            throw;
        }
        finally
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync();
            }
        }
    }
}
