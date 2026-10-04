using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace AzuraGameTranslate
{
    internal static class Program
    {
        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [STAThread]
        private static void Main()
        {
            try { SetProcessDPIAware(); } catch { }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    internal sealed class MainForm : Form
    {
        private readonly TextBox _apiKey;
        private readonly ComboBox _provider;
        private readonly CheckBox _speak;
        private readonly Button _startButton;
        private readonly Button _stopButton;
        private readonly Label _regionLabel;
        private readonly Label _statusLabel;
        private readonly HttpClient _http = new HttpClient();
        private Rectangle? _region;
        private CancellationTokenSource _cancellation;
        private TranslationOverlay _overlay;
        private int _session;
        private bool _closing;

        public MainForm()
        {
            Text = "Azura Game Translate";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(640, 445);
            MinimumSize = new Size(640, 445);
            MaximumSize = new Size(640, 445);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            Font = new Font("Segoe UI", 10F);
            BackColor = Color.White;

            Label title = new Label();
            title.Text = "Azura Game Translate";
            title.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
            title.Location = new Point(24, 18);
            title.Size = new Size(580, 38);
            Controls.Add(title);

            Label intro = new Label();
            intro.Text = "Oyun altyaz\u0131s\u0131n\u0131 yerel OCR ile oku, T\u00FCrk\u00E7e \u00E7eviriyi ekranda g\u00F6ster.";
            intro.Location = new Point(26, 59);
            intro.Size = new Size(585, 26);
            Controls.Add(intro);

            AddLabel("DeepL API plan\u0131", 26, 105, 112);
            _provider = new ComboBox();
            _provider.DropDownStyle = ComboBoxStyle.DropDownList;
            _provider.Items.Add("DeepL API Developer / Pro");
            _provider.Items.Add("DeepL API Free (eski hesaplar)");
            _provider.SelectedIndex = 0;
            _provider.Location = new Point(145, 101);
            _provider.Size = new Size(455, 29);
            Controls.Add(_provider);

            AddLabel("API anahtar\u0131", 26, 148, 112);
            _apiKey = new TextBox();
            _apiKey.UseSystemPasswordChar = true;
            _apiKey.Location = new Point(145, 144);
            _apiKey.Size = new Size(455, 29);
            Controls.Add(_apiKey);

            Label privacy = new Label();
            privacy.Text = "API anahtar\u0131 saklanmaz; yaln\u0131zca altyaz\u0131 metni DeepL'e g\u00F6nderilir.";
            privacy.ForeColor = Color.FromArgb(71, 85, 105);
            privacy.Location = new Point(145, 176);
            privacy.Size = new Size(455, 40);
            Controls.Add(privacy);

            Button chooseRegion = new Button();
            chooseRegion.Text = "Altyaz\u0131 alan\u0131n\u0131 se\u00E7";
            chooseRegion.Location = new Point(26, 226);
            chooseRegion.Size = new Size(180, 36);
            chooseRegion.Click += ChooseRegion;
            Controls.Add(chooseRegion);

            _regionLabel = new Label();
            _regionLabel.Text = "Hen\u00FCz alan se\u00E7ilmedi";
            _regionLabel.Location = new Point(220, 234);
            _regionLabel.Size = new Size(380, 24);
            Controls.Add(_regionLabel);

            _speak = new CheckBox();
            _speak.Text = "\u00C7eviriyi Windows'un T\u00FCrk\u00E7e ses motoruyla oku (varsa)";
            _speak.Location = new Point(26, 278);
            _speak.Size = new Size(570, 28);
            Controls.Add(_speak);

            _startButton = new Button();
            _startButton.Text = "\u00C7eviriyi ba\u015Flat";
            _startButton.Location = new Point(26, 321);
            _startButton.Size = new Size(574, 42);
            _startButton.BackColor = Color.FromArgb(37, 99, 235);
            _startButton.ForeColor = Color.White;
            _startButton.FlatStyle = FlatStyle.Flat;
            _startButton.FlatAppearance.BorderSize = 0;
            _startButton.Click += StartMonitoring;
            Controls.Add(_startButton);

            _stopButton = new Button();
            _stopButton.Text = "Durdur";
            _stopButton.Location = new Point(26, 370);
            _stopButton.Size = new Size(574, 34);
            _stopButton.Enabled = false;
            _stopButton.Click += delegate { StopMonitoring(true); };
            Controls.Add(_stopButton);

            _statusLabel = new Label();
            _statusLabel.Text = "Haz\u0131r. Oyun borderless/windowed modda olmal\u0131.";
            _statusLabel.ForeColor = Color.FromArgb(71, 85, 105);
            _statusLabel.Location = new Point(26, 411);
            _statusLabel.Size = new Size(590, 26);
            Controls.Add(_statusLabel);

            FormClosing += OnClosing;
        }

        private void AddLabel(string text, int x, int y, int width)
        {
            Label label = new Label();
            label.Text = text;
            label.Location = new Point(x, y);
            label.Size = new Size(width, 25);
            Controls.Add(label);
        }

        private void ChooseRegion(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
            timer.Interval = 300;
            timer.Tick += delegate
            {
                timer.Stop();
                timer.Dispose();
                if (_closing) return;
                RegionPicker picker = new RegionPicker();
                picker.Completed += delegate(Rectangle area)
                {
                    _region = area;
                    if (!_closing)
                    {
                        _regionLabel.Text = area.Width + " x " + area.Height + " px";
                        WindowState = FormWindowState.Normal;
                        Activate();
                    }
                };
                picker.Cancelled += delegate
                {
                    if (!_closing)
                    {
                        WindowState = FormWindowState.Normal;
                        Activate();
                    }
                };
                picker.Show();
                picker.Activate();
            };
            timer.Start();
        }

        private async void StartMonitoring(object sender, EventArgs e)
        {
            string apiKey = _apiKey.Text.Trim();
            if (apiKey.Length == 0)
            {
                MessageBox.Show("DeepL API anahtar\u0131n\u0131 girip tekrar dene.", "API anahtar\u0131 gerekli", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!_region.HasValue)
            {
                MessageBox.Show("\u00D6nce altyaz\u0131 alan\u0131n\u0131 se\u00E7.", "Altyaz\u0131 alan\u0131 gerekli", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string tesseract = TesseractLocator.Find();
            if (tesseract == null)
            {
                MessageBox.Show("Tesseract OCR bulunamad\u0131. Kurulumu yeniden \u00E7al\u0131\u015Ft\u0131r.", "OCR bulunamad\u0131", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string endpoint = _provider.SelectedIndex == 1
                ? "https://api-free.deepl.com/v2/translate"
                : "https://api.deepl.com/v2/translate";
            _cancellation = new CancellationTokenSource();
            int session = ++_session;
            _startButton.Enabled = false;
            _stopButton.Enabled = true;
            SetStatus("Altyaz\u0131 alan\u0131 izleniyor...");
            _overlay = new TranslationOverlay();
            _overlay.StopRequested += delegate { StopMonitoring(true); };
            _overlay.Show();
            Rectangle area = _region.Value;
            bool speak = _speak.Checked;
            CancellationToken token = _cancellation.Token;
            CancellationTokenSource source = _cancellation;
            Task scan = Task.Run(async () =>
            {
                try { await ScanLoop(session, token, apiKey, endpoint, tesseract, area, speak); }
                finally { FinishSession(session, source); }
            });
            await Task.Delay(200);
            if (!_closing && _cancellation != null && _session == session)
                WindowState = FormWindowState.Minimized;
            await scan;
        }

        private void FinishSession(int session, CancellationTokenSource source)
        {
            try { source.Dispose(); } catch { }
            if (_closing || IsDisposed || !IsHandleCreated) return;
            try
            {
                BeginInvoke((Action)delegate
                {
                    if (_closing || IsDisposed || _session != session) return;
                    _cancellation = null;
                    _startButton.Enabled = true;
                    _stopButton.Enabled = false;
                    if (_overlay != null)
                    {
                        _overlay.Close();
                        _overlay.Dispose();
                        _overlay = null;
                    }
                    WindowState = FormWindowState.Normal;
                    Activate();
                });
            }
            catch { }
        }

        private async Task ScanLoop(int session, CancellationToken token, string apiKey, string endpoint, string tesseract, Rectangle area, bool speak)
        {
            string candidate = "";
            string processed = "";
            DateTime candidateAt = DateTime.UtcNow;
            DateTime? emptyAt = null;
            object speech = null;
            try
            {
                if (speak)
                {
                    try
                    {
                        speech = CreateTurkishSpeaker();
                        if (speech == null)
                            SetStatus(session, "Windows'ta T\u00FCrk\u00E7e ses bulunamad\u0131; yaz\u0131 \u00E7evirisi a\u00E7\u0131k.");
                    }
                    catch
                    {
                        ReleaseSpeaker(speech);
                        speech = null;
                        SetStatus(session, "Windows ses motoru a\u00E7\u0131lamad\u0131; yaz\u0131 \u00E7evirisi a\u00E7\u0131k.");
                    }
                }

                while (!token.IsCancellationRequested)
                {
                    string recognized = await RecognizeAsync(tesseract, area, token);
                    string text = Regex.Replace(recognized, @"\s+", " ").Trim();
                    DateTime now = DateTime.UtcNow;
                    if (text.Length == 0)
                    {
                        candidate = "";
                        if (!emptyAt.HasValue) emptyAt = now;
                        else if ((now - emptyAt.Value).TotalMilliseconds > 1400) processed = "";
                    }
                    else
                    {
                        emptyAt = null;
                        if (!String.Equals(text, candidate, StringComparison.Ordinal))
                        {
                            candidate = text;
                            candidateAt = now;
                        }
                        else if ((now - candidateAt).TotalMilliseconds >= 450 && !String.Equals(text, processed, StringComparison.Ordinal))
                        {
                            SetStatus(session, "Altyaz\u0131 bulundu; \u00E7evriliyor...");
                            string translation = await TranslateAsync(apiKey, endpoint, text, token);
                            if (token.IsCancellationRequested) return;
                            SetTranslation(session, translation);
                            if (speech != null) Speak(speech, translation);
                            processed = text;
                            SetStatus(session, "\u00C7eviri haz\u0131r; altyaz\u0131 alan\u0131 izleniyor.");
                        }
                    }
                    await Task.Delay(250, token);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                SetStatus(session, "\u00C7eviri durdu: " + ex.Message);
            }
            finally
            {
                ReleaseSpeaker(speech);
            }
        }

        private static object CreateTurkishSpeaker()
        {
            Type type = Type.GetTypeFromProgID("SAPI.SpVoice");
            if (type == null) return null;
            dynamic speaker = Activator.CreateInstance(type);
            dynamic voices = speaker.GetVoices("", "");
            int count = Convert.ToInt32(voices.Count, CultureInfo.InvariantCulture);
            for (int i = 0; i < count; i++)
            {
                dynamic voice = voices.Item(i);
                string language = Convert.ToString(voice.GetAttribute("Language"), CultureInfo.InvariantCulture);
                if (Regex.IsMatch(language ?? "", @"(^|;)\s*(041f|41f)(;|$)", RegexOptions.IgnoreCase))
                {
                    speaker.Voice = voice;
                    return (object)speaker;
                }
            }
            ReleaseSpeaker((object)speaker);
            return null;
        }

        private static void Speak(object speaker, string text)
        {
            dynamic sapi = speaker;
            sapi.Speak(text, 1);
        }

        private static void ReleaseSpeaker(object speaker)
        {
            if (speaker != null && Marshal.IsComObject(speaker))
            {
                try { Marshal.FinalReleaseComObject(speaker); } catch { }
            }
        }

        private static async Task<string> RecognizeAsync(string tesseract, Rectangle area, CancellationToken token)
        {
            using (Bitmap bitmap = new Bitmap(area.Width, area.Height, PixelFormat.Format24bppRgb))
            {
                using (Graphics graphics = Graphics.FromImage(bitmap))
                    graphics.CopyFromScreen(area.Location, Point.Empty, area.Size, CopyPixelOperation.SourceCopy);
                using (MemoryStream image = new MemoryStream())
                {
                    bitmap.Save(image, ImageFormat.Png);
                    byte[] png = image.ToArray();
                    ProcessStartInfo start = new ProcessStartInfo();
                    start.FileName = tesseract;
                    start.Arguments = "stdin stdout -l eng --psm 6";
                    start.UseShellExecute = false;
                    start.CreateNoWindow = true;
                    start.RedirectStandardInput = true;
                    start.RedirectStandardOutput = true;
                    start.RedirectStandardError = true;
                    start.StandardOutputEncoding = Encoding.UTF8;
                    start.StandardErrorEncoding = Encoding.UTF8;
                    using (Process process = new Process())
                    {
                        process.StartInfo = start;
                        process.Start();
                        Task<string> output = process.StandardOutput.ReadToEndAsync();
                        Task<string> errors = process.StandardError.ReadToEndAsync();
                        using (token.Register(delegate
                        {
                            try { if (!process.HasExited) process.Kill(); } catch { }
                        }))
                        {
                            await process.StandardInput.BaseStream.WriteAsync(png, 0, png.Length, token);
                            process.StandardInput.Close();
                            await Task.Run(() => process.WaitForExit(), token);
                        }
                        string result = await output;
                        await errors;
                        if (process.ExitCode != 0) throw new InvalidOperationException("Tesseract OCR yanit vermedi.");
                        return result;
                    }
                }
            }
        }

        private async Task<string> TranslateAsync(string apiKey, string endpoint, string text, CancellationToken token)
        {
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, endpoint))
            {
                request.Headers.TryAddWithoutValidation("Authorization", "DeepL-Auth-Key " + apiKey);
                request.Content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("text", text),
                    new KeyValuePair<string, string>("source_lang", "EN"),
                    new KeyValuePair<string, string>("target_lang", "TR")
                });
                using (HttpResponseMessage response = await _http.SendAsync(request, token))
                {
                    string body = await response.Content.ReadAsStringAsync();
                    if (!response.IsSuccessStatusCode)
                        throw new InvalidOperationException("DeepL API HTTP " + (int)response.StatusCode + ".");
                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    Dictionary<string, object> data = serializer.Deserialize<Dictionary<string, object>>(body);
                    ArrayList translations = data["translations"] as ArrayList;
                    Dictionary<string, object> first = translations == null || translations.Count == 0
                        ? null
                        : translations[0] as Dictionary<string, object>;
                    if (first == null || !first.ContainsKey("text"))
                        throw new InvalidOperationException("DeepL yan\u0131t\u0131 okunamad\u0131.");
                    return Convert.ToString(first["text"]);
                }
            }
        }

        private void SetStatus(string value)
        {
            if (InvokeRequired)
            {
                if (!IsDisposed && IsHandleCreated) BeginInvoke((Action)(() => SetStatus(value)));
                return;
            }
            if (!_closing) _statusLabel.Text = value;
        }

        private void SetStatus(int session, string value)
        {
            if (InvokeRequired)
            {
                if (!IsDisposed && IsHandleCreated) BeginInvoke((Action)(() => SetStatus(session, value)));
                return;
            }
            if (!_closing && _session == session) SetStatus(value);
        }

        private void SetTranslation(int session, string value)
        {
            if (InvokeRequired)
            {
                if (!IsDisposed && IsHandleCreated) BeginInvoke((Action)(() => SetTranslation(session, value)));
                return;
            }
            if (!_closing && _session == session && _overlay != null) _overlay.SetTranslation(value);
        }

        private void StopMonitoring(bool restoreWindow)
        {
            ++_session;
            if (_cancellation != null)
            {
                try { _cancellation.Cancel(); } catch { }
                _cancellation = null;
            }
            if (_overlay != null)
            {
                _overlay.Close();
                _overlay.Dispose();
                _overlay = null;
            }
            if (!_closing)
            {
                _startButton.Enabled = true;
                _stopButton.Enabled = false;
                _statusLabel.Text = "Durduruldu.";
                if (restoreWindow)
                {
                    WindowState = FormWindowState.Normal;
                    Activate();
                }
            }
        }

        private void OnClosing(object sender, FormClosingEventArgs e)
        {
            _closing = true;
            StopMonitoring(false);
            _http.Dispose();
        }
    }

    internal sealed class RegionPicker : Form
    {
        private Point _start;
        private Point _current;
        private bool _dragging;
        private readonly Rectangle _screenBounds;
        public event Action<Rectangle> Completed;
        public event Action Cancelled;

        public RegionPicker()
        {
            _screenBounds = Screen.PrimaryScreen.Bounds;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Bounds = _screenBounds;
            ShowInTaskbar = false;
            TopMost = true;
            KeyPreview = true;
            Cursor = Cursors.Cross;
            BackColor = Color.Black;
            Opacity = 0.88;
            DoubleBuffered = true;
            KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Escape)
                {
                    e.Handled = true;
                    if (Cancelled != null) Cancelled();
                    Close();
                }
            };
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _start = e.Location;
                _current = e.Location;
                _dragging = true;
                Invalidate();
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_dragging)
            {
                _current = e.Location;
                Invalidate();
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (_dragging && e.Button == MouseButtons.Left)
            {
                _dragging = false;
                _current = e.Location;
                Rectangle picked = GetSelection();
                if (picked.Width >= 30 && picked.Height >= 15 && Completed != null)
                {
                    Rectangle absolute = new Rectangle(
                        _screenBounds.Left + picked.Left,
                        _screenBounds.Top + picked.Top,
                        picked.Width,
                        picked.Height);
                    Completed(absolute);
                    Close();
                }
                else Invalidate();
            }
            base.OnMouseUp(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (SolidBrush shade = new SolidBrush(Color.FromArgb(100, 0, 0, 0)))
                e.Graphics.FillRectangle(shade, ClientRectangle);
            using (Font instructionFont = new Font("Segoe UI", 15F, FontStyle.Bold))
                e.Graphics.DrawString("Altyaz\u0131 alan\u0131n\u0131 \u00E7er\u00E7evele ve b\u0131rak  |  Esc: iptal", instructionFont, Brushes.White, 22, 22);
            if (_dragging || _start != Point.Empty)
            {
                Rectangle selection = GetSelection();
                using (SolidBrush fill = new SolidBrush(Color.FromArgb(70, 37, 99, 235)))
                    e.Graphics.FillRectangle(fill, selection);
                using (Pen line = new Pen(Color.FromArgb(255, 103, 232, 249), 3F))
                    e.Graphics.DrawRectangle(line, selection);
            }
        }

        private Rectangle GetSelection()
        {
            return new Rectangle(
                Math.Min(_start.X, _current.X),
                Math.Min(_start.Y, _current.Y),
                Math.Abs(_current.X - _start.X),
                Math.Abs(_current.Y - _start.Y));
        }
    }

    internal sealed class TranslationOverlay : Form
    {
        private readonly Label _caption;
        private Point _dragPoint;
        public event Action StopRequested;

        public TranslationOverlay()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            Opacity = 0.96;
            BackColor = Color.FromArgb(17, 24, 39);
            ClientSize = new Size(800, 118);
            int x = Math.Max(0, (Screen.PrimaryScreen.Bounds.Width - Width) / 2);
            Location = new Point(x, Screen.PrimaryScreen.Bounds.Top + 45);

            Panel header = new Panel();
            header.BackColor = Color.FromArgb(31, 41, 55);
            header.Location = Point.Empty;
            header.Size = new Size(800, 34);
            Controls.Add(header);

            Label title = new Label();
            title.Text = "ENGLISH  >  T\u00DCRK\u00C7E";
            title.ForeColor = Color.FromArgb(103, 232, 249);
            title.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            title.Location = new Point(12, 8);
            title.Size = new Size(230, 20);
            header.Controls.Add(title);

            Button stop = new Button();
            stop.Text = "Durdur";
            stop.FlatStyle = FlatStyle.Flat;
            stop.FlatAppearance.BorderSize = 0;
            stop.ForeColor = Color.White;
            stop.BackColor = Color.FromArgb(185, 28, 28);
            stop.Location = new Point(720, 4);
            stop.Size = new Size(68, 26);
            stop.Click += delegate { if (StopRequested != null) StopRequested(); };
            header.Controls.Add(stop);

            _caption = new Label();
            _caption.Text = "Altyaz\u0131 \u00E7evirisi burada g\u00F6r\u00FCnecek";
            _caption.ForeColor = Color.White;
            _caption.BackColor = BackColor;
            _caption.Font = new Font("Segoe UI", 17F, FontStyle.Bold);
            _caption.TextAlign = ContentAlignment.MiddleLeft;
            _caption.Location = new Point(16, 39);
            _caption.Size = new Size(768, 70);
            _caption.AutoEllipsis = true;
            Controls.Add(_caption);

            header.MouseDown += BeginDrag;
            title.MouseDown += BeginDrag;
            _caption.MouseDown += BeginDrag;
            header.MouseMove += MoveDrag;
            title.MouseMove += MoveDrag;
            _caption.MouseMove += MoveDrag;
        }

        public void SetTranslation(string text)
        {
            _caption.Text = text;
        }

        private void BeginDrag(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) _dragPoint = new Point(e.X, e.Y);
        }

        private void MoveDrag(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Point screen = ((Control)sender).PointToScreen(e.Location);
                Location = new Point(screen.X - _dragPoint.X, screen.Y - _dragPoint.Y);
            }
        }
    }

    internal static class TesseractLocator
    {
        public static string Find()
        {
            string fromPath = FindOnPath();
            if (fromPath != null) return fromPath;
            List<string> candidates = new List<string>();
            string programFiles = Environment.GetEnvironmentVariable("ProgramFiles");
            string programFilesX86 = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
            if (!String.IsNullOrEmpty(programFiles)) candidates.Add(Path.Combine(programFiles, "Tesseract-OCR", "tesseract.exe"));
            if (!String.IsNullOrEmpty(programFilesX86)) candidates.Add(Path.Combine(programFilesX86, "Tesseract-OCR", "tesseract.exe"));
            return candidates.FirstOrDefault(File.Exists);
        }

        private static string FindOnPath()
        {
            string path = Environment.GetEnvironmentVariable("PATH") ?? "";
            foreach (string folder in path.Split(Path.PathSeparator))
            {
                try
                {
                    string candidate = Path.Combine(folder.Trim(), "tesseract.exe");
                    if (File.Exists(candidate)) return candidate;
                }
                catch { }
            }
            return null;
        }
    }
}
