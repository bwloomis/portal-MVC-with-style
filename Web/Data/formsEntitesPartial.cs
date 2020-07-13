using System;

namespace Assmnts
{
    using System.Data.Entity;
    public partial class formsEntities : DbContext
    {
        public formsEntities(string connName)
            : base("name=" + connName)
        {
        }
    }
}