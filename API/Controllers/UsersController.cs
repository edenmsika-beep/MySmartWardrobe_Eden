using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using DAL;
using Model;

namespace MySmartWardrobe.Controllers
{
    /// <summary>
    /// נקודות הקצה של טבלת המשתמשים. כל פעולה מפעילה שאילתה אחת ממחלקת הגישה למשתמשים (UserDB).
    /// הבקר מקבל בקשה ברשת, קורא לשכבת הגישה לנתונים ומחזיר תשובה עם קוד מצב מתאים.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly UserDB userDB;

        // מחלקת הגישה למשתמשים מגיעה אוטומטית מהרישום בקובץ ההפעלה (Dependency Injection)
        public UsersController(UserDB userDB)
        {
            this.userDB = userDB;
        }

        // שליפת כל המשתמשים. מחזיר קוד 200 ואת הרשימה
        [HttpGet]
        public async Task<ActionResult<List<User>>> GetAll()
        {
            return Ok(await userDB.GetAllUsersAsync());
        }

        // שליפת משתמש לפי מזהה. המזהה מגיע מסוף הכתובת וחייב להיות מספר שלם.
        // מחזיר 404 אם המשתמש לא נמצא
        [HttpGet("{id:int}")]
        public async Task<ActionResult<User>> GetById(int id)
        {
            User? user = await userDB.GetUserByIdAsync(id);
            return user == null ? NotFound() : Ok(user);
        }

        // בדיקה אם שם משתמש תפוס. מחזיר אמת או שקר
        [HttpGet("exists/{username}")]
        public async Task<ActionResult<bool>> UsernameExists(string username)
        {
            return Ok(await userDB.UsernameExistsAsync(username));
        }

        // בדיקה אם אימייל תפוס. מחזיר אמת או שקר
        [HttpGet("email-exists/{email}")]
        public async Task<ActionResult<bool>> EmailExists(string email)
        {
            return Ok(await userDB.EmailExistsAsync(email));
        }

        // ספירת המשתמשים בכל תפקיד
        [HttpGet("count-by-role")]
        public async Task<ActionResult<Dictionary<string, int>>> CountByRole()
        {
            return Ok(await userDB.CountUsersByRoleAsync());
        }

        // הוספת משתמש חדש.
        // פרטי המשתמש מגיעים בגוף הבקשה: שם משתמש, אימייל וסיסמה.
        // שם התפקיד מגיע בכתובת, וברירת המחדל היא "User" (בהתאם לטבלת Roles)
        [HttpPost]
        public async Task<ActionResult<User>> Add([FromBody] User user, [FromQuery] string roleName = "User")
        {
            // קוד 409: שם המשתמש או האימייל כבר תפוסים
            if (await userDB.UsernameExistsAsync(user.Username))
                return Conflict("Username already exists.");

            if (await userDB.EmailExistsAsync(user.Email))
                return Conflict("Email already exists.");

            try
            {
                await userDB.AddUserAsync(user, roleName);
            }
            catch (InvalidOperationException ex)
            {
                // תפקיד שלא קיים בטבלת התפקידים: קוד 400, בקשה שגויה
                return BadRequest(ex.Message);
            }

            // קוד 201: המשתמש נוצר. התשובה כוללת את המשתמש ואת הכתובת שבה אפשר לשלוף אותו
            return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
        }

        // התחברות. גוף הבקשה: שם משתמש וסיסמה.
        // מחזיר את המשתמש אם הפרטים נכונים, וקוד 401 אם לא
        [HttpPost("login")]
        public async Task<ActionResult<User>> Login([FromBody] User credentials)
        {
            User? user = await userDB.CheckLoginAsync(credentials.Username, credentials.PasswordHash);
            return user == null ? Unauthorized() : Ok(user);
        }

        // שינוי תפקיד. שם התפקיד החדש מגיע בכתובת.
        // קוד 204: העדכון הצליח ואין תוכן להחזיר
        [HttpPut("{id:int}/role")]
        public async Task<IActionResult> UpdateRole(int id, [FromQuery] string roleName)
        {
            return await userDB.UpdateUserRoleAsync(id, roleName) ? NoContent() : NotFound();
        }

        // עדכון סיסמה. גוף הבקשה: הסיסמה החדשה כמחרוזת
        [HttpPut("{id:int}/password")]
        public async Task<IActionResult> UpdatePassword(int id, [FromBody] string newPassword)
        {
            return await userDB.UpdatePasswordAsync(id, newPassword) ? NoContent() : NotFound();
        }

        // עדכון אימייל. גוף הבקשה: האימייל החדש כמחרוזת.
        // קודם בודקים שהאימייל לא שייך למשתמש אחר
        [HttpPut("{id:int}/email")]
        public async Task<IActionResult> UpdateEmail(int id, [FromBody] string newEmail)
        {
            if (await userDB.EmailExistsAsync(newEmail))
                return Conflict("Email already exists.");

            return await userDB.UpdateEmailAsync(id, newEmail) ? NoContent() : NotFound();
        }

        // מחיקת משתמש לפי מזהה
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            return await userDB.DeleteUserAsync(id) ? NoContent() : NotFound();
        }
    }
}
