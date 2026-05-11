using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace _06_adder_visualizer;

public partial class MainWindow : Window
{
    const int BITS = 8;

    int _valA, _valB;
    int[] _bA = new int[BITS], _bB = new int[BITS];
    int[] _carry = new int[BITS + 1];
    int[] _sum   = new int[BITS];
    int _currentBit = -1;
    int _subStep = 0;

    Border[] _cellA = new Border[BITS], _cellB = new Border[BITS];
    Border[] _cellC = new Border[BITS + 1], _cellS = new Border[BITS];

    DispatcherTimer _autoTimer = new() { Interval = TimeSpan.FromMilliseconds(280) };

    static Color C(string h) => (Color)ColorConverter.ConvertFromString(h);
    static readonly Color CBlue   = C("#89DCEB");
    static readonly Color CGreen  = C("#A6E3A1");
    static readonly Color CPink   = C("#F38BA8");
    static readonly Color COrange = C("#FAB387");
    static readonly Color CYellow = C("#F9E2AF");
    static readonly Color CDim    = C("#2A2A3E");
    static readonly Color CFg     = C("#CDD6F4");
    static readonly Color CAccent = C("#89B4FA");
    static readonly Color CPurple = C("#CBA6F7");

    readonly Dictionary<string, (Point from, Point to, Color col, int val)> _wires = [];

    public MainWindow()
    {
        InitializeComponent();
        _autoTimer.Tick += (_, _) => AdvanceSubStep();
        BuildBitCells();
        GateCanvas.SizeChanged += (_, _) => RedrawCircuit();
        LogText.Text = "A と B を入力して ▶ Step または ⏩ Auto を押してください";
    }

    // ── Bit cell UI ──────────────────────────────────────────────

    void BuildBitCells()
    {
        BitsA.Items.Clear(); BitsB.Items.Clear(); BitsCarry.Items.Clear(); BitsSum.Items.Clear();
        for (int i = 0; i < BITS; i++)
        {
            _cellA[i] = MkCell("·", CBlue);   BitsA.Items.Add(_cellA[i]);
            _cellB[i] = MkCell("·", CGreen);  BitsB.Items.Add(_cellB[i]);
            _cellS[i] = MkCell("·", CPink);   BitsSum.Items.Add(_cellS[i]);
        }
        for (int i = 0; i <= BITS; i++) { _cellC[i] = MkCell("·", COrange); BitsCarry.Items.Add(_cellC[i]); }
    }

    Border MkCell(string t, Color fg) => new()
    {
        Child = new TextBlock { Text = t, FontSize = 17, TextAlignment = TextAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center, Width = 30,
            Foreground = Br(fg), FontFamily = new FontFamily("Consolas") },
        Width = 36, Height = 28, Margin = new Thickness(2, 1, 2, 1),
        Background = Br(CDim), CornerRadius = new CornerRadius(4)
    };

    void SetCell(Border cell, int val, Color fg, bool hi = false)
    {
        var tb = (TextBlock)cell.Child;
        tb.Text = val.ToString();
        tb.Foreground = Br(hi ? CYellow : fg);
        cell.Background = Br(hi ? C("#3D3200") : (val == 1 ? C("#252540") : CDim));
    }

    void MarkActive(int bi)
    {
        for (int i = 0; i < BITS; i++)
        {
            bool a = i == bi;
            _cellA[i].BorderThickness = new Thickness(a ? 2 : 0); _cellA[i].BorderBrush = Br(CBlue);
            _cellB[i].BorderThickness = new Thickness(a ? 2 : 0); _cellB[i].BorderBrush = Br(CGreen);
        }
    }

    // ── Buttons ───────────────────────────────────────────────────

    void BtnStep_Click(object s, RoutedEventArgs e)
    {
        _autoTimer.Stop(); BtnAuto.Content = "⏩ Auto";
        if (_currentBit == -1) Initialize(); else AdvanceSubStep();
    }

    void BtnAuto_Click(object s, RoutedEventArgs e)
    {
        if (_autoTimer.IsEnabled) { _autoTimer.Stop(); BtnAuto.Content = "⏩ Auto"; return; }
        if (_currentBit == -1) Initialize();
        BtnAuto.Content = "⏸ Pause"; _autoTimer.Start();
    }

    void BtnReset_Click(object s, RoutedEventArgs e)
    {
        _autoTimer.Stop(); BtnAuto.Content = "⏩ Auto"; _currentBit = -1; _subStep = 0;
        for (int i = 0; i < BITS; i++)
        {
            Reset(_cellA[i]); Reset(_cellB[i]); Reset(_cellS[i]);
        }
        for (int i = 0; i <= BITS; i++) Reset(_cellC[i]);
        ResultLabel.Text = ""; GateCanvas.Children.Clear();
        LogText.Text = "A と B を入力して ▶ Step または ⏩ Auto を押してください";
    }

    void Reset(Border c)
    {
        ((TextBlock)c.Child).Text = "·"; c.Background = Br(CDim);
        c.BorderThickness = new Thickness(0);
    }

    void Initialize()
    {
        if (!int.TryParse(InputA.Text, out _valA) || !int.TryParse(InputB.Text, out _valB)
            || _valA < 0 || _valA > 255 || _valB < 0 || _valB > 255)
        { LogText.Text = "0〜255 の整数を入力してください"; return; }

        for (int i = 0; i < BITS; i++) { _bA[i] = (_valA >> (BITS-1-i)) & 1; _bB[i] = (_valB >> (BITS-1-i)) & 1; }
        Array.Fill(_carry, 0); Array.Fill(_sum, 0);

        for (int i = 0; i < BITS; i++) { SetCell(_cellA[i], _bA[i], CBlue); SetCell(_cellB[i], _bB[i], CGreen); }
        for (int i = 0; i <= BITS; i++) Reset(_cellC[i]);
        for (int i = 0; i < BITS; i++) Reset(_cellS[i]);
        SetCell(_cellC[BITS], 0, COrange);

        _currentBit = BITS - 1; _subStep = 0;
        MarkActive(_currentBit);
        RedrawCircuit();
        LogText.Text = $"開始: {_valA} ({Bin(_valA)}) + {_valB} ({Bin(_valB)})  — bit7 (LSB) から計算します";
    }

    // ── Sub-step animation ────────────────────────────────────────
    // 0=inputs  1=XOR1  2=AND1  3=XOR2(sum)  4=AND2  5=OR(carry)  6=next bit

    void AdvanceSubStep()
    {
        if (_currentBit < 0) { Initialize(); return; }
        int bi = _currentBit;
        int a=_bA[bi], b=_bB[bi], cin=_carry[bi+1];
        int xor1=a^b, and1=a&b, xor2=xor1^cin, and2=xor1&cin, or_=and1|and2;

        switch (_subStep)
        {
            case 0:
                LightWires("inA","inB","inCin");
                LogText.Text = $"[bit{BITS-1-bi}]  A={a}   B={b}   Carry_in={cin}  → 入力信号が流れ込みます";
                break;
            case 1:
                LightWires("xor1_out");
                SetGateGlow("xor1", CAccent);
                LogText.Text = $"[bit{BITS-1-bi}]  XOR(A={a}, B={b}) = {xor1}";
                break;
            case 2:
                LightWires("and1_out","and1_or");
                SetGateGlow("and1", CYellow);
                LogText.Text = $"[bit{BITS-1-bi}]  AND(A={a}, B={b}) = {and1}";
                break;
            case 3:
                LightWires("xor2_out");
                SetGateGlow("xor2", CAccent);
                _sum[bi] = xor2; SetCell(_cellS[bi], xor2, CPink, hi: true);
                LogText.Text = $"[bit{BITS-1-bi}]  XOR(xor1={xor1}, Cin={cin}) = {xor2}  → Sum = {xor2}";
                break;
            case 4:
                LightWires("and2_out","and2_or");
                SetGateGlow("and2", CYellow);
                LogText.Text = $"[bit{BITS-1-bi}]  AND(xor1={xor1}, Cin={cin}) = {and2}";
                break;
            case 5:
                LightWires("or_out");
                SetGateGlow("or_gate", CGreen);
                _carry[bi] = or_; SetCell(_cellC[bi], or_, COrange, hi: true);
                LogText.Text = $"[bit{BITS-1-bi}]  OR(and1={and1}, and2={and2}) = {or_}  → Carry_out = {or_}";
                break;
            case 6:
                if (bi == 0)
                {
                    int result = 0;
                    for (int i = 0; i < BITS; i++) result |= _sum[i] << (BITS-1-i);
                    ResultLabel.Text = $"= {result}";
                    LogText.Text = $"完成！  {_valA} + {_valB} = {result}  ({Bin(result)})";
                    _autoTimer.Stop(); BtnAuto.Content = "⏩ Auto";
                    MarkActive(-1); _currentBit = -1; _subStep = 0; return;
                }
                _currentBit = bi - 1; _subStep = 0;
                MarkActive(_currentBit); RedrawCircuit();
                LogText.Text = $"次のビット bit{BITS-1-_currentBit} へ";
                return;
        }
        _subStep++;
    }

    // gate element refs for glow
    readonly Dictionary<string, UIElement> _gateElements = [];

    void SetGateGlow(string key, Color col)
    {
        if (!_gateElements.TryGetValue(key, out var el)) return;
        var prev = ((Shape)el).Effect;
        ((Shape)el).Effect = new DropShadowEffect { Color = col, BlurRadius = 22, ShadowDepth = 0, Opacity = 1 };
    }

    // ── Circuit layout ────────────────────────────────────────────

    void RedrawCircuit()
    {
        GateCanvas.Children.Clear(); _wires.Clear(); _gateElements.Clear();
        if (_currentBit < 0) return;

        double W = GateCanvas.ActualWidth;  if (W < 10) W = 1064;
        double H = GateCanvas.ActualHeight; if (H < 10) H = 380;

        int bi = _currentBit;
        int a=_bA[bi], b=_bB[bi], cin=_carry[bi+1];
        int xor1=a^b, and1=a&b, xor2=xor1^cin, and2=xor1&cin, or_=and1|and2;

        double gW=96, gH=50;

        // gate columns (left edge)
        double cL = W * 0.22;   // XOR1, AND1
        double cR = W * 0.52;   // XOR2, AND2
        double cO = W * 0.78;   // OR

        // gate rows (top edge) — AND1 and AND2 at DIFFERENT Y to avoid wire crossings
        double rXor1 = H * 0.06;
        double rXor2 = H * 0.06;   // same row as XOR1
        double rAnd2 = H * 0.36;   // right AND: middle height
        double rAnd1 = H * 0.62;   // left AND:  lower (so and1 output wire goes below AND2)
        double rOr   = H * 0.50;   // OR: between AND1 and AND2

        // input wire Y = same as XOR1 pin Y (clean horizontal entry)
        double inAY   = rXor1 + gH * 0.30;
        double inBY   = rXor1 + gH * 0.70;
        double inCinY = H * 0.88;
        double inX    = W * 0.03;

        Lbl($"A = {a}",   inX, inAY   - 12, CBlue,   15, true);
        Lbl($"B = {b}",   inX, inBY   - 12, CGreen,  15, true);
        Lbl($"Cin={cin}", inX, inCinY - 12, COrange, 15, true);
        Lbl($"Full Adder — bit {BITS-1-bi}", W/2 - 90, 3, CFg, 13);

        DrawGate(cL, rXor1, gW, gH, "XOR", CAccent, "xor1");
        DrawGate(cL, rAnd1, gW, gH, "AND", CYellow, "and1");
        DrawGate(cR, rXor2, gW, gH, "XOR", CAccent, "xor2");
        DrawGate(cR, rAnd2, gW, gH, "AND", CYellow, "and2");
        DrawGate(cO, rOr,   gW, gH, "OR",  CGreen,  "or_gate");

        // pin positions
        Point xor1_iA = P(cL,      rXor1 + gH * 0.30);
        Point xor1_iB = P(cL,      rXor1 + gH * 0.70);
        Point xor1_o  = P(cL + gW, rXor1 + gH * 0.50);

        Point and1_iA = P(cL,      rAnd1 + gH * 0.30);
        Point and1_iB = P(cL,      rAnd1 + gH * 0.70);
        Point and1_o  = P(cL + gW, rAnd1 + gH * 0.50);

        Point xor2_iX = P(cR,      rXor2 + gH * 0.30);
        Point xor2_iC = P(cR,      rXor2 + gH * 0.70);
        Point xor2_o  = P(cR + gW, rXor2 + gH * 0.50);

        Point and2_iX = P(cR,      rAnd2 + gH * 0.30);
        Point and2_iC = P(cR,      rAnd2 + gH * 0.70);
        Point and2_o  = P(cR + gW, rAnd2 + gH * 0.50);

        Point or_iA = P(cO,      rOr + gH * 0.30);
        Point or_iB = P(cO,      rOr + gH * 0.70);
        Point or_o  = P(cO + gW, rOr + gH * 0.50);

        ValLabel(xor1_o, xor1, CAccent); ValLabel(and1_o, and1, CYellow);
        ValLabel(xor2_o, xor2, CPink);   ValLabel(and2_o, and2, CYellow);
        ValLabel(or_o,   or_,  COrange);
        Lbl("= Sum",  xor2_o.X + 22, xor2_o.Y - 9, CPink,   12);
        Lbl("= Cout", or_o.X  + 22,  or_o.Y  - 9,  COrange, 12);

        // junction X positions (left of gate columns)
        double jAx   = cL - 36;
        double jBx   = cL - 56;
        double jX1x  = cR - 36;
        double jCinx = cR - 56;

        // ── A input: horizontal to junction, branch down to AND1
        Reg("inA",      P(inX+55, inAY),        P(jAx, inAY),           CBlue,   a);
        Reg("inA_x1",   P(jAx,   inAY),         xor1_iA,                CBlue,   a);
        Reg("inA_a1",   P(jAx,   inAY),         P(jAx, and1_iA.Y),      CBlue,   a);
        Reg("inA_a1b",  P(jAx,   and1_iA.Y),    and1_iA,                CBlue,   a);

        // ── B input
        Reg("inB",      P(inX+55, inBY),        P(jBx, inBY),           CGreen,  b);
        Reg("inB_x1",   P(jBx,   inBY),         xor1_iB,                CGreen,  b);
        Reg("inB_a1",   P(jBx,   inBY),         P(jBx, and1_iB.Y),      CGreen,  b);
        Reg("inB_a1b",  P(jBx,   and1_iB.Y),    and1_iB,                CGreen,  b);

        // ── Cin: vertical bus at jCinx, branches to XOR2 and AND2
        Reg("inCin",    P(inX+58, inCinY),      P(jCinx, inCinY),       COrange, cin);
        Reg("inCin_x",  P(jCinx, inCinY),       P(jCinx, xor2_iC.Y),   COrange, cin);
        Reg("inCin_xb", P(jCinx, xor2_iC.Y),   xor2_iC,                COrange, cin);
        Reg("inCin_br", P(jCinx, inCinY),       P(jCinx-16, inCinY),   COrange, cin);
        Reg("inCin_a",  P(jCinx-16, inCinY),    P(jCinx-16, and2_iC.Y),COrange, cin);
        Reg("inCin_ab", P(jCinx-16, and2_iC.Y), and2_iC,               COrange, cin);

        // ── XOR1 output: to junction, branch up→XOR2, down→AND2
        Reg("xor1_out", xor1_o,                 P(jX1x, xor1_o.Y),     CAccent, xor1);
        Reg("xor1_x2",  P(jX1x, xor1_o.Y),     P(jX1x, xor2_iX.Y),    CAccent, xor1);
        Reg("xor1_x2b", P(jX1x, xor2_iX.Y),    xor2_iX,               CAccent, xor1);
        Reg("xor1_a2",  P(jX1x, xor1_o.Y),     P(jX1x, and2_iX.Y),    CAccent, xor1);
        Reg("xor1_a2b", P(jX1x, and2_iX.Y),    and2_iX,               CAccent, xor1);

        // ── AND1 output → OR input A
        // and1_o.Y > AND2 bottom (rAnd1=0.62H > rAnd2+gH=0.36H+50), so wire passes cleanly below AND2
        Reg("and1_out", and1_o,                 P(cO-22, and1_o.Y),    CYellow, and1);
        Reg("and1_or",  P(cO-22, and1_o.Y),     P(cO-22, or_iA.Y),    CYellow, and1);
        Reg("and1_orb", P(cO-22, or_iA.Y),      or_iA,                 CYellow, and1);

        // ── XOR2 output → Sum
        Reg("xor2_out", xor2_o,                 P(W*0.97, xor2_o.Y),   CPink,   xor2);

        // ── AND2 output → OR input B
        // and2_o.Y < rOr (0.36H+25 < 0.50H), so wire goes above OR then turns down
        Reg("and2_out", and2_o,                 P(cO-10, and2_o.Y),    CYellow, and2);
        Reg("and2_or",  P(cO-10, and2_o.Y),     P(cO-10, or_iB.Y),    CYellow, and2);
        Reg("and2_orb", P(cO-10, or_iB.Y),      or_iB,                 CYellow, and2);

        // ── OR output → Cout
        Reg("or_out",   or_o,                   P(W*0.97, or_o.Y),     COrange, or_);

        foreach (var kv in _wires) DrawWireDim(kv.Value.from, kv.Value.to);

        // junction dots
        Dot(jAx,      inAY,       CBlue);
        Dot(jBx,      inBY,       CGreen);
        Dot(jCinx,    inCinY,     COrange);
        Dot(jX1x,     xor1_o.Y,   CAccent);
    }

    void Reg(string n, Point f, Point t, Color c, int v) => _wires[n] = (f, t, c, v);
    static Point P(double x, double y) => new(x, y);

    // wire groups activated per sub-step
    static readonly string[][] WireGroups =
    [
        ["inA","inA_x1","inA_a1","inA_a1b","inB","inB_x1","inB_a1","inB_a1b",
         "inCin","inCin_br","inCin_x","inCin_xb","inCin_a","inCin_ab"],
        ["xor1_out","xor1_x2","xor1_x2b","xor1_a2","xor1_a2b"],
        ["and1_out","and1_or","and1_orb"],
        ["xor2_out"],
        ["and2_out","and2_or","and2_orb"],
        ["or_out"],
    ];

    void LightWires(params string[] keys)
    {
        var targets = new HashSet<string>();
        foreach (var k in keys)
            foreach (var g in WireGroups)
                if (g.Contains(k)) foreach (var n in g) targets.Add(n);

        foreach (var name in targets)
        {
            if (!_wires.TryGetValue(name, out var w)) continue;
            var col = w.val == 1 ? w.col : C("#585B70");
            DrawWireAnim(w.from, w.to, col);
        }
    }

    // ── Gate drawing (proper circuit symbols) ─────────────────────

    void DrawGate(double x, double y, double w, double h, string type, Color col, string key)
    {
        var fill = Color.FromArgb(38, col.R, col.G, col.B);
        Path body;

        if (type == "AND")
        {
            var geo = new StreamGeometry();
            using (var c = geo.Open())
            {
                c.BeginFigure(P(0, 0), true, true);
                c.LineTo(P(0, h), true, false);
                c.LineTo(P(w * 0.44, h), true, false);
                c.BezierTo(P(w, h), P(w, 0), P(w * 0.44, 0), true, false);
            }
            body = MkPath(geo, col, fill);
        }
        else // OR / XOR
        {
            if (type == "XOR")
            {
                // extra arc to the left
                var ag = new StreamGeometry();
                using (var c = ag.Open())
                {
                    c.BeginFigure(P(0, 0), false, false);
                    c.QuadraticBezierTo(P(w * 0.18, h * 0.5), P(0, h), true, false);
                }
                var ap = new Path { Data = ag, Stroke = Br(col), StrokeThickness = 2.2, Fill = Brushes.Transparent };
                Canvas.SetLeft(ap, x - 11); Canvas.SetTop(ap, y);
                GateCanvas.Children.Add(ap);
            }

            var geo = new StreamGeometry();
            using (var c = geo.Open())
            {
                c.BeginFigure(P(0, 0), true, true);
                c.BezierTo(P(w*0.42, 0), P(w, h*0.22), P(w, h*0.5), true, false);
                c.BezierTo(P(w, h*0.78), P(w*0.42, h), P(0, h), true, false);
                c.QuadraticBezierTo(P(w*0.20, h*0.5), P(0, 0), true, false);
            }
            body = MkPath(geo, col, fill);
        }

        body.Effect = new DropShadowEffect { Color = col, BlurRadius = 6, ShadowDepth = 0, Opacity = 0.4 };
        Canvas.SetLeft(body, x); Canvas.SetTop(body, y);
        GateCanvas.Children.Add(body);
        _gateElements[key] = body;

        // label
        double lx = type == "AND" ? x + 8 : x + w * 0.22;
        var tb = new TextBlock { Text = type, FontSize = 12, FontWeight = FontWeights.Bold,
            FontFamily = new FontFamily("Consolas"), Foreground = Br(col) };
        Canvas.SetLeft(tb, lx); Canvas.SetTop(tb, y + h/2 - 9);
        GateCanvas.Children.Add(tb);
    }

    Path MkPath(StreamGeometry geo, Color stroke, Color fill) => new()
    {
        Data = geo, Fill = Br(fill), Stroke = Br(stroke), StrokeThickness = 2.4,
        StrokeLineJoin = PenLineJoin.Round
    };

    // ── Wire drawing ──────────────────────────────────────────────

    void DrawWireDim(Point f, Point t)
    {
        var pl = new Polyline
        {
            Points = [f, t], Stroke = Br(C("#2E2E4A")),
            StrokeThickness = 1.8, StrokeDashArray = [5, 4]
        };
        GateCanvas.Children.Add(pl);
    }

    void DrawWireAnim(Point f, Point t, Color col)
    {
        double len = Math.Max(Dist(f, t), 1);
        var pl = new Polyline
        {
            Points = [f, t],
            Stroke = Br(col), StrokeThickness = 3,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap   = PenLineCap.Round,
            StrokeDashArray    = [len / 3, len / 3],
            StrokeDashOffset   = len / 3,
            Effect = new DropShadowEffect { Color = col, BlurRadius = 10, ShadowDepth = 0, Opacity = 0.85 }
        };
        GateCanvas.Children.Add(pl);
        pl.BeginAnimation(Polyline.StrokeDashOffsetProperty,
            new DoubleAnimation(len / 3, 0, TimeSpan.FromMilliseconds(380))
            { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } });
    }

    void ValLabel(Point pin, int val, Color col)
    {
        var tb = new TextBlock
        {
            Text = val.ToString(), FontSize = 13, FontWeight = FontWeights.Bold,
            Foreground = Br(val == 1 ? CYellow : C("#484860")),
            FontFamily = new FontFamily("Consolas")
        };
        Canvas.SetLeft(tb, pin.X + 4); Canvas.SetTop(tb, pin.Y - 8);
        GateCanvas.Children.Add(tb);
    }

    void Lbl(string t, double x, double y, Color col, double size, bool bold = false)
    {
        var tb = new TextBlock
        {
            Text = t, FontSize = size,
            FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
            Foreground = Br(col), FontFamily = new FontFamily("Consolas")
        };
        Canvas.SetLeft(tb, x); Canvas.SetTop(tb, y);
        GateCanvas.Children.Add(tb);
    }

    void Dot(double x, double y, Color col)
    {
        var e = new Ellipse { Width = 7, Height = 7, Fill = Br(col) };
        Canvas.SetLeft(e, x - 3.5); Canvas.SetTop(e, y - 3.5);
        GateCanvas.Children.Add(e);
    }

    static double Dist(Point a, Point b) =>
        Math.Sqrt((a.X-b.X)*(a.X-b.X) + (a.Y-b.Y)*(a.Y-b.Y));

    static SolidColorBrush Br(Color c) => new(c);
    static string Bin(int v) => Convert.ToString(v, 2).PadLeft(BITS, '0');
}
