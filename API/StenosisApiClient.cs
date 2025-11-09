using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DeepBridgeWindowsApp.API
{
    /// <summary>
    /// Client pour communiquer avec l'API Flask de détection de sténose
    /// </summary>
    public class StenosisApiClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public StenosisApiClient(string baseUrl = "http://localhost:5000")
        {
            _baseUrl = baseUrl;
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(5) // Timeout élevé pour le traitement ML
            };
        }

        /// <summary>
        /// Vérifie que l'API est accessible et fonctionnelle
        /// </summary>
        public async Task<bool> CheckHealthAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/health");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var healthData = JsonSerializer.Deserialize<HealthResponse>(content);
                    return healthData?.ModelLoaded ?? false;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Détecte la sténose à partir d'une liste d'images Bitmap
        /// </summary>
        /// <param name="images">Liste des images à analyser</param>
        /// <returns>Résultat de la détection</returns>
        public async Task<StenosisDetectionResult> DetectStenosisAsync(List<Bitmap> images)
        {
            try
            {
                // Convertir les images en base64
                var imageBase64List = new List<string>();
                foreach (var image in images)
                {
                    var base64 = ConvertBitmapToBase64(image);
                    imageBase64List.Add(base64);
                }

                // Créer le payload JSON
                var payload = new
                {
                    images = imageBase64List
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Envoyer la requête
                var response = await _httpClient.PostAsync($"{_baseUrl}/api/detect-stenosis", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Erreur API: {response.StatusCode} - {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<StenosisDetectionResult>(responseContent);

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Erreur lors de la détection de sténose: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Détecte la sténose à partir du centre du cou (NOUVEAU - Recommandé)
        /// Python charge lui-même les DICOM depuis le disque
        /// </summary>
        /// <param name="dicomFolder">Chemin vers le dossier contenant les fichiers DICOM</param>
        /// <param name="centerSlice">Numéro de la slice centrale du cou</param>
        /// <returns>Résultat de la détection</returns>
        public async Task<StenosisDetectionResult> DetectStenosisFromCenterAsync(string dicomFolder, int centerSlice)
        {
            try
            {
                // Créer le payload JSON
                var payload = new
                {
                    dicom_folder = dicomFolder,
                    center_slice = centerSlice
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Envoyer la requête au NOUVEAU endpoint
                var response = await _httpClient.PostAsync($"{_baseUrl}/api/detect-stenosis-center", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Erreur API: {response.StatusCode} - {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<StenosisDetectionResult>(responseContent);

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Erreur lors de la détection de sténose depuis le centre: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Traite une seule image et retourne le masque de segmentation
        /// </summary>
        public async Task<SingleImageResult> ProcessSingleImageAsync(Bitmap image)
        {
            try
            {
                var base64Image = ConvertBitmapToBase64(image);

                var payload = new
                {
                    image = base64Image
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/api/process-single", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Erreur API: {response.StatusCode} - {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<SingleImageResult>(responseContent);

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Erreur lors du traitement de l'image: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Convertit un Bitmap en string base64
        /// </summary>
        private string ConvertBitmapToBase64(Bitmap bitmap)
        {
            using (var memoryStream = new MemoryStream())
            {
                bitmap.Save(memoryStream, ImageFormat.Png);
                var bytes = memoryStream.ToArray();
                return Convert.ToBase64String(bytes);
            }
        }

        /// <summary>
        /// Convertit une string base64 en Bitmap
        /// </summary>
        public static Bitmap ConvertBase64ToBitmap(string base64String)
        {
            var bytes = Convert.FromBase64String(base64String);
            using (var memoryStream = new MemoryStream(bytes))
            {
                return new Bitmap(memoryStream);
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    #region Response Models

    public class HealthResponse
    {
        public string Status { get; set; }
        public bool ModelLoaded { get; set; }
        public string Version { get; set; }

        // Pour compatibilité JSON snake_case
        [System.Text.Json.Serialization.JsonPropertyName("status")]
        public string StatusJson { get => Status; set => Status = value; }
        
        [System.Text.Json.Serialization.JsonPropertyName("model_loaded")]
        public bool ModelLoadedJson { get => ModelLoaded; set => ModelLoaded = value; }
        
        [System.Text.Json.Serialization.JsonPropertyName("version")]
        public string VersionJson { get => Version; set => Version = value; }
    }

    public class StenosisDetectionResult
    {
        public bool Success { get; set; }
        public double StenosisLeftPercent { get; set; }
        public double StenosisRightPercent { get; set; }
        public int ProcessedImages { get; set; }
        public List<double> AreasLeft { get; set; }
        public List<double> AreasRight { get; set; }
        public List<string> Masks { get; set; } // Base64 encoded masks
        public string Error { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("success")]
        public bool SuccessJson { get => Success; set => Success = value; }

        [System.Text.Json.Serialization.JsonPropertyName("stenosis_left_percent")]
        public double StenosisLeftPercentJson { get => StenosisLeftPercent; set => StenosisLeftPercent = value; }

        [System.Text.Json.Serialization.JsonPropertyName("stenosis_right_percent")]
        public double StenosisRightPercentJson { get => StenosisRightPercent; set => StenosisRightPercent = value; }

        [System.Text.Json.Serialization.JsonPropertyName("processed_images")]
        public int ProcessedImagesJson { get => ProcessedImages; set => ProcessedImages = value; }

        [System.Text.Json.Serialization.JsonPropertyName("areas_left")]
        public List<double> AreasLeftJson { get => AreasLeft; set => AreasLeft = value; }

        [System.Text.Json.Serialization.JsonPropertyName("areas_right")]
        public List<double> AreasRightJson { get => AreasRight; set => AreasRight = value; }

        [System.Text.Json.Serialization.JsonPropertyName("masks")]
        public List<string> MasksJson { get => Masks; set => Masks = value; }

        [System.Text.Json.Serialization.JsonPropertyName("error")]
        public string ErrorJson { get => Error; set => Error = value; }
    }

    public class SingleImageResult
    {
        public bool Success { get; set; }
        public string Mask { get; set; } // Base64
        public double AreaLeft { get; set; }
        public double AreaRight { get; set; }
        public string Error { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("success")]
        public bool SuccessJson { get => Success; set => Success = value; }

        [System.Text.Json.Serialization.JsonPropertyName("mask")]
        public string MaskJson { get => Mask; set => Mask = value; }

        [System.Text.Json.Serialization.JsonPropertyName("area_left")]
        public double AreaLeftJson { get => AreaLeft; set => AreaLeft = value; }

        [System.Text.Json.Serialization.JsonPropertyName("area_right")]
        public double AreaRightJson { get => AreaRight; set => AreaRight = value; }

        [System.Text.Json.Serialization.JsonPropertyName("error")]
        public string ErrorJson { get => Error; set => Error = value; }
    }

    #endregion
}
