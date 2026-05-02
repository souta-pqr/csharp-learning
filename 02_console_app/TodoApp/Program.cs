string filePath = "todos.txt";

// 起動時にファイルからTODOを読み込む
var todos = new List<string>();
if (File.Exists(filePath)) {
    todos.AddRange(File.ReadAllLines(filePath));
    Console.WriteLine($"保存済みのTODOを{todos.Count}件読み込みました。");
}

while (true) {
    Console.WriteLine("\n=== TODOリスト ===");
    Console.WriteLine("1. 追加");
    Console.WriteLine("2. 一覧表示");
    Console.WriteLine("3. 削除");
    Console.WriteLine("4. 終了");
    Console.Write("選択: ");

    string choice = Console.ReadLine()!;

    switch (choice) {
        case "1":
            Console.Write("TODO を入力: ");
            string task = Console.ReadLine()!;
            todos.Add(task);
            File.WriteAllLines(filePath, todos);
            Console.WriteLine($"「{task}」を追加しました。");
            break;

        case "2":
            if (todos.Count == 0) {
                Console.WriteLine("TODOはありません。");
            } else {
                Console.WriteLine("\n--- TODO一覧 ---");
                for (int i = 0; i < todos.Count; i++) {
                    Console.WriteLine($"{i + 1}. {todos[i]}");
                }
            }
            break;

        case "3":
            if (todos.Count == 0) {
                Console.WriteLine("TODOはありません。");
                break;
            }
            for (int i = 0; i < todos.Count; i++) {
                Console.WriteLine($"{i + 1}. {todos[i]}");
            }
            Console.Write("削除する番号を入力: ");
            int index = int.Parse(Console.ReadLine()!) - 1;
            if (index >= 0 && index < todos.Count) {
                Console.WriteLine($"「{todos[index]}」を削除しました。");
                todos.RemoveAt(index);
                File.WriteAllLines(filePath, todos);
            } else {
                Console.WriteLine("無効な番号です。");
            }
            break;

        case "4":
            Console.WriteLine("終了します。");
            return;

        default:
            Console.WriteLine("1〜4 を入力してください。");
            break;
    }
}
