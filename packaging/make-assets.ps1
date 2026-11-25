# Generates Notify's logo images (MSIX/Store assets) and the app icon from one design:
# a Windows-blue gradient tile with white Wi-Fi arcs.
# Run from the repo root with Windows PowerShell 5.1:  powershell -File packaging\make-assets.ps1
# Output: packaging\Assets\*.png and Notify\Resources\Notify.ico

Add-Type -AssemblyName System.Drawing
$ErrorActionPreference = 'Stop'
$root   = Split-Path $PSScriptRoot -Parent
$assets = Join-Path $PSScriptRoot 'Assets'
New-Item -ItemType Directory -Force $assets | Out-Null

function New-Logo([int]$w, [int]$h, [bool]$plated) {
    $bmp = New-Object System.Drawing.Bitmap $w, $h
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    $g.PixelOffsetMode = 'HighQuality'
    $g.Clear([System.Drawing.Color]::Transparent)
    $s = [Math]::Min($w, $h)                      # glyph is drawn in a centred square
    $ox = ($w - $s) / 2.0; $oy = ($h - $s) / 2.0

    if ($plated) {
        # Rounded tile with a diagonal blue gradient (Windows 11 accent blues)
        $r = $s * 0.22
        $path = New-Object System.Drawing.Drawing2D.GraphicsPath
        $rect = New-Object System.Drawing.RectangleF ([float]$ox), ([float]$oy), ([float]$s), ([float]$s)
        $d = 2 * $r
        $path.AddArc($rect.X, $rect.Y, $d, $d, 180, 90)
        $path.AddArc($rect.Right - $d, $rect.Y, $d, $d, 270, 90)
        $path.AddArc($rect.Right - $d, $rect.Bottom - $d, $d, $d, 0, 90)
        $path.AddArc($rect.X, $rect.Bottom - $d, $d, $d, 90, 90)
        $path.CloseFigure()
        $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush $rect, ([System.Drawing.Color]::FromArgb(255, 0, 95, 184)), ([System.Drawing.Color]::FromArgb(255, 76, 194, 255)), 45
        $g.FillPath($brush, $path)
        $brush.Dispose(); $path.Dispose()
        $fg = [System.Drawing.Color]::White
    } else {
        $fg = [System.Drawing.Color]::FromArgb(255, 0, 120, 212)
    }

    # Wi-Fi glyph: three arcs and a dot, centred low in the tile
    $cx = $ox + $s / 2.0
    $cy = $oy + $s * 0.70
    $pen = New-Object System.Drawing.Pen $fg, ([float]([Math]::Max(1.2, $s * 0.075)))
    $pen.StartCap = 'Round'; $pen.EndCap = 'Round'
    foreach ($k in 0.40, 0.28, 0.16) {
        $rad = $s * $k
        $g.DrawArc($pen, [float]($cx - $rad), [float]($cy - $rad), [float](2 * $rad), [float](2 * $rad), 225, 90)
    }
    $dot = [Math]::Max(1.5, $s * 0.085)
    $b = New-Object System.Drawing.SolidBrush $fg
    $g.FillEllipse($b, [float]($cx - $dot), [float]($cy - $dot), [float](2 * $dot), [float](2 * $dot))
    $b.Dispose(); $pen.Dispose(); $g.Dispose()
    return $bmp
}

function Save-Png($bmp, $name) { $bmp.Save((Join-Path $assets $name), [System.Drawing.Imaging.ImageFormat]::Png); $bmp.Dispose() }

# MSIX visual assets (unqualified names = scale 100, matching Package.appxmanifest)
Save-Png (New-Logo 44 44 $true)   'Square44x44Logo.png'
Save-Png (New-Logo 150 150 $true) 'Square150x150Logo.png'
Save-Png (New-Logo 310 150 $true) 'Wide310x150Logo.png'
Save-Png (New-Logo 50 50 $true)   'StoreLogo.png'
# Large logo for the Store listing / screenshots (upload manually in Partner Center)
Save-Png (New-Logo 300 300 $true) 'StoreListing300.png'

# App icon (.ico with PNG frames at the sizes Windows asks for)
$sizes = 16, 20, 24, 32, 40, 48, 64, 256
$frames = foreach ($sz in $sizes) {
    $bmp = New-Logo $sz $sz $true
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png); $bmp.Dispose()
    ,$ms.ToArray()
}
$ico = Join-Path $root 'Notify\Resources\Notify.ico'
$fs = [System.IO.File]::Create($ico)
$bw = New-Object System.IO.BinaryWriter $fs
$bw.Write([UInt16]0); $bw.Write([UInt16]1); $bw.Write([UInt16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $sz = $sizes[$i]; $len = $frames[$i].Length
    $bw.Write([byte]($(if ($sz -ge 256) { 0 } else { $sz }))); $bw.Write([byte]($(if ($sz -ge 256) { 0 } else { $sz })))
    $bw.Write([byte]0); $bw.Write([byte]0); $bw.Write([UInt16]1); $bw.Write([UInt16]32)
    $bw.Write([UInt32]$len); $bw.Write([UInt32]$offset); $offset += $len
}
foreach ($f in $frames) { $bw.Write($f) }
$bw.Close()
Write-Host "Assets written to $assets and $ico"
