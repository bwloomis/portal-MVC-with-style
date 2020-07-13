using Assmnts.Infrastructure;

using Data.Concrete;

using System;
using System.Configuration;
using System.Data.Entity;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Mvc;
using System.Collections.Generic;

namespace Assmnts.Controllers
{
    [RedirectingAction]
    public class FormTransferController : Controller
    {
        [HttpGet]
        public FileStreamResult ExportFormJSON()
        {
            //use session variables to pick a FormResults object from the database
            string formId = Request["formId"] as string;
            Debug.WriteLine("* * *  GetFormResultsJSON formId: " + formId);
            
            int iFormId = Convert.ToInt32(formId);

            formsEntities db = DataContext.GetDbContext();

           db.Configuration.LazyLoadingEnabled = false;

           // def_Forms form = db.def_Forms
           //     .Include(fr => fr.def_FormParts
           //         .Select(fp => fp.def_Parts)
           //         .Select(pa => pa.def_PartSections
           //             .Select(p => p.def_Sections)
           //             .Select(s => s.def_SectionItems
           //             .Select(si => si.def_SubSections))))
           //     .Include(fr => fr.def_FormParts
           //         .Select(fp => fp.def_Parts)
           //         .Select(pa => pa.def_PartSections
           //             .Select(p => p.def_Sections)
           //             .Select(s => s.def_SectionItems
           //                 .Select(si => si.def_Items)
           //                 .Select(i => i.def_ItemVariables))))
           //     .Single(f => f.formId == iFormId);





            def_Forms form = db.def_Forms.Where(f => f.formId == iFormId).FirstOrDefault();
            List<def_FormParts> formParts = db.def_FormParts.Where(fp => fp.formId == iFormId).ToList();
            List<int> partIds = formParts.Select(fp => fp.partId).ToList();
            List<def_Parts> parts = db.def_Parts.Where(p => partIds.Contains(p.partId)).ToList();
            partIds = parts.Select(p => p.partId).ToList();
            List<def_PartSections> partSections = db.def_PartSections.Where(ps => partIds.Contains(ps.partId)).ToList();
            List<int> sectionIds = partSections.Select(ps => ps.sectionId).ToList();
            List<def_Sections> sections = db.def_Sections.Where(s => sectionIds.Contains(s.sectionId)).ToList();
            
            List<def_SectionItems> sectionItems = new List<def_SectionItems>();
            List<def_SubSections> subSections = new List<def_SubSections>();
            List<def_Items> items = new List<def_Items>();
            List<def_ItemVariables> itemVariables = new List<def_ItemVariables>();

            foreach (def_Sections section in sections) {
                GetSectionItemsAndSubSectionsRecursive(section.sectionId, sectionItems, subSections);
            }

            foreach (def_SectionItems si in sectionItems)
            {
                if (si.subSectionId == null)
                {
                    def_Items item = db.def_Items.Where(i => i.itemId == si.itemId).FirstOrDefault();

                    items.Add(item);

                    List<def_ItemVariables> newItemVariables = db.def_ItemVariables.Where(iv => iv.itemId == item.itemId).ToList();

                    itemVariables.AddRange(newItemVariables);
                }
            }


            string jsonString = "{\"data\":[" + fastJSON.JSON.ToJSON(form);
            jsonString += "," + fastJSON.JSON.ToJSON(formParts);
            jsonString += "," + fastJSON.JSON.ToJSON(parts);
            jsonString += "," + fastJSON.JSON.ToJSON(partSections);
            jsonString += "," + fastJSON.JSON.ToJSON(sections);
            jsonString += "," + fastJSON.JSON.ToJSON(sectionItems);
            jsonString += "," + fastJSON.JSON.ToJSON(subSections);
            jsonString += "," + fastJSON.JSON.ToJSON(items);
            jsonString += "," + fastJSON.JSON.ToJSON(itemVariables);
            jsonString += "]}";
            
            MemoryStream stream = new MemoryStream();
            
            //write json string to file, then stream it out
            //DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(FormResultJSON));
            
            StreamWriter streamWriter = new StreamWriter(stream);

            streamWriter.Write(jsonString);
            //streamWriter.Close();
            //stream.Close();

            FileStreamResult fsr = null;

            streamWriter.Flush();
            stream.Position = 0;
                        
            try
            {
                fsr = new FileStreamResult(stream, "application/json") { FileDownloadName = form.identifier + " " + DateTime.Now + ".json"};
            }
            catch (Exception exp)
            {
                Debug.WriteLine("File write exception: " + exp.Message);
                string errMsg = "File write exception: " + exp.Message;
                return ProcessError(errMsg);
            }

            
            return fsr;
        }


        private void GetSectionItemsAndSubSectionsRecursive(int sectionId, List<def_SectionItems> sectionItems, List<def_SubSections> subSections)
        {
            formsEntities db = DataContext.GetDbContext();
    
            db.Configuration.LazyLoadingEnabled = false;

            List<def_SectionItems> newSectionItems = db.def_SectionItems.Where(si => si.sectionId == sectionId).ToList();

            foreach (def_SectionItems si in newSectionItems)
            {
                sectionItems.Add(si);
                if (si.subSectionId != null)
                {
                    def_SubSections subSection = db.def_SubSections.Where(ss => ss.subSectionId == si.subSectionId).FirstOrDefault();

                    subSections.Add(subSection);

                    GetSectionItemsAndSubSectionsRecursive(subSection.sectionId, sectionItems, subSections);
                }
            }
        }
        
        private FileStreamResult ProcessError(String errMsg)
        {
            MemoryStream ms = new MemoryStream();
            FileStreamResult fsr = null;

            ms.Write(Encoding.ASCII.GetBytes(errMsg), 0, errMsg.Length);
            ms.Position = 0;
            fsr = new FileStreamResult(ms, "text/plain");
            return fsr;
        }
    }
}