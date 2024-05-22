using System.ComponentModel.DataAnnotations.Schema;

namespace Ensek.PeteForrest.Domain {
    public class Account {
        public int AccountId { get; set; }

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public MeterReading? CurrentReading { get; set; }

        [InverseProperty(nameof(MeterReading.Account))]
        public ICollection<MeterReading> MeterReadings { get; set; } = default!;
    }
}
