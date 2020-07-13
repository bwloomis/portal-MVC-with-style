
using Assmnts;
using Assmnts.Infrastructure;
using Assmnts.Models;
using Assmnts.UasServiceRef;
using Data.Abstract;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Web.Mvc;



namespace AJBoggs.Adap.Domain
{
    /*
     * This class is used to create/update a uas_User record based on an instance of Lousisiana's ADAP stub application
     * 
     */
    public partial class AdapLAStubApp
    {
        public void UpdateUASFromStubApp(int formResultId)
        {
            //retrieve relevent responses from the def_FormResult
            string firstName = GetStringResponse(formResultId, "ADAP_D1_FirstName");
            string lastName  = GetStringResponse(formResultId, "ADAP_D1_LastName");
            string sDob      = GetStringResponse(formResultId, "ADAP_D2_DOB");

            //parse DOB
            DateTime? dob = null; 
            DateTime fDob;
            if( DateTime.TryParse( sDob, out fDob ) )
                dob = fDob;

            //add or update a user record based on responses
            UserDisplay userToUpdate;
            def_FormResults formResult = formsRepo.GetFormResultById(formResultId);
            if (formResult.subject.HasValue)
            {
                //this formResult is already associated with a uas_User (subject)
                //still, do not update uas if there exists a more recent formResult with the same subject
                DateTime dateCreated = GetDateCreated( formResultId );
                DateTime mostRecentDateCreated = GetMostRecentDateCreatedForSubject( formResult.subject.Value );
                if ( mostRecentDateCreated > dateCreated)
                    userToUpdate = null;
                else
                    userToUpdate = auth.GetUserDisplay( formResult.subject.Value );
            
            }else{
                //this formResult is not yet associated with a uas_User, so add one
                userToUpdate = CreateNewUser();
                formResult.subject = userToUpdate.UserID;
                formsRepo.Save();
            }
            if( userToUpdate != null )
                SetUserProps( userToUpdate, firstName, lastName, dob );
        }

        private DateTime GetMostRecentDateCreatedForSubject(int subject)
        {
            IQueryable<int> frIdsForSubject = formsRepo
                .GetFormResultsBySubject(subject)
                .Where( fr => fr.formId == stubForm.formId)
                .Select(fr => fr.formResultId);

            DateTime result = DateTime.MinValue;
            foreach (int frId in frIdsForSubject)
            {
                DateTime dateCreated = GetDateCreated(frId);
                if (dateCreated > result)
                    result = dateCreated;
            }
            return result;
        }

        private DateTime GetDateCreated(int frId)
        {
            IQueryable<def_StatusLog> logs = formsRepo.GetStatusLogsForFormResultId(frId);
            if (logs.Count() == 0)
                return DateTime.MinValue;

            DateTime oldestStatusLogDate = logs.Select(sl => sl.statusLogDate).Min();
            return oldestStatusLogDate;
        }

        private string GetStringResponse(int formResultId, string ivIdentifier)
        {
            def_ResponseVariables rv = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, ivIdentifier);
            if (rv == null)
                return String.Empty;
            if (String.IsNullOrWhiteSpace(rv.rspValue))
                return String.Empty;
            return rv.rspValue;
        }

        private UserDisplay CreateNewUser()
        {
            //insert a new user with a temporary loginId
            string tempLoginId = DateTime.Now.Ticks.ToString();
            UserDisplay newUser = new UserDisplay();
            newUser.FirstName = String.Empty;
            newUser.LastName = String.Empty;
            newUser.DOB = null;
            newUser.LoginID = tempLoginId;
            newUser.CreatedDate = DateTime.Now;
            newUser.ModifiedDate = null;
            newUser.StatusFlag = 'A';
            newUser.CreatedBy = SessionHelper.LoginStatus.UserID;
            newUser.langId = 1;
            newUser.EnterpriseID = SessionHelper.LoginStatus.EnterpriseID;
            auth.AddUser(newUser);

            //retrieve the new user using the temprorary login ID, then replace the loginId 
            //with a string that includes their newly-assigned UserId number (Bug 13012) 
            UserDisplay addedUser = auth.GetUserDisplayByLoginId( tempLoginId );
            string finalLoginId = "ADAP-" + addedUser.UserID;
            addedUser.LoginID = finalLoginId;
            auth.UpdateUserDisplay(addedUser);

            return auth.GetUserDisplayByLoginId(finalLoginId);
        }

        private bool SetUserProps(UserDisplay usr, string firstName, string lastName, DateTime? dob)
        {
            usr.FirstName = firstName;
            usr.LastName = lastName;
            usr.DOB = dob;
            return auth.UpdateUserDisplay( usr );
        }
    }
}
