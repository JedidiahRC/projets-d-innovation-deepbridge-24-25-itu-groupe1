@echo off
chcp 65001 >nul
echo.
echo ================================================================================
echo    🚀 DEEPBRIDGE - PROJET COMPLET AVEC DÉTECTION DE STÉNOSE
echo ================================================================================
echo.
echo Ce script va démarrer:
echo   1. L'API Flask de détection de sténose (Python)
echo   2. L'application DeepBridge (C#)
echo.
echo ================================================================================
echo.

REM Vérifier Python
python --version >nul 2>&1
if errorlevel 1 (
    echo ❌ [ERREUR] Python n'est pas installé
    echo.
    echo 💡 Installez Python 3.11+ depuis: https://www.python.org/
    pause
    exit /b 1
)

echo ✅ Python détecté
echo.

REM Vérifier le modèle U-Net
if not exist "StenoseDetection_U-Net\carotide_detector_v2.h5" (
    echo ❌ [ERREUR] Modèle U-Net non trouvé
    echo.
    echo 💡 Le fichier carotide_detector_v2.h5 doit être dans:
    echo    StenoseDetection_U-Net\
    pause
    exit /b 1
)

echo ✅ Modèle U-Net trouvé
echo.

REM Vérifier les dépendances Python
echo 🔍 Vérification des dépendances Python...
pip show flask >nul 2>&1
if errorlevel 1 (
    echo.
    echo 📦 Installation des dépendances Python...
    cd StenoseDetection_U-Net
    pip install -r requirements_api.txt
    cd ..
    echo.
)

echo ✅ Dépendances Python OK
echo.

echo ================================================================================
echo    📡 DÉMARRAGE DE L'API FLASK
echo ================================================================================
echo.
echo L'API sera accessible sur: http://localhost:5000
echo.

REM Démarrer l'API Flask dans une nouvelle fenêtre
start "API Flask - Détection Sténose" /MIN cmd /k "cd StenoseDetection_U-Net && python flask_api.py"

echo ✅ API Flask démarrée (fenêtre séparée)
echo.

REM Attendre que l'API soit prête
echo ⏳ Attente du démarrage de l'API (5 secondes)...
timeout /t 5 /nobreak >nul

echo.
echo 🧪 Test de l'API...
curl -s http://localhost:5000/api/health >nul 2>&1
if errorlevel 1 (
    echo.
    echo ⚠️  L'API n'est pas encore prête
    echo    Elle devrait être accessible dans quelques secondes
) else (
    echo ✅ API opérationnelle !
)

echo.
echo ================================================================================
echo    💻 DÉMARRAGE DE L'APPLICATION C#
echo ================================================================================
echo.

REM Vérifier si le projet est compilé
if not exist "bin\Debug\net8.0-windows\DeepBridgeWindowsAppCore.exe" (
    if not exist "bin\Release\net8.0-windows\DeepBridgeWindowsAppCore.exe" (
        echo ⚠️  L'application n'est pas compilée
        echo.
        echo 💡 Compilez d'abord le projet dans Visual Studio:
        echo    1. Ouvrir DeepBridgeWindowsAppCore.sln
        echo    2. Build ^> Build Solution
        echo    3. Relancer ce script
        echo.
        pause
        exit /b 1
    )
)

REM Démarrer l'application C#
if exist "bin\Debug\net8.0-windows\DeepBridgeWindowsAppCore.exe" (
    echo 🚀 Lancement de l'application (Debug)...
    start "" "bin\Debug\net8.0-windows\DeepBridgeWindowsAppCore.exe"
) else (
    echo 🚀 Lancement de l'application (Release)...
    start "" "bin\Release\net8.0-windows\DeepBridgeWindowsAppCore.exe"
)

echo.
echo ================================================================================
echo    ✅ SYSTÈME DÉMARRÉ
echo ================================================================================
echo.
echo 🎯 L'application DeepBridge est lancée
echo 📡 L'API Flask tourne en arrière-plan
echo.
echo UTILISATION:
echo   1. Ouvrir une série DICOM dans l'application
echo   2. Localiser le cou avec le bouton approprié
echo   3. Cliquer sur "Détecter Sténose"
echo   4. Les résultats s'afficheront automatiquement
echo.
echo ARRÊT:
echo   - Fermer l'application C#
echo   - Fermer la fenêtre de l'API Flask (ou Ctrl+C)
echo.
echo 📚 Documentation:
echo   - DEMARRAGE_RAPIDE.md
echo   - INTEGRATION_GUIDE.md
echo   - ANALYSE_FAISABILITE.md
echo.
echo ================================================================================
echo.
pause
