using System;
using System.ComponentModel.DataAnnotations;

namespace MyDataHelper.Models
{
    public class tbl_file_types
    {
        public int id { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string extension { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string? category { get; set; } // Documents, Images, Videos, Audio, Archives, Code, etc.
        
        [MaxLength(255)]
        public string? description { get; set; }
        
        public long total_size { get; set; }
        
        public int file_count { get; set; }
        
        public long average_size { get; set; }
        
        public long min_size { get; set; }
        
        public long max_size { get; set; }
        
        public double percentage_of_total { get; set; } // Percentage of total scanned size
        
        [MaxLength(7)]
        public string? color_code { get; set; } // Hex color for visualization
        
        public DateTime last_updated { get; set; }
    }
}