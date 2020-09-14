using System.Text;

namespace JsonToCsharp
{
    public class Property
    {
        public string Name { get; set; }
        public string JsonName { get; set; }
        public string Type { get; set; }
        public bool IsList { get; set; }

        public override string ToString()
        {
            return $"{GetPropertyName()} ({Type})";
        }

        public string GetPropertyName()
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

            if (newTypeName.Length == 0)
                newTypeName.Append($"P{Name}");

            string name = newTypeName.ToString();

            if (IsList && !name.EndsWith("s"))
                name += "s";

            return name;
        }
    }
}