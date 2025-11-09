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

---

## 📚 Licence

Projet académique – Master 2 MIAGE MBDS 2024-2025  
Utilisation à des fins pédagogiques uniquement.

---

🧠 *« Mesurer pour comprendre, détecter pour prévenir. »*
