namespace AuthTemplate.Shared.Models;
//מחלקה ל"משחקים שלי". 
public class GameToTableDto
{
    public string gameName { get; set; }
    public int gameCode { get; set; }
    public int gameID{ get; set; }
    public bool canPublish { get; set; }
    public bool isPublish { get; set; }
    public int questionCount { get; set; }
    
}