using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.ServiceModel;

namespace WCFUtils.Usage
{
    public class Person
    {
        public string FirstName;
        public string LastName;
        //[Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.IsoDateTimeConverter))]
        public DateTime BirthDay;
        public List<Pet> Pets;
        public int Id;

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Person[");
            sb.AppendFormat(
                "FirstName={0},LastName={1},BirthDay={2},Id={3},Pets",
                this.FirstName,
                this.LastName,
                this.BirthDay.ToLongDateString(),
                this.Id);
            if (this.Pets == null)
            {
                sb.Append("=null");
            }
            else
            {
                sb.AppendFormat("(Count={0})=<", this.Pets.Count);
                for (int i = 0; i < this.Pets.Count; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(',');
                    }

                    sb.Append(this.Pets[i]);
                }

                sb.Append('>');
            }

            sb.Append(']');
            return sb.ToString();
        }
    }

    public class Pet
    {
        public string Name;
        public string Color;
        public string Markings;
        public DateTime? BirthDay;
        public int Id;

        public override string ToString()
        {
            return string.Format(
                "Pet[Name={0},Color={1},Markings={2},BirthDay={3},Id={4}]",
                this.Name,
                this.Color,
                this.Markings,
                this.BirthDay,
                this.Id);
        }
    }
}
