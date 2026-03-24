using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using Crestkey.Core;

namespace Crestkey.Forms
{
    public class MainForm : Form
    {
        // ── Colours ────────────────────────────────────────────────────────────
        static readonly Color C_BG = Color.FromArgb(12, 12, 14);
        static readonly Color C_SURFACE = Color.FromArgb(18, 18, 22);
        static readonly Color C_RAISED = Color.FromArgb(26, 26, 32);
        static readonly Color C_BORDER = Color.FromArgb(38, 38, 48);
        static readonly Color C_MUTED = Color.FromArgb(72, 72, 90);
        static readonly Color C_SUBTLE = Color.FromArgb(120, 120, 145);
        static readonly Color C_TEXT = Color.FromArgb(225, 225, 235);
        static readonly Color C_ACCENT = Color.FromArgb(99, 102, 241);
        static readonly Color C_GREEN = Color.FromArgb(52, 211, 153);
        static readonly Color C_RED = Color.FromArgb(248, 113, 113);
        static readonly Color C_AMBER = Color.FromArgb(251, 191, 36);

        // ── State ──────────────────────────────────────────────────────────────
        private Vault _vault;
        private Entry _selected;
        private bool _dirty;
        private const string SearchPlaceholder = "Search entries…";
        private const int IdleTimeoutSeconds = 300;

        // ── Layout panels ──────────────────────────────────────────────────────
        private Panel _toolbar, _sidebar, _listPanel, _detailPanel;

        // ── Toolbar ────────────────────────────────────────────────────────────
        private TextBox _txtSearch;
        private Button _btnAdd, _btnDelete, _btnSave, _btnGenerator, _btnLock;
        private Label _lblStatus;

        // ── Sidebar ────────────────────────────────────────────────────────────
        private ListBox _lstCategories;

        // ── Entry list ─────────────────────────────────────────────────────────
        private ListBox _lstEntries;
        private Label _lblEmpty;

        // ── Detail panel ───────────────────────────────────────────────────────
        private Label _lblEntryTitle;
        private DarkTextBox _txtTitle, _txtUsername, _txtPassword,
                            _txtUrl, _txtNotes, _txtCategory, _txtTotpSecret;
        private Button _btnTogglePass, _btnCopyPass, _btnCopyUser, _btnCopyTotp;
        private Label _lblTotpCode, _lblTotpTimer, _lblModified, _lblClipStatus;

        // ── Timers ─────────────────────────────────────────────────────────────
        private System.Windows.Forms.Timer _clipTimer;
        private System.Windows.Forms.Timer _totpTimer;
        private IdleLock _idleLock;

        // ══════════════════════════════════════════════════════════════════════
        public MainForm(Vault vault)
        {
            _vault = vault;
            InitTimers();
            BuildUI();
            RefreshCategories();
            RefreshList();
        }

        private void InitTimers()
        {
            _clipTimer = new System.Windows.Forms.Timer { Interval = 30000 };
            _clipTimer.Tick += (s, e) => { Clipboard.Clear(); _clipTimer.Stop(); SetStatus(""); if (_lblClipStatus != null) _lblClipStatus.Text = ""; };

            _totpTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _totpTimer.Tick += (s, e) => UpdateTotpDisplay();

            _idleLock = new IdleLock(IdleTimeoutSeconds, () => { if (IsHandleCreated) Invoke((Action)LockVault); });
        }

        // ══════════════════════════════════════════════════════════════════════
        // BUILD UI
        // ══════════════════════════════════════════════════════════════════════
        private void BuildUI()
        {
            Text = "Crestkey";
            Size = new Size(1080, 680);
            MinimumSize = new Size(900, 580);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = C_BG;
            ForeColor = C_TEXT;
            Font = new Font("Segoe UI", 9.5f);

            BuildToolbar();
            BuildSidebar();
            BuildListPanel();
            BuildDetailPanel();

            Controls.AddRange(new Control[] { _toolbar, _sidebar, _listPanel, _detailPanel });
            Resize += (s, e) => { LayoutPanels(); if (WindowState == FormWindowState.Minimized) LockVault(); };
            ResizeEnd += (s, e) => LayoutPanels();
            Shown += (s, e) => { _totpTimer.Start(); _idleLock.Reset(); };
            LayoutPanels();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            DwmHelper.SetTitleBarColor(Handle, C_SURFACE);
        }

        private void LayoutPanels()
        {
            const int tbH = 56;
            const int sideW = 172;
            const int detailW = 360;
            int h = ClientSize.Height - tbH;
            bool show = _detailPanel != null && _detailPanel.Visible;
            int listW = ClientSize.Width - sideW - (show ? detailW : 0);

            _toolbar.SetBounds(0, 0, ClientSize.Width, tbH);
            _sidebar.SetBounds(0, tbH, sideW, h);
            _listPanel.SetBounds(sideW, tbH, listW, h);
            if (show) _detailPanel.SetBounds(sideW + listW, tbH, detailW, h);
        }

        // ── TOOLBAR ────────────────────────────────────────────────────────────
        private void BuildToolbar()
        {
            _toolbar = new Panel { BackColor = C_SURFACE };
            _toolbar.Paint += (s, e) =>
                e.Graphics.DrawLine(new Pen(C_BORDER), 0, _toolbar.Height - 1, _toolbar.Width, _toolbar.Height - 1);

            var searchWrap = new Panel { Location = new Point(16, 13), Size = new Size(240, 30), BackColor = C_RAISED };
            searchWrap.Paint += PaintRoundBorder;

            _txtSearch = new TextBox
            {
                Text = SearchPlaceholder,
                Location = new Point(10, 6),
                Size = new Size(216, 20),
                BackColor = C_RAISED,
                ForeColor = C_MUTED,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9f)
            };
            _txtSearch.GotFocus += (s, e) => { if (_txtSearch.Text == SearchPlaceholder) { _txtSearch.Text = ""; _txtSearch.ForeColor = C_TEXT; } };
            _txtSearch.LostFocus += (s, e) => { if (string.IsNullOrEmpty(_txtSearch.Text)) { _txtSearch.Text = SearchPlaceholder; _txtSearch.ForeColor = C_MUTED; } };
            _txtSearch.TextChanged += (s, e) => { if (_txtSearch.Text != SearchPlaceholder) RefreshList(); };
            searchWrap.Controls.Add(_txtSearch);
            _toolbar.Controls.Add(searchWrap);

            int bx = 272;
            _btnAdd = ToolBtn("＋  New Entry", ref bx, C_ACCENT, Color.White);
            _btnGenerator = ToolBtn("⚡  Generator", ref bx, C_RAISED, C_TEXT);
            _btnDelete = ToolBtn("✕  Delete", ref bx, C_RAISED, C_RED);
            _btnSave = ToolBtn("↑  Save", ref bx, C_RAISED, C_GREEN);

            _btnAdd.Click += OnAdd;
            _btnDelete.Click += OnDelete;
            _btnSave.Click += OnSave;
            _btnGenerator.Click += OnGenerator;

            _lblStatus = new Label
            {
                AutoSize = true,
                Location = new Point(bx + 8, 20),
                ForeColor = C_GREEN,
                Font = new Font("Segoe UI", 8.5f),
                Text = ""
            };
            _toolbar.Controls.Add(_lblStatus);

            _btnLock = new Button
            {
                Text = "🔒  Lock",
                Size = new Size(94, 30),
                BackColor = C_RAISED,
                ForeColor = C_SUBTLE,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f),
                Cursor = Cursors.Hand
            };
            _btnLock.FlatAppearance.BorderColor = C_BORDER;
            _btnLock.Click += (s, e) => OnLock();
            _toolbar.Controls.Add(_btnLock);
            _toolbar.Resize += (s, e) => _btnLock.Location = new Point(_toolbar.Width - 110, 13);
        }

        private Button ToolBtn(string text, ref int x, Color back, Color fore)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(x, 13),
                Size = new Size(112, 30),
                BackColor = back,
                ForeColor = fore,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = back == C_ACCENT ? C_ACCENT : C_BORDER;
            _toolbar.Controls.Add(btn);
            x += 120;
            return btn;
        }

        // ── SIDEBAR ────────────────────────────────────────────────────────────
        private void BuildSidebar()
        {
            _sidebar = new Panel { BackColor = C_SURFACE };
            _sidebar.Paint += (s, e) =>
                e.Graphics.DrawLine(new Pen(C_BORDER), _sidebar.Width - 1, 0, _sidebar.Width - 1, _sidebar.Height);

            _sidebar.Controls.Add(new Label
            {
                Text = "CATEGORIES",
                Location = new Point(16, 18),
                AutoSize = true,
                ForeColor = C_MUTED,
                Font = new Font("Segoe UI", 7f, FontStyle.Bold)
            });

            _lstCategories = new ListBox
            {
                Location = new Point(0, 42),
                Width = 172,
                BackColor = C_SURFACE,
                ForeColor = C_TEXT,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9.5f),
                ItemHeight = 32,
                DrawMode = DrawMode.OwnerDrawFixed
            };
            _lstCategories.DrawItem += DrawCategoryItem;
            _lstCategories.SelectedIndexChanged += (s, e) => RefreshList();
            _sidebar.Controls.Add(_lstCategories);
            _sidebar.Resize += (s, e) => _lstCategories.Height = _sidebar.Height - 42;
        }

        private void DrawCategoryItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            bool sel = (e.State & DrawItemState.Selected) != 0;
            var g = e.Graphics;
            g.FillRectangle(new SolidBrush(sel ? C_RAISED : C_SURFACE), e.Bounds);
            if (sel) g.FillRectangle(new SolidBrush(C_ACCENT), new Rectangle(0, e.Bounds.Y + 4, 3, e.Bounds.Height - 8));
            g.DrawString(_lstCategories.Items[e.Index].ToString(), e.Font,
                new SolidBrush(sel ? C_TEXT : C_SUBTLE), new PointF(18, e.Bounds.Y + 8));
        }

        // ── ENTRY LIST ─────────────────────────────────────────────────────────
        private void BuildListPanel()
        {
            _listPanel = new Panel { BackColor = C_BG };

            _lstEntries = new ListBox
            {
                Dock = DockStyle.Fill,
                BackColor = C_BG,
                ForeColor = C_TEXT,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9.5f),
                ItemHeight = 56,
                DrawMode = DrawMode.OwnerDrawFixed
            };
            _lstEntries.DrawItem += DrawEntryItem;
            _lstEntries.SelectedIndexChanged += OnEntrySelected;

            _lblEmpty = new Label
            {
                Text = "No entries yet.\nClick  ＋ New Entry  to get started.",
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = C_MUTED,
                Font = new Font("Segoe UI", 10f),
                Dock = DockStyle.Fill,
                Visible = false
            };

            _listPanel.Controls.Add(_lstEntries);
            _listPanel.Controls.Add(_lblEmpty);
        }

        private static readonly Color[] AvatarColors =
        {
            Color.FromArgb(99,  102, 241),
            Color.FromArgb(16,  185, 129),
            Color.FromArgb(245, 101, 101),
            Color.FromArgb(251, 191,  36),
            Color.FromArgb(59,  130, 246),
            Color.FromArgb(168,  85, 247),
            Color.FromArgb(236,  72, 153),
            Color.FromArgb(20,  184, 166),
        };

        private void DrawEntryItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            var entry = _lstEntries.Items[e.Index] as Entry;
            if (entry == null) return;

            bool sel = (e.State & DrawItemState.Selected) != 0;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.FillRectangle(new SolidBrush(sel ? C_RAISED : C_BG), e.Bounds);
            if (sel) g.FillRectangle(new SolidBrush(C_ACCENT), new Rectangle(0, e.Bounds.Y, 3, e.Bounds.Height));

            int ax = 16, ay = e.Bounds.Y + 12, aSize = 32;
            char init = string.IsNullOrEmpty(entry.Title) ? '?' : char.ToUpper(entry.Title[0]);
            var col = AvatarColors[Math.Abs(entry.Title.GetHashCode()) % AvatarColors.Length];
            g.FillEllipse(new SolidBrush(col), ax, ay, aSize, aSize);
            var iFont = new Font("Segoe UI", 11f, FontStyle.Bold);
            var iStr = init.ToString();
            var iSize = g.MeasureString(iStr, iFont);
            g.DrawString(iStr, iFont, Brushes.White, ax + (aSize - iSize.Width) / 2, ay + (aSize - iSize.Height) / 2);

            int tx = ax + aSize + 12;
            g.DrawString(string.IsNullOrEmpty(entry.Title) ? "Untitled" : entry.Title,
                new Font("Segoe UI", 9.5f, FontStyle.Bold), new SolidBrush(C_TEXT), tx, e.Bounds.Y + 11);
            g.DrawString(string.IsNullOrEmpty(entry.Username) ? entry.Url : entry.Username,
                new Font("Segoe UI", 8.5f), new SolidBrush(C_MUTED), tx, e.Bounds.Y + 31);
            g.DrawLine(new Pen(C_BORDER), 0, e.Bounds.Bottom - 1, e.Bounds.Width, e.Bounds.Bottom - 1);
        }

        // ── DETAIL PANEL ───────────────────────────────────────────────────────
        private void BuildDetailPanel()
        {
            _detailPanel = new Panel { BackColor = C_SURFACE, AutoScroll = true, Visible = false };
            _detailPanel.Paint += (s, e) =>
                e.Graphics.DrawLine(new Pen(C_BORDER), 0, 0, 0, _detailPanel.Height);

            int y = 0; const int px = 24; const int fw = 288;

            _lblEntryTitle = new Label
            {
                Location = new Point(px, 22),
                Size = new Size(fw, 28),
                ForeColor = C_TEXT,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                Text = ""
            };
            _detailPanel.Controls.Add(_lblEntryTitle);
            y = 60;

            _detailPanel.Controls.Add(new Panel { Location = new Point(px, y), Size = new Size(fw, 1), BackColor = C_BORDER });
            y += 14;

            _txtTitle = DetailField("Title", ref y, px, fw);
            _txtUsername = DetailField("Username", ref y, px, fw);

            SectionLabel("Password", px, y); y += 20;
            _txtPassword = new DarkTextBox { Location = new Point(px, y), Size = new Size(fw - 66, 28), UseSystemPasswordChar = true, Font = new Font("Segoe UI", 9.5f) };
            _txtPassword.TextChanged += OnFieldChanged;
            _btnTogglePass = DetailSmallBtn("Show", px + fw - 60, y, 60);
            _btnTogglePass.Click += (s, e) =>
            {
                _txtPassword.UseSystemPasswordChar = !_txtPassword.UseSystemPasswordChar;
                _btnTogglePass.Text = _txtPassword.UseSystemPasswordChar ? "Show" : "Hide";
            };
            _btnCopyPass = DetailWideBtn("Copy password", px, y + 34, fw);
            _btnCopyPass.Click += (s, e) => CopyToClipboard(_txtPassword.Text, "password");
            _detailPanel.Controls.AddRange(new Control[] { _txtPassword, _btnTogglePass, _btnCopyPass });
            y += 74;

            _txtUrl = DetailField("URL", ref y, px, fw);
            _txtCategory = DetailField("Category", ref y, px, fw);

            SectionLabel("2FA / TOTP Secret", px, y); y += 20;
            _txtTotpSecret = new DarkTextBox { Location = new Point(px, y), Size = new Size(fw, 28), Font = new Font("Consolas", 9f) };
            _txtTotpSecret.TextChanged += OnFieldChanged;
            _detailPanel.Controls.Add(_txtTotpSecret);
            y += 36;

            _lblTotpCode = new Label { Location = new Point(px, y), Size = new Size(148, 34), ForeColor = C_GREEN, Font = new Font("Consolas", 18f, FontStyle.Bold), Text = "" };
            _lblTotpTimer = new Label { Location = new Point(px + 152, y + 10), Size = new Size(40, 18), ForeColor = C_MUTED, Font = new Font("Segoe UI", 8f), Text = "" };
            _btnCopyTotp = DetailSmallBtn("Copy", px + 198, y + 4, 70);
            _btnCopyTotp.Visible = false;
            _btnCopyTotp.Click += (s, e) => { if (!string.IsNullOrEmpty(_lblTotpCode.Text)) CopyToClipboard(_lblTotpCode.Text, "TOTP code"); };
            _detailPanel.Controls.AddRange(new Control[] { _lblTotpCode, _lblTotpTimer, _btnCopyTotp });
            y += 46;

            SectionLabel("Notes", px, y); y += 20;
            _txtNotes = new DarkTextBox { Location = new Point(px, y), Size = new Size(fw, 90), Multiline = true, ScrollBars = ScrollBars.Vertical, Font = new Font("Segoe UI", 9.5f) };
            _txtNotes.TextChanged += OnFieldChanged;
            _detailPanel.Controls.Add(_txtNotes);
            y += 98;

            _btnCopyUser = DetailWideBtn("Copy username", px, y, fw);
            _btnCopyUser.Click += (s, e) => CopyToClipboard(_txtUsername.Text, "username");
            y += 40;

            _lblClipStatus = new Label { Location = new Point(px, y), Size = new Size(fw, 18), ForeColor = C_GREEN, Font = new Font("Segoe UI", 7.5f), Text = "" };
            y += 22;
            _lblModified = new Label { Location = new Point(px, y), Size = new Size(fw, 18), ForeColor = C_MUTED, Font = new Font("Segoe UI", 7.5f), Text = "" };
            _detailPanel.Controls.AddRange(new Control[] { _lblClipStatus, _lblModified });
        }

        private DarkTextBox DetailField(string label, ref int y, int x, int w)
        {
            SectionLabel(label, x, y);
            y += 20;
            var txt = new DarkTextBox { Location = new Point(x, y), Size = new Size(w, 28), Font = new Font("Segoe UI", 9.5f) };
            txt.TextChanged += OnFieldChanged;
            _detailPanel.Controls.Add(txt);
            y += 38;
            return txt;
        }

        private void SectionLabel(string text, int x, int y)
        {
            _detailPanel.Controls.Add(new Label
            {
                Text = text.ToUpper(),
                Location = new Point(x, y),
                AutoSize = true,
                ForeColor = Color.FromArgb(80, 80, 100),
                Font = new Font("Segoe UI", 7f, FontStyle.Bold)
            });
        }

        private Button DetailSmallBtn(string text, int x, int y, int w)
        {
            var btn = new Button { Text = text, Location = new Point(x, y), Size = new Size(w, 28), BackColor = C_RAISED, ForeColor = C_SUBTLE, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8f), Cursor = Cursors.Hand };
            btn.FlatAppearance.BorderColor = C_BORDER;
            _detailPanel.Controls.Add(btn);
            return btn;
        }

        private Button DetailWideBtn(string text, int x, int y, int w)
        {
            var btn = new Button { Text = text, Location = new Point(x, y), Size = new Size(w, 30), BackColor = C_RAISED, ForeColor = C_TEXT, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5f), Cursor = Cursors.Hand };
            btn.FlatAppearance.BorderColor = C_BORDER;
            _detailPanel.Controls.Add(btn);
            return btn;
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

        // ══════════════════════════════════════════════════════════════════════
        // DATA / INTERACTION
        // ══════════════════════════════════════════════════════════════════════

        private void SetDetailVisible(bool visible) { _detailPanel.Visible = visible; LayoutPanels(); }

        private void OnEntrySelected(object sender, EventArgs e)
        {
            if (_dirty) PromptSave();
            _selected = _lstEntries.SelectedItem as Entry;
            if (_selected == null) { SetDetailVisible(false); ClearDetail(); return; }
            SetDetailVisible(true);
            PopulateDetail(_selected);
            _dirty = false;
        }

        private void PopulateDetail(Entry entry)
        {
            _lblEntryTitle.Text = entry.Title;
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
            if (_lblEntryTitle != null) _lblEntryTitle.Text = "";
            if (_txtTitle != null) _txtTitle.Text = _txtUsername.Text = _txtPassword.Text = "";
            if (_txtUrl != null) _txtUrl.Text = _txtNotes.Text = _txtCategory.Text = "";
            if (_txtTotpSecret != null) _txtTotpSecret.Text = "";
            if (_lblTotpCode != null) _lblTotpCode.Text = "";
            if (_lblTotpTimer != null) _lblTotpTimer.Text = "";
            if (_lblModified != null) _lblModified.Text = "";
            if (_lblClipStatus != null) _lblClipStatus.Text = "";
            if (_btnCopyTotp != null) _btnCopyTotp.Visible = false;
        }

        private void OnFieldChanged(object sender, EventArgs e)
        {
            if (_selected == null) return;
            _dirty = true;
            if (sender == _txtTitle && _lblEntryTitle != null)
                _lblEntryTitle.Text = _txtTitle.Text;
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

        private void OnAdd(object sender, EventArgs e)
        {
            var entry = new Entry { Title = "New Entry", Category = "General" };
            _vault.Entries.Add(entry);
            RefreshCategories();
            RefreshList();
            _lstEntries.SelectedItem = entry;
            _txtTitle?.Focus();
            _txtTitle?.SelectAll();
        }

        private void OnDelete(object sender, EventArgs e)
        {
            if (_selected == null) return;
            if (MessageBox.Show($"Delete \"{_selected.Title}\"?", "Confirm Delete",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            _vault.Entries.Remove(_selected);
            _selected = null; _dirty = false;
            RefreshCategories(); RefreshList(); ClearDetail(); SetDetailVisible(false);
        }

        private void OnSave(object sender, EventArgs e)
        {
            if (_selected != null) CommitDetail();
            _vault.Save();
            RefreshCategories();
            RefreshList();
            SetStatus("Vault saved  ✓");
        }

        private void OnLock()
        {
            if (_dirty && MessageBox.Show("You have unsaved changes. Lock anyway?",
                    "Unsaved Changes", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            LockVault();
        }

        private void LockVault()
        {
            _idleLock.Stop(); _clipTimer.Stop(); _totpTimer.Stop(); Clipboard.Clear();
            _selected = null; _dirty = false;
            var unlock = new UnlockForm();
            Hide();
            if (unlock.ShowDialog() == DialogResult.OK)
            {
                _vault = unlock.UnlockedVault;
                RefreshCategories(); RefreshList(); ClearDetail(); SetDetailVisible(false);
                WindowState = FormWindowState.Normal;
                Show();
                _totpTimer.Start();
                _idleLock.Reset();
            }
            else Application.Exit();
        }

        private void OnGenerator(object sender, EventArgs e)
        {
            using (var gen = new GeneratorForm())
            {
                if (gen.ShowDialog() == DialogResult.OK && _selected != null)
                { _txtPassword.Text = gen.GeneratedPassword; _dirty = true; }
            }
        }

        private void PromptSave()
        {
            if (MessageBox.Show("Save changes to current entry?", "Unsaved Changes",
                    MessageBoxButtons.YesNo) == DialogResult.Yes) CommitDetail();
        }

        private void CopyToClipboard(string text, string label)
        {
            if (string.IsNullOrEmpty(text)) return;
            Clipboard.SetText(text);
            _clipTimer.Stop(); _clipTimer.Start();
            string msg = $"Copied {label} — clears in 30s";
            SetStatus(msg);
            if (_lblClipStatus != null) _lblClipStatus.Text = msg;
        }

        private void SetStatus(string msg)
        {
            if (_lblStatus == null) return;
            _lblStatus.Text = msg;
            _lblStatus.ForeColor = C_GREEN;
        }

        private void UpdateTotpDisplay()
        {
            if (_selected == null || _txtTotpSecret == null || string.IsNullOrWhiteSpace(_txtTotpSecret.Text))
            {
                if (_lblTotpCode != null) _lblTotpCode.Text = "";
                if (_lblTotpTimer != null) _lblTotpTimer.Text = "";
                if (_btnCopyTotp != null) _btnCopyTotp.Visible = false;
                return;
            }
            if (!Totp.IsValidSecret(_txtTotpSecret.Text))
            {
                _lblTotpCode.Text = "Invalid"; _lblTotpCode.ForeColor = C_RED;
                _lblTotpTimer.Text = ""; _btnCopyTotp.Visible = false; return;
            }
            try
            {
                string code = Totp.Generate(_txtTotpSecret.Text);
                int secs = Totp.SecondsRemaining();
                _lblTotpCode.Text = code.Insert(3, " ");
                _lblTotpCode.ForeColor = secs <= 5 ? C_AMBER : C_GREEN;
                _lblTotpTimer.Text = $"{secs}s";
                _btnCopyTotp.Visible = true;
            }
            catch { _lblTotpCode.Text = "Error"; _btnCopyTotp.Visible = false; }
        }

        private void RefreshCategories()
        {
            string cur = _lstCategories.SelectedItem?.ToString();
            _lstCategories.Items.Clear();
            _lstCategories.Items.Add("All entries");
            foreach (var c in _vault.Entries.Select(e => e.Category).Distinct().OrderBy(c => c))
                _lstCategories.Items.Add(c);

            int idx = 0;
            if (cur != null)
                for (int i = 0; i < _lstCategories.Items.Count; i++)
                    if (_lstCategories.Items[i].ToString() == cur) { idx = i; break; }
            _lstCategories.SelectedIndex = idx;
        }

        private void RefreshList()
        {
            if (_lstEntries == null || _lstCategories == null || _txtSearch == null) return;
            string search = _txtSearch.Text == SearchPlaceholder ? "" : _txtSearch.Text.ToLower();
            string cat = _lstCategories.SelectedItem?.ToString();
            var filtered = _vault.Entries.AsEnumerable();
            if (!string.IsNullOrEmpty(search))
                filtered = filtered.Where(e =>
                    e.Title.ToLower().Contains(search) ||
                    e.Username.ToLower().Contains(search) ||
                    e.Url.ToLower().Contains(search));
            if (cat != null && cat != "All entries")
                filtered = filtered.Where(e => e.Category == cat);

            _lstEntries.Items.Clear();
            foreach (var e in filtered.OrderBy(e => e.Title))
                _lstEntries.Items.Add(e);

            bool empty = _lstEntries.Items.Count == 0;
            _lblEmpty.Visible = empty;
            _lstEntries.Visible = !empty;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_dirty)
            {
                var r = MessageBox.Show("Save changes before closing?", "Unsaved Changes", MessageBoxButtons.YesNoCancel);
                if (r == DialogResult.Cancel) { e.Cancel = true; return; }
                if (r == DialogResult.Yes) { CommitDetail(); _vault.Save(); }
            }
            _clipTimer.Stop(); _idleLock.Stop(); _totpTimer.Stop();
            Clipboard.Clear();
            base.OnFormClosing(e);
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // DarkTextBox — forces white-on-dark via GDI parent hook
    // ══════════════════════════════════════════════════════════════════════════
    internal class DarkTextBox : TextBox
    {
        static readonly Color Back = Color.FromArgb(26, 26, 32);
        static readonly Color Fore = Color.FromArgb(225, 225, 235);

        [System.Runtime.InteropServices.DllImport("gdi32.dll")] static extern IntPtr CreateSolidBrush(int c);
        [System.Runtime.InteropServices.DllImport("gdi32.dll")] static extern int SetBkColor(IntPtr hdc, int c);
        [System.Runtime.InteropServices.DllImport("gdi32.dll")] static extern int SetTextColor(IntPtr hdc, int c);
        static int ToRef(Color c) => c.R | (c.G << 8) | (c.B << 16);

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
            if (Parent != null) _hook = new ParentHook(Parent, this, _brush);
        }

        private class ParentHook : System.Windows.Forms.NativeWindow
        {
            readonly DarkTextBox _owner;
            readonly IntPtr _brush;
            public ParentHook(Control parent, DarkTextBox owner, IntPtr brush)
            { _owner = owner; _brush = brush; AssignHandle(parent.Handle); }

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

    // ══════════════════════════════════════════════════════════════════════════
    // DwmHelper — colors the native Windows title bar via DWMAPI
    // ══════════════════════════════════════════════════════════════════════════
    internal static class DwmHelper
    {
        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_CAPTION_COLOR = 35;

        public static void SetTitleBarColor(IntPtr handle, Color color)
        {
            int colorRef = color.B << 16 | color.G << 8 | color.R;
            DwmSetWindowAttribute(handle, DWMWA_CAPTION_COLOR, ref colorRef, sizeof(int));
        }
    }
}