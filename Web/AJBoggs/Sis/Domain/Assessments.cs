using Assmnts;
using Assmnts.Infrastructure;
using Assmnts.Reports;
using Data.Abstract;
using Data.Concrete;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net.Mail;
using UAS.Business;



namespace AJBoggs.Sis.Domain
{
    public enum FormResults_formStatus
    {
        // Most of these were taken from the SIS Status table.

        // Actual statuses were coming from vSISAssesmentForSearch view. Updated to use these. 
        //--LK 2/24/2015
        NEW = 0,
        IN_PROGRESS = 1,
        COMPLETED = 2,
        ABANDONED = 3,
        UPLOADED = 4
    }

    /*
     * This class is used to process SIS Assessments.
     * Assessments are stored in the DEF FormsRepository
     * 
     * It should be used by Controllers and WebServices for getting Assessments from the Forms Repository.
     * 
     */
    public partial class Assessments
    {
 
        private IFormsRepository formsRepo;

        /// <summary>
        /// Only use this constructor when there is no need for database access.
        /// </summary>
        public Assessments()
        {
            
        }

        public Assessments(IFormsRepository fr)
        {
            formsRepo = fr;
        }

        public void AssessmentComplete(def_FormResults frmRslt)
        {
            formsRepo.SetFormResultStatus(frmRslt, (byte)FormResults_formStatus.COMPLETED);
            if (!SessionHelper.IsVentureMode)
            {
                UpdateAssessmentScores(frmRslt);
            }
        }

        public void UpdateAssessmentScores(def_FormResults frmRslt)
        {
            def_Forms frm = formsRepo.GetFormById(frmRslt.formId);
            if (frm.identifier.Equals("SIS-A"))
            {
                SisAScoring.UpdateSisAScores(formsRepo, frmRslt.formResultId);
            }
            else if (frm.identifier.Equals("SIS-C"))
            {
                int ageInYears = SharedScoring.ComputeClientAgeInYears(formsRepo, frmRslt.formResultId);
                SisCScoring.UpdateSisCScores(formsRepo, frmRslt.formResultId, ageInYears);
            }
        }

        public List<def_FormResults> GetAssessmentsByTrackingNumber(string trackingNumber)
        {
            List<def_FormResults> assessments = formsRepo.GetFormResultsByIvIdentifierAndValue("sis_track_num", trackingNumber);

            return assessments;
        }

        public List<def_FormResults> GetAssessmentsByTrackingNumberFilterByAccess(UAS.DataDTO.LoginStatus loginStatus, string trackingNum)
        {
            List<def_FormResults> assessments = formsRepo.GetFormResultsByIvIdentifierAndValueFilterByAccess(loginStatus, "sis_track_num", trackingNum);

            return assessments;
        }
        
        /// <summary>
        /// 
        /// </summary>
        public void DeleteOldUploadedAssessments()
        {
            int defaultDaysUntilDelete = 60;
            int daysUntilDelete = 0;
            bool entAppConfigPresent = false;

            try
            {
                using (var uasContext = DataContext.getUasDbContext())
                {
                    entAppConfigPresent = Int32.TryParse(uasContext.uas_EntAppConfig.Where(e => e.ApplicationID == UAS.Business.Constants.APPLICATIONID && e.EnterpriseID == SessionHelper.LoginStatus.EnterpriseID && e.EnumCode == ConfigEnumCodes.VENTURE_DELETE && e.StatusFlag == "A").Select(e => e.ConfigValue).FirstOrDefault(), out daysUntilDelete);
                }

                if (!entAppConfigPresent)
                {
                    daysUntilDelete = defaultDaysUntilDelete;
                }

                DateTime deleteBeforeDate = DateTime.Now.AddDays(-daysUntilDelete);

                formsEntities defContext = DataContext.GetDbContext();
                List<def_FormResults> formResults = defContext.def_FormResults.Where(x => (x.statusChangeDate != null) && (x.statusChangeDate < deleteBeforeDate) && (x.formStatus == (int)FormResults_formStatus.UPLOADED) && (x.EnterpriseID == SessionHelper.LoginStatus.EnterpriseID)).ToList();
                foreach (def_FormResults formResult in formResults)
                {
                    // The database is configured with cascading SQL DELETE
                    defContext.def_FormResults.Remove(formResult);
                }

                defContext.SaveChanges();
            }
            catch (Exception excptn)
            {
                Debug.WriteLine("AJBoggs.Sis.Domain.Assessments.DeleteOldUploadedAssessments - exception:" + excptn.Message);
                string innerExcptnMsg = string.Empty;
                if ( (excptn.InnerException != null) && (excptn.InnerException.Message != null) )
                {
                    innerExcptnMsg = " * Inner Exception: " + excptn.InnerException.Message;
                    Debug.WriteLine("  InnerException: " + innerExcptnMsg);
                }
            }

        }

        public bool SendEmail(string toAddress, string fromAddress, string mailBody, string mailSubject, bool isHtml)
        {
            bool status = true;
            string smtpUser = ConfigurationManager.AppSettings["SmtpUser"].ToString();
            string smtpPass = ConfigurationManager.AppSettings["SmtpPass"].ToString();
            string smtpHost = ConfigurationManager.AppSettings["AcctHost"].ToString();
            int smtpPort = Convert.ToInt32(ConfigurationManager.AppSettings["SmtpPort"]);


            try
            {
                SmtpClient mySmtpClient = new SmtpClient(smtpHost);

                // set smtp-client with basicAuthentication
                mySmtpClient.UseDefaultCredentials = false;
                System.Net.NetworkCredential basicAuthenticationInfo = new System.Net.NetworkCredential(smtpUser, smtpPass);
                mySmtpClient.Credentials = basicAuthenticationInfo;
                mySmtpClient.Port = smtpPort;

                string[] addresses = toAddress.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                // add from,to mailaddresses
                MailAddress @from = new MailAddress(fromAddress);
                MailAddress recepient = new MailAddress(addresses[0]);
                MailMessage myMail = new System.Net.Mail.MailMessage(@from, recepient);

                for (int i = 1; i < addresses.Count(); i++)
                {
                    myMail.To.Add(addresses[i]);
                }

                // set subject and encoding
                myMail.Subject = mailSubject;
                myMail.SubjectEncoding = System.Text.Encoding.UTF8;

                // set body-message and encoding
                myMail.Body = mailBody;
                myMail.BodyEncoding = System.Text.Encoding.UTF8;
                // text or html
                myMail.IsBodyHtml = isHtml;

                mySmtpClient.Send(myMail);

            }
            catch (SmtpException ex)
            {
                //WriteLog(String.Format("SMTP Exception Occured: {0}", ex.Message), TAP.DAL.Enum.LogLevel_Enum.ERROR);
                status = false;
            }
            catch (Exception ex)
            {
                //WriteLog(String.Format("Exception Occured: {0}", ex.Message), TAP.DAL.Enum.LogLevel_Enum.ERROR);
                status = false;
            }

            return status;
        }

        
        
    }
}
