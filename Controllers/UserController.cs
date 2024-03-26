using Awsome.Data;
using Awsome.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.EntityFrameworkCore;

namespace Awsome.Controllers
{
    public class UserController : Controller
    {
        private ApplicationDbContext _db;
        public UserController(ApplicationDbContext db)
        {
            _db = db;
        }
        public IActionResult Index()
        {
            List<ApplicationUser> applicationUsers = _db.ApplicationUsers.Include(u=>u.Company).ToList();
            var userRoles = _db.UserRoles.ToList();
            var roles = _db.Roles.ToList();
            foreach(var applicationUser in applicationUsers)
            {
                var roleId = userRoles.FirstOrDefault(u => u.UserId == applicationUser.Id).RoleId;
                applicationUser.Role = roles.FirstOrDefault(u => u.Id == roleId).Name;
                
                var today = DateTime.Now;
                applicationUser.Today = today;
                if(applicationUser.Company==null)
                {
                    applicationUser.Company = new Company()
                    {
                        Name = ""
                    };
                }
            }
            return View(applicationUsers);
        }
        [HttpPost]
        public IActionResult LockUnlock([FromBody]string id)
        {
            var objFromDb = _db.ApplicationUsers.FirstOrDefault(u=>u.Id == id);
            if(objFromDb == null)
            {
                //TempData["success"] = "Error while Locking/Unlocking";
                return Json(new { success=false});
            }
            if(objFromDb.LockoutEnd!=null && objFromDb.LockoutEnd>DateTime.Now)
            {
                objFromDb.LockoutEnd = DateTime.Now;
            }
            else
            {
                objFromDb.LockoutEnd = DateTime.Now.AddYears(10);
            }
            _db.SaveChanges();


            List<ApplicationUser> applicationUsers = _db.ApplicationUsers.Include(u => u.Company).ToList();
            var userRoles = _db.UserRoles.ToList();
            var roles = _db.Roles.ToList();
            foreach (var applicationUser in applicationUsers)
            {
                var roleId = userRoles.FirstOrDefault(u => u.UserId == applicationUser.Id).RoleId;
                applicationUser.Role = roles.FirstOrDefault(u => u.Id == roleId).Name;

                var today = DateTime.Now;
                applicationUser.Today = today;
                if (applicationUser.Company == null)
                {
                    applicationUser.Company = new Company()
                    {
                        Name = ""
                    };
                }
            }

            return Json(new { success=true, user = applicationUsers });
        }
    }
}
