# API Flask - Detection de Stenose

## Demarrage

```bash
python flask_api.py
```

Serveur demarre sur: `http://localhost:5000`

---

## Endpoints

### 1. Health Check

**GET** `/api/health`

**Reponse:**
```json
{
  "status": "healthy",
  "model_loaded": true,
  "version": "1.0.0"
}
```

---

### 2. Detection Stenose (RECOMMANDE)

**POST** `/api/detect-stenosis-center`

**Body:**
```json
{
  "dicom_folder": "D:/Data/Patient001/DICOM",
  "center_slice": 595
}
```

**Reponse:**
```json
{
  "success": true,
  "stenosis_left_percent": 9.13,
  "stenosis_right_percent": 36.89,
  "processed_images": 61,
  "center_slice": 595,
  "start_slice": 565,
  "end_slice": 625
}
```

---

### 3. Detection Stenose (Legacy - avec images)

**POST** `/api/detect-stenosis`

**Body:**
```json
{
  "images": ["base64_image1", "base64_image2", ...]
}
```

---

## Configuration

Fichier: `config.ini`

```ini
[image]
width = 512
height = 512

[model]
class_weight = 2.0
threshold = 0.5
```

---

## Dependances

```
Flask>=2.3.0
tensorflow>=2.10.0
opencv-python>=4.5.0
numpy>=1.24.0
pydicom>=2.3.0
Pillow>=10.0.0
flask-cors>=4.0.0
```

Installation:
```bash
pip install -r requirements_api.txt
```

---

## Test API

```bash
python test_endpoint_centre.py "CHEMIN_DICOM" 595
```
