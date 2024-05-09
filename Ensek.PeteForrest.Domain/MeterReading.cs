namespace Ensek.PeteForrest.Domain {
    public class MeterReading {
        public int MeterReadingId { get; set; }

        public Account Account { get; set; }

        public DateTime DateTime { get; set; }

        public int Value { get; set; }
    }
}
