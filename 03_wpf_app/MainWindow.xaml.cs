using System.Windows;
using System.Windows.Controls;

namespace _03_wpf_app;

public partial class MainWindow : Window
{
    private double _firstNumber = 0;
    private string _operator = "";
    private bool _newInput = true;   // 次のキーで表示をリセットするか

    public MainWindow()
    {
        InitializeComponent();
    }

    private void Btn_Click(object sender, RoutedEventArgs e)
    {
        string tag = (string)((Button)sender).Tag;

        switch (tag)
        {
            case "0": case "1": case "2": case "3": case "4":
            case "5": case "6": case "7": case "8": case "9":
                AppendDigit(tag);
                break;
            case ".":
                AppendDot();
                break;
            case "+": case "-": case "*": case "/":
                SetOperator(tag);
                break;
            case "=":
                Calculate();
                break;
            case "C":
                Clear();
                break;
            case "+/-":
                ToggleSign();
                break;
            case "%":
                Percent();
                break;
        }
    }

    private void AppendDigit(string digit)
    {
        if (_newInput)
        {
            Display.Text = digit == "0" ? "0" : digit;
            _newInput = false;
        }
        else
        {
            if (Display.Text == "0")
                Display.Text = digit;
            else
                Display.Text += digit;
        }
    }

    private void AppendDot()
    {
        if (_newInput)
        {
            Display.Text = "0.";
            _newInput = false;
            return;
        }
        if (!Display.Text.Contains('.'))
            Display.Text += ".";
    }

    private void SetOperator(string op)
    {
        _firstNumber = double.Parse(Display.Text);
        _operator = op;
        _newInput = true;
    }

    private void Calculate()
    {
        if (_operator == "") return;

        double second = double.Parse(Display.Text);
        double result = _operator switch
        {
            "+" => _firstNumber + second,
            "-" => _firstNumber - second,
            "*" => _firstNumber * second,
            "/" => second != 0 ? _firstNumber / second : double.NaN,
            _ => second
        };

        Display.Text = double.IsNaN(result) ? "エラー" : Format(result);
        _operator = "";
        _newInput = true;
    }

    private void Clear()
    {
        Display.Text = "0";
        _firstNumber = 0;
        _operator = "";
        _newInput = true;
    }

    private void ToggleSign()
    {
        if (double.TryParse(Display.Text, out double val))
            Display.Text = Format(-val);
    }

    private void Percent()
    {
        if (double.TryParse(Display.Text, out double val))
            Display.Text = Format(val / 100);
    }

    private static string Format(double val)
    {
        return val == Math.Truncate(val) ? ((long)val).ToString() : val.ToString("G10");
    }
}
