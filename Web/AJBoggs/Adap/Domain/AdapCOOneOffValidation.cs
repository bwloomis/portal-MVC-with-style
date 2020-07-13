using Assmnts.Business;
using Assmnts.Models;


namespace AJBoggs.Adap.Domain
{
    
    public class AdapCOOneOffValidation
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
        public static bool RunOneOffValidation( SharedValidation sv, AdapValidationErrorsModel model ) {

            bool invalid = false;

            #region other validation (errors)

            #region Demographic Info
            //at least one of the D3 checkboxes must be checked
            if (!sv.AnyPositiveResponses( 
                "ADAP_D3_White", "ADAP_D3_Native", "ADAP_D3_Asian", /*"ADAP_D3_NA",*/ "ADAP_D3_Black", "ADAP_D3_Indian" /*, "ADAP_D3_Other"*/ ))
            {
                invalid = true;
                model.validationMessages.Add(Resources.AdapAppNavMenu.DemographicInfo + ": " + Resources.Validation.MustCheckOneUnderSection_ + " D3");
            }

            //if Hispanic/Latino (a), at least on D4 checkbox must be checked
            if (sv.HasExactResponse("ADAP_D4_EthnicDrop", "1") && !sv.AnyPositiveResponses(
                "ADAP_D4_Mexican", "ADAP_D4_Puerto", /*"ADAP_D4_NA",*/ "ADAP_D4_Cuban", "ADAP_D4_Other"/*, "ADAP_D4_Unknown"*/ ))
            {
                invalid = true;
                model.validationMessages.Add( Resources.AdapAppNavMenu.DemographicInfo +
                    " D4: " + Resources.Validation.SinceHispanic_ + Resources.Validation.MustCheckOneUnderSection_ + " D4");
            }

            //D5
            if (sv.HasPositiveResponse("ADAP_D3_Asian") && !sv.AnyPositiveResponses(
                "ADAP_D5_Indian", "ADAP_D5_Filipino", "ADAP_D5_Korean", "ADAP_D5_Other", 
                "ADAP_D5_Chinese", "ADAP_D5_Japanese", "ADAP_D5_Vietnamese", "ADAP_D5_NA"))
            {
                invalid = true;
                model.validationMessages.Add(Resources.AdapAppNavMenu.DemographicInfo +
                    " D5: " + Resources.Validation.SinceAsian_ + Resources.Validation.MustCheckOneUnderSection_ + " D5");
            }

            //D6
            if (sv.HasPositiveResponse("ADAP_D3_Native") && !sv.AnyPositiveResponses(
                "ADAP_D6_Native", "ADAP_D6_Guam", "ADAP_D6_Samoan", "ADAP_D6_Other", "ADAP_D6_NA" ))
            {
                invalid = true;
                model.validationMessages.Add(Resources.AdapAppNavMenu.DemographicInfo +
                    " D6: " + Resources.Validation.SinceIslander_ + Resources.Validation.MustCheckOneUnderSection_ + " D6");
            }

            //D7
            if (sv.HasExactResponse("ADAP_D7_LangDrop", "17") && !sv.HasAnyResponse("ADAP_D7_LangOther"))
            {
                invalid = true;
                model.validationMessages.Add(Resources.AdapAppNavMenu.DemographicInfo + 
                    " D7: " + Resources.Validation.MustExplainOtherLang );
            }

            //D9
            //If the Ramsell ID is a thing but not a positive (or, for that matter, any numeric) response...
            //ASSUMES The entry is "_" where it is not numeric.
            if (sv.HasAnyResponse("ADAP_D9_Ramsell") && !sv.HasPositiveResponse("ADAP_D9_Ramsell"))
            {
                invalid = true;
                model.validationMessages.Add(Resources.AdapAppNavMenu.DemographicInfo +
                    " D9: " + Resources.Validation.RamsellID11Digits);
            }

            #endregion

            #region Contact Info

            //C2
            if (!sv.HasExactResponse("ADAP_C2_SameAsMailing", "1") && !sv.AllComplete(
                "ADAP_C2_Address", "ADAP_C2_MayContactYN", "ADAP_C2_City", "ADAP_C2_State", "ADAP_C2_Zip", "ADAP_C2_County" ) )
            {
                invalid = true;
                model.validationMessages.Add(Resources.AdapAppNavMenu.ContactInfo +
                    ": " + Resources.Validation.SinceMailNotResid_ + " " + Resources.Validation.MustCompleteAllFieldsUnder_ + " C2.");
            }

            //C4
            if (sv.HasExactResponse("ADAP_C4_MayCallYN", "1") && !sv.AllComplete(
                "ADAP_C4_Name", "ADAP_C4_Phone", "ADAP_C4_KnowHivYN"))
            {
                invalid = true;
                model.validationMessages.Add(Resources.AdapAppNavMenu.ContactInfo + 
                    ": " + Resources.Validation.SinceYesUnder_ + " C4, " + Resources.Validation.MustCompleteAllFieldsUnder_  + " C4");
            }

            //C5
            if (sv.HasExactResponse("ADAP_C5_HasCaseMngrYN", "1") && !sv.AllComplete(
                "ADAP_C5_Mngr1_Name" /*, "ADAP_C5_Mngr1_Clinic", "ADAP_C5_Mngr2_Name", "ADAP_C5_Mngr2_Clinic"*/ ) )
            {
                invalid = true;
                model.validationMessages.Add(Resources.AdapAppNavMenu.ContactInfo +
                    ": " + Resources.Validation.SinceYesUnder_ + " C5, " + Resources.Validation.MustEnterCaseWorker );
            }

            #endregion

            #region Medical (currently empty)

            #endregion

            #region Health Insurance

            //I2 invoice uploads
            if (sv.HasExactResponse("ADAP_I2_AffCareOpt", "0")
                && sv.HasPositiveResponse("ADAP_I2_InvoiceYN")
                && !sv.HasAnyResponse("ADAP_I2_Invoice"))
            {
                invalid = true;
                model.validationMessages.Add(Resources.AdapAppNavMenu.Insurance + " I2: " + Resources.Validation.CheckedYesButNoInvoice);
            }

            //I3 invoice uploads
            if (sv.HasExactResponse("ADAP_I3_MedicareYN", "1")
                && sv.HasPositiveResponse("ADAP_I3_InvoiceYN")
                && !sv.HasAnyResponse("ADAP_I3_Invoice"))
            {
                invalid = true;
                model.validationMessages.Add(Resources.AdapAppNavMenu.Insurance + " I3: " + Resources.Validation.CheckedYesButNoInvoice);
            }
            
            //I3
            if (sv.HasExactResponse("ADAP_I3_MedicareYN", "1"))
            {
                bool partA = sv.HasExactResponse("ADAP_I3_PartAYN", "1");
                bool partB = sv.HasExactResponse("ADAP_I3_PartBYN", "1");

                if (!partA && !partB)
                {
                    invalid = true;
                    model.validationMessages.Add(Resources.AdapAppNavMenu.Insurance 
                        + " I3: " + Resources.Validation.SinceMedicareMustCheckAOrB );
                }
            }

            //I4 (if "other" is selected, require explaination)
            if (sv.HasExactResponse("ADAP_I4_InsSourceOpt", "4"))
            {
                if (!sv.HasAnyResponse("ADAP_I4_InsSourceOther"))
                {
                    invalid = true;
                    model.validationMessages.Add( Resources.AdapAppNavMenu.Insurance 
                        + " I4: " + Resources.Validation.MustExplainOther );
                }
            }

            #endregion

            #region Household

            //H2
            if (sv.HasExactResponse("ADAP_H2_RelnDrop", "0") && !sv.HasAnyResponse("ADAP_H2_RelnOther"))
            {
                invalid = true;
                model.validationMessages.Add( Resources.AdapAppNavMenu.Household +
                    " H2: " + Resources.Validation.MustExplainOther );
            }

            //H3
            int statusRsp = sv.GetResponseInt("ADAP_H3_FileTaxYN");
            if (((statusRsp == 1) && !sv.AllComplete( "ADAP_H3_TaxStatusOpt", "ADAP_H3_TaxDependants")) ||
                 ((statusRsp == 0) && !sv.AllComplete( "ADAP_H3_TaxNotFileOpt", "ADAP_H3_Relatives")))
            {
                invalid = true;
                model.validationMessages.Add(Resources.AdapAppNavMenu.Household +
                    " H3: " + Resources.Validation.MustCompleteAllFieldsUnder_ + " H3");
            }

            //H5
            //if (hasExactResponse("ADAP_D8_CurrGenderDrop", "2", formResultId ) ){
            //    if(!hasAnyResponse( "ADAP_H5_PregnantOpt", formResultId ) )
            //    {
            //        invalid = true;
            //        amt.validationMessages.Add("Since you are female, you must indicate whether or not you are pregnant under H5");
            //    }
            //    if (hasExactResponse("ADAP_H5_PregnantOpt", "1", formResultId) && !hasAnyResponse("ADAP_H5_PregnantDue", formResultId))
            //    {
            //        invalid = true;
            //        amt.validationMessages.Add("H5: since you indicated that you are pregnant, you must enter a due date.");
            //    }
            //}
            
            #endregion

            #region Household Income

            //F1, F2
            int employOptRsp = sv.GetResponseInt("ADAP_F1_EmployOpt");
            //if ((employOptRsp == 7) && !sv.HasAnyResponse("ADAP_F1_EmployOther")) 
            //{
            //    invalid = true;
            //    model.validationMessages.Add(Resources.AdapAppNavMenu.HouseholdIncome + 
            //        " F1: " + Resources.Validation.SinceEmpOtherMustSpecify );
            //}

            if ( (employOptRsp == 4) || (employOptRsp == 5) || (employOptRsp == 6) )
            {
                if (!sv.HasAnyResponse("ADAP_F1_EmployerInsOpt"))
                {
                    invalid = true;
                    model.validationMessages.Add(Resources.AdapAppNavMenu.HouseholdIncome +
                        " F1: " + Resources.Validation.SinceHaveEmp_ + ", " + Resources.Validation.MustSelectEmployerInsOpt);
                }
                else if (sv.HasExactResponse("ADAP_F1_EmployerInsOpt", "4") && !sv.HasAnyResponse("ADAP_F1_EmployNotEnrolled"))
                {
                    invalid = true;
                    model.validationMessages.Add(Resources.AdapAppNavMenu.HouseholdIncome +
                        " F1: " + Resources.Validation.NeeedReasonNotEnrolledEmpIns );
                }

                if (!sv.HasAnyResponse("ADAP_F2_EmployLast90YN"))
                {
                    invalid = true;
                    model.validationMessages.Add(Resources.AdapAppNavMenu.HouseholdIncome + 
                        " F2: " + Resources.Validation.SinceHaveEmp_ + ", " + Resources.Validation.YouMustComplete_ + " F2.");
                }
            }

            //F3

            //iterate through each of teh "proof of income #x" sections
            for (int proofSectionIndex = 1; proofSectionIndex <= 4; proofSectionIndex++)
            {
                string letter = "ABCD".Substring(proofSectionIndex - 1, 1);
                string prefix = "ADAP_F3_" + letter + "_";
                int typeRsp = sv.GetResponseInt(prefix + "IncomeTypeDrop");

                //if income type "other" was selected, but no explaination provided...
                if ((typeRsp == 14) && !sv.HasAnyResponse(prefix + "IncomeTypeOther"))
                {
                    invalid = true;
                    model.validationMessages.Add(Resources.AdapAppNavMenu.HouseholdIncome + " F3 (" 
                        + Resources.Validation.ProofOfIncome + " #" + proofSectionIndex + "): " 
                        + Resources.Validation.SinceIncomOtherMustSpecify );
                }

                //if employed and enrolled in employer insurance...
                if (typeRsp == 0 && sv.HasExactResponse("ADAP_F1_EmployerInsOpt", "1"))
                {
                    //if there are any missing responses in this "proof of income #X" section...
                    if (!sv.AllComplete(
                        prefix + "Employer", prefix + "EmployStart", prefix + "TempYN", prefix + "IncomeAmt", prefix + "IncomeProof", prefix + "EmployerForm"))
                    {
                        invalid = true;
                        model.validationMessages.Add(Resources.AdapAppNavMenu.HouseholdIncome + " F3 ("
                            + Resources.Validation.ProofOfIncome + " #" + proofSectionIndex + "): "
                            + Resources.Validation.SinceIncomeEmpMustComplete);
                    }
                }

                //if employed but not enrolled in employer insurance...
                else if( typeRsp == 0 )
                {
                    //if there are any missing responses in this "proof of income #X" section (other than the employer insurance form)...
                    if (!sv.AllComplete(
                        prefix + "Employer", prefix + "EmployStart", prefix + "TempYN", prefix + "IncomeAmt", prefix + "IncomeProof"))
                    {
                        invalid = true;
                        model.validationMessages.Add(Resources.AdapAppNavMenu.HouseholdIncome + " F3 ("
                            + Resources.Validation.ProofOfIncome + " #" + proofSectionIndex + "): "
                            + Resources.Validation.SinceIncomeEmpMustComplete);
                    }
                }
            }

            #endregion

            #endregion

            #region other validation (warnings)


            //C1
            if (!sv.HasAnyResponse("ADAP_C1_AddressProof"))
            {
                model.validationMessages.Add(Resources.Validation.WARNING + ": " + Resources.AdapAppNavMenu.ContactInfo + " C1: " + Resources.Validation.NoProofResidency);
            }

            //I3
            if (sv.HasExactResponse("ADAP_I3_MedicareYN", "1"))
            {
                bool partA = sv.HasExactResponse("ADAP_I3_PartAYN", "1");
                bool partB = sv.HasExactResponse("ADAP_I3_PartBYN", "1");

                if (partA && !sv.HasAnyResponse("ADAP_I3_PartADate"))
                {
                    model.validationMessages.Add(Resources.Validation.WARNING + ": " + Resources.AdapAppNavMenu.HealthInsurance + " I3: " + Resources.Validation.MedicareANoDate );
                }

                if (partB && !sv.HasAnyResponse("ADAP_I3_PartBDate"))
                {
                    model.validationMessages.Add(Resources.Validation.WARNING + ": " + Resources.AdapAppNavMenu.HealthInsurance + " I3: " + Resources.Validation.MedicareBNoDate);
                }
            }

            #endregion

            return invalid;
        }

    }
}
