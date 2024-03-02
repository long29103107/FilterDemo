using Microsoft.AspNetCore.Mvc;
using FilterExpression.Extensions;

namespace FilterExpression.Example.Controllers;
[Route("api/[controller]")]
[ApiController]
public class CustomerController : ControllerBase
{
    [HttpGet]
    public List<Customer> Get()
    {
        var list = new List<Customer>
        {
            new Customer{ Name = "Long", Age = 25, Id = 1, BirthDay = new DateTime(1997, 9, 15)  },
            new Customer{ Name = "Phuc", Age = 35, Id = 2, BirthDay = new DateTime(1990, 1, 1) },
            new Customer{ Name = "Hiep", Age = 15, Id = 3, BirthDay = new DateTime(2000, 5, 1)  },
        };

        var filteredList = new List<Customer>();
        try
        {
            filteredList = list.Filter("!((Id eq `1`)|(Id eq `3`))|!((Name eq `Long`)&(Id eq `1`))|(Name eq `3`)");
        }
        catch (Exception ex)
        {
            throw;
        }

        return filteredList;
    }    
}
