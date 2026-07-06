namespace AuthTemplate.Shared.Models;
// מחלקת DTO שפתחנו כדי להעביר את שם המשחק עם רשימת השאלות שלו מהקונטרולר אל עמוד העריכה 

public class GameEditDto
{
    public string gameName { get; set; } 
    public List<Question> Questions { get; set; }
    public int gameID { get; set; }
   
}
