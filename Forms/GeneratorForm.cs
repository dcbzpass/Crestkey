using System;
using System.Drawing;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace Crestkey.Forms
{
    public class GeneratorForm : Form
    {
        public string GeneratedPassword { get; private set; }

        private TextBox _txtOutput;
        private TrackBar _trkLength;
        private Label _lblLength;
        private CheckBox _chkUpper;
        private CheckBox _chkLower;
        private CheckBox _chkDigits;
        private CheckBox _chkSymbols;
        private CheckBox _chkExcludeAmbiguous;
        private Label _lblEntropy;
        private Panel _entropyBar;
        private Panel _entropyFill;
        private Button _btnGenerate;
        private Button _btnUse;
        private Button _btnCancel;

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

        private void BuildUI()
        {
            Text = "Password Generator";
            Size = new Size(440, 420);
            MinimumSize = Size;
            MaximumSize = Size;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            BackColor = Color.FromArgb(18, 18, 18);
            ForeColor = Color.FromArgb(245, 245, 245);

            int x = 24;
            int y = 20;

            var lblOutput = MakeLabel("Generated Password", x, y);
            y += 22;

            _txtOutput = new TextBox
            {
                Location = new Point(x, y),
                Size = new Size(372, 30),
                BackColor = Color.FromArgb(28, 28, 28),
                ForeColor = Color.FromArgb(245, 245, 245),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 11f),
                ReadOnly = true
            };
            y += 38;

            var entropyRow = MakeLabel("Entropy", x, y);
            _lblEntropy = new Label
            {
                Text = "",
                AutoSize = true,
                Location = new Point(280, y),
                ForeColor = Color.FromArgb(150, 150, 150),
                Font = new Font("Segoe UI", 8f)
            };
            y += 18;

            _entropyBar = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(372, 8),
                BackColor = Color.FromArgb(35, 35, 35)
            };
            _entropyFill = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(0, 8),
                BackColor = Color.FromArgb(60, 120, 255)
            };
            _entropyBar.Controls.Add(_entropyFill);
            y += 22;

            var lblLen = MakeLabel("Length", x, y);
            _lblLength = new Label
            {
                Text = "16",
                AutoSize = true,
                Location = new Point(380, y),
                ForeColor = Color.FromArgb(200, 200, 200),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            y += 20;

            _trkLength = new TrackBar
            {
                Location = new Point(x, y),
                Size = new Size(372, 36),
                Minimum = 8,
                Maximum = 64,
                Value = 16,
                TickFrequency = 8,
                BackColor = Color.FromArgb(18, 18, 18)
            };
            _trkLength.ValueChanged += (s, e) =>
            {
                _lblLength.Text = _trkLength.Value.ToString();
                Regenerate();
            };
            y += 44;

            var lblCharsets = MakeLabel("Character Sets", x, y);
            y += 22;

            _chkUpper = MakeCheckbox("Uppercase  (A–Z)", x, y, true);
            _chkLower = MakeCheckbox("Lowercase  (a–z)", x + 190, y, true);
            y += 28;

            _chkDigits = MakeCheckbox("Digits  (0–9)", x, y, true);
            _chkSymbols = MakeCheckbox("Symbols  (!@#…)", x + 190, y, true);
            y += 28;

            _chkExcludeAmbiguous = MakeCheckbox("Exclude ambiguous characters  (0, O, l, 1…)", x, y, false);
            y += 36;

            Action onChange = Regenerate;
            _chkUpper.CheckedChanged += (s, e) => onChange();
            _chkLower.CheckedChanged += (s, e) => onChange();
            _chkDigits.CheckedChanged += (s, e) => onChange();
            _chkSymbols.CheckedChanged += (s, e) => onChange();
            _chkExcludeAmbiguous.CheckedChanged += (s, e) => onChange();

            _btnGenerate = new Button
            {
                Text = "↺  Regenerate",
                Location = new Point(x, y),
                Size = new Size(120, 32),
                BackColor = Color.FromArgb(32, 32, 32),
                ForeColor = Color.FromArgb(200, 200, 200),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand
            };
            _btnGenerate.FlatAppearance.BorderColor = Color.FromArgb(50, 50, 50);
            _btnGenerate.Click += (s, e) => Regenerate();

            _btnUse = new Button
            {
                Text = "Use Password",
                Location = new Point(x + 134, y),
                Size = new Size(120, 32),
                BackColor = Color.FromArgb(60, 120, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnUse.FlatAppearance.BorderSize = 0;
            _btnUse.Click += (s, e) =>
            {
                GeneratedPassword = _txtOutput.Text;
                DialogResult = DialogResult.OK;
            };

            _btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(x + 268, y),
                Size = new Size(82, 32),
                BackColor = Color.FromArgb(32, 32, 32),
                ForeColor = Color.FromArgb(180, 180, 180),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand,
                DialogResult = DialogResult.Cancel
            };
            _btnCancel.FlatAppearance.BorderColor = Color.FromArgb(50, 50, 50);

            Controls.AddRange(new Control[] {
                lblOutput, _txtOutput,
                entropyRow, _lblEntropy,
                _entropyBar,
                lblLen, _lblLength, _trkLength,
                lblCharsets,
                _chkUpper, _chkLower, _chkDigits, _chkSymbols,
                _chkExcludeAmbiguous,
                _btnGenerate, _btnUse, _btnCancel
            });
        }

        private void Regenerate()
        {
            string charset = BuildCharset();
            if (string.IsNullOrEmpty(charset))
            {
                _txtOutput.Text = "(select at least one character set)";
                _lblEntropy.Text = "";
                _entropyFill.Width = 0;
                return;
            }

            int length = _trkLength.Value;
            var sb = new StringBuilder(length);
            var rng = new RNGCryptoServiceProvider();

            byte[] buf = new byte[4];
            for (int i = 0; i < length; i++)
            {
                rng.GetBytes(buf);
                uint rand = BitConverter.ToUInt32(buf, 0);
                sb.Append(charset[(int)(rand % (uint)charset.Length)]);
            }

            _txtOutput.Text = sb.ToString();

            double entropy = length * Math.Log(charset.Length, 2);
            _lblEntropy.Text = $"{entropy:F1} bits";

            double ratio = Math.Min(entropy / 128.0, 1.0);
            _entropyFill.Width = (int)(_entropyBar.Width * ratio);
            _entropyFill.BackColor = entropy < 40
                ? Color.FromArgb(200, 60, 60)
                : entropy < 60
                    ? Color.FromArgb(200, 150, 40)
                    : entropy < 80
                        ? Color.FromArgb(60, 180, 80)
                        : Color.FromArgb(60, 120, 255);
        }

        private string BuildCharset()
        {
            var sb = new StringBuilder();
            if (_chkUpper.Checked) sb.Append(Upper);
            if (_chkLower.Checked) sb.Append(Lower);
            if (_chkDigits.Checked) sb.Append(Digits);
            if (_chkSymbols.Checked) sb.Append(Symbols);

            if (_chkExcludeAmbiguous.Checked)
            {
                var filtered = new StringBuilder();
                foreach (char c in sb.ToString())
                    if (!Ambiguous.Contains(c.ToString()))
                        filtered.Append(c);
                return filtered.ToString();
            }

            return sb.ToString();
        }

        private Label MakeLabel(string text, int x, int y)
        {
            return new Label
            {
                Text = text.ToUpper(),
                Location = new Point(x, y),
                AutoSize = true,
                ForeColor = Color.FromArgb(90, 90, 90),
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold)
            };
        }

        private CheckBox MakeCheckbox(string text, int x, int y, bool isChecked)
        {
            var chk = new CheckBox
            {
                Text = text,
                Location = new Point(x, y),
                AutoSize = true,
                Checked = isChecked,
                ForeColor = Color.FromArgb(200, 200, 200),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f)
            };
            chk.FlatAppearance.BorderColor = Color.FromArgb(60, 60, 60);
            return chk;
        }
    }
}