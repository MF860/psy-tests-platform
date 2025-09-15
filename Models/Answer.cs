namespace psy_tests_platform.Models
{
    public class Answer
    {
        public int Id { get; set; }
        public object? Value { get; set; }
        public DateTime AnsweredAt { get; set; } = DateTime.UtcNow;
    }
}
