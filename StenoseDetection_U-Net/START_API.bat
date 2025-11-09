@echo off
echo ========================================
echo  API STENOSE - DEMARRAGE
echo ========================================
echo.

REM Verifier si Python est installe
python --version >nul 2>&1
if errorlevel 1 (
    echo [ERREUR] Python n'est pas installe ou n'est pas dans le PATH
    echo Installez Python 3.11+ depuis https://www.python.org/
    pause
    exit /b 1
)

echo [OK] Python detecte
echo.

REM Verifier si le modele existe
if not exist "carotide_detector_v2.h5" (
    echo [ERREUR] Le modele carotide_detector_v2.h5 n'existe pas!
    echo Assurez-vous qu'il est dans le dossier StenoseDetection_U-Net/
    pause
    exit /b 1
)

echo [OK] Modele U-Net trouve
echo.

REM Verifier si les dependances sont installees
echo Verification des dependances...
pip show flask >nul 2>&1
if errorlevel 1 (
    echo [INFO] Installation des dependances...
    pip install -r requirements_api.txt
)

echo [OK] Dependances installees
echo.

echo ========================================
echo  DEMARRAGE DE L'API
echo ========================================
echo.
echo L'API sera accessible sur: http://localhost:5000
echo.
echo Endpoints disponibles:
echo   - GET  /api/health           : Verification de l'etat
echo   - POST /api/detect-stenosis  : Detection complete
echo   - POST /api/process-single   : Traitement image unique
echo.
echo Appuyez sur Ctrl+C pour arreter l'API
echo ========================================
echo.

python flask_api.py

pause
