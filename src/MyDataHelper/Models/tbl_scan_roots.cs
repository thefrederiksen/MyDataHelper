using System;
using System.ComponentModel.DataAnnotations;

namespace MyDataHelper.Models
{
    public class tbl_scan_roots
    {
        public int id { get; set; }
        
        [Required]
        [MaxLength(500)]
        public string path { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(255)]
        public string display_name { get; set; } = string.Empty;
        
        public bool is_active { get; set; } = true;
        
        public bool include_subdirectories { get; set; } = true;
        
        public bool follow_symlinks { get; set; } = false;
        
        public DateTime? last_scan_time { get; set; }
        
        public long? last_scan_size { get; set; }
        
        public int? last_scan_file_count { get; set; }
        
        public int? last_scan_folder_count { get; set; }
        
        public TimeSpan? last_scan_duration { get; set; }
        
        public string? drive_type { get; set; } // Fixed, Network, Removable, etc.
        
        public string? volume_label { get; set; }
        
        public long? total_space { get; set; }
        
        public long? free_space { get; set; }
        
        // Navigation properties
        public virtual ICollection<tbl_folders> folders { get; set; } = new List<tbl_folders>();
        public virtual ICollection<tbl_scan_history> scan_history { get; set; } = new List<tbl_scan_history>();
    }
}