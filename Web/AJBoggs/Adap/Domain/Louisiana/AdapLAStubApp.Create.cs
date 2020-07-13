
using Assmnts;
using Assmnts.Business;
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
     * This class is used to create new instances (def_FormResults) of Lousisiana's ADAP stub application
     * 
     */
    public partial class AdapLAStubApp
    {
        public static readonly string stubFormIdentifier = "LA-ADAP-Stub";

        private readonly IFormsRepository formsRepo;
        private readonly AuthenticationClient auth;
        private readonly def_Forms stubForm;

        public AdapLAStubApp(IFormsRepository fr)
        {
            formsRepo = fr;
            auth = new AuthenticationClient();
            stubForm = formsRepo.GetFormByIdentifier(stubFormIdentifier);

            if (stubForm == null)
                throw new Exception("Could not find LA Stub-application form with identifier \"" + stubFormIdentifier + "\"");
        }

        /// <summary>
        /// Creates a new LA-ADAP Stub application for the given ramsellId
        /// 
        /// This may involve inserting a new UAS_User.
        /// </summary>
        /// <returns>the formResultId of the newly-created application</returns>
        public int CreateAndPrepopulateNewStubApp( string ramsellId )
        {
            def_FormResults previousApp = GetMostRecentAppWithRamsellId(ramsellId);
            def_FormResults previousStubApp = GetMostRecentAppWithRamsellId(ramsellId, stubForm.formId);
            def_FormResults newStubApp = CreateNewBlankStubApp(ramsellId, previousApp );
            PrepopulateStubApp(newStubApp, previousStubApp, ramsellId);
            formsRepo.AddFormResult(newStubApp);
            ReviewStatus.ChangeStatus(formsRepo, newStubApp, ReviewStatus.TO_BE_REVIEWED, "Created Stub Application");
            return newStubApp.formResultId;
        }

        public def_Forms GetStubForm()
        {
            return stubForm;
        }

        private def_FormResults CreateNewBlankStubApp( string ramsellId, def_FormResults previousApp )
        {
            int? subjectUserId = previousApp == null ? null : previousApp.subject;
            return new def_FormResults()
            {
                formId = stubForm.formId,
                formStatus = 0,
                sessionStatus = 0,
                dateUpdated = DateTime.Now,
                deleted = false,
                locked = false,
                archived = false,
                EnterpriseID = SessionHelper.LoginStatus.EnterpriseID,
                GroupID = 0,
                subject = subjectUserId,
                interviewer = SessionHelper.LoginStatus.UserID,
                assigned = SessionHelper.LoginStatus.UserID,
                training = false,
                reviewStatus = 0,
                statusChangeDate = DateTime.Now
            };
        }

        /// <summary>
        /// Inserts def_ItemResults and def_ResponseVariables for:
        ///     the given ramsellId
        ///     the subject's uas data, if subject is non-null
        ///     the "HIV diagnosis date" response from the subject's previous stub application, if it exists
        /// </summary>
        /// <param name="stubAppToPrepopulate">formResult record to prepopulate</param>
        private void PrepopulateStubApp(def_FormResults stubAppToPrepopulate, def_FormResults previousStubApp, string ramsellId )
        {
            InsertNewResponse(stubAppToPrepopulate, "ADAP_D9_Ramsell", ramsellId );

            if (previousStubApp != null)
            {
                string previousHivDate = GetStringResponse(previousStubApp.formResultId, "LA_ADAP_DiagnosisDt");
                InsertNewResponse(stubAppToPrepopulate, "LA_ADAP_DiagnosisDt", previousHivDate);
            }

            if (stubAppToPrepopulate.subject.HasValue)
            {
                User usr = new AuthenticationClient().GetUser(stubAppToPrepopulate.subject.Value);
                InsertNewResponse(stubAppToPrepopulate, "ADAP_D1_FirstName", usr.FirstName);
                InsertNewResponse(stubAppToPrepopulate, "ADAP_D1_LastName", usr.LastName);
                InsertNewResponse(stubAppToPrepopulate, "ADAP_D2_DOB", usr.DOB.ToString() );
            }
        }

        private def_FormResults GetMostRecentAppWithRamsellId(string ramsellId, int? formId = null)
        {
            IQueryable<def_FormResults> candidates = formsRepo
                .GetFormResultsByIvIdentifierAndValue("ADAP_D9_Ramsell", ramsellId).AsQueryable(); //get all formresults with matching subject

            if (formId.HasValue)
                candidates = candidates.Where(fr => fr.formId == formId.Value); //filter for a specific form if necessary

            def_FormResults result = candidates      
                .OrderByDescending(fr => fr.dateUpdated) //get most recently updated
                .FirstOrDefault();
            return result;
        }

        private void InsertNewResponse(def_FormResults fr, string ivIdentifier, string response)
        {
            def_ItemVariables iv = formsRepo.GetItemVariableByIdentifier(ivIdentifier);

            def_ResponseVariables rv = new def_ResponseVariables()
            {
                itemVariableId = iv.itemVariableId,
                rspValue = response
            };
               
            def_ItemResults ir = new def_ItemResults()
            {
                itemId = iv.itemId,
                sessionStatus = 0,
                dateUpdated = DateTime.Now,
                def_ResponseVariables = { rv }
            };
            
            fr.def_ItemResults.Add(ir);
        }
    }
}
