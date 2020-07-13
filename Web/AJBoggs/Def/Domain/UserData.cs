
using Data.Abstract;



namespace AJBoggs.Def.Domain
{
    /*
     * This class is used to process FormResults (User Data) in DEF.
     * All FormResults (including ItemResults and ResponseVariables) link back to Forms meta data
     * 
     * It should be used by Controllers and WebServices for saving data to the Forms Repository.
     * 
     */
    public partial class UserData
    {
 
        private IFormsRepository formsRepo;

        public UserData(IFormsRepository fr)
        {
            formsRepo = fr;
        }


    }
}
