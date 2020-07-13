using Assmnts;
using Assmnts.Business;
using Assmnts.Models;
using Data.Abstract;
using System;


namespace AJBoggs.Adap.Domain
{

    public class AdapCAOneOffValidation
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

        public static bool RunOneOffValidation(IFormsRepository formsRepo, SharedValidation sv, AdapValidationErrorsModel model)
        {

            bool invalid = false;

            #region Contact Information

            //5. What is your Social Security Number?
            if (sv.HasPositiveResponse("C1_MemberHasSSN") && !sv.HasAnyResponse("C1_MemberSocSecNumber"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-Contact", 2, "CA-ADAP-Contact",
                         "You must enter your SSN or indicate that you do not have a SSN"));
            }
            //if (!sv.HasPositiveResponse("C1_MemberHasSSN"))
            //{
            //    invalid = true;
            //    model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-Contact", 2, "CA-ADAP-Contact-2_sub",
            //             "You must select SSN field"));
            //}
            if (sv.HasPositiveResponse("C1_AddressHomeless"))
            {
                if (sv.GetResponseInt("C1_ResidentialAddressMayContact") != 3)
                {
                    invalid = true;
                    model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-Contact", 3, "CA-ADAP-Contact-3_sub",
                             "You selected Check this box if you are homeless. You must also select No, my enrollment site will receive information on my behalf for the question May we contact you at this address?"));
                }
            }
            else if (!sv.HasPositiveResponse("C1_AddressHomeless"))
            {
                if (sv.GetResponseInt("C1_ResidentialAddressMayContact") == 2)
                {
                    if (!sv.HasAnyResponse("C1_MailingAddressLine1"))
                    {
                        invalid = true;
                        model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-Contact", 3, "CA-ADAP-Contact-3_sub",
                                 "Please Enter Mailing Address"));
                    }
                    if (!sv.HasAnyResponse("C1_MailingAddressCity"))
                    {
                        invalid = true;
                        model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-Contact", 3, "CA-ADAP-Contact-3_sub",
                                 "Please Enter Mailing Address City"));
                    }
                    if (!sv.HasAnyResponse("C1_MailingAddressState"))
                    {
                        invalid = true;
                        model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-Contact", 3, "CA-ADAP-Contact-3_sub",
                                 "Please Enter Mailing Address State"));
                    }
                    if (!sv.HasAnyResponse("C1_MailingAddressZIP"))
                    {
                        invalid = true;
                        model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-Contact", 3, "CA-ADAP-Contact-3_sub",
                                 "Please Enter Mailing Address Zip Code"));
                    }
                }
                else if (sv.GetResponseInt("C1_ResidentialAddressMayContact") == 1)
                {
                    if (!sv.HasAnyResponse("C1_ResidentialAddressLine1"))
                    {
                        invalid = true;
                        model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-Contact", 3, "CA-ADAP-Contact-3_sub",
                                 "Please Enter Residential Address"));
                    }
                    if (!sv.HasAnyResponse("C1_ResidentialAddressCity"))
                    {
                        invalid = true;
                        model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-Contact", 3, "CA-ADAP-Contact-3_sub",
                                 "Please Enter Residential Address City"));
                    }
                    if (!sv.HasAnyResponse("C1_ResidentialAddressState"))
                    {
                        invalid = true;
                        model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-Contact", 3, "CA-ADAP-Contact-3_sub",
                                 "Please Enter Residential Address State"));
                    }
                    if (!sv.HasAnyResponse("C1_ResidentialAddressZIP"))
                    {
                        invalid = true;
                        model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-Contact", 3, "CA-ADAP-Contact-3_sub",
                                 "Please Enter Residential Address Zip Code"));
                    }
                }
                else if (sv.GetResponseInt("C1_ResidentialAddressMayContact") == 3)
                {
                    invalid = true;
                    model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-Contact", 3, "CA-ADAP-Contact-3_sub",
                             "Please check the Homeless box"));
                }
                else
                {
                    invalid = true;
                    model.validationMessages.Add("Contact Information:C3.Addresses: Please answer the question May we contact you at this address?");

                    //model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-Contact", 3, "CA-ADAP-Contact-3_sub",
                    //         "Please select 'No, my enrollment site will receive information on my behalf'"));

                }
            }
            //if (!sv.HasAnyResponse("C1_DaytimePhoneNumber"))
            //{
            //    invalid = true;
            //    model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-Contact", 5, "CA-ADAP-Contact-5_sub",
            //             "Please Enter Your Phone Number"));
            //}

            #endregion

            #region Health Coverage

            //If Response is Yes on Radiobutton on HC3

            if(sv.HasPositiveResponse("C1_ACAStateExchgEnroll"))
            {

                //C1_OtherInsuranceProvider
                //C1_OtherInsuranceMemberID2
                //C1_OtherInsuranceEffDate

                //if (!sv.HasAnyResponse("C1_OtherInsuranceProvider"))
                //{
                //    invalid = true;
                //    model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-Insurance", 3, "CA-ADAP-Insurance-3_sub",
                //       "Please Enter Health Insurance Plan Name"));
                //}

                if (!sv.HasAnyResponse("C1_OtherInsuranceMemberID2"))
                {
                    invalid = true;
                    model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-Insurance", 3, "CA-ADAP-Insurance-3_sub",
                       "Please Enter Insurance Member ID"));
                }

                if (!sv.HasAnyResponse("C1_OtherInsuranceEffDate"))
                {
                    invalid = true;
                    model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-Insurance", 3, "CA-ADAP-Insurance-3_sub",
                       "Please Enter Effective Start Date"));
                }

            
            
            }
            else if (!sv.HasPositiveResponse("C1_ACAStateExchgEnroll") && sv.HasPositiveResponse("C1_OtherInsuranceEnroll"))
            {
                if (sv.GetResponseInt("C1_OtherInsuranceEnroll") == 2)
                {
                    if(!sv.HasAnyResponse("C1_NoInsurnaceDescription"))
                    {
                        invalid = true;
                        model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-Insurance", 3, "CA-ADAP-Insurance-3_sub",
                       "Please Explain"));
                    }
                }
                else
                {
                    if (sv.GetResponseInt("C1_OtherInsuranceType") == -1)
                    {
                        invalid = true;
                        model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-Insurance", 3, "CA-ADAP-Insurance-3_sub",
                                 "Please select 'If yes, what health insurance coverage do you have?'"));
                    }


                    else if (sv.GetResponseInt("C1_OtherInsuranceType") == 1 || sv.GetResponseInt("C1_OtherInsuranceType") == 2 || sv.GetResponseInt("C1_OtherInsuranceType") == 3 || sv.GetResponseInt("C1_OtherInsuranceType") == 5 || sv.GetResponseInt("C1_OtherInsuranceType") == 7)
                    {

                        if (!sv.HasAnyResponse("C1_OtherInsuranceProvider"))
                        {
                            invalid = true;
                            model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-Insurance", 3, "CA-ADAP-Insurance-3_sub",
                                     "Please Enter Name of health Insurance Plan"));
                        }
                        if (!sv.HasAnyResponse("C1_OtherInsurancePlanID"))
                        {
                            invalid = true;
                            model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-Insurance", 3, "CA-ADAP-Insurance-3_sub",
                                     "Please Enter Policy number"));
                        }
                        if (!sv.HasAnyResponse("C1_OtherInsuranceMemberID2"))
                        {
                            invalid = true;
                            model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-Insurance", 3, "CA-ADAP-Insurance-3_sub",
                                     "Please Enter Insurance Member ID Number"));
                        }
                        if (!sv.HasAnyResponse("C1_OtherInsuranceEffDate"))
                        {
                            invalid = true;
                            model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-Insurance", 3, "CA-ADAP-Insurance-3_sub",
                                     "Please Enter Effective Start Date"));
                        }
                                       
                        //if (!sv.HasAnyResponse("C1_OtherInsuranceEndDate"))
                        //{
                        //    invalid = true;
                        //    model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-Insurance", 3, "CA-ADAP-Insurance-3_sub",
                        //             "Please Enter Effective End Date"));
                        //}
                    }
                     if (sv.GetResponseInt("C1_OtherInsuranceType") == 4)
                   {
                    if (!sv.HasAnyResponse("C1_OtherInsuranceEffDate"))
                    {
                        invalid = true;
                        model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-Insurance", 3, "CA-ADAP-Insurance-3_sub",
                                 "Please Enter Effective Date"));
                    }
                }
                       
                }
            }
            else if (sv.HasPositiveResponse("C1_MedicareEligible") && sv.HasPositiveResponse("C1_MedicarePartDEnroll"))
            {
                if (!sv.HasAnyResponse("C1_MedicarePartDEffDate"))
                {
                    invalid = true;
                    model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-Insurance", 2, "CA-ADAP-Insurance-2_sub",
                             "Please Enter Part D Eligibility Effective Date"));
                }
                //Commented required fields for now
                //if (!sv.HasAnyResponse("C1_MedicarePartDEnrollDate"))//Can be used if needed again for magellan
                //{
                //    invalid = true;
                //    model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-Insurance", 2, "CA-ADAP-Insurance-2_sub",
                //             "Please Enter Part D Enrollment Effective Date"));
                //}
                //if (!sv.HasAnyResponse("C1_MedicarePartDIdNumber"))
                //{
                //    invalid = true;
                //    model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-Insurance", 2, "CA-ADAP-Insurance-2_sub",
                //             "Please Enter What is your Medicare Health Insurance Claim (HIC) Number?"));
                //}
                //if (!sv.HasAnyResponse("C1_MedicarePartDPlanName"))
                //{
                //    invalid = true;
                //    model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-Insurance", 2, "CA-ADAP-Insurance-2_sub",
                //             "Please Enter What is the name of the Medicare Part D plan?"));
                //}

            }
            else if (sv.HasPositiveResponse("C1_MedicareEligible") && !sv.HasPositiveResponse("C1_MedicarePartDEnroll"))
            {
                invalid = true;
                model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-Insurance", 2, "CA-ADAP-Insurance-2_sub",
                         "Please check Medicare Part D (Prescription Drug Coverage) in order to get your medical benefits"));
            }
            else if (sv.GetResponseInt("C1_StateMedicaidEnrolled") == 0)
            {
                //if (!sv.HasAnyResponse("C1_StateMedicaidMemberId"))//Required field can be used if needed for magellan again
                //{
                //    invalid = true;
                //    model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-Insurance", 1, "CA-ADAP-Insurance-1_sub",
                //             "Please Enter your Medi-Cal Benefits Identification Card (BIC) Number"));
                //}
                
                if (!sv.HasAnyResponse("C1_StateMAidEffDate"))
                {
                    invalid = true;
                    model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-Insurance", 1, "CA-ADAP-Insurance-1_sub",
                             "Please Enter your Medi-Cal Effective Date"));
                }
            }
            else
            {
                invalid = true;
                model.validationMessages.Add("<p>Health Coverage: 'HC1: You must answer HC1'</p><p>Health Coverage:'HC2: You must answer HC2'</p><p>Health Coverage:'HC3: If you do not have health insurance through a Covered CA plan, then you must indicate if you are enrolled in a different health insurance plan or if you do not have any insurance.'</p>");
                //model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-Insurance", 1, "CA-ADAP-Insurance-1_sub",
                //         "Please select any one of the condition 'Medi-Cal Coverage/Medicare Coverage/Private Insurance Coverage' in Health Coverage"));
            }

            #endregion

            #region Insurance Assistance

            if (sv.GetResponseInt("C1_MemberRequestsHIPP3") == -1)
            {
                invalid = true;
                model.validationMessages.Add("Insurance Assistance: Please select one of the options from 'Would you like to receive assistance with your premium payments?'");
                //model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-Insurance", 1, "CA-ADAP-Insurance-1_sub",
                //         "Please select any one of the condition 'Medi-Cal Coverage/Medicare Coverage/Private Insurance Coverage' in Health Coverage"));
            }
            else if (sv.GetResponseInt("C1_MemberRequestsHIPP3") == 1)
            {
                if (sv.HasPositiveResponse("C1_MemberHIPPEnrollPgm1"))
                {
                    if (sv.GetResponseInt("C1_MemberMedicalPlanType") == -1)
                    {
                        invalid = true;
                        model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-InsuranceAssistance", 2, "CA-ADAP-InsAssist-2_sub",
                                 "Please select 'What type of coverage is this medical plan?"));

                    }
                    if (sv.GetResponseInt("C1_MemberMedicalPlanType") == 0)
                    {
                        if (sv.GetResponseInt("C1_CoveredCAMedicalPlanType") == -1)
                        {
                            invalid = true;
                            model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-InsuranceAssistance", 2, "CA-ADAP-InsAssist-2_sub",
                                     "Please select 'What Covered CA metal tier did you select?"));
                        }
                        if (!sv.HasAnyResponse("C1_CoveredCAAPTCAmount"))
                        {
                            invalid = true;
                            model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-InsuranceAssistance", 2, "CA-ADAP-InsAssist-2_sub",
                                     "Please Enter If eligible, you are required to take the maximum Advanced Premium Tax Credit (APTC) offered by Covered CA. Please enter the amount of APTC you received."));
                        }


                        if (!sv.HasAnyResponse("C1_CoveredCAMedicalPayee"))
                        {
                            invalid = true;
                            model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-InsuranceAssistance", 2, "CA-ADAP-InsAssist-2_sub",
                                     "Please Enter What is the name of your health insurance plan?"));
                        }
                        if (!sv.HasAnyResponse("C1_MemberMedicalPlanSubscriberID"))
                        {
                            invalid = true;
                            model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-InsuranceAssistance", 2, "CA-ADAP-InsAssist-2_sub",
                                     "Please Enter What is your Member or Subscriber ID?"));
                        }
                        if (!sv.HasAnyResponse("C1_CoveredCAPlanStartDate"))
                        {
                            invalid = true;
                            model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-InsuranceAssistance", 2, "CA-ADAP-InsAssist-2_sub",
                                     "Please Enter When did your medical plan start?"));
                        }
                        if (!sv.HasAnyResponse("C1_CoveredCAPremiumAfterAPTC"))
                        {
                            invalid = true;
                            model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-InsuranceAssistance", 2, "CA-ADAP-InsAssist-2_sub",
                                     "Please Enter What is your net premium amount?"));
                        }
                        if (sv.GetResponseInt("C1_CoveredCAPremiumDue") == -1)
                        {
                            invalid = true;
                            model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-InsuranceAssistance", 2, "CA-ADAP-InsAssist-2_sub",
                                     "Please select How often is your premium due?"));
                        }
                        if (!sv.HasAnyResponse("C1_CoveredCABillingDoc"))
                        {
                            invalid = true;
                            model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-InsuranceAssistance", 2, "CA-ADAP-InsAssist-2_sub",
                                     "Please Upload file in Click BROWSE to attach a copy of your most recent medical plan billing statement."));
                        }


                    }

                   
                    
                    //if (!sv.HasAnyResponse("C1_CoveredCAProjectedStartHIPPDate"))
                    //{
                    //    invalid = true;
                    //    model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-InsuranceAssistance", 2, "CA-ADAP-InsAssist-2_sub",
                    //             "Please Enter 'Premium payments begin on the month the complete application was submitted and approved. You will need to continue making payments until you are notified of your eligibility for premium payment assistance. Once approved, the payment start date will be listed here.'"));
                    //}

                    //private,CobrA,Cal-Cobra,other
                    else if (sv.GetResponseInt("C1_MemberMedicalPlanType") == 1 || sv.GetResponseInt("C1_MemberMedicalPlanType") == 2 || sv.GetResponseInt("C1_MemberMedicalPlanType") == 3 || sv.GetResponseInt("C1_MemberMedicalPlanType") == 4)
                    {


                        if (!sv.HasAnyResponse("C1_PrivateMedicalPayee"))
                        {
                            invalid = true;
                            model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-InsuranceAssistance", 2, "CA-ADAP-InsAssist-2_sub",
                                     "Please Enter What is the name of your health insurance plan?"));
                        }
                        if (!sv.HasAnyResponse("C1_MemberMedicalPlanSubscriberID"))
                        {
                            invalid = true;
                            model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-InsuranceAssistance", 2, "CA-ADAP-InsAssist-2_sub",
                                     "Please Enter What is your Member or Subscriber ID?"));
                        }
                        if (!sv.HasAnyResponse("C1_MemberMedicalPlanStartDate"))
                        {
                            invalid = true;
                            model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-InsuranceAssistance", 2, "CA-ADAP-InsAssist-2_sub",
                                     "Please Enter When did your medical plan start?"));
                        }
                        if (!sv.HasAnyResponse("C1_MemberMedicalNetPremium"))
                        {
                            invalid = true;
                            model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-InsuranceAssistance", 2, "CA-ADAP-InsAssist-2_sub",
                                     "Please Enter What is your net premium amount?"));
                        }
                        if (sv.GetResponseInt("C1_MemberMedicalPremiumFreqDue") == -1)
                        {
                            invalid = true;
                            model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-InsuranceAssistance", 2, "CA-ADAP-InsAssist-2_sub",
                                     "Please select How often is your premium due?"));
                        }
                        if (!sv.HasAnyResponse("C1_NonCoveredCABillingDoc"))
                        {
                            invalid = true;
                            model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-InsuranceAssistance", 2, "CA-ADAP-InsAssist-2_sub",
                                     "Please Upload file in Click BROWSE to attach a copy of your most recent medical plan billing statement."));
                        }


                    }

                    //Cobra
                    //Cal-CObra
                    //OTHER


                }
                if (sv.HasPositiveResponse("C1_MemberHIPPEnrollPgm2"))
                {
                    if (sv.GetResponseInt("C1_MemberDentalPlanIncluded") == -1)
                    {
                        invalid = true;
                        model.validationMessages.Add("Insurance Assistance: Please select 'You indicated you would also like dental premium payment assistance. Was this dental plan purchased separately or is it included with your medical?'");
                    }
                    else if (sv.GetResponseInt("C1_MemberDentalPlanIncluded") == 0)
                    {
                        if (!sv.HasAnyResponse("C1_DentalPayee"))
                        {
                            invalid = true;
                            model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-InsuranceAssistance", 3, "CA-ADAP-InsAssist-3_sub",
                                     "Please Enter What is the name of your health insurance plan?"));
                        }
                        if (!sv.HasAnyResponse("C1_MemberDentalPlanSubscriberID"))
                        {
                            invalid = true;
                            model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-InsuranceAssistance", 3, "CA-ADAP-InsAssist-3_sub",
                                     "Please Enter What is your Member or Subscriber ID?"));
                        }
                        if (!sv.HasAnyResponse("C1_DentalPlanStartDate"))
                        {
                            invalid = true;
                            model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-InsuranceAssistance", 3, "CA-ADAP-InsAssist-3_sub",
                                     "Please Enter When did your medical plan start?"));
                        }
                        if (!sv.HasAnyResponse("C1_DentalPremium"))
                        {
                            invalid = true;
                            model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-InsuranceAssistance", 3, "CA-ADAP-InsAssist-3_sub",
                                     "Please Enter What is your net premium amount?"));
                        }
                        if (sv.GetResponseInt("C1_MemberDentalPremiumFreqDue") == -1)
                        {
                            invalid = true;
                            model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-InsuranceAssistance", 3, "CA-ADAP-InsAssist-3_sub",
                                     "Please select How often is your premium due?"));
                        }
                        if (!sv.HasAnyResponse("C1_DentalBillingDoc"))
                        {
                            invalid = true;
                            model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-InsuranceAssistance", 3, "CA-ADAP-InsAssist-3_sub",
                                     "Please Upload file in Click BROWSE to and attach a copy of your most recent dental billing statement."));
                        }
                        //if (!sv.HasAnyResponse("C1_DentalProjectedStartHIPPDate"))
                        //{
                        //    invalid = true;
                        //    model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-InsuranceAssistance", 3, "CA-ADAP-InsAssist-3_sub",
                        //             "Please Enter 'Premium payments begin on the month the complete application was submitted and approved. You will need to continue making payments until you are notified of your eligibility for premium payment assistance. Once approved, your payments will start the same day as your medical plan premium payment assistance.'"));
                        //}
                    }
                }
                if (!sv.HasPositiveResponse("C1_MemberHIPPEnrollPgm1") && !sv.HasPositiveResponse("C1_MemberHIPPEnrollPgm2") && !sv.HasPositiveResponse("C1_MemberHIPPEnrollPgm3"))
                {
                    invalid = true;
                    model.validationMessages.Add("Insurance Assistance: Please select What type of health insurance premium plan assistance do you need?");
                }
            }
            else if (sv.GetResponseInt("C1_MemberRequestsHIPP3") == 2)
            {
                if (!sv.HasAnyResponse("C1_MemberMedicareDSubscriberID"))
                {
                    invalid = true;
                    model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-InsuranceAssistance", 6, "CA-ADAP-InsAssist-6_sub",
                             "Please Enter What is your Member or Subscriber ID?"));
                }
                if (!sv.HasAnyResponse("C1_MedicareDStartDate"))
                {
                    invalid = true;
                    model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-InsuranceAssistance", 6, "CA-ADAP-InsAssist-6_sub",
                             "Please Enter When did your Medicare Part D plan start?"));
                }
                if (!sv.HasAnyResponse("C1_MedicareDPremium"))
                {
                    invalid = true;
                    model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-InsuranceAssistance", 6, "CA-ADAP-InsAssist-6_sub",
                             "Please Enter What is your Medicare Part D premium amount excluding any fees"));
                }
                if (sv.GetResponseInt("C1_MedicareDPremiumFreqDue") == -1)
                {
                    invalid = true;
                    model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-InsuranceAssistance", 6, "CA-ADAP-InsAssist-6_sub",
                             "Please select How often is your Medicare Part D plan premium due?"));
                }
                if (!sv.HasAnyResponse("C1_MedicareDBillingDoc"))
                {
                    invalid = true;
                    model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-InsuranceAssistance", 6, "CA-ADAP-InsAssist-6_sub",
                             "Please Upload file If available, click BROWSE to attach the most recent copy of your Medicare Part D health insurance billing statement. Note: ADAP must send payments directly to Medicare Part D plans and will not pay for any fees (e.g. late fees, penalties)."));
                }
                //if (!sv.HasAnyResponse("C1_MedicareDProjectedStartHIPPDate"))
                //{
                //    invalid = true;
                //    model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-InsuranceAssistance", 6, "CA-ADAP-InsAssist-6_sub",
                //             "Please Enter 'Premium payments begin on the month the complete application was submitted and approved. You will need to continue making payments until you are notified of your eligibility for premium payment assistance. Once approved, your payments will start:'"));
                //}

                //if (!sv.HasAnyResponse("C1_MedicareDBillingDoc"))
                //{
                //    invalid = true;
                //    model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-InsuranceAssistance", 6, "CA-ADAP-InsAssist-6_sub",
                //             "Please Upload file in 'If available, click BROWSE to attach the most recent copy of your Medicare Part D health insurance billing statement. Note: ADAP must send payments directly to Medicare Part D plans and will not pay for any fees (e.g. late fees, penalties).'"));
                //}
                //if (!sv.HasAnyResponse("C1_MedicareDProjectedStartHIPPDate"))
                //{
                //    invalid = true;
                //    model.validationMessages.Add(valErrMsg(formsRepo, "CA-ADAP-InsuranceAssistance", 6, "CA-ADAP-InsAssist-6_sub",
                //             "Please Enter 'Premium payments begin on the month the complete application was submitted and approved. You will need to continue making payments until you are notified of your eligibility for premium payment assistance. Once approved, your payments will start:'"));
                //}

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

    }
}



