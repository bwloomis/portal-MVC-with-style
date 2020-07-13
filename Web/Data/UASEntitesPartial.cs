using System;

namespace Assmnts
{
    using System.Data.Entity;
    public partial class UASEntities : DbContext
    {
        public UASEntities(string connName)
            : base("name=" + connName)
        {
        }
    }
}