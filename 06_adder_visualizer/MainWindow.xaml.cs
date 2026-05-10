using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace _06_adder_visualizer;

public partial class MainWindow : Window
{
    private const int BITS = 8;
    private DispatcherTimer _timer = new();
    private int _step = 0;
    private int _valA, _valB;
    private int[] _bitsA = new int[BITS];
    private int[] _bitsB = new int[BITS];
    private int[] _carry = new int[BITS + 1];
    private int[] _sum   = new int[BITS];

    // UI要素の参照
    private Border[] _cellA     = new Border[BITS];
    private Border[] _cellB     = new Border[BITS];
    private Border[] _cellCarry = new Border[BITS + 1];
    private Border[] _cellSum   = new Border[BITS];

    // 色
    static readonly SolidColorBrush ColA     = new(Color.FromRgb(0x89, 0xDC, 0xEB));
    static readonly SolidColorBrush ColB     = new(Color.FromRgb(0xA6, 0xE3, 0xA1));
    static readonly SolidColorBrush ColSum   = new(Color.FromRgb(0xF3, 0x8B, 0xA8));
    static readonly SolidColorBrush ColCarry = new(Color.FromRgb(0xFA, 0xB3, 0x87));
    static readonly SolidColorBrush ColHigh  = new(Color.FromRgb(0xFF, 0xE0, 0x70));
    static readonly SolidColorBrush ColLow   = new(Color.FromRgb(0x45, 0x47, 0x5A));
    static readonly SolidColorBrush ColBg    = new(Color.FromRgb(0x31, 0x32, 0x44));
    static readonly SolidColorBrush ColFg    = new(Color.FromRgb(0xCD, 0xD6, 0xF4));

    public MainWindow()
    {
        InitializeComponent();
        _timer.Interval = TimeSpan.FromMilliseconds(900);
        _timer.Tick += OnTick;
        BuildBitRows();
    }

    // ビット行のUIを生成
    void BuildBitRows()
    {
        BitsA.Items.Clear(); BitsB.Items.Clear();
        BitsCarry.Items.Clear(); BitsSum.Items.Clear();

        for (int i = 0; i < BITS; i++)
        {
            _cellA[i]     = MakeCell("?", ColFg);
            _cellB[i]     = MakeCell("?", ColFg);
            _cellSum[i]   = MakeCell("?", ColFg);
            BitsA.Items.Add(_cellA[i]);
            BitsB.Items.Add(_cellB[i]);
            BitsSum.Items.Add(_cellSum[i]);
        }
        for (int i = 0; i <= BITS; i++)
        {
            _cellCarry[i] = MakeCell("?", ColFg);
            BitsCarry.Items.Add(_cellCarry[i]);
        }
    }

    Border MakeCell(string text, SolidColorBrush fg)
    {
        var tb = new TextBlock
        {
            Text = text, FontSize = 18, FontFamily = new FontFamily("Consolas"),
            Foreground = fg, Width = 32, TextAlignment = TextAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        var b = new Border
        {
            Child = tb, Width = 38, Height = 30, Margin = new Thickness(2),
            Background = ColLow, CornerRadius = new CornerRadius(4)
        };
        return b;
    }

    void SetCell(Border cell, int val, SolidColorBrush fg, bool highlight = false)
    {
        var tb = (TextBlock)cell.Child;
        tb.Text = val.ToString();
        tb.Foreground = fg;
        cell.Background = highlight ? ColHigh : (val == 1 ? ColBg : ColLow);
        if (highlight)
        {
            var anim = new ColorAnimation(
                ((SolidColorBrush)ColHigh).Color,
                ((SolidColorBrush)ColBg).Color,
                TimeSpan.FromMilliseconds(800));
            cell.Background = new SolidColorBrush(((SolidColorBrush)ColHigh).Color);
            ((SolidColorBrush)cell.Background).BeginAnimation(SolidColorBrush.ColorProperty, anim);
        }
    }

    void BtnStart_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(InputA.Text, out _valA) || !int.TryParse(InputB.Text, out _valB)
            || _valA < 0 || _valB < 0 || _valA > 255 || _valB > 255)
        {
            LogText.Text = "0〜255の整数を入力してください";
            return;
        }
        BtnStart.IsEnabled = false;
        for (int i = 0; i < BITS; i++)
        {
            _bitsA[i] = (_valA >> (BITS - 1 - i)) & 1;
            _bitsB[i] = (_valB >> (BITS - 1 - i)) & 1;
        }
        _carry[BITS] = 0;
        _step = 0;
        ResetBitUI();
        // A, B のビットを表示
        for (int i = 0; i < BITS; i++)
        {
            SetCell(_cellA[i],     _bitsA[i], ColA);
            SetCell(_cellB[i],     _bitsB[i], ColB);
            SetCell(_cellCarry[i], -1, ColFg);
            SetCell(_cellSum[i],   -1, ColFg);
        }
        ((TextBlock)_cellCarry[0].Child).Text = "?";
        ((TextBlock)_cellCarry[BITS].Child).Text = "0";
        SetCell(_cellCarry[BITS], 0, ColCarry);

        LogText.Text = $"A = {_valA}  ({ToBin(_valA)})　B = {_valB}  ({ToBin(_valB)})　→ 右端のビットから順に計算します";
        DrawAllAdders();
        _timer.Start();
    }

    void ResetBitUI()
    {
        for (int i = 0; i < BITS; i++)
        {
            ((TextBlock)_cellA[i].Child).Text = "?";
            ((TextBlock)_cellB[i].Child).Text = "?";
            ((TextBlock)_cellCarry[i].Child).Text = "?";
            ((TextBlock)_cellSum[i].Child).Text = "?";
            _cellA[i].Background = ColLow; _cellB[i].Background = ColLow;
            _cellCarry[i].Background = ColLow; _cellSum[i].Background = ColLow;
        }
        ((TextBlock)_cellCarry[BITS].Child).Text = "?";
        _cellCarry[BITS].Background = ColLow;
    }

    // ステップ実行
    void OnTick(object? sender, EventArgs e)
    {
        // step 0..BITS-1 = 各桁の計算 (右から: index = BITS-1-step)
        if (_step < BITS)
        {
            int col = BITS - 1 - _step; // bit index (0=MSB, BITS-1=LSB)
            int a = _bitsA[col];
            int b = _bitsB[col];
            int cin = _carry[col + 1]; // carry in from previous (lower) bit

            int xor1 = a ^ b;
            int s    = xor1 ^ cin;
            int c    = (a & b) | (xor1 & cin);

            _sum[col]   = s;
            _carry[col] = c;

            SetCell(_cellSum[col],   s,   ColSum,   highlight: true);
            SetCell(_cellCarry[col], c,   ColCarry, highlight: c == 1);
            HighlightAdder(_step, a, b, cin, xor1, s, c);

            string cinNote = _step == 0 ? "carry_in=0 (初期値)" : $"carry_in={cin}";
            LogText.Text = $"[Bit {BITS - 1 - col}] A={a}, B={b}, {cinNote}  →  XOR={xor1}  XOR(+carry)={s}  AND carry_out={c}";
            _step++;
        }
        else
        {
            _timer.Stop();
            int result = 0;
            for (int i = 0; i < BITS; i++) result |= _sum[i] << (BITS - 1 - i);
            LogText.Text = $"完成！  {_valA} + {_valB} = {result}  ({ToBin(result)})";
            BtnStart.IsEnabled = true;
        }
    }

    void BtnReset_Click(object sender, RoutedEventArgs e)
    {
        _timer.Stop();
        _step = 0;
        BtnStart.IsEnabled = true;
        ResetBitUI();
        GateCanvas.Children.Clear();
        LogText.Text = "";
    }

    // ======== Canvas 描画 ========

    // 全アダー（8個）を最初に薄く描画
    void DrawAllAdders()
    {
        GateCanvas.Children.Clear();
        for (int i = 0; i < BITS; i++)
            DrawAdder(i, dimmed: true);
    }

    // i = ステップ番号 (0=LSB側)
    // adder列: 右から i 番目 = canvas左からの位置
    void DrawAdder(int step, bool dimmed, int? a = null, int? b = null,
                   int? cin = null, int? xor1 = null, int? s = null, int? c = null)
    {
        double unitW = GateCanvas.Width / BITS;
        double x = step * unitW + unitW * 0.1;
        double w = unitW * 0.8;

        double alpha = dimmed ? 0.25 : 1.0;

        Color GateCol(byte r, byte g, byte b2) =>
            Color.FromArgb((byte)(255 * alpha), r, g, b2);

        // XOR1 ゲート (a XOR b)
        double xorY  = 60;
        double andY  = 160;
        double xor2Y = 220;

        DrawGateBox(x, xorY, w, 40, "XOR", GateCol(0x89, 0xB4, 0xFA),
            a?.ToString(), b?.ToString(), xor1?.ToString(), alpha);

        // AND1 (a AND b) → carry下段へ
        DrawGateBox(x, andY - 40, w, 40, "AND", GateCol(0xF9, 0xE2, 0xAF),
            a?.ToString(), b?.ToString(), (a.HasValue ? (a & b).ToString() : null), alpha);

        // XOR2 (xor1 XOR cin) → Sum
        DrawGateBox(x, xor2Y, w, 40, "XOR", GateCol(0x89, 0xB4, 0xFA),
            xor1?.ToString(), cin?.ToString(), s?.ToString(), alpha);

        // AND2 (xor1 AND cin)
        DrawGateBox(x, xor2Y + 50, w, 40, "AND", GateCol(0xF9, 0xE2, 0xAF),
            xor1?.ToString(), cin?.ToString(), (xor1.HasValue ? (xor1 & cin).ToString() : null), alpha);

        // OR (carry_out)
        DrawGateBox(x, 280, w, 40, "OR", GateCol(0xA6, 0xE3, 0xA1),
            (a.HasValue ? (a & b).ToString() : null),
            (xor1.HasValue ? (xor1 & cin).ToString() : null),
            c?.ToString(), alpha);

        // ラベル（桁番号）
        var lbl = new TextBlock
        {
            Text = $"bit{BITS - 1 - step}",
            Foreground = new SolidColorBrush(Color.FromArgb((byte)(255 * alpha), 0xCD, 0xD6, 0xF4)),
            FontSize = 11, FontFamily = new FontFamily("Consolas")
        };
        Canvas.SetLeft(lbl, x + w / 2 - 16);
        Canvas.SetTop(lbl, 8);
        GateCanvas.Children.Add(lbl);
    }

    void DrawGateBox(double x, double y, double w, double h, string label,
        Color borderCol, string? in1, string? in2, string? output, double alpha)
    {
        var rect = new Rectangle
        {
            Width = w, Height = h,
            Fill = new SolidColorBrush(Color.FromArgb((byte)(255 * alpha * 0.15), borderCol.R, borderCol.G, borderCol.B)),
            Stroke = new SolidColorBrush(borderCol),
            StrokeThickness = 1.5,
            RadiusX = 5, RadiusY = 5
        };
        Canvas.SetLeft(rect, x); Canvas.SetTop(rect, y);
        GateCanvas.Children.Add(rect);

        var tb = new TextBlock
        {
            Text = label,
            Foreground = new SolidColorBrush(borderCol),
            FontSize = 12, FontWeight = FontWeights.Bold,
            FontFamily = new FontFamily("Consolas")
        };
        Canvas.SetLeft(tb, x + 4); Canvas.SetTop(tb, y + 4);
        GateCanvas.Children.Add(tb);

        // 入力
        if (in1 != null)
        {
            var t = new TextBlock { Text = $"↑{in1}", Foreground = new SolidColorBrush(borderCol), FontSize = 10, FontFamily = new FontFamily("Consolas") };
            Canvas.SetLeft(t, x + 4); Canvas.SetTop(t, y + h - 16);
            GateCanvas.Children.Add(t);
        }
        // 出力
        if (output != null)
        {
            var isHigh = output == "1";
            var outCol = isHigh
                ? Color.FromArgb((byte)(255 * alpha), 0xFF, 0xE0, 0x70)
                : Color.FromArgb((byte)(255 * alpha), borderCol.R, borderCol.G, borderCol.B);
            var t = new TextBlock { Text = $"={output}", Foreground = new SolidColorBrush(outCol), FontSize = 12, FontWeight = FontWeights.Bold, FontFamily = new FontFamily("Consolas") };
            Canvas.SetLeft(t, x + w - 28); Canvas.SetTop(t, y + 4);
            GateCanvas.Children.Add(t);
        }
    }

    void HighlightAdder(int step, int a, int b, int cin, int xor1, int s, int c)
    {
        // 対象のアダーだけ再描画（明るく）
        DrawAdder(step, dimmed: false, a: a, b: b, cin: cin, xor1: xor1, s: s, c: c);
    }

    string ToBin(int v) => Convert.ToString(v, 2).PadLeft(BITS, '0');
}
