using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Crestkey.Core;

namespace Crestkey.Forms
{
    public class MainForm : Form
    {
        private Vault _vault;
        private Entry _selected;

        private Panel _sidebar;
        private Panel _listPanel;
        private Panel _detailPanel;
        private Panel _toolbar;

        private ListBox _lstEntries;
        private ListBox _lstCategories;

        private TextBox _txtSearch;
        private Button _btnAdd;
        private Button _btnDelete;
        private Button _btnLock;
        private Button _btnSave;
        private Button _btnGenerator;

        private TextBox _txtTitle;
        private TextBox _txtUsername;
        private TextBox _txtPassword;
        private TextBox _txtUrl;
        private TextBox _txtNotes;
        private TextBox _txtCategory;
        private TextBox _txtTotpSecret;
        private Button _btnCopyUser;
        private Button _btnCopyPass;
        private Button _btnTogglePass;
        private Button _btnCopyTotp;
        private Label _lblModified;
        private Label _lblClipStatus;
        private Label _lblTotpCode;
        private Label _lblTotpTimer;
        private System.Windows.Forms.Timer _totpTimer;

        private System.Windows.Forms.Timer _clipTimer;
        private IdleLock _idleLock;
        private bool _dirty;
        private const string SearchPlaceholder = "Search entries...";
        private const int IdleTimeoutSeconds = 300;

        public MainForm(Vault vault)
        {
            _vault = vault;
            BuildUI();
            RefreshCategories();
            RefreshList();
        }

        private void BuildUI()
        {
            Text = "Crestkey";
            Size = new Size(1000, 640);
            MinimumSize = new Size(900, 580);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(13, 13, 13);
            ForeColor = Color.FromArgb(245, 245, 245);

            _clipTimer = new System.Windows.Forms.Timer { Interval = 30000 };
            _clipTimer.Tick += (s, e) =>
            {
                Clipboard.Clear();
                _clipTimer.Stop();
                if (_lblClipStatus != null)
                    _lblClipStatus.Text = "";
            };

            _idleLock = new IdleLock(IdleTimeoutSeconds, () => Invoke((Action)LockVault));
            _idleLock.Reset();

            _totpTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _totpTimer.Tick += OnTotpTick;
            _totpTimer.Start();

            BuildToolbar();
            BuildSidebar();
            BuildListPanel();
            BuildDetailPanel();

            Controls.AddRange(new Control[] { _toolbar, _sidebar, _listPanel, _detailPanel });

            ResizeEnd += (s, e) => LayoutPanels();
            Resize += (s, e) =>
            {
                LayoutPanels();
                if (WindowState == FormWindowState.Minimized)
                    LockVault();
            };

            LayoutPanels();
        }

        private void LayoutPanels()
        {
            int tbH = 48;
            int sideW = 160;
            int detailW = 340;
            int h = ClientSize.Height - tbH;
            bool showDetail = _detailPanel != null && _detailPanel.Visible;
            int listW = ClientSize.Width - sideW - (showDetail ? detailW : 0);

            _toolbar.SetBounds(0, 0, ClientSize.Width, tbH);
            _sidebar.SetBounds(0, tbH, sideW, h);
            _listPanel.SetBounds(sideW, tbH, listW, h);
            if (showDetail)
                _detailPanel.SetBounds(sideW + listW, tbH, detailW, h);
        }

        private void BuildToolbar()
        {
            _toolbar = new Panel { BackColor = Color.FromArgb(20, 20, 20) };

            _txtSearch = new TextBox
            {
                Text = SearchPlaceholder,
                Location = new Point(12, 11),
                Size = new Size(220, 26),
                BackColor = Color.FromArgb(32, 32, 32),
                ForeColor = Color.FromArgb(100, 100, 100),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9f)
            };
            _txtSearch.GotFocus += (s, e) =>
            {
                if (_txtSearch.Text == SearchPlaceholder)
                {
                    _txtSearch.Text = "";
                    _txtSearch.ForeColor = Color.FromArgb(245, 245, 245);
                }
            };
            _txtSearch.LostFocus += (s, e) =>
            {
                if (string.IsNullOrEmpty(_txtSearch.Text))
                {
                    _txtSearch.Text = SearchPlaceholder;
                    _txtSearch.ForeColor = Color.FromArgb(100, 100, 100);
                }
            };
            _txtSearch.TextChanged += (s, e) =>
            {
                if (_txtSearch.Text != SearchPlaceholder) RefreshList();
            };

            _btnAdd = MakeToolButton("+ Add", 250);
            _btnAdd.Click += OnAdd;

            _btnDelete = MakeToolButton("Delete", 330);
            _btnDelete.ForeColor = Color.FromArgb(220, 80, 80);
            _btnDelete.Click += OnDelete;

            _btnSave = MakeToolButton("Save", 410);
            _btnSave.ForeColor = Color.FromArgb(80, 200, 120);
            _btnSave.Click += OnSave;

            _btnGenerator = MakeToolButton("Generator", 490);
            _btnGenerator.Click += OnGenerator;

            _btnLock = MakeToolButton("Lock", -1);
            _btnLock.Click += (s, e) => OnLock();

            _toolbar.Controls.AddRange(new Control[] {
                _txtSearch, _btnAdd, _btnDelete, _btnSave, _btnGenerator, _btnLock
            });

            _toolbar.Resize += (s, e) =>
            {
                _btnLock.Location = new Point(_toolbar.Width - 90, 10);
            };
        }

        private Button MakeToolButton(string text, int x)
        {
            var btn = new Button
            {
                Text = text,
                Size = new Size(72, 28),
                Location = x >= 0 ? new Point(x, 10) : new Point(0, 10),
                BackColor = Color.FromArgb(32, 32, 32),
                ForeColor = Color.FromArgb(245, 245, 245),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(50, 50, 50);
            return btn;
        }

        private void BuildSidebar()
        {
            _sidebar = new Panel { BackColor = Color.FromArgb(18, 18, 18) };

            var lblCat = new Label
            {
                Text = "CATEGORIES",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 100, 100),
                Location = new Point(12, 14),
                AutoSize = true
            };

            _lstCategories = new ListBox
            {
                Location = new Point(0, 36),
                Width = 160,
                BackColor = Color.FromArgb(18, 18, 18),
                ForeColor = Color.FromArgb(220, 220, 220),
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9.5f),
                ItemHeight = 28
            };
            _lstCategories.SelectedIndexChanged += (s, e) => RefreshList();
            _lstCategories.DrawMode = DrawMode.OwnerDrawFixed;
            _lstCategories.DrawItem += DrawCategoryItem;

            _sidebar.Controls.AddRange(new Control[] { lblCat, _lstCategories });
            _sidebar.Resize += (s, e) => _lstCategories.Height = _sidebar.Height - 36;
        }

        private void DrawCategoryItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            bool selected = (e.State & DrawItemState.Selected) != 0;
            e.Graphics.FillRectangle(
                new SolidBrush(selected ? Color.FromArgb(35, 35, 35) : Color.FromArgb(18, 18, 18)),
                e.Bounds
            );
            e.Graphics.DrawString(
                _lstCategories.Items[e.Index].ToString(),
                e.Font,
                new SolidBrush(selected ? Color.FromArgb(100, 160, 255) : Color.FromArgb(210, 210, 210)),
                new Point(e.Bounds.X + 14, e.Bounds.Y + 6)
            );
        }

        private void BuildListPanel()
        {
            _listPanel = new Panel { BackColor = Color.FromArgb(15, 15, 15) };

            _lstEntries = new ListBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(15, 15, 15),
                ForeColor = Color.FromArgb(220, 220, 220),
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9.5f),
                ItemHeight = 44,
                DrawMode = DrawMode.OwnerDrawFixed
            };
            _lstEntries.DrawItem += DrawEntryItem;
            _lstEntries.SelectedIndexChanged += OnEntrySelected;

            _listPanel.Controls.Add(_lstEntries);
        }

        private void DrawEntryItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            var entry = _lstEntries.Items[e.Index] as Entry;
            if (entry == null) return;

            bool selected = (e.State & DrawItemState.Selected) != 0;
            e.Graphics.FillRectangle(
                new SolidBrush(selected ? Color.FromArgb(28, 28, 28) : Color.FromArgb(15, 15, 15)),
                e.Bounds
            );

            if (selected)
                e.Graphics.FillRectangle(
                    new SolidBrush(Color.FromArgb(60, 120, 255)),
                    new Rectangle(e.Bounds.X, e.Bounds.Y, 3, e.Bounds.Height)
                );

            e.Graphics.DrawString(
                entry.Title,
                new Font("Segoe UI", 9.5f, FontStyle.Bold),
                new SolidBrush(Color.FromArgb(240, 240, 240)),
                new Point(e.Bounds.X + 14, e.Bounds.Y + 7)
            );
            e.Graphics.DrawString(
                entry.Username,
                new Font("Segoe UI", 8.5f),
                new SolidBrush(Color.FromArgb(120, 120, 120)),
                new Point(e.Bounds.X + 14, e.Bounds.Y + 26)
            );
            e.Graphics.DrawLine(
                new Pen(Color.FromArgb(24, 24, 24)),
                e.Bounds.Left, e.Bounds.Bottom - 1,
                e.Bounds.Right, e.Bounds.Bottom - 1
            );
        }

        private void BuildDetailPanel()
        {
            _detailPanel = new Panel
            {
                BackColor = Color.FromArgb(20, 20, 20),
                AutoScroll = true
            };

            int y = 20;
            int labelX = 20;
            int fieldX = 20;
            int fieldW = 280;

            _txtTitle = MakeDetailField("Title", ref y, labelX, fieldX, fieldW);
            _txtUsername = MakeDetailField("Username", ref y, labelX, fieldX, fieldW);

            var lblPass = MakeDetailLabel("Password", labelX, y);
            _detailPanel.Controls.Add(lblPass);
            y += 20;

            _txtPassword = new DarkTextBox
            {
                Location = new Point(fieldX, y),
                Size = new Size(fieldW - 60, 26),
                UseSystemPasswordChar = true,
                Font = new Font("Segoe UI", 9.5f)
            };
            _txtPassword.TextChanged += OnFieldChanged;

            _btnTogglePass = new Button
            {
                Text = "Show",
                Location = new Point(fieldX + fieldW - 54, y),
                Size = new Size(54, 26),
                BackColor = Color.FromArgb(32, 32, 32),
                ForeColor = Color.FromArgb(180, 180, 180),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8f),
                Cursor = Cursors.Hand
            };
            _btnTogglePass.FlatAppearance.BorderColor = Color.FromArgb(50, 50, 50);
            _btnTogglePass.Click += (s, e) =>
            {
                _txtPassword.UseSystemPasswordChar = !_txtPassword.UseSystemPasswordChar;
                _btnTogglePass.Text = _txtPassword.UseSystemPasswordChar ? "Show" : "Hide";
            };

            _btnCopyPass = MakeCopyButton("Copy Password", fieldX, y + 30);
            _btnCopyPass.Click += (s, e) => CopyToClipboard(_txtPassword.Text, "password");
            y += 66;

            _detailPanel.Controls.AddRange(new Control[] { _txtPassword, _btnTogglePass, _btnCopyPass });

            _txtUrl = MakeDetailField("URL", ref y, labelX, fieldX, fieldW);
            _txtCategory = MakeDetailField("Category", ref y, labelX, fieldX, fieldW);

            var lblTotp = MakeDetailLabel("2FA / TOTP Secret", labelX, y);
            _detailPanel.Controls.Add(lblTotp);
            y += 20;

            _txtTotpSecret = new DarkTextBox
            {
                Location = new Point(fieldX, y),
                Size = new Size(fieldW, 26),
                Font = new Font("Consolas", 9f)
            };
            _txtTotpSecret.TextChanged += OnFieldChanged;
            _detailPanel.Controls.Add(_txtTotpSecret);
            y += 30;

            _lblTotpCode = new Label
            {
                Text = "",
                Location = new Point(fieldX, y),
                Size = new Size(160, 28),
                ForeColor = Color.FromArgb(100, 200, 120),
                Font = new Font("Consolas", 16f, FontStyle.Bold)
            };
            _lblTotpTimer = new Label
            {
                Text = "",
                Location = new Point(fieldX + 168, y + 8),
                Size = new Size(60, 18),
                ForeColor = Color.FromArgb(100, 100, 100),
                Font = new Font("Segoe UI", 8f)
            };
            _btnCopyTotp = MakeCopyButton("Copy Code", fieldX + 168, y - 2);
            _btnCopyTotp.Size = new Size(100, 26);
            _btnCopyTotp.Click += (s, e) =>
            {
                if (!string.IsNullOrEmpty(_lblTotpCode.Text))
                    CopyToClipboard(_lblTotpCode.Text, "TOTP code");
            };
            _detailPanel.Controls.AddRange(new Control[] { _lblTotpCode, _lblTotpTimer, _btnCopyTotp });
            y += 36;

            var lblNotes = MakeDetailLabel("Notes", labelX, y);
            _detailPanel.Controls.Add(lblNotes);
            y += 20;

            _txtNotes = new DarkTextBox
            {
                Location = new Point(fieldX, y),
                Size = new Size(fieldW, 80),
                Multiline = true,
                Font = new Font("Segoe UI", 9.5f),
                ScrollBars = ScrollBars.Vertical
            };
            _txtNotes.TextChanged += OnFieldChanged;
            y += 88;

            _btnCopyUser = MakeCopyButton("Copy Username", fieldX, y);
            _btnCopyUser.Click += (s, e) => CopyToClipboard(_txtUsername.Text, "username");
            y += 36;

            _lblClipStatus = new Label
            {
                Text = "",
                Location = new Point(fieldX, y),
                Size = new Size(fieldW, 18),
                ForeColor = Color.FromArgb(80, 160, 80),
                Font = new Font("Segoe UI", 7.5f)
            };
            y += 22;

            _lblModified = new Label
            {
                Text = "",
                Location = new Point(fieldX, y),
                Size = new Size(fieldW, 18),
                ForeColor = Color.FromArgb(80, 80, 80),
                Font = new Font("Segoe UI", 7.5f)
            };

            _detailPanel.Controls.AddRange(new Control[] {
                _txtNotes, _btnCopyUser,
                _lblClipStatus, _lblModified
            });

            SetDetailEnabled(false);
        }

        private TextBox MakeDetailField(string label, ref int y, int labelX, int fieldX, int fieldW)
        {
            _detailPanel.Controls.Add(MakeDetailLabel(label, labelX, y));
            y += 20;

            var txt = new DarkTextBox
            {
                Location = new Point(fieldX, y),
                Size = new Size(fieldW, 26),
                Font = new Font("Segoe UI", 9.5f)
            };
            txt.TextChanged += OnFieldChanged;
            _detailPanel.Controls.Add(txt);
            y += 34;
            return txt;
        }

        private Label MakeDetailLabel(string text, int x, int y)
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

        private Button MakeCopyButton(string text, int x, int y)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(140, 26),
                BackColor = Color.FromArgb(32, 32, 32),
                ForeColor = Color.FromArgb(180, 180, 180),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(50, 50, 50);
            return btn;
        }

        private void SetDetailEnabled(bool enabled)
        {
            _detailPanel.Visible = enabled;
            LayoutPanels();
        }

        private void OnEntrySelected(object sender, EventArgs e)
        {
            if (_dirty) PromptSave();

            _selected = _lstEntries.SelectedItem as Entry;
            if (_selected == null)
            {
                SetDetailEnabled(false);
                ClearDetail();
                return;
            }

            SetDetailEnabled(true);
            PopulateDetail(_selected);
            _dirty = false;
        }

        private void PopulateDetail(Entry entry)
        {
            _txtTitle.Text = entry.Title;
            _txtUsername.Text = entry.Username;
            _txtPassword.Text = entry.Password;
            _txtUrl.Text = entry.Url;
            _txtNotes.Text = entry.Notes;
            _txtCategory.Text = entry.Category;
            _txtTotpSecret.Text = entry.TotpSecret ?? "";
            _lblModified.Text = "Modified " + entry.Modified.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
            _dirty = false;
            UpdateTotpDisplay();
        }

        private void ClearDetail()
        {
            _txtTitle.Text = "";
            _txtUsername.Text = "";
            _txtPassword.Text = "";
            _txtUrl.Text = "";
            _txtNotes.Text = "";
            _txtCategory.Text = "";
            _txtTotpSecret.Text = "";
            _lblTotpCode.Text = "";
            _lblTotpTimer.Text = "";
            _lblModified.Text = "";
            _lblClipStatus.Text = "";
        }

        private void OnFieldChanged(object sender, EventArgs e)
        {
            if (_selected != null) _dirty = true;
        }

        private void OnAdd(object sender, EventArgs e)
        {
            var entry = new Entry { Title = "New Entry" };
            _vault.Entries.Add(entry);
            RefreshCategories();
            RefreshList();
            _lstEntries.SelectedItem = entry;
        }

        private void OnDelete(object sender, EventArgs e)
        {
            if (_selected == null) return;
            if (MessageBox.Show($"Delete \"{_selected.Title}\"?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            _vault.Entries.Remove(_selected);
            _selected = null;
            _dirty = false;
            RefreshCategories();
            RefreshList();
            ClearDetail();
            SetDetailEnabled(false);
        }

        private void OnSave(object sender, EventArgs e)
        {
            if (_selected != null) CommitDetail();
            _vault.Save();
            RefreshCategories();
            RefreshList();
            MessageBox.Show("Vault saved.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void CommitDetail()
        {
            _selected.Title = _txtTitle.Text;
            _selected.Username = _txtUsername.Text;
            _selected.Password = _txtPassword.Text;
            _selected.Url = _txtUrl.Text;
            _selected.Notes = _txtNotes.Text;
            _selected.Category = string.IsNullOrWhiteSpace(_txtCategory.Text) ? "General" : _txtCategory.Text;
            _selected.TotpSecret = _txtTotpSecret.Text.Trim();
            _selected.Modified = DateTime.UtcNow;
            _dirty = false;
        }

        private void OnLock()
        {
            if (_dirty && MessageBox.Show("You have unsaved changes. Lock anyway?", "Unsaved", MessageBoxButtons.YesNo) != DialogResult.Yes)
                return;

            LockVault();
        }

        private void LockVault()
        {
            _idleLock.Stop();
            _clipTimer.Stop();
            Clipboard.Clear();
            _selected = null;
            _dirty = false;

            var unlock = new UnlockForm();
            Hide();
            if (unlock.ShowDialog() == DialogResult.OK)
            {
                _vault = unlock.UnlockedVault;
                RefreshCategories();
                RefreshList();
                ClearDetail();
                SetDetailEnabled(false);
                WindowState = FormWindowState.Normal;
                Show();
                _idleLock.Reset();
            }
            else
            {
                Application.Exit();
            }
        }

        private void OnGenerator(object sender, EventArgs e)
        {
            using (var gen = new GeneratorForm())
            {
                if (gen.ShowDialog() == DialogResult.OK && _selected != null)
                {
                    _txtPassword.Text = gen.GeneratedPassword;
                    _dirty = true;
                }
            }
        }

        private void OnTotpTick(object sender, EventArgs e)
        {
            UpdateTotpDisplay();
        }

        private void UpdateTotpDisplay()
        {
            if (_selected == null || string.IsNullOrWhiteSpace(_txtTotpSecret.Text))
            {
                _lblTotpCode.Text = "";
                _lblTotpTimer.Text = "";
                _btnCopyTotp.Visible = false;
                return;
            }

            if (!Totp.IsValidSecret(_txtTotpSecret.Text))
            {
                _lblTotpCode.Text = "Invalid";
                _lblTotpCode.ForeColor = Color.FromArgb(200, 80, 80);
                _lblTotpTimer.Text = "";
                _btnCopyTotp.Visible = false;
                return;
            }

            try
            {
                string code = Totp.Generate(_txtTotpSecret.Text);
                int secs = Totp.SecondsRemaining();
                _lblTotpCode.Text = code.Insert(3, " ");
                _lblTotpCode.ForeColor = secs <= 5
                    ? Color.FromArgb(220, 100, 60)
                    : Color.FromArgb(100, 200, 120);
                _lblTotpTimer.Text = $"{secs}s";
                _btnCopyTotp.Visible = true;
            }
            catch
            {
                _lblTotpCode.Text = "Error";
                _lblTotpCode.ForeColor = Color.FromArgb(200, 80, 80);
                _btnCopyTotp.Visible = false;
            }
        }

        private void PromptSave()
        {
            if (MessageBox.Show("Save changes to current entry?", "Unsaved Changes", MessageBoxButtons.YesNo) == DialogResult.Yes)
                CommitDetail();
        }

        private void CopyToClipboard(string text, string label)
        {
            if (string.IsNullOrEmpty(text)) return;
            Clipboard.SetText(text);
            _clipTimer.Stop();
            _clipTimer.Start();
            _lblClipStatus.Text = $"Copied {label} — clears in 30s";
        }

        private void RefreshCategories()
        {
            string current = _lstCategories.SelectedItem?.ToString();
            _lstCategories.Items.Clear();
            _lstCategories.Items.Add("All");

            var cats = _vault.Entries.Select(e => e.Category).Distinct().OrderBy(c => c);
            foreach (var c in cats)
                _lstCategories.Items.Add(c);

            if (current != null && _lstCategories.Items.Contains(current))
                _lstCategories.SelectedItem = current;
            else
                _lstCategories.SelectedIndex = 0;
        }

        private void RefreshList()
        {
            string search = _txtSearch.Text == SearchPlaceholder ? "" : _txtSearch.Text.ToLower();
            string cat = _lstCategories.SelectedItem?.ToString();

            var filtered = _vault.Entries.AsEnumerable();

            if (!string.IsNullOrEmpty(search))
                filtered = filtered.Where(e =>
                    e.Title.ToLower().Contains(search) ||
                    e.Username.ToLower().Contains(search) ||
                    e.Url.ToLower().Contains(search));

            if (cat != null && cat != "All")
                filtered = filtered.Where(e => e.Category == cat);

            _lstEntries.Items.Clear();
            foreach (var e in filtered.OrderBy(e => e.Title))
                _lstEntries.Items.Add(e);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_dirty)
            {
                var result = MessageBox.Show("Save changes before closing?", "Unsaved Changes", MessageBoxButtons.YesNoCancel);
                if (result == DialogResult.Cancel) { e.Cancel = true; return; }
                if (result == DialogResult.Yes) { CommitDetail(); _vault.Save(); }
            }
            _clipTimer.Stop();
            _idleLock.Stop();
            _totpTimer.Stop();
            Clipboard.Clear();
            base.OnFormClosing(e);
        }
    }

    internal class DarkTextBox : TextBox
    {
        private static readonly Color Back = Color.FromArgb(28, 28, 28);
        private static readonly Color Fore = Color.FromArgb(245, 245, 245);

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern IntPtr CreateSolidBrush(int color);
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern int SetBkColor(IntPtr hdc, int color);
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern int SetTextColor(IntPtr hdc, int color);
        private static int ToRef(Color c) => c.R | (c.G << 8) | (c.B << 16);

        private readonly IntPtr _brush;
        private ParentHook _hook;

        public DarkTextBox()
        {
            BackColor = Back;
            ForeColor = Fore;
            BorderStyle = BorderStyle.FixedSingle;
            _brush = CreateSolidBrush(ToRef(Back));
        }

        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            _hook?.ReleaseHandle();
            if (Parent != null)
                _hook = new ParentHook(Parent, this, _brush);
        }

        private class ParentHook : System.Windows.Forms.NativeWindow
        {
            private readonly DarkTextBox _owner;
            private readonly IntPtr _brush;

            public ParentHook(Control parent, DarkTextBox owner, IntPtr brush)
            {
                _owner = owner;
                _brush = brush;
                AssignHandle(parent.Handle);
            }

            protected override void WndProc(ref Message m)
            {
                if ((m.Msg == 0x0133 || m.Msg == 0x0138) && m.LParam == _owner.Handle)
                {
                    SetTextColor(m.WParam, ToRef(Fore));
                    SetBkColor(m.WParam, ToRef(Back));
                    m.Result = _brush;
                    return;
                }
                base.WndProc(ref m);
            }
        }
    }
}