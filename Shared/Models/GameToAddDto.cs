using System.ComponentModel.DataAnnotations;

namespace AuthTemplate.Shared.Models;
// מחלקה שאנחנו מעבירים בה את הפרטים של המשחק לצורך יצירת משחק חדש
//ה-DTO משמש אותנו גם לעמוד של הגדרות כלליות, שכן יש כאן כל התכונות הרלוונטיות
public class GameToAddDto
{
    [Required(ErrorMessage = "שדה חובה")]
    [MinLength(1, ErrorMessage = "יש להזין לפחות תו אחד")]
    [StringLength(20, ErrorMessage = "מספר התווים המקסימלי הוא 20")]
    public string gameName { get; set; } 
   
    [Range(0, 1000, ErrorMessage = "הזמן לשאלה חייב להיות בין 45 ל-180 שניות או ללא הגבלה")]
    public int time { get; set; }
    
    public bool hasPotion { get; set; }
    
    public bool isPublish { get; set; }
    public bool canPublish { get; set; }
    
}