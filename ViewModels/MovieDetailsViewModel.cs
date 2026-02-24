using SP2026_Assignment3_amcdavid.Models;

namespace SP2026_Assignment3_amcdavid.ViewModels
{
    public class MovieDetailsViewModel
    {
        public Movie Movie { get; set; }
        public List<(string Comment, string Sentiment)> RedditSentiments { get; set; } = new();
        public string OverallSentiment { get; set; } = string.Empty;
    }
}