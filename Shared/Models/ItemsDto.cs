namespace AuthTemplate.Shared.Models;
// מחלקת DTO שפתחנו כדי להעביר את נתוני הפריטים (גוף התולעת) לקונטרולר היוניטי (צד השחקן).
public class ItemsDto
{
    public int answerID { get; set; }
    public string content { get; set; }
    public bool isImage { get; set; }
    public int orderIndex { get; set; }
}