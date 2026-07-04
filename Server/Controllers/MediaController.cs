using Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

    //קונטרולר שמשמש לצורך העלאת תמונה
namespace AuthTemplate.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MediaController : ControllerBase
    {
        //משתנה פנימי בשביל מחלקת העזר
        private readonly FilesManage _filesManage;   
        
       //מאפשר לפנות דרך הקונטרולר לשיטת במחלקת העזר
        public MediaController(FilesManage filesManage)
        {
            _filesManage = filesManage;
        } 
        //שיטת פוסט שמקבלת תמונה מהבלייזור ושומרת אותה בַתיקייה
        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile([FromBody] string imageBase64)
        {
            // הפונקציה מקבלת את התמונה מממשק המשתמש, קובעת את הסיומת (png), 
            // ושומרת אותה בַּתיקייה uploadedFiles
            string fileName = await _filesManage.SaveFile(imageBase64, "png", "uploadedFiles");
    
            // מחזירים לבלייזור את השם הֶחדש שהתמונה קיבלה בתיקיית הקבצים
            return Ok(fileName);
        }
        
        // שיטת POST שמקבלת רשימת שמות של תמונות ומוחקת אותן מתיקיית הקבצים בשרת
        [HttpPost("deleteImages")]
        public async Task<IActionResult> DeleteImages([FromBody] List<string> images)
        {
            var countFalseTry = 0;
            
            // לולאה שעוברת לנו על כל תמונה ברשימה ומבצעת מחיקה
            foreach (string img in images)
            {
                // אנחנו קוראים לפונקציית המחיקה ממחלקת העזר
                if (_filesManage.DeleteFile(img, "uploadedFiles") == false)
                {
                    countFalseTry++; // אם המחיקה כשלה, נספור את הניסיון 
                }
            }
            
            // אם נכשלנו בביצוע של מחיקה אחת או יותר, נחזיר שגיאה
            if (countFalseTry > 0)
            {
                // לעורך אין גישה לשרת ולא יוכל לתקן דבר. הבדיקה משמשת אותנו לצורך דיווח וידיעה
                return BadRequest("problem with " + countFalseTry.ToString() + " images");
            }
            
            // אם המחיקה עברה בהצלחה
            return Ok("deleted");
        }
        
    }
}
