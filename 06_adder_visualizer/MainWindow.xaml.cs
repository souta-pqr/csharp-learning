using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace _06_adder_visualizer;

public partial class MainWindow : Window
{
    const int BITS = 8;

    // --- state ---
    int _valA, _valB;
    int[] _bA = new int[BITS], _bB = new int[BITS];
    int[] _carry = new int[BITS + 1];  // carry[i] = carry INTO bit i (0=MSB)
    int[] _sum   = new int[BITS];
    int _currentBit = -1;   // 0=MSB ... 7=LSB, -1=not started
    int _subStep = 0;        // animation sub-step within one bit

    // bit cells
    Border[] _cellA = new Border[BITS], _cellB = new Border[BITS];
    Border[] _cellC = new Border[BITS + 1], _cellS = new Border[BITS];

    DispatcherTimer _autoTimer = new() { Interval = TimeSpan.FromMilliseconds(260) };

    // --- colors ---
    static Color C(string hex) { var c = (Color)ColorConverter.ConvertFromString(hex); return c; }
    static readonly Color CBlue   = C("#89DCEB");
    static readonly Color CGreen  = C("#A6E3A1");
    static readonly Color CPink   = C("#F38BA8");
    static readonly Color COrange = C("#FAB387");
    static readonly Color CPurple = C("#CBA6F7");
    static readonly Color CYellow = C("#F9E2AF");
    static readonly Color CDim    = C("#313244");
    static readonly Color CDark   = C("#13131F");
    static readonly Color CFg     = C("#CDD6F4");
    static readonly Color CAccent = C("#89B4FA");

    public MainWindow()
    {
        InitializeComponent();
        _autoTimer.Tick += (_, _) => AdvanceSubStep();
        BuildBitCells();
        GateCanvas.SizeChanged += (_, _) => RedrawCircuit();
    }

    // ==================== Bit Cell UI ====================

    void BuildBitCells()
    {
        BitsA.Items.Clear(); BitsB.Items.Clear(); BitsCarry.Items.Clear(); BitsSum.Items.Clear();
        for (int i = 0; i < BITS; i++)
        {
            _cellA[i] = BitCell("·", CBlue);   BitsA.Items.Add(_cellA[i]);
            _cellB[i] = BitCell("·", CGreen);  BitsB.Items.Add(_cellB[i]);
            _cellS[i] = BitCell("·", CPink);   BitsSum.Items.Add(_cellS[i]);
        }
        for (int i = 0; i <= BITS; i++)
        {
            _cellC[i] = BitCell("·", COrange); BitsCarry.Items.Add(_cellC[i]);
        }
    }

    Border BitCell(string t, Color fg)
    {
        var tb = new TextBlock { Text = t, FontSize = 17, TextAlignment = TextAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center, Width = 30,
            Foreground = Br(fg), FontFamily = new FontFamily("Consolas") };
        return new Border { Child = tb, Width = 36, Height = 28, Margin = new Thickness(2,1,2,1),
            Background = Br(CDim), CornerRadius = new CornerRadius(4) };
    }

    void SetBitCell(Border cell, int val, Color fg, bool highlight = false)
    {
        var tb = (TextBlock)cell.Child;
        tb.Text = val.ToString();
        tb.Foreground = Br(highlight ? CYellow : fg);
        cell.Background = Br(highlight ? C("#3D3D00") : (val == 1 ? C("#2A2A40") : CDim));
    }

    void MarkActive(int bitIndex)
    {
        for (int i = 0; i < BITS; i++)
        {
            bool active = i == bitIndex;
            _cellA[i].BorderThickness = new Thickness(active ? 2 : 0);
            _cellA[i].BorderBrush = Br(CBlue);
            _cellB[i].BorderThickness = new Thickness(active ? 2 : 0);
            _cellB[i].BorderBrush = Br(CGreen);
        }
    }

    // ==================== Buttons ====================

    void BtnStep_Click(object s, RoutedEventArgs e)
    {
        _autoTimer.Stop();
        BtnAuto.Content = "⏩ Auto";
        if (_currentBit == -1) Initialize();
        else AdvanceSubStep();
    }

    void BtnAuto_Click(object s, RoutedEventArgs e)
    {
        if (_autoTimer.IsEnabled) { _autoTimer.Stop(); BtnAuto.Content = "⏩ Auto"; return; }
        if (_currentBit == -1) Initialize();
        BtnAuto.Content = "⏸ Pause";
        _autoTimer.Start();
    }

    void BtnReset_Click(object s, RoutedEventArgs e)
    {
        _autoTimer.Stop();
        BtnAuto.Content = "⏩ Auto";
        _currentBit = -1; _subStep = 0;
        for (int i = 0; i < BITS; i++)
        {
            ((TextBlock)_cellA[i].Child).Text = "·"; _cellA[i].Background = Br(CDim); _cellA[i].BorderThickness = new Thickness(0);
            ((TextBlock)_cellB[i].Child).Text = "·"; _cellB[i].Background = Br(CDim); _cellB[i].BorderThickness = new Thickness(0);
            ((TextBlock)_cellS[i].Child).Text = "·"; _cellS[i].Background = Br(CDim);
        }
        for (int i = 0; i <= BITS; i++) { ((TextBlock)_cellC[i].Child).Text = "·"; _cellC[i].Background = Br(CDim); }
        ResultLabel.Text = "";
        LogText.Text = "A と B を入力して ▶ Step または ⏩ Auto を押してください";
        GateCanvas.Children.Clear();
    }

    void Initialize()
    {
        if (!int.TryParse(InputA.Text, out _valA) || !int.TryParse(InputB.Text, out _valB)
            || _valA < 0 || _valA > 255 || _valB < 0 || _valB > 255)
        { LogText.Text = "0〜255 の整数を入力してください"; return; }

        for (int i = 0; i < BITS; i++)
        {
            _bA[i] = (_valA >> (BITS - 1 - i)) & 1;
            _bB[i] = (_valB >> (BITS - 1 - i)) & 1;
        }
        Array.Fill(_carry, 0); Array.Fill(_sum, 0);
        _carry[BITS] = 0; // carry into LSB = 0

        for (int i = 0; i < BITS; i++)
        {
            SetBitCell(_cellA[i], _bA[i], CBlue);
            SetBitCell(_cellB[i], _bB[i], CGreen);
            ((TextBlock)_cellS[i].Child).Text = "·"; _cellS[i].Background = Br(CDim);
        }
        for (int i = 0; i <= BITS; i++) { ((TextBlock)_cellC[i].Child).Text = "·"; _cellC[i].Background = Br(CDim); }
        SetBitCell(_cellC[BITS], 0, COrange);

        _currentBit = BITS - 1; // start from LSB (index 7)
        _subStep = 0;
        MarkActive(_currentBit);
        RedrawCircuit();
        LogText.Text = $"開始: {_valA} ({Bin(_valA)}) + {_valB} ({Bin(_valB)})  — bit7(LSB)から計算します";
    }

    // ==================== Sub-step animation ====================
    // SubSteps per bit: 0=inputs, 1=XOR1, 2=AND1, 3=XOR2(sum), 4=AND2, 5=OR(carry), 6=advance

    void AdvanceSubStep()
    {
        if (_currentBit < 0) { Initialize(); return; }

        int bi = _currentBit; // bit index (0=MSB,7=LSB)
        int a   = _bA[bi];
        int b   = _bB[bi];
        int cin = _carry[bi + 1];
        int xor1 = a ^ b;
        int and1 = a & b;
        int xor2 = xor1 ^ cin;   // = sum
        int and2 = xor1 & cin;
        int or_  = and1 | and2;  // = carry_out

        switch (_subStep)
        {
            case 0: // show inputs flowing in
                AnimateWire("inA");
                AnimateWire("inB");
                AnimateWire("inCin");
                LogText.Text = $"[bit{BITS-1-bi}]  A={a}   B={b}   Carry_in={cin}  → 入力信号が流れ込みます";
                break;
            case 1: // XOR1
                AnimateWire("xor1_out");
                LogText.Text = $"[bit{BITS-1-bi}]  XOR(A,B) = {a} XOR {b} = {xor1}";
                break;
            case 2: // AND1
                AnimateWire("and1_out");
                LogText.Text = $"[bit{BITS-1-bi}]  AND(A,B) = {a} AND {b} = {and1}";
                break;
            case 3: // XOR2 = sum
                AnimateWire("xor2_out");
                _sum[bi] = xor2;
                SetBitCell(_cellS[bi], xor2, CPink, highlight: true);
                LogText.Text = $"[bit{BITS-1-bi}]  XOR(xor1,Cin) = {xor1} XOR {cin} = {xor2}  → Sum bit = {xor2}";
                break;
            case 4: // AND2
                AnimateWire("and2_out");
                LogText.Text = $"[bit{BITS-1-bi}]  AND(xor1,Cin) = {xor1} AND {cin} = {and2}";
                break;
            case 5: // OR = carry_out
                AnimateWire("or_out");
                _carry[bi] = or_;
                SetBitCell(_cellC[bi], or_, COrange, highlight: true);
                LogText.Text = $"[bit{BITS-1-bi}]  OR(and1,and2) = {and1} OR {and2} = {or_}  → Carry_out = {or_}";
                break;
            case 6: // advance to next bit
                if (bi == 0)
                {
                    // done
                    int result = 0;
                    for (int i = 0; i < BITS; i++) result |= _sum[i] << (BITS - 1 - i);
                    ResultLabel.Text = $"= {result}";
                    LogText.Text = $"完成！  {_valA} + {_valB} = {result}  ({Bin(result)})";
                    _autoTimer.Stop(); BtnAuto.Content = "⏩ Auto";
                    MarkActive(-1);
                    _currentBit = -1; _subStep = 0;
                    return;
                }
                _currentBit = bi - 1;
                _subStep = 0;
                MarkActive(_currentBit);
                RedrawCircuit();
                LogText.Text = $"次のビット bit{BITS-1-_currentBit} へ";
                return;
        }
        _subStep++;
    }

    // ==================== Circuit Drawing ====================

    // Named wire endpoints, set during RedrawCircuit
    readonly Dictionary<string, (Point from, Point to, Color col, int val)> _wires = [];

    void RedrawCircuit()
    {
        GateCanvas.Children.Clear();
        _wires.Clear();
        if (_currentBit < 0) return;

        double W = GateCanvas.ActualWidth;  if (W < 10) W = 1064;
        double H = GateCanvas.ActualHeight; if (H < 10) H = 400;

        int bi  = _currentBit;
        int a   = _bA[bi], b = _bB[bi], cin = _carry[bi + 1];
        int xor1 = a ^ b, and1 = a & b;
        int xor2 = xor1 ^ cin, and2 = xor1 & cin;
        int or_  = and1 | and2;

        // ---- layout constants ----
        double gW = 110, gH = 46;  // gate box size
        double col1 = W * 0.26, col2 = W * 0.52, col3 = W * 0.78;
        double rowXOR1 = H * 0.12, rowAND1 = H * 0.55;
        double rowXOR2 = H * 0.12, rowAND2 = H * 0.55;
        double rowOR   = H * 0.34;

        // split canvas vertically: left half = first-stage gates, right half = second-stage
        double lGateX  = col1 - gW / 2;  // XOR1, AND1
        double rGateX  = col2 - gW / 2;  // XOR2, AND2
        double orGateX = col3 - gW / 2;  // OR

        // input pin positions
        double inAY   = H * 0.20;
        double inBY   = H * 0.44;
        double inCinY = H * 0.68;
        double inputX = W * 0.04;

        // draw input labels
        DrawLabel($"A = {a}", inputX, inAY - 10, CBlue, 15, bold: true);
        DrawLabel($"B = {b}", inputX, inBY - 10, CGreen, 15, bold: true);
        DrawLabel($"Cin= {cin}", inputX, inCinY - 10, COrange, 15, bold: true);

        // bit position label
        DrawLabel($"Full Adder  bit{BITS - 1 - bi}", W / 2 - 80, 4, CFg, 14);

        // --- XOR1 gate ---
        double xor1Y = (inAY + inBY) / 2 - gH / 2;
        DrawGate(lGateX, xor1Y, gW, gH, "XOR", CAccent);
        Point xor1In_A = new(lGateX, xor1Y + gH * 0.33);
        Point xor1In_B = new(lGateX, xor1Y + gH * 0.67);
        Point xor1Out  = new(lGateX + gW, xor1Y + gH / 2);
        DrawValue(xor1Out.X + 4, xor1Out.Y - 8, xor1, CAccent);

        // --- AND1 gate ---
        double and1Y = (inAY + inBY) / 2 - gH / 2 + H * 0.35;
        DrawGate(lGateX, and1Y, gW, gH, "AND", CYellow);
        Point and1In_A = new(lGateX, and1Y + gH * 0.33);
        Point and1In_B = new(lGateX, and1Y + gH * 0.67);
        Point and1Out  = new(lGateX + gW, and1Y + gH / 2);
        DrawValue(and1Out.X + 4, and1Out.Y - 8, and1, CYellow);

        // --- XOR2 gate ---
        double xor2Y = (xor1Y + inCinY) / 2 - gH / 2 + H * 0.0;
        if (xor2Y < xor1Y + gH + 10) xor2Y = xor1Y + gH + 10;
        xor2Y = H * 0.12;
        DrawGate(rGateX, xor2Y, gW, gH, "XOR", CAccent);
        Point xor2In_X = new(rGateX, xor2Y + gH * 0.33);
        Point xor2In_C = new(rGateX, xor2Y + gH * 0.67);
        Point xor2Out  = new(rGateX + gW, xor2Y + gH / 2);
        DrawValue(xor2Out.X + 4, xor2Out.Y - 8, xor2, CPink);
        DrawLabel("= Sum", xor2Out.X + 26, xor2Out.Y - 9, CPink, 13);

        // --- AND2 gate ---
        double and2Y = H * 0.55;
        DrawGate(rGateX, and2Y, gW, gH, "AND", CYellow);
        Point and2In_X = new(rGateX, and2Y + gH * 0.33);
        Point and2In_C = new(rGateX, and2Y + gH * 0.67);
        Point and2Out  = new(rGateX + gW, and2Y + gH / 2);
        DrawValue(and2Out.X + 4, and2Out.Y - 8, and2, CYellow);

        // --- OR gate ---
        double orY = (and1Y + and2Y) / 2 - gH / 2;
        DrawGate(orGateX, orY, gW, gH, "OR", CGreen);
        Point orIn1  = new(orGateX, orY + gH * 0.33);
        Point orIn2  = new(orGateX, orY + gH * 0.67);
        Point orOut  = new(orGateX + gW, orY + gH / 2);
        DrawValue(orOut.X + 4, orOut.Y - 8, or_, COrange);
        DrawLabel("= Cout", orOut.X + 26, orOut.Y - 9, COrange, 13);

        // ---- define wires ----
        double inAx   = inputX + 60;
        double inBx   = inputX + 60;
        double inCinx = inputX + 60;

        // fan-out junctions
        double jAX  = lGateX - 30;
        double jBX  = lGateX - 50;
        double jX1X = rGateX - 30;
        double jCinX = rGateX - 50;

        // inA  → junction → xor1In_A and and1In_A
        // inB  → junction → xor1In_B and and1In_B
        // xor1 → junction → xor2In_X and and2In_X
        // cin  → junction → xor2In_C and and2In_C

        // wires: (from, to, color, value)
        // We define named segments
        RegisterWire("inA",     new(inAx,   inAY),    new(jAX,      inAY),     CBlue,   a);
        RegisterWire("inA_x1",  new(jAX,    inAY),    xor1In_A,                CBlue,   a);
        RegisterWire("inA_a1",  new(jAX,    inAY),    new(jAX,      and1In_A.Y), CBlue, a);
        RegisterWire("inA_a1b", new(jAX,    and1In_A.Y), and1In_A,             CBlue,   a);

        RegisterWire("inB",     new(inBx,   inBY),    new(jBX,      inBY),     CGreen,  b);
        RegisterWire("inB_x1",  new(jBX,    inBY),    xor1In_B,                CGreen,  b);
        RegisterWire("inB_a1",  new(jBX,    inBY),    new(jBX,      and1In_B.Y), CGreen, b);
        RegisterWire("inB_a1b", new(jBX,    and1In_B.Y), and1In_B,             CGreen,  b);

        RegisterWire("inCin",   new(inCinx, inCinY),  new(jCinX,    inCinY),   COrange, cin);
        RegisterWire("inCin_x2",new(jCinX,  inCinY),  new(jCinX,    xor2In_C.Y), COrange, cin);
        RegisterWire("inCin_x2b",new(jCinX, xor2In_C.Y), xor2In_C,             COrange, cin);
        RegisterWire("inCin_a2",new(jCinX,  inCinY),  new(jCinX - 14, inCinY), COrange, cin);
        RegisterWire("inCin_a2b",new(jCinX - 14, inCinY), new(jCinX - 14, and2In_C.Y), COrange, cin);
        RegisterWire("inCin_a2c",new(jCinX - 14, and2In_C.Y), and2In_C,        COrange, cin);

        RegisterWire("xor1_out",xor1Out,              new(jX1X,     xor1Out.Y), CAccent, xor1);
        RegisterWire("xor1_x2", new(jX1X, xor1Out.Y), new(jX1X,    xor2In_X.Y), CAccent, xor1);
        RegisterWire("xor1_x2b",new(jX1X, xor2In_X.Y), xor2In_X,               CAccent, xor1);
        RegisterWire("xor1_a2", new(jX1X, xor1Out.Y), new(jX1X - 14, xor1Out.Y), CAccent, xor1);
        RegisterWire("xor1_a2b",new(jX1X - 14, xor1Out.Y), new(jX1X - 14, and2In_X.Y), CAccent, xor1);
        RegisterWire("xor1_a2c",new(jX1X - 14, and2In_X.Y), and2In_X,           CAccent, xor1);

        RegisterWire("and1_out",and1Out,               new(orIn1.X - 20, and1Out.Y), CYellow, and1);
        RegisterWire("and1_or", new(orIn1.X - 20, and1Out.Y), new(orIn1.X - 20, orIn1.Y), CYellow, and1);
        RegisterWire("and1_orb",new(orIn1.X - 20, orIn1.Y), orIn1,                CYellow, and1);

        RegisterWire("xor2_out",xor2Out,               new(W * 0.97,  xor2Out.Y), CPink,   xor2);

        RegisterWire("and2_out",and2Out,               new(orIn2.X - 8, and2Out.Y), CYellow, and2);
        RegisterWire("and2_or", new(orIn2.X - 8, and2Out.Y), new(orIn2.X - 8, orIn2.Y), CYellow, and2);
        RegisterWire("and2_orb",new(orIn2.X - 8, orIn2.Y), orIn2,                CYellow, and2);

        RegisterWire("or_out",  orOut,                 new(W * 0.97,  orOut.Y),   COrange, or_);

        // draw all wires dimmed
        foreach (var kv in _wires)
            DrawWireDimmed(kv.Value.from, kv.Value.to);

        // draw junction dots
        DrawDot(jAX, inAY, CBlue);
        DrawDot(jBX, inBY, CGreen);
        DrawDot(jCinX, inCinY, COrange);
        DrawDot(jX1X, xor1Out.Y, CAccent);
    }

    void RegisterWire(string name, Point from, Point to, Color col, int val)
        => _wires[name] = (from, to, col, val);

    // Groups: each button press lights up one group of wires
    static readonly string[][] WireGroups =
    [
        ["inA", "inA_x1", "inA_a1", "inA_a1b", "inB", "inB_x1", "inB_a1", "inB_a1b",
         "inCin", "inCin_x2", "inCin_x2b", "inCin_a2", "inCin_a2b", "inCin_a2c"],
        ["xor1_out", "xor1_x2", "xor1_x2b", "xor1_a2", "xor1_a2b", "xor1_a2c"],
        ["and1_out", "and1_or", "and1_orb"],
        ["xor2_out"],
        ["and2_out", "and2_or", "and2_orb"],
        ["or_out"],
    ];

    void AnimateWire(string group)
    {
        // find matching group
        string[][] groups = WireGroups;
        string[]? match = null;
        foreach (var g in groups)
            if (g.Contains(group)) { match = g; break; }
        if (match == null) match = [group];

        foreach (var name in match)
        {
            if (!_wires.TryGetValue(name, out var w)) continue;
            var val = w.val;
            var col = val == 1 ? w.col : Color.FromRgb(0x58, 0x5B, 0x70);
            DrawWireAnimated(w.from, w.to, col);
        }
    }

    // ==================== Drawing Helpers ====================

    void DrawGate(double x, double y, double w, double h, string label, Color col)
    {
        var fill = Color.FromArgb(40, col.R, col.G, col.B);
        var rect = new Rectangle
        {
            Width = w, Height = h,
            Fill = Br(fill), Stroke = Br(col),
            StrokeThickness = 2, RadiusX = 8, RadiusY = 8
        };
        Canvas.SetLeft(rect, x); Canvas.SetTop(rect, y);
        GateCanvas.Children.Add(rect);

        var tb = new TextBlock
        {
            Text = label, FontSize = 16, FontWeight = FontWeights.Bold,
            FontFamily = new FontFamily("Consolas"),
            Foreground = Br(col),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        Canvas.SetLeft(tb, x + w / 2 - 22); Canvas.SetTop(tb, y + h / 2 - 11);
        GateCanvas.Children.Add(tb);
    }

    void DrawValue(double x, double y, int val, Color col)
    {
        var tb = new TextBlock
        {
            Text = val.ToString(), FontSize = 13, FontWeight = FontWeights.Bold,
            Foreground = Br(val == 1 ? CYellow : Color.FromRgb(0x58, 0x5B, 0x70)),
            FontFamily = new FontFamily("Consolas")
        };
        Canvas.SetLeft(tb, x); Canvas.SetTop(tb, y);
        GateCanvas.Children.Add(tb);
    }

    void DrawLabel(string text, double x, double y, Color col, double size, bool bold = false)
    {
        var tb = new TextBlock
        {
            Text = text, FontSize = size,
            FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
            Foreground = Br(col), FontFamily = new FontFamily("Consolas")
        };
        Canvas.SetLeft(tb, x); Canvas.SetTop(tb, y);
        GateCanvas.Children.Add(tb);
    }

    void DrawDot(double x, double y, Color col)
    {
        var e = new Ellipse { Width = 7, Height = 7, Fill = Br(col) };
        Canvas.SetLeft(e, x - 3.5); Canvas.SetTop(e, y - 3.5);
        GateCanvas.Children.Add(e);
    }

    void DrawWireDimmed(Point from, Point to)
        => DrawPolyLine([from, to], Color.FromRgb(0x30, 0x30, 0x45), 1.5, dash: true);

    void DrawWireAnimated(Point from, Point to, Color col)
    {
        var line = new Polyline
        {
            Points = [from, to],
            Stroke = Br(col), StrokeThickness = 3,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap   = PenLineCap.Round,
            Effect = new System.Windows.Media.Effects.DropShadowEffect
                { Color = col, BlurRadius = 10, ShadowDepth = 0, Opacity = 0.8 }
        };
        double len = Distance(from, to);
        line.StrokeDashArray = [len / 3, len / 3];
        line.StrokeDashOffset = len / 3;
        GateCanvas.Children.Add(line);

        var anim = new DoubleAnimation(len / 3, 0, TimeSpan.FromMilliseconds(350))
            { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };
        line.BeginAnimation(Polyline.StrokeDashOffsetProperty, anim);
    }

    void DrawPolyLine(Point[] pts, Color col, double thick, bool dash = false)
    {
        var line = new Polyline { Points = new PointCollection(pts), Stroke = Br(col), StrokeThickness = thick };
        if (dash) line.StrokeDashArray = [4, 4];
        GateCanvas.Children.Add(line);
    }

    static double Distance(Point a, Point b)
        => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));

    static SolidColorBrush Br(Color c) => new(c);
    static string Bin(int v) => Convert.ToString(v, 2).PadLeft(BITS, '0');
}
