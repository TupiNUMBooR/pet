$compiler="C:\Program Files\AutoHotkey\v2\AutoHotkey.exe"

Write-Host "Building..."

& $compiler `
/in pet.ahk `
/out pet.exe `
/icon assets\pet.ico

if ($LASTEXITCODE -eq 0) {
    Write-Host "Running..."
    ./pet.exe
}
