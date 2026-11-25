$ErrorActionPreference = "Stop"

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$RepoName = Split-Path $RepoRoot -Leaf
$Timestamp = Get-Date -Format "yyyyMMdd-HHmmss"

$ArchivePath = Join-Path (Split-Path $RepoRoot -Parent) "$RepoName-source-$Timestamp.zip"
$StagePath = Join-Path $env:TEMP "$RepoName-stage-$Timestamp"

$ExcludedDirectories = @(
    ".git", ".vs", ".idea", ".vscode",
    "bin", "obj", "node_modules", "TestResults",
    "artifacts", "dist", "coverage", "__pycache__"
)

$ExcludedExtensions = @(
    ".tmp", ".temp", ".bak", ".log", ".old",
    ".orig", ".rej", ".swp", ".zip", ".7z",
    ".rar", ".tar", ".gz", ".dump", ".backup"
)

function Test-ExcludedFile {
    param([System.IO.FileInfo]$File)

    $Relative = $File.FullName.Substring($RepoRoot.Length).TrimStart('\', '/')
    $Parts = $Relative -split '[\\/]'

    foreach ($Part in $Parts) {
        if ($ExcludedDirectories -contains $Part) {
            return $true
        }
    }

    if ($File.Name -eq ".env") { return $true }

    if ($File.Name -like ".env.*" -and $File.Name -ne ".env.example") {
        return $true
    }

    if ($ExcludedExtensions -contains $File.Extension.ToLowerInvariant()) {
        return $true
    }

    if ($File.Name -match '^appsettings\.(Development|Local)\.json$') {
        return $true
    }

    if ($File.Extension -match '^\.(pfx|p12|pem|key|keystore)$') {
        return $true
    }

    return $false
}

function Protect-Json {
    param([object]$Node)

    if ($null -eq $Node) { return $null }

    if ($Node -is [System.Management.Automation.PSCustomObject]) {
        foreach ($Property in @($Node.PSObject.Properties)) {
            $Name = $Property.Name

            if ($Name -match '(?i)connectionstrings') {
                $Property.Value = "[REDACTED]"
                continue
            }

            if ($Name -match '(?i)password|secret|api.?key|credential|private.?key|(^|_)key$|token') {
                $Property.Value = "[REDACTED]"
                continue
            }

            $Property.Value = Protect-Json -Node $Property.Value
        }

        return $Node
    }

    if ($Node -is [array]) {
        $Result = @()
        foreach ($Item in $Node) {
            $Result += Protect-Json -Node $Item
        }
        return ,$Result
    }

    return $Node
}

try {
    Write-Host "Repository: $RepoRoot"

    New-Item -ItemType Directory -Path $StagePath -Force | Out-Null

    $Included = 0
    $Excluded = 0

    Get-ChildItem $RepoRoot -Recurse -File -Force | ForEach-Object {
        $File = $_

        if (Test-ExcludedFile -File $File) {
            $Excluded++
            return
        }

        $Relative = $File.FullName.Substring($RepoRoot.Length).TrimStart('\', '/')
        $Destination = Join-Path $StagePath $Relative

        New-Item -ItemType Directory -Path (Split-Path $Destination -Parent) -Force |
            Out-Null

        if ($File.Name -eq "appsettings.json") {
            $Json = Get-Content $File.FullName -Raw | ConvertFrom-Json
            $SafeJson = Protect-Json -Node $Json

            $SafeJson |
                ConvertTo-Json -Depth 100 |
                Set-Content -Path $Destination -Encoding UTF8
        }
        else {
            Copy-Item $File.FullName $Destination -Force
        }

        $Included++
    }

    Compress-Archive -Path (Join-Path $StagePath "*") `
        -DestinationPath $ArchivePath `
        -CompressionLevel Optimal

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $Zip = [System.IO.Compression.ZipFile]::OpenRead($ArchivePath)

    try {
        $Names = @($Zip.Entries | ForEach-Object {
            $_.FullName.Replace('\', '/')
        })

        $Forbidden = @($Names | Where-Object {
            $_ -match '(^|/)(\.git|\.vs|bin|obj|node_modules|TestResults)/' -or
            ($_ -match '(^|/)\.env($|\.)' -and $_ -notmatch '(^|/)\.env\.example$') -or
            $_ -match '\.(tmp|temp|bak|log|old|orig|rej|swp|dump|backup)$' -or
            $_ -match '(^|/)appsettings\.(Development|Local)\.json$' -or
            $_ -match '\.(pfx|p12|pem|key|keystore)$'
        })

        if ($Forbidden.Count -gt 0) {
            throw "Archive contains forbidden entries: $($Forbidden -join ', ')"
        }

        Write-Host "`n========== ARCHIVE SUCCESS ==========" -ForegroundColor Green
        Write-Host "Archive: $ArchivePath"
        Write-Host "Included files: $Included"
        Write-Host "Excluded files: $Excluded"
        Write-Host "Archive entries: $($Names.Count)"
        Write-Host "Archive size: $((Get-Item $ArchivePath).Length) bytes"
        Write-Host "Forbidden entries: 0"
    }
    finally {
        $Zip.Dispose()
    }
}
catch {
    if (Test-Path $ArchivePath) {
        Remove-Item $ArchivePath -Force
    }
    throw
}
finally {
    if (Test-Path $StagePath) {
        Remove-Item $StagePath -Recurse -Force
    }
}
