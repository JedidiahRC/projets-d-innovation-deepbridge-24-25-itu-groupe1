using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using EvilDICOM.Core.Helpers;
using EvilDICOM.Core;
using DeepBridgeWindowsApp.DICOM;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Security.Policy;
using DeepBridgeWindowsApp.Dicom;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.Text.Json;
using DeepBridgeWindowsApp.API;

namespace DeepBridgeWindowsApp
{
    public partial class DicomViewerForm : Form
    {
        private readonly DicomDisplayManager displayManager;
        private PictureBox mainPictureBox;
        private TrackBar sliceTrackBar;
        private TrackBar windowWidthTrackBar;
        private TrackBar windowCenterTrackBar;
        private DoubleTrackBar doubleTrackBar;
        private Label sliceLabel;
        private Label windowCenterLabel;
        private Label windowWidthLabel;
        private Label minLabel;
        private Label maxLabel;
        private Point startPoint;
        private Point endPoint;
        private bool isDrawing = false;
        private Label startPointLabel;
        private Label endPointLabel;
        private Label areaLabel;
        private Button optimizeWindowButton;
        private Button resetSelectionButton;
        private Button detectStenosisButton;
        private Button findNeckButton;
        private Button findCarotidButton;
        private const int TARGET_SIZE = 512;
        private string currentDicomFolderPath = "";  // Stocker le chemin du dossier DICOM

        // Variables pour la sélection des carotides
        private Rectangle carotidDisplayRect;
        private bool showCarotidSelection = false;

        public DicomViewerForm(DicomReader reader, string dicomFolderPath = "")
        {
            displayManager = new DicomDisplayManager(reader);
            currentDicomFolderPath = dicomFolderPath;  // Stocker le chemin
            InitializeComponents();
            mainPictureBox.MouseDown += MainPictureBox_MouseDown;
            mainPictureBox.MouseMove += MainPictureBox_MouseMove;
            mainPictureBox.MouseUp += MainPictureBox_MouseUp;
            mainPictureBox.Paint += MainPictureBox_Paint;
        }

        private void InitializeComponents()
        {
            this.Size = new Size(1424, 768); // Increased width to accommodate both panels
            this.Text = "DICOM Viewer";

            // Left info panel
            var infoPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 250,
                BackColor = SystemColors.Control,
                Padding = new Padding(5, 5, 5, 10),
            };

            var patientInfo = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.Single
            };

            var currentSlice = displayManager.GetSlice(displayManager.GetCurrentSliceIndex() + 1);

            AddInfoRow(patientInfo, "Patient ID", currentSlice.PatientID);
            AddInfoRow(patientInfo, "Patient Name", currentSlice.PatientName);
            AddInfoRow(patientInfo, "Patient Sex", currentSlice.PatientSex);
            AddInfoRow(patientInfo, "Modality", currentSlice.Modality);
            AddInfoRow(patientInfo, "Resolution", currentSlice.Rows + "x" + currentSlice.Columns);
            infoPanel.Controls.Add(patientInfo);

            // Add labels for start point, end point, and area
            startPointLabel = new Label
            {
                Text = "Start Point: (0, 0)",
                AutoSize = true,
                Location = new Point(10, patientInfo.Bottom + 10)
            };
            endPointLabel = new Label
            {
                Text = "End Point: (0, 0)",
                AutoSize = true,
                Location = new Point(10, startPointLabel.Bottom + 10)
            };
            areaLabel = new Label
            {
                Text = "Area: 0",
                AutoSize = true,
                Location = new Point(10, endPointLabel.Bottom + 10)
            };

            infoPanel.Controls.Add(startPointLabel);
            infoPanel.Controls.Add(endPointLabel);
            infoPanel.Controls.Add(areaLabel);

            var buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                BackColor = SystemColors.Control,
                Height = 150 // Augmenté pour accommoder le nouveau bouton
            };

            // Bouton pour trouver automatiquement le cou
            findNeckButton = new Button
            {
                Dock = DockStyle.Bottom,
                Text = "Localiser le cou",
                AutoSize = true,
                Margin = new Padding(10, 5, 10, 5),
                Height = 30
            };
            findNeckButton.Click += FindNeckButton_Click;

            // Bouton pour trouver automatiquement les carotides
            findCarotidButton = new Button
            {
                Dock = DockStyle.Bottom,
                Text = "Localiser les carotides",
                AutoSize = true,
                Margin = new Padding(10, 5, 10, 5),
                Height = 30
            };
            findCarotidButton.Click += FindCarotidButton_Click;

            var renderButton = new Button
            {
                Dock = DockStyle.Bottom,
                Text = "3D Render",
                AutoSize = true,
                Margin = new Padding(10, 5, 10, 5),
                Height = 30
            };
            renderButton.Click += Button_Click;

            resetSelectionButton = new Button
            {
                Dock = DockStyle.Bottom,
                Text = "Réinitialiser les sélections",
                AutoSize = true,
                Margin = new Padding(10, 5, 10, 5),
                Height = 30
            };
            resetSelectionButton.Click += ResetSelection_Click;

            // Bouton de détection de sténose
            detectStenosisButton = new Button
            {
                Dock = DockStyle.Bottom,
                Text = "Localiser Sténose",
                AutoSize = true,
                Margin = new Padding(10, 5, 10, 5),
                Height = 30,
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Enabled = true
            };
            detectStenosisButton.Click += DetectStenosisButton_Click;

            buttonPanel.Controls.Add(resetSelectionButton);
            buttonPanel.Controls.Add(detectStenosisButton);
            buttonPanel.Controls.Add(findNeckButton);
            buttonPanel.Controls.Add(findCarotidButton);
            buttonPanel.Controls.Add(renderButton);
            infoPanel.Controls.Add(buttonPanel);

            // Right top view panel
            var globalViewPanel = new Panel
            {
                Dock = DockStyle.Right,
                Width = 300,
                BackColor = SystemColors.Control
            };

            var globalTopViewPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 768 / 2
            };

            var globalViewPictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom
            };
            globalViewPictureBox.Image = displayManager.GetGlobalViewImage();
            globalTopViewPanel.Controls.Add(globalViewPictureBox);

            // Right control view panel
            var globalBottomViewPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 768 / 2,
            };

            var controlPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 768 / 4
            };

            windowWidthTrackBar = new TrackBar
            {
                Dock = DockStyle.Bottom,
                Minimum = 0,
                Maximum = 4000,
                Value = displayManager.windowWidth,
                TickStyle = TickStyle.TopLeft
            };
            windowWidthTrackBar.ValueChanged += TrackBar_ValueChanged;

            windowCenterTrackBar = new TrackBar
            {
                Dock = DockStyle.Bottom,
                Minimum = 0,
                Maximum = 800,
                Value = displayManager.windowCenter,
                TickStyle = TickStyle.TopLeft
            };
            windowCenterTrackBar.ValueChanged += TrackBar_ValueChanged;

            windowCenterLabel = new Label
            {
                Dock = DockStyle.Bottom,
                TextAlign = ContentAlignment.MiddleCenter,
                Height = 20,
                Text = "Window Center:" + displayManager.windowCenter
            };

            windowWidthLabel = new Label
            {
                Dock = DockStyle.Bottom,
                TextAlign = ContentAlignment.MiddleCenter,
                Height = 20,
                Text = "Window Width:" + displayManager.windowWidth
            };

            // Bouton d'optimisation des fenêtres
            optimizeWindowButton = new Button
            {
                Dock = DockStyle.Bottom,
                Text = "Optimiser Fenêtrage",
                Height = 30,
                Margin = new Padding(0, 5, 0, 5)
            };
            optimizeWindowButton.Click += OptimizeWindow_Click;

            controlPanel.Controls.AddRange(new Control[] {
                windowCenterLabel,
                windowCenterTrackBar,
                windowWidthLabel,
                windowWidthTrackBar,
                optimizeWindowButton
            });

            globalBottomViewPanel.Controls.Add(controlPanel);

            globalViewPanel.Controls.AddRange(new Control[] { globalTopViewPanel, globalBottomViewPanel });

            // Main content
            var contentPanel = new Panel
            {
                Dock = DockStyle.Fill
            };

            mainPictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom
            };

            sliceTrackBar = new TrackBar
            {
                Dock = DockStyle.Bottom,
                Minimum = 0,
                Maximum = displayManager.GetTotalSlices() - 1,
                TickStyle = TickStyle.TopLeft
            };
            sliceTrackBar.ValueChanged += TrackBar_ValueChanged;

            sliceLabel = new Label
            {
                Dock = DockStyle.Bottom,
                TextAlign = ContentAlignment.MiddleCenter,
                Height = 20
            };

            // Double slider min max
            doubleTrackBar = new DoubleTrackBar
            {
                Dock = DockStyle.Bottom,
                Minimum = 0,
                Maximum = displayManager.GetTotalSlices(),
                MinValue = 0,
                MaxValue = displayManager.GetTotalSlices(),
                TickStyle = TickStyle.TopLeft
            };
            doubleTrackBar.ValueChanged += DoubleTrackBar_ValueChanged;
            doubleTrackBar.MouseMove += DoubleTrackBar_MouseMove;
            doubleTrackBar.MouseUp += DoubleTrackBar_MouseUp;

            minLabel = new Label
            {
                Dock = DockStyle.Bottom,
                TextAlign = ContentAlignment.MiddleCenter,
                Height = 20,
                Text = "Min: 0"
            };

            maxLabel = new Label
            {
                Dock = DockStyle.Bottom,
                TextAlign = ContentAlignment.MiddleCenter,
                Height = 20,
                Text = $"Max: {displayManager.GetTotalSlices()}"
            };

            contentPanel.Controls.AddRange(new Control[] { mainPictureBox, sliceLabel, sliceTrackBar, doubleTrackBar, minLabel, maxLabel });

            this.Controls.AddRange(new Control[] { contentPanel, infoPanel, globalViewPanel });
            UpdateDisplay();
        }

        private void FindCarotidButton_Click(object sender, EventArgs e)
        {
            if (doubleTrackBar.MinValue >= doubleTrackBar.MaxValue)
            {
                MessageBox.Show("Veuillez d'abord localiser le cou ou sélectionner une plage de coupes valide.",
                                "Plage de coupes non définie",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                return;
            }

            // Réinitialiser le dessin manuel lorsqu'on utilise la localisation automatique
            isDrawing = false;
            startPoint = Point.Empty;
            endPoint = Point.Empty;

            LocalizeCarotidsSimplified();
        }

        private void LocalizeCarotidsSimplified()
        {
            try
            {
                Cursor = Cursors.WaitCursor;

                // Utiliser un fenêtrage optimisé pour les vaisseaux
                WindowPreset("Angiographie (Carotides)");

                // Fixer des proportions pour le rectangle (par rapport à la taille du PictureBox)
                double rectWidthPercent = 0.4;   // 40% de la largeur du PictureBox
                double rectHeightPercent = 0.3;  // 30% de la hauteur du PictureBox

                // Calculer la position et les dimensions du rectangle
                int rectWidth = (int)(mainPictureBox.ClientSize.Width * rectWidthPercent);
                int rectHeight = (int)(mainPictureBox.ClientSize.Height * rectHeightPercent);

                // Positionner le rectangle au centre exact du PictureBox
                int rectX = (mainPictureBox.ClientSize.Width - rectWidth) / 2;
                int rectY = (mainPictureBox.ClientSize.Height - rectHeight) / 2;

                // Stocker ces coordonnées dans l'espace d'affichage
                carotidDisplayRect = new Rectangle(rectX, rectY, rectWidth, rectHeight);

                // Calculer les coins du rectangle pour les afficher comme points de départ et d'arrivée
                Point startPoint = new Point(rectX, rectY);
                Point endPoint = new Point(rectX + rectWidth, rectY + rectHeight);

                // Convertir les coordonnées pour les afficher dans le format de l'image
                var startResized = ConvertToResizedCoordinates(startPoint);
                var endResized = ConvertToResizedCoordinates(endPoint);

                // Activer l'affichage
                showCarotidSelection = true;
                mainPictureBox.Invalidate();

                // Mise à jour des informations dans le même format que la sélection manuelle
                startPointLabel.Text = $"Start Point: ({startResized.X}, {startResized.Y})";
                endPointLabel.Text = $"End Point: ({endResized.X}, {endResized.Y})";
                areaLabel.Text = $"Area: {rectWidth * rectHeight}";

                MessageBox.Show("Région des carotides sélectionnée.",
                              "Localisation terminée", MessageBoxButtons.OK, MessageBoxIcon.Information);

                Cursor = Cursors.Default;
            }
            catch (Exception ex)
            {
                Cursor = Cursors.Default;
                MessageBox.Show($"Erreur: {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FindNeckButton_Click(object sender, EventArgs e)
        {
            FindNeckPosition();
        }

        private void FindNeckPosition()
        {
            try
            {
                int totalSlices = displayManager.GetTotalSlices();

                // Rechercher dans un intervalle de ±35% autour du milieu
                int centerSlice = totalSlices / 2;
                int searchRange = (int)(totalSlices * 0.35);
                int startSlice = Math.Max(0, centerSlice - searchRange);
                int endSlice = Math.Min(totalSlices - 1, centerSlice + searchRange);

                int bestSlice = centerSlice; // Position centrale du cou (par défaut)
                int neckTop = startSlice;    // Limite supérieure (début de la mâchoire)
                int neckBottom = endSlice;   // Limite inférieure (début des épaules)
                double maxEmptyRatio = 0;

                // Structure pour stocker les ratios de vide pour chaque slice
                Dictionary<int, double> sliceEmptyRatios = new Dictionary<int, double>();

                // Créer un indicateur de progression
                using (var progress = new Form())
                {
                    progress.StartPosition = FormStartPosition.CenterParent;
                    progress.FormBorderStyle = FormBorderStyle.FixedDialog;
                    progress.ControlBox = false;
                    progress.Text = "Recherche du cou en cours...";
                    progress.Size = new Size(300, 100);

                    var progressBar = new ProgressBar
                    {
                        Minimum = 0,
                        Maximum = endSlice - startSlice,
                        Value = 0,
                        Dock = DockStyle.Top,
                        Margin = new Padding(10)
                    };

                    var label = new Label
                    {
                        Text = "Analyse des coupes...",
                        Dock = DockStyle.Top,
                        TextAlign = ContentAlignment.MiddleCenter
                    };

                    progress.Controls.Add(progressBar);
                    progress.Controls.Add(label);

                    // Lancer l'analyse en arrière-plan
                    var task = Task.Run(() =>
                    {
                        for (int i = startSlice; i <= endSlice; i++)
                        {
                            int sliceIndex = i;
                            displayManager.SetSliceIndex(sliceIndex);

                            // Éviter l'accès inter-thread aux contrôles UI
                            int windowWidthValue = 0;
                            int windowCenterValue = 0;

                            this.Invoke((MethodInvoker)delegate {
                                windowWidthValue = windowWidthTrackBar.Value;
                                windowCenterValue = windowCenterTrackBar.Value;
                            });

                            // Puis utilisez ces valeurs dans le thread d'arrière-plan
                            displayManager.SetSliceIndex(sliceIndex);
                            var sliceImage = displayManager.GetCurrentSliceImage(windowWidthValue, windowCenterValue);

                            // Calculer le ratio de vide (pixels noirs ou presque noirs)
                            double emptyRatio = CalculateEmptyRatio(sliceImage);
                            sliceEmptyRatios[sliceIndex] = emptyRatio;

                            // Mise à jour UI
                            this.Invoke((MethodInvoker)delegate {
                                progressBar.Value = sliceIndex - startSlice;
                                label.Text = $"Analyse coupe {sliceIndex}/{endSlice}";
                            });

                            // Si cette slice a plus de vide (cou plus fin), la mémoriser
                            if (emptyRatio > maxEmptyRatio)
                            {
                                maxEmptyRatio = emptyRatio;
                                bestSlice = sliceIndex;
                            }
                        }

                        // Maintenant, déterminer les limites du cou
                        // Utiliser un seuil pour détecter des changements significatifs dans le ratio de vide
                        double thresholdMultiplier = 0.9; // 90% du ratio maximum
                        double threshold = maxEmptyRatio * thresholdMultiplier;

                        // Remonter depuis le cou pour trouver la limite supérieure (mâchoire)
                        neckTop = bestSlice;
                        for (int i = bestSlice; i >= startSlice; i--)
                        {
                            if (sliceEmptyRatios[i] < threshold)
                            {
                                neckTop = i + 1; // Prendre la slice juste avant la chute du ratio
                                break;
                            }
                        }

                        // Descendre depuis le cou pour trouver la limite inférieure (épaules)
                        neckBottom = bestSlice;
                        for (int i = bestSlice; i <= endSlice; i++)
                        {
                            if (sliceEmptyRatios[i] < threshold)
                            {
                                neckBottom = i - 1; // Prendre la slice juste avant la chute du ratio
                                break;
                            }
                        }

                        // Fermer la fenêtre de progression
                        this.Invoke((MethodInvoker)delegate {
                            progress.Close();
                        });
                    });

                    progress.ShowDialog();
                }

                // Mettre à jour le curseur du slicer et les sliders min/max
                this.Invoke((MethodInvoker)delegate {
                    // Positionner le slicer principal sur le milieu du cou
                    sliceTrackBar.Value = bestSlice;

                    // Mettre à jour les sliders min/max pour délimiter la zone du cou
                    doubleTrackBar.MinValue = neckTop;
                    doubleTrackBar.MaxValue = neckBottom;

                    // Mettre à jour les labels
                    minLabel.Text = "Min:" + neckTop;
                    maxLabel.Text = "Max:" + neckBottom;

                    // Afficher un message de confirmation avec les coordonnées complètes
                    MessageBox.Show($"Zone du cou identifiée :\n" +
                                   $"- Haut (mâchoire) : coupe {neckTop + 1}\n" +
                                   $"- Centre du cou : coupe {bestSlice + 1}\n" +
                                   $"- Bas (épaules) : coupe {neckBottom + 1}",
                                   "Localisation terminée",
                                   MessageBoxButtons.OK,
                                   MessageBoxIcon.Information);
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la recherche du cou: {ex.Message}",
                               "Erreur",
                               MessageBoxButtons.OK,
                               MessageBoxIcon.Error);
            }
        }

        private double CalculateEmptyRatio(Bitmap image)
        {
            // Seuil de luminosité en dessous duquel un pixel est considéré comme "vide"
            const int EMPTY_THRESHOLD = 30;

            int emptyPixels = 0;
            int totalPixels = 0;

            using (Bitmap bmp = new Bitmap(image))
            {
                // Échantillonnage pour accélérer le calcul (1 pixel sur 5)
                for (int y = 0; y < bmp.Height; y += 5)
                {
                    for (int x = 0; x < bmp.Width; x += 5)
                    {
                        Color pixel = bmp.GetPixel(x, y);
                        int brightness = (pixel.R + pixel.G + pixel.B) / 3;

                        if (brightness <= EMPTY_THRESHOLD)
                        {
                            emptyPixels++;
                        }

                        totalPixels++;
                    }
                }
            }

            return (double)emptyPixels / totalPixels;
        }

        private void OptimizeWindow_Click(object sender, EventArgs e)
        {
            // Afficher les préréglages au lieu de l'optimisation automatique
            ShowWindowPresetsMenu();
        }

        private void ShowWindowPresetsMenu()
        {
            var presetsMenu = new ContextMenuStrip();

            // Préréglage spécifique pour carotides
            AddPresetMenuItem(presetsMenu, "Angiographie (Carotides)", 300, 120);

            // Préréglages pour différents types de tissus
            AddPresetMenuItem(presetsMenu, "Tissus mous du cou", 350, 70);
            AddPresetMenuItem(presetsMenu, "Cerveau", 80, 40);
            AddPresetMenuItem(presetsMenu, "Poumon", 1500, -600);
            AddPresetMenuItem(presetsMenu, "Os", 2500, 480);
            AddPresetMenuItem(presetsMenu, "Contraste standard", 400, 50);

            // Ajouter l'option d'optimisation automatique
            var autoItem = new ToolStripMenuItem("Optimisation automatique");
            autoItem.Click += (s, e) => OptimizeWindowSettings();
            presetsMenu.Items.Add(autoItem);

            // Afficher le menu contextuel à côté du bouton
            presetsMenu.Show(optimizeWindowButton, new Point(0, optimizeWindowButton.Height));
        }

        private void AddPresetMenuItem(ContextMenuStrip menu, string name, int width, int center)
        {
            var item = new ToolStripMenuItem(name);
            item.Click += (s, e) =>
            {
                windowWidthTrackBar.Value = Math.Min(windowWidthTrackBar.Maximum, Math.Max(windowWidthTrackBar.Minimum, width));
                windowCenterTrackBar.Value = Math.Min(windowCenterTrackBar.Maximum, Math.Max(windowCenterTrackBar.Minimum, center));
                UpdateDisplay();
            };
            menu.Items.Add(item);
        }

        private void WindowPreset(string presetName)
        {
            // Appliquer un préréglage de fenêtrage
            switch (presetName)
            {
                case "Angiographie (Carotides)":
                    windowWidthTrackBar.Value = Math.Min(windowWidthTrackBar.Maximum, Math.Max(windowWidthTrackBar.Minimum, 300));
                    windowCenterTrackBar.Value = Math.Min(windowCenterTrackBar.Maximum, Math.Max(windowCenterTrackBar.Minimum, 120));
                    break;
                case "Tissus mous du cou":
                    windowWidthTrackBar.Value = Math.Min(windowWidthTrackBar.Maximum, Math.Max(windowWidthTrackBar.Minimum, 350));
                    windowCenterTrackBar.Value = Math.Min(windowCenterTrackBar.Maximum, Math.Max(windowCenterTrackBar.Minimum, 70));
                    break;
                    // Autres préréglages au besoin
            }
            UpdateDisplay();
        }

        private void OptimizeWindowSettings()
        {
            try
            {
                // Récupérer les données de pixel de l'image actuelle
                var currentImage = displayManager.GetCurrentSliceImage(4000, 400); // Utiliser une fenêtre large pour l'analyse
                using (var bitmap = new Bitmap(currentImage))
                {
                    int[] histogram = new int[4096]; // Pour les valeurs HU typiques
                    int minValue = 4095;
                    int maxValue = 0;
                    int totalPixels = 0;

                    // Calculer l'histogramme et trouver les valeurs min/max significatives
                    for (int y = 0; y < bitmap.Height; y++)
                    {
                        for (int x = 0; x < bitmap.Width; x++)
                        {
                            var pixel = bitmap.GetPixel(x, y);
                            int intensity = (pixel.R + pixel.G + pixel.B) / 3; // Niveau de gris moyen

                            if (intensity > 0) // Ignorer les pixels noirs
                            {
                                histogram[intensity < 4096 ? intensity : 4095]++;
                                totalPixels++;

                                if (intensity < minValue) minValue = intensity;
                                if (intensity > maxValue) maxValue = intensity;
                            }
                        }
                    }

                    // Ignorer les valeurs extrêmes (5% de chaque côté de l'histogramme)
                    int minPixelCount = (int)(totalPixels * 0.05);
                    int maxPixelCount = (int)(totalPixels * 0.95);
                    int pixelSum = 0;

                    int effectiveMin = 0;
                    int effectiveMax = 4095;

                    // Trouver les percentiles 5 et 95
                    for (int i = 0; i < histogram.Length; i++)
                    {
                        pixelSum += histogram[i];
                        if (pixelSum >= minPixelCount && effectiveMin == 0)
                        {
                            effectiveMin = i;
                        }
                        if (pixelSum >= maxPixelCount)
                        {
                            effectiveMax = i;
                            break;
                        }
                    }

                    // Calculer les nouveaux paramètres de fenêtre
                    int newWindowWidth = effectiveMax - effectiveMin;
                    int newWindowCenter = effectiveMin + (newWindowWidth / 2);

                    // Appliquer des limites raisonnables
                    newWindowWidth = Math.Max(50, Math.Min(windowWidthTrackBar.Maximum, newWindowWidth));
                    newWindowCenter = Math.Max(0, Math.Min(windowCenterTrackBar.Maximum, newWindowCenter));

                    // Mettre à jour les trackbars
                    windowWidthTrackBar.Value = newWindowWidth;
                    windowCenterTrackBar.Value = newWindowCenter;

                    // Mettre à jour l'affichage
                    UpdateDisplay();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'optimisation: {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ResetSelection_Click(object sender, EventArgs e)
        {
            // Reset neck slice locator
            doubleTrackBar.MinValue = 0;
            doubleTrackBar.MaxValue = displayManager.GetTotalSlices();
            minLabel.Text = "Min: 0";
            maxLabel.Text = $"Max: {displayManager.GetTotalSlices()}";

            // Reset carotid selection
            showCarotidSelection = false;
            carotidDisplayRect = Rectangle.Empty;
            startPoint = Point.Empty;
            endPoint = Point.Empty;
            startPointLabel.Text = "Start Point: (0, 0)";
            endPointLabel.Text = "End Point: (0, 0)";
            areaLabel.Text = "Area: 0";

            // Redraw the picture box to clear any selection rectangle
            mainPictureBox.Invalidate();

            MessageBox.Show("Toutes les sélections ont été réinitialisées.", "Réinitialisation terminée",
                           MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Button_Click(object sender, EventArgs e)
        {
            // Récupérer le chemin du dossier patient
            string basePath = Path.GetDirectoryName(displayManager.DirectoryPath);

            // Récupérer le nom du dossier actuel (le nom du scan)
            string scanFolderName = new DirectoryInfo(displayManager.DirectoryPath).Name;

            // Combiner le chemin avec le nom du dossier
            string fullPath = Path.Combine(basePath, scanFolderName);

            Rectangle? carotidRect = null;

            if (carotidDisplayRect != Rectangle.Empty)
            {
                // Always convert display coordinates to image coordinates
                var topLeft = ConvertToResizedCoordinates(new Point(carotidDisplayRect.X, carotidDisplayRect.Y));
                var bottomRight = ConvertToResizedCoordinates(new Point(carotidDisplayRect.Right, carotidDisplayRect.Bottom));

                if (topLeft != Point.Empty && bottomRight != Point.Empty)
                {
                    carotidRect = new Rectangle(
                        topLeft.X,
                        topLeft.Y,
                        bottomRight.X - topLeft.X,
                        bottomRight.Y - topLeft.Y
                    );

                    Debug.WriteLine("Carotid Rect (image coordinates):" + carotidRect);
                }
            }

            var renderForm = new RenderDicomForm(displayManager, doubleTrackBar.MinValue, doubleTrackBar.MaxValue, fullPath, carotidRect);
            renderForm.Show();
        }

        private void TrackBar_ValueChanged(object sender, EventArgs e)
        {
            displayManager.SetSliceIndex(sliceTrackBar.Value);
            UpdateDisplay();
        }

        private void AddInfoRow(TableLayoutPanel table, string label, string value)
        {
            var labelControl = new Label
            {
                Text = label,
                AutoSize = true,
                Margin = new Padding(2),
                Font = new Font(Font.FontFamily, 9, FontStyle.Bold)
            };

            var valueControl = new Label
            {
                Text = value,
                AutoSize = true,
                Margin = new Padding(2)
            };

            table.Controls.Add(labelControl);
            table.Controls.Add(valueControl);
        }

        private void UpdateDisplay()
        {
            mainPictureBox.Image?.Dispose();
            var originalImage = displayManager.GetCurrentSliceImage(windowWidthTrackBar.Value, windowCenterTrackBar.Value);

            var resizedImage = new Bitmap(TARGET_SIZE, TARGET_SIZE);
            using (var g = Graphics.FromImage(resizedImage))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(originalImage, 0, 0, TARGET_SIZE, TARGET_SIZE);
            }

            mainPictureBox.Image = resizedImage;
            sliceLabel.Text = $"Slice {displayManager.GetCurrentSliceIndex() + 1} of {displayManager.GetTotalSlices()}";
            windowCenterLabel.Text = "Window Center:" + windowCenterTrackBar.Value;
            windowWidthLabel.Text = "Window Width:" + windowWidthTrackBar.Value;
        }

        private Point ConvertToResizedCoordinates(Point clickPoint)
        {
            var displayedSize = GetDisplayedImageSize();
            var picBox = mainPictureBox;

            int offsetX = (picBox.ClientSize.Width - displayedSize.Width) / 2;
            int offsetY = (picBox.ClientSize.Height - displayedSize.Height) / 2;

            clickPoint.X -= offsetX;
            clickPoint.Y -= offsetY;

            if (clickPoint.X < 0 || clickPoint.Y < 0 ||
                clickPoint.X > displayedSize.Width || clickPoint.Y > displayedSize.Height)
                return Point.Empty;

            float scaleX = (float)TARGET_SIZE / displayedSize.Width;
            float scaleY = (float)TARGET_SIZE / displayedSize.Height;

            return new Point(
                (int)(clickPoint.X * scaleX),
                (int)(clickPoint.Y * scaleY)
            );
        }

        private Rectangle GetResizedRectangle(Rectangle originalRect)
        {
            var p1 = ConvertToResizedCoordinates(new Point(originalRect.X, originalRect.Y));
            var p2 = ConvertToResizedCoordinates(new Point(originalRect.Right, originalRect.Bottom));

            if (p1 == Point.Empty || p2 == Point.Empty)
                return Rectangle.Empty;

            return new Rectangle(
                Math.Min(p1.X, p2.X),
                Math.Min(p1.Y, p2.Y),
                Math.Abs(p2.X - p1.X),
                Math.Abs(p2.Y - p1.Y)
            );
        }

        private void DoubleTrackBar_MouseUp(object sender, MouseEventArgs e)
        {
            minLabel.Text = "Min:" + doubleTrackBar.MinValue;
            maxLabel.Text = "Max:" + doubleTrackBar.MaxValue;
        }

        private void DoubleTrackBar_MouseMove(object sender, MouseEventArgs e)
        {
            minLabel.Text = "Min:" + doubleTrackBar.MinValue;
            maxLabel.Text = "Max:" + doubleTrackBar.MaxValue;
        }

        private void DoubleTrackBar_ValueChanged(object sender, EventArgs e)
        {
            minLabel.Text = "Min:" + doubleTrackBar.MinValue;
            maxLabel.Text = "Max:" + doubleTrackBar.MaxValue;
        }

        private void MainPictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // Désactiver l'affichage de la sélection des carotides quand on commence un dessin manuel
                showCarotidSelection = false;

                isDrawing = true;
                startPoint = e.Location;
                var resizedPoint = ConvertToResizedCoordinates(startPoint);
                if (resizedPoint != Point.Empty)
                {
                    startPointLabel.Text = $"Start Point: ({resizedPoint.X}, {resizedPoint.Y})";
                }
                mainPictureBox.Invalidate();
            }
        }

        private void MainPictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDrawing)
            {
                endPoint = e.Location;
                var resizedPoint = ConvertToResizedCoordinates(endPoint);
                if (resizedPoint != Point.Empty)
                {
                    endPointLabel.Text = $"End Point: ({resizedPoint.X}, {resizedPoint.Y})";
                    var resizedRect = GetResizedRectangle(GetRectangle(startPoint, endPoint));
                    if (resizedRect != Rectangle.Empty)
                    {
                        areaLabel.Text = $"Area: {resizedRect.Width * resizedRect.Height}";
                    }
                }
                mainPictureBox.Invalidate();
            }
        }

        private void MainPictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDrawing = false;
                endPoint = e.Location;
                var resizedPoint = ConvertToResizedCoordinates(endPoint);
                if (resizedPoint != Point.Empty)
                {
                    endPointLabel.Text = $"End Point: ({resizedPoint.X}, {resizedPoint.Y})";

                    // Get display rectangle (for drawing)
                    var displayRect = GetRectangle(startPoint, endPoint);

                    // Get resized rectangle (for coordinates in image space)
                    var resizedRect = GetResizedRectangle(displayRect);

                    if (resizedRect != Rectangle.Empty)
                    {
                        areaLabel.Text = $"Area: {resizedRect.Width * resizedRect.Height}";

                        // Store the display rectangle for later use
                        carotidDisplayRect = displayRect;

                        // IMPORTANT: Enable the selection for 3D rendering
                        showCarotidSelection = true;

                        Debug.WriteLine($"Manual rectangle: Display={displayRect}, Image={resizedRect}");
                    }
                }
                mainPictureBox.Invalidate();
            }
        }

        private void MainPictureBox_Paint(object sender, PaintEventArgs e)
        {
            // Dessiner le rectangle de sélection manuel (si en cours de dessin)
            if (isDrawing || startPoint != endPoint)
            {
                var rect = GetRectangle(startPoint, endPoint);
                e.Graphics.DrawRectangle(Pens.Red, rect);
            }

            // Dessiner le rectangle des carotides si activé
            if (showCarotidSelection)
            {
                // Dessiner directement le rectangle avec les coordonnées d'affichage
                using (Pen redPen = new Pen(Color.Red, 2))
                {
                    e.Graphics.DrawRectangle(redPen, carotidDisplayRect);

                    // Calculer les coordonnées des points marqueurs
                    int centerX = carotidDisplayRect.X + carotidDisplayRect.Width / 2;
                    int centerY = carotidDisplayRect.Y + carotidDisplayRect.Height / 2;

                    // Placer les points à 1/4 et 3/4 de la largeur
                    int leftX = carotidDisplayRect.X + carotidDisplayRect.Width / 4;
                    int rightX = carotidDisplayRect.X + carotidDisplayRect.Width * 3 / 4;

                    // Dessiner les points
                    e.Graphics.FillEllipse(Brushes.Red, leftX - 3, centerY - 3, 6, 6);
                    e.Graphics.FillEllipse(Brushes.Red, rightX - 3, centerY - 3, 6, 6);
                }
            }
        }

        private Rectangle GetRectangle(Point p1, Point p2)
        {
            return new Rectangle(
                Math.Min(p1.X, p2.X),
                Math.Min(p1.Y, p2.Y),
                Math.Abs(p1.X - p2.X),
                Math.Abs(p1.Y - p2.Y));
        }

        private Size GetDisplayedImageSize()
        {
            if (mainPictureBox.Image == null) return Size.Empty;

            var image = mainPictureBox.Image;
            var picBox = mainPictureBox;

            float imageRatio = (float)image.Width / image.Height;
            float containerRatio = (float)picBox.ClientSize.Width / picBox.ClientSize.Height;

            if (imageRatio > containerRatio)
            {
                return new Size(
                    picBox.ClientSize.Width,
                    (int)(picBox.ClientSize.Width / imageRatio)
                );
            }
            else
            {
                return new Size(
                    (int)(picBox.ClientSize.Height * imageRatio),
                    picBox.ClientSize.Height
                );
            }
        }
        private async void DetectStenosisButton_Click(object sender, EventArgs e)
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                detectStenosisButton.Enabled = false;
                detectStenosisButton.Text = "Analyse en cours...";
                Application.DoEvents();

                using (var apiClient = new StenosisApiClient("http://localhost:5000"))
                {
                    var isHealthy = await apiClient.CheckHealthAsync();
                    if (!isHealthy)
                    {
                        MessageBox.Show(
                            "[ERREUR] L'API de détection de sténose n'est pas accessible.\n\n" +
                            "Pour démarrer l'API:\n1. Ouvrir un terminal dans StenoseDetection_U-Net\n2. Exécuter: START_API.bat",
                            "API Non Disponible", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // Étape 1: Vérifier si le cou a été localisé
                    bool neckDetected = IsNeckDetected();
                    
                    if (!neckDetected)
                    {
                        // Le cou n'est pas encore localisé, le détecter automatiquement
                        detectStenosisButton.Text = "Detection du cou...";
                        Application.DoEvents();
                        
                        try
                        {
                            FindNeckPosition();
                            
                            // Vérifier si la détection a réussi
                            if (!IsNeckDetected())
                            {
                                MessageBox.Show(
                                    "Impossible de localiser automatiquement le cou.\n\n" +
                                    "Veuillez localiser manuellement le cou avec le bouton 'Localiser le cou',\n" +
                                    "puis réessayez.",
                                    "Detection du Cou Echouee",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Warning);
                                return;
                            }
                            
                            int neckSlices = doubleTrackBar.MaxValue - doubleTrackBar.MinValue + 1;
                            MessageBox.Show(
                                $"Cou detecte automatiquement\n\n" +
                                $"Zone: Slice {doubleTrackBar.MinValue} a {doubleTrackBar.MaxValue}\n" +
                                $"Total: {neckSlices} slices\n\n" +
                                $"L'analyse va maintenant commencer sur cette zone.",
                                "Detection du Cou Reussie",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(
                                $"Erreur lors de la detection du cou:\n\n{ex.Message}\n\n" +
                                "Veuillez localiser manuellement le cou.",
                                "Erreur",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                            return;
                        }
                    }
                    
                    // Étape 2: Collecter les images de la zone du cou
                    detectStenosisButton.Text = "Collecte des images...";
                    Application.DoEvents();
                    
                    // Obtenir le centre du cou
                    int centerUsed = sliceTrackBar.Value;
                    
                    // Vérifier si on a le chemin du dossier DICOM
                    if (string.IsNullOrEmpty(currentDicomFolderPath))
                    {
                        MessageBox.Show(
                            "ATTENTION: Chemin DICOM non disponible.\n\n" +
                            "Le système va utiliser les images en mémoire (méthode classique).\n" +
                            "Pour utiliser la méthode optimisée, assurez-vous que le chemin DICOM est stocké.",
                            "Mode Classique",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                        
                        // Méthode classique : collecter et envoyer les images
                        var images = CollectAllPatientImages();
                        if (images.Count == 0)
                        {
                            MessageBox.Show("Aucune image à traiter.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        var progressForm = CreateProgressForm(images.Count);
                        progressForm.Show();
                        Application.DoEvents();

                        var result = await apiClient.DetectStenosisAsync(images);
                        progressForm.Close();

                        if (result.Success)
                        {
                            ShowStenosisResults(result);
                        }
                        else
                        {
                            MessageBox.Show($"[ERREUR] Erreur:\n\n{result.Error}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        return;
                    }
                    
                    // NOUVELLE MÉTHODE OPTIMISÉE : Envoyer juste le chemin + centre
                    MessageBox.Show(
                        $"MODE OPTIMISÉ\n\n" +
                        $"Centre du cou: slice {centerUsed}\n" +
                        $"Dossier DICOM: {currentDicomFolderPath}\n\n" +
                        $"Python va charger les slices {centerUsed - 30} à {centerUsed + 30}",
                        "Détection Optimisée",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    
                    detectStenosisButton.Text = "Analyse en cours...";
                    Application.DoEvents();
                    
                    var resultOptimized = await apiClient.DetectStenosisFromCenterAsync(currentDicomFolderPath, centerUsed);

                    if (resultOptimized.Success)
                    {
                        ShowStenosisResults(resultOptimized);
                    }
                    else
                    {
                        MessageBox.Show($"[ERREUR] Erreur:\n\n{resultOptimized.Error}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show($"Erreur de connexion:\n\n{ex.Message}", "Erreur Réseau", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"[ERREUR] Erreur:\n\n{ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
                detectStenosisButton.Enabled = true;
                detectStenosisButton.Text = "Localiser Sténose";
            }
        }
        /// <summary>
        /// Vérifie si le cou a été détecté (zone sélectionnée valide)
        /// </summary>
        private bool IsNeckDetected()
        {
            int minValue = doubleTrackBar.MinValue;
            int maxValue = doubleTrackBar.MaxValue;
            int totalSlices = displayManager.GetTotalSlices();
            
            // Le cou est considéré comme détecté si:
            // 1. MinValue et MaxValue sont différents
            // 2. La zone n'est pas toute la série (trop large)
            // 3. La zone a une taille raisonnable (au moins 50 slices, max 400 slices)
            
            if (minValue >= maxValue)
                return false;
            
            int rangeSize = maxValue - minValue + 1;
            
            // Si la zone = toute la série, le cou n'est pas détecté
            if (rangeSize >= totalSlices - 10)
                return false;
            
            // Zone trop petite ou trop grande
            if (rangeSize < 50 || rangeSize > 400)
                return false;
            
            return true;
        }



        private List<Bitmap> CollectAllPatientImages()
        {
            var images = new List<Bitmap>();
            
            // Utiliser le centre du cou (sliceTrackBar.Value) comme point de référence
            int centerSlice = sliceTrackBar.Value;
            int totalSlices = displayManager.GetTotalSlices();
            
            // Collecter 30 slices au-dessus du centre
            int startAbove = Math.Max(0, centerSlice - 30);
            for (int i = startAbove; i < centerSlice && i < totalSlices; i++)
            {
                displayManager.SetSliceIndex(i);
                images.Add(new Bitmap(displayManager.GetCurrentSliceImage(windowWidthTrackBar.Value, windowCenterTrackBar.Value)));
            }
            
            // Ajouter le centre
            displayManager.SetSliceIndex(centerSlice);
            images.Add(new Bitmap(displayManager.GetCurrentSliceImage(windowWidthTrackBar.Value, windowCenterTrackBar.Value)));
            
            // Collecter 30 slices en dessous du centre
            int endBelow = Math.Min(totalSlices - 1, centerSlice + 30);
            for (int i = centerSlice + 1; i <= endBelow; i++)
            {
                displayManager.SetSliceIndex(i);
                images.Add(new Bitmap(displayManager.GetCurrentSliceImage(windowWidthTrackBar.Value, windowCenterTrackBar.Value)));
            }
            
            return images;
        }

        private Form CreateProgressForm(int imageCount)
        {
            var progressForm = new Form
            {
                Text = "Détection de sténose",
                Size = new Size(450, 180),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                ControlBox = false
            };
            var titleLabel = new Label
            {
                Text = "Analyse par Intelligence Artificielle",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 40,
                TextAlign = ContentAlignment.MiddleCenter
            };
            var messageLabel = new Label
            {
                Text = $"Traitement de {imageCount} images...\nVeuillez patienter.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            progressForm.Controls.Add(titleLabel);
            progressForm.Controls.Add(messageLabel);
            return progressForm;
        }

        private void ShowStenosisResults(StenosisDetectionResult result)
        {
            var severity = GetStenosisSeverity(result.StenosisLeftPercent, result.StenosisRightPercent);
            var message = $"RESULTATS\n\nImages: {result.ProcessedImages}\n\nCarotide GAUCHE: {result.StenosisLeftPercent:F2}%\nCarotide DROITE: {result.StenosisRightPercent:F2}%\n\n{GetSeverityMessage(severity)}";
            var resultForm = new Form { Text = "Résultats Sténose", Size = new Size(500, 400), StartPosition = FormStartPosition.CenterParent };
            var textBox = new TextBox { Text = message, Multiline = true, ReadOnly = true, Dock = DockStyle.Fill, Font = new Font("Consolas", 10) };
            var closeButton = new Button { Text = "Fermer", Dock = DockStyle.Bottom, Height = 40 };
            closeButton.Click += (s, e) => resultForm.Close();
            resultForm.Controls.Add(textBox);
            resultForm.Controls.Add(closeButton);
            resultForm.ShowDialog();
        }

        private StenosisSeverity GetStenosisSeverity(double left, double right)
        {
            var max = Math.Max(left, right);
            if (max < 30) return StenosisSeverity.Mild;
            if (max < 50) return StenosisSeverity.Moderate;
            if (max < 70) return StenosisSeverity.Significant;
            return StenosisSeverity.Severe;
        }

        private string GetSeverityMessage(StenosisSeverity severity)
        {
            return severity switch
            {
                StenosisSeverity.Mild => "LEGERE - Surveillance recommandee",
                StenosisSeverity.Moderate => "MODEREE - Consultation medicale",
                StenosisSeverity.Significant => "SIGNIFICATIVE - Intervention possible",
                StenosisSeverity.Severe => "SEVERE - Urgence medicale",
                _ => "Analyse terminée"
            };
        }

    }

    public enum StenosisSeverity { Mild, Moderate, Significant, Severe }
}