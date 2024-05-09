namespace Ensek.PeteForrest.Domain {
    public class MeterReading {
        public int MeterReadingId { get; set; }

        public Account Account { get; set; }

        public DateTime DateTime { get; set; }

        public int Value { get; set; }

        public static bool TryParseValue(string value, out int result)
        {
            if (value.Length == 5 && value.All(char.IsNumber)) return int.TryParse(value, out result);

            result = 0;
            return false;
        }
    }
}
