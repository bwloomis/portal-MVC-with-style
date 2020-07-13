using System;
using System.Linq;
using System.ComponentModel.DataAnnotations;

using Assmnts.Infrastructure;

using Data.Concrete;

namespace Assmnts.Models
{
    public class PreferenceModel
    {

       /*
        public string FirstName { get; set; }

       
        public string LastName { get; set; }

       
        public string PhoneNumber { get; set; }
        
      
        public string Position { get; set; }

       
        public string Address { get; set; }

       
        public string  StateProvince { get; set; }

        
        public string MiddleName { get; set; }

      
        public string Email  { get; set; }

       
        public string Extension { get; set; }

       
        public string Organization { get; set; }

       
        public string City { get; set; }

      
        public string Zip { get; set; }
        */

 
        
        [Required]
        [DataType(DataType.Text)]
        [Display(Name = "Change the number of assessments shown at a time:")]
        public string numAssmnts { get; set; }

    //  [Required]
        [DataType(DataType.Text)]
        [Display(Name = "Change the number of users records shown at a time*: *Note: only used if you have access to the user administrative module (i.e. \"Add / Modify User permission\")")]
        public string numRecords { get; set; }
        
             
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPasswordOne { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Verify New Password")]
        public string NewPasswordTwo { get; set; }

        public string encryptedPassword { get; set; }
        public bool passWrong { get; set; }
        public bool passNoMatch { get; set; }
        public uas_User user { get; set; }

        public uas_UserPhone phone { get; set; }

        public uas_UserAddress address { get; set; }

        public uas_UserEmail email { get; set; }

        public uas_Enterprise enterprise { get; set; }

        public UASEntities db { get; set; }

        public bool ChangePass { get; set; } 

        public PreferenceModel()
        {
            db = DataContext.getUasDbContext();

            string loginId = SessionHelper.LoginInfo.LoginID;

            //context.Configuration.ValidateOnSaveEnabled = false;

            user = db.uas_User.Where(u => u.LoginID == loginId).FirstOrDefault();
            phone = user.uas_UserPhone.FirstOrDefault();

            if (phone == null)
            {
                phone = new uas_UserPhone();
                phone.UserID = user.UserID;
                phone.uas_User = user;

                phone.PhoneNumber = String.Empty;
                phone.PhoneType = "Primary";
                phone.SortOrder = 1;
                phone.StatusFlag = "A";
                phone.CreatedDate = DateTime.Now;
                phone.CreatedBy = SessionHelper.LoginStatus.UserID;

                db.uas_UserPhone.Add(phone);
                db.SaveChanges();

                phone = user.uas_UserPhone.FirstOrDefault();
            }

            address = user.uas_UserAddress.FirstOrDefault();

            if (address == null)
            {
                address = new uas_UserAddress();
                address.UserID = user.UserID;
                address.uas_User = user;
                address.AddressType = "Primary";
                address.Address1 = String.Empty;
                address.Address2 = String.Empty;
                address.City = String.Empty;
                address.StateProvince = String.Empty;
                address.SortOrder = 1;
                address.StatusFlag = "A";
                address.CreatedDate = DateTime.Now;
                address.CreatedBy = SessionHelper.LoginStatus.UserID;

                db.uas_UserAddress.Add(address);
                db.SaveChanges();

                address = user.uas_UserAddress.FirstOrDefault();
            }

            email = user.uas_UserEmail.FirstOrDefault();

            if (email == null)
            {
                email = new uas_UserEmail();
                email.UserID = user.UserID;
                email.uas_User = user;
                email.EmailType = "Primary";
                email.EmailAddress = String.Empty;
                email.SortOrder = 1;
                email.StatusFlag = "A";
                email.CreatedDate = DateTime.Now;
                email.CreatedBy = SessionHelper.LoginStatus.UserID;

                db.uas_UserEmail.Add(email);
                db.SaveChanges();

                email = user.uas_UserEmail.FirstOrDefault();
            }

            enterprise = user.uas_Enterprise;

        }
       
    }
}