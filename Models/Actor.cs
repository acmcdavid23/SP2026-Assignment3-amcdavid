namespace SP2026_Assignment3_amcdavid.Models
{
    public class Actor
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public int Age { get; set; }
        public string ImdbUrl { get; set; } = string.Empty;
        public string PhotoUrl { get; set; } = string.Empty;

        public ICollection<MovieActor> MovieActors { get; set; } = new List<MovieActor>();
    }
}
