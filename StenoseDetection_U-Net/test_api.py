"""
Script de test pour l'API Flask de détection de sténose
Teste les différents endpoints et valide les réponses
"""

import requests
import base64
import os
from glob import glob
import json

API_URL = "http://localhost:5000"

def test_health():
    """Test de l'endpoint /api/health"""
    print("🧪 Test 1: Health Check")
    try:
        response = requests.get(f"{API_URL}/api/health")
        if response.status_code == 200:
            data = response.json()
            print(f"✅ API accessible")
            print(f"   Status: {data.get('status')}")
            print(f"   Model loaded: {data.get('model_loaded')}")
            print(f"   Version: {data.get('version')}")
            return True
        else:
            print(f"❌ Erreur: {response.status_code}")
            return False
    except Exception as e:
        print(f"❌ Erreur de connexion: {e}")
        print("   💡 Assurez-vous que l'API est démarrée: python flask_api.py")
        return False

def test_detect_stenosis():
    """Test de l'endpoint /api/detect-stenosis avec images du dossier input"""
    print("\n🧪 Test 2: Détection de Sténose")
    
    # Charger quelques images de test
    image_paths = glob("input/*.png")[:5]  # Prendre 5 images
    
    if not image_paths:
        print("❌ Aucune image trouvée dans le dossier input/")
        return False
    
    print(f"📁 Chargement de {len(image_paths)} images...")
    
    # Encoder les images en base64
    images_b64 = []
    for img_path in image_paths:
        with open(img_path, "rb") as f:
            img_data = f.read()
            img_b64 = base64.b64encode(img_data).decode('utf-8')
            images_b64.append(img_b64)
    
    # Envoyer la requête
    payload = {
        "images": images_b64
    }
    
    print("📤 Envoi de la requête à l'API...")
    try:
        response = requests.post(
            f"{API_URL}/api/detect-stenosis",
            json=payload,
            timeout=60
        )
        
        if response.status_code == 200:
            result = response.json()
            
            if result.get('success'):
                print("✅ Détection réussie!")
                print(f"   Images traitées: {result.get('processed_images')}")
                print(f"   📊 Sténose gauche: {result.get('stenosis_left_percent'):.2f}%")
                print(f"   📊 Sténose droite: {result.get('stenosis_right_percent'):.2f}%")
                print(f"   Masques générés: {len(result.get('masks', []))}")
                
                # Sauvegarder les résultats
                with open("test_results.json", "w") as f:
                    # Ne pas sauvegarder les masques (trop gros)
                    result_copy = result.copy()
                    result_copy['masks'] = f"{len(result.get('masks', []))} masques"
                    json.dump(result_copy, f, indent=2)
                print("   💾 Résultats sauvegardés dans test_results.json")
                
                return True
            else:
                print(f"❌ Erreur dans la détection: {result.get('error')}")
                return False
        else:
            print(f"❌ Erreur HTTP: {response.status_code}")
            print(f"   {response.text}")
            return False
    
    except Exception as e:
        print(f"❌ Erreur: {e}")
        return False

def test_process_single():
    """Test de l'endpoint /api/process-single"""
    print("\n🧪 Test 3: Traitement Image Unique")
    
    # Trouver une image de test
    image_paths = glob("input/*.png")
    
    if not image_paths:
        print("❌ Aucune image trouvée dans le dossier input/")
        return False
    
    img_path = image_paths[0]
    print(f"📁 Image: {os.path.basename(img_path)}")
    
    # Encoder en base64
    with open(img_path, "rb") as f:
        img_data = f.read()
        img_b64 = base64.b64encode(img_data).decode('utf-8')
    
    payload = {
        "image": img_b64
    }
    
    print("📤 Envoi de la requête...")
    try:
        response = requests.post(
            f"{API_URL}/api/process-single",
            json=payload,
            timeout=30
        )
        
        if response.status_code == 200:
            result = response.json()
            
            if result.get('success'):
                print("✅ Traitement réussi!")
                print(f"   Aire gauche: {result.get('area_left'):.2f} pixels²")
                print(f"   Aire droite: {result.get('area_right'):.2f} pixels²")
                print(f"   Masque généré: Oui")
                
                # Sauvegarder le masque
                mask_b64 = result.get('mask')
                if mask_b64:
                    mask_data = base64.b64decode(mask_b64)
                    with open("test_mask.png", "wb") as f:
                        f.write(mask_data)
                    print("   💾 Masque sauvegardé dans test_mask.png")
                
                return True
            else:
                print(f"❌ Erreur: {result.get('error')}")
                return False
        else:
            print(f"❌ Erreur HTTP: {response.status_code}")
            print(f"   {response.text}")
            return False
    
    except Exception as e:
        print(f"❌ Erreur: {e}")
        return False

def run_all_tests():
    """Exécute tous les tests"""
    print("=" * 60)
    print("🚀 TEST DE L'API DE DÉTECTION DE STÉNOSE")
    print("=" * 60)
    
    results = []
    
    # Test 1: Health check
    results.append(("Health Check", test_health()))
    
    # Test 2: Détection complète
    if results[0][1]:  # Si l'API est accessible
        results.append(("Détection Sténose", test_detect_stenosis()))
        results.append(("Image Unique", test_process_single()))
    
    # Résumé
    print("\n" + "=" * 60)
    print("📊 RÉSUMÉ DES TESTS")
    print("=" * 60)
    
    for test_name, success in results:
        status = "✅ PASS" if success else "❌ FAIL"
        print(f"{status} - {test_name}")
    
    total = len(results)
    passed = sum(1 for _, success in results if success)
    
    print(f"\nRésultat: {passed}/{total} tests passés")
    
    if passed == total:
        print("🎉 Tous les tests sont réussis! L'API est prête.")
    else:
        print("⚠️  Certains tests ont échoué. Vérifiez les logs ci-dessus.")

if __name__ == "__main__":
    run_all_tests()
