<#
.SYNOPSIS
    Unified CRManager Android publish pipeline.
    Handles keystore creation or selection, dotnet publish, APK fix, align, and sign.

.DESCRIPTION
    Run from the project root (the folder containing Publish-Android.ps1).
    All values are prompted interactively — nothing is hardcoded to a specific machine.

.EXAMPLE
    .\Publish-Android.ps1
#>

$ErrorActionPreference = "Continue"
$ProjectRoot = $PSScriptRoot

# ─── HELPERS ─────────────────────────────────────────────────────────────────
function Write-Step($n, $total, $msg) {
    Write-Host ""
    Write-Host "[$n/$total] $msg" -ForegroundColor Cyan
    Write-Host ("─" * 50) -ForegroundColor DarkGray
}

function Read-Password($prompt) {
    $ss = Read-Host $prompt -AsSecureString
    [Runtime.InteropServices.Marshal]::PtrToStringAuto(
        [Runtime.InteropServices.Marshal]::SecureStringToBSTR($ss))
}

function Abort($msg) {
    Write-Host ""
    Write-Host "ABORTED: $msg" -ForegroundColor Red
    pause
    exit 1
}

# ─── BANNER ──────────────────────────────────────────────────────────────────
Clear-Host
Write-Host ""
Write-Host "╔══════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   CRManager — Android Publish Tool   ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

$totalSteps = 5

# ═══════════════════════════════════════════════════════════════════════════════
# PREREQUISITES CHECK
# ═══════════════════════════════════════════════════════════════════════════════
Write-Host "Checking prerequisites..." -ForegroundColor DarkGray
Write-Host ""

$prereqFailed = $false

function Check-Prereq($label, $ok, $foundMsg, $fixMsg) {
    if ($ok) {
        Write-Host "  [OK] $label" -ForegroundColor Green
        if ($foundMsg) { Write-Host "       $foundMsg" -ForegroundColor DarkGray }
    } else {
        Write-Host "  [MISSING] $label" -ForegroundColor Red
        Write-Host "       Fix: $fixMsg" -ForegroundColor Yellow
        $script:prereqFailed = $true
    }
}

# dotnet
$dotnetVer = (Get-Command dotnet -ErrorAction SilentlyContinue)?.Source
Check-Prereq "dotnet SDK" `
    ($null -ne $dotnetVer) `
    "$(if($dotnetVer){ "dotnet $(dotnet --version 2>$null)" })" `
    "winget install Microsoft.DotNet.SDK.8"

# Android workload
$androidWorkload = dotnet workload list 2>$null | Select-String "maui-android"
Check-Prereq "dotnet maui-android workload" `
    ($null -ne $androidWorkload) `
    "$($androidWorkload -replace '\s+', ' ')" `
    "dotnet workload install maui-android"

# Java / keytool
$javaExe     = (Get-Command java     -ErrorAction SilentlyContinue)?.Source
$keytoolExe  = (Get-Command keytool  -ErrorAction SilentlyContinue)?.Source
if (-not $keytoolExe -and $javaExe) {
    $candidate = Join-Path ([System.IO.Path]::GetDirectoryName($javaExe)) "keytool.exe"
    if (Test-Path $candidate) { $keytoolExe = $candidate }
}
if (-not $keytoolExe) {
    foreach ($jDir in @("C:\Program Files\Java","C:\Program Files\Android\openjdk","C:\Program Files\Microsoft","C:\Program Files (x86)\Java","$env:LocalAppData\Android\Android Studio\jbr",$env:JAVA_HOME) | Where-Object { $_ -and (Test-Path $_) }) {
        $found = Get-ChildItem $jDir -Recurse -Filter "keytool.exe" -File -EA SilentlyContinue | Select-Object -First 1
        if ($found) { $keytoolExe = $found.FullName; break }
    }
}
Check-Prereq "Java JDK (keytool)" `
    ($null -ne $keytoolExe) `
    "$(if($keytoolExe){ $keytoolExe })" `
    "winget install Microsoft.OpenJDK.21"

# Python
$pythonExe = (Get-Command python -ErrorAction SilentlyContinue)?.Source
Check-Prereq "Python" `
    ($null -ne $pythonExe) `
    "$(if($pythonExe){ "python $(python --version 2>&1)" })" `
    "winget install Python.Python.3.12"

# Android SDK (zipalign + apksigner)
$sdkRoots = @($env:ANDROID_HOME, $env:ANDROID_SDK_ROOT, "C:\Program Files (x86)\Android\android-sdk", "$env:LocalAppData\Android\Sdk") |
            Where-Object { $_ -and (Test-Path $_) }
$zipalignExe  = $null
$apksignerExe = $null
foreach ($sdk in $sdkRoots) {
    if (-not $zipalignExe)  { $zipalignExe  = (Get-ChildItem "$sdk\build-tools" -Recurse -Filter "zipalign.exe"  -File -EA SilentlyContinue | Sort-Object FullName -Descending | Select-Object -First 1)?.FullName }
    if (-not $apksignerExe) { $apksignerExe = (Get-ChildItem "$sdk\build-tools" -Recurse -Filter "apksigner.bat" -File -EA SilentlyContinue | Sort-Object FullName -Descending | Select-Object -First 1)?.FullName }
}
Check-Prereq "Android SDK (zipalign)" `
    ($null -ne $zipalignExe) `
    "$(if($zipalignExe){ $zipalignExe })" `
    "Install Android SDK Build-Tools via Android Studio > SDK Manager > SDK Tools"
Check-Prereq "Android SDK (apksigner)" `
    ($null -ne $apksignerExe) `
    "$(if($apksignerExe){ $apksignerExe })" `
    "Install Android SDK Build-Tools via Android Studio > SDK Manager > SDK Tools"

Write-Host ""

if ($prereqFailed) {
    Write-Host "One or more prerequisites are missing. Install them and re-run the script." -ForegroundColor Red
    pause
    exit 1
}

Write-Host "All prerequisites satisfied. Starting build..." -ForegroundColor Green
Write-Host ""

# ═══════════════════════════════════════════════════════════════════════════════
# STEP 1 — KEYSTORE
# ═══════════════════════════════════════════════════════════════════════════════
Write-Step 1 $totalSteps "Keystore Setup"

$keystoreChoice = ""
while ($keystoreChoice -notin @("1","2")) {
    Write-Host "  [1] Use an existing keystore"
    Write-Host "  [2] Create a new keystore"
    $keystoreChoice = Read-Host "Choice"
}

$KeystorePath = ""
$KeyAlias     = ""
$StorePass    = ""
$KeyPass      = ""

if ($keystoreChoice -eq "2") {
    # ── CREATE NEW KEYSTORE ──
    Write-Host ""

    $defaultKsPath = Join-Path $env:USERPROFILE "release.keystore"
    $inputPath = Read-Host "Keystore save path (Enter for: $defaultKsPath)"
    if ([string]::IsNullOrWhiteSpace($inputPath)) { $inputPath = $defaultKsPath }
    $KeystorePath = $inputPath

    $inputAlias = Read-Host "Key alias (e.g. release, myapp)"
    if ([string]::IsNullOrWhiteSpace($inputAlias)) { $inputAlias = "release" }
    $KeyAlias = $inputAlias

    $StorePass = Read-Password "Store password"
    $useSame = Read-Host "Use same password for key? (Y/n)"
    if ($useSame -eq "" -or $useSame -match "^[Yy]") {
        $KeyPass = $StorePass
    } else {
        $KeyPass = Read-Password "Key password"
    }

    Write-Host ""
    Write-Host "Generating keystore..." -ForegroundColor Yellow
    Write-Host "(You will be asked to fill in your name and organisation details)" -ForegroundColor DarkGray
    Write-Host ""

    # Auto-detect keytool — it ships with the JDK but is not always in PATH
    $keytoolExe = (Get-Command keytool -ErrorAction SilentlyContinue)?.Source

    # If keytool isn't in PATH but java is, keytool.exe lives in the same bin folder
    if (-not $keytoolExe) {
        $javaExe = (Get-Command java -ErrorAction SilentlyContinue)?.Source
        if ($javaExe) {
            $candidate = Join-Path ([System.IO.Path]::GetDirectoryName($javaExe)) "keytool.exe"
            if (Test-Path $candidate) { $keytoolExe = $candidate }
        }
    }

    # Fallback: scan common JDK install locations
    if (-not $keytoolExe) {
        $jdkSearchRoots = @(
            "C:\Program Files\Java",
            "C:\Program Files\Android\openjdk",
            "C:\Program Files\Microsoft",
            "C:\Program Files (x86)\Java",
            "$env:LocalAppData\Android\Android Studio\jbr",
            $env:JAVA_HOME
        ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) -and (Test-Path $_) }

        foreach ($jDir in $jdkSearchRoots) {
            $found = Get-ChildItem $jDir -Recurse -Filter "keytool.exe" -File -ErrorAction SilentlyContinue | Select-Object -First 1
            if ($found) { $keytoolExe = $found.FullName; break }
        }
    }

    if (-not $keytoolExe) { Abort "keytool.exe not found. Run: winget install Microsoft.OpenJDK.21 -- then restart your terminal." }
    Write-Host "  keytool: $keytoolExe" -ForegroundColor DarkGray

    # No -dname: keytool prompts the user interactively for name/org/country
    $keytoolArgs = @(
        "-genkeypair", "-v",
        "-keystore", $KeystorePath,
        "-alias", $KeyAlias,
        "-keyalg", "RSA",
        "-keysize", "2048",
        "-validity", "10000",
        "-storepass", $StorePass,
        "-keypass", $KeyPass
    )
    & $keytoolExe @keytoolArgs
    if ($LASTEXITCODE -ne 0) { Abort "keytool failed." }
    Write-Host "Keystore created: $KeystorePath" -ForegroundColor Green

} else {
    # ── USE EXISTING KEYSTORE ──

    # Auto-discover all *.keystore files in common locations
    $searchDirs = @(
        $env:USERPROFILE,
        (Join-Path $env:USERPROFILE "Desktop"),
        (Join-Path $env:USERPROFILE "Documents"),
        (Join-Path $env:USERPROFILE ".android"),
        (Join-Path $env:LocalAppData "Xamarin\Mono for Android")
    ) | Where-Object { Test-Path $_ }

    $candidates = $searchDirs | ForEach-Object {
        Get-ChildItem -Path $_ -Filter "*.keystore" -File -ErrorAction SilentlyContinue
    } | Select-Object -ExpandProperty FullName -Unique

    if ($candidates.Count -gt 0) {
        Write-Host ""
        Write-Host "Found keystore(s):" -ForegroundColor Cyan
        for ($i = 0; $i -lt $candidates.Count; $i++) {
            Write-Host "  [$i] $($candidates[$i])"
        }
        Write-Host "  [c] Enter custom path"
        $sel = Read-Host "Select"
        if ($sel -match "^\d+$" -and [int]$sel -lt $candidates.Count) {
            $KeystorePath = $candidates[[int]$sel]
        } else {
            $KeystorePath = Read-Host "Keystore full path"
        }
    } else {
        $KeystorePath = Read-Host "No keystores found automatically. Enter full path"
    }

    if (-not (Test-Path $KeystorePath)) { Abort "Keystore not found: $KeystorePath" }

    $inputAlias = Read-Host "Key alias (e.g. release, myapp)"
    if ([string]::IsNullOrWhiteSpace($inputAlias)) { $inputAlias = "release" }
    $KeyAlias = $inputAlias

    $StorePass = Read-Password "Store password"
    $useSame = Read-Host "Key password same as store? (Y/n)"
    if ($useSame -eq "" -or $useSame -match "^[Yy]") {
        $KeyPass = $StorePass
    } else {
        $KeyPass = Read-Password "Key password"
    }
}

Write-Host ""
Write-Host "  Keystore : $KeystorePath" -ForegroundColor DarkGray
Write-Host "  Alias    : $KeyAlias"     -ForegroundColor DarkGray

# ═══════════════════════════════════════════════════════════════════════════════
# STEP 2 — DOTNET PUBLISH
# ═══════════════════════════════════════════════════════════════════════════════
Write-Step 2 $totalSteps "dotnet publish (Release Android)"

$csproj = Join-Path $ProjectRoot "src\CRManager.Client.Maui\CRManager.Client.Maui.csproj"
if (-not (Test-Path $csproj)) { Abort "Project not found: $csproj`nMake sure you are running this script from the project root folder." }

$publishArgs = @(
    "publish", $csproj,
    "-f", "net8.0-android",
    "-c", "Release",
    "/p:AndroidPackageFormat=apk",
    "/p:AndroidKeyStore=true",
    "/p:AndroidSigningKeyStore=$KeystorePath",
    "/p:AndroidSigningKeyAlias=$KeyAlias",
    "/p:AndroidSigningKeyPass=$KeyPass",
    "/p:AndroidSigningStorePass=$StorePass"
)

Write-Host ""
& dotnet @publishArgs
if ($LASTEXITCODE -ne 0) { Abort "dotnet publish failed." }

$publishDir = Join-Path $ProjectRoot "src\CRManager.Client.Maui\bin\Release\net8.0-android\publish"

# Grab the newest APK produced (handles any package ID)
$signedApk = Get-ChildItem -Path $publishDir -Filter "*-Signed.apk" -File -ErrorAction SilentlyContinue |
             Sort-Object LastWriteTime -Descending |
             Select-Object -First 1 -ExpandProperty FullName

if (-not $signedApk) {
    # Fallback: any APK
    $signedApk = Get-ChildItem -Path $publishDir -Filter "*.apk" -File |
                 Sort-Object LastWriteTime -Descending |
                 Select-Object -First 1 -ExpandProperty FullName
}

if (-not $signedApk -or -not (Test-Path $signedApk)) { Abort "Could not find output APK in $publishDir" }
Write-Host ""
Write-Host "Published APK: $signedApk" -ForegroundColor Green

# ═══════════════════════════════════════════════════════════════════════════════
# STEP 3 — DETECT ANDROID SDK TOOLS
# ═══════════════════════════════════════════════════════════════════════════════
Write-Step 3 $totalSteps "Locating Android SDK Tools (zipalign / apksigner)"

$possibleSdkRoots = @(
    $env:ANDROID_HOME,
    $env:ANDROID_SDK_ROOT,
    "C:\Program Files (x86)\Android\android-sdk",
    (Join-Path $env:LocalAppData "Android\Sdk")
) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) -and (Test-Path $_) }

$zipalign  = $null
$apksigner = $null

foreach ($sdk in $possibleSdkRoots) {
    if (-not $zipalign) {
        $z = Get-ChildItem "$sdk\build-tools" -Recurse -Filter "zipalign.exe" -File -ErrorAction SilentlyContinue |
             Sort-Object FullName -Descending | Select-Object -First 1
        if ($z) { $zipalign = $z.FullName }
    }
    if (-not $apksigner) {
        $a = Get-ChildItem "$sdk\build-tools" -Recurse -Filter "apksigner.bat" -File -ErrorAction SilentlyContinue |
             Sort-Object FullName -Descending | Select-Object -First 1
        if ($a) { $apksigner = $a.FullName }
    }
}

# Java auto-detect
$javaSearchRoots = @(
    "C:\Program Files\Android\openjdk",
    "C:\Program Files\Java",
    "C:\Program Files\Microsoft",
    $env:JAVA_HOME
) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) -and (Test-Path $_) }

foreach ($jDir in $javaSearchRoots) {
    $jExec = Get-ChildItem $jDir -Recurse -Filter "java.exe" -File -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($jExec) {
        $bin = [System.IO.Path]::GetDirectoryName($jExec.FullName)
        $env:JAVA_HOME = [System.IO.Path]::GetDirectoryName($bin)
        $env:Path = "$bin;$env:Path"
        break
    }
}

if (-not $zipalign) { Abort "zipalign.exe not found. Install Android SDK Build-Tools via Android Studio or Visual Studio." }

Write-Host "  zipalign : $zipalign"  -ForegroundColor DarkGray
Write-Host "  apksigner: $apksigner" -ForegroundColor DarkGray

# ═══════════════════════════════════════════════════════════════════════════════
# STEP 4 — REPACK + ZIPALIGN
# ═══════════════════════════════════════════════════════════════════════════════
Write-Step 4 $totalSteps "Repack (uncompressed resources) + ZipAlign"

$uncompressed = Join-Path ([System.IO.Path]::GetTempPath()) "maui_uncompressed.apk"

# Derive output name from the signed APK: strip "-Signed" suffix, add "-Release"
$baseName  = [System.IO.Path]::GetFileNameWithoutExtension($signedApk) -replace "-Signed$", ""
$fixedApk  = Join-Path $publishDir "$baseName-Release.apk"

if (Test-Path $uncompressed) { Remove-Item $uncompressed -Force }
if (Test-Path $fixedApk)     { Remove-Item $fixedApk     -Force }

Write-Host "Repacking resources.arsc & native libs as STORED..." -ForegroundColor Yellow
$pyScript = @"
import zipfile
src = r'$signedApk'
dst = r'$uncompressed'
with zipfile.ZipFile(src, 'r') as zin:
    with zipfile.ZipFile(dst, 'w') as zout:
        for item in zin.infolist():
            data = zin.read(item.filename)
            if item.filename == 'resources.arsc' or item.filename.startswith('lib/') or item.filename.startswith('assemblies/'):
                zout.writestr(item.filename, data, compress_type=0)
            else:
                zout.writestr(item.filename, data, compress_type=item.compress_type)
"@

python -c "$pyScript"
if ($LASTEXITCODE -ne 0) { Abort "Python repack failed. Is Python installed?" }

Write-Host "ZipAligning to 4-byte boundaries..." -ForegroundColor Yellow
$align = Start-Process -FilePath $zipalign -ArgumentList "-f -v 4 `"$uncompressed`" `"$fixedApk`"" -NoNewWindow -PassThru -Wait
if ($align.ExitCode -ne 0) { Abort "ZipAlign failed (exit $($align.ExitCode))." }

# ═══════════════════════════════════════════════════════════════════════════════
# STEP 5 — RE-SIGN ALIGNED APK
# ═══════════════════════════════════════════════════════════════════════════════
Write-Step 5 $totalSteps "Sign Final APK"

if ($apksigner -and (Test-Path $KeystorePath)) {
    Write-Host "Signing with production keystore..." -ForegroundColor Yellow
    $signArgs = "sign --ks `"$KeystorePath`" --ks-key-alias `"$KeyAlias`" --ks-pass pass:$StorePass --key-pass pass:$KeyPass `"$fixedApk`""
    $sign = Start-Process -FilePath $apksigner -ArgumentList $signArgs -NoNewWindow -PassThru -Wait
    if ($sign.ExitCode -ne 0) { Abort "apksigner failed (exit $($sign.ExitCode))." }
} else {
    Write-Host "WARNING: apksigner not found — APK is aligned but unsigned." -ForegroundColor Yellow
}

# ─── DONE ────────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "╔══════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║           BUILD COMPLETE ✓            ║" -ForegroundColor Green
Write-Host "╚══════════════════════════════════════╝" -ForegroundColor Green
Write-Host ""
Write-Host "Final APK:" -ForegroundColor White
Write-Host "  $fixedApk" -ForegroundColor Cyan
Write-Host ""
Write-Host "Transfer this file to your Android device and install it." -ForegroundColor DarkGray
Write-Host ""
pause
