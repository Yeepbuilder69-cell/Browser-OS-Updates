using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using DirectN;
using VCamNetSampleSource;

namespace VCamNetSample
{
    public partial class Main : Form
    {
        private IComObject<IMFVirtualCamera>? _camera;
        private bool _mfStarted;
        private readonly Button _toggle;
        private readonly Label _status;

        public Main()
        {
            Text = "Bro Cam";
            Width = 520; Height = 300; MinimumSize = new Size(440, 260);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(20, 16, 31);
            ForeColor = Color.White;
            Controls.Add(new Label { Text = "BRO CAM", ForeColor = Color.FromArgb(190,145,255),
                Font = new Font("Segoe UI", 22, FontStyle.Bold), AutoSize = true, Location = new Point(28,24) });
            Controls.Add(new Label { Text = "Windows virtual camera", ForeColor = Color.Gainsboro,
                Font = new Font("Segoe UI", 10), AutoSize = true, Location = new Point(31,68) });
            _status = new Label { Text = "Virtual camera is stopped", ForeColor = Color.LightGray,
                Font = new Font("Segoe UI", 11), AutoSize = true, Location = new Point(32,120) };
            Controls.Add(_status);
            _toggle = new Button { Text = "Start Virtual Camera", Location = new Point(30,165),
                Size = new Size(220,44), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(112,63,180),
                ForeColor = Color.White, Font = new Font("Segoe UI",10,FontStyle.Bold) };
            _toggle.FlatAppearance.BorderSize = 0;
            _toggle.Click += (_,_) => { if (_camera == null) StartCamera(); else StopCamera(); };
            Controls.Add(_toggle);
            Controls.Add(new Label { Text = "Choose “Bro Cam Virtual Camera” in your video app.",
                ForeColor = Color.Silver, AutoSize = true, Location = new Point(31,225) });
        }

        private void StartCamera()
        {
            try
            {
                MFFunctions.MFStartup(); _mfStarted = true;
                var hr = Functions.MFCreateVirtualCamera(
                    __MIDL___MIDL_itf_mfvirtualcamera_0000_0000_0001.MFVirtualCameraType_SoftwareCameraSource,
                    __MIDL___MIDL_itf_mfvirtualcamera_0000_0000_0002.MFVirtualCameraLifetime_Session,
                    __MIDL___MIDL_itf_mfvirtualcamera_0000_0000_0003.MFVirtualCameraAccess_CurrentUser,
                    "Bro Cam Virtual Camera", "{" + Shared.CLSID_VCamNet + "}", null, 0, out var camera);
                if (hr.IsError) throw new Win32Exception(hr.Value);
                _camera = new ComObject<IMFVirtualCamera>(camera);
                hr = _camera.Object.Start(null);
                if (hr.IsError) throw new Win32Exception(hr.Value);
                _status.Text = "Virtual camera is running"; _status.ForeColor = Color.LightGreen;
                _toggle.Text = "Stop Virtual Camera";
            }
            catch (Exception ex)
            {
                StopCamera();
                MessageBox.Show(this, "Bro Cam could not start the virtual camera.\r\n\r\n" + ex.Message,
                    "Bro Cam", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void StopCamera()
        {
            try { _camera?.Object.Remove(); } catch { }
            _camera?.Dispose(); _camera = null;
            if (_mfStarted) { try { MFFunctions.MFShutdown(); } catch { } _mfStarted = false; }
            _status.Text = "Virtual camera is stopped"; _status.ForeColor = Color.LightGray;
            _toggle.Text = "Start Virtual Camera";
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        { StopCamera(); base.OnFormClosing(e); }
    }
}