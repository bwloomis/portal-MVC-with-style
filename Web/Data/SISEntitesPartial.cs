using System;

namespace Assmnts
{
    using System.Data.Entity;
    public partial class SISEntities : DbContext
    {
        public SISEntities(string connName)
            : base("name=" + connName)
        {
        }
    }
}