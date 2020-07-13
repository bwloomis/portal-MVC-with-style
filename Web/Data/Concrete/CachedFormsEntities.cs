using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using Assmnts;
using Data.Abstract;
using NLog;

namespace Data.Concrete
{
	public class CachedFormsEntities : ICachedFormsEntities
	{
		public const string SET_PROPERTY_LOG_MESSAGE = "An attempt was made to set a property on a cached entity. "
		+ "Setting properties of cached entities is forbidden. "
		+ "Consider creating a new entity with Constructor(CachedType) and/or explicitly querying the repository "
		+ "for an attached version of the entity.";

		public const string SET_PROPERTY_EXCEPTION_MESSAGE = "Setting properties of cached entities is forbidden. "
		+ "Consider creating a new entity with Constructor(CachedType) and/or explicitly querying the repository "
		+ "for an attached version of the entity.";

		public const string NAVIGATION_PROPERTY_ACCESS_LOG_MESSAGE = "An attempt was made to access a navigation property. "
		+ "Access to navigation properties of cached entities is forbidden. "
		+ "Consider creating a new entity with Constructor(CachedType) and/or explicitly querying the repository "
		+ "for the navigation property.";

		public const string NAVIGATION_PROPERTY_ACCESS_EXCEPTION_MESSAGE = "Access to navigation properties of cached entities is forbidden. "
			+ "Consider creating a new entity with Constructor(CachedType) and/or explicitly querying the repository "
			+ "for the navigation property.";

		private ILogger mLogger = LogManager.GetCurrentClassLogger();

		private readonly IList<CachedAttachType> mDef_AttachType;
		private readonly IList<CachedBaseType> mDef_BaseTypes;
		private readonly IList<CachedFormPart> mDef_FormParts;
		private readonly IList<CachedForm> mDef_Forms;
		private readonly IList<CachedFormText> mDef_FormText;
		private readonly IList<CachedItem> mDef_Items;
		private readonly IList<CachedItemsEnt> mDef_ItemsEnt;
		private readonly IList<CachedItemText> mDef_ItemText;
		private readonly IList<CachedItemVariable> mDef_ItemVariables;
		private readonly IList<CachedLanguage> mDef_Languages;
		private readonly IList<CachedLookupDetail> mDef_LookupDetail;
		private readonly IList<CachedLookupMaster> mDef_LookupMaster;
		private readonly IList<CachedLookupText> mDef_LookupText;
		private readonly IList<CachedPart> mDef_Parts;
		private readonly IList<CachedPartSection> mDef_PartSections;
		private readonly IList<CachedPartSectionsEnt> mDef_PartSectionsEnt;
		private readonly IList<CachedPartText> mDef_PartText;
		private readonly IList<CachedSectionItem> mDef_SectionItems;
		private readonly IList<CachedSectionItemsEnt> mDef_SectionItemsEnt;
		private readonly IList<CachedSection> mDef_Sections;
		private readonly IList<CachedSectionText> mDef_SectionText;
		private readonly IList<CachedSubSection> mDef_SubSections;

		public CachedFormsEntities()
		{
			using (var formsEntities = DataContext.GetDbContext()) {
				formsEntities.Configuration.LazyLoadingEnabled = false;

				mDef_AttachType = formsEntities.def_AttachType
						.AsNoTracking()
						.ToList() //We have to do this to be able to use a non-default constructor
						.Select(x => new CachedAttachType(x))
						.ToList();

				mDef_BaseTypes = formsEntities.def_BaseTypes
					.AsNoTracking()
					.ToList()
					.Select(x => new CachedBaseType(x))
					.ToList();

				mDef_FormParts = formsEntities.def_FormParts
					.AsNoTracking()
					.ToList()
					.Select(x => new CachedFormPart(x))
					.ToList();

				mDef_Forms = formsEntities.def_Forms
					.AsNoTracking()
					.ToList()
					.Select(x => new CachedForm(x))
					.ToList();

				mDef_FormText = formsEntities.def_FormText
					.AsNoTracking()
					.ToList()
					.Select(x => new CachedFormText(x))
					.ToList();

				mDef_Items = formsEntities.def_Items
					.AsNoTracking()
					.ToList()
					.Select(x => new CachedItem(x))
					.ToList();

				mDef_ItemsEnt = formsEntities.def_ItemsEnt
					.AsNoTracking()
					.ToList()
					.Select(x => new CachedItemsEnt(x))
					.ToList();

				mDef_ItemText = formsEntities.def_ItemText
					.AsNoTracking()
					.ToList()
					.Select(x => new CachedItemText(x))
					.ToList();

				mDef_ItemVariables = formsEntities.def_ItemVariables
					.AsNoTracking()
					.ToList()
					.Select(x => new CachedItemVariable(x))
					.ToList();

				mDef_Languages = formsEntities.def_Languages
					.AsNoTracking()
					.ToList()
					.Select(x => new CachedLanguage(x))
					.ToList();

				mDef_LookupDetail = formsEntities.def_LookupDetail
					.AsNoTracking()
					.ToList()
					.Select(x => new CachedLookupDetail(x))
					.ToList();

				mDef_LookupMaster = formsEntities.def_LookupMaster
					.AsNoTracking()
					.ToList()
					.Select(x => new CachedLookupMaster(x))
					.ToList();

				mDef_LookupText = formsEntities.def_LookupText
					.AsNoTracking()
					.ToList()
					.Select(x => new CachedLookupText(x))
					.ToList();

				mDef_Parts = formsEntities.def_Parts
					.AsNoTracking()
					.ToList()
					.Select(x => new CachedPart(x))
					.ToList();

				mDef_PartSections = formsEntities.def_PartSections
					.AsNoTracking()
					.ToList()
					.Select(x => new CachedPartSection(x))
					.ToList();

				mDef_PartSectionsEnt = formsEntities.def_PartSectionsEnt
					.AsNoTracking()
					.ToList()
					.Select(x => new CachedPartSectionsEnt(x))
					.ToList();

				mDef_PartText = formsEntities.def_PartText
					.AsNoTracking()
					.ToList()
					.Select(x => new CachedPartText(x))
					.ToList();

				mDef_SectionItems = formsEntities.def_SectionItems
					.AsNoTracking()
					.ToList()
					.Select(x => new CachedSectionItem(x))
					.ToList();

				mDef_SectionItemsEnt = formsEntities.def_SectionItemsEnt
					.AsNoTracking()
					.ToList()
					.Select(x => new CachedSectionItemsEnt(x))
					.ToList();

				mDef_Sections = formsEntities.def_Sections
					.AsNoTracking()
					.ToList()
					.Select(x => new CachedSection(x))
					.ToList();

				mDef_SectionText = formsEntities.def_SectionText
					.AsNoTracking()
					.ToList()
					.Select(x => new CachedSectionText(x))
					.ToList();

				mDef_SubSections = formsEntities.def_SubSections
					.AsNoTracking()
					.ToList()
					.Select(x => new CachedSubSection(x))
					.ToList();
			}
		}
		public IList<CachedAttachType> def_AttachType {
			get { return mDef_AttachType; }
		}

		public IList<CachedBaseType> def_BaseTypes {
			get { return mDef_BaseTypes; }
		}

		public IList<CachedFormPart> def_FormParts {
			get { return mDef_FormParts; }
		}

		public IList<CachedForm> def_Forms {
			get { return mDef_Forms; }
		}

		public IList<CachedFormText> def_FormText {
			get { return mDef_FormText; }
		}

		public IList<CachedItem> def_Items {
			get { return mDef_Items; }
		}

		public IList<CachedItemsEnt> def_ItemsEnt {
			get { return mDef_ItemsEnt; }
		}

		public IList<CachedItemText> def_ItemText {
			get { return mDef_ItemText; }
		}

		public IList<CachedItemVariable> def_ItemVariables {
			get { return mDef_ItemVariables; }
		}

		public IList<CachedLanguage> def_Languages {
			get { return mDef_Languages; }
		}
		public IList<CachedLookupDetail> def_LookupDetail {
			get { return mDef_LookupDetail; }
		}

		public IList<CachedLookupMaster> def_LookupMaster {
			get { return mDef_LookupMaster; }
		}

		public IList<CachedLookupText> def_LookupText {
			get { return mDef_LookupText; }
		}

		public IList<CachedPart> def_Parts {
			get { return mDef_Parts; }
		}

		public IList<CachedPartSection> def_PartSections {
			get { return mDef_PartSections; }
		}

		public IList<CachedPartSectionsEnt> def_PartSectionsEnt {
			get { return mDef_PartSectionsEnt; }
		}

		public IList<CachedPartText> def_PartText {
			get { return mDef_PartText; }
		}

		public IList<CachedSectionItem> def_SectionItems {
			get { return mDef_SectionItems; }
		}

		public IList<CachedSectionItemsEnt> def_SectionItemsEnt {
			get { return mDef_SectionItemsEnt; }
		}

		public IList<CachedSection> def_Sections {
			get { return mDef_Sections; }
		}

		public IList<CachedSectionText> def_SectionText {
			get { return mDef_SectionText; }
		}

		public IList<CachedSubSection> def_SubSections {
			get { return mDef_SubSections; }
		}
	}
}