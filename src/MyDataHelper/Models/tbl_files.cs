using System;
using System.ComponentModel.DataAnnotations;

namespace MyDataHelper.Models
{
    public class tbl_files
    {
        public int id { get; set; }
        
        public int folder_id { get; set; }
        
        [Required]
        [MaxLength(255)]
        public string name { get; set; } = string.Empty;
        
        [MaxLength(50)]
        public string? extension { get; set; }
        
        public long size { get; set; }
        
        public DateTime created { get; set; }
        
        public DateTime last_modified { get; set; }
        
        public DateTime last_accessed { get; set; }
        
        [MaxLength(64)]
        public string? hash { get; set; } // SHA256 hash for duplicate detection
        
        public bool is_readonly { get; set; }
        
        public bool is_hidden { get; set; }
        
        public bool is_system { get; set; }
        
        public bool is_archive { get; set; }
        
        public bool is_compressed { get; set; }
        
        public bool is_encrypted { get; set; }
        
        // Navigation property
        public virtual tbl_folders? folder { get; set; }
    }
}