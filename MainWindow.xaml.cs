using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using LibVLCSharp.Shared;
using System.Diagnostics;

namespace ImageAndMp4WebBuilder
{
    public partial class MainWindow : Window
    {
        private readonly List<ThumbnailItem> _allItems = new();
        private int _currentPage = 0;
        private const int PageSize = 40; // roughly fits depending on window size
        private LibVLC? _libVlc;
        private string? _currentFolder;
        private string? _backToUrl; // URL read from out .txt / out.txt

        public MainWindow()
        {
            InitializeComponent();
            Core.Initialize();
            _libVlc = new LibVLC();
            UpdatePaging();
        }

        private void HelpCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var wnd = new HelpWindow
            {
                Owner = this
            };
            wnd.ShowDialog();
        }

        private async void SelectFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new System.Windows.Forms.FolderBrowserDialog();
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                _currentFolder = dlg.SelectedPath;
                SelectedFolderText.Text = dlg.SelectedPath;
                StatusText.Text = "Generating thumbnails...";
                try
                {
                    // Load backto URL (look for both naming variants)
                    _backToUrl = ReadBackToUrl(_currentFolder);
                    await Task.Run(() => LoadAndGenerateThumbnails(dlg.SelectedPath));
                    StatusText.Text = $"Loaded {_allItems.Count} items";
                }
                catch (Exception ex)
                {
                    StatusText.Text = "Error";
                    System.Windows.MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                _currentPage = 0;
                UpdatePaging();
            }
        }

        private string? ReadBackToUrl(string folder)
        {
            try
            {
                // User specified a file named "out .txt" (with a space) but also handle normal "out.txt"
                string[] candidateNames = new[] { "out .txt", "out.txt" };
                foreach (var name in candidateNames)
                {
                    string path = Path.Combine(folder, name);
                    if (File.Exists(path))
                    {
                        string? line = File.ReadLines(path).FirstOrDefault(l => !string.IsNullOrWhiteSpace(l));
                        if (!string.IsNullOrWhiteSpace(line))
                            return line.Trim();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to read backto url: {ex.Message}");
            }
            return null;
        }

        private void LoadAndGenerateThumbnails(string folder)
        {
            _allItems.Clear();
            string thumbDir = Path.Combine(folder, "thumbnails");
            Directory.CreateDirectory(thumbDir);

            var imageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg" };
            var videoExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".mp4" };

            var files = Directory.EnumerateFiles(folder)
                .Where(f => imageExtensions.Contains(Path.GetExtension(f)) || videoExtensions.Contains(Path.GetExtension(f)))
                .OrderByDescending(f => videoExtensions.Contains(Path.GetExtension(f))) // videos first
                .ThenBy(f => f, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var file in files)
            {
                bool isVideo = videoExtensions.Contains(Path.GetExtension(file));
                string thumbName = Path.GetFileNameWithoutExtension(file) + "_thumb.jpg"; // unify format
                string thumbPath = Path.Combine(thumbDir, thumbName);
                if (!File.Exists(thumbPath))
                {
                    try
                    {
                        if (isVideo)
                        {
                            CreateVideoThumbnail(file, thumbPath);
                        }
                        else
                        {
                            CreateImageThumbnail(file, thumbPath, 200);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Thumbnail failed for {file}: {ex.Message}");
                        continue;
                    }
                }
                _allItems.Add(new ThumbnailItem
                {
                    OriginalPath = file,
                    ThumbnailPath = thumbPath,
                    IsVideo = isVideo
                });
            }
        }

        private void CreateImageThumbnail(string sourcePath, string destPath, int width)
        {
            using var fs = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var decoder = BitmapDecoder.Create(fs, BitmapCreateOptions.IgnoreColorProfile, BitmapCacheOption.OnLoad);
            var frame = decoder.Frames[0];
            double scale = (double)width / frame.PixelWidth;
            var resized = new TransformedBitmap(frame, new System.Windows.Media.ScaleTransform(scale, scale));
            var encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(resized));
            using var outStream = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None);
            encoder.Save(outStream);
        }

        private void CreateVideoThumbnail(string videoPath, string destPath)
        {
            if (_libVlc == null) throw new InvalidOperationException("LibVLC not initialized");
            using var media = new Media(_libVlc, new Uri(videoPath));
            using var mp = new MediaPlayer(media);
            media.Parse(MediaParseOptions.ParseLocal);
            long duration = media.Duration; // ms
            if (duration > 0)
            {
                var position = Math.Min(0.1f, (float)1000 / duration);
                mp.Position = position;
            }
            mp.Play();
            System.Threading.Thread.Sleep(500);
            string tempDir = Path.GetDirectoryName(destPath)!;
            Directory.CreateDirectory(tempDir);
            mp.TakeSnapshot(0, destPath, 200, 0);
            mp.Stop();
        }

        private void UpdatePaging()
        {
            Dispatcher.Invoke(() =>
            {
                int totalPages = _allItems.Count == 0 ? 1 : (int)Math.Ceiling(_allItems.Count / (double)PageSize);
                if (_currentPage >= totalPages) _currentPage = totalPages - 1;
                if (_currentPage < 0) _currentPage = 0;
                var pageItems = _allItems.Skip(_currentPage * PageSize).Take(PageSize).ToList();
                ThumbnailsItemsControl.ItemsSource = pageItems;
                PageInfoText.Text = $"Page {_currentPage + 1} of {totalPages}";
                PrevPageButton.IsEnabled = _currentPage > 0;
                NextPageButton.IsEnabled = _currentPage < totalPages - 1;
            });
        }

        private void PrevPageButton_Click(object sender, RoutedEventArgs e)
        {
            _currentPage--;
            UpdatePaging();
        }

        private void NextPageButton_Click(object sender, RoutedEventArgs e)
        {
            _currentPage++;
            UpdatePaging();
        }

        private void Thumbnail_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is System.Windows.Controls.Image img && img.DataContext is ThumbnailItem item)
            {
                if (item.IsVideo)
                {
                    try
                    {
                        System.Diagnostics.Process.Start(new ProcessStartInfo
                        {
                            FileName = item.OriginalPath,
                            UseShellExecute = true
                        });
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"Unable to open video: {ex.Message}");
                    }
                }
                else
                {
                    var wnd = new ImagePreviewWindow(item.OriginalPath)
                    {
                        Owner = this
                    };
                    wnd.ShowDialog();
                }
            }
        }

        private void ExportHtmlButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_currentFolder))
            {
                System.Windows.MessageBox.Show("Select a folder first.");
                return;
            }
            string folderName = new DirectoryInfo(_currentFolder).Name;
            var inputWindow = new SimpleTextInputWindow($"Enter base name for pages (suggested: {folderName}):", folderName);
            inputWindow.Owner = this;
            if (inputWindow.ShowDialog() == true)
            {
                string baseName = inputWindow.InputText.Trim();
                if (string.IsNullOrEmpty(baseName)) return;
                // If the user chooses NOT to ignore NSFW (checkbox unchecked), append _NSFW suffix to filenames.
                if (IgnoreNsfwCheckBox != null && IgnoreNsfwCheckBox.IsChecked == false)
                {
                    if (!baseName.EndsWith("_NSFW", StringComparison.OrdinalIgnoreCase))
                        baseName += "_NSFW";
                }
                try
                {
                    int perPage = PageSize; // reuse page size
                    int totalPages = Math.Max(1, (int)Math.Ceiling(_allItems.Count / (double)perPage));
                    string firstPageFullPath = Path.Combine(_currentFolder, $"{baseName}.html");
                    for (int p = 0; p < totalPages; p++)
                    {
                        string fileName = p == 0 ? $"{baseName}.html" : $"{baseName}{p + 1}.html";
                        string fullPath = Path.Combine(_currentFolder, fileName);
                        var pageItems = _allItems.Skip(p * perPage).Take(perPage).ToList();
                        string html = BuildHtmlPage(baseName, p, totalPages, pageItems);
                        File.WriteAllText(fullPath, html);
                    }
                    // After generating pages, ensure subdirectories without their own index html
                    // get an out.txt pointing back to the generated first page.
                    CreateOutTxtInSubdirectories(firstPageFullPath);
                    System.Windows.MessageBox.Show("HTML pages generated.");
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Failed to generate HTML: {ex.Message}");
                }
            }
        }

        private void CreateOutTxtInSubdirectories(string baseHtmlFullPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_currentFolder)) return;
                foreach (var dir in Directory.EnumerateDirectories(_currentFolder))
                {
                    var dirName = Path.GetFileName(dir);
                    if (string.IsNullOrWhiteSpace(dirName)) continue;
                    var htmlCandidate = Path.Combine(dir, dirName + ".html");
                    if (!File.Exists(htmlCandidate))
                    {
                        string rel = MakeRelativePath(dir, baseHtmlFullPath);
                        var outPath = Path.Combine(dir, "out.txt");
                        File.WriteAllText(outPath, rel);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to create out.txt in subdirectories: {ex.Message}");
            }
        }

        private string BuildHtmlPage(string baseName, int pageIndex, int totalPages, List<ThumbnailItem> items)
        {
            string nav = "<div class='nav'>";
            for (int i = 0; i < totalPages; i++)
            {
                string fn = i == 0 ? $"{baseName}.html" : $"{baseName}{i + 1}.html";
                if (i == pageIndex)
                    nav += $"<span class='current'>Page {i + 1}</span> ";
                else
                    nav += $"<a href='{fn}'>Page {i + 1}</a> ";
            }
            nav += "</div>";

            // Index heading plus optional backto link.
            string indexSection = string.Empty;
            if (pageIndex == 0)
            {
                if (!string.IsNullOrWhiteSpace(_backToUrl))
                {
                    // If the current folder name contains 'NSFW' and the back URL is an html file
                    // that does not already have the _NSFW suffix, append it.
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(_currentFolder))
                        {
                            var folderName = new DirectoryInfo(_currentFolder).Name;
                            if (folderName.Contains("NSFW", StringComparison.OrdinalIgnoreCase)
                                && _backToUrl.EndsWith(".html", StringComparison.OrdinalIgnoreCase)
                                && !_backToUrl.EndsWith("_NSFW.html", StringComparison.OrdinalIgnoreCase))
                            {
                                int idx = _backToUrl.LastIndexOf('.');
                                if (idx > 0)
                                {
                                    _backToUrl = _backToUrl.Substring(0, idx) + "_NSFW" + _backToUrl.Substring(idx);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to adjust back URL for NSFW: {ex.Message}");
                    }
                    string safeUrl = System.Net.WebUtility.HtmlEncode(_backToUrl);
                    indexSection = $"<div class='backto'><button type='button' onclick=\"location.href='{safeUrl}'\">Back</button></div>";
                }
                else
                {
                    indexSection = string.Empty; // formerly showed heading 'Index'; now suppressed per request
                }
            }

            // Subdirectory links section: if any subdirectory contains a .html file with the same name as the subdirectory,
            // include a link to it.
            string subdirsSection = string.Empty;
            try
            {
                var subLinks = GetSubdirectoryLinks();
                if (subLinks.Count > 0)
                {
                    var linksHtml = string.Join(" ", subLinks.Select(l => $"<a href='{l.href}'>{System.Net.WebUtility.HtmlEncode(l.name)}</a>"));
                    subdirsSection = $"<div class='sublinks'><span class='label'>Subfolders:</span> {linksHtml}</div>";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to gather subdirectory links: {ex.Message}");
            }

            string thumbs = items.Count == 0
                ? "<div class='empty'>No images or videos found in this folder.</div>"
                : string.Join("", items.Select(it =>
                {
                    string relThumb = MakeRelativePath(_currentFolder!, it.ThumbnailPath);
                    string relOriginal = MakeRelativePath(_currentFolder!, it.OriginalPath);
                    string borderColor = it.IsVideo ? "green" : "blue";
                    return $"<div class='thumb'><a href='{relOriginal}'><img src='{relThumb}' style='width:200px;border:3px solid {borderColor};border-radius:4px;'/></a><div class='name'>{System.Net.WebUtility.HtmlEncode(Path.GetFileName(it.OriginalPath))}</div></div>";
                }));

            return $@"<!DOCTYPE html>
<html>
<head>
<meta charset='utf-8'/>
<title>{System.Net.WebUtility.HtmlEncode(baseName)} - Page {pageIndex + 1}</title>
<style>
body {{ font-family: Arial, sans-serif; }}
.nav {{ margin:10px 0; }}
.nav a {{ margin-right:5px; text-decoration:none; }}
.nav .current {{ font-weight:bold; }}
.container {{ display:flex; flex-wrap:wrap; }}
.thumb {{ width:210px; margin:5px; text-align:center; font-size:12px; }}
.name {{ white-space:nowrap; overflow:hidden; text-overflow:ellipsis; }}
.backto a {{ font-weight:bold; color:#3366cc; text-decoration:none; }}
.backto a:hover {{ text-decoration:underline; }}
.sublinks {{ margin:10px 0; font-size:14px; }}
.sublinks .label {{ font-weight:bold; margin-right:6px; }}
.sublinks a {{ margin-right:8px; text-decoration:none; color:#3366cc; }}
.sublinks a:hover {{ text-decoration:underline; }}
</style>
</head>
<body>
<h1>{System.Net.WebUtility.HtmlEncode(baseName)}</h1>
{nav}
{indexSection}
{subdirsSection}
<div class='container'>
{thumbs}
</div>
{nav}
</body>
</html>";
        }

        private List<(string name, string href)> GetSubdirectoryLinks()
        {
            var results = new List<(string name, string href)>();
            if (string.IsNullOrWhiteSpace(_currentFolder)) return results;
            try
            {
                foreach (var dir in Directory.EnumerateDirectories(_currentFolder))
                {
                    var dirName = Path.GetFileName(dir);
                    if (string.IsNullOrWhiteSpace(dirName)) continue;
                    // If Ignore NSFW is enabled, skip directories whose name contains 'NSFW' (case-insensitive)
                    if (IgnoreNsfwCheckBox != null && IgnoreNsfwCheckBox.IsChecked == true && dirName.Contains("NSFW", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    var candidate = Path.Combine(dir, dirName + ".html");
                    if (File.Exists(candidate))
                    {
                        string rel = MakeRelativePath(_currentFolder, candidate);
                        results.Add((dirName, rel));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error enumerating subdirectories for links: {ex.Message}");
            }
            return results;
        }

        private static string MakeRelativePath(string baseDir, string fullPath)
        {
            var uriBase = new Uri(AppendDirectorySeparatorChar(baseDir));
            var uriFull = new Uri(fullPath);
            return uriBase.MakeRelativeUri(uriFull).ToString().Replace('%', '%');
        }

        private static string AppendDirectorySeparatorChar(string path)
        {
            if (!path.EndsWith(Path.DirectorySeparatorChar))
                return path + Path.DirectorySeparatorChar;
            return path;
        }
    }
}