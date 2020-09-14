using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JsonToCsharp
{
    public class Clazz
    {
        public Clazz(string name)
        {
            Name = name;
        }

        public string Name { get; set; }
        public IList<Property> Properties { get; set; } = new List<Property>();

        public string GetClassName()
        {
            bool nextUpper = true;
            StringBuilder newTypeName = new StringBuilder();

            foreach (char c in Name)
            {
                if (!char.IsLetterOrDigit(c) && c != '_')
                    continue;
                if (newTypeName.Length == 0 && !char.IsLetter(c))
                    continue;

                if (nextUpper)
                {
                    newTypeName.Append(char.ToUpper(c));
                    nextUpper = false;
                    continue;
                }
                if (c == '_')
                {
                    nextUpper = true;
                    continue;
                }
                
                newTypeName.Append(c);
            }

            return newTypeName.ToString();
        }

        public void AddProperty(Property property)
        {
            if (Properties.All(x => x.JsonName != property.JsonName))
                Properties.Add(property);
        }

        public override string ToString()
        {
            return $"{Name}: {GetClassName()} /w {Properties.Count} Properties";
        }
    }
}