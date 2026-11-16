using System.Globalization;
using Coffee.Models;

namespace Coffee.Services;

public class CoffeeService
{
    private string DrinksPath = "./Data/Drinks.csv";
    private string OrdersPath = "./Data/Orders.csv";

    private readonly List<Drink> drinks = new List<Drink>();
    private readonly List<Order> orders = new List<Order>();

    // The service is a singleton, so every connected browser shares these lists.
    // Lock around changes so two customers ordering at once can't interleave file writes.
    private readonly object _lock = new object();

    public CoffeeService()
    {
        ReadDrinks();
        ReadOrders();
    }

    public CoffeeService(string drinksPath, string ordersPath)
    {
        DrinksPath = drinksPath;
        OrdersPath = ordersPath;
        ReadDrinks();
        ReadOrders();
    }

    // ----- Menu -----

    public List<Drink> GetDrinks()
    {
        return drinks;
    }

    public Drink? GetDrink(int id)
    {
        return drinks.FirstOrDefault(d => d.Id == id);
    }

    // ----- Orders -----

    public List<Order> GetOrders()
    {
        return orders;
    }

    public Order? GetOrder(int id)
    {
        return orders.FirstOrDefault(o => o.Id == id);
    }

    public int CountPending()
    {
        return orders.Count(o => !o.Completed);
    }

    // Menu price of the drink plus the size upcharge. Unknown drink costs 0.
    public decimal GetPrice(Order order)
    {
        var drink = GetDrink(order.DrinkId);
        if (drink == null) return 0m;

        return drink.Price + Order.SizeUpcharge(order.Size);
    }

    public void AddOrder(Order order)
    {
        lock (_lock)
        {
            order.Id = orders.Count == 0 ? 1 : orders.Max(o => o.Id) + 1;
            orders.Add(order);
            WriteOrders();
        }
    }

    public void EditOrder(Order updated)
    {
        lock (_lock)
        {
            var order = orders.FirstOrDefault(o => o.Id == updated.Id);
            if (order == null) return;

            order.Customer = updated.Customer;
            order.DrinkId = updated.DrinkId;
            order.Size = updated.Size;
            order.Notes = updated.Notes;
            WriteOrders();
        }
    }

    public void CompleteOrder(int id)
    {
        lock (_lock)
        {
            var order = orders.FirstOrDefault(o => o.Id == id);
            if (order == null) return;

            order.Completed = true;
            WriteOrders();
        }
    }

    public void DeleteOrder(int id)
    {
        lock (_lock)
        {
            orders.RemoveAll(o => o.Id == id);
            WriteOrders();
        }
    }

    // ----- CSV reading -----

    private void ReadDrinks()
    {
        try
        {
            foreach (string line in File.ReadLines(DrinksPath))
            {
                string[] fields = line.Replace("\"", "").Split(',');

                if (fields.Length >= 4)
                {
                    // Id,Name,Price,Description. The description is last, so it may contain commas.
                    drinks.Add(new Drink
                    {
                        Id = int.Parse(fields[0].Trim()),
                        Name = fields[1].Trim(),
                        Price = decimal.Parse(fields[2].Trim(), CultureInfo.InvariantCulture),
                        Description = string.Join(",", fields[3..]).Trim()
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }

    private void ReadOrders()
    {
        try
        {
            foreach (string line in File.ReadLines(OrdersPath))
            {
                string[] fields = line.Replace("\"", "").Split(',');

                if (fields.Length >= 5)
                {
                    // Id,Customer,DrinkId,Size,Completed,Notes. Notes are free text and last,
                    // so everything after the fifth comma belongs to the notes (and may be empty).
                    orders.Add(new Order
                    {
                        Id = int.Parse(fields[0].Trim()),
                        Customer = fields[1].Trim(),
                        DrinkId = int.Parse(fields[2].Trim()),
                        Size = fields[3].Trim(),
                        Completed = bool.Parse(fields[4].Trim()),
                        Notes = string.Join(",", fields[5..]).Trim()
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }

    // ----- CSV writing -----

    // Quotes and line breaks are stripped everywhere. Commas are stripped from every field
    // except the notes, which is the only field the reader above allows to contain commas.
    private void WriteOrders()
    {
        var lines = orders.Select(o => string.Join(",",
            o.Id,
            Clean(o.Customer).Replace(",", ""),
            o.DrinkId,
            Clean(o.Size).Replace(",", ""),
            o.Completed,
            Clean(o.Notes)));
        File.WriteAllLines(OrdersPath, lines);
    }

    private static string Clean(string value)
    {
        return value.Replace("\"", "").Replace("\r", " ").Replace("\n", " ");
    }
}
