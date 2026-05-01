Console.WriteLine("=== 電卓アプリ ===");

while (true) {
    Console.Write("数値1を入力: ");
    double num1 = double.Parse(Console.ReadLine()!);

    Console.Write("演算子 (+,-,*,/) を入力: ");
    string op = Console.ReadLine()!;

    Console.Write("数値2を入力: ");
    double num2 = double.Parse(Console.ReadLine()!);

    double result = op switch {
        "+" => num1 + num2,
        "-" => num1 - num2,
        "*" => num1 * num2,
        "/" => num2 != 0 ? num1 / num2 : throw new DivideByZeroException(),
        _ => throw new InvalidOperationException("不明な演算子")
    };

    Console.WriteLine($"結果: {result}");
    Console.Write("続けますか？ (y/n): ");
    if (Console.ReadLine()?.ToLower() != "y") break;
}
