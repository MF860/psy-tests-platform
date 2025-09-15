namespace psy_tests_platform.Models
{
    public class TestItem
    {
        public int Id { get; set; }
        public string ItemType { get; set; } = string.Empty; // MCQ, TimedNumeric, Ordering, Likert, Text
        public string? Content { get; set; }
        public object? CorrectAnswer { get; set; }
        public double MaxScore { get; set; }
        public string? Dimension { get; set; }
        public Dictionary<string, object>? ScoringParameters { get; set; }
    }
}
