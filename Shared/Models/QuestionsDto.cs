namespace AuthTemplate.Shared.Models;
// מחלקת DTO שפתחנו כדי להעביר את פְּרָטי השאלות לקונטרולר היוניטי (צד השחקן)
public class QuestionsDto
{
    public string endLabel { get; set; }
    public string instruction { get; set; }
    public int questionID { get; set; }
    public string startLabel { get; set; }
    public List<ItemsDto> Items { get; set; }

}