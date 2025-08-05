using System;
using System.ComponentModel.DataAnnotations;

namespace MyDataHelper.Models
{
    public class tbl_scan_history
    {
        public int id { get; set; }
        
        public int scan_root_id { get; set; }
        
        public DateTime start_time { get; set; }
        
        public DateTime? end_time { get; set; }
        
        [MaxLength(50)]
        public string status { get; set; } = "Running"; // Running, Completed, Failed, Cancelled
        
        public long files_scanned { get; set; }
        
        public long folders_scanned { get; set; }
        
        public long total_size_scanned { get; set; }
        
        public int new_files { get; set; }
        
        public int updated_files { get; set; }
        
        public int deleted_files { get; set; }
        
        public int new_folders { get; set; }
        
        public int deleted_folders { get; set; }
        
        public int errors { get; set; }
        
        public TimeSpan? duration { get; set; }
        
        public double? scan_speed_mbps { get; set; } // Megabytes per second
        
        public string? error_message { get; set; }
        
        [MaxLength(50)]
        public string? scan_type { get; set; } // Full, Incremental, Quick
        
        // Navigation property
        public virtual tbl_scan_roots? scan_root { get; set; }
    }
}