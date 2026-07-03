using Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UsersManager.Server;

//קונטרולר שפתחנו כדי לנהל את הקריאות בין הקונטרולרים ולטפל במצבי פרסום
namespace AuthTemplate.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    
    [ServiceFilter(typeof(AuthCheck))] //בדיקה שהמשתמש מחובר
    public class BaseController : ControllerBase
    {
        //קישור ל-DBREPOSITORY
        private readonly DbRepository _db;
        public BaseController( DbRepository db) 
        { 
            _db = db;
            // בתוך הקיימות השיטות אל לפנות מאפשר DBRepository
        }
        
        [HttpGet]
        public async Task<ActionResult<int>> GetLoginUser(int authUserId)
        {
            return Ok(authUserId);
        }
        
        public async Task<int> SaveChanges(int gameID, string query, object param)
        {
            int isSuccessful = 0;
            
            // מצב הוספה מטופל באופן שונה ממצבי עדכון ושמירה (InsertReturnIdAsync vs. SaveDataAsync) 
            // לכן נבדוק האם בשאילתה יש את המילה INSERT ובכדי לוודא שלא יהיו בעיות של אותיות קטנות/גדולות נעשה ToUpper() ובכך כל השאילתה תהיה בוודאות באותיות גדולות לפני הבדיקה
            if (query.ToUpper().Contains("INSERT")) 
            {
                isSuccessful = await _db.InsertReturnIdAsync(query, param);
            }
            else
            {
                isSuccessful = await _db.SaveDataAsync(query, param);
            }
                
            await CanPublishFunc(gameID); //ככה אין מצב שעושים שינוי ללא
            
            return isSuccessful;
        }

        // שכפול של הפונקציה הנמצאת בקונטרולר המשחקים כדי שיהיה נוח לקרוא לה מהקונטרולר הזה (של השאלות)
        public async Task<bool> CanPublishFunc(int gameId)
        {
            //במקרה שלנו - התנאי לפרסום משחק הוא לפחות שלוש שאלות
            //יש לשנות את השיטה בהתאם לתנאי הפרסום עליהם החלטתם
            int minQuestions = 3;
            //אצלנו יש מינימום 4 פריטים בשאלה כדי לפרסם את המשחק ומקסימום 8 פריטים בשאלה. 
            int minItemsPerQuestion = 4;
            int maxItemsPerQuestion = 8;

            //משתנה לשמירה של הסטטוס - האם ניתן לפרסום
            bool canPublish = false;

            object param = new{
                ID = gameId
            };

            //שאילתה שבודקת כמה שאלות יש במשחק
            string queryQuestionCount = "SELECT Count(questionID) FROM Questions WHERE gameID=@ID";
            var recordQuestionCount = await _db.GetRecordsAsync<int>(queryQuestionCount, param);
            int numberOfQuestions = recordQuestionCount.FirstOrDefault();

            //נשמור משתנה ריק שיכיל את שאילתת העדכון בהתאם למספר השאלות
            string updateQuery;
            //אם יש מספיק שאלות במשחק
            if (numberOfQuestions >= minQuestions) 
            {
                // אנחנו שולפים את המזהים של השאלות ששייכות למשחק הזה
                string queryGetQuestions = "SELECT questionID FROM Questions WHERE gameID=@ID";
                var questionIds = await _db.GetRecordsAsync<int>(queryGetQuestions, param);

                // נקודת המוצא היא שניתן לפרסם, אלא אם הלולאה תמצא שאלה לא תקינה
                canPublish = true;

                // לולאה שעוברת שאלה-שאלה ובודקת את הפריטים 
                foreach (int qId in questionIds)
                {
                    // אובייקט פרמטרים שמכיל לנו את המזהה של השאלה הנוכחית בלולאה
                    object itemParam = new { QID = qId };

                    // ספירת הפריטים ששייכים אך ורק לשאלה הספציפית הזו
                    string queryItemCount = "SELECT Count(answerID) FROM Items WHERE questionID=@QID";
                    var recordItemCount = await _db.GetRecordsAsync<int>(queryItemCount, itemParam);
                    int itemsInThisQuestion = recordItemCount.FirstOrDefault(); // משתנה שמכיל בתוכו את מספר הפריטים שיש בשאלה

                    // אנחנו בודקים אם כמות הפריטים חורגת מהטווח (פחות מהמינימום או יותר מהמקסימום שהגדרנו)
                    if (itemsInThisQuestion < minItemsPerQuestion || itemsInThisQuestion > maxItemsPerQuestion)
                    {
                        canPublish = false; // המשחק נפסל לפרסום
                        break; //יציאה מהלולאה
                    }
                }
            }
            // אנחנו מעדכנים את מצב הפרסום בטבלת המשחקים בהתאם לתוצאה 
            if (canPublish == true) 
            {
                updateQuery = "UPDATE Games SET canPublish=true WHERE gameID=@ID";
            }
            else
            {
                updateQuery = "UPDATE Games SET isPublish=false, canPublish=false WHERE gameID=@ID";
            }

            //מעדכנים את בסיס הנתונים
            int isUpdate = await _db.SaveDataAsync(updateQuery, param);
            
            //נחזיר משתנה בוליאני שאומר אם ניתן לפרסם את המשחק או לא
            return canPublish;
        }
    }
}
