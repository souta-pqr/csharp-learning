/* 変数の宣言と型 */
int age = 25;
string name = "田中太郎";
double price = 198.5;
bool isStudent = true;

Console.WriteLine($"名前: {name}, 年齢: {age}");

/* 条件分岐 */
if (isStudent) {
    Console.WriteLine("学生割引: 20%OFF");
}

/* ループ */
for (int i = 1; i <= 5; i++) {
    Console.WriteLine($"{i}回目のループ");
}
