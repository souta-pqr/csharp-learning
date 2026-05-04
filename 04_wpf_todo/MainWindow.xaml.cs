using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace _04_wpf_todo;

public partial class MainWindow : Window
{
    private readonly TodoService _service = new();
    private List<TodoItem> _todos = new();

    public MainWindow()
    {
        InitializeComponent();
        _todos = _service.Load();
        RefreshList();
    }

    // リストを再描画する
    private void RefreshList()
    {
        TodoList.ItemsSource = null;
        TodoList.ItemsSource = _todos;
    }

    // 追加ボタン
    private void AddButton_Click(object sender, RoutedEventArgs e) => AddTodo();

    // Enterキーでも追加できる
    private void InputBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) AddTodo();
    }

    private void AddTodo()
    {
        string title = InputBox.Text.Trim();
        if (string.IsNullOrEmpty(title)) return;

        _todos.Add(new TodoItem
        {
            Id = _service.NextId(_todos),
            Title = title,
            IsDone = false,
            CreatedAt = DateTime.Now
        });

        _service.Save(_todos);
        InputBox.Text = "";
        RefreshList();
        InputBox.Focus();
    }

    // チェックボックスの変更
    private void CheckBox_Changed(object sender, RoutedEventArgs e)
    {
        _service.Save(_todos);
    }

    // 削除ボタン
    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        int id = (int)((Button)sender).Tag;
        _todos.RemoveAll(t => t.Id == id);
        _service.Save(_todos);
        RefreshList();
    }
}
