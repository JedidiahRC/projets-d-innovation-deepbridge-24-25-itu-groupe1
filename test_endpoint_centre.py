#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Test du nouvel endpoint avec juste le centre du cou"""

import requests
import json
import sys

if len(sys.argv) < 3:
    print("Usage: python test_endpoint_centre.py <dossier_dicom> <centre_slice>")
    print("Exemple: python test_endpoint_centre.py 'D:/Data/Patient001' 625")
    sys.exit(1)

dicom_folder = sys.argv[1]
center_slice = int(sys.argv[2])

print("="*60)
print("TEST NOUVEL ENDPOINT - /api/detect-stenosis-center")
print("="*60)
print(f"Dossier DICOM: {dicom_folder}")
print(f"Centre du cou: slice {center_slice}")
print(f"Zone analysée: {center_slice - 30} à {center_slice + 30}")
print()
print("Envoi à l'API...")

# Envoyer seulement le chemin + centre
payload = {
    "dicom_folder": dicom_folder,
    "center_slice": center_slice
}

try:
    response = requests.post(
        "http://localhost:5000/api/detect-stenosis-center",
        json=payload,
        timeout=300
    )
    
    result = response.json()
    
    if not result.get('success'):
        print(f"ERREUR: {result.get('error')}")
        sys.exit(1)
    
    print()
    print("="*60)
    print("RÉSULTATS")
    print("="*60)
    print(f"Centre envoyé:       Slice {center_slice}")
    print(f"Zone analysée:       Slices {result['start_slice']} à {result['end_slice']}")
    print(f"Images traitées:     {result['processed_images']}")
    print()
    print(f"Sténose GAUCHE:      {result['stenosis_left_percent']}%")
    print(f"Sténose DROITE:      {result['stenosis_right_percent']}%")
    print()
    
    # Sévérité
    max_stenosis = max(result['stenosis_left_percent'], result['stenosis_right_percent'])
    if max_stenosis < 30:
        severity = "LÉGÈRE"
    elif max_stenosis < 50:
        severity = "MODÉRÉE"
    elif max_stenosis < 70:
        severity = "SIGNIFICATIVE"
    else:
        severity = "SÉVÈRE"
    
    print(f"Sévérité:            {severity}")
    print("="*60)
    
    # Sauvegarder
    output_file = f"test_endpoint_centre_{center_slice}.json"
    with open(output_file, 'w') as f:
        json.dump(result, f, indent=2)
    
    print()
    print(f"Résultats sauvegardés: {output_file}")
    
except requests.exceptions.ConnectionError:
    print("ERREUR: Impossible de se connecter à l'API")
    print("Assurez-vous que l'API Flask est démarrée:")
    print("  cd StenoseDetection_U-Net")
    print("  python flask_api.py")
    sys.exit(1)
except Exception as e:
    print(f"ERREUR: {e}")
    sys.exit(1)
