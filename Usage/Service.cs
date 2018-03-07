using System;
using System.Collections.Generic;

namespace WCFUtils.Usage
{
    //[InjectableBehavior]
    [LogErrorHandler]
    //[System.ServiceModel.Activation.AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    public class Service : ITestService
    {
        public Person GetPerson()
        {
            return new Person
            {
                FirstName = "First",
                LastName = "Last",
                BirthDay = new DateTime(1993, 4, 17, 2, 51, 37, 47, DateTimeKind.Local),
                Id = 0,
                Pets = new List<Pet>
                {
                    new Pet { Name= "Generic Pet 1", Color = "Beige", Id = 0, Markings = "Some markings" },
                    new Pet { Name= "Generic Pet 2", Color = "Gold", Id = 0, Markings = "Other markings" },
                },
            };
        }

        public Pet EchoPet(Pet pet)
        {
            return pet;
        }

        public int Add(int x, int y)
        {
            return x + y;
        }
        public int AddGet(int x, int y) {
            return Add(x, y);
        }
        public int Throw(int type) {
            throw CommonFault.Exception(new CommonFault("Configuration error"));
        }
        public int ThrowGet(int type) {
            return Throw(type);
        }
    }
}
