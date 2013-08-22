using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Security;
using System.Runtime.InteropServices;

namespace DavuxLib2.Extensions
{
    public static class StringExtensions
    {
        #region Base64 Encode/Decode
        public static string ToBase64(this string str)
        {
            byte[] encbuff = System.Text.Encoding.UTF8.GetBytes(str);
            return Convert.ToBase64String(encbuff);
        }

        public static string FromBase64(this string str)
        {
            byte[] decbuff = Convert.FromBase64String(str);
            return System.Text.Encoding.UTF8.GetString(decbuff);
        }
        #endregion

        public static string GetMD5Hash(this string input)
        {
            MD5CryptoServiceProvider x = new MD5CryptoServiceProvider();
            byte[] bs = Encoding.UTF8.GetBytes(input);
            bs = x.ComputeHash(bs);
            StringBuilder s = new StringBuilder();
            foreach (byte b in bs)
            {
                s.Append(b.ToString("x2").ToLower());
            }
            return s.ToString();
        }

        public static SecureString ToSecureString(this string input)
        {
            SecureString ret = new SecureString();
            foreach (char c in input.ToCharArray())
            {
                ret.AppendChar(c);
            }
            return ret;
        }

        public static string ToStringUnSecure(this SecureString securePassword)
        {
            if (securePassword == null)
                throw new ArgumentNullException("securePassword");

            IntPtr unmanagedString = IntPtr.Zero;
            try
            {
                unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(securePassword);
                return Marshal.PtrToStringUni(unmanagedString);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
            }
        }

        #region AES Encryption

        internal static string FormatByteArray(byte[] b)
        {
            System.Text.StringBuilder sb1 = new System.Text.StringBuilder();
            int i = 0;
            for (i = 0; i < b.Length; i++)
            {
                if (i != 0 && i % 16 == 0)
                    sb1.Append("\n");
                sb1.Append(System.String.Format("{0:X2} ", b[i]));
            }
            return sb1.ToString();
        }


        public static byte[] Encrypt(this string str, string keyString)
        {
            if (keyString.Length != 32) keyString = keyString.GetMD5Hash();

            byte[] IV = new byte[16];   // match slowaes IV

            var ascii = new System.Text.ASCIIEncoding();

            byte[] key = ascii.GetBytes(keyString);

            RijndaelManaged myRijndael = new RijndaelManaged();
            myRijndael.BlockSize = 128;
            myRijndael.KeySize = 256;
            myRijndael.IV = IV;
            myRijndael.Padding = PaddingMode.PKCS7;
            myRijndael.Mode = CipherMode.CBC;
            myRijndael.Key = key;

            // Encrypt the string to an array of bytes.
            byte[] plainText = new System.Text.ASCIIEncoding().GetBytes(str);
            ICryptoTransform transform = myRijndael.CreateEncryptor();
            byte[] cipherText = transform.TransformFinalBlock(plainText, 0, plainText.Length);

            return cipherText;

            // Decrypt the bytes to a string.
            transform = myRijndael.CreateDecryptor();
            plainText = transform.TransformFinalBlock(cipherText, 0, cipherText.Length);


            string roundtrip = ascii.GetString(plainText);

            Console.WriteLine("Round Trip: {0}", roundtrip);
        }

        public static string Decrypt(this byte[] cipherText, string keyString)
        {
            if (keyString.Length != 32) keyString = keyString.GetMD5Hash();
            byte[] IV = new byte[16];   // match slowaes IV

            var ascii = new System.Text.ASCIIEncoding();

            byte[] key = ascii.GetBytes(keyString);

            RijndaelManaged myRijndael = new RijndaelManaged();
            myRijndael.BlockSize = 128;
            myRijndael.KeySize = 256;
            myRijndael.IV = IV;
            myRijndael.Padding = PaddingMode.PKCS7;
            myRijndael.Mode = CipherMode.CBC;
            myRijndael.Key = key;

            ICryptoTransform transform = myRijndael.CreateEncryptor();
            // Decrypt the bytes to a string.
            transform = myRijndael.CreateDecryptor();
            byte[] plainText = transform.TransformFinalBlock(cipherText, 0, cipherText.Length);


            return ascii.GetString(plainText);


        }

        #endregion

    }
}
