using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PromptLab.Data;
using PromptLab.Models;
using PromptLab.Models.Dtos;

namespace PromptLab.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class ModelCatalogController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ModelCatalogController(ApplicationDbContext db)
    {
        _db = db;
    }

    private static ModelCatalogDto ToDto(ModelCatalog m) => new()
    {
        Id = m.Id,
        OpenRouterModelId = m.OpenRouterModelId,
        DisplayName = m.DisplayName,
        InputPricePerMillion = m.InputPricePerMillion,
        OutputPricePerMillion = m.OutputPricePerMillion,
        IsFree = m.IsFree,
        IsActive = m.IsActive,
        LastSyncedAt = m.LastSyncedAt
    };

    [HttpGet]
    public async Task<ActionResult<List<ModelCatalogDto>>> GetAll()
    {
        var models = await _db.ModelCatalogs.AsNoTracking().ToListAsync();
        return models.Select(ToDto).ToList();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ModelCatalogDto>> GetById(int id)
    {
        var model = await _db.ModelCatalogs.FindAsync(id);
        return model is null ? NotFound() : ToDto(model);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ModelCatalogDto>> Create(ModelCatalogCreateDto dto)
    {
        var exists = await _db.ModelCatalogs.AnyAsync(m => m.OpenRouterModelId == dto.OpenRouterModelId);
        if (exists)
        {
            return Conflict("Bu OpenRouter kimliğine sahip bir model zaten var.");
        }

        var model = new ModelCatalog
        {
            OpenRouterModelId = dto.OpenRouterModelId,
            DisplayName = dto.DisplayName,
            InputPricePerMillion = dto.InputPricePerMillion,
            OutputPricePerMillion = dto.OutputPricePerMillion,
            IsFree = dto.IsFree,
            IsActive = dto.IsActive
        };

        _db.ModelCatalogs.Add(model);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new {id = model.Id}, ToDto(model));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, ModelCatalogUpdateDto dto)
    {
        var model = await _db.ModelCatalogs.FindAsync(id);
        if(model is null)
        {
           return NotFound(); 
        }

        model.DisplayName = dto.DisplayName;
        model.InputPricePerMillion = dto.InputPricePerMillion;
        model.OutputPricePerMillion = dto.OutputPricePerMillion;
        model.IsFree = dto.IsFree;
        model.IsActive = dto.IsActive;
        
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var model = await _db.ModelCatalogs.FindAsync(id);
        if (model is null)
        {
            return NotFound();
        }

        var isUsed = await _db.ComparisonResults.AnyAsync(r => r.ModelId == id);
        if (isUsed)
        {
            return Conflict("Bu model karşılaştırmalarda kullanıldı, silinemez. Pasifleştirin.");
        }

        _db.ModelCatalogs.Remove(model);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}