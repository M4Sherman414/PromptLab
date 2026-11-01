using System.ComponentModel.DataAnnotations;

namespace PromptLab.Models.Dtos;

public class ModelCatalogUpdateDto
{
    [Required, MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;
    
    [Range(typeof(decimal),"0","1000000")]
    public decimal InputPricePerMillion { get; set;}
    
    [Range(typeof(decimal),"0","1000000")]
    public decimal OutputPricePerMillion { get; set;}
 
    public bool IsFree { get; set; }
    public bool IsActive { get; set; }   
}