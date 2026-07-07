namespace AuthTemplate.Shared.Models;
// מחלקת משחקים שפתחנו כדי שיהיה אפשר לשלוף את נתוני המשחק

public class GamesDto
{
    public string gameName { get; set; }
    public int gameID { get; set; }
    public bool hasPotion { get; set; }
    public bool isPublish { get; set; }
    public int time { get; set; }
    public List<QuestionsDto> Questions { get; set; }

}