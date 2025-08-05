using Microsoft.EntityFrameworkCore;
using MyDataHelper.Models;

namespace MyDataHelper.Data
{
    public class MyDataHelperDbContext : DbContext
    {
        public MyDataHelperDbContext(DbContextOptions<MyDataHelperDbContext> options)
            : base(options)
        {
        }

        public DbSet<tbl_folders> tbl_folders { get; set; } = null!;
        public DbSet<tbl_files> tbl_files { get; set; } = null!;
        public DbSet<tbl_scan_roots> tbl_scan_roots { get; set; } = null!;
        public DbSet<tbl_file_types> tbl_file_types { get; set; } = null!;
        public DbSet<tbl_scan_history> tbl_scan_history { get; set; } = null!;
        public DbSet<tbl_app_settings> tbl_app_settings { get; set; } = null!;
        public DbSet<tbl_version> tbl_version { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure folder entity
            modelBuilder.Entity<tbl_folders>(entity =>
            {
                entity.HasKey(e => e.id);
                entity.HasIndex(e => e.parent_folder_id);
                entity.HasIndex(e => e.scan_root_id);
                entity.HasIndex(e => new { e.path, e.scan_root_id }).IsUnique();
                entity.HasIndex(e => e.total_size);
                entity.HasIndex(e => e.last_modified);
            });

            // Configure file entity
            modelBuilder.Entity<tbl_files>(entity =>
            {
                entity.HasKey(e => e.id);
                entity.HasIndex(e => e.folder_id);
                entity.HasIndex(e => e.size);
                entity.HasIndex(e => e.extension);
                entity.HasIndex(e => e.hash);
                entity.HasIndex(e => e.last_modified);
                entity.HasIndex(e => new { e.name, e.folder_id }).IsUnique();
            });

            // Configure scan roots
            modelBuilder.Entity<tbl_scan_roots>(entity =>
            {
                entity.HasKey(e => e.id);
                entity.HasIndex(e => e.path).IsUnique();
            });

            // Configure file types
            modelBuilder.Entity<tbl_file_types>(entity =>
            {
                entity.HasKey(e => e.id);
                entity.HasIndex(e => e.extension).IsUnique();
                entity.HasIndex(e => e.total_size);
                entity.HasIndex(e => e.file_count);
            });

            // Configure scan history
            modelBuilder.Entity<tbl_scan_history>(entity =>
            {
                entity.HasKey(e => e.id);
                entity.HasIndex(e => e.scan_root_id);
                entity.HasIndex(e => e.start_time);
            });

            // Configure app settings
            modelBuilder.Entity<tbl_app_settings>(entity =>
            {
                entity.HasKey(e => e.setting_key);
            });

            // Configure version
            modelBuilder.Entity<tbl_version>(entity =>
            {
                entity.HasKey(e => e.id);
            });
        }
    }
}