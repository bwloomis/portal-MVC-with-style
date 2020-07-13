using Data.Abstract;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using UAS.Business;

namespace Assmnts.Business
{
    public static class Forms
    {
        public static Dictionary<int, string> GetFormsDictionaryThrow(IFormsRepository formsRepo)
        {
            Dictionary<int, string> forms = null;
            try
            {
                List<string> formIdentifiers = UAS_Business_Functions.GetForms();

                forms = (from i in formsRepo.GetFormsByIdentifiers(formIdentifiers)
                         select i).ToDictionary(i => i.formId, i => i.identifier);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception: GetFormsDictionary: No forms configured for use.");
                throw new Exception("You are not configured to use any SIS forms.");
            }

            return forms;
        }

        public static Dictionary<int, string> GetFormsDictionary(IFormsRepository formsRepo)
        {
            Dictionary<int, string> forms = null;

            try
            {
                forms = GetFormsDictionaryThrow(formsRepo);
            }
            catch (Exception ex)
            {
                forms = new Dictionary<int, string>();
            }

            return forms;
        }

        
    }
}