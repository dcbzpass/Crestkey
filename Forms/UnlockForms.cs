using System;
using System.Drawing;
using System.Windows.Forms;
using Crestkey.Core;

namespace Crestkey.Forms
{
    public class UnlockForm : Form
    {
        private Label _lblTitle;
        private Label _lblSub;
        private TextBox _txtPassword;
        private Button _btnUnlock;
        private Label _lblError;
        private CheckBox _chkShow;

        public Vault UnlockedVault { get; private set; }

        public UnlockForm()
        {
            BuildUI();
        }

        private void BuildUI()
        {
            Text = "Crestkey";
            Size = new Size(400, 300);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            BackColor = Color.FromArgb(18, 18, 18);
            ForeColor = Color.FromArgb(245, 245, 245);

            _lblTitle = new Label
            {
                Text = "Crestkey",
                Font = new Font("Segoe UI", 20f, FontStyle.Bold),
                ForeColor = Color.FromArgb(245, 245, 245),
                AutoSize = true,
                Location = new Point(30, 30)
            };

            _lblSub = new Label
            {
                Text = Vault.VaultExists() ? "Enter your master password" : "Create a master password",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(150, 150, 150),
                AutoSize = true,
                Location = new Point(32, 70)
            };

            _txtPassword = new TextBox
            {
                Location = new Point(30, 100),
                Size = new Size(320, 30),
                UseSystemPasswordChar = true,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.FromArgb(245, 245, 245),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10f)
            };
            _txtPassword.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) TryUnlock(); };

            _chkShow = new CheckBox
            {
                Text = "Show password",
                Location = new Point(30, 135),
                AutoSize = true,
                ForeColor = Color.FromArgb(150, 150, 150),
                FlatStyle = FlatStyle.Flat
            };
            _chkShow.CheckedChanged += (s, e) => _txtPassword.UseSystemPasswordChar = !_chkShow.Checked;

            _lblError = new Label
            {
                Text = "",
                ForeColor = Color.FromArgb(220, 80, 80),
                AutoSize = true,
                Location = new Point(30, 163),
                Font = new Font("Segoe UI", 9f)
            };

            _btnUnlock = new Button
            {
                Text = Vault.VaultExists() ? "Unlock" : "Create Vault",
                Location = new Point(30, 190),
                Size = new Size(320, 36),
                BackColor = Color.FromArgb(60, 120, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnUnlock.FlatAppearance.BorderSize = 0;
            _btnUnlock.Click += (s, e) => TryUnlock();

            Controls.AddRange(new Control[] {
                _lblTitle, _lblSub, _txtPassword, _chkShow, _lblError, _btnUnlock
            });
        }

        private void TryUnlock()
        {
            string password = _txtPassword.Text;

            if (string.IsNullOrEmpty(password))
            {
                _lblError.Text = "Password cannot be empty.";
                return;
            }

            if (!Vault.VaultExists())
            {
                if (password.Length < 8)
                {
                    _lblError.Text = "Master password must be at least 8 characters.";
                    return;
                }
                UnlockedVault = Vault.CreateNew(password);
                DialogResult = DialogResult.OK;
                return;
            }

            _btnUnlock.Enabled = false;
            _btnUnlock.Text = "Unlocking...";

            var (vault, _) = Vault.LoadRaw();
            if (vault.TryUnlock(password))
            {
                UnlockedVault = vault;
                DialogResult = DialogResult.OK;
            }
            else
            {
                _lblError.Text = "Incorrect password.";
                _btnUnlock.Enabled = true;
                _btnUnlock.Text = "Unlock";
                _txtPassword.Clear();
                _txtPassword.Focus();
            }
        }
    }
}