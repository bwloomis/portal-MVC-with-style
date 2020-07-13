using Assmnts.Infrastructure;

using Data.Abstract;

using System;
using System.Collections.Generic;

namespace Assmnts.Business.Workflow
{
    public partial class AdapApprovals
    {
        /// <summary>
        /// Get List of all Application Status
        /// </summary>
        /// <param name="formsRepo">FormsRepository interface</param>
        /// <returns>Dic</returns>
        public static Dictionary<int, string> GetAdapStatusList(IFormsRepository formsRepo, int enterpriseId, int statusMasterId=1)
        {
            Dictionary<int, string> result = new Dictionary<int, string>();

            // RRB 3/24/16
            // Need to add GetStatusMaster(ApplicationId, FormId)
            List<def_StatusDetail> allSd = formsRepo.GetStatusDetails(statusMasterId);

            foreach (def_StatusDetail sd in allSd)
            {
                def_StatusText st = formsRepo.GetStatusText(sd.statusDetailId, enterpriseId, 1);
                int sortOrder = sd.sortOrder.Value;
                string text = st.displayText;
                result.Add(sortOrder, text);
            }

            return result;

        }

        /// <summary>
        /// Get the list of possible Application Status based on the method parameters
        ///         rolePermission [Applicant 0, Staff 1, Mgt 2])
        /// </summary>
        /// <param name="formsRepo">FormsRepository interface</param>
        /// <param name="currentStatus">current application status code</param>
        /// <param name="rolePermission">Role Permission of User</param>
        /// <returns></returns>
        public static Dictionary<int, string> PossibleWorkflow(IFormsRepository formsRepo, int statusMasterId, int currentStatus, int rolePermission)
        {
            Dictionary<int, string> possibleStatus = new Dictionary<int, string>();

            string currentStatusIdentifier = formsRepo.GetStatusDetailBySortOrder(statusMasterId, currentStatus).identifier;
            int enterpriseId = SessionHelper.LoginStatus.EnterpriseID;

            string[] nextWorkflowStatuses = GetNextWorkflowStatusIdentifiers(currentStatusIdentifier, statusMasterId);
            foreach (string statusIdentifier in nextWorkflowStatuses)
            {
                def_StatusDetail sd = formsRepo.GetStatusDetailByMasterIdentifier(statusMasterId, statusIdentifier);
                def_StatusText st = formsRepo.GetStatusText(sd.statusDetailId, enterpriseId, 1);
                int sortOrder = sd.sortOrder.Value;
                string text = st.displayText;
                possibleStatus.Add(sortOrder, text);
            }

            return possibleStatus;
        }

        public static string[] GetNextWorkflowStatusIdentifiers(string currentStatusIdentifier, int statusMasterId)
        {
            if (currentStatusIdentifier.StartsWith("Approved - "))
            {
                var statuses = new List<String> { "Approved - Medication Assistance Only", "Approved - Medication Assistance but OA-HIPP needs more information",
                        "Approved - Medication Assistance but OA-HIPP still needs review", "Approved - Medication Assistance but OA-HIPP denied", 
                        "Approved - Medication Assistance with OA-HIPP", "Approved - Medication Assistance but Medicare Part D needs more information",
                        "Approved - Medication Assistance but Medicare Part D still needs review", "Approved - Medication Assistance but Medicare Part D denied",
                        "Approved - Medication Assistance with Medicare Part D"};
                statuses.Remove(currentStatusIdentifier);
                return statuses.ToArray();
            }

            switch (currentStatusIdentifier)
            {
                case "IN_PROCESS":          return new string[] { "NEEDS_REVIEW", "CANCELLED" };

                case "NEEDS_REVIEW":
                    if (statusMasterId == 4)
                    {
                        return new string[] { "NEEDS_INFORMATION", "DENIED", "Approved - Medication Assistance Only", "Approved - Medication Assistance but OA-HIPP needs more information",
                        "Approved - Medication Assistance but OA-HIPP still needs review", "Approved - Medication Assistance but OA-HIPP denied", 
                        "Approved - Medication Assistance with OA-HIPP", "Approved - Medication Assistance but Medicare Part D needs more information",
                        "Approved - Medication Assistance but Medicare Part D still needs review", "Approved - Medication Assistance but Medicare Part D denied",
                        "Approved - Medication Assistance with Medicare Part D"};
                    }
                    else if (statusMasterId == 5)
                    {
                        return new string[] { "DENIED", "APPROVED" };
                    }
                    else
                    {
                        return new string[] { "NEEDS_INFORMATION", "DENIED", "APPROVED" };
                    }

                case "NEEDS_INFORMATION":   return new string[] { "NEEDS_REVIEW" };

                case "DENIED":              return new string[] { "IN_PROCESS" };

                case "APPROVED":            return new string[] { };

                case "CANCELLED":
                    if (statusMasterId == 6)
                    {
                        return new string[] { "IN PROGRESS" };
                    }
                    else
                    {
                        return new string[] { "IN_PROCESS" };
                    }

                case "IN PROGRESS": return new string[] { "APPROVED", "CANCELLED" };

                default: return new string[] { };
            }
        }

    }
}