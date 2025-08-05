-- MyDataHelper Database Schema v1.0.0
-- Initial database creation script

-- Version tracking table
CREATE TABLE IF NOT EXISTS tbl_version (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    version INTEGER NOT NULL,
    applied_date DATETIME NOT NULL
);

-- Application settings
CREATE TABLE IF NOT EXISTS tbl_app_settings (
    setting_key TEXT PRIMARY KEY,
    setting_value TEXT,
    data_type TEXT,
    description TEXT,
    last_modified DATETIME NOT NULL
);

-- Scan roots (drives/folders to scan)
CREATE TABLE IF NOT EXISTS tbl_scan_roots (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    path TEXT NOT NULL UNIQUE,
    display_name TEXT NOT NULL,
    is_active INTEGER NOT NULL DEFAULT 1,
    include_subdirectories INTEGER NOT NULL DEFAULT 1,
    follow_symlinks INTEGER NOT NULL DEFAULT 0,
    last_scan_time DATETIME,
    last_scan_size INTEGER,
    last_scan_file_count INTEGER,
    last_scan_folder_count INTEGER,
    last_scan_duration TEXT,
    drive_type TEXT,
    volume_label TEXT,
    total_space INTEGER,
    free_space INTEGER
);

-- Folders table
CREATE TABLE IF NOT EXISTS tbl_folders (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    parent_folder_id INTEGER,
    scan_root_id INTEGER NOT NULL,
    path TEXT NOT NULL,
    name TEXT NOT NULL,
    total_size INTEGER NOT NULL DEFAULT 0,
    folder_size INTEGER NOT NULL DEFAULT 0,
    file_count INTEGER NOT NULL DEFAULT 0,
    total_file_count INTEGER NOT NULL DEFAULT 0,
    subfolder_count INTEGER NOT NULL DEFAULT 0,
    last_modified DATETIME NOT NULL,
    last_scanned DATETIME,
    depth INTEGER NOT NULL DEFAULT 0,
    is_accessible INTEGER NOT NULL DEFAULT 1,
    error_message TEXT,
    FOREIGN KEY (parent_folder_id) REFERENCES tbl_folders(id) ON DELETE CASCADE,
    FOREIGN KEY (scan_root_id) REFERENCES tbl_scan_roots(id) ON DELETE CASCADE
);

-- Files table
CREATE TABLE IF NOT EXISTS tbl_files (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    folder_id INTEGER NOT NULL,
    name TEXT NOT NULL,
    extension TEXT,
    size INTEGER NOT NULL,
    created DATETIME NOT NULL,
    last_modified DATETIME NOT NULL,
    last_accessed DATETIME NOT NULL,
    hash TEXT,
    is_readonly INTEGER NOT NULL DEFAULT 0,
    is_hidden INTEGER NOT NULL DEFAULT 0,
    is_system INTEGER NOT NULL DEFAULT 0,
    is_archive INTEGER NOT NULL DEFAULT 0,
    is_compressed INTEGER NOT NULL DEFAULT 0,
    is_encrypted INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (folder_id) REFERENCES tbl_folders(id) ON DELETE CASCADE
);

-- File types summary
CREATE TABLE IF NOT EXISTS tbl_file_types (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    extension TEXT NOT NULL UNIQUE,
    category TEXT,
    description TEXT,
    total_size INTEGER NOT NULL DEFAULT 0,
    file_count INTEGER NOT NULL DEFAULT 0,
    average_size INTEGER NOT NULL DEFAULT 0,
    min_size INTEGER NOT NULL DEFAULT 0,
    max_size INTEGER NOT NULL DEFAULT 0,
    percentage_of_total REAL NOT NULL DEFAULT 0,
    color_code TEXT,
    last_updated DATETIME NOT NULL
);

-- Scan history
CREATE TABLE IF NOT EXISTS tbl_scan_history (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    scan_root_id INTEGER NOT NULL,
    start_time DATETIME NOT NULL,
    end_time DATETIME,
    status TEXT NOT NULL,
    files_scanned INTEGER NOT NULL DEFAULT 0,
    folders_scanned INTEGER NOT NULL DEFAULT 0,
    total_size_scanned INTEGER NOT NULL DEFAULT 0,
    new_files INTEGER NOT NULL DEFAULT 0,
    updated_files INTEGER NOT NULL DEFAULT 0,
    deleted_files INTEGER NOT NULL DEFAULT 0,
    new_folders INTEGER NOT NULL DEFAULT 0,
    deleted_folders INTEGER NOT NULL DEFAULT 0,
    errors INTEGER NOT NULL DEFAULT 0,
    duration TEXT,
    scan_speed_mbps REAL,
    error_message TEXT,
    scan_type TEXT,
    FOREIGN KEY (scan_root_id) REFERENCES tbl_scan_roots(id) ON DELETE CASCADE
);

-- Create indexes for performance
CREATE INDEX IF NOT EXISTS idx_folders_parent ON tbl_folders(parent_folder_id);
CREATE INDEX IF NOT EXISTS idx_folders_scan_root ON tbl_folders(scan_root_id);
CREATE UNIQUE INDEX IF NOT EXISTS idx_folders_path_scan_root ON tbl_folders(path, scan_root_id);
CREATE INDEX IF NOT EXISTS idx_folders_size ON tbl_folders(total_size);
CREATE INDEX IF NOT EXISTS idx_folders_modified ON tbl_folders(last_modified);

CREATE INDEX IF NOT EXISTS idx_files_folder ON tbl_files(folder_id);
CREATE INDEX IF NOT EXISTS idx_files_size ON tbl_files(size);
CREATE INDEX IF NOT EXISTS idx_files_extension ON tbl_files(extension);
CREATE INDEX IF NOT EXISTS idx_files_hash ON tbl_files(hash);
CREATE INDEX IF NOT EXISTS idx_files_modified ON tbl_files(last_modified);
CREATE UNIQUE INDEX IF NOT EXISTS idx_files_name_folder ON tbl_files(name, folder_id);

CREATE INDEX IF NOT EXISTS idx_file_types_size ON tbl_file_types(total_size);
CREATE INDEX IF NOT EXISTS idx_file_types_count ON tbl_file_types(file_count);

CREATE INDEX IF NOT EXISTS idx_scan_history_root ON tbl_scan_history(scan_root_id);
CREATE INDEX IF NOT EXISTS idx_scan_history_time ON tbl_scan_history(start_time);

-- Insert initial version
INSERT INTO tbl_version (version, applied_date) VALUES (1, datetime('now'));