using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PromptLab.Models
{
    public class ModelStat
    {
        [Key, ForeignKey("ModelCatalog")]
        public int ModelId { get; set; }

        public ModelCatalog? ModelCatalog { get; set; }

        public int Wins { get; set; }

        public int Losses { get; set; }

        public int Ties { get; set; }

        public int BothBad { get; set; }

        public double EloRating { get; set; } = 1200.0; // Standart başlangıç Elo değeri eklenebilir

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}