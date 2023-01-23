using bulkybookshop.DataAccess.Data;
using bulkybookshop.Models;
using bulkybookshop.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bulkybookshop.DataAccess.DbInitializer
{
    public class DbInitializer : IDbInitializer
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _db;

        public DbInitializer(
            RoleManager<IdentityRole> roleManager,
            UserManager<IdentityUser> userManager,
            ApplicationDbContext db)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _db = db;
        }
        public void Initialize()
        {
            //Migrations if they are not applied
            try
            {
                if (_db.Database.GetPendingMigrations().Count() > 0)
                {
                    _db.Database.Migrate();
                }
            }
            catch (Exception ex)
            {

            }

            //create roles if they not created

            if (!_roleManager.RoleExistsAsync(SD.Role_Admin).GetAwaiter().GetResult())
            {
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Admin)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Employee)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(SD.Role_User_Indi)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(SD.Role_User_Comp)).GetAwaiter().GetResult();


                //if roles are not created, then we will create admin user as well

                _userManager.CreateAsync(new ApplicationUser
                {
                    UserName = "dhirajbhirud2699@gmail.com",
                    Email = "dhirajbhirud2699@gmail.com",
                    Name = "Dhiraj Bhirud",
                    PhoneNumber = "9857286869",
                    StreetAddress = "Grren avenue",
                    State = "Maharashta",
                    PostalCode = "422010",
                    City = "Nashik"
                }, "Admin@45").GetAwaiter().GetResult();

                ApplicationUser user = _db.ApplicationUsers.FirstOrDefault(u => u.Email == "dhirajbhirud2699@gmail.com");
                _userManager.AddToRoleAsync(user, SD.Role_Admin).GetAwaiter().GetResult();
            }

            return;
        }
    }
}