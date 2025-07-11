namespace Ensek.PeteForrest.Domain {
    public class Account {
        public int AccountId { get; set; }

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public int? CurrentMeterReadingId { get; set; }

        public MeterReading? CurrentMeterReading { get; set; }

        public ICollection<MeterReading> MeterReadings { get; set; } = default!;
    }
}
