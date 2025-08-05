using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace MyDataHelper.Services
{
    public class FileIconService : IFileIconService
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }
        
        [DllImport("shell32.dll")]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);
        
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);
        
        private const uint SHGFI_ICON = 0x100;
        private const uint SHGFI_LARGEICON = 0x0;
        private const uint SHGFI_SMALLICON = 0x1;
        private const uint SHGFI_USEFILEATTRIBUTES = 0x10;
        
        public Task<string> GetIconBase64Async(string extension)
        {
            try
            {
                var icon = GetSystemIcon($"dummy{extension}", false);
                if (icon != null)
                {
                    using (var bitmap = icon.ToBitmap())
                    using (var ms = new System.IO.MemoryStream())
                    {
                        bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        var base64 = Convert.ToBase64String(ms.ToArray());
                        return Task.FromResult($"data:image/png;base64,{base64}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, $"Failed to get icon for extension {extension}");
            }
            
            return Task.FromResult(GetDefaultIconBase64());
        }
        
        public Icon? GetSystemIcon(string filePath, bool largeIcon = false)
        {
            try
            {
                SHFILEINFO shinfo = new SHFILEINFO();
                uint flags = SHGFI_ICON | SHGFI_USEFILEATTRIBUTES;
                flags |= largeIcon ? SHGFI_LARGEICON : SHGFI_SMALLICON;
                
                IntPtr hImgSmall = SHGetFileInfo(filePath, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), flags);
                
                if (hImgSmall != IntPtr.Zero && shinfo.hIcon != IntPtr.Zero)
                {
                    Icon icon = (Icon)Icon.FromHandle(shinfo.hIcon).Clone();
                    DestroyIcon(shinfo.hIcon);
                    return icon;
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, $"Failed to get system icon for {filePath}");
            }
            
            return null;
        }
        
        public string GetIconClass(string extension)
        {
            var category = new FileTypeService().GetFileCategory(extension);
            
            return category switch
            {
                "Images" => "oi-image",
                "Videos" => "oi-video",
                "Audio" => "oi-musical-note",
                "Documents" => "oi-document",
                "Spreadsheets" => "oi-spreadsheet",
                "Archives" => "oi-file",
                "Code" => "oi-code",
                "Executables" => "oi-cog",
                "Databases" => "oi-database",
                _ => "oi-file"
            };
        }
        
        public string GetDefaultIconBase64()
        {
            // Simple default file icon as base64
            return "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAA4AAAAOCAYAAAAfSC3RAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAADJSURBVDhPY2AYBaNgFAwkYPz//z8T1ChmIBFgqmlkgipFA0RqJBuwaCRaI1YAUcuEDfz9+xdM//nzB0wj00+ePMEKeHl5Gf78+YPu3Llzwe1hYWEEm4SNkOPHjzP8+vWL4efPn2D669evDL9//2b48eMHw7dv3xh+//7N8OXLF7ArwAqxOZUJ2S9fvnxhuH//PsO9e/cYbt26xXDz5k2Ga9euMVy5coXh8uXLDOfPn2c4e/Ysw6lTpxj09fVxuhybU0fBKBgFgx4AAIH5ODF7HhO3AAAAAElFTkSuQmCC";
        }
    }
}