using System.Text.Json;

string filePath = "todos.json";

// 起動時にファイルからTODOを読み込む
var todos = new List<TodoItem>();
if (File.Exists(filePath)) {
    string json = File.ReadAllText(filePath);
    todos = JsonSerializer.Deserialize<List<TodoItem>>(json) ?? new List<TodoItem>();
    Console.WriteLine($"保存済みのTODOを{todos.Count}件読み込みました。");
}

// ファイルに保存するヘルパー
void Save() {
    string json = JsonSerializer.Serialize(todos, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText(filePath, json);
}

// 次のIDを採番するヘルパー
int NextId() => todos.Count == 0 ? 1 : todos.Max(t => t.Id) + 1;

while (true) {
    Console.WriteLine("\n=== TODOリスト ===");
    Console.WriteLine("1. 追加");
    Console.WriteLine("2. 一覧表示");
    Console.WriteLine("3. 完了にする");
    Console.WriteLine("4. 削除");
    Console.WriteLine("5. 終了");
    Console.Write("選択: ");

    string choice = Console.ReadLine()!;

    switch (choice) {
        case "1":
            Console.Write("TODO を入力: ");
            string title = Console.ReadLine()!;
            todos.Add(new TodoItem {
                Id = NextId(),
                Title = title,
                IsDone = false,
                CreatedAt = DateTime.Now
            });
            Save();
            Console.WriteLine($"「{title}」を追加しました。");
            break;

        case "2":
            if (todos.Count == 0) {
                Console.WriteLine("TODOはありません。");
            } else {
                Console.WriteLine("\n--- TODO一覧 ---");
                for (int i = 0; i < todos.Count; i++) {
                    var t = todos[i];
                    string status = t.IsDone ? "✓" : " ";
                    Console.WriteLine($"{i + 1}. [{status}] {t.Title}  ({t.CreatedAt:yyyy-MM-dd HH:mm})");
                }
            }
            break;

        case "3":
            if (todos.Count == 0) {
                Console.WriteLine("TODOはありません。");
                break;
            }
            for (int i = 0; i < todos.Count; i++) {
                var t = todos[i];
                string status = t.IsDone ? "✓" : " ";
                Console.WriteLine($"{i + 1}. [{status}] {t.Title}");
            }
            Console.Write("完了にする番号を入力: ");
            try {
                int index = int.Parse(Console.ReadLine()!) - 1;
                if (index >= 0 && index < todos.Count) {
                    todos[index].IsDone = true;
                    Console.WriteLine($"「{todos[index].Title}」を完了にしました。");
                    Save();
                } else {
                    Console.WriteLine("無効な番号です。");
                }
            } catch (FormatException) {
                Console.WriteLine("数字を入力してください。");
            }
            break;

        case "4":
            if (todos.Count == 0) {
                Console.WriteLine("TODOはありません。");
                break;
            }
            for (int i = 0; i < todos.Count; i++) {
                var t = todos[i];
                string status = t.IsDone ? "✓" : " ";
                Console.WriteLine($"{i + 1}. [{status}] {t.Title}");
            }
            Console.Write("削除する番号を入力: ");
            try {
                int index = int.Parse(Console.ReadLine()!) - 1;
                if (index >= 0 && index < todos.Count) {
                    Console.WriteLine($"「{todos[index].Title}」を削除しました。");
                    todos.RemoveAt(index);
                    Save();
                } else {
                    Console.WriteLine("無効な番号です。");
                }
            } catch (FormatException) {
                Console.WriteLine("数字を入力してください。");
            }
            break;

        case "5":
            Console.WriteLine("終了します。");
            return;

        default:
            Console.WriteLine("1〜5 を入力してください。");
            break;
    }
}

class TodoItem {
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public bool IsDone { get; set; }
    public DateTime CreatedAt { get; set; }
}
