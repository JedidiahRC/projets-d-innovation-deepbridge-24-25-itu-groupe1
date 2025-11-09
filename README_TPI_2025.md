# Détection de Sténose Carotidienne

## Objectif du projet

Ce projet vise à **déterminer le pourcentage de sténose** (bouchage) des **carotides gauche et droite** à partir d’images médicales **en noir et blanc** (coupes suivant l’axe Z du cerveau).

L’idée est d’utiliser un modèle de segmentation (U-Net) pour détecter automatiquement les carotides, puis d’estimer le **degré de rétrécissement** à partir de la surface détectée sur chaque image.

---

## Membres du projet

| Nom complet | Numéro |
|--------------|---------|
| 👤 ANDRIANTSILAVINA Tina | n°9 |
| 👤 MAHEFARISON Itokiana Ghislain | n°15 |
| 👤 RABEMIARINTSOA Christy Jedidiah | n°17 |
| 👤 RANDRIAMAROSAINA Sombiniaina Fitahiana | n°43 |
| 👤 RANDRIANIFANANA Petit Jean Clavel | n°46 |

---

## Base du projet

Le projet s’appuie sur le travail existant :

🔗 [Détection des carotides (U-Net)](https://github.com/Master-2-MIAGE-MBDS/projets-d-innovation-24-25-rocamora-quatela-lafaire-canavaggio)

Ce dépôt contient un modèle de segmentation **U-Net** nommé **IACarotideU-Net**, qui est utilisé ici pour détecter les zones carotidiennes sur les images d’entrée.

---

## Processus général

1. **Entrée** : images pré-sélectionnées (coupes suivant l’axe Z).
2. **Segmentation** : le modèle `carotide_detector_v2.h5` détecte les carotides gauche et droite.
3. **Extraction des surfaces** :
    - Les **aires** (en pixels²) de chaque carotide sont mesurées à partir des masques binaires.
4. **Détermination du pourcentage de sténose** :
    - On considère :
        - `A_max` = aire maximale observée (carotide non bouchée)
        - `A_min` = aire minimale observée (carotide la plus sténosée)
    - Le **pourcentage de sténose** est calculé par :

      $\text{Sténose estimée (\%)} =
      \frac{\sum_i w_i \cdot \left( 1 - \frac{A_i}{A_{\max}} \right)}{\sum_i w_i} \times 100$
        - $A_{\max}$ = aire maximale (approximation du diamètre normal)
        - $A_i$ = aire détectée sur l’image $i$
        - $w_i$ = poids de chaque image (par exemple $1$ si toutes les images ont le même poids, ou selon qualité de segmentation)
    - Pour un ensemble d’images, une **moyenne pondérée** est calculée pour obtenir un **pourcentage global** de sténose par côté.

---

## 📁 Structure du projet

```
/
│
├── input/                # Images d'entrée (format PNG)
│
├── result/               # Résultats de la détection
│   ├── mask/             # Masques binaires (carotides détectées)
│   └── overlay/          # Superposition image + masque
│
├── carotide_detector_v2.h5   # Modèle U-Net pré-entraîné
├── main.ipynb                # Script principal (Jupyter Notebook)
└── README.md                 # Documentation du projet
```

---

## Installation et exécution

### Installation des dépendances

Ouvre un terminal dans le dossier du projet et exécute :

```bash
pip install -r requirements.txt
```

### Lancement du projet

Lance le notebook principal :

```bash
jupyter notebook main.ipynb
```

---

## Résultats attendus

- Le dossier `result/mask/` contiendra les **zones carotidiennes détectées**.
- Le dossier `result/overlay/` affichera les **superpositions** image originale + masque.
- Le script affichera le **pourcentage de sténose gauche et droite**, calculé selon la formule décrite.

---

## Modèle utilisé : U-Net

Le modèle **U-Net** est un réseau de neurones convolutionnel conçu pour la **segmentation d’images biomédicales**.  
Il présente une structure en **U symétrique** :
- Une **phase de contraction** (encodage) pour extraire les caractéristiques,
- Une **phase d’expansion** (décodage) pour reconstruire les contours précis des régions d’intérêt.

---

## Référence mathématique

Formule du **taux de sténose** :   
$S(\%) = \left(1 - \frac{A_\text{sténosée}}{A_\text{normale}}\right) \times 100$

Formule du **taux global pondéré** sur plusieurs images :  
$S_\text{global} = \frac{\sum_i S_i \cdot w_i}{\sum_i w_i}$
où \( $w_i$ = $A_i$ \) représente un poids basé sur la taille mesurée.


# Integration Detection de Stenose - Documentation Technique

## Vue d'Ensemble

Integration d'un modele U-Net Python (detection de stenose carotidienne) dans une application C# WinForms via API REST Flask.

---

## Etapes d'Integration

### 1. Point de Depart
- Dossier `StenoseDetection_U-Net/` ajoute au projet
- Contient: modele U-Net (`carotide_detector_v2.h5`), scripts Python

### 2. Creation API Python
- Fichier: `StenoseDetection_U-Net/flask_api.py`
- Endpoint: `/api/detect-stenosis-center`
- Fonction: `detect_stenosis_from_center(dicom_folder, center_slice)`

### 3. Creation Client C#
- Fichier: `API/StenosisApiClient.cs`
- Methode: `DetectStenosisFromCenterAsync(string dicomFolder, int centerSlice)`

### 4. Integration Interface
- Fichier: `DicomViewerForm.cs`
- Ajout: Detection automatique du cou, bouton "Localiser Stenose"


---

## Outils et Librairies

### Backend Python

| Librairie | Version | Role |
|-----------|---------|------|
| Flask | 2.3.x | Serveur API REST |
| TensorFlow | 2.x | Chargement et execution U-Net |
| OpenCV (cv2) | 4.x | Traitement d'images |
| NumPy | 1.24.x | Calculs mathematiques |
| pydicom | 2.x | Lecture fichiers DICOM |
| Pillow (PIL) | 10.x | Conversion images |

### Frontend C#

| Package | Version | Role |
|---------|---------|------|
| System.Net.Http | .NET 8 | Client HTTP |
| System.Text.Json | .NET 8 | Serialization JSON |
| System.Drawing | .NET 8 | Manipulation images |
| fo-dicom | 5.x | Lecture DICOM |

---

## Flux de Donnees

```
[C# Application]
      |
      | 1. Charge dossier DICOM
      |    Stocke: currentDicomFolderPath
      |
      | 2. Detection automatique cou
      |    Fonction: FindNeckPosition()
      |    Resultat: centerSlice = 595
      |
      | 3. Clic "Localiser Stenose"
      |    Fonction: DetectStenosisButton_Click()
      |
      v
[HTTP POST]
      |
      | Payload JSON:
      | {
      |   "dicom_folder": "D:/Data/Patient/DICOM",
      |   "center_slice": 595
      | }
      |
      v
[Python Flask API]
      |
      | Endpoint: /api/detect-stenosis-center
      | Fonction: detect_stenosis_from_center()
      |
      | 4. Charge fichiers DICOM
      |    Slices: 565 a 625 (center +/- 30)
      |
      | 5. Pour chaque image (61 total):
      |    a. preprocess_image()
      |       - Resize 512x512
      |       - Normalisation 0-1
      |       - CLAHE (amelioration contraste)
      |
      |    b. model.predict() [U-Net]
      |       - Segmentation carotides
      |       - Output: masque binaire
      |
      |    c. extract_carotid_areas()
      |       - Detection contours
      |       - Calcul aires gauche/droite
      |       - Stockage: areas_left[], areas_right[]
      |
      | 6. calculate_stenosis()
      |    - Max aire: A_max = max(areas)
      |    - Pour chaque aire:
      |      stenosis = 1 - sqrt(aire / A_max)
      |    - Moyenne ponderee * 100
      |
      v
[Response JSON]
      |
      | {
      |   "stenosis_left_percent": 9.13,
      |   "stenosis_right_percent": 36.89,
      |   "processed_images": 61
      | }
      |
      v
[C# Application]
      |
      | 7. Fonction: ShowStenosisResults()
      |    Affichage interface utilisateur
```

---

## Flux d'Execution des Fonctions

### Demarrage Application

```
Program.Main()
  -> MainForm.Load()
     -> ViewDicomButton_Click()
        -> DicomViewerForm(reader, dicomPath)
           -> InitializeComponents()
              - Cree findNeckButton
              - Cree detectStenosisButton
```

### Detection Stenose

```
1. Utilisateur: Clic "Localiser Stenose"
   |
   v
2. DetectStenosisButton_Click()
   |
   |- IsNeckDetected() ? NON
   |  |
   |  v
   |- FindNeckPosition()
   |  |- Analyse slices [centerSlice - 35%, centerSlice + 35%]
   |  |- CalculateEmptyRatio() pour chaque slice
   |  |- Trouve centre: bestSlice = 595
   |  |- Met a jour: sliceTrackBar.Value = 595
   |
   v
3. DetectStenosisFromCenterAsync(dicomPath, 595)
   |
   |- HttpClient.PostAsync("/api/detect-stenosis-center")
   |
   v
4. [Python] detect_stenosis_from_center()
   |
   |- pydicom.dcmread() x61 fichiers
   |
   |- Pour i = 565 a 625:
   |  |- preprocess_image(dicom[i])
   |  |- model.predict(image) -> mask
   |  |- extract_carotid_areas(mask) -> (area_L, area_R)
   |  |- Stocke areas_left[i], areas_right[i]
   |
   |- calculate_stenosis(areas_left, areas_right)
   |  |- A_max_left = max(areas_left)
   |  |- stenosis_left = mean([1 - sqrt(a/A_max_left)]) * 100
   |
   |- Return JSON
   |
   v
5. ShowStenosisResults(result)
   |- Affiche MessageBox avec resultats
```

---

## Architecture Technique

### Modele U-Net

**Fichier:** `StenoseDetection_U-Net/carotide_detector_v2.h5`

**Architecture:**
- Type: CNN (Convolutional Neural Network)
- Structure: Encoder-Decoder avec skip connections
- Input: Image 512x512x1 (grayscale)
- Output: Masque 512x512x1 (binaire)
- Fonction perte: Weighted Binary Crossentropy
- Metrique: Dice Coefficient

**Preprocessing:**
```python
1. Resize: 512x512
2. Normalisation: pixels / 255.0 (0-1)
3. CLAHE: clipLimit=2.0, tileGridSize=8x8
4. Reshape: (1, 512, 512, 1)
```

**Postprocessing:**
```python
1. Seuil: prediction > 0.5 -> binaire
2. Contours: cv2.findContours()
3. Moments: Calcul centroide (cx, cy)
4. Tri: Gauche (cx petit) vs Droite (cx grand)
5. Aire: cv2.contourArea()
```

### Calcul Stenose

**Formule:**
```
Pour chaque slice i:
  stenosis_i = 1 - sqrt(aire_i / aire_max)

Stenose finale = mean(stenosis_i) * 100%
```

**Interpretation:**
- < 30%: Legere
- 30-50%: Moderee
- 50-70%: Significative
- > 70%: Severe

---

## Configuration Requise

### Python
```
Python >= 3.8
TensorFlow >= 2.10
Flask >= 2.3
OpenCV >= 4.5
pydicom >= 2.3
```

### C#
```
.NET 8.0
fo-dicom >= 5.0
```

---

## Lancement

### 1. Demarrer API Python
```bash
cd StenoseDetection_U-Net
python flask_api.py
```

### 2. Lancer Application C#
```bash
dotnet run
```

### 3. Utilisation
1. Charger dossier DICOM
2. Cliquer "Localiser Stenose"
3. Attendre resultats (5-10s)

---

## Structure Fichiers Principaux

```
/
├── API/
│   └── StenosisApiClient.cs          # Client HTTP C#
├── StenoseDetection_U-Net/
│   ├── flask_api.py                  # API REST Python
│   ├── carotide_detector_v2.h5       # Modele U-Net
│   └── config.ini                    # Configuration
├── DicomViewerForm.cs                # Interface principale
├── MainForm.cs                       # Fenetre principale
└── Program.cs                        # Point entree

Fichiers de test:
├── test_endpoint_centre.py           # Test API Python
└── StenoseDetection_U-Net/commandesTerminal.txt  # Commandes test
```

---

## Points Techniques Importants

1. **Moyenne ponderee**: 61 images analysees, pas une seule
2. **Detection automatique**: Centre du cou detecte par analyse ratio de vide
3. **Architecture REST**: Separation frontend C# / backend Python
4. **Precision**: Moyenne sur multiple slices reduit bruit et artefacts
5. **Scalabilite**: API peut traiter plusieurs patients simultanement

---

## 📚 Licence

Projet académique – Master 2 MIAGE MBDS 2024-2025  
Utilisation à des fins pédagogiques uniquement.
