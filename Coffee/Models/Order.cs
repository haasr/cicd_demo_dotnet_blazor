using System.ComponentModel.DataAnnotations;

namespace Coffee.Models;

public class Order
{
    public static readonly string[] Sizes = { "Small", "Medium", "Large" };

    public int Id { get; set; }

    [Required]
    public string Customer { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Pick a drink.")]
    public int DrinkId { get; set; }

    [Required]
    public string Size { get; set; } = "Medium";

    public string Notes { get; set; } = string.Empty;

    // False while the drink is still being made; true once it has been handed over.
    public bool Completed { get; set; }

    public static decimal SizeUpcharge(string size) => size switch
    {
        "Medium" => 0.50m,
        "Large" => 1.00m,
        _ => 0m
    };
}
