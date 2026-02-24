using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SP2026_Assignment3_amcdavid.Data;
using SP2026_Assignment3_amcdavid.Models;
using SP2026_Assignment3_amcdavid.ViewModels;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SP2026_Assignment3_amcdavid.Controllers
{
    public class ActorsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const int MaxInputLength = 512;

        public ActorsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Actors.ToListAsync());
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Actor actor)
        {
            if (ModelState.IsValid)
            {
                _context.Add(actor);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(actor);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var actor = await _context.Actors.FirstOrDefaultAsync(a => a.Id == id);
            if (actor == null) return NotFound();

            var comments = await SearchRedditAsync(actor.Name);
            var sentiments = new List<(string Comment, string Sentiment)>();
            double totalScore = 0;
            int validResponses = 0;

            foreach (var comment in comments)
            {
                var (label, score) = await GetSentimentAsync(comment);
                var sentimentText = $"{label}: {Math.Round(score * 100, 2)}";
                sentiments.Add((comment, sentimentText));

                double adjustedScore = label == "NEGATIVE" ? -score : score;
                totalScore += adjustedScore;
                validResponses++;
            }

            string overallSentiment = "No data available";
            if (validResponses > 0)
            {
                double avg = totalScore / validResponses;
                overallSentiment = avg >= 0
                    ? $"POSITIVE: {Math.Round(avg * 100, 2)}"
                    : $"NEGATIVE: {Math.Round(Math.Abs(avg * 100), 2)}";
            }

            var viewModel = new ActorDetailsViewModel
            {
                Actor = actor,
                RedditSentiments = sentiments,
                OverallSentiment = overallSentiment
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var actor = await _context.Actors.FindAsync(id);
            if (actor == null) return NotFound();
            return View(actor);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Actor actor)
        {
            if (id != actor.Id) return NotFound();
            if (ModelState.IsValid)
            {
                _context.Update(actor);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(actor);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var actor = await _context.Actors.FirstOrDefaultAsync(a => a.Id == id);
            if (actor == null) return NotFound();
            return View(actor);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var actor = await _context.Actors.FindAsync(id);
            if (actor != null) _context.Actors.Remove(actor);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private static async Task<(string Label, float Score)> GetSentimentAsync(string text)
        {
            var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);
            var url = "https://router.huggingface.co/hf-inference/models/distilbert/distilbert-base-uncased-finetuned-sst-2-english";
            var apiKey = Environment.GetEnvironmentVariable("HuggingFace__ApiKey") ?? "";

            var data = new { inputs = new[] { text } };
            var json = JsonSerializer.Serialize(data);
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(url),
                Headers = { { "Authorization", $"Bearer {apiKey}" } },
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            try
            {
                var response = await httpClient.SendAsync(request);
                var responseString = await response.Content.ReadAsStringAsync();

                try
                {
                    var sentimentResults = JsonSerializer.Deserialize<List<List<SentimentResponse>>>(responseString);
                    if (sentimentResults != null && sentimentResults.Count > 0)
                        return (sentimentResults[0][0].Label, sentimentResults[0][0].Score);
                }
                catch
                {
                    var sentimentResults = JsonSerializer.Deserialize<List<SentimentResponse>>(responseString);
                    if (sentimentResults != null && sentimentResults.Count > 0)
                        return (sentimentResults[0].Label, sentimentResults[0].Score);
                }
            }
            catch { }

            return ("UNKNOWN", 0);
        }

        private static async Task<List<string>> SearchRedditAsync(string searchQuery)
        {
            var returnList = new List<string>();
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");

            try
            {
                string json = await client.GetStringAsync("https://api.pullpush.io/reddit/search/comment/?size=25&q=" + WebUtility.UrlEncode(searchQuery));
                JsonDocument doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("data", out JsonElement dataArray))
                {
                    foreach (JsonElement comment in dataArray.EnumerateArray())
                    {
                        if (comment.TryGetProperty("body", out JsonElement bodyElement))
                        {
                            string textToAdd = bodyElement.GetString() ?? "";
                            if (!string.IsNullOrEmpty(textToAdd))
                            {
                                textToAdd = TruncateToMaxLength(textToAdd, MaxInputLength);
                                returnList.Add(textToAdd);
                            }
                        }
                    }
                }
            }
            catch { }

            return returnList;
        }

        private static string TruncateToMaxLength(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
                return text;
            return text.Substring(0, maxLength);
        }
    }

    public class ActorSentimentResponse
    {
        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;
        [JsonPropertyName("score")]
        public float Score { get; set; }
    }
}