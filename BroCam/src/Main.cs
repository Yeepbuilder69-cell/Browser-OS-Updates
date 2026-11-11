using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Windows.Forms;
using DirectN;
using VCamNetSampleSource;

namespace VCamNetSample
{
    public partial class Main : Form
    {
        private const int W = 1280, H = 960;
        private readonly List<Layer> _layers = new();
        private readonly Panel _preview = new();
        private readonly ListBox _list = new();
        private readonly Button _toggle = new();
        private readonly Label _status = new();
        private MemoryMappedFile? _map;
        private MemoryMappedViewAccessor? _view;
        private System.Windows.Forms.Timer? _timer;
        private bool _running;
        private IComObject<IMFVirtualCamera>? _camera;
        private bool _mfStarted;
        private Layer? _drag;
        private Point _dragOffset;

        private sealed class Layer
        {
            public string Name = "";
            public string Kind = "Text";
            public string Text = "";
            public string? Path;
            public PointF Position = new(100, 100);
            public SizeF Size = new(500, 120);
            public Color Color = Color.White;
            public bool Visible = true;
        }

        public Main()
        {
            Text = "Bro Cam";
            Width = 1120; Height = 760; MinimumSize = new Size(900, 620);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(20, 16, 31); ForeColor = Color.White;
            var header = new Label { Text = "BRO CAM", ForeColor = Color.FromArgb(190,145,255), Font = new Font("Segoe UI", 22, FontStyle.Bold), AutoSize = true, Location = new Point(18, 12) };
            Controls.Add(header);
            _status.Text = "Virtual camera stopped"; _status.AutoSize = true; _status.Location = new Point(210, 30); _status.ForeColor = Color.Gainsboro; Controls.Add(_status);
            _toggle.Text = "Start Virtual Camera"; _toggle.Location = new Point(870, 18); _toggle.Size = new Size(210, 42); Style(_toggle); _toggle.Click += (_,__) => ToggleCamera(); Controls.Add(_toggle);

            var sidebar = new Panel { Location = new Point(15, 75), Size = new Size(245, 630), BackColor = Color.FromArgb(31, 25, 45), Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left };
            Controls.Add(sidebar);
            var addImage = MakeButton("Add Image", 12, 14); addImage.Click += (_,__) => AddImage(); sidebar.Controls.Add(addImage);
            var addText = MakeButton("Add Text", 12, 60); addText.Click += (_,__) => AddText(); sidebar.Controls.Add(addText);
            var remove = MakeButton("Remove Layer", 12, 106); remove.Click += (_,__) => RemoveLayer(); sidebar.Controls.Add(remove);
            var up = MakeButton("Move Layer Up", 12, 152); up.Click += (_,__) => MoveLayer(1); sidebar.Controls.Add(up);
            var down = MakeButton("Move Layer Down", 12, 198); down.Click += (_,__) => MoveLayer(-1); sidebar.Controls.Add(down);
            var save = MakeButton("Save Scene", 12, 244); save.Click += (_,__) => SaveScene(); sidebar.Controls.Add(save);
            var load = MakeButton("Load Scene", 12, 290); load.Click += (_,__) => LoadScene(); sidebar.Controls.Add(load);
            _list.Location = new Point(12, 350); _list.Size = new Size(220, 250); _list.BackColor = Color.FromArgb(42,35,58); _list.ForeColor = Color.White; _list.SelectedIndexChanged += (_,__) => _preview.Invalidate(); sidebar.Controls.Add(_list);

            _preview.Location = new Point(280, 85); _preview.Size = new Size(800, 600); _preview.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _preview.BackColor = Color.Black; _preview.Paint += DrawPreview; _preview.MouseDown += PreviewDown; _preview.MouseMove += PreviewMove; _preview.MouseUp += (_,__) => _drag = null; Controls.Add(_preview);
            var hint = new Label { Text = "Drag layers in the preview. Add images and text, then start the virtual camera.", AutoSize = true, ForeColor = Color.Silver, Location = new Point(282, 695), Anchor = AnchorStyles.Bottom | AnchorStyles.Left }; Controls.Add(hint);
            _layers.Add(new Layer { Name = "Welcome text", Text = "Bro Cam", Position = new PointF(100, 100), Size = new SizeF(650, 150), Color = Color.White });
            RefreshLayers();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            MFFunctions.MFStartup();
            _mfStarted = true;
            var hr = Functions.MFCreateVirtualCamera(
                __MIDL___MIDL_itf_mfvirtualcamera_0000_0000_0001.MFVirtualCameraType_SoftwareCameraSource,
                __MIDL___MIDL_itf_mfvirtualcamera_0000_0000_0002.MFVirtualCameraLifetime_Session,
                __MIDL___MIDL_itf_mfvirtualcamera_0000_0000_0003.MFVirtualCameraAccess_CurrentUser,
                "Bro Cam Virtual Camera", "{" + Shared.CLSID_VCamNet + "}", null, 0, out var camera);
            if (hr.IsSuccess)
            {
                _camera = new ComObject<IMFVirtualCamera>(camera);
                hr = _camera.Object.Start(null);
            }
            if (hr.IsError)
            {
                MessageBox.Show(this, $"Could not start Bro Cam Virtual Camera: {hr} ({hr.Value})", "Bro Cam", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _status.Text = "Virtual camera registration failed";
            }
            else
            {
                _status.Text = "Bro Cam Virtual Camera is registered and ready";
            }
        }

        private static void Style(Button b) { b.FlatStyle = FlatStyle.Flat; b.FlatAppearance.BorderSize = 0; b.BackColor = Color.FromArgb(112,63,180); b.ForeColor = Color.White; b.Font = new Font("Segoe UI", 9, FontStyle.Bold); }
        private Button MakeButton(string text, int x, int y) { var b = new Button { Text = text, Location = new Point(x,y), Size = new Size(220,38) }; Style(b); return b; }
        private void AddImage() { using var d = new OpenFileDialog { Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.gif" }; if (d.ShowDialog(this) != DialogResult.OK) return; using var im = Image.FromFile(d.FileName); _layers.Add(new Layer { Name = Path.GetFileName(d.FileName), Kind = "Image", Path = d.FileName, Size = new SizeF(Math.Min(600,im.Width), Math.Min(450,im.Height)) }); RefreshLayers(); }
        private void AddText() { var value = Microsoft.VisualBasic.Interaction.InputBox("Text to display:", "Add text", "New text"); if (string.IsNullOrWhiteSpace(value)) return; _layers.Add(new Layer { Name = value, Text = value }); RefreshLayers(); }
        private void RemoveLayer() { if (_list.SelectedIndex >= 0) { _layers.RemoveAt(_list.SelectedIndex); RefreshLayers(); } }
        private void MoveLayer(int delta) { int i = _list.SelectedIndex, j = i + delta; if (i < 0 || j < 0 || j >= _layers.Count) return; (_layers[i],_layers[j]) = (_layers[j],_layers[i]); RefreshLayers(); _list.SelectedIndex = j; }
        private void RefreshLayers() { _list.Items.Clear(); foreach (var l in _layers) _list.Items.Add(l.Name); _preview.Invalidate(); }
        private void PreviewDown(object? s, MouseEventArgs e) { var p = ToCanvas(e.Location); _drag = _layers.AsEnumerable().Reverse().FirstOrDefault(l => l.Visible && new RectangleF(l.Position,l.Size).Contains(p)); if (_drag != null) _dragOffset = new Point((int)(p.X-_drag.Position.X),(int)(p.Y-_drag.Position.Y)); }
        private void PreviewMove(object? s, MouseEventArgs e) { if (_drag == null || e.Button != MouseButtons.Left) return; var p = ToCanvas(e.Location); _drag.Position = new PointF(Math.Max(0,p.X-_dragOffset.X),Math.Max(0,p.Y-_dragOffset.Y)); _preview.Invalidate(); }
        private PointF ToCanvas(Point p) => new(p.X * W / Math.Max(1,_preview.ClientSize.Width), p.Y * H / Math.Max(1,_preview.ClientSize.Height));
        private void DrawPreview(object? s, PaintEventArgs e) { using var bmp = RenderScene(); e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic; e.Graphics.DrawImage(bmp,_preview.ClientRectangle); }
        private Bitmap RenderScene() { var bmp = new Bitmap(W,H,PixelFormat.Format32bppArgb); using var g = Graphics.FromImage(bmp); g.Clear(Color.FromArgb(24,18,38)); g.SmoothingMode = SmoothingMode.AntiAlias; foreach (var l in _layers) DrawLayer(g,l); return bmp; }
        private static void DrawLayer(Graphics g, Layer l) { if (!l.Visible) return; if (l.Kind == "Image" && l.Path != null && File.Exists(l.Path)) { using var im = Image.FromFile(l.Path); g.DrawImage(im,new RectangleF(l.Position,l.Size)); } else { using var f = new Font("Segoe UI", Math.Max(18,l.Size.Height/3), FontStyle.Bold); using var b = new SolidBrush(l.Color); g.DrawString(l.Text,f,b,new RectangleF(l.Position,l.Size)); } }
        private void SaveScene() { using var d = new SaveFileDialog { Filter = "Bro Cam Scene|*.brocam", FileName = "Scene.brocam" }; if(d.ShowDialog(this)!=DialogResult.OK)return; var lines=_layers.Select(l=>string.Join("|",l.Kind,Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(l.Name)),Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(l.Text)),Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(l.Path??"")),l.Position.X,l.Position.Y,l.Size.Width,l.Size.Height,l.Color.ToArgb())); File.WriteAllLines(d.FileName,lines); }
        private void LoadScene() { using var d=new OpenFileDialog { Filter="Bro Cam Scene|*.brocam" }; if(d.ShowDialog(this)!=DialogResult.OK)return; try { _layers.Clear(); foreach(var line in File.ReadAllLines(d.FileName)){var a=line.Split('|'); _layers.Add(new Layer { Kind=a[0],Name=System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(a[1])),Text=System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(a[2])),Path=System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(a[3])),Position=new PointF(float.Parse(a[4]),float.Parse(a[5])),Size=new SizeF(float.Parse(a[6]),float.Parse(a[7])),Color=Color.FromArgb(int.Parse(a[8])) }); } RefreshLayers(); } catch(Exception ex) { MessageBox.Show(this,ex.Message,"Could not load scene"); } }

        private void ToggleCamera() { if (_running) StopCamera(); else StartCamera(); }
        private void StartCamera() { try { _map=MemoryMappedFile.CreateOrOpen("BroCamFrame",W*H*4); _view=_map.CreateViewAccessor(); _timer=new System.Windows.Forms.Timer { Interval=33 }; _timer.Tick += (_,__) => PublishFrame(); _timer.Start(); _running=true; _toggle.Text="Stop Virtual Camera"; _status.Text="Publishing scene • start camera in your video app"; _status.ForeColor=Color.LightGreen; } catch(Exception ex) { StopCamera(); MessageBox.Show(this,ex.Message,"Bro Cam"); } }
        private void PublishFrame() { if(_view==null)return; using var bmp=RenderScene(); var data=bmp.LockBits(new Rectangle(0,0,W,H),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb); try { var bytes=new byte[W*H*4]; System.Runtime.InteropServices.Marshal.Copy(data.Scan0,bytes,0,bytes.Length); _view.WriteArray(0,bytes,0,bytes.Length); } finally { bmp.UnlockBits(data); } }
        private void StopCamera() { _timer?.Stop(); _timer?.Dispose(); _timer=null; _view?.Dispose(); _view=null; _map?.Dispose(); _map=null; _running=false; _toggle.Text="Start Virtual Camera"; _status.Text="Virtual camera stopped"; _status.ForeColor=Color.Gainsboro; }
        protected override void OnFormClosing(FormClosingEventArgs e) { StopCamera(); try { _camera?.Object.Remove(); } catch { } _camera?.Dispose(); if (_mfStarted) MFFunctions.MFShutdown(); base.OnFormClosing(e); }
    }
}