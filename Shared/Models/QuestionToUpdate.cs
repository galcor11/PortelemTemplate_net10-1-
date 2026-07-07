namespace AuthTemplate.Shared.Models;

public class QuestionToUpdate
{
    //מחלקה שפתחנו לצורך עדכון של ההנחיה והקצוות של השאלה
    public string instruction { get; set; }
    public string startLabel{ get; set; }
    public string endLabel{ get; set; }
}