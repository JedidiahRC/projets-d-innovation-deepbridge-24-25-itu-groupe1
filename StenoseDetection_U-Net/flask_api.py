"""
API Flask pour la détection de sténose carotidienne
Expose les fonctionnalités du modèle U-Net via REST API
"""

from flask import Flask, request, jsonify
from flask_cors import CORS
import cv2
import numpy as np
import tensorflow as tf
from tensorflow.keras.models import load_model
from tensorflow.keras.optimizers import Adam
import os
import base64
from io import BytesIO
from PIL import Image
import configparser

app = Flask(__name__)
CORS(app)  # Permet les requêtes depuis l'application C#

# Configuration
config = configparser.ConfigParser()
config.read("config.ini")
IMG_WIDTH = int(config["image"]["width"])
IMG_HEIGHT = int(config["image"]["height"])
CLASS_WEIGHT = float(config["model"]["class_weight"])
THRESHOLD = float(config["model"]["threshold"])

# Modèle global (chargé au démarrage)
model = None

# Fonctions personnalisées pour le modèle
def weighted_binary_crossentropy(y_true, y_pred):
    pos_weight = CLASS_WEIGHT
    epsilon = tf.keras.backend.epsilon()
    y_pred = tf.clip_by_value(y_pred, epsilon, 1.0 - epsilon)
    bce = -(y_true * tf.math.log(y_pred) + (1.0 - y_true) * tf.math.log(1.0 - y_pred))
    weighted_bce = bce * (y_true * pos_weight + (1.0 - y_true))
    return tf.reduce_mean(weighted_bce)

def dice_coefficient(y_true, y_pred, smooth=1.0):
    y_true_f = tf.cast(tf.keras.backend.flatten(y_true), tf.float32)
    y_pred_f = tf.keras.backend.flatten(y_pred)
    intersection = tf.keras.backend.sum(y_true_f * y_pred_f)
    return (2. * intersection + smooth) / (tf.keras.backend.sum(y_true_f) + tf.keras.backend.sum(y_pred_f) + smooth)

def load_unet_model(model_path="carotide_detector_v2.h5"):
    """Charge le modèle U-Net au démarrage de l'API"""
    try:
        model = load_model(model_path, compile=False)
        model.compile(
            optimizer=Adam(learning_rate=0.0001),
            loss=weighted_binary_crossentropy,
            metrics=['accuracy', dice_coefficient]
        )
        print(f" Modèle {model_path} chargé avec succès")
        return model
    except Exception as e:
        print(f" Erreur lors du chargement du modèle: {e}")
        return None

def preprocess_image(image_array):
    """Prétraite une image pour la prédiction"""
    # Convertir en grayscale si nécessaire
    if len(image_array.shape) == 3:
        image = cv2.cvtColor(image_array, cv2.COLOR_BGR2GRAY)
    else:
        image = image_array
    
    # Redimensionner
    image = cv2.resize(image, (IMG_WIDTH, IMG_HEIGHT))
    
    # Normaliser
    image = image / 255.0
    
    # CLAHE pour améliorer le contraste
    clahe = cv2.createCLAHE(clipLimit=2.0, tileGridSize=(8, 8))
    image = clahe.apply((image * 255).astype(np.uint8)) / 255.0
    
    return image

def extract_carotid_areas(mask):
    """Extrait les aires des carotides gauche et droite depuis un masque"""
    # Binariser le masque
    _, thresh = cv2.threshold((mask * 255).astype(np.uint8), 127, 255, cv2.THRESH_BINARY)
    
    # Trouver les contours
    contours, _ = cv2.findContours(thresh, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
    
    shapes = []
    for i, cnt in enumerate(contours):
        area = cv2.contourArea(cnt)
        M = cv2.moments(cnt)
        if M["m00"] != 0:
            cx = int(M["m10"] / M["m00"])
            shapes.append({"area": area, "cx": cx})
    
    # Trier par position x (gauche à droite)
    shapes.sort(key=lambda s: s["cx"])
    
    if len(shapes) >= 2:
        return shapes[0]["area"], shapes[1]["area"]
    elif len(shapes) == 1:
        return shapes[0]["area"], 0
    else:
        return 0, 0

def calculate_stenosis(areas_left, areas_right):
    """Calcule le pourcentage de sténose pour chaque carotide"""
    if len(areas_left) == 0 or len(areas_right) == 0:
        return 0.0, 0.0
    
    A_left_max = max(areas_left)
    A_right_max = max(areas_right)
    
    # Éviter division par zéro
    if A_left_max == 0 or A_right_max == 0:
        return 0.0, 0.0
    
    # Calcul de la sténose pondérée
    stenosis_left = np.mean([1 - np.sqrt(a / A_left_max) for a in areas_left]) * 100
    stenosis_right = np.mean([1 - np.sqrt(a / A_right_max) for a in areas_right]) * 100
    
    return stenosis_left, stenosis_right

@app.route('/api/health', methods=['GET'])
def health_check():
    """Vérifie que l'API est fonctionnelle"""
    return jsonify({
        "status": "healthy",
        "model_loaded": model is not None,
        "version": "1.0.0"
    })

@app.route('/api/detect-stenosis', methods=['POST'])
def detect_stenosis():
    """
    Endpoint principal pour détecter la sténose
    
    Accepte:
    - images: liste d'images encodées en base64
    OU
    - files: upload de fichiers images
    
    Retourne:
    - stenosis_left: % de sténose carotide gauche
    - stenosis_right: % de sténose carotide droite
    - processed_images: nombre d'images traitées
    """
    try:
        if model is None:
            return jsonify({"error": "Modèle non chargé"}), 500
        
        images = []
        
        # Cas 1: Images encodées en base64 dans le JSON
        if request.is_json:
            data = request.get_json()
            image_data_list = data.get('images', [])
            
            for img_b64 in image_data_list:
                # Décoder base64
                img_bytes = base64.b64decode(img_b64)
                img = Image.open(BytesIO(img_bytes))
                img_array = np.array(img)
                images.append(img_array)
        
        # Cas 2: Upload de fichiers
        elif 'files' in request.files:
            files = request.files.getlist('files')
            for file in files:
                img = Image.open(file.stream)
                img_array = np.array(img)
                images.append(img_array)
        
        else:
            return jsonify({"error": "Aucune image fournie"}), 400
        
        if len(images) == 0:
            return jsonify({"error": "Aucune image valide trouvée"}), 400
        
        # Traiter chaque image
        areas_left = []
        areas_right = []
        masks_b64 = []
        
        for img_array in images:
            # Prétraitement
            processed_img = preprocess_image(img_array)
            input_img = processed_img.reshape(1, IMG_HEIGHT, IMG_WIDTH, 1)
            
            # Prédiction
            prediction = model.predict(input_img, verbose=0)[0]
            
            # Binariser
            binary_pred = (prediction > THRESHOLD).astype(np.uint8)
            
            # Extraire les aires
            area_left, area_right = extract_carotid_areas(binary_pred)
            areas_left.append(area_left)
            areas_right.append(area_right)
            
            # Encoder le masque en base64 pour le retour (optionnel)
            mask_img = (binary_pred.reshape(IMG_HEIGHT, IMG_WIDTH) * 255).astype(np.uint8)
            _, buffer = cv2.imencode('.png', mask_img)
            mask_b64 = base64.b64encode(buffer).decode('utf-8')
            masks_b64.append(mask_b64)
        
        # Calculer le pourcentage de sténose
        stenosis_left, stenosis_right = calculate_stenosis(areas_left, areas_right)
        
        return jsonify({
            "success": True,
            "stenosis_left_percent": round(stenosis_left, 2),
            "stenosis_right_percent": round(stenosis_right, 2),
            "processed_images": len(images),
            "areas_left": areas_left,
            "areas_right": areas_right,
            "masks": masks_b64  # Masques encodés en base64
        })
    
    except Exception as e:
        return jsonify({
            "success": False,
            "error": str(e)
        }), 500

@app.route('/api/process-single', methods=['POST'])
def process_single_image():
    """
    Traite une seule image et retourne le masque de segmentation
    """
    try:
        if model is None:
            return jsonify({"error": "Modèle non chargé"}), 500
        
        # Recevoir l'image
        if 'file' in request.files:
            file = request.files['file']
            img = Image.open(file.stream)
            img_array = np.array(img)
        elif request.is_json:
            data = request.get_json()
            img_b64 = data.get('image')
            img_bytes = base64.b64decode(img_b64)
            img = Image.open(BytesIO(img_bytes))
            img_array = np.array(img)
        else:
            return jsonify({"error": "Aucune image fournie"}), 400
        
        # Traitement
        processed_img = preprocess_image(img_array)
        input_img = processed_img.reshape(1, IMG_HEIGHT, IMG_WIDTH, 1)
        
        # Prédiction
        prediction = model.predict(input_img, verbose=0)[0]
        binary_pred = (prediction > THRESHOLD).astype(np.uint8)
        
        # Extraire les aires
        area_left, area_right = extract_carotid_areas(binary_pred)
        
        # Encoder le masque
        mask_img = (binary_pred.reshape(IMG_HEIGHT, IMG_WIDTH) * 255).astype(np.uint8)
        _, buffer = cv2.imencode('.png', mask_img)
        mask_b64 = base64.b64encode(buffer).decode('utf-8')
        
        return jsonify({
            "success": True,
            "mask": mask_b64,
            "area_left": float(area_left),
            "area_right": float(area_right)
        })
    
    except Exception as e:
        return jsonify({
            "success": False,
            "error": str(e)
        }), 500


@app.route('/api/detect-stenosis-center', methods=['POST'])
def detect_stenosis_from_center():
    """
    Endpoint pour détecter la sténose à partir du centre du cou
    
    Accepte:
    - dicom_folder: chemin vers le dossier contenant les DICOM
    - center_slice: numéro de la slice centrale du cou
    
    Retourne:
    - stenosis_left: % de sténose carotide gauche
    - stenosis_right: % de sténose carotide droite
    - processed_images: nombre d'images traitées
    """
    try:
        if model is None:
            return jsonify({"error": "Modèle non chargé"}), 500
        
        # Recevoir les paramètres
        data = request.get_json()
        dicom_folder = data.get('dicom_folder')
        center_slice = data.get('center_slice')
        
        if not dicom_folder or center_slice is None:
            return jsonify({"error": "Paramètres manquants: dicom_folder et center_slice requis"}), 400
        
        # Importer pydicom
        try:
            import pydicom
            from pathlib import Path
        except ImportError:
            return jsonify({"error": "Module pydicom non installé. Installer avec: pip install pydicom"}), 500
        
        # Lister les fichiers DICOM
        dicom_path = Path(dicom_folder)
        if not dicom_path.exists():
            return jsonify({"error": f"Dossier non trouvé: {dicom_folder}"}), 404
        
        dicom_files = sorted(list(dicom_path.glob("*.dcm")))
        if len(dicom_files) == 0:
            return jsonify({"error": f"Aucun fichier DICOM trouvé dans {dicom_folder}"}), 404
        
        # Calculer les limites
        total_files = len(dicom_files)
        start_slice = max(0, center_slice - 30)
        end_slice = min(total_files - 1, center_slice + 30)
        
        # Sélectionner les slices
        selected_files = dicom_files[start_slice:end_slice + 1]
        
        images = []
        for dcm_file in selected_files:
            # Lire le DICOM
            ds = pydicom.dcmread(str(dcm_file))
            img_array = ds.pixel_array
            
            # Normaliser
            img_normalized = ((img_array - img_array.min()) / 
                            (img_array.max() - img_array.min()) * 255).astype(np.uint8)
            
            images.append(img_normalized)
        
        # Traiter chaque image
        areas_left = []
        areas_right = []
        masks_b64 = []
        
        for img_array in images:
            # Prétraitement
            processed_img = preprocess_image(img_array)
            input_img = processed_img.reshape(1, IMG_HEIGHT, IMG_WIDTH, 1)
            
            # Prédiction
            prediction = model.predict(input_img, verbose=0)[0]
            
            # Binariser
            binary_pred = (prediction > THRESHOLD).astype(np.uint8)
            
            # Extraire les aires
            area_left, area_right = extract_carotid_areas(binary_pred)
            areas_left.append(area_left)
            areas_right.append(area_right)
            
            # Encoder le masque (optionnel)
            mask_img = (binary_pred.reshape(IMG_HEIGHT, IMG_WIDTH) * 255).astype(np.uint8)
            _, buffer = cv2.imencode('.png', mask_img)
            mask_b64 = base64.b64encode(buffer).decode('utf-8')
            masks_b64.append(mask_b64)
        
        # Calculer le pourcentage de sténose
        stenosis_left, stenosis_right = calculate_stenosis(areas_left, areas_right)
        
        return jsonify({
            "success": True,
            "stenosis_left_percent": round(stenosis_left, 2),
            "stenosis_right_percent": round(stenosis_right, 2),
            "processed_images": len(images),
            "center_slice": center_slice,
            "start_slice": start_slice,
            "end_slice": end_slice,
            "areas_left": areas_left,
            "areas_right": areas_right,
            "masks": masks_b64
        })
    
    except Exception as e:
        return jsonify({
            "success": False,
            "error": str(e)
        }), 500


if __name__ == '__main__':
    print(" Démarrage de l'API de détection de sténose...")
    model = load_unet_model()
    
    if model is None:
        print("  ATTENTION: L'API démarre mais le modèle n'est pas chargé!")
    
    app.run(host='0.0.0.0', port=5000, debug=True)
