using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace KGySoft.Libraries
{
    /// <summary>
    /// Szabványos Ini file-ok kezelése
    /// </summary>
    public static class IniFiles
    {
        #region .INI file tools

        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section,
            string key, string val, string filePath);
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section,
                 string key, string def, StringBuilder retVal,
            int size, string filePath);

        /// <summary>
        /// Érték beolvasása .INI file-ból (hiba esetén kivételt dob)
        /// </summary>
        /// <param name="fileName">File név (útvonallal is lehet)</param>
        /// <param name="section">Tartomány</param>
        /// <param name="key">Tartományon belüli kulcsnév</param>
        /// <returns>A kulcshoz tartozó érték</returns>
        static public string IniReadValue(string fileName, string section, string key)
        {
            StringBuilder temp = new StringBuilder(255);
            GetPrivateProfileString(section, key, "", temp, 255, fileName);
            return temp.ToString();                        
        }

        /// <summary>
        /// Érték beolvasása .INI file-ból (hiba esetén a defaultValue értékkel tér vissza)
        /// </summary>
        /// <param name="fileName">File név (útvonallal is lehet)</param>
        /// <param name="section">Tartomány</param>
        /// <param name="key">Tartományon belüli kulcsnév</param>
        /// <param name="defaultValue">Hiba esetén ezt kapjuk vissza</param>
        /// <returns>A kulcshoz tartozó érték</returns>
        static public string IniReadValue(string fileName, string section, string key, string defaultValue)
        {
            try
            {
                StringBuilder temp = new StringBuilder(255);
                GetPrivateProfileString(section, key, defaultValue, temp, 255, fileName);
                return temp.ToString();
            }
            catch
            {
                return defaultValue;
            }
        }


        /// <summary>
        /// Érték írása .INI file-ba
        /// </summary>
        /// <param name="fileName">File név (útvonallal is lehet)</param>
        /// <param name="section">Tartomány</param>
        /// <param name="key">Tartományon belüli kulcsnév</param>
        /// <param name="value">A kiírandó kulcsérték</param>
        /// <returns>Sikeresség</returns>
        static public bool IniWriteValue(string fileName, string section, string key, string value)
        {
            try
            {
                WritePrivateProfileString(section, key, value, fileName);
                return true;
            }
            catch
            {
                return false;
            }
        }
        #endregion
    }
}
