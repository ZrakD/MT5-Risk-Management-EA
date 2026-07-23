using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        // ── Tema ──────────────────────────────────────────────────
        private readonly Color BgBase = Color.FromArgb(8, 10, 15);
        private readonly Color BgSurface = Color.FromArgb(13, 16, 24);
        private readonly Color BgCard = Color.FromArgb(17, 21, 32);
        private readonly Color BgElevated = Color.FromArgb(22, 27, 42);
        private readonly Color BgHover = Color.FromArgb(28, 34, 52);
        private readonly Color BgInput = Color.FromArgb(12, 15, 23);

        private readonly Color AccentGreen = Color.FromArgb(50, 210, 120);
        private readonly Color AccentRed = Color.FromArgb(255, 75, 95);
        private readonly Color AccentAmber = Color.FromArgb(255, 185, 40);
        private readonly Color AccentBlue = Color.FromArgb(70, 160, 255);
        private readonly Color AccentPurple = Color.FromArgb(155, 100, 255);

        private readonly Color TextPrimary = Color.FromArgb(218, 226, 248);
        private readonly Color TextSecondary = Color.FromArgb(95, 108, 140);
        private readonly Color TextMuted = Color.FromArgb(45, 55, 80);
        private readonly Color BorderDim = Color.FromArgb(22, 255, 255, 255);
        private readonly Color BorderMid = Color.FromArgb(40, 255, 255, 255);

        // ── State ─────────────────────────────────────────────────
        private string activeNav = "Position Sizer";
        private string selectedPair = "EURUSD";
        private string tradeDir = "BUY";   // BUY / SELL
        private double accountBal = 10000.0;
        private double riskPct = 1.0;
        private double entryPrice = 1.08350;
        private double stopLoss = 1.08100;
        private double takeProfit = 1.08850;
        private double livePrice = 1.08350;
        private double lotSize = 0.0;
        private double riskAmount = 0.0;
        private double rewardAmount = 0.0;
        private double rrRatio = 0.0;
        private double pipValue = 10.0;
        private readonly Random rng = new Random();
        private System.Windows.Forms.Timer ticker;

        // ── Kontroller ────────────────────────────────────────────
        private Panel pnlTitle, pnlSidebar, pnlMain, pnlHeader, pnlContent, pnlStatus;
        private Label lblPrice, lblPriceChg, lblClock, lblStatusTxt;
        private Label lblLotResult, lblRiskAmt, lblRewardAmt, lblRR, lblPips, lblMargin;
        private TextBox txtBalance, txtRiskPct, txtEntry, txtSL, txtTP;
        private Button btnBuy, btnSell, btnCalc, btnTrade;
        private Panel pnlDirBuy, pnlDirSell;
        private ComboBox cmbPair;
        private Panel pnlRiskMeter, pnlResult, pnlTradeHistory;
        private ListView lvHistory;
        private TrackBar trkRisk;
        private Label lblRiskTrack;

        // ── Trade Geçmişi ─────────────────────────────────────────
        private class TradeRecord
        {
            public string Pair, Dir, Entry, SL, TP, Lot, Result, Time;
            public bool IsWin;
        }
        private readonly List<TradeRecord> history = new List<TradeRecord>
        {
            new TradeRecord { Pair="EURUSD", Dir="BUY",  Entry="1.08210", SL="1.07900", TP="1.08730", Lot="0.20", Result="+$124.00", IsWin=true,  Time="09:42" },
            new TradeRecord { Pair="GBPUSD", Dir="SELL", Entry="1.27350", SL="1.27620", TP="1.26810", Lot="0.15", Result="-$60.75",  IsWin=false, Time="11:15" },
            new TradeRecord { Pair="XAUUSD", Dir="BUY",  Entry="2341.50", SL="2335.00", TP="2354.00", Lot="0.05", Result="+$62.50",  IsWin=true,  Time="13:08" },
            new TradeRecord { Pair="EURUSD", Dir="BUY",  Entry="1.08480", SL="1.08200", TP="1.09040", Lot="0.18", Result="+$100.80", IsWin=true,  Time="14:55" },
        };

        public Form1()
        {
            BuildUI();
            Calculate();
            StartTicker();
        }

        // ─────────────────────────────────────────────────────────
        private void BuildUI()
        {
            this.Text = "AutoScripts · Risk Management EA";
            this.Size = new Size(1160, 740);
            this.MinimumSize = new Size(980, 640);
            this.BackColor = BgBase;
            this.ForeColor = TextPrimary;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9f);
            this.DoubleBuffered = true;
            this.FormBorderStyle = FormBorderStyle.None;

            BuildTitleBar();
            BuildSidebar();
            BuildMain();
            BuildStatusBar();
            this.Resize += (s, e) => Relayout();
            Relayout();
        }

        // ── TitleBar ──────────────────────────────────────────────
        private void BuildTitleBar()
        {
            pnlTitle = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = BgSurface };
            pnlTitle.Paint += TitleBar_Paint;
            pnlTitle.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    NativeMethods.ReleaseCapture();
                    NativeMethods.SendMessage(this.Handle, 0xA1, (IntPtr)0x2, IntPtr.Zero);
                }
            };

            var btnCl = MakeWinBtn(Color.FromArgb(255, 85, 75));
            var btnMn = MakeWinBtn(Color.FromArgb(255, 185, 40));
            var btnMx = MakeWinBtn(Color.FromArgb(35, 195, 55));
            btnCl.Click += (s, e) => this.Close();
            btnMn.Click += (s, e) => this.WindowState = FormWindowState.Minimized;
            btnMx.Click += (s, e) => this.WindowState =
                this.WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;
            pnlTitle.Controls.AddRange(new Control[] { btnMn, btnMx, btnCl });

            void Pos()
            {
                btnCl.Location = new Point(pnlTitle.Width - 26, 17);
                btnMx.Location = new Point(pnlTitle.Width - 46, 17);
                btnMn.Location = new Point(pnlTitle.Width - 66, 17);
            }
            Pos();
            pnlTitle.Resize += (s, e) => { Pos(); pnlTitle.Invalidate(); };
            this.Controls.Add(pnlTitle);
        }

        private void TitleBar_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // sol şerit (yeşil-kırmızı gradient)
            using (var br = new LinearGradientBrush(new Point(0, 0), new Point(4, 0), AccentGreen, AccentRed))
                g.FillRectangle(br, 0, 0, 4, pnlTitle.Height);

            // ikon — kalkan
            DrawShieldIcon(g, 18, 13, AccentGreen);

            using (var f = new Font("Segoe UI", 11f, FontStyle.Bold))
            using (var br = new SolidBrush(TextPrimary))
                g.DrawString("RISK MANAGEMENT", f, br, 46, 13);
            using (var f = new Font("Consolas", 8f))
            using (var br = new SolidBrush(TextMuted))
                g.DrawString("Position Sizer  ·  EA  ·  EarnForex Style", f, br, 48, 29);

            // balance pill
            int pw = pnlTitle.Width;
            DrawPill(g, pw - 280, 13, "BALANCE", $"${accountBal:N0}", AccentGreen);
            DrawPill(g, pw - 155, 13, "RISK", $"{riskPct:F1}%", AccentAmber);

            using (var pen = new Pen(BorderDim))
                g.DrawLine(pen, 0, 45, pnlTitle.Width, 45);
        }

        private void DrawShieldIcon(Graphics g, int x, int y, Color col)
        {
            var pts = new PointF[]
            {
                new PointF(x + 9, y),
                new PointF(x + 18, y + 4),
                new PointF(x + 18, y + 12),
                new PointF(x + 9, y + 20),
                new PointF(x, y + 12),
                new PointF(x, y + 4),
            };
            using (var br = new SolidBrush(Color.FromArgb(30, col.R, col.G, col.B)))
                g.FillPolygon(br, pts);
            using (var pen = new Pen(col, 1.5f))
                g.DrawPolygon(pen, pts);
            using (var f = new Font("Segoe UI", 8f, FontStyle.Bold))
            using (var br = new SolidBrush(col))
            {
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString("R", f, br, new RectangleF(x, y + 2, 18, 16), sf);
            }
        }

        private void DrawPill(Graphics g, int x, int y, string label, string val, Color col)
        {
            var r = new Rectangle(x, y, 115, 20);
            using (var br = new SolidBrush(Color.FromArgb(22, col.R, col.G, col.B)))
                g.FillRectangle(br, r);
            using (var pen = new Pen(Color.FromArgb(60, col.R, col.G, col.B)))
                g.DrawRectangle(pen, r);
            using (var f = new Font("Consolas", 7f))
            using (var br = new SolidBrush(TextMuted))
                g.DrawString(label, f, br, x + 7, y + 3);
            using (var f = new Font("Consolas", 9f, FontStyle.Bold))
            using (var br = new SolidBrush(col))
                g.DrawString(val, f, br, x + 60, y + 3);
        }

        private Panel MakeWinBtn(Color col)
        {
            var p = new Panel { Size = new Size(12, 12), BackColor = col, Cursor = Cursors.Hand };
            p.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var br = new SolidBrush(p.BackColor))
                    e.Graphics.FillEllipse(br, 0, 0, 11, 11);
            };
            p.MouseEnter += (s, e) => p.BackColor = ControlPaint.Light(col, 0.3f);
            p.MouseLeave += (s, e) => p.BackColor = col;
            return p;
        }

        // ── Sidebar ───────────────────────────────────────────────
        private void BuildSidebar()
        {
            pnlSidebar = new Panel { BackColor = BgSurface };
            pnlSidebar.Paint += (s, e) =>
            {
                using (var pen = new Pen(BorderDim))
                    e.Graphics.DrawLine(pen, pnlSidebar.Width - 1, 0, pnlSidebar.Width - 1, pnlSidebar.Height);
                // sol risk şeridi
                using (var br = new LinearGradientBrush(new Point(0, 0), new Point(0, pnlSidebar.Height),
                    Color.FromArgb(40, 50, 210, 120), Color.FromArgb(40, 255, 75, 95)))
                    e.Graphics.FillRectangle(br, 0, 0, 3, pnlSidebar.Height);
            };

            var items = new[]
            {
                new[]{ "MENU",       "" },
                new[]{ "▤",          "Library" },
                new[]{ "⌂",          "Home" },
                new[]{ "◈",          "Dashboard" },
                new[]{ "EA TOOLS",   "" },
                new[]{ "⊞",          "Position Sizer" },
                new[]{ "▦",          "Volume Profile" },
                new[]{ "🕒",          "Session" },
                new[]{ "◫",          "Supply/Demand" },
                new[]{ "SYSTEM",     "" },
                new[]{ "ℹ",          "About" },
                new[]{ "✉",          "Contact" },
            };

            int y = 12;
            foreach (var item in items)
            {
                string icon = item[0], label = item[1];
                if (label == "")
                {
                    pnlSidebar.Controls.Add(new Label
                    {
                        Text = icon,
                        Location = new Point(16, y),
                        Size = new Size(180, 18),
                        ForeColor = TextMuted,
                        Font = new Font("Consolas", 7.5f, FontStyle.Bold),
                        BackColor = Color.Transparent,
                    });
                    y += 22;
                }
                else
                {
                    string nav = label;
                    bool act = nav == activeNav;
                    var pnl = new Panel
                    {
                        Location = new Point(0, y),
                        Size = new Size(210, 34),
                        BackColor = act ? Color.FromArgb(20, 50, 210, 120) : Color.Transparent,
                        Cursor = Cursors.Hand,
                        Tag = nav,
                    };
                    pnl.Paint += (s, e) =>
                    {
                        if ((string)pnl.Tag == activeNav)
                            using (var pen = new Pen(AccentGreen, 2))
                                e.Graphics.DrawLine(pen, pnl.Width - 1, 0, pnl.Width - 1, pnl.Height);
                    };
                    pnl.MouseEnter += (s, e) => { if ((string)pnl.Tag != activeNav) pnl.BackColor = BgHover; };
                    pnl.MouseLeave += (s, e) => pnl.BackColor = (string)pnl.Tag == activeNav ? Color.FromArgb(20, 50, 210, 120) : Color.Transparent;

                    var icL = new Label { Text = icon, Location = new Point(14, 9), Size = new Size(18, 16), ForeColor = act ? AccentGreen : TextSecondary, Font = new Font("Segoe UI", 9f), BackColor = Color.Transparent, Tag = nav + "_ic" };
                    var txL = new Label { Text = nav, Location = new Point(36, 9), Size = new Size(128, 16), ForeColor = act ? AccentGreen : TextSecondary, Font = new Font("Segoe UI", 9f), BackColor = Color.Transparent, Tag = nav + "_tx" };

                    pnl.Click += (s, e) => SetNav(nav);
                    icL.Click += (s, e) => SetNav(nav);
                    txL.Click += (s, e) => SetNav(nav);
                    pnl.Controls.AddRange(new Control[] { icL, txL });

                    if (nav == "Position Sizer")
                    {
                        var badge = new Label { Text = "EA", Location = new Point(168, 10), Size = new Size(26, 14), BackColor = Color.FromArgb(50, 210, 120), ForeColor = BgBase, Font = new Font("Consolas", 7f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter };
                        badge.Click += (s, e) => SetNav(nav);
                        pnl.Controls.Add(badge);
                    }
                    pnlSidebar.Controls.Add(pnl);
                    y += 36;
                }
            }

            // stats footer
            var footer = new Panel { Size = new Size(210, 80), BackColor = Color.Transparent };
            footer.Paint += (s, e) => DrawSidebarFooter(e.Graphics);
            pnlSidebar.Controls.Add(footer);
            pnlSidebar.Resize += (s, e) => footer.Location = new Point(0, pnlSidebar.Height - 82);
            this.Controls.Add(pnlSidebar);
        }

        private void DrawSidebarFooter(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var pen = new Pen(BorderDim)) g.DrawLine(pen, 8, 0, 202, 0);

            // mini stats
            DrawMiniStat(g, 14, 8, "WIN RATE", "75%", AccentGreen);
            DrawMiniStat(g, 110, 8, "TRADES", "12", AccentBlue);
            DrawMiniStat(g, 14, 44, "PROFIT", "+$226.55", AccentGreen);
            DrawMiniStat(g, 110, 44, "LOSS", "-$60.75", AccentRed);
        }

        private void DrawMiniStat(Graphics g, int x, int y, string label, string val, Color col)
        {
            using (var f = new Font("Consolas", 7f))
            using (var br = new SolidBrush(TextMuted))
                g.DrawString(label, f, br, x, y);
            using (var f = new Font("Consolas", 9f, FontStyle.Bold))
            using (var br = new SolidBrush(col))
                g.DrawString(val, f, br, x, y + 13);
        }

        private void SetNav(string nav)
        {
            activeNav = nav;
            foreach (Control c in pnlSidebar.Controls)
            {
                if (!(c is Panel p) || !(p.Tag is string t)) continue;
                bool act = t == nav;
                p.BackColor = act ? Color.FromArgb(20, 50, 210, 120) : Color.Transparent;
                p.Invalidate();
                foreach (Control ch in p.Controls)
                {
                    if (!(ch is Label l) || l.Text == "EA") continue;
                    bool isThis = l.Tag is string lt && (lt == nav + "_ic" || lt == nav + "_tx");
                    l.ForeColor = isThis ? AccentGreen : TextSecondary;
                }
            }
        }

        // ── Main ──────────────────────────────────────────────────
        private void BuildMain()
        {
            pnlMain = new Panel { BackColor = BgBase };
            BuildHeader();
            BuildContent();
            this.Controls.Add(pnlMain);
        }

        private void BuildHeader()
        {
            pnlHeader = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = BgSurface };
            pnlHeader.Paint += (s, e) =>
            {
                var g = e.Graphics;
                // üst yeşil-kırmızı şerit
                int hw = pnlHeader.Width / 2;
                using (var br = new LinearGradientBrush(new Point(0, 0), new Point(hw, 0),
                    Color.FromArgb(70, 50, 210, 120), Color.Transparent))
                    g.FillRectangle(br, 0, 0, hw, 3);
                using (var br = new LinearGradientBrush(new Point(hw, 0), new Point(pnlHeader.Width, 0),
                    Color.Transparent, Color.FromArgb(70, 255, 75, 95)))
                    g.FillRectangle(br, hw, 0, hw, 3);
                using (var pen = new Pen(BorderDim))
                    g.DrawLine(pen, 0, 51, pnlHeader.Width, 51);
            };

            // Pair
            cmbPair = new ComboBox { Location = new Point(14, 15), Size = new Size(100, 22), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = BgElevated, ForeColor = TextPrimary, FlatStyle = FlatStyle.Flat, Font = new Font("Consolas", 10f, FontStyle.Bold) };
            cmbPair.Items.AddRange(new object[] { "EURUSD", "GBPUSD", "USDJPY", "XAUUSD", "AUDUSD", "USDCHF", "USDCAD" });
            cmbPair.SelectedIndex = 0;
            cmbPair.SelectedIndexChanged += (s, e) =>
            {
                selectedPair = cmbPair.SelectedItem.ToString();
                UpdatePairDefaults();
                Calculate();
            };

            lblPrice = new Label { Text = "1.08350", Location = new Point(124, 13), Size = new Size(105, 26), ForeColor = AccentGreen, Font = new Font("Consolas", 15f, FontStyle.Bold), BackColor = Color.Transparent };
            lblPriceChg = new Label { Text = "▲ +0.00021", Location = new Point(232, 19), Size = new Size(110, 16), ForeColor = AccentGreen, Font = new Font("Consolas", 9f), BackColor = Color.Transparent };

            // BUY / SELL seçici
            btnBuy = MakeDirBtn("▲  BUY", AccentGreen, true);
            btnSell = MakeDirBtn("▼  SELL", AccentRed, false);
            btnBuy.Click += (s, e) => SetDir("BUY");
            btnSell.Click += (s, e) => SetDir("SELL");

            pnlHeader.Controls.AddRange(new Control[] { cmbPair, lblPrice, lblPriceChg, btnBuy, btnSell });

            void PosH()
            {
                btnSell.Location = new Point(pnlHeader.Width - 106, 12);
                btnBuy.Location = new Point(pnlHeader.Width - 210, 12);
            }
            PosH();
            pnlHeader.Resize += (s, e) => PosH();
            pnlMain.Controls.Add(pnlHeader);
        }

        private Button MakeDirBtn(string text, Color col, bool active)
        {
            var b = new Button
            {
                Text = text,
                Size = new Size(98, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = active ? Color.FromArgb(30, col.R, col.G, col.B) : Color.Transparent,
                ForeColor = active ? col : TextSecondary,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
            };
            b.FlatAppearance.BorderColor = active ? Color.FromArgb(100, col.R, col.G, col.B) : Color.FromArgb(20, 255, 255, 255);
            b.FlatAppearance.BorderSize = 1;
            return b;
        }

        private void SetDir(string dir)
        {
            tradeDir = dir;
            // BUY butonu
            btnBuy.BackColor = dir == "BUY" ? Color.FromArgb(30, AccentGreen.R, AccentGreen.G, AccentGreen.B) : Color.Transparent;
            btnBuy.ForeColor = dir == "BUY" ? AccentGreen : TextSecondary;
            btnBuy.FlatAppearance.BorderColor = dir == "BUY" ? Color.FromArgb(100, AccentGreen.R, AccentGreen.G, AccentGreen.B) : Color.FromArgb(20, 255, 255, 255);
            // SELL butonu
            btnSell.BackColor = dir == "SELL" ? Color.FromArgb(30, AccentRed.R, AccentRed.G, AccentRed.B) : Color.Transparent;
            btnSell.ForeColor = dir == "SELL" ? AccentRed : TextSecondary;
            btnSell.FlatAppearance.BorderColor = dir == "SELL" ? Color.FromArgb(100, AccentRed.R, AccentRed.G, AccentRed.B) : Color.FromArgb(20, 255, 255, 255);
            Calculate();
        }

        // ── Content ───────────────────────────────────────────────
        private void BuildContent()
        {
            pnlContent = new Panel { BackColor = BgBase, AutoScroll = true };

            BuildInputPanel();
            BuildResultPanel();
            BuildRiskMeter();
            BuildTradeHistory();

            pnlMain.Controls.Add(pnlContent);
            pnlContent.Resize += (s, e) => LayoutContent();
            LayoutContent();
        }

        // ── Input Panel ───────────────────────────────────────────
        private Panel pnlInput;
        private void BuildInputPanel()
        {
            pnlInput = new Panel { BackColor = BgCard };
            pnlInput.Paint += (s, e) => PaintCardHeader(e.Graphics, pnlInput, "Trade Parameters", AccentGreen);

            int y = 34;

            // Account Balance
            AddInputRow(pnlInput, "Account Balance ($)", ref y, out txtBalance, "10000");
            txtBalance.TextChanged += (s, e) => { if (double.TryParse(txtBalance.Text, out double v)) { accountBal = v; pnlTitle?.Invalidate(); Calculate(); } };

            // Risk %
            AddInputRow(pnlInput, "Risk %", ref y, out txtRiskPct, "1.0");
            txtRiskPct.TextChanged += (s, e) => { if (double.TryParse(txtRiskPct.Text, out double v)) { riskPct = Math.Min(v, 10); trkRisk.Value = (int)(riskPct * 10); pnlTitle?.Invalidate(); Calculate(); } };

            // TrackBar
            trkRisk = new TrackBar { Location = new Point(10, y), Size = new Size(200, 30), Minimum = 1, Maximum = 100, Value = 10, TickFrequency = 10, BackColor = BgCard };
            trkRisk.Scroll += (s, e) => { riskPct = trkRisk.Value / 10.0; txtRiskPct.Text = riskPct.ToString("F1"); Calculate(); };
            lblRiskTrack = new Label { Location = new Point(216, y + 6), Size = new Size(60, 16), ForeColor = AccentAmber, Font = new Font("Consolas", 8.5f, FontStyle.Bold), BackColor = Color.Transparent, Text = "1.0%" };
            pnlInput.Controls.AddRange(new Control[] { trkRisk, lblRiskTrack });
            y += 36;

            // Entry
            AddInputRow(pnlInput, "Entry Price", ref y, out txtEntry, "1.08350");
            txtEntry.TextChanged += (s, e) => { if (double.TryParse(txtEntry.Text, out double v)) { entryPrice = v; Calculate(); } };

            // Stop Loss
            AddInputRow(pnlInput, "Stop Loss", ref y, out txtSL, "1.08100");
            txtSL.TextChanged += (s, e) => { if (double.TryParse(txtSL.Text, out double v)) { stopLoss = v; Calculate(); } };
            // SL hint
            var hintSL = new Label { Location = new Point(155, y - 18), Size = new Size(80, 14), ForeColor = AccentRed, Font = new Font("Consolas", 7.5f), BackColor = Color.Transparent };
            pnlInput.Controls.Add(hintSL);

            // Take Profit
            AddInputRow(pnlInput, "Take Profit", ref y, out txtTP, "1.08850");
            txtTP.TextChanged += (s, e) => { if (double.TryParse(txtTP.Text, out double v)) { takeProfit = v; Calculate(); } };

            // Calculate button
            btnCalc = new Button
            {
                Text = "⟳  CALCULATE",
                Location = new Point(10, y + 6),
                Size = new Size(230, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(22, 50, 210, 120),
                ForeColor = AccentGreen,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
            };
            btnCalc.FlatAppearance.BorderColor = Color.FromArgb(70, 50, 210, 120);
            btnCalc.Click += (s, e) => { Calculate(); FlashBtn(btnCalc, AccentGreen); };
            pnlInput.Controls.Add(btnCalc);

            pnlContent.Controls.Add(pnlInput);
        }

        private void AddInputRow(Panel parent, string label, ref int y, out TextBox txt, string defVal)
        {
            var lbl = new Label { Text = label, Location = new Point(10, y), Size = new Size(240, 15), ForeColor = TextSecondary, Font = new Font("Segoe UI", 8f), BackColor = Color.Transparent };
            parent.Controls.Add(lbl);
            y += 17;
            txt = new TextBox
            {
                Text = defVal,
                Location = new Point(10, y),
                Size = new Size(230, 22),
                BackColor = BgInput,
                ForeColor = TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 10f, FontStyle.Bold),
            };
            parent.Controls.Add(txt);
            y += 28;
        }

        // ── Result Panel ──────────────────────────────────────────
        private void BuildResultPanel()
        {
            pnlResult = new Panel { BackColor = BgCard };
            pnlResult.Paint += (s, e) => DrawResultPanel(e.Graphics, pnlResult);
            pnlContent.Controls.Add(pnlResult);
        }

        private void DrawResultPanel(Graphics g, Panel p)
        {
            int W = p.Width, H = p.Height;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            PaintCardHeader(g, p, "Calculation Results", AccentBlue);

            // LOT büyük gösterim
            var lotRect = new RectangleF(10, 32, W - 20, 60);
            using (var br = new SolidBrush(Color.FromArgb(15, AccentGreen.R, AccentGreen.G, AccentGreen.B)))
                g.FillRectangle(br, lotRect.X, lotRect.Y, lotRect.Width, lotRect.Height);
            using (var pen = new Pen(Color.FromArgb(40, AccentGreen.R, AccentGreen.G, AccentGreen.B)))
                g.DrawRectangle(pen, lotRect.X, lotRect.Y, lotRect.Width - 1, lotRect.Height - 1);

            using (var f = new Font("Consolas", 7.5f))
            using (var br = new SolidBrush(TextMuted))
                g.DrawString("LOT SIZE", f, br, 18, 38);
            using (var f = new Font("Consolas", 26f, FontStyle.Bold))
            using (var br = new SolidBrush(AccentGreen))
                g.DrawString(lotSize.ToString("F2"), f, br, 14, 50);
            using (var f = new Font("Consolas", 8.5f))
            using (var br = new SolidBrush(TextSecondary))
                g.DrawString("lots", f, br, W - 52, 68);

            int ry = 102;
            DrawResultRow(g, W, ry, "Risk Amount", "$" + riskAmount.ToString("N2"), AccentRed);
            DrawResultRow(g, W, ry + 26, "Reward Amount", "$" + rewardAmount.ToString("N2"), AccentGreen);
            DrawResultRow(g, W, ry + 52, "Risk/Reward", "1 : " + rrRatio.ToString("F2"), rrRatio >= 2 ? AccentGreen : rrRatio >= 1 ? AccentAmber : AccentRed);
            DrawResultRow(g, W, ry + 78, "Stop Pips", Math.Abs(entryPrice - stopLoss < 0.1 ? (entryPrice - stopLoss) * 10000 : (entryPrice - stopLoss)).ToString("F0") + " pips", AccentRed);
            DrawResultRow(g, W, ry + 104, "Pip Value", "$" + pipValue.ToString("F2") + "/pip", TextPrimary);
            DrawResultRow(g, W, ry + 130, "Est. Margin", "$" + (lotSize * 1000).ToString("N2"), AccentPurple);

            // R:R bar
            int barY = ry + 162;
            using (var pen = new Pen(BorderDim))
                g.DrawLine(pen, 10, barY, W - 10, barY);
            using (var f = new Font("Consolas", 7.5f))
            using (var br = new SolidBrush(TextMuted))
                g.DrawString("R:R RATIO VISUAL", f, br, 10, barY + 4);

            int barBottom = barY + 20;
            float half = (W - 20) / 2f;
            float riskW = Math.Min(half, half * (float)(riskAmount / Math.Max(riskAmount + rewardAmount, 1)));
            float rewW = Math.Min(half * 2 - riskW, (W - 20) - riskW);

            using (var br = new SolidBrush(Color.FromArgb(130, AccentRed.R, AccentRed.G, AccentRed.B)))
                g.FillRectangle(br, 10, barBottom, riskW, 14);
            using (var br = new SolidBrush(Color.FromArgb(130, AccentGreen.R, AccentGreen.G, AccentGreen.B)))
                g.FillRectangle(br, 10 + riskW, barBottom, rewW, 14);

            using (var f = new Font("Consolas", 7f))
            {
                using (var br = new SolidBrush(AccentRed))
                    g.DrawString("RISK", f, br, 14, barBottom + 2);
                using (var br = new SolidBrush(AccentGreen))
                    g.DrawString("REWARD", f, br, 10 + riskW + 4, barBottom + 2);
            }
        }

        private void DrawResultRow(Graphics g, int W, int y, string label, string val, Color col)
        {
            using (var f = new Font("Segoe UI", 8.5f))
            using (var br = new SolidBrush(TextSecondary))
                g.DrawString(label, f, br, 10, y);
            using (var f = new Font("Consolas", 9.5f, FontStyle.Bold))
            using (var br = new SolidBrush(col))
            {
                var sf = new StringFormat { Alignment = StringAlignment.Far };
                g.DrawString(val, f, br, new RectangleF(0, y, W - 10, 20), sf);
            }
            using (var pen = new Pen(Color.FromArgb(8, 255, 255, 255)))
                g.DrawLine(pen, 10, y + 20, W - 10, y + 20);
        }

        // ── Risk Meter ────────────────────────────────────────────
        private void BuildRiskMeter()
        {
            pnlRiskMeter = new Panel { BackColor = BgCard };
            pnlRiskMeter.Paint += RiskMeter_Paint;

            // Trade butonu
            btnTrade = new Button
            {
                Text = "▶  OPEN TRADE",
                FlatStyle = FlatStyle.Flat,
                BackColor = tradeDir == "BUY" ? Color.FromArgb(40, 50, 210, 120) : Color.FromArgb(40, 255, 75, 95),
                ForeColor = tradeDir == "BUY" ? AccentGreen : AccentRed,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                Cursor = Cursors.Hand,
            };
            btnTrade.FlatAppearance.BorderColor = tradeDir == "BUY" ? Color.FromArgb(100, 50, 210, 120) : Color.FromArgb(100, 255, 75, 95);
            btnTrade.FlatAppearance.BorderSize = 1;
            btnTrade.Click += BtnTrade_Click;

            pnlRiskMeter.Controls.Add(btnTrade);
            pnlRiskMeter.Resize += (s, e) =>
            {
                btnTrade.Location = new Point(10, pnlRiskMeter.Height - 48);
                btnTrade.Size = new Size(pnlRiskMeter.Width - 20, 36);
            };
            pnlContent.Controls.Add(pnlRiskMeter);
        }

        private void RiskMeter_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            int W = pnlRiskMeter.Width, H = pnlRiskMeter.Height;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            PaintCardHeader(g, pnlRiskMeter, "Risk Meter", AccentAmber);

            // Gauge arka planı
            int gX = W / 2, gY = 80, gR = Math.Min(W / 2 - 20, 70);
            var gRect = new Rectangle(gX - gR, gY - gR, gR * 2, gR * 2);

            // arka yay (gri)
            using (var pen = new Pen(BgElevated, 12))
                g.DrawArc(pen, gRect, 180, 180);

            // risk renk yayı
            float pct = (float)Math.Min(riskPct / 5.0, 1.0);
            float sweep = 180 * pct;
            Color meterCol = riskPct <= 1.5 ? AccentGreen : riskPct <= 3 ? AccentAmber : AccentRed;
            using (var pen = new Pen(meterCol, 10))
                g.DrawArc(pen, gRect, 180, sweep);

            // ibre
            double angle = Math.PI + (Math.PI * pct);
            float nx = gX + (float)(Math.Cos(angle) * (gR - 8));
            float ny = gY + (float)(Math.Sin(angle) * (gR - 8));
            using (var pen = new Pen(TextPrimary, 2))
                g.DrawLine(pen, gX, gY, nx, ny);
            using (var br = new SolidBrush(TextPrimary))
                g.FillEllipse(br, gX - 4, gY - 4, 8, 8);

            // değer
            using (var f = new Font("Consolas", 18f, FontStyle.Bold))
            using (var br = new SolidBrush(meterCol))
            {
                var sf = new StringFormat { Alignment = StringAlignment.Center };
                g.DrawString(riskPct.ToString("F1") + "%", f, br, new RectangleF(0, gY + 4, W, 28), sf);
            }
            using (var f = new Font("Segoe UI", 8f))
            using (var br = new SolidBrush(TextMuted))
            {
                var sf = new StringFormat { Alignment = StringAlignment.Center };
                g.DrawString(riskPct <= 1.5 ? "LOW RISK" : riskPct <= 3 ? "MODERATE" : "HIGH RISK", f, br, new RectangleF(0, gY + 30, W, 16), sf);
            }

            // Gauge etiketleri
            using (var f = new Font("Consolas", 7f))
            {
                using (var br = new SolidBrush(AccentGreen)) g.DrawString("0%", f, br, gX - gR - 2, gY + 6);
                using (var br = new SolidBrush(AccentAmber)) g.DrawString("2.5%", f, br, gX - 14, gY - gR - 14);
                using (var br = new SolidBrush(AccentRed)) g.DrawString("5%", f, br, gX + gR - 14, gY + 6);
            }

            // Özet kutular
            int bY = gY + gR + 20;
            DrawMiniBox(g, 10, bY, W / 2 - 14, "Risk $", "$" + riskAmount.ToString("N2"), AccentRed);
            DrawMiniBox(g, W / 2 + 4, bY, W / 2 - 14, "Reward $", "$" + rewardAmount.ToString("N2"), AccentGreen);
        }

        private void DrawMiniBox(Graphics g, int x, int y, int w, string label, string val, Color col)
        {
            using (var br = new SolidBrush(Color.FromArgb(15, col.R, col.G, col.B)))
                g.FillRectangle(br, x, y, w, 36);
            using (var pen = new Pen(Color.FromArgb(35, col.R, col.G, col.B)))
                g.DrawRectangle(pen, x, y, w - 1, 35);
            using (var f = new Font("Consolas", 7f))
            using (var br = new SolidBrush(TextMuted))
                g.DrawString(label, f, br, x + 6, y + 4);
            using (var f = new Font("Consolas", 9f, FontStyle.Bold))
            using (var br = new SolidBrush(col))
                g.DrawString(val, f, br, x + 6, y + 17);
        }

        // ── Trade History ─────────────────────────────────────────
        private void BuildTradeHistory()
        {
            pnlTradeHistory = new Panel { BackColor = BgCard };
            pnlTradeHistory.Paint += (s, e) => PaintCardHeader(e.Graphics, pnlTradeHistory, "Trade History", AccentPurple);

            lvHistory = new ListView
            {
                View = View.Details,
                FullRowSelect = true,
                GridLines = false,
                BackColor = BgCard,
                ForeColor = TextPrimary,
                BorderStyle = BorderStyle.None,
                Font = new Font("Consolas", 8.5f),
                OwnerDraw = true,
            };
            lvHistory.Columns.Add("Pair", 65);
            lvHistory.Columns.Add("Dir", 45);
            lvHistory.Columns.Add("Entry", 70);
            lvHistory.Columns.Add("SL", 70);
            lvHistory.Columns.Add("TP", 70);
            lvHistory.Columns.Add("Lot", 45);
            lvHistory.Columns.Add("Result", 80);
            lvHistory.Columns.Add("Time", 45);

            foreach (var t in history) AddHistoryItem(t);

            lvHistory.DrawColumnHeader += (s, e) =>
            {
                e.Graphics.FillRectangle(new SolidBrush(BgSurface), e.Bounds);
                using (var pen = new Pen(BorderDim))
                    e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
                using (var f = new Font("Consolas", 7.5f, FontStyle.Bold))
                using (var br = new SolidBrush(TextMuted))
                    e.Graphics.DrawString(e.Header.Text.ToUpper(), f, br, e.Bounds.Left + 5, e.Bounds.Top + 5);
            };
            lvHistory.DrawItem += (s, e) => e.DrawBackground();
            lvHistory.DrawSubItem += History_DrawSubItem;

            pnlTradeHistory.Controls.Add(lvHistory);
            pnlTradeHistory.Resize += (s, e) =>
            {
                lvHistory.Location = new Point(0, 26);
                lvHistory.Size = new Size(pnlTradeHistory.Width, pnlTradeHistory.Height - 26);
            };
            pnlContent.Controls.Add(pnlTradeHistory);
        }

        private void AddHistoryItem(TradeRecord t)
        {
            var it = new ListViewItem(t.Pair) { Tag = t };
            it.SubItems.Add(t.Dir);
            it.SubItems.Add(t.Entry);
            it.SubItems.Add(t.SL);
            it.SubItems.Add(t.TP);
            it.SubItems.Add(t.Lot);
            it.SubItems.Add(t.Result);
            it.SubItems.Add(t.Time);
            lvHistory.Items.Insert(0, it);
        }

        private void History_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            if (!(e.Item.Tag is TradeRecord t)) return;
            var g = e.Graphics; var rc = e.Bounds;
            Color fg = TextPrimary;
            if (e.ColumnIndex == 1) fg = t.Dir == "BUY" ? AccentGreen : AccentRed;
            if (e.ColumnIndex == 6) fg = t.IsWin ? AccentGreen : AccentRed;
            if (e.ColumnIndex == 0) fg = AccentBlue;

            // zebra
            if (e.Item.Index % 2 == 0)
                using (var br = new SolidBrush(Color.FromArgb(8, 255, 255, 255)))
                    g.FillRectangle(br, rc);

            using (var f = new Font("Consolas", 8.5f))
            using (var br = new SolidBrush(fg))
                g.DrawString(e.SubItem.Text, f, br, rc.X + 5, rc.Y + 4);
            using (var pen = new Pen(Color.FromArgb(7, 255, 255, 255)))
                g.DrawLine(pen, rc.Left, rc.Bottom - 1, rc.Right, rc.Bottom - 1);
        }

        // ── Layout ────────────────────────────────────────────────
        private void LayoutContent()
        {
            if (pnlContent == null) return;
            int W = Math.Max(pnlContent.ClientSize.Width - 24, 300);
            int pad = 10;

            // Sol sütun: input (250px) | orta: result | sağ: risk meter
            int col1 = 250, col3 = 220;
            int col2 = W - col1 - col3 - pad * 2;

            int totalH = Math.Max(pnlContent.ClientSize.Height - 24, 400);
            int topH = (int)(totalH * 0.58);
            int botH = totalH - topH - pad;

            pnlInput.Location = new Point(12, 12);
            pnlInput.Size = new Size(col1, topH);

            pnlResult.Location = new Point(12 + col1 + pad, 12);
            pnlResult.Size = new Size(col2, topH);

            pnlRiskMeter.Location = new Point(12 + col1 + pad + col2 + pad, 12);
            pnlRiskMeter.Size = new Size(col3, topH);

            int row2Y = 12 + topH + pad;
            pnlTradeHistory.Location = new Point(12, row2Y);
            pnlTradeHistory.Size = new Size(W, botH);

            // trade buton pozisyonu
            btnTrade.Location = new Point(10, pnlRiskMeter.Height - 48);
            btnTrade.Size = new Size(pnlRiskMeter.Width - 20, 36);
        }

        // ── Hesaplama ─────────────────────────────────────────────
        private void Calculate()
        {
            double slPips = Math.Abs(entryPrice - stopLoss);
            double tpPips = Math.Abs(takeProfit - entryPrice);

            // XAUUSD için farklı pip değeri
            if (selectedPair == "XAUUSD") { pipValue = 1.0; slPips *= 10; tpPips *= 10; }
            else if (selectedPair == "USDJPY") { pipValue = 9.26; slPips *= 100; tpPips *= 100; }
            else { pipValue = 10.0; slPips *= 10000; tpPips *= 10000; }

            riskAmount = accountBal * riskPct / 100.0;
            lotSize = slPips > 0 ? riskAmount / (slPips * pipValue) : 0;
            lotSize = Math.Round(Math.Max(lotSize, 0), 2);
            rewardAmount = lotSize * tpPips * pipValue;
            rrRatio = riskAmount > 0 ? rewardAmount / riskAmount : 0;

            // label güncelle
            if (lblRiskTrack != null) lblRiskTrack.Text = riskPct.ToString("F1") + "%";

            // trade buton rengi
            if (btnTrade != null)
            {
                Color col = tradeDir == "BUY" ? AccentGreen : AccentRed;
                btnTrade.BackColor = Color.FromArgb(40, col.R, col.G, col.B);
                btnTrade.ForeColor = col;
                btnTrade.FlatAppearance.BorderColor = Color.FromArgb(100, col.R, col.G, col.B);
                btnTrade.Text = $"▶  OPEN {tradeDir}  —  {lotSize:F2} Lot";
            }

            pnlResult?.Invalidate();
            pnlRiskMeter?.Invalidate();
            pnlTitle?.Invalidate();
        }

        private void UpdatePairDefaults()
        {
            switch (selectedPair)
            {
                case "GBPUSD": entryPrice = 1.27350; stopLoss = 1.27050; takeProfit = 1.27950; livePrice = 1.27350; break;
                case "USDJPY": entryPrice = 149.50; stopLoss = 149.00; takeProfit = 150.50; livePrice = 149.50; break;
                case "XAUUSD": entryPrice = 2341.50; stopLoss = 2335.00; takeProfit = 2354.00; livePrice = 2341.50; break;
                case "AUDUSD": entryPrice = 0.65420; stopLoss = 0.65150; takeProfit = 0.65960; livePrice = 0.65420; break;
                default: entryPrice = 1.08350; stopLoss = 1.08100; takeProfit = 1.08850; livePrice = 1.08350; break;
            }
            if (txtEntry != null) txtEntry.Text = entryPrice.ToString("F5");
            if (txtSL != null) txtSL.Text = stopLoss.ToString("F5");
            if (txtTP != null) txtTP.Text = takeProfit.ToString("F5");
            if (lblPrice != null) lblPrice.Text = livePrice.ToString(selectedPair == "USDJPY" ? "F3" : selectedPair == "XAUUSD" ? "F2" : "F5");
        }

        // ── Open Trade ────────────────────────────────────────────
        private async void BtnTrade_Click(object sender, EventArgs e)
        {
            btnTrade.Text = "◌  Sending Order...";
            btnTrade.Enabled = false;
            await System.Threading.Tasks.Task.Delay(1400);

            // Yeni kayıt ekle
            bool win = rng.NextDouble() > 0.35;
            double pnl = win ? rewardAmount : -riskAmount;
            var rec = new TradeRecord
            {
                Pair = selectedPair,
                Dir = tradeDir,
                Entry = entryPrice.ToString("F5"),
                SL = stopLoss.ToString("F5"),
                TP = takeProfit.ToString("F5"),
                Lot = lotSize.ToString("F2"),
                Result = (pnl >= 0 ? "+" : "") + "$" + pnl.ToString("N2"),
                IsWin = win,
                Time = DateTime.Now.ToString("HH:mm"),
            };
            history.Insert(0, rec);
            AddHistoryItem(rec);

            if (lblStatusTxt != null) lblStatusTxt.Text = $"● {tradeDir} {selectedPair} {lotSize:F2} lot — Order sent";
            btnTrade.Text = "✓  Order Sent!";
            await System.Threading.Tasks.Task.Delay(2000);
            btnTrade.Enabled = true;
            Calculate(); // text güncelle
        }

        private async void FlashBtn(Button btn, Color col)
        {
            btn.BackColor = Color.FromArgb(60, col.R, col.G, col.B);
            await System.Threading.Tasks.Task.Delay(200);
            btn.BackColor = Color.FromArgb(22, col.R, col.G, col.B);
        }

        // ── Card Header ───────────────────────────────────────────
        private void PaintCardHeader(Graphics g, Panel p, string title, Color accent)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var pen = new Pen(Color.FromArgb(28, 255, 255, 255)))
                g.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
            // üst accent şeridi
            using (var br = new LinearGradientBrush(new Point(0, 0), new Point(p.Width / 2, 0),
                Color.FromArgb(60, accent.R, accent.G, accent.B), Color.Transparent))
                g.FillRectangle(br, 0, 0, p.Width / 2, 3);
            using (var br = new SolidBrush(Color.FromArgb(15, 255, 255, 255)))
                g.FillRectangle(br, 0, 3, p.Width, 22);
            using (var pen = new Pen(BorderDim))
                g.DrawLine(pen, 0, 25, p.Width, 25);
            using (var br = new SolidBrush(accent))
                g.FillEllipse(br, 8, 11, 4, 4);
            using (var f = new Font("Consolas", 7.5f, FontStyle.Bold))
            using (var br = new SolidBrush(TextMuted))
                g.DrawString(title.ToUpper(), f, br, 16, 8);
        }

        // ── Status Bar ────────────────────────────────────────────
        private void BuildStatusBar()
        {
            pnlStatus = new Panel { Dock = DockStyle.Bottom, Height = 26, BackColor = BgSurface };
            pnlStatus.Paint += (s, e) =>
            {
                var g = e.Graphics;
                using (var br = new LinearGradientBrush(new Point(0, 0), new Point(pnlStatus.Width, 0),
                    Color.FromArgb(40, AccentGreen.R, AccentGreen.G, AccentGreen.B),
                    Color.FromArgb(40, AccentRed.R, AccentRed.G, AccentRed.B)))
                    g.FillRectangle(br, 0, 0, pnlStatus.Width, 2);
                using (var pen = new Pen(BorderDim)) g.DrawLine(pen, 0, 2, pnlStatus.Width, 2);
                using (var f = new Font("Consolas", 8f))
                using (var br = new SolidBrush(TextMuted))
                {
                    g.DrawString($"Lot: {lotSize:F2}  ·  Risk: ${riskAmount:N2}  ·  R:R {rrRatio:F2}", f, br, 200, 7);
                    g.DrawString("EA v1.0.0", f, br, pnlStatus.Width - 75, 7);
                }
            };

            lblStatusTxt = new Label { Text = "● Ready", Location = new Point(14, 6), Size = new Size(180, 14), ForeColor = AccentGreen, Font = new Font("Consolas", 8f), BackColor = Color.Transparent };
            lblClock = new Label { Location = new Point(pnlStatus.Width - 230, 6), Size = new Size(200, 14), ForeColor = TextMuted, Font = new Font("Consolas", 8f), BackColor = Color.Transparent };
            pnlStatus.Controls.AddRange(new Control[] { lblStatusTxt, lblClock });
            pnlStatus.Resize += (s, e) => { lblClock.Location = new Point(pnlStatus.Width - 230, 6); pnlStatus.Invalidate(); };
            this.Controls.Add(pnlStatus);
        }

        // ── Relayout ──────────────────────────────────────────────
        private void Relayout()
        {
            int top = pnlTitle?.Height ?? 46;
            int bot = pnlStatus?.Height ?? 26;

            if (pnlSidebar != null) { pnlSidebar.Location = new Point(0, top); pnlSidebar.Size = new Size(210, this.ClientSize.Height - top - bot); }
            if (pnlMain != null) { pnlMain.Location = new Point(210, top); pnlMain.Size = new Size(this.ClientSize.Width - 210, this.ClientSize.Height - top - bot); }
            if (pnlContent != null && pnlHeader != null) { pnlContent.Location = new Point(0, pnlHeader.Height); pnlContent.Size = new Size(pnlMain.Width, pnlMain.Height - pnlHeader.Height); }
            LayoutContent();
        }

        // ── Ticker ────────────────────────────────────────────────
        private void StartTicker()
        {
            ticker = new System.Windows.Forms.Timer { Interval = 1700 };
            ticker.Tick += (s, e) =>
            {
                livePrice += (rng.NextDouble() - 0.49) * 0.00008;
                double chg = livePrice - entryPrice;
                if (lblPrice != null) lblPrice.Text = livePrice.ToString(selectedPair == "USDJPY" ? "F3" : selectedPair == "XAUUSD" ? "F2" : "F5");
                if (lblPriceChg != null) { lblPriceChg.Text = (chg >= 0 ? "▲ +" : "▼ ") + chg.ToString("F5"); lblPriceChg.ForeColor = chg >= 0 ? AccentGreen : AccentRed; }
                if (lblClock != null) lblClock.Text = DateTime.Now.ToString("HH:mm:ss  ·  dd.MM.yyyy");
                pnlStatus?.Invalidate();
            };
            ticker.Start();
        }

        protected override void OnFormClosed(FormClosedEventArgs e) { ticker?.Stop(); base.OnFormClosed(e); }
    }

    internal static class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")] public static extern bool ReleaseCapture();
        [System.Runtime.InteropServices.DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
    }
}
