
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using ClinicManagement.DAL;
using ClinicManagement.Models;

namespace ClinicManagement.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly ClinicDbContext db = new ClinicDbContext();

        // DANH SÁCH TÀI KHOẢN
        public ActionResult Index()
        {
            var users = db.Users
                .Include(u => u.Role)
                .Include(u => u.Doctor)
                .OrderBy(u => u.Username)
                .ToList();

            return View(users);
        }

        // GET: User/Create
        public ActionResult Create()
        {
            LoadRoles();
            LoadDoctors();
            return View();
        }

        // POST: User/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(User user, string password)
        {
            if (user == null)
                return new HttpStatusCodeResult(400);

            ModelState.Remove("PasswordHash");
            ModelState.Remove("Doctor");

            ValidateUsername(user.Username, null);

            if (string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError(
                    "Password",
                    "Vui lòng nhập mật khẩu."
                );
            }
            else if (password.Length < 10)
            {
                ModelState.AddModelError(
                    "Password",
                    "Mật khẩu phải có ít nhất 10 ký tự."
                );
            }

            var selectedRole = db.Roles.Find(user.RoleID);

            if (selectedRole == null)
            {
                ModelState.AddModelError(
                    "RoleID",
                    "Vui lòng chọn quyền hợp lệ."
                );
            }
            else
            {
                ValidateDoctorAssignment(
                    selectedRole.RoleName,
                    user.DoctorID,
                    null
                );
            }

            if (ModelState.IsValid)
            {
                user.Username = user.Username.Trim();
                user.PasswordHash =
                    PasswordHelper.HashPassword(password);

                user.IsActive = true;

                if (selectedRole.RoleName != "Doctor")
                    user.DoctorID = null;

                db.Users.Add(user);
                db.SaveChanges();

                TempData["Success"] =
                    "Thêm tài khoản thành công!";

                return RedirectToAction("Index");
            }

            LoadRoles(user.RoleID);
            LoadDoctors(user.DoctorID);

            return View(user);
        }

        // GET: User/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return HttpNotFound();

            var user = db.Users
                .Include(u => u.Role)
                .Include(u => u.Doctor)
                .FirstOrDefault(u => u.UserID == id.Value);

            if (user == null)
                return HttpNotFound();

            LoadRoles(user.RoleID);
            LoadDoctors(user.DoctorID);

            return View(user);
        }

        // POST: User/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(
            [Bind(Include =
            "UserID,Username,FullName,Email,RoleID,DoctorID")] User user)
        {
            if (user == null)
                return new HttpStatusCodeResult(400);

            ModelState.Remove("PasswordHash");
            ModelState.Remove("Doctor");

            var existingUser = db.Users
                .Include(u => u.Role)
                .FirstOrDefault(u => u.UserID == user.UserID);

            if (existingUser == null)
                return HttpNotFound();

            ValidateUsername(user.Username, user.UserID);

            var selectedRole = db.Roles.Find(user.RoleID);

            if (selectedRole == null)
            {
                ModelState.AddModelError(
                    "RoleID",
                    "Quyền được chọn không hợp lệ."
                );
            }
            else
            {
                ValidateDoctorAssignment(
                    selectedRole.RoleName,
                    user.DoctorID,
                    user.UserID
                );
            }

            // Không cho Admin tự hạ quyền của chính mình
            if (existingUser.Username == User.Identity.Name &&
                (selectedRole == null ||
                 selectedRole.RoleName != "Admin"))
            {
                ModelState.AddModelError(
                    "RoleID",
                    "Bạn không thể tự hạ quyền Admin của chính mình."
                );
            }

            // Không cho hạ quyền Admin cuối cùng đang hoạt động
            if (existingUser.IsActive &&
                existingUser.Role != null &&
                existingUser.Role.RoleName == "Admin" &&
                (selectedRole == null ||
                 selectedRole.RoleName != "Admin"))
            {
                int activeAdmins = db.Users
                    .Include(u => u.Role)
                    .Count(u =>
                        u.IsActive &&
                        u.Role != null &&
                        u.Role.RoleName == "Admin"
                    );

                if (activeAdmins <= 1)
                {
                    ModelState.AddModelError(
                        "RoleID",
                        "Không thể hạ quyền Admin cuối cùng đang hoạt động."
                    );
                }
            }

            if (ModelState.IsValid)
            {
                existingUser.Username = user.Username.Trim();
                existingUser.FullName = user.FullName;
                existingUser.Email = user.Email;
                existingUser.RoleID = user.RoleID;

                if (selectedRole.RoleName == "Doctor")
                {
                    existingUser.DoctorID = user.DoctorID;
                }
                else
                {
                    existingUser.DoctorID = null;
                }

                db.SaveChanges();

                TempData["Success"] =
                    "Cập nhật tài khoản thành công!";

                return RedirectToAction("Index");
            }

            user.IsActive = existingUser.IsActive;

            LoadRoles(user.RoleID);
            LoadDoctors(user.DoctorID);

            return View(user);
        }

        // POST: User/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleStatus(int id)
        {
            var user = db.Users
                .Include(u => u.Role)
                .FirstOrDefault(u => u.UserID == id);

            if (user == null)
                return HttpNotFound();

            // Không cho khóa tài khoản đang đăng nhập
            if (user.Username == User.Identity.Name)
            {
                TempData["Error"] =
                    "Bạn không thể khóa tài khoản đang đăng nhập.";

                return RedirectToAction("Index");
            }

            // Không cho khóa Admin cuối cùng
            if (user.IsActive &&
                user.Role != null &&
                user.Role.RoleName == "Admin")
            {
                int activeAdmins = db.Users
                    .Include(u => u.Role)
                    .Count(u =>
                        u.IsActive &&
                        u.Role != null &&
                        u.Role.RoleName == "Admin"
                    );

                if (activeAdmins <= 1)
                {
                    TempData["Error"] =
                        "Không thể khóa Admin cuối cùng đang hoạt động.";

                    return RedirectToAction("Index");
                }
            }

            user.IsActive = !user.IsActive;

            db.SaveChanges();

            TempData["Success"] = user.IsActive
                ? "Mở khóa tài khoản thành công!"
                : "Khóa tài khoản thành công!";

            return RedirectToAction("Index");
        }

        // GET: User/ResetPassword/5
        public ActionResult ResetPassword(int? id)
        {
            if (id == null)
                return HttpNotFound();

            var user = db.Users.Find(id.Value);

            if (user == null)
                return HttpNotFound();

            return View(user);
        }

        // POST: User/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResetPassword(
            int UserID,
            string newPassword,
            string confirmPassword)
        {
            var user = db.Users.Find(UserID);

            if (user == null)
                return HttpNotFound();

            if (string.IsNullOrWhiteSpace(newPassword))
            {
                ModelState.AddModelError(
                    "newPassword",
                    "Vui lòng nhập mật khẩu mới."
                );
            }
            else if (newPassword.Length < 10)
            {
                ModelState.AddModelError(
                    "newPassword",
                    "Mật khẩu phải có ít nhất 10 ký tự."
                );
            }

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError(
                    "confirmPassword",
                    "Mật khẩu xác nhận không khớp."
                );
            }

            if (ModelState.IsValid)
            {
                user.PasswordHash =
                    PasswordHelper.HashPassword(newPassword);

                db.SaveChanges();

                TempData["Success"] =
                    "Đặt lại mật khẩu cho tài khoản " +
                    user.Username + " thành công!";

                return RedirectToAction("Index");
            }

            return View(user);
        }

        // KIỂM TRA TÊN ĐĂNG NHẬP
        private void ValidateUsername(
            string username,
            int? excludeUserID)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                ModelState.AddModelError(
                    "Username",
                    "Vui lòng nhập tên đăng nhập."
                );

                return;
            }

            username = username.Trim();

            if (username.Length > 50)
            {
                ModelState.AddModelError(
                    "Username",
                    "Tên đăng nhập không được vượt quá 50 ký tự."
                );

                return;
            }

            bool duplicated = db.Users.Any(u =>
                u.Username == username &&
                (!excludeUserID.HasValue ||
                 u.UserID != excludeUserID.Value)
            );

            if (duplicated)
            {
                ModelState.AddModelError(
                    "Username",
                    "Tên đăng nhập đã tồn tại."
                );
            }
        }

        // KIỂM TRA LIÊN KẾT TÀI KHOẢN - BÁC SĨ
        private void ValidateDoctorAssignment(
            string roleName,
            int? doctorID,
            int? excludeUserID)
        {
            if (roleName != "Doctor")
                return;

            if (!doctorID.HasValue)
            {
                ModelState.AddModelError(
                    "DoctorID",
                    "Vui lòng chọn hồ sơ bác sĩ."
                );

                return;
            }

            bool doctorExists = db.Doctors.Any(d =>
                d.DoctorID == doctorID.Value &&
                d.IsActive
            );

            if (!doctorExists)
            {
                ModelState.AddModelError(
                    "DoctorID",
                    "Hồ sơ bác sĩ không tồn tại hoặc đã ngừng hoạt động."
                );

                return;
            }

            bool alreadyAssigned = db.Users.Any(u =>
                u.DoctorID == doctorID.Value &&
                (!excludeUserID.HasValue ||
                 u.UserID != excludeUserID.Value)
            );

            if (alreadyAssigned)
            {
                ModelState.AddModelError(
                    "DoctorID",
                    "Bác sĩ này đã được liên kết với tài khoản khác."
                );
            }
        }

        // NẠP DANH SÁCH QUYỀN
        private void LoadRoles(int? selectedRole = null)
        {
            ViewBag.RoleID = new SelectList(
                db.Roles
                    .OrderBy(r => r.RoleName)
                    .ToList(),
                "RoleID",
                "RoleName",
                selectedRole
            );
        }

        // NẠP DANH SÁCH BÁC SĨ
        private void LoadDoctors(int? selectedDoctor = null)
        {
            ViewBag.DoctorID = new SelectList(
                db.Doctors
                    .Where(d => d.IsActive)
                    .OrderBy(d => d.FullName)
                    .ToList(),
                "DoctorID",
                "FullName",
                selectedDoctor
            );
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }
    }
}
