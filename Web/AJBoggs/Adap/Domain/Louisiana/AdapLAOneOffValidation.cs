using Assmnts;
using Assmnts.Business;
using Assmnts.Models;
using Data.Abstract;
using System;


namespace AJBoggs.Adap.Domain
{
    
    public class AdapLAOneOffValidation
    {

        /// <summary>
        /// Used by COADAPController.Validate()
        /// 
        /// Runs the hard-coded validation rules, which can't be encoded into meta-data, for the CO-ADAP application.
        /// 
        /// adds to the model's validation messages for any validation errors or warnings.
        /// </summary>
        /// <param name="sv"></param>
        /// <param name="model"></param>
        /// <returns>true if any validation ERRORS where found, false if nothin (or only wanrings) found</returns>
        public static bool RunOneOffValidation( IFormsRepository formsRepo, SharedValidation sv, AdapValidationErrorsModel model ) {

            bool invalid = false;

            #region other validation (errors)

            #region Contact Info

            //3. Have you had a name change in the last 12 months?
            if (sv.HasPositiveResponse("LA_ADAP_NameChgYN") && !sv.HasAnyResponse("LA_ADAP_NameChgFirst"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_Contact", 3, "LA_ADAP_Contact_NameChg",
                    "You must enter a first name or indicate that you did not change your name in the last 12 months."));
            }

            if (sv.HasPositiveResponse("LA_ADAP_NameChgYN") && !sv.HasAnyResponse("LA_ADAP_NameChgLast"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_Contact", 3, "LA_ADAP_Contact_NameChg",
                    "You must enter a last name or indicate that you did not change your name in the last 12 months."));
            }

            //5. What is your Social Security Number?
            if (!sv.HasPositiveResponse("LA_ADAP_NoSSN") && !sv.HasPattern("ADAP_D10_SSN", @"\d{3}\-\d{2}\-\d{4}"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg( formsRepo, "LA_ADAP_Contact", 5, "LA_ADAP_Contact_SSN",
                    "You must enter a SSN or indicate that you do not have a SSN"));
            }

            //6. What is your preferred language?
            if (sv.HasExactResponse("ADAP_D7_LangDrop", "17") && !sv.HasAnyResponse("ADAP_D7_LangOther"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg( formsRepo, "LA_ADAP_Contact", 6, "LA_ADAP_Contact_Language",
                    "Since you selected language \"other\", you must indicate your language in the text box"));
            }

            //9. Do you want mail, including your LA HAP card, sent to your residential address?
            //10. What address can you receive mail at? This should be somewhere you can regularly check for mail.
            if (sv.HasExactResponse("LA_ADAP_ResidMailYN", "0") && !sv.AllComplete("ADAP_C2_Address", "ADAP_C2_City", "ADAP_C2_State", "ADAP_C2_Zip"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_Contact", 10, "LA_ADAP_Contact_MailAddr",
                    "Since you indicated that you do not want mail sent to your residential address, you must provide a mailing address, city, state, and zip code"));
            }

            //14. Do you have a friend or family member (alternate contact) that LA HAP may speak to about your application on your behalf?
            if (sv.HasExactResponse("ADAP_C4_MayCallYN", "1") && !sv.AllComplete("ADAP_C4_Name", "LA_ADAP_AltContact_Reln", "ADAP_C4_Phone"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_Contact", 14, "LA_ADAP_Contact_AltContact",
                    "Since you indicated that you do have an alternate contact, you must enter their name, phone number, and relationship to you."));
            }
            //11.  What is your primary phone number? 
            if (!sv.HasPositiveResponse("LA_ADAP_NoPhone") && !sv.HasPattern("ADAP_C3_Phone1_Num", @"[(]{0,1}[0-9]{3}[)]{0,1}[-\s\.]{0,1}[0-9]{3}[-\s\.]{0,1}[0-9]{4}"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_Contact", 11, "LA_ADAP_Contact_Phone1",
                    "You must enter a Primary phone number or indicate that you do not have a primary phone number"));
            }
            //12.  What is your secondary phone number?  
            //if (!sv.HasPositiveResponse("LA_ADAP_NoSecondPhone") && !sv.HasPattern("ADAP_C3_Phone2_Num", @"[(]{0,1}[0-9]{3}[)]{0,1}[-\s\.]{0,1}[0-9]{3}[-\s\.]{0,1}[0-9]{4}"))
            //{
            //    invalid = true;
            //    model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_Contact", 12, "",
            //        "You must enter a secondary phone number or indicate that you do not have a secondary phone number"));
            //}

            #endregion

            #region Demographics info

            //2. What is your race?
            if (!sv.AnyPositiveResponses("ADAP_D3_White", "ADAP_D3_Native", "ADAP_D3_Asian", "ADAP_D3_Black", "ADAP_D3_Indian", "ADAP_D3_Other"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "Global_ADAP_Demographic", 2, "Global_ADAP_Demographic_Race",
                    "You must check at least one box to indicate your race"));
            }
            if (sv.HasPositiveResponse("ADAP_D3_Other") && !sv.HasAnyResponse("ADAP_D3_OtherText"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "Global_ADAP_Demographic", 2, "Global_ADAP_Demographic_Race",
                    "Since you checked the \"other\" checkbox, you must indicate your race in the text box"));
            }

            //4. If you answered that you are Asian, how do you identify? (Check all that apply)
            if (sv.HasPositiveResponse("ADAP_D3_Asian") && !sv.AnyPositiveResponses("ADAP_D5_Indian", "ADAP_D5_Filipino",
                "ADAP_D5_Korean", "ADAP_D5_Other", "ADAP_D5_Chinese", "ADAP_D5_Japanese", "ADAP_D5_Vietnamese", "ADAP_D5_NA"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "Global_ADAP_Demographic", 4, "Global_ADAP_Demographic_Asian",
                    "Since you indicated that you are Asian, you must check at least one box in this section"));
            }

            //5. If you answered that you are Native Hawaiian/Pacific Islander, how do you identify? (check all that apply)
            if (sv.HasPositiveResponse("ADAP_D3_Native") && !sv.AnyPositiveResponses("ADAP_D6_Native", "ADAP_D6_Guam", "ADAP_D6_Samoan", "ADAP_D6_Other", "ADAP_D6_NA"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "Global_ADAP_Demographic", 5, "Global_ADAP_Demographic_Native",
                    "Since you indicated that you are Native Hawaiian/Pacific Islander, you must check at least one box in this section"));
            }

            //6. If you answered that you are of Hispanic or Latina/o ethnicity, how do you identify?(Check all that apply)
            if (sv.HasExactResponse("ADAP_D4_EthnicDrop", "1") && !sv.AnyPositiveResponses("ADAP_D4_Mexican", "ADAP_D4_Puerto", "ADAP_D4_NA", "ADAP_D4_Cuban", "ADAP_D4_Other", "ADAP_D4_Unknown"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "Global_ADAP_Demographic", 6, "Global_ADAP_Demographic_Hisp",
                    "Since you indicated that you are of Hispanic or Latina/o ethnicity, you must check at least one box in this section"));
            }

            //7. What is your current relationship status?
            if (!sv.AnyPositiveResponses("ADAP_RelnStatus_Single", "ADAP_RelnStatus_MarriedHouse",
                "ADAP_RelnStatus_MarriedSep", "ADAP_RelnStatus_Unmarried", "ADAP_RelnStatus_Partnered", "ADAP_RelnStatus_Widow"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "Global_ADAP_Demographic", 7, "Global_ADAP_Demographic_RelnStatus",
                    "You must check at least one box to indicate your relationship status"));
            }
            #endregion

            #endregion

            #region Employment info
            //3. How often are you paid?
            if (sv.HasPositiveResponse("LA_OtherPay") && !sv.HasAnyResponse("LA_ADAP_PayFreqOther"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_Employment", 3, "LA_ADAP_Employment_PayFreq",
                    "Since you Checked how often paid  as \"other\", you must indicate your other payment in the text box"));
            }
            
            #endregion

            #region Assister info
            //2. received help on appliaction from one of these people
            if (sv.HasPositiveResponse("LA_ADAP_Assister_Other") && !sv.HasAnyResponse("LA_ADAP_Assister_OtherAssistType"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_Assister", 2, "LA_ADAP_Assister_Help",
                    "Since you Checked received help on appliaction from as \"other\", you must indicate your other person name in the text box"));
            }

            #endregion

            #region Case Management info
            //1. Provider 1
            if (sv.HasExactResponse("LA_ADAP_Case_ProvYN", "1") && !sv.AllComplete("LA_ADAP_Case_Prov1Agency", "LA_ADAP_Case_Prov1Phone"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_Case", 1, "LA_ADAP_Case_Prov1",
                    "Since you indicated that you have provider to access your LA HAP records , you must enter Provider 1 entity/agency name and phone number"));
            }

            #endregion

            #region Diagnosis and Medication Information
            //1. I am taking medication now but I will run out of medication in the next 96 hours (4 days) 
            if (sv.HasExactResponse("LA_ADAP_Diagnosis_MedRef", "1") && !sv.HasAnyResponse("LA_ADAP_Diagnosis_MedRefDate"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_Diagnosis", 14, "LA_ADAP_Diagnosis_MedRef",
                    "Since you indicated that your taking medication now but I will run out of medication in the next 96 hours (4 days) , you must enter Date you last filled your medication"));
            }

            //2. I have just been diagnosed with HIV OR I have just gotten back into care for my HIV

            if (sv.HasExactResponse("LA_ADAP_Diagnosis_HIVDiag", "1") && !sv.HasAnyResponse("LA_ADAP_Diagnosis_HIVDiagDate"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_Diagnosis", 2, "LA_ADAP_Diagnosis_HIVDiag",
                    "Since you indicated that you have been diagnosed with HIV OR I have just gotten back into care for my HIV , you must enter Date you were diagnosed with HIV"));
            }



            #endregion

            #region Medicare Insurance Policy information
            //2. What is your current Low-Income Subsidy (LIS) status?
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsMed") && !sv.HasAnyResponse("LA_ADAP_LIS_Status"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_MedPolicy", 2, "LA_ADAP_MedPolicy_LIS",
                    "Since you Checked i have Medicare Part A,B,C,and/or D ,and/or Medicare Supplement in Assistance Information Page, you must select What is your current Low-Income Subsidy (LIS) status?"));
            }
            //3. Medicare Part B Information and Assistance
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsMed") && sv.HasPositiveResponse("LA_ADAP_MedPolicy_TypeB") && !sv.HasAnyResponse("LA_ADAP_MedB_Asst"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_MedPolicy", 3, "LA_ADAP_MedPolicy_MedB",
                    "Since you Checked i have Medicare Part A,B,C,and/or D ,and/or Medicare Supplement in Assistance Information Page, you must select What type of assistance are you requesting from LA HAP for Medicare Part B?"));
            }
            if ((sv.HasPositiveResponse("LA_ADAP_HaveInsMed") && (sv.HasPositiveResponse("LA_ADAP_MedPolicy_TypeAB") || sv.HasPositiveResponse("LA_ADAP_MedPolicy_TypeB"))) && !sv.HasAnyResponse("LA_ADAP_MedAB_Num"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_MedPolicy", 3, "LA_ADAP_MedPolicy_MedB",
                    "Since you Checked i have Medicare Part A,B,C,and/or D ,and/or Medicare Supplement in Assistance Information Page, you must enter What is your Medicare Part A and B number with letter?"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsMed") && sv.HasPositiveResponse("LA_ADAP_MedPolicy_TypeAB") && !sv.HasAnyResponse("ADAP_I3_PartADate"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_MedPolicy", 3, "LA_ADAP_MedPolicy_MedB",
                    "Since you Checked i have Medicare Part A,B,C,and/or D ,and/or Medicare Supplement in Assistance Information Page, you must enter What is your Medicare Part A Effective Date? "));
            }
            //4. Medicare Part C Information and Assistance
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsMed") && sv.HasPositiveResponse("LA_ADAP_MedPolicy_MedC") && !sv.HasAnyResponse("LA_ADAP_MedC_Asst"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_MedPolicy", 4, "LA_ADAP_MedPolicy_MedC",
                    "Since you Checked i have Medicare Part A,B,C,and/or D ,and/or Medicare Supplement in Assistance Information Page, you must select What type of assistance are you requesting from LA HAP for Medicare Part C?"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsMed") && sv.HasPositiveResponse("LA_ADAP_MedPolicy_MedC") && !sv.HasAnyResponse("LA_ADAP_MedC_Plan"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_MedPolicy", 4, "LA_ADAP_MedPolicy_MedC",
                    "Since you Checked i have Medicare Part A,B,C,and/or D ,and/or Medicare Supplement in Assistance Information Page, you must enter Medicare Part C Company and Plan Name"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsMed") && sv.HasPositiveResponse("LA_ADAP_MedPolicy_MedC") && !sv.HasAnyResponse("LA_ADAP_MedC_ID"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_MedPolicy", 4, "LA_ADAP_MedPolicy_MedC",
                    "Since you Checked i have Medicare Part A,B,C,and/or D ,and/or Medicare Supplement in Assistance Information Page, you must enter Medicare Part C Member ID/Policy Number"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsMed") && sv.HasPositiveResponse("LA_ADAP_MedPolicy_MedC") && !sv.HasAnyResponse("LA_ADAP_MedC_Group"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_MedPolicy", 4, "LA_ADAP_MedPolicy_MedC",
                    "Since you Checked i have Medicare Part A,B,C,and/or D ,and/or Medicare Supplement in Assistance Information Page, you must enter Medicare Part C Group Number"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsMed") && sv.HasPositiveResponse("LA_ADAP_MedPolicy_MedC") && !sv.HasAnyResponse("LA_ADAP_MedC_Date"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_MedPolicy", 4, "LA_ADAP_MedPolicy_MedC",
                    "Since you Checked i have Medicare Part A,B,C,and/or D ,and/or Medicare Supplement in Assistance Information Page, you must enter Medicare Part C Start Date"));
            }
            //5. Medicare Part D Information and Assistance
            //if (sv.HasPositiveResponse("LA_ADAP_HaveInsMed") && sv.HasPositiveResponse("LA_ADAP_MedPolicy_MedD") && !sv.HasAnyResponse("LA_ADAP_MedD_Asst"))
            //{
            //    invalid = true;
            //    model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_MedPolicy", 5, "LA_ADAP_MedPolicy_MedD",
            //        "Since you Checked i have Medicare Part A,B,C,and/or D ,and/or Medicare Supplement in Assistance Information Page, you must select What type of assistance are you requesting from LA HAP for Medicare Part D?"));
            //}
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsMed") && sv.HasPositiveResponse("LA_ADAP_MedPolicy_MedD") && !sv.HasAnyResponse("LA_ADAP_MedD_Plan"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_MedPolicy", 5, "LA_ADAP_MedPolicy_MedD",
                    "Since you Checked i have Medicare Part A,B,C,and/or D ,and/or Medicare Supplement in Assistance Information Page, you must enter Medicare Part D company and plan name"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsMed") && sv.HasPositiveResponse("LA_ADAP_MedPolicy_MedD") && !sv.HasAnyResponse("LA_ADAP_MedD_ID"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_MedPolicy", 5, "LA_ADAP_MedPolicy_MedD",
                    "Since you Checked i have Medicare Part A,B,C,and/or D ,and/or Medicare Supplement in Assistance Information Page, you must enter Medicare Part D member ID/policy number"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsMed") && sv.HasPositiveResponse("LA_ADAP_MedPolicy_MedD") && !sv.HasAnyResponse("LA_ADAP_MedD_Group"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_MedPolicy", 5, "LA_ADAP_MedPolicy_MedD",
                    "Since you Checked i have Medicare Part A,B,C,and/or D ,and/or Medicare Supplement in Assistance Information Page, you must enter Medicare Part D group number"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsMed") && sv.HasPositiveResponse("LA_ADAP_MedPolicy_MedD") && !sv.HasAnyResponse("LA_ADAP_MedD_Date"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_MedPolicy", 5, "LA_ADAP_MedPolicy_MedD",
                    "Since you Checked i have Medicare Part A,B,C,and/or D ,and/or Medicare Supplement in Assistance Information Page, you must enter Medicare Part D start date"));
            }
            //6. Medicare Supplemental Information and Assistance
            //if (sv.HasPositiveResponse("LA_ADAP_HaveInsMed") && sv.HasPositiveResponse("LA_ADAP_MedPolicy_MedD") && !sv.HasAnyResponse("LA_ADAP_MedD_Asst"))
            //{
            //    invalid = true;
            //    model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_MedPolicy", 5, "LA_ADAP_MedPolicy_MedD",
            //        "Since you Checked i have Medicare Part A,B,C,and/or D ,and/or Medicare Supplement in Assistance Information Page, you must select What type of assistance are you requesting from LA HAP for Medicare Part D?"));
            //}
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsMed") && sv.HasPositiveResponse("LA_ADAP_MedPolicy_MedSupp") && !sv.HasAnyResponse("LA_ADAP_MedSupp_Plan"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_MedPolicy", 6, "LA_ADAP_MedPolicy_MedSupp",
                    "Since you Checked i have Medicare Part A,B,C,and/or D ,and/or Medicare Supplement in Assistance Information Page, you must enter Medicare Supplemental company and plan name"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsMed") && sv.HasPositiveResponse("LA_ADAP_MedPolicy_MedSupp") && !sv.HasAnyResponse("LA_ADAP_MedSupp_ID"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_MedPolicy", 6, "LA_ADAP_MedPolicy_MedSupp",
                    "Since you Checked i have Medicare Part A,B,C,and/or D ,and/or Medicare Supplement in Assistance Information Page, you must enter Medicare Supplemental member ID/policy number"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsMed") && sv.HasPositiveResponse("LA_ADAP_MedPolicy_MedSupp") && !sv.HasAnyResponse("LA_ADAP_MedSupp_Group"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_MedPolicy", 6, "LA_ADAP_MedPolicy_MedSupp",
                    "Since you Checked i have Medicare Part A,B,C,and/or D ,and/or Medicare Supplement in Assistance Information Page, you must enter Medicare Supplemental group number"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsMed") && sv.HasPositiveResponse("LA_ADAP_MedPolicy_MedSupp") && !sv.HasAnyResponse("LA_ADAP_MedSupp_Date"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_MedPolicy", 6, "LA_ADAP_MedPolicy_MedSupp",
                    "Since you Checked i have Medicare Part A,B,C,and/or D ,and/or Medicare Supplement in Assistance Information Page, you must enter Medicare Supplemental start date"));
            }
            #endregion

            #region Medicare Insurance Premium Payment Information
            //1. Medicare Part B Premium Payments
            if ((sv.HasPositiveResponse("LA_ADAP_HaveInsMed") && (sv.HasPositiveResponse("LA_ADAP_MedPolicy_TypeA") || sv.HasPositiveResponse("LA_ADAP_MedPolicy_TypeAB"))) && !sv.HasAnyResponse("LA_ADAP_MedB_Admin"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_MedPrem", 1, "LA_ADAP_MedPrem_MedB",
                    "Enter Medicare Part B insurance company or third party administrator name (who should the premium check be made out to)"));
            }
            if ((sv.HasPositiveResponse("LA_ADAP_HaveInsMed") && (sv.HasPositiveResponse("LA_ADAP_MedPolicy_TypeA") || sv.HasPositiveResponse("LA_ADAP_MedPolicy_TypeAB"))) && !sv.HasAnyResponse("LA_ADAP_MedB_Addr"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_MedPrem", 1, "LA_ADAP_MedPrem_MedB",
                    "Enter Medicare Part B insurance company or third party administrator street address (who should the premium check be made out to)"));
            }
           
            if ((sv.HasPositiveResponse("LA_ADAP_HaveInsMed") && (sv.HasPositiveResponse("LA_ADAP_MedPolicy_TypeA") || sv.HasPositiveResponse("LA_ADAP_MedPolicy_TypeAB"))) && !sv.HasAnyResponse("LA_ADAP_MedB_City"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_MedPrem", 1, "LA_ADAP_MedPrem_MedB",
                    "Enter City"));
            }
            if ((sv.HasPositiveResponse("LA_ADAP_HaveInsMed") && (sv.HasPositiveResponse("LA_ADAP_MedPolicy_TypeA") || sv.HasPositiveResponse("LA_ADAP_MedPolicy_TypeAB"))) && !sv.HasAnyResponse("LA_ADAP_MedB_State"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_MedPrem", 1, "LA_ADAP_MedPrem_MedB",
                    "Enter State"));
            }
            if ((sv.HasPositiveResponse("LA_ADAP_HaveInsMed") && (sv.HasPositiveResponse("LA_ADAP_MedPolicy_TypeA") || sv.HasPositiveResponse("LA_ADAP_MedPolicy_TypeAB"))) && !sv.HasAnyResponse("LA_ADAP_MedB_Zip"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_MedPrem", 1, "LA_ADAP_MedPrem_MedB",
                    "Enter Zipcode"));
            }
            if ((sv.HasPositiveResponse("LA_ADAP_HaveInsMed") && (sv.HasPositiveResponse("LA_ADAP_MedPolicy_TypeA") || sv.HasPositiveResponse("LA_ADAP_MedPolicy_TypeAB"))) && !sv.HasAnyResponse("LA_ADAP_MedB_AppAmt"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_MedPrem", 1, "LA_ADAP_MedPrem_MedB",
                    "Enter What is the applicant's portion of the Part B premium amount?"));
            }
            if ((sv.HasPositiveResponse("LA_ADAP_HaveInsMed") && (sv.HasPositiveResponse("LA_ADAP_MedPolicy_TypeA") || sv.HasPositiveResponse("LA_ADAP_MedPolicy_TypeAB"))) && !sv.HasAnyResponse("LA_ADAP_MedB_PayFreq"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_MedPrem", 1, "LA_ADAP_MedPrem_MedB",
                    "Select How often is the Part B premium paid?"));
            }
            if ((sv.HasPositiveResponse("LA_ADAP_HaveInsMed") && (sv.HasPositiveResponse("LA_ADAP_MedPolicy_TypeA") || sv.HasPositiveResponse("LA_ADAP_MedPolicy_TypeAB"))) && !sv.HasAnyResponse("LA_ADAP_MedB_NextDue"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_MedPrem", 1, "LA_ADAP_MedPrem_MedB",
                    "Enter Next payment due date"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_MedB_RegDue") && !sv.HasAnyResponse("LA_ADAP_MedB_AltDue"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_MedPrem", 1, "LA_ADAP_MedPrem_MedB",
                    "Since you selected the Regular Payment Due Date as \"other\", you must enter Other regular payment due date in the text box"));
            }
            if ((sv.HasPositiveResponse("LA_ADAP_HaveInsMed") && (sv.HasPositiveResponse("LA_ADAP_MedPolicy_TypeA") || sv.HasPositiveResponse("LA_ADAP_MedPolicy_TypeAB"))) && !sv.HasAnyResponse("LA_ADAP_MedB_PastDue"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_MedPrem", 1, "LA_ADAP_MedPrem_MedB",
                    "Select Do you have any premium payments that are past due?"));
            }
            //2. Medicare Part C Premium Payments
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsMed") && sv.HasPositiveResponse("LA_ADAP_MedPolicy_TypeC")  & !sv.HasAnyResponse("LA_ADAP_MedC_Admin"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_MedPrem", 2, "LA_ADAP_MedPrem_MedC",
                    "Enter Medicare Part C insurance company or third party administrator name (who should the premium check be made out to)"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsMed") && sv.HasPositiveResponse("LA_ADAP_MedPolicy_TypeC") && !sv.HasAnyResponse("LA_ADAP_MedC_Addr"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_MedPrem", 2, "LA_ADAP_MedPrem_MedC",
                    "Enter Medicare Part C insurance company or third party administrator street address (who should the premium check be made out to)"));
            }
            
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsMed") && sv.HasPositiveResponse("LA_ADAP_MedPolicy_TypeC") && !sv.HasAnyResponse("LA_ADAP_MedC_City"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_MedPrem", 2, "LA_ADAP_MedPrem_MedC",
                    "Enter City"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsMed") && sv.HasPositiveResponse("LA_ADAP_MedPolicy_TypeC")  && !sv.HasAnyResponse("LA_ADAP_MedC_State"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_MedPrem", 2, "LA_ADAP_MedPrem_MedC",
                    "Enter State"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsMed") && sv.HasPositiveResponse("LA_ADAP_MedPolicy_TypeC") && !sv.HasAnyResponse("LA_ADAP_MedC_Zip"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_MedPrem", 2, "LA_ADAP_MedPrem_MedC",
                    "Enter Zipcode"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsMed") && sv.HasPositiveResponse("LA_ADAP_MedPolicy_TypeC") && !sv.HasAnyResponse("LA_ADAP_MedC_AppAmt"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_MedPrem", 2, "LA_ADAP_MedPrem_MedC",
                    "Enter What is the applicant's portion of the Part C premium amount?"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsMed") && sv.HasPositiveResponse("LA_ADAP_MedPolicy_TypeC")  && !sv.HasAnyResponse("LA_ADAP_MedC_PayFreq"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_MedPrem", 2, "LA_ADAP_MedPrem_MedC",
                    "Select How often is the Part C premium paid?"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsMed") && sv.HasPositiveResponse("LA_ADAP_MedPolicy_TypeC")  && !sv.HasAnyResponse("LA_ADAP_MedC_NextDue"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_MedPrem", 2, "LA_ADAP_MedPrem_MedC",
                    "Enter Next payment due date"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_MedC_RegDue") && !sv.HasAnyResponse("LA_ADAP_MedC_AltDue"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_MedPrem", 2, "LA_ADAP_MedPrem_MedC",
                    "Since you selected the Regular Payment Due Date as \"other\", you must enter Other regular payment due date in the text box"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsMed") && sv.HasPositiveResponse("LA_ADAP_MedPolicy_TypeC")  && !sv.HasAnyResponse("LA_ADAP_MedC_PastDue"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_MedPrem", 2, "LA_ADAP_MedPrem_MedC",
                    "Select Do you have any premium payments that are past due?"));
            }

            //3. Medicare Part D Premium Payments
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsMed") && sv.HasPositiveResponse("LA_ADAP_MedPolicy_TypeD") & !sv.HasAnyResponse("LA_ADAP_MedD_Admin"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_MedPrem", 3, "LA_ADAP_MedPrem_MedD",
                    "Enter Medicare Part D insurance company or third party administrator name (who should the premium check be made out to)"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsMed") && sv.HasPositiveResponse("LA_ADAP_MedPolicy_TypeD") && !sv.HasAnyResponse("LA_ADAP_MedD_Addr"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_MedPrem", 3, "LA_ADAP_MedPrem_MedD",
                    "Enter Medicare Part D insurance company or third party administrator street address (who should the premium check be made out to)"));
            }

            if (sv.HasPositiveResponse("LA_ADAP_HaveInsMed") && sv.HasPositiveResponse("LA_ADAP_MedPolicy_TypeD") && !sv.HasAnyResponse("LA_ADAP_MedD_City"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_MedPrem", 3, "LA_ADAP_MedPrem_MedD",
                    "Enter City"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsMed") && sv.HasPositiveResponse("LA_ADAP_MedPolicy_TypeD") && !sv.HasAnyResponse("LA_ADAP_MedD_State"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_MedPrem", 3, "LA_ADAP_MedPrem_MedD",
                    "Enter State"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsMed") && sv.HasPositiveResponse("LA_ADAP_MedPolicy_TypeD") && !sv.HasAnyResponse("LA_ADAP_MedD_Zip"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_MedPrem", 3, "LA_ADAP_MedPrem_MedD",
                    "Enter Zipcode"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsMed") && sv.HasPositiveResponse("LA_ADAP_MedPolicy_TypeD") && !sv.HasAnyResponse("LA_ADAP_MedD_AppAmt"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_MedPrem", 3, "LA_ADAP_MedPrem_MedD",
                    "Enter What is the applicant's portion of the Part D premium amount?"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsMed") && sv.HasPositiveResponse("LA_ADAP_MedPolicy_TypeD") && !sv.HasAnyResponse("LA_ADAP_MedD_PayFreq"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_MedPrem", 3, "LA_ADAP_MedPrem_MedD",
                    "Select How often is the Part D premium paid?"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsMed") && sv.HasPositiveResponse("LA_ADAP_MedPolicy_TypeD") && !sv.HasAnyResponse("LA_ADAP_MedD_NextDue"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_MedPrem", 3, "LA_ADAP_MedPrem_MedD",
                    "Enter Next payment due date"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_MedD_RegDue") && !sv.HasAnyResponse("LA_ADAP_MedD_AltDue"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_MedPrem", 3, "LA_ADAP_MedPrem_MedD",
                    "Since you selected the Regular Payment Due Date as \"other\", you must enter Other regular payment due date in the text box"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsMed") && sv.HasPositiveResponse("LA_ADAP_MedPolicy_TypeD") && !sv.HasAnyResponse("LA_ADAP_MedD_PastDue"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_MedPrem", 3, "LA_ADAP_MedPrem_MedD",
                    "Select Do you have any premium payments that are past due?"));
            }

            //4. Medicare Supplemental Premium Payments
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsMed") && sv.HasPositiveResponse("LA_ADAP_MedPolicy_TypeSupp") & !sv.HasAnyResponse("LA_ADAP_MedSupp_Admin"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_MedPrem", 4, "LA_ADAP_MedPrem_MedSupp",
                    "Enter Medicare Supplemental insurance company or third party administrator name (who should the premium check be made out to)"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsMed") && sv.HasPositiveResponse("LA_ADAP_MedPolicy_TypeSupp") && !sv.HasAnyResponse("LA_ADAP_MedSupp_Addr"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_MedPrem", 4, "LA_ADAP_MedPrem_MedSupp",
                    "Enter Medicare Supplemental insurance company or third party administrator street address (who should the premium check be made out to)"));
            }

            if (sv.HasPositiveResponse("LA_ADAP_HaveInsMed") && sv.HasPositiveResponse("LA_ADAP_MedPolicy_TypeSupp") && !sv.HasAnyResponse("LA_ADAP_MedSupp_City"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_MedPrem", 4, "LA_ADAP_MedPrem_MedSupp",
                    "Enter City"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsMed") && sv.HasPositiveResponse("LA_ADAP_MedPolicy_TypeSupp") && !sv.HasAnyResponse("LA_ADAP_MedSupp_State"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_MedPrem", 4, "LA_ADAP_MedPrem_MedSupp",
                    "Enter State"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsMed") && sv.HasPositiveResponse("LA_ADAP_MedPolicy_TypeSupp") && !sv.HasAnyResponse("LA_ADAP_MedSupp_Zip"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_MedPrem", 4, "LA_ADAP_MedPrem_MedSupp",
                    "Enter Zipcode"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsMed") && sv.HasPositiveResponse("LA_ADAP_MedPolicy_TypeSupp") && !sv.HasAnyResponse("LA_ADAP_MedSupp_AppAmt"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_MedPrem", 4, "LA_ADAP_MedPrem_MedSupp",
                    "Enter What is the applicant's portion of the Supplemental premium amount?"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsMed") && sv.HasPositiveResponse("LA_ADAP_MedPolicy_TypeSupp") && !sv.HasAnyResponse("LA_ADAP_MedSupp_PayFreq"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_MedPrem", 4, "LA_ADAP_MedPrem_MedSupp",
                    "Select How often is the Supplemental premium paid?"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsMed") && sv.HasPositiveResponse("LA_ADAP_MedPolicy_TypeSupp") && !sv.HasAnyResponse("LA_ADAP_MedSupp_NextDue"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_MedPrem", 4, "LA_ADAP_MedPrem_MedSupp",
                    "Enter Next payment due date"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_MedSupp_RegDue") && !sv.HasAnyResponse("LA_ADAP_MedSupp_AltDue"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_MedPrem", 4, "LA_ADAP_MedPrem_MedSupp",
                    "Since you selected the Regular Payment Due Date as \"other\", you must enter Other regular payment due date in the text box"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsMed") && sv.HasPositiveResponse("LA_ADAP_MedPolicy_TypeSupp") && !sv.HasAnyResponse("LA_ADAP_MedSupp_PastDue"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_MedPrem", 4, "LA_ADAP_MedPrem_MedSupp",
                    "Select Do you have any premium payments that are past due?"));
            }
            #endregion

            #region Non-Medicare Health Insurance Policy Information

            //1.What type of primary health insurance policy do you have?
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsNotMed") && !sv.HasAnyResponse("LA_ADAP_InsPolicy_TypeDrop"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_InsPolicy", 1, "LA_ADAP_InsPolicy_Type",
                    "Select What type of primary health insurance policy do you have?"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_InsPolicy_TypeDrop") && !sv.HasAnyResponse("LA_ADAP_InsPolicy_TypeOther"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_InsPolicy", 1, "LA_ADAP_InsPolicy_Type",
                    "Since you selected What type of primary health insurance policy do you have as \"other\", you must enter Other type of insurance in the text box"));
            }
            //2. What type of assistance are you requesting from LA HAP for your primary health policy? Check all that apply.
            if (!sv.AnyPositiveResponses("LA_ADAP_InsPolicy_Req_Prem", "LA_ADAP_InsPolicy_Req_Copay", "LA_ADAP_InsPolicy_Req_Drug"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_InsPolicy", 2, "LA_ADAP_InsPolicy_Request",
                    "You must check at least one box to indicate What type of assistance are you requesting from LA HAP for your primary health policy"));
            }
            //3. Primary Health Insurance Company and Plan Name
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsNotMed") && !sv.HasAnyResponse("LA_ADAP_InsPolicy_Co"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_InsPolicy", 3, "LA_ADAP_InsPolicy_Name",
                    "Enter Primary Health Insurance Company (example: Blue Cross Blue Shield)"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsNotMed") && !sv.HasAnyResponse("LA_ADAP_InsPolicy_Plan"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_InsPolicy", 3, "LA_ADAP_InsPolicy_Name",
                    "Enter Primary Health Insurance Plan Name (example: Blue Max 100/80 $1800)"));
            }

            //4.Primary Health Insurance Member ID/Policy #
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsNotMed") && !sv.HasAnyResponse("LA_ADAP_InsPolicy_IDNum"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_InsPolicy", 4, "LA_ADAP_InsPolicy_ID",
                    "Enter Primary Health Insurance Member ID/Policy#"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsNotMed") && !sv.HasAnyResponse("LA_ADAP_InsPolicy_NoID"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_InsPolicy", 4, "LA_ADAP_InsPolicy_ID",
                    "Check My insurance company hasn't given me a Member ID or Policy # for my primary insurance policy yet"));
            }
            //5.Primary Health Insurance Group Number
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsNotMed") && !sv.HasAnyResponse("LA_ADAP_InsPolicy_GroupNum"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_InsPolicy", 5, "LA_ADAP_InsPolicy_Group",
                    "Enter Primary Health Insurance Group Number"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsNotMed") && !sv.HasAnyResponse("LA_ADAP_InsPolicy_NoGroup"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_InsPolicy", 5, "LA_ADAP_InsPolicy_Group",
                    "Check My insurance company hasn't given me a group number for my primary health insurance policy yet "));
            }
            //6.Primary Health Insurance Policy Start Date
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsNotMed") && !sv.HasAnyResponse("LA_ADAP_InsPolicy_StartDt"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_InsPolicy", 6, "LA_ADAP_InsPolicy_Start",
                    "Enter Primary Health Insurance Policy Start Date"));
            }
            //7.COBRA Policy End Date
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsNotMed") && !sv.HasAnyResponse("LA_ADAP_InsPolicy_EndDt"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_InsPolicy", 7, "LA_ADAP_InsPolicy_End",
                    "Enter COBRA Policy End Date"));
            }
            #endregion

            #region Non-Medicare Health Insurance Premium Information
            //Primary Insurance
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsNotMed") && !sv.HasAnyResponse("LA_ADAP_InsPrem_PrimCProvider"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsgWithoutSectitle(formsRepo, "LA_ADAP_InsPrem", 10,
                    "Enter Primary health insurance company, employer, or third party administrator name (who should the premium check be made out to)"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsNotMed") && !sv.HasAnyResponse("LA_ADAP_InsPrem_PrimCStreet"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsgWithoutSectitle(formsRepo, "LA_ADAP_InsPrem", 10,
                    "Enter Street address for primary health insurance premium payment"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsNotMed") && !sv.HasAnyResponse("LA_ADAP_InsPrem_PrimCCity"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsgWithoutSectitle(formsRepo, "LA_ADAP_InsPrem", 10,
                    "Enter City"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsNotMed") && !sv.HasAnyResponse("LA_ADAP_InsPrem_PrimCState"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsgWithoutSectitle(formsRepo, "LA_ADAP_InsPrem", 10,
                    "Select State"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsNotMed") && !sv.HasAnyResponse("LA_ADAP_InsPrem_PrimCZipCode"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsgWithoutSectitle(formsRepo, "LA_ADAP_InsPrem", 10,
                    "Enter Zip Code"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsNotMed") && !sv.HasAnyResponse("LA_ADAP_InsPrem_PrimCPortion"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsgWithoutSectitle(formsRepo, "LA_ADAP_InsPrem", 10,
                    "Enter What is the applicant's portion of the primary health premium amount?"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsNotMed") && !sv.HasAnyResponse("LA_ADAP_InsPrem_PrimCFreq"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsgWithoutSectitle(formsRepo, "LA_ADAP_InsPrem", 10,
                    "Select How often is the premium paid?"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsNotMed") && !sv.HasAnyResponse("LA_ADAP_InsPrem_PrimCNext"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsgWithoutSectitle(formsRepo, "LA_ADAP_InsPrem", 10,
                    "Enter Next payment due date"));
            }
            //other no identifier for textbox
            //if (sv.HasPositiveResponse("LA_ADAP_InsPrem_PrimCDueDate") && !sv.HasAnyResponse(""))
            //{
            //    invalid = true;
            //    model.validationMessages.Add(valErrMsgWithoutSectitle(formsRepo, "LA_ADAP_InsPrem", 10,
            //        "Enter Other regular payment due date"));
            //}
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsNotMed") && !sv.HasAnyResponse("LA_ADAP_InsPrem_PrimCPastDue"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsgWithoutSectitle(formsRepo, "LA_ADAP_InsPrem", 10,
                    "Select Do you have any premium payments that are past due?"));
            }

            //Secondary Insurance
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsNotMed") && sv.HasExactResponse("LA_DoYouHaveSecHealthIns", "1") && !sv.HasAnyResponse("LA_ADAP_InsPrem_SecondCProvider"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsgWithoutSectitle(formsRepo, "LA_ADAP_InsPrem", 10,
                    "Enter Secondary health insurance company, employer, or third party administrator name (who should the premium check be made out to)"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsNotMed") && sv.HasExactResponse("LA_DoYouHaveSecHealthIns", "1") && !sv.HasAnyResponse("LA_ADAP_InsPrem_SecondCStreet"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsgWithoutSectitle(formsRepo, "LA_ADAP_InsPrem", 10,
                    "Enter Street address for Secondary health insurance premium payment"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsNotMed") && sv.HasExactResponse("LA_DoYouHaveSecHealthIns", "1") && !sv.HasAnyResponse("LA_ADAP_InsPrem_SecondCCity"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsgWithoutSectitle(formsRepo, "LA_ADAP_InsPrem", 10,
                    "Enter City"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsNotMed") && sv.HasExactResponse("LA_DoYouHaveSecHealthIns", "1") && !sv.HasAnyResponse("LA_ADAP_InsPrem_SecondCState"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsgWithoutSectitle(formsRepo, "LA_ADAP_InsPrem", 10,
                    "Select State"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsNotMed") && sv.HasExactResponse("LA_DoYouHaveSecHealthIns", "1") && !sv.HasAnyResponse("LA_ADAP_InsPrem_SecondCZipCode"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsgWithoutSectitle(formsRepo, "LA_ADAP_InsPrem", 10,
                    "Enter Zip Code"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsNotMed") && sv.HasExactResponse("LA_DoYouHaveSecHealthIns", "1") && !sv.HasAnyResponse("LA_ADAP_InsPrem_SecondCPortion"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsgWithoutSectitle(formsRepo, "LA_ADAP_InsPrem", 10,
                    "Enter What is the applicant's portion of the primary health premium amount?"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsNotMed") && sv.HasExactResponse("LA_DoYouHaveSecHealthIns", "1") && !sv.HasAnyResponse("LA_ADAP_InsPrem_SecondCFreq"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsgWithoutSectitle(formsRepo, "LA_ADAP_InsPrem", 10,
                    "Select How often is the premium paid?"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsNotMed") && sv.HasExactResponse("LA_DoYouHaveSecHealthIns", "1") && !sv.HasAnyResponse("LA_ADAP_InsPrem_SecondCNext"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsgWithoutSectitle(formsRepo, "LA_ADAP_InsPrem", 10,
                    "Enter Next payment due date"));
            }
            //other no identifier for textbox
            //if (sv.HasPositiveResponse("LA_ADAP_InsPrem_PrimCDueDate") && sv.HasExactResponse("LA_DoYouHaveSecHealthIns", "1") && !sv.HasAnyResponse(""))
            //{
            //    invalid = true;
            //    model.validationMessages.Add(valErrMsgWithoutSectitle(formsRepo, "LA_ADAP_InsPrem", 10,
            //        "Enter Other regular payment due date"));
            //}
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsNotMed") && sv.HasExactResponse("LA_DoYouHaveSecHealthIns", "1") && !sv.HasAnyResponse("LA_ADAP_InsPrem_SecondCPastDue"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsgWithoutSectitle(formsRepo, "LA_ADAP_InsPrem", 10,
                    "Select Do you have any premium payments that are past due?"));
            }

            #endregion

            #region Dental and Vision Insurance Policy Information

            //1.What type of dental insurance policy do you have?
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsNotHealth") && !sv.HasAnyResponse("LA_ADAP_DentPolicy_InsTypeDrop"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_DentPolicy", 1, "LA_ADAP_DentPolicy_InsType",
                    "Select What type of dental insurance policy do you have?"));
            }
            //3. Dental Insurance Company (example: AlwaysCare) 
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsNotHealth") && !sv.HasAnyResponse("LA_ADAP_DentPolicy_CompName"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsgWithoutSectitle(formsRepo, "LA_ADAP_DentPolicy", 3,
                    "Enter Dental Insurance Company (example: AlwaysCare)"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsNotHealth") && !sv.HasAnyResponse("LA_ADAP_DentPolicy_PlanName"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsgWithoutSectitle(formsRepo, "LA_ADAP_DentPolicy", 3,
                    "Enter Dental Insurance Plan Name"));
            }

            //4.Dental Insurance Member ID/Policy #
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsNotHealth") && !sv.HasAnyResponse("LA_ADAP_DentPolicy_PolicyNo"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_DentPolicy", 4, "LA_ADAP_DentPolicy_PolicyNo",
                    "Enter Dental Insurance Member ID/Policy #"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsNotHealth") && !sv.HasAnyResponse("LA_ADAP_DentPolicy_NoPolicyNo"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_DentPolicy", 4, "LA_ADAP_DentPolicy_PolicyNo",
                    "Check My dental insurance company hasn't given me a Member ID or Policy # for my Dental insurance policy yet"));
            }
            //5.Dental Insurance Group Number
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsNotHealth") && !sv.HasAnyResponse("LA_ADAP_DentPolicy_GroupNo"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_DentPolicy", 5, "LA_ADAP_DentPolicy_GroupNo",
                    "Enter Dental Insurance Group Number"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsNotHealth") && !sv.HasAnyResponse("LA_ADAP_DentPolicy_NoGroupNo"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_DentPolicy", 5, "LA_ADAP_DentPolicy_GroupNo",
                    "Check My Dental insurance company hasn't given me a group number for my Dental insurance policy yet"));
            }
            //6.Dental Insurance Policy Start Date
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsNotHealth") && !sv.HasAnyResponse("LA_ADAP_DentPolicy_PolicyStart"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_DentPolicy", 6, "LA_ADAP_DentPolicy_PolicyStart",
                    "Enter Dental Insurance Policy Start Date"));
            }

            //Do you have stand-alone vision insurance coverage (vision ONLY) that is not included in a health and/or dental policy? 
            //7. What type of assistance are you requesting from LA HAP for the VISION INSURANCE policy
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsNotHealth") && sv.HasExactResponse("LA_ADAP_DentPolicy_VisionIns", "1") && !sv.AnyPositiveResponses("LA_ADAP_DentPolicy_VisionAssistPrem", "LA_ADAP_DentPolicy_VisionAssistCopay"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_DentPolicy", 7, "LA_ADAP_DentPolicy_VisionAssist",
                    "You must check at least one box to indicate What type of assistance are you requesting from LA HAP for the VISION INSURANCE policy"));
            }
            //8. Vision Insurance Company 
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsNotHealth") && sv.HasExactResponse("LA_ADAP_DentPolicy_VisionIns", "1") && !sv.HasAnyResponse("LA_ADAP_DentPolicy_VisionName"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsgWithoutSectitle(formsRepo, "LA_ADAP_DentPolicy", 8,
                    "Enter Vision Insurance Company"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsNotHealth") && sv.HasExactResponse("LA_ADAP_DentPolicy_VisionIns", "1") && !sv.HasAnyResponse("LA_ADAP_DentPolicy_VisionPlan"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsgWithoutSectitle(formsRepo, "LA_ADAP_DentPolicy", 8,
                    "Enter Vision Insurance Plan Name"));
            }

            //9.Vision Insurance Member ID/Policy # 
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsNotHealth") && sv.HasExactResponse("LA_ADAP_DentPolicy_VisionIns", "1") && !sv.HasAnyResponse("LA_ADAP_DentPolicy_VisionPolicy"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsgWithoutSectitle(formsRepo, "LA_ADAP_DentPolicy", 9, 
                    "Enter Vision Insurance Member ID/Policy #"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsNotHealth") && sv.HasExactResponse("LA_ADAP_DentPolicy_VisionIns", "1") && !sv.HasAnyResponse("LA_ADAP_DentPolicy_NoVisionPolicy"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsgWithoutSectitle(formsRepo, "LA_ADAP_DentPolicy", 9, 
                    "Check My Vision insurance company hasn't given me a Member ID or Policy # for my primary insurance policy yet"));
            }

            //10.Vision Insurance Group Number 
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsNotHealth") && sv.HasExactResponse("LA_ADAP_DentPolicy_VisionIns", "1") && !sv.HasAnyResponse("LA_ADAP_DentPolicy_VisionGroup"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsgWithoutSectitle(formsRepo, "LA_ADAP_DentPolicy", 10, 
                    "Enter Vision Insurance Group Number "));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsNotHealth") && sv.HasExactResponse("LA_ADAP_DentPolicy_VisionIns", "1") && !sv.HasAnyResponse("LA_ADAP_DentPolicy_NoVisionGroup"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsgWithoutSectitle(formsRepo, "LA_ADAP_DentPolicy", 10, 
                    "Check My Vision insurance company hasn't given me a group number for my Vision insurance policy yet"));
            }
            //11.Dental Insurance Policy Start Date
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsNotHealth") && sv.HasExactResponse("LA_ADAP_DentPolicy_VisionIns", "1") && !sv.HasAnyResponse("LA_ADAP_DentPolicy_VisionStart"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "LA_ADAP_DentPolicy", 11, "LA_ADAP_DentPolicy_VisionStart",
                    "Enter Vision Insurance Policy Start Date"));
            }

            #endregion

            #region Dental and Vision Insurance Premium Information
            //Dental Insurance
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsNotHealth") && !sv.HasAnyResponse("LA_ADAP_DentalPrem_DentalName"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsgWithoutSectitle(formsRepo, "LA_ADAP_DentalPrem", 12,
                    "Enter Dental insurance company, employer, or third party administrator name (who should the premium check be made out to)"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsNotHealth") && !sv.HasAnyResponse("LA_ADAP_DentalPrem_DentalStreet"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsgWithoutSectitle(formsRepo, "LA_ADAP_DentalPrem", 12,
                    "Enter Street address for dental insurance premium payment"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsNotHealth") && !sv.HasAnyResponse("LA_ADAP_DentalPrem_DentalCity"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsgWithoutSectitle(formsRepo, "LA_ADAP_DentalPrem", 12,
                    "Enter City"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsNotHealth") && !sv.HasAnyResponse("LA_ADAP_DentalPrem_DentalState"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsgWithoutSectitle(formsRepo, "LA_ADAP_DentalPrem", 12,
                    "Select State"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsNotHealth") && !sv.HasAnyResponse("LA_ADAP_DentalPrem_DentalZip"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsgWithoutSectitle(formsRepo, "LA_ADAP_DentalPrem", 12,
                    "Enter Zip Code"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsNotHealth") && !sv.HasAnyResponse("LA_ADAP_DentalPrem_DentalAmt"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsgWithoutSectitle(formsRepo, "LA_ADAP_DentalPrem", 12,
                    "Enter What is the applicant's portion of the dental insurance premium amount?"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsNotHealth") && !sv.HasAnyResponse("LA_ADAP_DentalPrem_DentalFreq"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsgWithoutSectitle(formsRepo, "LA_ADAP_DentalPrem", 12,
                    "Select How often is the premium paid?"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsNotHealth") && !sv.HasAnyResponse("LA_ADAP_DentalPrem_DentalNext"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsgWithoutSectitle(formsRepo, "LA_ADAP_DentalPrem", 12,
                    "Enter Next payment due date"));
            }

            if (sv.HasPositiveResponse("LA_ADAP_DentalPrem_DentalDueDate") && !sv.HasAnyResponse("LA_ADAP_DentalPrem_DentalOtherDue"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsgWithoutSectitle(formsRepo, "LA_ADAP_DentalPrem", 12,
                    "Enter Other regular payment due date"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsNotHealth") && !sv.HasAnyResponse("LA_ADAP_DentalPrem_DentalPastDue"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsgWithoutSectitle(formsRepo, "LA_ADAP_DentalPrem", 12,
                    "Select Do you have any premium payments that are past due?"));
            }

            //Secondary Vision
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsNotHealth") && sv.HasExactResponse("LA_ADAP_DentPolicy_VisionIns", "1") && !sv.HasAnyResponse("LA_ADAP_DentalPrem_VisionName"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsgWithoutSectitle(formsRepo, "LA_ADAP_DentalPrem", 12,
                    "Enter Dental insurance company, employer, or third party administrator name (who should the premium check be made out to)"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsNotHealth") && sv.HasExactResponse("LA_ADAP_DentPolicy_VisionIns", "1") && !sv.HasAnyResponse("LA_ADAP_DentalPrem_VisionStreet"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsgWithoutSectitle(formsRepo, "LA_ADAP_DentalPrem", 12,
                    "Enter Street address for dental insurance premium payment"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsNotHealth") && sv.HasExactResponse("LA_ADAP_DentPolicy_VisionIns", "1") && !sv.HasAnyResponse("LA_ADAP_DentalPrem_VisionCity"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsgWithoutSectitle(formsRepo, "LA_ADAP_DentalPrem", 12,
                    "Enter City"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsNotHealth") && sv.HasExactResponse("LA_ADAP_DentPolicy_VisionIns", "1") && !sv.HasAnyResponse("LA_ADAP_DentalPrem_VisionState"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsgWithoutSectitle(formsRepo, "LA_ADAP_DentalPrem", 12,
                    "Select State"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsNotHealth") && sv.HasExactResponse("LA_ADAP_DentPolicy_VisionIns", "1") && !sv.HasAnyResponse("LA_ADAP_DentalPrem_VisionZip"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsgWithoutSectitle(formsRepo, "LA_ADAP_DentalPrem", 12,
                    "Enter Zip Code"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsNotHealth") && sv.HasExactResponse("LA_ADAP_DentPolicy_VisionIns", "1") && !sv.HasAnyResponse("LA_ADAP_DentalPrem_VisionAmt"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsgWithoutSectitle(formsRepo, "LA_ADAP_DentalPrem", 12,
                    "Enter What is the applicant's portion of the dental insurance premium amount?"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsNotHealth") && sv.HasExactResponse("LA_ADAP_DentPolicy_VisionIns", "1") && !sv.HasAnyResponse("LA_ADAP_DentalPrem_VisionFreq"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsgWithoutSectitle(formsRepo, "LA_ADAP_DentalPrem", 12,
                    "Select How often is the premium paid?"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsNotHealth") && sv.HasExactResponse("LA_ADAP_DentPolicy_VisionIns", "1") && !sv.HasAnyResponse("LA_ADAP_DentalPrem_VisionNext"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsgWithoutSectitle(formsRepo, "LA_ADAP_DentalPrem", 12,
                    "Enter Next payment due date"));
            }

            if (sv.HasPositiveResponse("LA_ADAP_DentalPrem_VisionDueDate") && sv.HasExactResponse("LA_ADAP_DentPolicy_VisionIns", "1") && !sv.HasAnyResponse("LA_ADAP_DentalPrem_VisionOtherDue"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsgWithoutSectitle(formsRepo, "LA_ADAP_DentalPrem", 12,
                    "Enter Other regular payment due date"));
            }
            if (sv.HasPositiveResponse("LA_ADAP_HaveInsNotHealth") && sv.HasExactResponse("LA_ADAP_DentPolicy_VisionIns", "1") && !sv.HasAnyResponse("LA_ADAP_DentalPrem_VisionPastDue"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsgWithoutSectitle(formsRepo, "LA_ADAP_DentalPrem", 12,
                    "Select Do you have any premium payments that are past due?"));
            }

            #endregion

            return invalid;
        }

        private static string valErrMsg(IFormsRepository formsRepo, string sectionIdentifier, int subsectionNumber, string subsectionIdentifier, string message)
        {
            def_Sections section = formsRepo.GetSectionByIdentifier(sectionIdentifier);
            if (section == null)
                throw new Exception("Validation: Building one-off validation error message: could not find section with identifier \"" + sectionIdentifier + "\"");

            def_Sections sub = formsRepo.GetSectionByIdentifier(subsectionIdentifier);
            if (sub == null)
                throw new Exception("Validation: Building one-off validation error message: could not find section with identifier \"" + subsectionIdentifier + "\"");

            return section.title + ": " + subsectionNumber + ". " + sub.title + ": " + message;
        }
        private static string valErrMsgWithoutSectitle(IFormsRepository formsRepo, string sectionIdentifier, int subsectionNumber, string message)
        {
            def_Sections section = formsRepo.GetSectionByIdentifier(sectionIdentifier);
            if (section == null)
                throw new Exception("Validation: Building one-off validation error message: could not find section with identifier \"" + sectionIdentifier + "\"");

            
            return section.title + ": " + subsectionNumber +". "+  message;
        }
    }
}
