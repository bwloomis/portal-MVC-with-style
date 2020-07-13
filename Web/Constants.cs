using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Assmnts
{
	public static class Constants
	{
		public const string ATTACHMENT_DIRECTORY_SETTING = "AttachmentDir";

        public const int GROUP_DESCRIPTION_MAX_LENGTH_FOR_DROP_DOWN_LIST_DISPLAY = 50;

        public static class CAADAP
		{
			public const int CA_ADAP_DASHBOARD_FORM_ID = 18;
			public const string IDENTIFIER_SUFFIX = "_item";
			public const int BASE_TYPE_BOOLEAN = 1;
			public const string ITEMS_TO_PREPOPULATE_FILEPATH = "~/App_Data/adapCaItemsToPrepopulate.json";
			public const string PREPOPULATE_SOURCE_PREVIOUS_APPLICATION = "PREVIOUS_APPLICATION";
			public const string PREPOPULATE_SOURCE_DASHBOARD = "DASHBOARD";
			public const string PREPOPULATE_SOURCE_UAS = "UAS";
            public const int ENROLLMENT_SITES_GROUPTYPE_ID = 193;
        }

		public static class Data
		{
			public static class FormsRepository
			{
				public const int SP_GETITEMLABELSRESPONSES_ITEMIDSCSV_MAX_LENGTH = 4000;
			}
		}
	}
}