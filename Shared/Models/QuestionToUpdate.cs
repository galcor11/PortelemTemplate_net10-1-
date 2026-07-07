namespace AuthTemplate.Shared.Models;
//מחלקה שפתחנו לצורך עדכון של ההנחיה והקצוות של השאלה
public class QuestionToUpdate
{
    public string instruction { get; set; }
    public string startLabel{ get; set; }
    public string endLabel{ get; set; }
}