namespace ExpenseTracker.Domain.Entities;

public class Category
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;

    // Used by EF
    private Category()
    {
    }

    public Category(Guid id, string name)
    {
        Id = id;
        Name = name;
    }
}
