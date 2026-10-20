param([string]$OutputDirectory = (Join-Path $PSScriptRoot '..\assets'))
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$sizes = @(16,20,24,32,40,48,64,128,256)
$frames = @()
foreach ($size in $sizes) {
    $bitmap = [Drawing.Bitmap]::new($size,$size)
    $graphics = [Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.ScaleTransform(($size / 256.0),($size / 256.0))
    $path=[Drawing.Drawing2D.GraphicsPath]::new()
    $path.AddArc(8,8,64,64,180,90);$path.AddArc(184,8,64,64,270,90);$path.AddArc(184,184,64,64,0,90);$path.AddArc(8,184,64,64,90,90);$path.CloseFigure()
    $brush=[Drawing.Drawing2D.LinearGradientBrush]::new([Drawing.Rectangle]::new(0,0,256,256),[Drawing.Color]::FromArgb(255,39,39,50),[Drawing.Color]::FromArgb(255,16,16,23),45)
    $graphics.FillPath($brush,$path)
    $accent=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(255,255,214,65))
    $points=[Drawing.PointF[]]@([Drawing.PointF]::new(108,65),[Drawing.PointF]::new(181,110),[Drawing.PointF]::new(108,155))
    $graphics.FillPolygon($accent,$points)
    $pen=[Drawing.Pen]::new([Drawing.Color]::FromArgb(255,255,214,65),12);$pen.StartCap='Round';$pen.EndCap='Round'
    foreach($bar in @(@(66,112,148),@(90,94,166),@(116,180,198),@(141,164,198),@(166,177,198),@(191,146,190))) {
        $graphics.DrawLine($pen,[single]$bar[0],[single]$bar[1],[single]$bar[0],[single]$bar[2])
    }
    $memory=[IO.MemoryStream]::new();$bitmap.Save($memory,[Drawing.Imaging.ImageFormat]::Png)
    $frames += ,$memory.ToArray()
    if($size -eq 256){$bitmap.Save((Join-Path $OutputDirectory 'app.png'),[Drawing.Imaging.ImageFormat]::Png)}
    $memory.Dispose();$pen.Dispose();$accent.Dispose();$brush.Dispose();$path.Dispose();$graphics.Dispose();$bitmap.Dispose()
}
$file=[IO.File]::Create((Join-Path $OutputDirectory 'app.ico'));$writer=[IO.BinaryWriter]::new($file)
try {
    $writer.Write([uint16]0);$writer.Write([uint16]1);$writer.Write([uint16]$sizes.Count)
    $offset=6+16*$sizes.Count
    for($i=0;$i -lt $sizes.Count;$i++){
        $dimension=if($sizes[$i] -eq 256){0}else{$sizes[$i]}
        $writer.Write([byte]$dimension);$writer.Write([byte]$dimension);$writer.Write([byte]0);$writer.Write([byte]0)
        $writer.Write([uint16]1);$writer.Write([uint16]32);$writer.Write([uint32]$frames[$i].Length);$writer.Write([uint32]$offset)
        $offset += $frames[$i].Length
    }
    foreach($frame in $frames){$writer.Write([byte[]]$frame)}
} finally {$writer.Dispose();$file.Dispose()}
