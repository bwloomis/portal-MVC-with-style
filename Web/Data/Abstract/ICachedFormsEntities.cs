using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using Assmnts;
using Data.Concrete;

namespace Data.Abstract
{
    public interface ICachedFormsEntities
    {
        IList<CachedAttachType> def_AttachType { get; }
        IList<CachedBaseType> def_BaseTypes { get; }
        IList<CachedFormPart> def_FormParts { get; }
        IList<CachedForm> def_Forms { get; }
        IList<CachedFormText> def_FormText { get; }
        IList<CachedItem> def_Items { get; }
        IList<CachedItemsEnt> def_ItemsEnt { get; }
        IList<CachedItemText> def_ItemText { get; }
        IList<CachedItemVariable> def_ItemVariables { get; }
        IList<CachedLanguage> def_Languages { get; }
        IList<CachedLookupDetail> def_LookupDetail { get; }
        IList<CachedLookupMaster> def_LookupMaster { get; }
        IList<CachedLookupText> def_LookupText { get; }
        IList<CachedPart> def_Parts { get; }
        IList<CachedPartSection> def_PartSections { get; }
        IList<CachedPartSectionsEnt> def_PartSectionsEnt { get; }
        IList<CachedPartText> def_PartText { get; }
        IList<CachedSectionItem> def_SectionItems { get; }
        IList<CachedSectionItemsEnt> def_SectionItemsEnt { get; }
        IList<CachedSection> def_Sections { get; }
        IList<CachedSectionText> def_SectionText { get; }
        IList<CachedSubSection> def_SubSections { get; }
    }
}