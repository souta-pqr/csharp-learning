/* 使う側のコード（クラス定義より先に書く） */
var person = new Person("田中太郎", 25);
Console.WriteLine(person.Greet());

/* クラスの定義 */
class Person {
    public string Name { get; set; }
    public int Age { get; set; }

    public Person(string name, int age) {
        Name = name;
        Age = age;
    }

    public string Greet() {
        return $"こんにちは！{Name}です．{Age}歳です．";
    }
}
