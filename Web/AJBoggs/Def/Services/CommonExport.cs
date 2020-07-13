using Assmnts;
using Data.Concrete;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Web;

namespace AJBoggs.Def.Services
{
    public enum FormResultExportTagName
    {
        formResultId,
        recipientId,
        identifier,
        formId,
        group,
        enterprise,
        assigned,
        statusId,
        dateUpdated,
        statusChangeDate,
        deleted,
        locked,
        archived,
        reviewStatus,
        lastModifiedByUserId,
        reviewStatusText,
        statusText,
        groupName,
        assignedLoginId,
        lastModifiedByLoginId,
        enterpriseName,
    }

    public class ValuePair
    {
        public string identifier { get; set; }
        public string rspValue { get; set; }

        public ValuePair(string id, string val)
        {
            identifier = id;
            rspValue = val;
        }
    }
    
    public static class CommonExport
    {       
        
        
        /// <summary>
        /// Returns all response data for an assessment, in ValuePairs
        /// 
        /// Uses direct SQL calls instead of EntityFramework to speed it up.
        /// </summary>
        /// <param name="formResultId">The form result ID to get data from</param>
        /// <returns>A list of value pairs. identifier = the item variable identifier, value = rspValue from the response variable</returns>
        public static List<ValuePair> GetDataByFormResultId(int formResultId)
        {
            List< ValuePair > result = new List<ValuePair>();

            using (formsEntities tempDB = DataContext.GetDbContext())
            {
                IQueryable qry = (from ir in tempDB.def_ItemResults
                                  where (ir.formResultId == formResultId)
                                  join rv in tempDB.def_ResponseVariables on ir.itemResultId equals rv.itemResultId
                                  join iv in tempDB.def_ItemVariables on rv.itemVariableId equals iv.itemVariableId
                                  select new { iv.identifier, rv.rspValue });
                foreach (dynamic elem in qry)
                {
                    result.Add(new ValuePair(elem.identifier, elem.rspValue));
                }
                //IQueryable qry = (from rv in tempDB.def_ResponseVariables
                //                  join ir in tempDB.def_ItemResults on rv.itemResultId equals ir.itemResultId
                //                  join iv in tempDB.def_ItemVariables on rv.itemVariableId equals iv.itemVariableId
                //                  where (ir.formResultId == formResultId)
                //                  select new { iv.identifier, rv.rspValue });
                //foreach (dynamic elem in qry)
                //{
                //    result.Add(new ValuePair(elem.identifier, elem.rspValue));
                //}
            }

            return result;
        }

        /// <summary>
        /// Recursive function to get all of the sub sections of a section.
        /// </summary>
        /// <param name="sectionId">The section to get subsections of</param>
        /// <returns>A list of section IDs for the subsections</returns>
        public static void GetSubSectionsRecursive(int sectionId, ref List<int> data)
        {
            using (formsEntities context = new formsEntities())
            {
                try
                {
                    StringBuilder qry = new StringBuilder("SELECT ss.sectionId  FROM def_SectionItems si");
                    qry.Append(" JOIN def_SubSections ss ON si.subSectionId = ss.subSectionId");
                    qry.Append(" WHERE si.sectionId = " + sectionId + " ORDER BY si.[order]");

                    using (SqlConnection sqlConn = new SqlConnection(context.Database.Connection.ConnectionString))
                    {
                        sqlConn.Open();
                        using (SqlCommand command = new SqlCommand(qry.ToString(), sqlConn))
                        {
                            command.CommandType = System.Data.CommandType.Text;
                            DataTable dt = new DataTable();
                            dt.Load(command.ExecuteReader());

                            data = dt.Rows.OfType<DataRow>().Select(dr => dr.Field<int>("sectionId")).ToList();
                        }
                    }

                }
                catch (Exception ex)
                {
                    return;
                }

                List<int> sectionIds = new List<int>(data);
                if (sectionIds.Count() > 0)
                {
                    foreach (int sId in sectionIds)
                    {
                        List<int> toCombine = new List<int>();

                        GetSubSectionsRecursive(sId, ref toCombine);

                        data.AddRange(toCombine);
                    }
                }

            }
        }

        /// <summary>
        /// Calls the recursive subsection obtaining function
        /// </summary>
        /// <param name="sectionId">The section to get subsections of</param>
        /// <returns>List of section IDs for the subsections</returns>
        public static List<int> GetSubSections(int sectionId)
        {
            List<int> subSectionIds = new List<int>();

            GetSubSectionsRecursive(sectionId, ref subSectionIds);

            return subSectionIds;

        }

        /// <summary>
        /// Gets data from the def_FormResults record for a form result
        /// </summary>
        /// <param name="formResult">The form result to get data from</param>
        /// <param name="form">The form result's form</param>
        /// <returns>ValuePairs containing identifier = label for a form result entry, value = the form result entry (string)</returns>
        public static List<ValuePair> GetFormResultValues(def_FormResults formResult, def_Forms form)
        {
            List<ValuePair> values = new List<ValuePair>();

            //number
            ValuePair valuePair = new ValuePair( FormResultExportTagName.recipientId.ToString(), formResult.subject.ToString());
            values.Add(valuePair);

            //number
            valuePair = new ValuePair(FormResultExportTagName.formResultId.ToString(), formResult.formResultId.ToString());
            values.Add(valuePair);

            //^ SisId in csv second row

            //text
            valuePair = new ValuePair(FormResultExportTagName.identifier.ToString(), form.identifier);
            values.Add(valuePair);

            //add formId (number)
            valuePair = new ValuePair(FormResultExportTagName.formId.ToString(), form.formId.ToString());
            values.Add(valuePair);

            //number
            valuePair = new ValuePair(FormResultExportTagName.group.ToString(), formResult.GroupID.ToString());
            values.Add(valuePair);

            //number
            valuePair = new ValuePair(FormResultExportTagName.enterprise.ToString(), formResult.EnterpriseID.ToString());
            values.Add(valuePair);

            ////number
            //valuePair = new ValuePair(interviewerId, formResult.interviewer.ToString());
            //values.Add(valuePair);

            //number
            valuePair = new ValuePair(FormResultExportTagName.assigned.ToString(), formResult.assigned.ToString());
            values.Add(valuePair);

            //number
            valuePair = new ValuePair(FormResultExportTagName.statusId.ToString(), formResult.formStatus.ToString());
            values.Add(valuePair);

            //text
            valuePair = new ValuePair(FormResultExportTagName.dateUpdated.ToString(), formResult.dateUpdated.ToString());
            values.Add(valuePair);

            //text
            valuePair = new ValuePair(FormResultExportTagName.statusChangeDate.ToString(), formResult.statusChangeDate.ToString());
            values.Add(valuePair);

            valuePair = new ValuePair(FormResultExportTagName.deleted.ToString(), formResult.deleted.ToString());
            values.Add(valuePair);

            valuePair = new ValuePair(FormResultExportTagName.locked.ToString(), formResult.locked.ToString());
            values.Add(valuePair);

            valuePair = new ValuePair(FormResultExportTagName.archived.ToString(), formResult.archived.ToString());
            values.Add(valuePair);

            //number
            valuePair = new ValuePair(FormResultExportTagName.reviewStatus.ToString(), formResult.reviewStatus.ToString());
            values.Add(valuePair);

            //number
            valuePair = new ValuePair(FormResultExportTagName.lastModifiedByUserId.ToString(), formResult.LastModifiedByUserId.ToString());
            values.Add(valuePair);          
            
            //pull info that comes from other def tables
            using (formsEntities def = DataContext.GetDbContext())
            {
                try
                {
                    int statusMasterId = def.def_StatusMaster.Where(sm => sm.formId == 1 && sm.ApplicationId == 1).Select(sm => sm.statusMasterId).First();
                    int statusDetailId = def.def_StatusDetail.Where(sd => sd.statusMasterId == statusMasterId && sd.sortOrder == formResult.reviewStatus).Select(sd => sd.statusDetailId).First();
                    string reviewStatusText = def.def_StatusText.Where(st => st.statusDetailId == statusDetailId).Select(st => st.displayText).First();
                    valuePair = new ValuePair(FormResultExportTagName.reviewStatusText.ToString(), reviewStatusText);
                    values.Add(valuePair);
                }
                catch (Exception e) { Debug.WriteLine(e); }

                try
                {
                    valuePair = new ValuePair(FormResultExportTagName.statusText.ToString(), 
                        ((WebService.WSConstants.FR_formStatus)(formResult.formStatus)).ToString() );
                    values.Add(valuePair);
                }
                catch (Exception e) { Debug.WriteLine(e); }
            }

            //pull info that comes from uas tables
            using (UASEntities uas = DataContext.getUasDbContext() )
            {
                try
                {
                    valuePair = new ValuePair(FormResultExportTagName.groupName.ToString(), 
                        uas.uas_Group
                        .Where(g => g.GroupID == formResult.GroupID)
                        .Select(g => g.GroupName).First());
                    values.Add(valuePair);
                }
                catch (Exception e) { Debug.WriteLine(e); }

                try
                {
                    valuePair = new ValuePair(FormResultExportTagName.assignedLoginId.ToString(), 
                        uas.uas_User
                        .Where(u => u.UserID == formResult.assigned)
                        .Select(u => u.LoginID).First());
                    values.Add(valuePair);
                }
                catch (Exception e) { Debug.WriteLine(e); }

                try
                {
                    valuePair = new ValuePair(FormResultExportTagName.lastModifiedByLoginId.ToString(), 
                        uas.uas_User
                        .Where(u => u.UserID == formResult.LastModifiedByUserId)
                        .Select(u => u.LoginID).First());
                    values.Add(valuePair);
                }
                catch (Exception e) { Debug.WriteLine(e); }

                try
                {
                    valuePair = new ValuePair(FormResultExportTagName.enterpriseName.ToString(), 
                        uas.uas_Enterprise
                        .Where(e => e.EnterpriseID == formResult.EnterpriseID)
                        .Select(e => e.EnterpriseName).First());
                    values.Add(valuePair);
                }
                catch (Exception e) { Debug.WriteLine(e); }
            }

            return values;
        }
    }
}