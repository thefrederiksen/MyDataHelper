# PowerShell script to convert SVG to ICO using .NET
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms

# Read SVG content
$svgPath = "C:\ReposFred\MyDataHelper\src\MyDataHelper\temp\database-icon.svg"
$svgContent = Get-Content $svgPath -Raw

# Create sizes for ICO (16x16, 32x32, 48x48, 64x64, 128x128, 256x256)
$sizes = @(16, 32, 48, 64, 128, 256)

# For now, let's create a simple database icon programmatically
# Since SVG to bitmap conversion requires additional libraries

function Create-DatabaseIcon {
    param([int]$size)
    
    $bitmap = New-Object System.Drawing.Bitmap $size, $size
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    
    # Clear background (transparent)
    $graphics.Clear([System.Drawing.Color]::Transparent)
    
    # Scale factors
    $scale = $size / 256.0
    
    # Define colors - modern blue gradient
    $darkBlue = [System.Drawing.Color]::FromArgb(41, 98, 255)    # Bright blue
    $lightBlue = [System.Drawing.Color]::FromArgb(80, 140, 255)  # Lighter blue
    $highlight = [System.Drawing.Color]::FromArgb(150, 255, 255, 255)
    
    # Create brushes
    $brush = New-Object System.Drawing.SolidBrush $darkBlue
    $lightBrush = New-Object System.Drawing.SolidBrush $lightBlue
    $highlightBrush = New-Object System.Drawing.SolidBrush $highlight
    
    # Calculate dimensions
    $padding = [int](20 * $scale)
    $width = $size - (2 * $padding)
    $height = [int]($width * 1.2)  # Make it taller for cylinder effect
    $ellipseHeight = [int]($width * 0.25)
    
    # Center vertically
    $y = ($size - $height) / 2
    
    # Draw bottom ellipse (darker)
    $graphics.FillEllipse($brush, $padding, $y + $height - $ellipseHeight, $width, $ellipseHeight)
    
    # Draw middle cylinder
    $graphics.FillRectangle($brush, $padding, $y + $ellipseHeight/2, $width, $height - $ellipseHeight)
    
    # Draw middle ring 1
    $ring1Y = $y + $height * 0.35
    $graphics.FillEllipse($lightBrush, $padding, $ring1Y - $ellipseHeight/2, $width, $ellipseHeight)
    $graphics.DrawEllipse([System.Drawing.Pens]::White, $padding, $ring1Y - $ellipseHeight/2, $width, $ellipseHeight)
    
    # Draw middle ring 2
    $ring2Y = $y + $height * 0.6
    $graphics.FillEllipse($lightBrush, $padding, $ring2Y - $ellipseHeight/2, $width, $ellipseHeight)
    $graphics.DrawEllipse([System.Drawing.Pens]::White, $padding, $ring2Y - $ellipseHeight/2, $width, $ellipseHeight)
    
    # Draw top ellipse (lightest - represents top of cylinder)
    $graphics.FillEllipse($lightBrush, $padding, $y, $width, $ellipseHeight)
    
    # Add highlight to top
    $highlightRect = New-Object System.Drawing.Rectangle ($padding + $width * 0.1), ($y + 2), ($width * 0.5), ($ellipseHeight * 0.4)
    $graphics.FillEllipse($highlightBrush, $highlightRect)
    
    # Clean up
    $brush.Dispose()
    $lightBrush.Dispose()
    $highlightBrush.Dispose()
    $graphics.Dispose()
    
    return $bitmap
}

# Create icon with multiple sizes
$icon256 = Create-DatabaseIcon -size 256
$icon128 = Create-DatabaseIcon -size 128
$icon64 = Create-DatabaseIcon -size 64
$icon48 = Create-DatabaseIcon -size 48
$icon32 = Create-DatabaseIcon -size 32
$icon16 = Create-DatabaseIcon -size 16

# Save as PNG first to verify
$icon256.Save("C:\ReposFred\MyDataHelper\src\MyDataHelper\temp\database-icon-256.png", [System.Drawing.Imaging.ImageFormat]::Png)
$icon128.Save("C:\ReposFred\MyDataHelper\src\MyDataHelper\temp\database-icon-128.png", [System.Drawing.Imaging.ImageFormat]::Png)
$icon64.Save("C:\ReposFred\MyDataHelper\src\MyDataHelper\temp\database-icon-64.png", [System.Drawing.Imaging.ImageFormat]::Png)
$icon32.Save("C:\ReposFred\MyDataHelper\src\MyDataHelper\temp\database-icon-32.png", [System.Drawing.Imaging.ImageFormat]::Png)
$icon16.Save("C:\ReposFred\MyDataHelper\src\MyDataHelper\temp\database-icon-16.png", [System.Drawing.Imaging.ImageFormat]::Png)

# Create ICO file manually
function Save-AsIco {
    param(
        [System.Drawing.Bitmap[]]$bitmaps,
        [string]$outputPath
    )
    
    $stream = [System.IO.FileStream]::new($outputPath, [System.IO.FileMode]::Create)
    $writer = [System.IO.BinaryWriter]::new($stream)
    
    # ICO header
    $writer.Write([uint16]0)  # Reserved
    $writer.Write([uint16]1)  # Type (1 = ICO)
    $writer.Write([uint16]$bitmaps.Length)  # Number of images
    
    # Calculate offsets
    $headerSize = 6 + (16 * $bitmaps.Length)
    $currentOffset = $headerSize
    
    # Write directory entries
    foreach ($bitmap in $bitmaps) {
        $writer.Write([byte]($bitmap.Width % 256))   # Width (0 = 256)
        $writer.Write([byte]($bitmap.Height % 256))  # Height (0 = 256)
        $writer.Write([byte]0)    # Color palette
        $writer.Write([byte]0)    # Reserved
        $writer.Write([uint16]1)  # Color planes
        $writer.Write([uint16]32) # Bits per pixel
        
        # Calculate size (we'll use PNG format)
        $memStream = New-Object System.IO.MemoryStream
        $bitmap.Save($memStream, [System.Drawing.Imaging.ImageFormat]::Png)
        $size = $memStream.Length
        
        $writer.Write([uint32]$size)         # Size of image data
        $writer.Write([uint32]$currentOffset) # Offset to image data
        
        $currentOffset += $size
        $memStream.Dispose()
    }
    
    # Write image data
    foreach ($bitmap in $bitmaps) {
        $memStream = New-Object System.IO.MemoryStream
        $bitmap.Save($memStream, [System.Drawing.Imaging.ImageFormat]::Png)
        $writer.Write($memStream.ToArray())
        $memStream.Dispose()
    }
    
    $writer.Close()
    $stream.Close()
}

# Save as ICO
$allBitmaps = @($icon256, $icon128, $icon64, $icon48, $icon32, $icon16)
Save-AsIco -bitmaps $allBitmaps -outputPath "C:\ReposFred\MyDataHelper\src\MyDataHelper\MyDataHelper.ico"
Save-AsIco -bitmaps $allBitmaps -outputPath "C:\ReposFred\MyDataHelper\src\MyDataHelper\wwwroot\favicon.ico"

# Clean up
foreach ($bitmap in $allBitmaps) {
    $bitmap.Dispose()
}

Write-Host "Icons created successfully!"
Write-Host "- MyDataHelper.ico created"
Write-Host "- favicon.ico created"
Write-Host "- PNG previews saved in temp folder"