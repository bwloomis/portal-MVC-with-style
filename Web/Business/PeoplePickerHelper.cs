using Assmnts.UasServiceRef;
using Data.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace Assmnts.Business
{
    public class PeoplePickerHelper
    {

        IFormsRepository formsRepo;
        IAuthentication authClient;

        public PeoplePickerHelper(IAuthentication auth, IFormsRepository formsRepository)
        {
            authClient = auth;
            formsRepo = formsRepository;
        }

        public void SaveresultsForEnWrkerPicker(int sectionId, int formResultId)
        {


            if (sectionId == 708)
            {
                //Consent & Submit
                UpdateInterviewer(formResultId);

            }

            if (sectionId == 732)
            {
                //Insurance Assistance
                UpdateAssigned(formResultId);

            }
        }

        private void UpdateAssigned(int formResultId)
        {
            string enWorkerName = string.Empty;
            string enWorkerSiteValue = string.Empty;
            string enWorkerSite = string.Empty;

            def_ResponseVariables enWorkerVariable = null;
            def_ResponseVariables enWorkerSitevariable = null;

            def_FormResults formResult = formsRepo.GetFormResultById(formResultId);
            int enWorkerId = 0;

            try
            {
                enWorkerVariable = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, "C1_InsuranceAssistEnrollPerson");

                if (enWorkerVariable != null)
                {
                    enWorkerName = enWorkerVariable.rspValue;

                }

                enWorkerSitevariable = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, "C1_InsuranceAssistEnrollSite");

                if (enWorkerSitevariable != null)
                {
                    enWorkerSite = enWorkerSitevariable.rspValue;
                }

                if (!String.IsNullOrEmpty(enWorkerSite) && !String.IsNullOrEmpty(enWorkerName))
                {

                    enWorkerId = CheckAvailabilityAndGetPersonID(enWorkerSite, enWorkerName);

                    if (enWorkerId > 0)
                    {
                        if (enWorkerId != formResult.assigned)
                        {
                            formResult.assigned = enWorkerId;
                            formResult.dateUpdated = DateTime.Now;
                            formsRepo.Save();
                        }

                    }
                }

                if(String.IsNullOrEmpty(enWorkerName))
                {
                    formResult.assigned = null;
                    formResult.dateUpdated = DateTime.Now;
                    formsRepo.Save();

                }


            }
            catch (Exception ex)
            {


            }


        }

        private void UpdateInterviewer(int formResultId)
        {

            string enWorkerName = string.Empty;
            string enWorkerSite = string.Empty;

            def_ResponseVariables enWorkerVariable = null;
            def_ResponseVariables enWorkerSitevariable = null;
            def_FormResults formResult = formsRepo.GetFormResultById(formResultId);
            int enWorkerId = 0;


            enWorkerVariable = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, "C1_FormSubmitEnrollmentWorkerName");

            if (enWorkerVariable != null)
            {
                enWorkerName = enWorkerVariable.rspValue;

            }

            enWorkerSitevariable = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, "C1_FormSubmitEnrollmentSiteName");

            if (enWorkerSitevariable != null)
            {
                enWorkerSite = enWorkerSitevariable.rspValue;

            }

            if (!String.IsNullOrEmpty(enWorkerSite) && !String.IsNullOrEmpty(enWorkerName))
            {

                enWorkerId = CheckAvailabilityAndGetPersonID(enWorkerSite, enWorkerName);

                if (enWorkerId > 0)
                {
                    if (enWorkerId != formResult.interviewer)
                    {
                        formResult.interviewer = enWorkerId;
                        formResult.dateUpdated = DateTime.Now;
                        formsRepo.Save();

                    }

                }
            }

            if (String.IsNullOrEmpty(enWorkerName))
            {
                formResult.interviewer = null;
                formResult.dateUpdated = DateTime.Now;
                formsRepo.Save();

            }



        }

        private int CheckAvailabilityAndGetPersonID(string enWorkerSite, string enWorkerName)
        {
            int groupID;
            bool result = Int32.TryParse(enWorkerSite, out groupID);
            //List<UserDisplayShort> listWorkers = authClient.List_ENWorkers(8, GetGroupNameFromFormattedText(enWorkerSite));
            if (result)
            {
                List<UserDisplayShort> listWorkers = authClient.List_ENWorkers(8, groupID);

                foreach (var worker in listWorkers)
                {
                    if (enWorkerName == FormatName(worker.LastName, worker.FirstName))
                    {
                        return worker.UserID;
                    }
                }
            }
            return 0;
        }

        private string FormatName(string lastName, string firstName)
        {
            return lastName + ", " + firstName;
        }

        private string GetGroupNameFromFormattedText(string frText)
        {
            string[] splitStrings = frText.Split('(');

            int count = splitStrings.Length;

            string groupName = splitStrings[count - 1].Split(')')[0];

            return groupName;
        }


        public string RetrieveEnWorkersByGroup(string site)
        {
            string htmlOptions = string.Empty;

            int groupID;
            bool result = Int32.TryParse(site, out groupID);
            if (!String.IsNullOrEmpty(site) && result)
            {
                //string groupName = GetGroupNameFromFormattedText(site);
                List<UserDisplayShort> listWorkers = authClient.List_ENWorkers(8, groupID);


                StringBuilder optionsHtml = new StringBuilder();

                foreach (var worker in listWorkers)
                {
                    optionsHtml.Append("<option value=" + FormatName(worker.LastName, worker.FirstName) + ">" + FormatName(worker.LastName, worker.FirstName) + "</option>");

                }

                htmlOptions = optionsHtml.ToString();

            }


            return htmlOptions;

        }
    }
}