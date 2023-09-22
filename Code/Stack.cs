public class Stack<T>
{
    private List<T> items = new();

    public void Push(T item)
    {
        items.Insert(0, item);
    }

    public T Pop()
    {
        var item = items[0];
        items.RemoveAt(0);
        return item;
    }
}
