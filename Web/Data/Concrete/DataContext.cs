using Assmnts;
using Assmnts.Infrastructure;
using System;
using System.Configuration;
using System.Diagnostics;

namespace Data.Concrete
{
    /// <summary>
    /// Static classes for the 3 database contexts (DEF, UAS, SIS).
    /// </summary>
    /// <remarks>
    /// There is a context for each group of tables.
    /// They are usually all in the same database.
    /// </remarks>
    public static class DataContext
    {
        // If this password is changed, it will also need to be changed in:
        //      AccountController.Venture.PackingFunction
        //      FormsSql.SetIdentityInsert
        private static string ventureConnectionString = @"Password='P@ssword1';data source='|DataDirectory|\forms.vdb5'";
        private static string ventureProviderString = "System.Data.VistaDB5";

        /// <summary>Database Context for the def_ tables</summary>
        /// 
        public static formsEntities GetDbContext()
        {
            bool ventureMode = GetVentureMode();
            // string currentPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);
            // if (currentPath.ToLower().Contains("venture"))
            if (ventureMode)
            {
                Debug.WriteLine("GetDbContext Venture mode - using formsEntitiesVenture");
                // formsEntities fe = new formsEntities("formsEntitiesVenture");
                
                formsEntities fe = new formsEntities("formsEntitiesVenture");
                fe.Database.Connection.ConnectionString = ventureConnectionString;
                
                Debug.WriteLine("   ConnectionString: " + fe.Database.Connection.DataSource);
                return fe;
            }

            return new formsEntities();
        }

        /// <summary>Database Context for the uas_ tables</summary>
        /// 
        public static UASEntities getUasDbContext()
        {
            bool ventureMode = GetVentureMode();
            if (ventureMode)
            {
                Debug.WriteLine("GetDbContext Venture mode - using UASEntitiesVenture");
                UASEntities ue = new UASEntities("UASEntitiesVenture");
                ue.Database.Connection.ConnectionString = ventureConnectionString;
                return ue;
            }

            return new UASEntities("UASEntities");
        }

        /// <summary>Database Context for the SIS tables</summary>
        /// <remarks>
        /// The SIS tables are not prefixed with the application name.
        /// </remarks>
        public static SISEntities getSisDbContext()
        {
            bool ventureMode = GetVentureMode();
            // string currentPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);
            // if (currentPath.ToLower().Contains("venture"))
            if (ventureMode)
            {
                Debug.WriteLine("GetDbContext Venture mode - using SISEntitiesVenture");
                SISEntities se = new SISEntities("SISEntitiesVenture");
                se.Database.Connection.ConnectionString = ventureConnectionString;
                return se;
            }

            return new SISEntities();
        }

        public static string GetUasConnectionString()
        {
            bool ventureMode = GetVentureMode();
            string connString = GetConnectionStringByName(ventureMode ? "UASEntitiesVenture" : "UASEntities");
            // RRB -10/7/2015

            Debug.WriteLine("GetUasConnectionString:" + connString);

            return connString;
        }


        public static string GetUasConnectionStringName()
        {
            bool ventureMode = GetVentureMode();
            string connStringName = ventureMode ? "UASEntitiesAdoVenture" : "UASEntitiesAdo";
            //string connStringName = ventureMode ? "connectionString='" + ventureConnectionString + ";providerName='" + ventureProviderString + "'" : "UASEntitiesAdo";
            Debug.WriteLine("GetUasConnectionStringName:" + connStringName);

            return connStringName;
        }
        
        
        private static bool GetVentureMode()
        {
            return SessionHelper.IsVentureMode;
        }

        public static ConnectionStringSettings getUASEntitiesAdoVenture()
        {
            ConnectionStringSettings UASAdoVenture = new ConnectionStringSettings();
            UASAdoVenture.Name = "UASEntitiesAdoVenture";
            UASAdoVenture.ConnectionString = ventureConnectionString;
            UASAdoVenture.ProviderName = ventureProviderString;

            return UASAdoVenture;
        }


        // Retrieves a connection string by name.
        // Returns null if the name is not found.
        static string GetConnectionStringByName(string name)
        {
            // Assume failure.
            string returnValue = null;

            // Look for the name in the connectionStrings section.
            ConnectionStringSettings settings = ConfigurationManager.ConnectionStrings[name];

            // If found, return the connection string.
            if (settings != null)
            {
                returnValue = settings.ConnectionString;
            }

            return returnValue;
        }

    }
}
