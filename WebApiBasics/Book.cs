namespace WebApiBasics
{
    public class Book
    {
        public int Id { get; set; }

        public string Author { get; set; }

        public int Year { get; set; }

        public string? Summary { get; set; }


        public Book(int id, string author, int year, string summary)
        {
            Id = id;
            Author = author;
            Year = year;
            Summary = summary;
        }
    }
}
