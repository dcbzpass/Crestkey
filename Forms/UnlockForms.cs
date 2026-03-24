using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Crestkey.Core;

namespace Crestkey.Forms
{
    public class UnlockForm : Form
    {
        static readonly Color C_BG = Color.FromArgb(12, 12, 14);
        static readonly Color C_SURFACE = Color.FromArgb(18, 18, 22);
        static readonly Color C_RAISED = Color.FromArgb(26, 26, 32);
        static readonly Color C_BORDER = Color.FromArgb(38, 38, 48);
        static readonly Color C_MUTED = Color.FromArgb(72, 72, 90);
        static readonly Color C_TEXT = Color.FromArgb(225, 225, 235);
        static readonly Color C_ACCENT = Color.FromArgb(99, 102, 241);
        static readonly Color C_RED = Color.FromArgb(248, 113, 113);

        private TextBox _txtPassword;
        private Button _btnUnlock;
        private Label _lblError;
        private CheckBox _chkShow;

        public Vault UnlockedVault { get; private set; }

        public UnlockForm()
        {
            BuildUI();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            DwmHelper.SetTitleBarColor(Handle, C_SURFACE);
        }

        private void BuildUI()
        {
            bool exists = Vault.VaultExists();

            Text = "Crestkey";
            Size = new Size(420, 320);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            ShowIcon = false;
            BackColor = C_BG;
            ForeColor = C_TEXT;
            Font = new Font("Segoe UI", 9.5f);

            // header
            var lblTitle = new Label
            {
                Text = "Crestkey",
                Location = new Point(30, 26),
                AutoSize = true,
                ForeColor = C_TEXT,
                Font = new Font("Segoe UI", 22f, FontStyle.Bold)
            };

            var lblSub = new Label
            {
                Text = exists
                    ? "Enter your master password to unlock"
                    : "Choose a master password to get started",
                Location = new Point(32, 74),
                AutoSize = true,
                ForeColor = C_MUTED,
                Font = new Font("Segoe UI", 9f)
            };

            var divider = new Panel
            {
                Location = new Point(30, 100),
                Size = new Size(340, 1),
                BackColor = C_BORDER
            };

            // password label
            var lblPass = new Label
            {
                Text = "MASTER PASSWORD",
                Location = new Point(30, 114),
                AutoSize = true,
                ForeColor = C_MUTED,
                Font = new Font("Segoe UI", 7f, FontStyle.Bold)
            };

            // password field in rounded wrapper
            var fieldWrap = new Panel
            {
                Location = new Point(30, 132),
                Size = new Size(340, 36),
                BackColor = C_RAISED
            };
            fieldWrap.Paint += PaintRoundBorder;

            _txtPassword = new TextBox
            {
                Location = new Point(10, 8),
                Size = new Size(320, 20),
                UseSystemPasswordChar = true,
                BackColor = C_RAISED,
                ForeColor = C_TEXT,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10.5f)
            };
            _txtPassword.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) TryUnlock(); };
            fieldWrap.Controls.Add(_txtPassword);

            _chkShow = new CheckBox
            {
                Text = "Show password",
                Location = new Point(30, 176),
                AutoSize = true,
                ForeColor = C_MUTED,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f)
            };
            _chkShow.FlatAppearance.BorderColor = C_BORDER;
            _chkShow.CheckedChanged += (s, e) =>
                _txtPassword.UseSystemPasswordChar = !_chkShow.Checked;

            _lblError = new Label
            {
                Text = "",
                Location = new Point(30, 202),
                AutoSize = true,
                ForeColor = C_RED,
                Font = new Font("Segoe UI", 8.5f)
            };

            _btnUnlock = new Button
            {
                Text = exists ? "Unlock Vault" : "Create Vault",
                Location = new Point(30, 240),
                Size = new Size(340, 38),
                BackColor = C_ACCENT,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnUnlock.FlatAppearance.BorderSize = 0;
            _btnUnlock.Click += (s, e) => TryUnlock();

            Controls.AddRange(new Control[]
            {
                lblTitle, lblSub, divider,
                lblPass, fieldWrap, _chkShow, _lblError, _btnUnlock
            });
        }

        private void TryUnlock()
        {
            string pw = _txtPassword.Text;

            if (string.IsNullOrEmpty(pw))
            {
                _lblError.Text = "Password cannot be empty.";
                return;
            }

            if (!Vault.VaultExists())
            {
                if (pw.Length < 8)
                {
                    _lblError.Text = "Master password must be at least 8 characters.";
                    return;
                }
                UnlockedVault = Vault.CreateNew(pw);
                DialogResult = DialogResult.OK;
                return;
            }

            _btnUnlock.Enabled = false;
            _btnUnlock.Text = "Unlocking…";

            var (vault, _) = Vault.LoadRaw();
            if (vault.TryUnlock(pw))
            {
                UnlockedVault = vault;
                DialogResult = DialogResult.OK;
            }
            else
            {
                _lblError.Text = "Incorrect password. Please try again.";
                _btnUnlock.Enabled = true;
                _btnUnlock.Text = "Unlock Vault";
                _txtPassword.Clear();
                _txtPassword.Focus();
            }
        }

        private static void PaintRoundBorder(object sender, PaintEventArgs e)
        {
            var p = sender as Panel;
            if (p == null) return;
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