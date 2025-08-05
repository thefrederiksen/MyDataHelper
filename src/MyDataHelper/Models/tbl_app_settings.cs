using System.ComponentModel.DataAnnotations;

namespace MyDataHelper.Models
{
    public class tbl_app_settings
    {
        [Key]
        [MaxLength(100)]
        public string setting_key { get; set; } = string.Empty;
        
        public string? setting_value { get; set; }
        
        [MaxLength(50)]
        public string? data_type { get; set; } // string, int, bool, json
        
        [MaxLength(255)]
        public string? description { get; set; }
        
        public DateTime last_modified { get; set; }
    }
}