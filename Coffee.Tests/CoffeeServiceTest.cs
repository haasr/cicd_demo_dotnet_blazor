using Coffee.Models;
using Coffee.Services;

namespace Coffee.Tests;

[TestClass]
public sealed class CoffeeServiceTest
{
    private string tempDirectory = string.Empty;
    private string ordersPath = string.Empty;
    private CoffeeService service = null!;

    // Runs before EVERY test, so each test gets a fresh service and fresh CSV files in a throwaway folder.
    [TestInitialize]
    public void Setup()
    {
        tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDirectory);

        var drinksPath = Path.Combine(tempDirectory, "Drinks.csv");
        ordersPath = Path.Combine(tempDirectory, "Orders.csv");

        File.WriteAllLines(drinksPath, new[]
        {
            "1,Latte,4.50,Espresso with steamed milk",
            "2,Cold Brew,4.00,Steeped, then served over ice"
        });
        File.WriteAllLines(ordersPath, new[]
        {
            "1,Ada Lovelace,1,Medium,False,Extra hot",
            "2,Alan Turing,2,Large,True,"
        });

        service = new CoffeeService(drinksPath, ordersPath);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(tempDirectory))
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [TestMethod]
    public void Constructor_LoadsDrinksAndOrders()
    {
        Assert.HasCount(2, service.GetDrinks());
        Assert.HasCount(2, service.GetOrders());
    }

    [TestMethod]
    public void GetDrink_DescriptionWithComma_IsKeptWhole()
    {
        Assert.AreEqual("Steeped, then served over ice", service.GetDrink(2)!.Description);
    }

    [TestMethod]
    public void GetOrder_UnknownId_ReturnsNull()
    {
        Assert.IsNull(service.GetOrder(99));
    }

    [TestMethod]
    public void GetPrice_AddsSizeUpcharge()
    {
        Assert.AreEqual(5.00m, service.GetPrice(service.GetOrder(1)!));  // Latte 4.50 + Medium 0.50
        Assert.AreEqual(5.00m, service.GetPrice(service.GetOrder(2)!));  // Cold Brew 4.00 + Large 1.00
    }

    [TestMethod]
    public void CountPending_IgnoresCompletedOrders()
    {
        Assert.AreEqual(1, service.CountPending());
    }

    [TestMethod]
    public void AddOrder_AssignsNextId_AndSavesToFile()
    {
        var order = new Order { Customer = "Grace Hopper", DrinkId = 1, Size = "Small", Notes = "Oat milk, no foam" };

        service.AddOrder(order);

        Assert.AreEqual(3, order.Id);
        var reloaded = new CoffeeService(Path.Combine(tempDirectory, "Drinks.csv"), ordersPath);
        Assert.AreEqual("Oat milk, no foam", reloaded.GetOrder(3)!.Notes);
    }

    [TestMethod]
    public void EditOrder_UpdatesFields_AndKeepsCompletedFlag()
    {
        service.EditOrder(new Order { Id = 2, Customer = "Alan T.", DrinkId = 1, Size = "Small", Notes = "Iced" });

        var order = service.GetOrder(2)!;
        Assert.AreEqual("Alan T.", order.Customer);
        Assert.AreEqual(1, order.DrinkId);
        Assert.IsTrue(order.Completed);
    }

    [TestMethod]
    public void CompleteOrder_MarksOrderCompleted_AndSavesToFile()
    {
        service.CompleteOrder(1);

        Assert.IsTrue(service.GetOrder(1)!.Completed);
        Assert.AreEqual(0, service.CountPending());
        StringAssert.Contains(File.ReadAllText(ordersPath), "1,Ada Lovelace,1,Medium,True,Extra hot");
    }

    [TestMethod]
    public void DeleteOrder_RemovesOrder_AndSavesToFile()
    {
        service.DeleteOrder(1);

        Assert.IsNull(service.GetOrder(1));
        Assert.HasCount(1, File.ReadAllLines(ordersPath));
    }
}
