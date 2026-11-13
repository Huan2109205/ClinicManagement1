
using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Data.Entity;
using ClinicManagement.DAL;
using ClinicManagement.Models;

namespace ClinicManagement.Controllers
{
    public class AccountController : Controller
    {
        private ClinicDbContext db = new ClinicDbContext();

        // GET: Account/Login
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(
            string username,
            string password,
            string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;

            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ tài khoản và mật khẩu.";
                return View();
            }

            var user = db.Users
                .Include(u => u.Role)
                .FirstOrDefault(u => u.Username == username);

            if (user == null ||
                !user.IsActive ||
                user.Role == null ||
                !PasswordHelper.VerifyPassword(
                    password, user.PasswordHash))
            {
                ViewBag.Error = "Tài khoản hoặc mật khẩu không chính xác.";
                return View();
            }

            // Lưu thông tin quyền vào Forms Authentication Ticket
            FormsAuthenticationTicket ticket =
                new FormsAuthenticationTicket(
                    1,
                    user.Username,
                    DateTime.Now,
                    DateTime.Now.AddMinutes(30),
                    false,
                    user.Role.RoleName
                );

            string encryptedTicket =
                FormsAuthentication.Encrypt(ticket);

            HttpCookie cookie = new HttpCookie(
                FormsAuthentication.FormsCookieName,
                encryptedTicket
            );

            cookie.HttpOnly = true;
            cookie.Secure = Request.IsSecureConnection;
            cookie.Path = FormsAuthentication.FormsCookiePath;

            Response.Cookies.Add(cookie);

            if (!string.IsNullOrEmpty(returnUrl) &&
                Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Dashboard");
        }

        // GET: Account/Logout
        [Authorize]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();

            Session.Clear();
            Session.Abandon();

            return RedirectToAction("Login", "Account");
        }

        // GET: Account/SetupAdmin
        [AllowAnonymous]
        public ActionResult SetupAdmin()
        {
            if (!Request.IsLocal || db.Users.Any())
                return new HttpStatusCodeResult(404);

            return View();
        }

        // POST: Account/SetupAdmin
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult SetupAdmin(
            string username,
            string password,
            string confirmPassword,
            string fullName,
            string email)
        {
            if (!Request.IsLocal || db.Users.Any())
                return new HttpStatusCodeResult(404);

            username = (username ?? "").Trim();
            fullName = (fullName ?? "").Trim();
            email = (email ?? "").Trim();

            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(fullName))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin bắt buộc.";
                return View();
            }

            if (username.Length > 50 ||
                fullName.Length > 100 ||
                email.Length > 100)
            {
                ViewBag.Error = "Thông tin nhập vượt quá độ dài cho phép.";
                return View();
            }

            if (password.Length < 10)
            {
                ViewBag.Error = "Mật khẩu phải có ít nhất 10 ký tự.";
                return View();
            }

            if (password != confirmPassword)
            {
                ViewBag.Error = "Mật khẩu xác nhận không khớp.";
                return View();
            }

            if (db.Users.Any(u => u.Username == username))
            {
                ViewBag.Error = "Tên đăng nhập đã tồn tại.";
                return View();
            }

            var adminRole = db.Roles
                .FirstOrDefault(r => r.RoleName == "Admin");

            if (adminRole == null)
            {
                ViewBag.Error = "Không tìm thấy quyền Admin trong database.";
                return View();
            }

            var admin = new User
            {
                Username = username,
                PasswordHash = PasswordHelper.HashPassword(password),
                FullName = fullName,
                Email = string.IsNullOrWhiteSpace(email) ? null : email,
                RoleID = adminRole.RoleID,
                IsActive = true
            };

            db.Users.Add(admin);
            db.SaveChanges();

            TempData["Success"] =
                "Tạo tài khoản Admin thành công. Hãy đăng nhập.";

            return RedirectToAction("Login");
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }

        // GET: Account/ChangePassword
        [Authorize]
        public ActionResult ChangePassword()
        {
            return View();
        }

        // POST: Account/ChangePassword
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(
            string currentPassword,
            string newPassword,
            string confirmPassword)
        {
            string username = User.Identity.Name;

            var user = db.Users.FirstOrDefault(u => u.Username == username);

            if (user == null || !user.IsActive)
            {
                FormsAuthentication.SignOut();
                return RedirectToAction("Login");
            }

            if (string.IsNullOrWhiteSpace(currentPassword) ||
                !PasswordHelper.VerifyPassword(currentPassword, user.PasswordHash))
            {
                ModelState.AddModelError(
                    "currentPassword",
                    "Mật khẩu hiện tại không chính xác.");
            }

            if (string.IsNullOrWhiteSpace(newPassword))
            {
                ModelState.AddModelError(
                    "newPassword",
                    "Vui lòng nhập mật khẩu mới.");
            }
            else if (newPassword.Length < 10)
            {
                ModelState.AddModelError(
                    "newPassword",
                    "Mật khẩu mới phải có ít nhất 10 ký tự.");
            }
            else if (newPassword == currentPassword)
            {
                ModelState.AddModelError(
                    "newPassword",
                    "Mật khẩu mới phải khác mật khẩu hiện tại.");
            }

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError(
                    "confirmPassword",
                    "Mật khẩu xác nhận không khớp.");
            }

            if (!ModelState.IsValid)
                return View();

            user.PasswordHash = PasswordHelper.HashPassword(newPassword);
            db.SaveChanges();

            FormsAuthentication.SignOut();
            Session.Clear();
            Session.Abandon();

            TempData["Success"] = "Đổi mật khẩu thành công. Vui lòng đăng nhập lại.";

            return RedirectToAction("Login");
        }

    }
}
