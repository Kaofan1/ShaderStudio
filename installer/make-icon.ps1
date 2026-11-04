Add-Type -AssemblyName System.Drawing
$ErrorActionPreference = 'Stop'

$outDir = Join-Path $PSScriptRoot '..\src\ShaderStudio.App\Assets'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$icoPath = Join-Path $outDir 'app.ico'

$size = 64
$bmp = New-Object System.Drawing.Bitmap($size, $size)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit

$bg = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 11, 14, 19))
$g.FillRectangle($bg, 0, 0, $size, $size)

# Cyan frame
$pen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 10, 194, 255), 4)
$g.DrawRectangle($pen, 5, 5, $size - 11, $size - 11)

# Concentric "shader" rings
$penThin = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(150, 10, 194, 255), 2)
$g.DrawEllipse($penThin, 18, 18, 28, 28)

# Letter F/X mark
$font = New-Object System.Drawing.Font('Segoe UI', 22, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$brush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 232, 238, 247))
$g.DrawString('FX', $font, $brush, 11, 19)

$g.Dispose()

$handle = $bmp.GetHicon()
$icon = [System.Drawing.Icon]::FromHandle($handle)
$stream = [System.IO.File]::Create($icoPath)
$icon.Save($stream)
$stream.Close()
$icon.Dispose()
$bmp.Dispose()

Write-Output ("ICON OK: " + (Resolve-Path $icoPath) + " (" + (Get-Item $icoPath).Length + " B)")
