using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace Crestkey.Forms
{
    public class GeneratorForm : Form
    {
        public string GeneratedPassword { get; private set; }

        static readonly Color C_BG = Color.FromArgb(12, 12, 14);
        static readonly Color C_SURFACE = Color.FromArgb(18, 18, 22);
        static readonly Color C_RAISED = Color.FromArgb(26, 26, 32);
        static readonly Color C_BORDER = Color.FromArgb(38, 38, 48);
        static readonly Color C_MUTED = Color.FromArgb(72, 72, 90);
        static readonly Color C_SUBTLE = Color.FromArgb(120, 120, 145);
        static readonly Color C_TEXT = Color.FromArgb(225, 225, 235);
        static readonly Color C_ACCENT = Color.FromArgb(99, 102, 241);
        static readonly Color C_GREEN = Color.FromArgb(52, 211, 153);
        static readonly Color C_AMBER = Color.FromArgb(251, 191, 36);
        static readonly Color C_RED = Color.FromArgb(248, 113, 113);

        private TextBox _txtOutput;
        private TrackBar _trkLength;
        private Label _lblLength, _lblEntropy, _lblStrength;
        private Panel _entropyFill, _entropyBar;
        private CheckBox _chkUpper, _chkLower, _chkDigits, _chkSymbols, _chkNoAmbig;
        private Button _btnRegen, _btnUse, _btnCancel;

        private const string Upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string Lower = "abcdefghijklmnopqrstuvwxyz";
        private const string Digits = "0123456789";
        private const string Symbols = "!@#$%^&*()-_=+[]{}|;:,.<>?";
        private const string Ambiguous = "0Oo1lI";

        public GeneratorForm()
        {
            BuildUI();
            Regenerate();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            DwmHelper.SetTitleBarColor(Handle, C_SURFACE);
        }

        private void BuildUI()
        {
            Text = "Password Generator";
            Size = new Size(460, 440);
            MinimumSize = Size;
            MaximumSize = Size;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            ShowIcon = false;
            ShowInTaskbar = false;
            BackColor = C_BG;
            ForeColor = C_TEXT;
            Font = new Font("Segoe UI", 9.5f);

            const int px = 24;
            const int fw = 392;
            int y = 24;

            // output field
            SmallLabel("Generated Password", px, y);
            y += 20;

            var outWrap = new Panel { Location = new Point(px, y), Size = new Size(fw, 36), BackColor = C_RAISED };
            outWrap.Paint += PaintRoundBorder;

            _txtOutput = new TextBox
            {
                Location = new Point(8, 8),
                Size = new Size(fw - 16, 20),
                BackColor = C_RAISED,
                ForeColor = C_TEXT,
                BorderStyle = BorderStyle.None,
                Font = new Font("Consolas", 11f),
                ReadOnly = true
            };
            outWrap.Controls.Add(_txtOutput);
            Controls.Add(outWrap);
            y += 46;

            // entropy bar
            var entropyRow = new Panel { Location = new Point(px, y), Size = new Size(fw, 18), BackColor = C_BG };
            _lblStrength = new Label { Location = new Point(0, 0), AutoSize = true, ForeColor = C_SUBTLE, Font = new Font("Segoe UI", 8f) };
            _lblEntropy = new Label { Location = new Point(fw - 70, 0), Size = new Size(70, 16), TextAlign = ContentAlignment.MiddleRight, ForeColor = C_MUTED, Font = new Font("Segoe UI", 8f) };
            entropyRow.Controls.AddRange(new Control[] { _lblStrength, _lblEntropy });
            Controls.Add(entropyRow);
            y += 22;

            _entropyBar = new Panel { Location = new Point(px, y), Size = new Size(fw, 5), BackColor = C_RAISED };
            _entropyFill = new Panel { Location = new Point(0, 0), Size = new Size(0, 5), BackColor = C_GREEN };
            _entropyBar.Controls.Add(_entropyFill);
            Controls.Add(_entropyBar);
            y += 18;

            // length
            var lenRow = new Panel { Location = new Point(px, y), Size = new Size(fw, 20), BackColor = C_BG };
            SmallLabel("Length", 0, 0, lenRow);
            _lblLength = new Label { Location = new Point(fw - 30, 0), Size = new Size(30, 16), TextAlign = ContentAlignment.MiddleRight, ForeColor = C_TEXT, Font = new Font("Segoe UI", 9f, FontStyle.Bold), Text = "16" };
            lenRow.Controls.Add(_lblLength);
            Controls.Add(lenRow);
            y += 22;

            _trkLength = new TrackBar
            {
                Location = new Point(px - 4, y),
                Size = new Size(fw + 8, 36),
                Minimum = 8,
                Maximum = 64,
                Value = 16,
                TickFrequency = 8,
                BackColor = C_BG
            };
            _trkLength.ValueChanged += (s, e) => { _lblLength.Text = _trkLength.Value.ToString(); Regenerate(); };
            Controls.Add(_trkLength);
            y += 42;

            // charsets
            SmallLabel("Character Sets", px, y);
            y += 20;

            _chkUpper = DarkCheck("Uppercase  (A–Z)", px, y);
            _chkLower = DarkCheck("Lowercase  (a–z)", px + 200, y);
            y += 30;
            _chkDigits = DarkCheck("Digits  (0–9)", px, y);
            _chkSymbols = DarkCheck("Symbols  (!@#…)", px + 200, y);
            y += 30;
            _chkNoAmbig = DarkCheck("Exclude ambiguous chars  (0, O, l, 1, I)", px, y);
            y += 38;

            Action regen = Regenerate;
            _chkUpper.CheckedChanged += (s, e) => regen();
            _chkLower.CheckedChanged += (s, e) => regen();
            _chkDigits.CheckedChanged += (s, e) => regen();
            _chkSymbols.CheckedChanged += (s, e) => regen();
            _chkNoAmbig.CheckedChanged += (s, e) => regen();

            // buttons
            _btnRegen = new Button
            {
                Text = "↺  Regenerate",
                Location = new Point(px, y),
                Size = new Size(120, 32),
                BackColor = C_RAISED,
                ForeColor = C_SUBTLE,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand
            };
            _btnRegen.FlatAppearance.BorderColor = C_BORDER;
            _btnRegen.Click += (s, e) => Regenerate();

            _btnUse = new Button
            {
                Text = "Use Password",
                Location = new Point(px + 128, y),
                Size = new Size(130, 32),
                BackColor = C_ACCENT,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnUse.FlatAppearance.BorderSize = 0;
            _btnUse.Click += (s, e) => { GeneratedPassword = _txtOutput.Text; DialogResult = DialogResult.OK; };

            _btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(px + 266, y),
                Size = new Size(90, 32),
                BackColor = C_RAISED,
                ForeColor = C_SUBTLE,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand,
                DialogResult = DialogResult.Cancel
            };
            _btnCancel.FlatAppearance.BorderColor = C_BORDER;

            Controls.AddRange(new Control[] { _btnRegen, _btnUse, _btnCancel });
        }

        private void Regenerate()
        {
            string charset = BuildCharset();
            if (string.IsNullOrEmpty(charset))
            {
                _txtOutput.Text = "(select at least one character set)";
                _txtOutput.ForeColor = C_MUTED;
                _lblEntropy.Text = "";
                _lblStrength.Text = "";
                _entropyFill.Width = 0;
                return;
            }

            _txtOutput.ForeColor = C_TEXT;
            int len = _trkLength.Value;
            var sb = new StringBuilder(len);
            var rng = new RNGCryptoServiceProvider();
            byte[] buf = new byte[4];
            for (int i = 0; i < len; i++)
            {
                rng.GetBytes(buf);
                uint rand = BitConverter.ToUInt32(buf, 0);
                sb.Append(charset[(int)(rand % (uint)charset.Length)]);
            }
            _txtOutput.Text = sb.ToString();

            double entropy = len * Math.Log(charset.Length, 2);
            _lblEntropy.Text = $"{entropy:F0} bits";

            string strength; Color fillColor;
            if (entropy < 40) { strength = "Weak"; fillColor = C_RED; }
            else if (entropy < 60) { strength = "Fair"; fillColor = C_AMBER; }
            else if (entropy < 80) { strength = "Good"; fillColor = C_GREEN; }
            else if (entropy < 100) { strength = "Strong"; fillColor = C_GREEN; }
            else { strength = "Very strong"; fillColor = C_ACCENT; }

            _lblStrength.Text = strength;
            _lblStrength.ForeColor = fillColor;
            _entropyFill.BackColor = fillColor;
            _entropyFill.Width = (int)(_entropyBar.Width * Math.Min(entropy / 128.0, 1.0));
        }

        private string BuildCharset()
        {
            var sb = new StringBuilder();
            if (_chkUpper.Checked) sb.Append(Upper);
            if (_chkLower.Checked) sb.Append(Lower);
            if (_chkDigits.Checked) sb.Append(Digits);
            if (_chkSymbols.Checked) sb.Append(Symbols);
            if (_chkNoAmbig.Checked)
            {
                var filtered = new StringBuilder();
                foreach (char c in sb.ToString())
                    if (!Ambiguous.Contains(c.ToString())) filtered.Append(c);
                return filtered.ToString();
            }
            return sb.ToString();
        }

        private void SmallLabel(string text, int x, int y, Control parent = null)
        {
            var lbl = new Label
            {
                Text = text.ToUpper(),
                Location = new Point(x, y),
                AutoSize = true,
                ForeColor = Color.FromArgb(80, 80, 100),
                Font = new Font("Segoe UI", 7f, FontStyle.Bold)
            };
            (parent ?? (Control)this).Controls.Add(lbl);
        }

        private CheckBox DarkCheck(string text, int x, int y)
        {
            var chk = new CheckBox
            {
                Text = text,
                Location = new Point(x, y),
                AutoSize = true,
                Checked = true,
                ForeColor = C_TEXT,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f)
            };
            chk.FlatAppearance.BorderColor = C_BORDER;
            Controls.Add(chk);
            return chk;
        }

        private static void PaintRoundBorder(object sender, PaintEventArgs e)
        {
            var p = sender as Panel; if (p == null) return;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var pen = new Pen(Color.FromArgb(50, 50, 65)))
            {
                var r = new Rectangle(0, 0, p.Width - 1, p.Height - 1);
                int rad = 6;
                var path = new GraphicsPath();
                path.AddArc(r.X, r.Y, rad, rad, 180, 90);
                path.AddArc(r.Right - rad, r.Y, rad, rad, 270, 90);
                path.AddArc(r.Right - rad, r.Bottom - rad, rad, rad, 0, 90);
                path.AddArc(r.X, r.Bottom - rad, rad, rad, 90, 90);
                path.CloseFigure();
                e.Graphics.DrawPath(pen, path);
            }
        }
    }
}