
using Data.Abstract;
using Data.Concrete;

using System;
using System.Diagnostics;

namespace AJBoggs.Adap.Services.Sql
{
    public static class Uas
    {
		
		public static void UpdateUserDob(int? userId, string dob)
		{
            // Update the UAS User DOB.
            if (userId != null)
            {
                IUasSql uasSql = new UasSql();
                uasSql.SaveDOB(userId.Value, dob);
            }
			
		}

    }
}