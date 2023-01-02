using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Obecto.Perper.Examples.AddressBook;

namespace BasicSample.Calls
{
    // TODO: This does not currently work
    public class TestProto
    {
        public async Task<AddressBook> RunAsync(Person person)
        {
            Console.WriteLine(person.Name);
            var addressBook = new AddressBook();
            addressBook.People.Add(person);
            return addressBook;
        }
    }
}
