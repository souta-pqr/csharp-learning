using System.IO;
using System.Text.Json;

namespace _04_wpf_todo;

class TodoService
{
    private readonly string _filePath = "todos.json";
    private readonly JsonSerializerOptions _options = new() { WriteIndented = true };

    public List<TodoItem> Load()
    {
        if (!File.Exists(_filePath)) return new();
        string json = File.ReadAllText(_filePath);
        return JsonSerializer.Deserialize<List<TodoItem>>(json) ?? new();
    }

    public void Save(List<TodoItem> todos)
    {
        File.WriteAllText(_filePath, JsonSerializer.Serialize(todos, _options));
    }

    public int NextId(List<TodoItem> todos)
        => todos.Count == 0 ? 1 : todos.Max(t => t.Id) + 1;
}
