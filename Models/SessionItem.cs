namespace psy_tests_platform.Models
{
    public class SessionItem
    {
        public int Id { get; set; }
        public int SessionId { get; set; }
        public int TestItemId { get; set; }
        public int? AnswerId { get; set; }
        public long? ResponseTimeMs { get; set; }

        // Navigation properties
        public TestItem? TestItem { get; set; }
        public Answer? Answer { get; set; }
    }
}
