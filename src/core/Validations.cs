using System;

namespace BotMarfu.core
{
    public static class Validations
    {
        public static void ValidateInput<T>(T obj, string name)
        {
            if (Equals(obj, default(T)))
            {
                throw new ArgumentException($"Input parameter '{name}' is null. Please correct it.");
            }
        }

        public static void ValidateInput(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException($"Input string parameter '{name}' is null or empty. Please correct it.");
            }
        }
    }
}
