using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgriSmart.Web.Models
{
    public class AdvisoryRecord
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public DateTime Date { get; set; }

        [Required, MaxLength(100)]
        public string CropName { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }

        [Required, MaxLength(50)]
        public string Tag { get; set; } // Fertilizer, Pest Control, Irrigation, Harvesting
    }
}
