namespace ExpenseTracker.Domain.Entities;

public class Category
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;

    // Used by EF 
    private Category()
    {
    }

    public Category(Guid userId, string name)
    {
        Id = Guid.CreateVersion7();
        UserId = userId;
        Name = name.Trim();
    }
}
