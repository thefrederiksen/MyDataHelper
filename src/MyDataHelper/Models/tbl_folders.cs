using System;
using System.ComponentModel.DataAnnotations;

namespace MyDataHelper.Models
{
    public class tbl_folders
    {
        public int id { get; set; }
        
        public int? parent_folder_id { get; set; }
        
        public int scan_root_id { get; set; }
        
        [Required]
        [MaxLength(500)]
        public string path { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(255)]
        public string name { get; set; } = string.Empty;
        
        public long total_size { get; set; }
        
        public long folder_size { get; set; } // Size of files directly in this folder
        
        public int file_count { get; set; } // Number of files directly in this folder
        
        public int total_file_count { get; set; } // Total files including subfolders
        
        public int subfolder_count { get; set; }
        
        public DateTime last_modified { get; set; }
        
        public DateTime? last_scanned { get; set; }
        
        public int depth { get; set; } // Depth from scan root (0 = root)
        
        public bool is_accessible { get; set; } = true;
        
        public string? error_message { get; set; }
        
        // Navigation properties
        public virtual tbl_scan_roots? scan_root { get; set; }
        public virtual tbl_folders? parent_folder { get; set; }
        public virtual ICollection<tbl_folders> subfolders { get; set; } = new List<tbl_folders>();
        public virtual ICollection<tbl_files> files { get; set; } = new List<tbl_files>();
    }
}