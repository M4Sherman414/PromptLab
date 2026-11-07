using System.Globalization;
using Microsoft.EntityFrameworkCore;
using PromptLab.Data;
using PromptLab.Models;

namespace PromptLab.Services;

public record SyncResult(int Added, int Updated, int Deactivated, int Skipped);

public class ModelSyncService
{
    private readonly ApplicationDbContext _db;
    private readonly OpenRouterClient _client;

    public ModelSyncService(ApplicationDbContext db, OpenRouterClient client)
    {
        _db = db;
        _client = client;
    }

    public async Task<SyncResult> SyncAsync(CancellationToken ct = default)
    {
        var remoteModels = await _client.GetModelsAsync(ct);
        var existing = await _db.ModelCatalogs.ToDictionaryAsync(m => m.OpenRouterModelId, ct);

        var now = DateTime.UtcNow;
        var seenIds = new HashSet<string>();
        int added = 0, updated = 0, deactivated = 0, skipped = 0;

        foreach (var remote in remoteModels)
        {
            if (string.IsNullOrWhiteSpace(remote.Id) || remote.Id.Length > 200
                || !TryParsePrice(remote.Pricing?.Prompt, out var inputPerToken)
                || !TryParsePrice(remote.Pricing?.Completion, out var outputPerToken))
            {
                skipped++;
                continue;
            }

            seenIds.Add(remote.Id);

            var inputPerMillion = inputPerToken * 1_000_000m;
            var outputPerMillion = outputPerToken * 1_000_000m;
            var isFree = inputPerToken == 0 && outputPerToken == 0;

            if (existing.TryGetValue(remote.Id, out var model))
            {
                model.InputPricePerMillion = inputPerMillion;
                model.OutputPricePerMillion = outputPerMillion;
                model.IsFree = isFree;
                model.LastSyncedAt = now;
                updated++;
            }
            else
            {
                var name = string.IsNullOrWhiteSpace(remote.Name) ? remote.Id : remote.Name.Trim();
                if (name.Length > 200)
                {
                    name = name[..200];
                }

                _db.ModelCatalogs.Add(new ModelCatalog
                {
                    OpenRouterModelId = remote.Id,
                    DisplayName = name,
                    InputPricePerMillion = inputPerMillion,
                    OutputPricePerMillion = outputPerMillion,
                    IsFree = isFree,
                    IsActive = true,
                    LastSyncedAt = now
                });
                added++;
            }
        }

        foreach (var model in existing.Values)
        {
            if (!seenIds.Contains(model.OpenRouterModelId) && model.IsActive)
            {
                model.IsActive = false;
                deactivated++;
            }
        }

        await _db.SaveChangesAsync(ct);
        return new SyncResult(added, updated, deactivated, skipped);
    }

    private static bool TryParsePrice(string? value, out decimal price)
    {
        return decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out price)
            && price >= 0;
    }
}
