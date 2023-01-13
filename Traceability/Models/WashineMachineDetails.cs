namespace Traceability.Models
{
    public class WashineMachineDetails
    {
        public string RDummy { get; set; }
        public string LDummy { get; set; }
        public string DummyProduct { get; set; }
        public bool IsCorrectRDummy { get => System.Text.RegularExpressions.Regex.IsMatch(RDummy, @"^R\d{12}$"); }
        public bool IsCorrectLDummy { get => System.Text.RegularExpressions.Regex.IsMatch(RDummy, @"^L\d{12}$"); }
    }
}