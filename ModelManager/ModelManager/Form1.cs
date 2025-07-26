using System.Drawing;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ModelManager
{
    public partial class Form1 : Form
    {
        private List<ModelInfo> models = new();
        private List<ModelInfo> filteredModels = new();
        private Size modelItemSize = new Size(128, 170);
        private Size thumbnailSize = new Size(128, 128);
        private Dictionary<string, Image> thumbnailCache = new();
        private ContextMenuStrip modelContextMenu;
        private ModelInfo? contextModel;
        private ModelInfo? currentModel;
        private System.Windows.Forms.Timer trackbarTimer;     

        private static readonly string NoInfoMarker = "{\"no_model_info\":true}";

        private enum ModelSortOrder { FilenameAsc, FilenameDesc, DateAsc, DateDesc, SizeAsc, SizeDesc }
        private ModelSortOrder currentSortOrder = ModelSortOrder.FilenameAsc;

        public Form1()
        {
            InitializeComponent();
            InitializeModelContextMenu();
            // Setup timer for trackbar
            trackbarTimer = new System.Windows.Forms.Timer();
            trackbarTimer.Interval = 400; // ms          
            trackbarTimer.Tick += (s, e) =>
            {
                trackbarTimer.Stop();
                int thumbW = trackModelScale.Value;
                thumbnailSize = new Size(thumbW, thumbW);
                modelItemSize = new Size(thumbW + 12, thumbW + 42);
                UpdateModelItemSizes();
            };
            trackModelScale.Scroll += (s, e) => {
                trackbarTimer.Stop();
                trackbarTimer.Start();
            };
            trackModelScale.Value = 260;
            trackbarTimer.Stop();
            trackbarTimer.Start();

            // Model name filter textbox           
            txtModelNameFilter.PlaceholderText = "Model name";
            txtModelNameFilter.TextChanged += (s, e) => ApplyFilters();           

            // Remove btnFilter from panelTop and Form1 controls
            // Add event handlers to filter controls
            cmbBaseModel.SelectedIndexChanged += (s, e) => ApplyFilters();
            dtpCreatedAfter.ValueChanged += (s, e) => ApplyFilters();
            chkSearchSubfolders.CheckedChanged += (s, e) => ApplyFilters();
            // Ensure controls are parented to panelTop
            panelTop.Controls.Add(btnSelectFolder);
            panelTop.Controls.Add(chkSearchSubfolders);
            panelTop.Controls.Add(dtpCreatedAfter);
            panelTop.Controls.Add(cmbBaseModel);
            panelTop.Controls.Add(trackModelScale);          

            groupModelInfo.Resize += (s, e) => ShowModelInfo(currentModel);
        }

        private async void btnSelectFolder_Click(object sender, EventArgs e)
        {
            using var fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                await LoadModelsWithDialogAsync(fbd.SelectedPath);
            }
        }

        //protected override void OnResizeEnd(EventArgs e)
        //{
        //    base.OnResizeEnd(e);
        //    // Calculate new thumbnail size based on window size
        //    int itemsPerRow = Math.Max(1, flowModels.Width / 160);
        //    int thumbW = Math.Max(64, Math.Min(256, flowModels.Width / itemsPerRow - 12));
        //    thumbnailSize = new Size(thumbW, thumbW);
        //    modelItemSize = new Size(thumbW + 12, thumbW + 42);
        //    UpdateModelItemSizes();
        //}

        private void UpdateModelItemSizes()
        {
            foreach (Panel panel in flowModels.Controls)
            {
                panel.Width = modelItemSize.Width;
                panel.Height = modelItemSize.Height;
                foreach (Control c in panel.Controls)
                {
                    if (c is PictureBox pb)
                    {
                        pb.Width = thumbnailSize.Width;
                        pb.Height = thumbnailSize.Height;
                        if (panel.Tag is ModelInfo model && model.ThumbnailPath != null)
                        {
                            string cacheKey = model.ThumbnailPath + $"_{thumbnailSize.Width}x{thumbnailSize.Height}";
                            if (!thumbnailCache.TryGetValue(cacheKey, out var cachedImg))
                            {
                                try
                                {
                                    using var img = Image.FromFile(model.ThumbnailPath);
                                    cachedImg = FitImageToBox(img, thumbnailSize.Width, thumbnailSize.Height);
                                    thumbnailCache[cacheKey] = cachedImg;
                                }
                                catch { cachedImg = null; }
                            }
                            pb.Image = cachedImg;
                        }
                    }
                }
            }
        }

        private void ApplyFilters()
        {
            filteredModels.Clear();
            flowModels.Controls.Clear();
            DateTime createdAfter = dtpCreatedAfter.Value.Date;
            string baseModel = cmbBaseModel.SelectedItem?.ToString() ?? "All";
            string nameFilter = txtModelNameFilter.Text.Trim().ToLowerInvariant();
            foreach (var model in models)
            {
                if (model.Metadata == null) continue;
                // Filter by createdAt
                if (model.Metadata.TryGetValue("createdAt", out var createdAtObj) && DateTime.TryParse(createdAtObj?.ToString(), out var createdAt))
                {
                    if (createdAt < createdAfter)
                        continue;
                }
                // Filter by baseModel
                if (baseModel != "All")
                {
                    string? modelBaseModel = null;
                    if (model.Metadata.TryGetValue("baseModel", out var bmObj))
                    {
                        if (bmObj is JsonElement bmElem && bmElem.ValueKind == JsonValueKind.String)
                            modelBaseModel = bmElem.GetString();
                        else
                            modelBaseModel = bmObj?.ToString();
                    }
                    if (!string.Equals(modelBaseModel, baseModel, StringComparison.OrdinalIgnoreCase))
                        continue;
                }
                // Filter by model name (from metadata or fallback to file name)
                string displayName = model.Metadata.TryGetValue("name", out var metaName) && metaName is string s && !string.IsNullOrEmpty(s)
                    ? s
                    : model.Name;
                if (!string.IsNullOrEmpty(nameFilter) && !displayName.ToLowerInvariant().Contains(nameFilter))
                    continue;
                filteredModels.Add(model);
            }
            DisplayModels(filteredModels);
        }

        private void DisplayModels(List<ModelInfo> modelsToShow)
        {
            flowModels.Controls.Clear();
            foreach (var model in modelsToShow)
            {
                Image? thumb = null;
                if (model.ThumbnailPath != null)
                {
                    string cacheKey = model.ThumbnailPath + $"_{thumbnailSize.Width}x{thumbnailSize.Height}";
                    if (!thumbnailCache.TryGetValue(cacheKey, out thumb))
                    {
                        try
                        {
                            using var img = Image.FromFile(model.ThumbnailPath);
                            thumb = FitImageToBox(img, thumbnailSize.Width, thumbnailSize.Height);
                            thumbnailCache[cacheKey] = thumb;
                        }
                        catch { thumb = null; }
                    }
                }               
                var panel = new Panel
                {
                    Width = modelItemSize.Width,
                    Height = modelItemSize.Height,
                    Margin = new Padding(8),
                    Tag = model
                };
                var pb = new PictureBox
                {
                    Width = thumbnailSize.Width,
                    Height = thumbnailSize.Height,
                    SizeMode = PictureBoxSizeMode.CenterImage,
                    Image = thumb,
                    Cursor = Cursors.Hand,
                    Top = 0,
                    Left = 6
                };
                pb.Click += (s, e) => ShowModelInfo(model);                
                pb.MouseUp += (s, e) =>
                {
                    if (e.Button == MouseButtons.Right)
                    {
                        contextModel = model;
                        modelContextMenu.Show(pb, e.Location);
                    }
                };
                var lbl = new Label
                {
                    Text = model.Name,
                    AutoSize = false,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Dock = DockStyle.Bottom,
                    Height = 32
                };
                panel.Controls.Add(pb);
                panel.Controls.Add(lbl);
                flowModels.Controls.Add(panel);
            }
        }

        private async Task<string?> GetModelInfoFromApiAsync(string filePath, string infoPath)
        {
            try
            {
                using var sha256 = SHA256.Create();
                using var stream = File.OpenRead(filePath);
                var hashBytes = await sha256.ComputeHashAsync(stream);
                var hash = BitConverter.ToString(hashBytes).Replace("-", string.Empty).ToLowerInvariant();
                using var client = new HttpClient();
                var url = $"https://civitai.com/api/v1/model-versions/by-hash/{hash}";
                var response = await client.GetAsync(url);
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    await File.WriteAllTextAsync(infoPath, NoInfoMarker);
                    return NoInfoMarker;
                }
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    await File.WriteAllTextAsync(infoPath, json);
                    return json;
                }
            }
            catch { }
            return null;
        }

        private async Task DownloadThumbnailFromModelInfoAsync(ModelInfo model, string pngPath)
        {
            if (model.Metadata == null) return;
            if (!model.Metadata.TryGetValue("images", out var imagesObj)) return;
            if (imagesObj is not JsonElement imagesElem || imagesElem.ValueKind != JsonValueKind.Array) return;
            // Iterate images and skip gif/video
            foreach (var imgElem in imagesElem.EnumerateArray())
            {
                if (imgElem.ValueKind != JsonValueKind.Object) continue;
                if (!imgElem.TryGetProperty("url", out var urlElem)) continue;
                var url = urlElem.GetString();
                if (string.IsNullOrEmpty(url)) continue;
                string urlLower = url.ToLowerInvariant();
                // Skip gif and video formats
                if (urlLower.EndsWith(".gif") || urlLower.EndsWith(".webm") || urlLower.EndsWith(".mp4") || urlLower.EndsWith(".mov") || urlLower.EndsWith(".avi") || urlLower.Contains("/gif") || urlLower.Contains("/video"))
                    continue;
                try
                {
                    using var client = new HttpClient();
                    var imgBytes = await client.GetByteArrayAsync(url);
                    await File.WriteAllBytesAsync(pngPath, imgBytes);
                    model.ThumbnailPath = pngPath;
                    break; // Only use the first valid image
                }
                catch { }
            }
        }

        string placeholderCard = "img\\placeholder_card.png";
        private async Task LoadModelsWithDialogAsync(string folderPath)
        {
            using var dialog = new ProgressDialog();
            dialog.ProgressBar.Minimum = 0;
            dialog.ProgressBar.Value = 0;
            dialog.StatusLabel.Text = "Initializing...";
            dialog.StartPosition = FormStartPosition.Manual;
            dialog.Location = GetCenterLocationForDialog(dialog);
            dialog.Show();
            dialog.BringToFront();
            dialog.TopMost = true;
            var cts = dialog.CancellationTokenSource;
            var modelExtensions = new[] { ".ckpt", ".safetensors", ".pt" };
            var searchOption = chkSearchSubfolders.Checked ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var files = Directory.GetFiles(folderPath, "*.*", searchOption);
            var modelFiles = files.Where(f => modelExtensions.Contains(Path.GetExtension(f).ToLowerInvariant())).ToList();
            int total = modelFiles.Count;
            dialog.ProgressBar.Maximum = total;
            models.Clear();
            flowModels.Controls.Clear();
            thumbnailCache.Clear();
            ClearModelInfo();
            int current = 0;
            int maxDegreeOfParallelism = 4; // Optimal for IO-bound tasks
            var semaphore = new SemaphoreSlim(maxDegreeOfParallelism);
            var tasks = new List<Task>();
            object progressLock = new object();
            foreach (var file in modelFiles)
            {
                await semaphore.WaitAsync();
                if (dialog.CancellationTokenSource.IsCancellationRequested)
                {
                    semaphore.Release();
                    break;
                }
                var task = Task.Run(async () =>
                {
                    try
                    {
                        var ext = Path.GetExtension(file).ToLowerInvariant();
                        var name = Path.GetFileNameWithoutExtension(file);
                        var infoPath = Path.Combine(Path.GetDirectoryName(file) ?? folderPath, name + ".civitai.info");
                        var pngPath = Path.Combine(Path.GetDirectoryName(file) ?? folderPath, name + ".preview.png");
                        var model = new ModelInfo
                        {
                            FilePath = file,
                            Name = name,
                            Type = ext == ".ckpt" ? "Checkpoint" : ext == ".safetensors" ? "LoRA/Safetensors" : "Other",
                            InfoJsonPath = File.Exists(infoPath) ? infoPath : null,
                            ThumbnailPath = File.Exists(pngPath) ? pngPath : placeholderCard
                        };
                        string? json = null;
                        if (model.InfoJsonPath != null)
                        {
                            try
                            {
                                json = await File.ReadAllTextAsync(model.InfoJsonPath);
                                if (json == NoInfoMarker)
                                {
                                    model.Metadata = null;
                                }
                                else
                                {
                                    model.Metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                                    if (!dialog.CancellationTokenSource.IsCancellationRequested && model.ThumbnailPath == placeholderCard)
                                    {
                                        dialog.Invoke(new Action(() => dialog.StatusLabel.Text = $"Downloading thumbnail for {name}..."));
                                        await DownloadThumbnailFromModelInfoAsync(model, pngPath);
                                    }
                                }
                            }
                            catch { }
                        }
                        else if (!dialog.CancellationTokenSource.IsCancellationRequested)
                        {
                            dialog.Invoke(new Action(() => dialog.StatusLabel.Text = $"Fetching info for {name}..."));
                            json = await GetModelInfoFromApiAsync(file, infoPath);
                            if (!string.IsNullOrEmpty(json))
                            {
                                model.InfoJsonPath = infoPath;
                                if (json == NoInfoMarker)
                                {
                                    model.Metadata = null;
                                }
                                else
                                {
                                    try
                                    {
                                        model.Metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                                        if (model.ThumbnailPath == placeholderCard)
                                        {
                                            dialog.Invoke(new Action(() => dialog.StatusLabel.Text = $"Downloading thumbnail for {name}..."));
                                            await DownloadThumbnailFromModelInfoAsync(model, pngPath);
                                        }
                                    }
                                    catch { }
                                }
                            }
                        }
                        lock (models)
                        {
                            models.Add(model);
                        }
                    }
                    finally
                    {
                        int progressValue;
                        lock (progressLock)
                        {
                            current++;
                            progressValue = current;
                        }
                        dialog.Invoke(new Action(() => {
                            dialog.ProgressBar.Value = progressValue;
                            dialog.StatusLabel.Text = $"Processing {progressValue} of {total}: {Path.GetFileName(file)}";
                        }));
                        semaphore.Release();
                    }
                });
                tasks.Add(task);
            }
            await Task.WhenAll(tasks);
            // If cancelled, add any missing models as basic ModelInfo
            if (dialog.CancellationTokenSource.IsCancellationRequested)
            {
                var processedFiles = new HashSet<string>(models.Select(m => m.FilePath));
                foreach (var file in modelFiles)
                {
                    if (!processedFiles.Contains(file))
                    {
                        var ext = Path.GetExtension(file).ToLowerInvariant();
                        var name = Path.GetFileNameWithoutExtension(file);
                        var pngPath = Path.Combine(Path.GetDirectoryName(file) ?? folderPath, name + ".preview.png");
                        var model = new ModelInfo
                        {
                            FilePath = file,
                            Name = name,
                            Type = ext == ".ckpt" ? "Checkpoint" : ext == ".safetensors" ? "LoRA/Safetensors" : "Other",
                            InfoJsonPath = null,
                            ThumbnailPath = File.Exists(pngPath) ? pngPath : placeholderCard,
                            Metadata = null
                        };
                        models.Add(model);
                    }
                }
            }
            dialog.Close();
            // Populate baseModel filter
            var baseModels = models.Select(m => m.Metadata != null && m.Metadata.ContainsKey("baseModel") ? m.Metadata["baseModel"]?.ToString() : null)
                .Where(bm => !string.IsNullOrEmpty(bm)).Distinct().OrderBy(bm => bm).ToList();
            cmbBaseModel.Items.Clear();
            cmbBaseModel.Items.Add("All"); // Add 'All' option
            cmbBaseModel.Items.AddRange(baseModels.ToArray());
            cmbBaseModel.SelectedIndex = 0;
            DisplayModels(models);
        }

        private void SortModels(ModelSortOrder order)
        {
            currentSortOrder = order;
            switch (order)
            {
                case ModelSortOrder.FilenameAsc:
                    models = models.OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase).ToList();
                    break;
                case ModelSortOrder.FilenameDesc:
                    models = models.OrderByDescending(m => m.Name, StringComparer.OrdinalIgnoreCase).ToList();
                    break;
                case ModelSortOrder.DateAsc:
                    models = models.OrderBy(m => File.Exists(m.FilePath) ? File.GetLastWriteTime(m.FilePath) : DateTime.MinValue).ToList();
                    break;
                case ModelSortOrder.DateDesc:
                    models = models.OrderByDescending(m => File.Exists(m.FilePath) ? File.GetLastWriteTime(m.FilePath) : DateTime.MinValue).ToList();
                    break;
                case ModelSortOrder.SizeAsc:
                    models = models.OrderBy(m => File.Exists(m.FilePath) ? new FileInfo(m.FilePath).Length : 0L).ToList();
                    break;
                case ModelSortOrder.SizeDesc:
                    models = models.OrderByDescending(m => File.Exists(m.FilePath) ? new FileInfo(m.FilePath).Length : 0L).ToList();
                    break;
            }
            DisplayModels(models);
        }

        private void InitializeModelContextMenu()
        {
            modelContextMenu = new ContextMenuStrip();
            var openCivitaiItem = new ToolStripMenuItem("Open in Civitai");
            openCivitaiItem.Click += (s, e) =>
            {
                if (contextModel != null && contextModel.Metadata != null &&
                    contextModel.Metadata.TryGetValue("id", out var idObj) &&
                    contextModel.Metadata.TryGetValue("modelId", out var modelIdObj))
                {
                    string id = idObj?.ToString() ?? "";
                    string modelId = modelIdObj?.ToString() ?? "";
                    if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(modelId))
                    {
                        string url = $"https://civitai.com/models/{modelId}?modelVersionId={id}";
                        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true }); } catch { }
                    }
                }
            };
            var deleteModelItem = new ToolStripMenuItem("Delete Model from Disk");
            deleteModelItem.Click += (s, e) =>
            {
                if (contextModel != null)
                {
                    var result = MessageBox.Show($"Are you sure you want to delete '{contextModel.Name}'? This cannot be undone.", "Delete Model", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (result == DialogResult.Yes)
                    {
                        try
                        {
                            if (File.Exists(contextModel.FilePath)) File.Delete(contextModel.FilePath);
                            if (!string.IsNullOrEmpty(contextModel.InfoJsonPath) && File.Exists(contextModel.InfoJsonPath)) File.Delete(contextModel.InfoJsonPath);
                            if (!string.IsNullOrEmpty(contextModel.ThumbnailPath) && File.Exists(contextModel.ThumbnailPath)) File.Delete(contextModel.ThumbnailPath);
                            models.Remove(contextModel);
                            filteredModels.Remove(contextModel);
                            DisplayModels(filteredModels.Count > 0 ? filteredModels : models);
                            ClearModelInfo();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error deleting model: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            };
            // --- Sort submenu ---
            var sortMenu = new ToolStripMenuItem("Sort Models");
            var sortByFilename = new ToolStripMenuItem("By Filename (A-Z)");
            sortByFilename.Click += (s, e) => SortModels(ModelSortOrder.FilenameAsc);
            var sortByFilenameDesc = new ToolStripMenuItem("By Filename (Z-A)");
            sortByFilenameDesc.Click += (s, e) => SortModels(ModelSortOrder.FilenameDesc);
            var sortByDate = new ToolStripMenuItem("By Modification Date (Newest)");
            sortByDate.Click += (s, e) => SortModels(ModelSortOrder.DateDesc);
            var sortByDateAsc = new ToolStripMenuItem("By Modification Date (Oldest)");
            sortByDateAsc.Click += (s, e) => SortModels(ModelSortOrder.DateAsc);
            var sortBySize = new ToolStripMenuItem("By Filesize (Largest)");
            sortBySize.Click += (s, e) => SortModels(ModelSortOrder.SizeDesc);
            var sortBySizeAsc = new ToolStripMenuItem("By Filesize (Smallest)");
            sortBySizeAsc.Click += (s, e) => SortModels(ModelSortOrder.SizeAsc);
            sortMenu.DropDownItems.AddRange(new ToolStripItem[] { sortByFilename, sortByFilenameDesc, sortByDate, sortByDateAsc, sortBySize, sortBySizeAsc });
            // ---
            modelContextMenu.Items.Add(openCivitaiItem);
            modelContextMenu.Items.Add(deleteModelItem);
            modelContextMenu.Items.Add(new ToolStripSeparator());
            modelContextMenu.Items.Add(sortMenu);
        }

        private void trackModelScale_Scroll(object sender, EventArgs e)
        {
            int thumbW = trackModelScale.Value;
            thumbnailSize = new Size(thumbW, thumbW);
            modelItemSize = new Size(thumbW + 12, thumbW + 42);
            UpdateModelItemSizes();
        }

        private Image FitImageToBox(Image img, int maxW, int maxH)
        {
            float ratio = Math.Min((float)maxW / img.Width, (float)maxH / img.Height);
            int w = (int)(img.Width * ratio);
            int h = (int)(img.Height * ratio);
            var bmp = new Bitmap(maxW, maxH);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                int x = (maxW - w) / 2;
                int y = (maxH - h) / 2;
                g.DrawImage(img, x, y, w, h);
            }
            return bmp;
        }

        private void ShowModelInfo(ModelInfo model)
        {
            if (model == null) return;
            currentModel = model;
            if (model.Metadata == null)
            {
                groupModelInfo.Text = "Model Info";
                webModelInfo.DocumentText = "<b>No metadata available.</b>";
                return;
            }
            // Extract top-level fields
            string id = model.Metadata.TryGetValue("id", out var idObj) ? idObj?.ToString() ?? "" : "";
            string modelId = model.Metadata.TryGetValue("modelId", out var modelIdObj) ? modelIdObj?.ToString() ?? "" : "";
            string name = model.Metadata.TryGetValue("name", out var nameObj) ? nameObj?.ToString() ?? "" : model.Name;          

            string createdAt = model.Metadata.TryGetValue("createdAt", out var createdAtObj) ? createdAtObj?.ToString() ?? "" : "";
            string updatedAt = model.Metadata.TryGetValue("updatedAt", out var updatedAtObj) ? updatedAtObj?.ToString() ?? "" : "";
            string publishedAt = model.Metadata.TryGetValue("publishedAt", out var publishedAtObj) ? publishedAtObj?.ToString() ?? "" : "";
            string baseModel = model.Metadata.TryGetValue("baseModel", out var baseModelObj) ? baseModelObj?.ToString() ?? "" : "";
            string baseModelType = model.Metadata.TryGetValue("baseModelType", out var baseModelTypeObj) ? baseModelTypeObj?.ToString() ?? "" : "";

            // File size
            string fileSizeStr = "";
            try
            {
                if (File.Exists(model.FilePath))
                {
                    long fileSize = new FileInfo(model.FilePath).Length;
                    fileSizeStr = fileSize >= 1024 * 1024 ? $"{fileSize / (1024.0 * 1024.0):F2} MB" : fileSize >= 1024 ? $"{fileSize / 1024.0:F2} KB" : $"{fileSize} bytes";
                }
            }
            catch { fileSizeStr = ""; }

            // Extract nested model info
            string modelName = "", modelType = "", modelDescription = "", modelTags = "", modelNSFW = "", modelPOI = "", allowNoCredit = "", allowCommercialUse = "", allowDerivatives = "", allowDifferentLicense = "";

            modelDescription = model.Metadata.TryGetValue("description", out var d) ? modelDescription = d?.ToString() ?? "" : "";

            if (model.Metadata.TryGetValue("model", out var modelObj) && modelObj is JsonElement modelElem && modelElem.ValueKind == JsonValueKind.Object)
            {
                if (modelElem.TryGetProperty("name", out var n)) modelName = n.GetString() ?? "";
                if (modelElem.TryGetProperty("type", out var t)) modelType = t.GetString() ?? "";
                if (modelDescription == "" && modelElem.TryGetProperty("description", out var d2)) modelDescription = d2.GetString() ?? "";
                if (modelElem.TryGetProperty("tags", out var tagsElem) && tagsElem.ValueKind == JsonValueKind.Array)
                    modelTags = string.Join(", ", tagsElem.EnumerateArray().Select(x => x.GetString()));
                if (modelElem.TryGetProperty("nsfw", out var nsfwElem)) modelNSFW = nsfwElem.GetBoolean() ? "Yes" : "No";
                if (modelElem.TryGetProperty("poi", out var poiElem)) modelPOI = poiElem.GetBoolean() ? "Yes" : "No";
                if (modelElem.TryGetProperty("allowNoCredit", out var ancElem)) allowNoCredit = ancElem.GetBoolean() ? "Yes" : "No";
                if (modelElem.TryGetProperty("allowCommercialUse", out var acuElem) && acuElem.ValueKind == JsonValueKind.Array)
                    allowCommercialUse = string.Join(", ", acuElem.EnumerateArray().Select(x => x.GetString()));
                if (modelElem.TryGetProperty("allowDerivatives", out var adElem)) allowDerivatives = adElem.GetBoolean() ? "Yes" : "No";
                if (modelElem.TryGetProperty("allowDifferentLicense", out var adlElem)) allowDifferentLicense = adlElem.GetBoolean() ? "Yes" : "No";
            }

            string civitaiLink = "";
            if (!string.IsNullOrEmpty(modelId) && !string.IsNullOrEmpty(id))
            {
                civitaiLink = $"<a href='https://civitai.com/models/{modelId}?modelVersionId={id}' target='_blank'>View on Civitai</a>";
            }

            // Get panel size for scaling images
            int panelW = groupModelInfo.Width;
            int panelH = groupModelInfo.Height;
            int imgW = (int)(panelW * 0.95);
            int imgH = (int)(panelH * 0.5);
            if (imgW < 100) imgW = 220;
            if (imgH < 100) imgH = 220;
            string imagesHtml = "";
            if (model.Metadata.TryGetValue("images", out var imagesObj) && imagesObj is JsonElement imagesElem && imagesElem.ValueKind == JsonValueKind.Array)
            {
                var urls = imagesElem.EnumerateArray()
                    .Where(img => img.ValueKind == JsonValueKind.Object && img.TryGetProperty("url", out var urlElem))
                    .Select(img => img.GetProperty("url").GetString())
                    .Where(url => !string.IsNullOrEmpty(url));
                foreach (var url in urls)
                {
                    imagesHtml += $"<img src='{url}' style='width:{imgW}px;max-width:100%;height:auto;max-height:{imgH}px;margin:4px;border:1px solid #ccc;display:block;' />";
                }
            }
            string html = $@"
            <html><head>
            <meta name='viewport' content='width=device-width, initial-scale=1'>
            <style>
              body {{ margin:0; padding:0; font-family:sans-serif; }}
              .container {{ width:100%; height:100%; box-sizing:border-box; padding:12px; }}
              table {{ width:100%; font-size:14px; }}
              img {{ box-sizing:border-box; }}
              .images {{ display:flex; flex-direction:column; align-items:center; width:100%; }}
              .civitai-link {{ margin:8px 0; font-size:16px; }}
            </style>
            </head><body>
            <div class='container'>
              <h2>{modelName}</h2>
              <div class='civitai-link'>{civitaiLink}</div>
              <table>
                <tr><td><b>ID</b></td><td>{id}</td></tr>
                <tr><td><b>Model ID</b></td><td>{modelId}</td></tr>
                <tr><td><b>Name</b></td><td>{name}</td></tr>
                <tr><td><b>Filesize</b></td><td>{fileSizeStr}</td></tr>
                <tr><td><b>Created At</b></td><td>{createdAt}</td></tr>
                <tr><td><b>Updated At</b></td><td>{updatedAt}</td></tr>
                <tr><td><b>Published At</b></td><td>{publishedAt}</td></tr>
                <tr><td><b>Base Model</b></td><td>{baseModel}</td></tr>
                <tr><td><b>Base Model Type</b></td><td>{baseModelType}</td></tr>
              </table>
              <hr />
              <h3>Model Details</h3>
              <table>
                <tr><td><b>Name</b></td><td>{modelName}</td></tr>
                <tr><td><b>Type</b></td><td>{modelType}</td></tr>
                <tr><td><b>NSFW</b></td><td>{modelNSFW}</td></tr>
                <tr><td><b>POI</b></td><td>{modelPOI}</td></tr>
                <tr><td><b>Tags</b></td><td>{modelTags}</td></tr>
                <tr><td><b>Allow No Credit</b></td><td>{allowNoCredit}</td></tr>
                <tr><td><b>Allow Commercial Use</b></td><td>{allowCommercialUse}</td></tr>
                <tr><td><b>Allow Derivatives</b></td><td>{allowDerivatives}</td></tr>
                <tr><td><b>Allow Different License</b></td><td>{allowDifferentLicense}</td></tr>
              </table>
              <hr />
              <h3>Description</h3>
              <div>{modelDescription}</div>
              <hr />
              <h3>Images</h3>
              <div class='images'>{imagesHtml}</div>
            </div>
            </body></html>
            ";
            webModelInfo.DocumentText = html;
        }

        private void ClearModelInfo()
        {
            webModelInfo.DocumentText = "";
        }

        private Point GetCenterLocationForDialog(Form dialog)
        {
            // Get the center of the main form
            int x = this.Location.X + (this.Width - dialog.Width) / 2;
            int y = this.Location.Y + (this.Height - dialog.Height) / 2;
            return new Point(Math.Max(x, 0), Math.Max(y, 0));
        }
    }   
}
