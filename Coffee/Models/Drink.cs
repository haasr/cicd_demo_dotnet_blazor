namespace Coffee.Models;

// One item on the menu. Loaded from Data/Drinks.csv; the menu is read-only in the app.
public class Drink
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    // Price of a Small. Larger sizes add to this (see Order.SizeUpcharge).
    public decimal Price { get; set; }

    public string Description { get; set; } = string.Empty;
}
