$ErrorActionPreference = "Stop"
try {
  [Console]::OutputEncoding = [System.Text.Encoding]::UTF8
  $OutputEncoding = [System.Text.Encoding]::UTF8
} catch {}

$installer = Join-Path $env:TEMP "MicrosoftEdgeWebview2Setup.exe"
$url = "https://go.microsoft.com/fwlink/p/?LinkId=2124703"

function Invoke-WebRequestCompat {
  param([string]$Uri, [string]$OutFile)
  if ($PSVersionTable.PSVersion.Major -lt 6) {
    Invoke-WebRequest -Uri $Uri -OutFile $OutFile -UseBasicParsing
  } else {
    Invoke-WebRequest -Uri $Uri -OutFile $OutFile
  }
}

Write-Host "Downloading Microsoft Edge WebView2 Evergreen Runtime bootstrapper..."
Invoke-WebRequestCompat -Uri $url -OutFile $installer

Write-Host "Running installer. A UAC prompt can appear depending on your system."
$process = Start-Process -FilePath $installer -ArgumentList "/silent", "/install" -Wait -PassThru
if ($process.ExitCode -ne 0) {
  throw "WebView2 installer exited with code $($process.ExitCode)."
}

Write-Host "WebView2 Runtime install/repair completed."
