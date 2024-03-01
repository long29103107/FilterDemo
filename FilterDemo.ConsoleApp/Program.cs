var list = new List<Customer>
{
    new Customer{ Name = "Long", Age = 25, Id = 1, BirthDay = new DateTime(1997, 9, 15)  },
    new Customer{ Name = "Phuc", Age = 35, Id = 2, BirthDay = new DateTime(1990, 1, 1) },
    new Customer{ Name = "Hiep", Age = 15, Id = 3, BirthDay = new DateTime(2000, 5, 1)  },
};

var filteredList = new List<Customer>();
try
{
    filteredList = list.Filter("!((Id eq `1`)|(Id eq `3`))|((Name eq `Long`)&(Id eq `1`))");
}
catch (Exception ex)
{
    throw;
}


public class Customer
{
    public int Id { get; set; }

    public string Name { get; set; }

    public int Age { get; set; }

    public DateTime BirthDay { get; set; }
}
