using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace UAS.Business
{
    public static class UtilityFunction
    {

        #region - constants
        // private const string ENCKEY = "d05Xf6dR";
        private const string TENCKEY = "d05Xf6dR24xvyj8b";
        // private const string TPSSKEY = "xc567hSrjb$^*[{:";
        #endregion - END constants


        public static bool VerifyPassKey(string passkey, string loginid)
        {

            MD5 algo = MD5.Create();
            string s = Constants.PASSKEY_CRYPT + loginid + Constants.PASSKEY_SALT1 + Constants.PASSKEY_SALT2;
            byte[] textin = Encoding.ASCII.GetBytes(s);
            byte[] hshe = algo.ComputeHash(textin);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hshe.Length; i++)
            {
                sb.Append(hshe[i].ToString("X2"));
            }

            bool _isValid = (passkey == sb.ToString()) ? true : false;

            return _isValid;
        }

        public static string EncryptPassKey(string loginid)
        {
            MD5 algo = MD5.Create();
            string s = Constants.PASSKEY_CRYPT + loginid + Constants.PASSKEY_SALT1 + Constants.PASSKEY_SALT2;
            byte[] textin = Encoding.ASCII.GetBytes(s);
            byte[] hshe = algo.ComputeHash(textin);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hshe.Length; i++)
            {
                sb.Append(hshe[i].ToString("X2"));
            }

            return sb.ToString();
        }

        public static string EncryptPassword(string pwd)
        {
            MD5 algo = MD5.Create();
            string s = Constants.PASSKEY_CRYPT + Constants.PASSKEY_SALT1 + pwd + Constants.PASSKEY_SALT2;
            byte[] textin = Encoding.ASCII.GetBytes(s);
            byte[] hshe = algo.ComputeHash(textin);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hshe.Length; i++)
            {
                sb.Append(hshe[i].ToString("X2"));
            }

            return sb.ToString();
        }

        public static string DataEncrypt(string inline)
        {
            string outline = string.Empty;
            if (string.IsNullOrEmpty(inline))
            {
                return outline;
            }
            byte[] bytes = ASCIIEncoding.ASCII.GetBytes(TENCKEY);
            //DESCryptoServiceProvider des = new DESCryptoServiceProvider();
            TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();
            MemoryStream memS = new MemoryStream();
            CryptoStream crypS = new CryptoStream(memS, tdes.CreateEncryptor(bytes, bytes), CryptoStreamMode.Write);
            StreamWriter swr = new StreamWriter(crypS);
            swr.Write(inline);
            swr.Flush();
            crypS.FlushFinalBlock();
            swr.Flush();
            outline = Convert.ToBase64String(memS.GetBuffer(), 0, (int)memS.Length);
            return outline;
        }

        public static string DataDecrypt(string inline)
        {
            string outline = string.Empty;
            if (string.IsNullOrEmpty(inline))
            {
                return outline;
            }
            byte[] bytes = ASCIIEncoding.ASCII.GetBytes(TENCKEY);
            TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();
            MemoryStream memS = new MemoryStream(Convert.FromBase64String(inline));
            CryptoStream crypS = new CryptoStream(memS, tdes.CreateDecryptor(bytes, bytes), CryptoStreamMode.Read);
            StreamReader RD = new StreamReader(crypS);
            outline = RD.ReadToEnd();

            return outline;
        }

        /// <summary>
        /// Get current user ip address.
        /// </summary>
        /// <returns>The IP Address</returns>
        public static string GetUserIPAddress()
        {
            var context = System.Web.HttpContext.Current;
            string ip = String.Empty;

            if (context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"] != null)
                ip = context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"].ToString();
            else if (!String.IsNullOrWhiteSpace(context.Request.UserHostAddress))
                ip = context.Request.UserHostAddress;

            if (ip == "::1")
                ip = "127.0.0.1";

            return ip;
        }
    }
}
