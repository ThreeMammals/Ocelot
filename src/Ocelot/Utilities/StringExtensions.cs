namespace Ocelot.Utilities
{
    public static class StringExtensions
    {
        public static string SetLastCharacterAs(this string valueToSetLastChar, 
            char expectedLastChar)
        {
            var last = valueToSetLastChar[valueToSetLastChar.Length - 1];

            if (last != expectedLastChar)
            {
                valueToSetLastChar = $"{valueToSetLastChar}{expectedLastChar}";
            }
            return valueToSetLastChar;
        }
    }
}
