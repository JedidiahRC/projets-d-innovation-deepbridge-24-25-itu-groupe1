# Script PowerShell pour démarrer l'API Flask de détection de sténose
# Utilisation: .\START_API.ps1

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  API STENOSE - DEMARRAGE" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Vérifier Python
Write-Host "[1/4] Verification de Python..." -ForegroundColor Yellow
try {
    $pythonVersion = python --version 2>&1
    Write-Host "  ✓ $pythonVersion" -ForegroundColor Green
} catch {
    Write-Host "  ✗ Python n'est pas installe!" -ForegroundColor Red
    Write-Host "  Installez Python 3.11+ depuis https://www.python.org/" -ForegroundColor Red
    pause
    exit 1
}

# Vérifier le modèle
Write-Host ""
Write-Host "[2/4] Verification du modele U-Net..." -ForegroundColor Yellow
if (Test-Path "carotide_detector_v2.h5") {
    $fileSize = (Get-Item "carotide_detector_v2.h5").Length / 1MB
    Write-Host ("  ✓ Modele trouve ({0:N2} MB)" -f $fileSize) -ForegroundColor Green
} else {
    Write-Host "  ✗ Le modele carotide_detector_v2.h5 n'existe pas!" -ForegroundColor Red
    Write-Host "  Placez le modele dans le dossier StenoseDetection_U-Net/" -ForegroundColor Red
    pause
    exit 1
}

# Vérifier les dépendances
Write-Host ""
Write-Host "[3/4] Verification des dependances..." -ForegroundColor Yellow
$flaskInstalled = pip show flask 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "  Installation des dependances..." -ForegroundColor Yellow
    pip install -r requirements_api.txt
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  ✓ Dependances installees" -ForegroundColor Green
    } else {
        Write-Host "  ✗ Erreur installation dependances" -ForegroundColor Red
        pause
        exit 1
    }
} else {
    Write-Host "  ✓ Dependances OK" -ForegroundColor Green
}

# Démarrer l'API
Write-Host ""
Write-Host "[4/4] Demarrage de l'API Flask..." -ForegroundColor Yellow
Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  API PRETE" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "URL: " -NoNewline
Write-Host "http://localhost:5000" -ForegroundColor Cyan
Write-Host ""
Write-Host "Endpoints disponibles:" -ForegroundColor Yellow
Write-Host "  - GET  /api/health           : Verification de l'etat"
Write-Host "  - POST /api/detect-stenosis  : Detection complete"
Write-Host "  - POST /api/process-single   : Traitement image unique"
Write-Host ""
Write-Host "Appuyez sur Ctrl+C pour arreter l'API" -ForegroundColor Red
Write-Host "========================================" -ForegroundColor Green
Write-Host ""

# Lancer Flask
python flask_api.py
