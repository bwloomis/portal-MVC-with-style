
using Data.Abstract;



namespace AJBoggs.Def.Domain
{
    /*
     * This class is used to process Forms (Meta Data) in DEF.
     * All Forms, Part, Sections, Items 
     * 
     * It should be used by Controllers and WebServices for getting meta data from the Forms Repository.
     * 
     */
    public partial class MetaData
    {
 
        private IFormsRepository formsRepo;

        public MetaData(IFormsRepository fr)
        {
            formsRepo = fr;
        }


    }
}
