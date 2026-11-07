using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PromptLab.Data;
using PromptLab.Models.ViewModels;
using PromptLab.Services;

namespace PromptLab.Controllers;

[Authorize(Roles = "Admin")]
public class AdminModelsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ModelSyncService _sync;

    public AdminModelsController(ApplicationDbContext db, ModelSyncService sync)
    {
        _db = db;
        _sync = sync;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? q, bool onlyFree = false)
    {
        var query = _db.ModelCatalogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var pattern = $"%{q.Trim()}%";
            query = query.Where(m =>
                EF.Functions.ILike(m.DisplayName, pattern) ||
                EF.Functions.ILike(m.OpenRouterModelId, pattern));
        }

        if (onlyFree)
        {
            query = query.Where(m => m.IsFree);
        }

        var models = await query
            .OrderByDescending(m => m.IsActive)
            .ThenBy(m => m.DisplayName)
            .ToListAsync();

        ViewData["Q"] = q;
        ViewData["OnlyFree"] = onlyFree;
        return View(models);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var model = await _db.ModelCatalogs.FindAsync(id);
        if (model is null)
        {
            return NotFound();
        }

        var viewModel = new ModelEditViewModel
        {
            Id = model.Id,
            OpenRouterModelId = model.OpenRouterModelId,
            DisplayName = model.DisplayName,
            IsActive = model.IsActive
        };
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ModelEditViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        var model = await _db.ModelCatalogs.FindAsync(viewModel.Id);
        if (model is null)
        {
            return NotFound();
        }

        model.DisplayName = viewModel.DisplayName.Trim();
        model.IsActive = viewModel.IsActive;
        await _db.SaveChangesAsync();

        TempData["Message"] = $"{model.DisplayName} güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var model = await _db.ModelCatalogs.FindAsync(id);
        if (model is null)
        {
            return NotFound();
        }

        model.IsActive = !model.IsActive;
        await _db.SaveChangesAsync();

        TempData["Message"] = $"{model.DisplayName} {(model.IsActive ? "aktifleştirildi" : "pasifleştirildi")}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sync()
    {
        try
        {
            var result = await _sync.SyncAsync();
            TempData["Message"] =
                $"Senkronizasyon tamamlandı: {result.Added} yeni, {result.Updated} güncellenen, " +
                $"{result.Deactivated} pasifleştirilen, {result.Skipped} atlanan model.";
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            TempData["Error"] = "OpenRouter'a bağlanılamadı. Lütfen daha sonra tekrar deneyin.";
        }

        return RedirectToAction(nameof(Index));
    }
}
