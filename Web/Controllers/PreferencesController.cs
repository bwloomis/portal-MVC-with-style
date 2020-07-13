using System;
using System.Diagnostics;
using System.Linq;
using System.Web.Mvc;

using System.Data.Entity.Validation;

using UAS.Business;

using Assmnts.Infrastructure;
using Assmnts.Models;

using Data.Concrete;

namespace Assmnts.Controllers
{
    [RedirectingAction]
    public class PreferencesController : Controller
    {
        
        // Post: Preferences
        [HttpPost]
        public ActionResult Index(PreferenceModel pref)
        {
            if (!SessionHelper.IsUserLoggedIn)
            {
                return RedirectToAction("Index", "Account");
            }
           
            return View("_Preferences2", pref);
        }

        // Post: Preferences
        [HttpPost]
        public ActionResult indexUser(PreferenceModel pref)
        {
            uas_Config config = pref.db.uas_Config.Where(c => c.ConfigName == "NoGridAssmnts").FirstOrDefault();
            if (config != null)
            {
                uas_ProfileConfig profileConfig = pref.db.uas_ProfileConfig.Where(p => p.ConfigID == config.ConfigID).FirstOrDefault();

                if (profileConfig != null)
                {
                    uas_UserProfile userProfile = pref.db.uas_UserProfile.Where(u => u.ProfileConfigID == profileConfig.ProfileConfigID
                                                                                && u.UserID == SessionHelper.LoginStatus.UserID).FirstOrDefault();

                    if (userProfile != null)
                    {
                        pref.numAssmnts = userProfile.OptionSet;
                    }
                }
            }

            return View("_Preferences1", pref);
        }

        // Post: Preferences
        [HttpPost]
        public ActionResult indexPassword(PreferenceModel pref)
        {
            if (!SessionHelper.IsUserLoggedIn)
            {
                return RedirectToAction("Index", "Account");
            }

            if (NeedChangePassword(SessionHelper.LoginStatus.UserID))
            {
                pref.ChangePass = true;
            }
            return View("_Preferences3", pref);
        }

        [HttpPost]
        public ActionResult ChangeNum(PreferenceModel pref)
        {
            uas_Config config = pref.db.uas_Config.Where(c => c.ConfigName == "NoGridAssmnts").FirstOrDefault();

            if (config != null)
            {
                uas_ProfileConfig profileConfig = pref.db.uas_ProfileConfig.Where(p => p.ConfigID == config.ConfigID).FirstOrDefault();

                if (profileConfig != null)
                {
                    uas_UserProfile userProfile = pref.db.uas_UserProfile.Where(u => u.ProfileConfigID == profileConfig.ProfileConfigID
                                                                                && u.UserID == SessionHelper.LoginStatus.UserID).FirstOrDefault();
                    if (userProfile != null)
                    {
                        userProfile.OptionSet = pref.numAssmnts;
                        userProfile.ModifiedBy = SessionHelper.LoginStatus.UserID;
                        userProfile.ModifiedDate = DateTime.Now;
                        pref.db.Entry(userProfile).State = System.Data.Entity.EntityState.Modified;
                        pref.db.SaveChanges();
                    }
                    else
                    {
                        newUserProfile(pref, profileConfig);
                    }
                }
                else
                {
                    profileConfig = newProfileConfig(pref, config);
                    
                    newUserProfile(pref, profileConfig);
                }
            }
            else
            {
                config = newConfig(pref);

                uas_ProfileConfig profileConfig = newProfileConfig(pref, config);

                newUserProfile(pref, profileConfig);

            }

            return RedirectToAction("Index", "Search");
        }

        private uas_Config newConfig(PreferenceModel pref)
        {
            uas_Config config = new uas_Config();

            config.ConfigName = "NoGridAssmnts";
            config.ConfigParams = "number";

            pref.db.uas_Config.Add(config);

            pref.db.SaveChanges();

            config = pref.db.uas_Config.Where(c => c.ConfigName == "NoGridAssmnts").FirstOrDefault();
            
            return config;

        }

        private uas_ProfileConfig newProfileConfig(PreferenceModel pref, uas_Config config)
        {
            uas_ProfileConfig profileConfig = new uas_ProfileConfig();

            profileConfig.ConfigID = config.ConfigID;
            profileConfig.ProfileSet = null;

            pref.db.uas_ProfileConfig.Add(profileConfig);

            pref.db.SaveChanges();

            profileConfig = pref.db.uas_ProfileConfig.Where(p => p.ConfigID == config.ConfigID).FirstOrDefault();

            return profileConfig;

        }


        private void newUserProfile(PreferenceModel pref, uas_ProfileConfig profileConfig)
        {
            uas_UserProfile userProfile = new uas_UserProfile();
            userProfile.ProfileConfigID = profileConfig.ProfileConfigID;
            userProfile.UserID = SessionHelper.LoginStatus.UserID;
            userProfile.OptionSet = pref.numAssmnts;
            userProfile.SortOrder = 0;
            userProfile.StatusFlag = "A";
            userProfile.CreatedBy = SessionHelper.LoginStatus.UserID;
            userProfile.CreatedDate = DateTime.Now;

            pref.db.uas_UserProfile.Add(userProfile);

            pref.db.SaveChanges();
        }

        [HttpPost]
        public string checkOldPass(PreferenceModel pref)
        {

            uas_User user = pref.db.uas_User.Where(u => u.UserID == SessionHelper.LoginStatus.UserID).FirstOrDefault();

            if (UtilityFunction.EncryptPassword(pref.Password) == user.Password)
            {
                return "success";
            }
            else if (String.IsNullOrEmpty(user.Password) && user.ChangePassword && UtilityFunction.EncryptPassword(pref.Password) == user.TempPassword)
            {
                return "success";
            }

            return "fail";
        }
        
        
        [HttpPost]
        public string ChangePassword(PreferenceModel pref)
        {

            uas_User user = pref.db.uas_User.Where(u => u.UserID == SessionHelper.LoginStatus.UserID).FirstOrDefault();
            try
            {
                string encryptedNewPassword = UtilityFunction.EncryptPassword(pref.NewPasswordOne);
                
                user.Password = encryptedNewPassword;
                user.ModifiedBy = SessionHelper.LoginStatus.UserID;
                user.ModifiedDate = DateTime.Now;
                pref.db.Entry(user).State = System.Data.Entity.EntityState.Modified;
                pref.db.SaveChanges();
            }
            catch (Exception ex){
                Debug.Write("Change password: " + ex.Message);
                return ex.Message;
            }

            if (NeedChangePassword(SessionHelper.LoginStatus.UserID))
            {
                try
                {
                    user.ChangePassword = false;
                    pref.db.Entry(user).State = System.Data.Entity.EntityState.Modified;
                    pref.db.SaveChanges();
                }
                catch (Exception ex)
                {
                    Debug.Write("Change password: Error changing if force change password.");
                }
            }
            
            return "success";
        }

        public PartialViewResult Partial(PreferenceModel pref)
        {
           
 
            return PartialView("_Preferences2", pref);
        }
        

        [HttpPost]
        public string Save(PreferenceModel pref)
        {
            Debug.WriteLine("PreferencesController.Save from form - first last:" + pref.user.FirstName + " " + pref.user.LastName);
            Debug.WriteLine("PreferencesController.Save address: " + pref.address.Address1);
            if (!SessionHelper.IsUserLoggedIn)
            {
                return "User not logged in";
            }

            uas_User user = pref.db.uas_User.Where(u => u.UserID == SessionHelper.LoginStatus.UserID).FirstOrDefault();
            uas_UserPhone userPhone = pref.db.uas_UserPhone.Where(u => u.UserID == SessionHelper.LoginStatus.UserID).FirstOrDefault();
            uas_UserAddress userAddress = pref.db.uas_UserAddress.Where(u => u.UserID == SessionHelper.LoginStatus.UserID).FirstOrDefault();
            uas_UserEmail userEmail = pref.db.uas_UserEmail.Where(u => u.UserID == SessionHelper.LoginStatus.UserID).FirstOrDefault();
            
            user.FirstName = pref.user.FirstName;
            user.LastName = pref.user.LastName;
            user.MiddleName = pref.user.MiddleName;
            user.Title = pref.user.Title;
            user.Area = pref.user.Area;
            userPhone.PhoneNumber = pref.phone.PhoneNumber;
            userPhone.Extension = pref.phone.Extension;
            userAddress.Address1 = pref.address.Address1;
            userAddress.City = pref.address.City;
            userAddress.StateProvince = pref.address.StateProvince;
            userAddress.PostalCode = pref.address.PostalCode;
            userEmail.EmailAddress = pref.email.EmailAddress;

            user.ModifiedBy = SessionHelper.LoginStatus.UserID;
            user.ModifiedDate = DateTime.Now;
            userPhone.ModifiedBy = SessionHelper.LoginStatus.UserID;
            userPhone.ModifiedDate = DateTime.Now;
            userAddress.ModifiedBy = SessionHelper.LoginStatus.UserID;
            userAddress.ModifiedDate = DateTime.Now;
            userEmail.ModifiedBy = SessionHelper.LoginStatus.UserID;
            userEmail.ModifiedDate = DateTime.Now;
   
            pref.db.Entry(user).State = System.Data.Entity.EntityState.Modified;
            pref.db.Entry(userAddress).State = System.Data.Entity.EntityState.Modified;
            pref.db.Entry(userPhone).State = System.Data.Entity.EntityState.Modified;
            pref.db.Entry(userEmail).State = System.Data.Entity.EntityState.Modified;

            pref.user = user;
            pref.address = userAddress;
            pref.phone = userPhone;
            pref.email = userEmail;

            try
            {
                pref.db.SaveChanges();
            }
            catch (DbEntityValidationException dbEx)
            {
                Debug.WriteLine("Save DbEntityValidation Exception: ");
                string errors = "";
                foreach (DbEntityValidationResult devr in dbEx.EntityValidationErrors)
                {
                    foreach (DbValidationError dve in devr.ValidationErrors)
                    {
                        Debug.WriteLine("    DbEntityValidationResult: " + dve.ErrorMessage);
                        errors += dve.ErrorMessage + "<br />";
                    }
                }
                return errors;
            }
            catch (System.Data.DataException de)
            {
                Debug.WriteLine("Save DataException: " + de.Message);
                return de.Message;
            }
            catch (Exception xcptn)
            {
                Debug.WriteLine("Save Exception: " + xcptn.Message);
                return xcptn.Message;
            }

            return "success";

        }

        private bool NeedChangePassword(int userId)
        {
            using (var context = DataContext.getUasDbContext())
            {
                bool change = context.uas_User.Where(u => u.UserID == userId).SingleOrDefault().ChangePassword;
                return change;
            }
        }
        
    }
}