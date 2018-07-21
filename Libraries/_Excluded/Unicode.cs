using System;
using System.Collections.Generic;
using System.Text;

namespace KGySoft.Libraries
{
    /// <summary>
    /// This class provides constants for Unicode characters.
    /// Constant names are the standard SGML entity names defined at
    /// <a href="ftp://ftp.unicode.org/Public/MAPPINGS/VENDORS/MISC/SGML.TXT" target="_blank"/>
    /// </summary>
    public static class Unicode
    {
        #region Added Latin 1 Entities (ISOlat1)

        /// <summary>
        /// LATIN CAPITAL LETTER A WITH ACUTE
        /// </summary>
        public const char Aacute = '\u00C1';

        /// <summary>
        /// LATIN SMALL LETTER A WITH ACUTE
        /// </summary>
        public const char aacute = '\u00E1';

        /// <summary>
        /// LATIN CAPITAL LETTER A WITH CIRCUMFLEX
        /// </summary>
        public const char Acirc = '\u00C2';

        /// <summary>
        /// LATIN SMALL LETTER A WITH CIRCUMFLEX
        /// </summary>
        public const char acirc = '\u00E2';

        /// <summary>
        /// LATIN CAPITAL LETTER AE
        /// </summary>
        public const char AElig = '\u00C6';

        /// <summary>
        /// LATIN SMALL LETTER AE
        /// </summary>
        public const char aelig = '\u00E6';

        /// <summary>
        /// LATIN CAPITAL LETTER A WITH GRAVE
        /// </summary>
        public const char Agrave = '\u00C0';

        /// <summary>
        /// LATIN SMALL LETTER A WITH GRAVE
        /// </summary>
        public const char agrave = '\u00E0';

        /// <summary>
        /// LATIN CAPITAL LETTER A WITH RING ABOVE
        /// </summary>
        public const char Aring = '\u00C5';

        /// <summary>
        /// LATIN SMALL LETTER A WITH RING ABOVE
        /// </summary>
        public const char aring = '\u00E5';

        /// <summary>
        /// LATIN CAPITAL LETTER A WITH TILDE
        /// </summary>
        public const char Atilde = '\u00C3';

        /// <summary>
        /// LATIN SMALL LETTER A WITH TILDE
        /// </summary>
        public const char atilde = '\u00E3';

        /// <summary>
        /// LATIN CAPITAL LETTER A WITH DIAERESIS
        /// </summary>
        public const char Auml = '\u00C4';

        /// <summary>
        /// LATIN SMALL LETTER A WITH DIAERESIS
        /// </summary>
        public const char auml = '\u00E4';

        /// <summary>
        /// LATIN CAPITAL LETTER C WITH CEDILLA
        /// </summary>
        public const char Ccedil = '\u00C7';

        /// <summary>
        /// LATIN SMALL LETTER C WITH CEDILLA
        /// </summary>
        public const char ccedil = '\u00E7';

        /// <summary>
        /// LATIN CAPITAL LETTER E WITH ACUTE
        /// </summary>
        public const char Eacute = '\u00C9';

        /// <summary>
        /// LATIN SMALL LETTER E WITH ACUTE
        /// </summary>
        public const char eacute = '\u00E9';

        /// <summary>
        /// LATIN CAPITAL LETTER E WITH CIRCUMFLEX
        /// </summary>
        public const char Ecirc = '\u00CA';

        /// <summary>
        /// LATIN SMALL LETTER E WITH CIRCUMFLEX
        /// </summary>
        public const char ecirc = '\u00EA';

        /// <summary>
        /// LATIN CAPITAL LETTER E WITH GRAVE
        /// </summary>
        public const char Egrave = '\u00C8';

        /// <summary>
        /// LATIN SMALL LETTER E WITH GRAVE
        /// </summary>
        public const char egrave = '\u00E8';

        /// <summary>
        /// LATIN CAPITAL LETTER ETH
        /// </summary>
        public const char ETH = '\u00D0';

        /// <summary>
        /// LATIN SMALL LETTER ETH
        /// </summary>
        public const char eth = '\u00F0';

        /// <summary>
        /// LATIN CAPITAL LETTER E WITH DIAERESIS
        /// </summary>
        public const char Euml = '\u00CB';

        /// <summary>
        /// LATIN SMALL LETTER E WITH DIAERESIS
        /// </summary>
        public const char euml = '\u00EB';

        /// <summary>
        /// LATIN CAPITAL LETTER I WITH ACUTE
        /// </summary>
        public const char Iacute = '\u00CD';

        /// <summary>
        /// LATIN SMALL LETTER I WITH ACUTE
        /// </summary>
        public const char iacute = '\u00ED';

        /// <summary>
        /// LATIN CAPITAL LETTER I WITH CIRCUMFLEX
        /// </summary>
        public const char Icirc = '\u00CE';

        /// <summary>
        /// LATIN SMALL LETTER I WITH CIRCUMFLEX
        /// </summary>
        public const char icirc = '\u00EE';

        /// <summary>
        /// LATIN CAPITAL LETTER I WITH GRAVE
        /// </summary>
        public const char Igrave = '\u00CC';

        /// <summary>
        /// LATIN SMALL LETTER I WITH GRAVE
        /// </summary>
        public const char igrave = '\u00EC';

        /// <summary>
        /// LATIN CAPITAL LETTER I WITH DIAERESIS
        /// </summary>
        public const char Iuml = '\u00CF';

        /// <summary>
        /// LATIN SMALL LETTER I WITH DIAERESIS
        /// </summary>
        public const char iuml = '\u00EF';

        /// <summary>
        /// LATIN CAPITAL LETTER N WITH TILDE
        /// </summary>
        public const char Ntilde = '\u00D1';

        /// <summary>
        /// LATIN SMALL LETTER N WITH TILDE
        /// </summary>
        public const char ntilde = '\u00F1';

        /// <summary>
        /// LATIN CAPITAL LETTER O WITH ACUTE
        /// </summary>
        public const char Oacute = '\u00D3';

        /// <summary>
        /// LATIN SMALL LETTER O WITH ACUTE
        /// </summary>
        public const char oacute = '\u00F3';

        /// <summary>
        /// LATIN CAPITAL LETTER O WITH CIRCUMFLEX
        /// </summary>
        public const char Ocirc = '\u00D4';

        /// <summary>
        /// LATIN SMALL LETTER O WITH CIRCUMFLEX
        /// </summary>
        public const char ocirc = '\u00F4';

        /// <summary>
        /// LATIN CAPITAL LETTER O WITH GRAVE
        /// </summary>
        public const char Ograve = '\u00D2';

        /// <summary>
        /// LATIN SMALL LETTER O WITH GRAVE
        /// </summary>
        public const char ograve = '\u00F2';

        /// <summary>
        /// LATIN CAPITAL LETTER O WITH STROKE
        /// </summary>
        public const char Oslash = '\u00D8';

        /// <summary>
        /// LATIN SMALL LETTER O WITH STROKE
        /// </summary>
        public const char oslash = '\u00F8';

        /// <summary>
        /// LATIN CAPITAL LETTER O WITH TILDE
        /// </summary>
        public const char Otilde = '\u00D5';

        /// <summary>
        /// LATIN SMALL LETTER O WITH TILDE
        /// </summary>
        public const char otilde = '\u00F5';

        /// <summary>
        /// LATIN CAPITAL LETTER O WITH DIAERESIS
        /// </summary>
        public const char Ouml = '\u00D6';

        /// <summary>
        /// LATIN SMALL LETTER O WITH DIAERESIS
        /// </summary>
        public const char ouml = '\u00F6';

        /// <summary>
        /// LATIN SMALL LETTER SHARP S
        /// </summary>
        public const char szlig = '\u00DF';

        /// <summary>
        /// LATIN CAPITAL LETTER THORN
        /// </summary>
        public const char THORN = '\u00DE';

        /// <summary>
        /// LATIN SMALL LETTER THORN
        /// </summary>
        public const char thorn = '\u00FE';

        /// <summary>
        /// LATIN CAPITAL LETTER U WITH ACUTE
        /// </summary>
        public const char Uacute = '\u00DA';

        /// <summary>
        /// LATIN SMALL LETTER U WITH ACUTE
        /// </summary>
        public const char uacute = '\u00FA';

        /// <summary>
        /// LATIN CAPITAL LETTER U WITH CIRCUMFLEX
        /// </summary>
        public const char Ucirc = '\u00DB';

        /// <summary>
        /// LATIN SMALL LETTER U WITH CIRCUMFLEX
        /// </summary>
        public const char ucirc = '\u00FB';

        /// <summary>
        /// LATIN CAPITAL LETTER U WITH GRAVE
        /// </summary>
        public const char Ugrave = '\u00D9';

        /// <summary>
        /// LATIN SMALL LETTER U WITH GRAVE
        /// </summary>
        public const char ugrave = '\u00F9';

        /// <summary>
        /// LATIN CAPITAL LETTER U WITH DIAERESIS
        /// </summary>
        public const char Uuml = '\u00DC';

        /// <summary>
        /// LATIN SMALL LETTER U WITH DIAERESIS
        /// </summary>
        public const char uuml = '\u00FC';

        /// <summary>
        /// LATIN CAPITAL LETTER Y WITH ACUTE
        /// </summary>
        public const char Yacute = '\u00DD';

        /// <summary>
        /// LATIN SMALL LETTER Y WITH ACUTE
        /// </summary>
        public const char yacute = '\u00FD';

        /// <summary>
        /// LATIN SMALL LETTER Y WITH DIAERESIS
        /// </summary>
        public const char yuml = '\u00FF';

        #endregion

        #region Added Latin 2 Entities (ISOlat2)

        /// <summary>
        /// LATIN CAPITAL LETTER A WITH BREVE
        /// </summary>
        public const char Abreve = '\u0102';

        /// <summary>
        /// LATIN SMALL LETTER A WITH BREVE
        /// </summary>
        public const char abreve = '\u0103';

        /// <summary>
        /// LATIN CAPITAL LETTER A WITH MACRON
        /// </summary>
        public const char Amacr = '\u0100';

        /// <summary>
        /// LATIN SMALL LETTER A WITH MACRON
        /// </summary>
        public const char amacr = '\u0101';

        /// <summary>
        /// LATIN CAPITAL LETTER A WITH OGONEK
        /// </summary>
        public const char Aogon = '\u0104';

        /// <summary>
        /// LATIN SMALL LETTER A WITH OGONEK
        /// </summary>
        public const char aogon = '\u0105';

        /// <summary>
        /// LATIN CAPITAL LETTER C WITH ACUTE
        /// </summary>
        public const char Cacute = '\u0106';

        /// <summary>
        /// LATIN SMALL LETTER C WITH ACUTE
        /// </summary>
        public const char cacute = '\u0107';

        /// <summary>
        /// LATIN CAPITAL LETTER C WITH CARON
        /// </summary>
        public const char Ccaron = '\u010C';

        /// <summary>
        /// LATIN SMALL LETTER C WITH CARON
        /// </summary>
        public const char ccaron = '\u010D';

        /// <summary>
        /// LATIN CAPITAL LETTER C WITH CIRCUMFLEX
        /// </summary>
        public const char Ccirc = '\u0108';

        /// <summary>
        /// LATIN SMALL LETTER C WITH CIRCUMFLEX
        /// </summary>
        public const char ccirc = '\u0109';

        /// <summary>
        /// LATIN CAPITAL LETTER C WITH DOT ABOVE
        /// </summary>
        public const char Cdot = '\u010A';

        /// <summary>
        /// LATIN SMALL LETTER C WITH DOT ABOVE
        /// </summary>
        public const char cdot = '\u010B';

        /// <summary>
        /// LATIN CAPITAL LETTER D WITH CARON
        /// </summary>
        public const char Dcaron = '\u010E';

        /// <summary>
        /// LATIN SMALL LETTER D WITH CARON
        /// </summary>
        public const char dcaron = '\u010F';

        /// <summary>
        /// LATIN CAPITAL LETTER D WITH STROKE
        /// </summary>
        public const char Dstrok = '\u0110';

        /// <summary>
        /// LATIN SMALL LETTER D WITH STROKE
        /// </summary>
        public const char dstrok = '\u0111';

        /// <summary>
        /// LATIN CAPITAL LETTER E WITH CARON
        /// </summary>
        public const char Ecaron = '\u011A';

        /// <summary>
        /// LATIN SMALL LETTER E WITH CARON
        /// </summary>
        public const char ecaron = '\u011B';

        /// <summary>
        /// LATIN CAPITAL LETTER E WITH DOT ABOVE
        /// </summary>
        public const char Edot = '\u0116';

        /// <summary>
        /// LATIN SMALL LETTER E WITH DOT ABOVE
        /// </summary>
        public const char edot = '\u0117';

        /// <summary>
        /// LATIN CAPITAL LETTER E WITH MACRON
        /// </summary>
        public const char Emacr = '\u0112';

        /// <summary>
        /// LATIN SMALL LETTER E WITH MACRON
        /// </summary>
        public const char emacr = '\u0113';

        /// <summary>
        /// LATIN CAPITAL LETTER ENG
        /// </summary>
        public const char ENG = '\u014A';

        /// <summary>
        /// LATIN SMALL LETTER ENG
        /// </summary>
        public const char eng = '\u014B';

        /// <summary>
        /// LATIN CAPITAL LETTER E WITH OGONEK
        /// </summary>
        public const char Eogon = '\u0118';

        /// <summary>
        /// LATIN SMALL LETTER E WITH OGONEK
        /// </summary>
        public const char eogon = '\u0119';

        /// <summary>
        /// LATIN SMALL LETTER G WITH ACUTE
        /// </summary>
        public const char gacute = '\u01F5';

        /// <summary>
        /// LATIN CAPITAL LETTER G WITH BREVE
        /// </summary>
        public const char Gbreve = '\u011E';

        /// <summary>
        /// LATIN SMALL LETTER G WITH BREVE
        /// </summary>
        public const char gbreve = '\u011F';

        /// <summary>
        /// LATIN CAPITAL LETTER G WITH CEDILLA
        /// </summary>
        public const char Gcedil = '\u0122';

        /// <summary>
        /// LATIN SMALL LETTER G WITH CEDILLA
        /// </summary>
        public const char gcedil = '\u0123';

        /// <summary>
        /// LATIN CAPITAL LETTER G WITH CIRCUMFLEX
        /// </summary>
        public const char Gcirc = '\u011C';

        /// <summary>
        /// LATIN SMALL LETTER G WITH CIRCUMFLEX
        /// </summary>
        public const char gcirc = '\u011D';

        /// <summary>
        /// LATIN CAPITAL LETTER G WITH DOT ABOVE
        /// </summary>
        public const char Gdot = '\u0120';

        /// <summary>
        /// LATIN SMALL LETTER G WITH DOT ABOVE
        /// </summary>
        public const char gdot = '\u0121';

        /// <summary>
        /// LATIN CAPITAL LETTER H WITH CIRCUMFLEX
        /// </summary>
        public const char Hcirc = '\u0124';

        /// <summary>
        /// LATIN SMALL LETTER H WITH CIRCUMFLEX
        /// </summary>
        public const char hcirc = '\u0125';

        /// <summary>
        /// LATIN CAPITAL LETTER H WITH STROKE
        /// </summary>
        public const char Hstrok = '\u0126';

        /// <summary>
        /// LATIN SMALL LETTER H WITH STROKE
        /// </summary>
        public const char hstrok = '\u0127';

        /// <summary>
        /// LATIN CAPITAL LETTER I WITH DOT ABOVE
        /// </summary>
        public const char Idot = '\u0130';

        /// <summary>
        /// LATIN CAPITAL LIGATURE IJ
        /// </summary>
        public const char IJlig = '\u0132';

        /// <summary>
        /// LATIN SMALL LIGATURE IJ
        /// </summary>
        public const char ijlig = '\u0133';

        /// <summary>
        /// LATIN CAPITAL LETTER I WITH MACRON
        /// </summary>
        public const char Imacr = '\u012A';

        /// <summary>
        /// LATIN SMALL LETTER I WITH MACRON
        /// </summary>
        public const char imacr = '\u012B';

        /// <summary>
        /// LATIN SMALL LETTER DOTLESS I
        /// </summary>
        public const char inodot = '\u0131';

        /// <summary>
        /// LATIN CAPITAL LETTER I WITH OGONEK
        /// </summary>
        public const char Iogon = '\u012E';

        /// <summary>
        /// LATIN SMALL LETTER I WITH OGONEK
        /// </summary>
        public const char iogon = '\u012F';

        /// <summary>
        /// LATIN CAPITAL LETTER I WITH TILDE
        /// </summary>
        public const char Itilde = '\u0128';

        /// <summary>
        /// LATIN SMALL LETTER I WITH TILDE
        /// </summary>
        public const char itilde = '\u0129';

        /// <summary>
        /// LATIN CAPITAL LETTER J WITH CIRCUMFLEX
        /// </summary>
        public const char Jcirc = '\u0134';

        /// <summary>
        /// LATIN SMALL LETTER J WITH CIRCUMFLEX
        /// </summary>
        public const char jcirc = '\u0135';

        /// <summary>
        /// LATIN CAPITAL LETTER K WITH CEDILLA
        /// </summary>
        public const char Kcedil = '\u0136';

        /// <summary>
        /// LATIN SMALL LETTER K WITH CEDILLA
        /// </summary>
        public const char kcedil = '\u0137';

        /// <summary>
        /// LATIN SMALL LETTER KRA
        /// </summary>
        public const char kgreen = '\u0138';

        /// <summary>
        /// LATIN CAPITAL LETTER L WITH ACUTE
        /// </summary>
        public const char Lacute = '\u0139';

        /// <summary>
        /// LATIN SMALL LETTER L WITH ACUTE
        /// </summary>
        public const char lacute = '\u013A';

        /// <summary>
        /// LATIN CAPITAL LETTER L WITH CARON
        /// </summary>
        public const char Lcaron = '\u013D';

        /// <summary>
        /// LATIN SMALL LETTER L WITH CARON
        /// </summary>
        public const char lcaron = '\u013E';

        /// <summary>
        /// LATIN CAPITAL LETTER L WITH CEDILLA
        /// </summary>
        public const char Lcedil = '\u013B';

        /// <summary>
        /// LATIN SMALL LETTER L WITH CEDILLA
        /// </summary>
        public const char lcedil = '\u013C';

        /// <summary>
        /// LATIN CAPITAL LETTER L WITH MIDDLE DOT
        /// </summary>
        public const char Lmidot = '\u013F';

        /// <summary>
        /// LATIN SMALL LETTER L WITH MIDDLE DOT
        /// </summary>
        public const char lmidot = '\u0140';

        /// <summary>
        /// LATIN CAPITAL LETTER L WITH STROKE
        /// </summary>
        public const char Lstrok = '\u0141';

        /// <summary>
        /// LATIN SMALL LETTER L WITH STROKE
        /// </summary>
        public const char lstrok = '\u0142';

        /// <summary>
        /// LATIN CAPITAL LETTER N WITH ACUTE
        /// </summary>
        public const char Nacute = '\u0143';

        /// <summary>
        /// LATIN SMALL LETTER N WITH ACUTE
        /// </summary>
        public const char nacute = '\u0144';

        /// <summary>
        /// LATIN SMALL LETTER N PRECEDED BY APOSTROPHE
        /// </summary>
        public const char napos = '\u0149';

        /// <summary>
        /// LATIN CAPITAL LETTER N WITH CARON
        /// </summary>
        public const char Ncaron = '\u0147';

        /// <summary>
        /// LATIN SMALL LETTER N WITH CARON
        /// </summary>
        public const char ncaron = '\u0148';

        /// <summary>
        /// LATIN CAPITAL LETTER N WITH CEDILLA
        /// </summary>
        public const char Ncedil = '\u0145';

        /// <summary>
        /// LATIN SMALL LETTER N WITH CEDILLA
        /// </summary>
        public const char ncedil = '\u0146';

        /// <summary>
        /// LATIN CAPITAL LETTER O WITH DOUBLE ACUTE
        /// </summary>
        public const char Odblac = '\u0150';

        /// <summary>
        /// LATIN SMALL LETTER O WITH DOUBLE ACUTE
        /// </summary>
        public const char odblac = '\u0151';

        /// <summary>
        /// LATIN CAPITAL LIGATURE OE
        /// </summary>
        public const char OElig = '\u0152';

        /// <summary>
        /// LATIN SMALL LIGATURE OE
        /// </summary>
        public const char oelig = '\u0153';

        /// <summary>
        /// LATIN CAPITAL LETTER O WITH MACRON
        /// </summary>
        public const char Omacr = '\u014C';

        /// <summary>
        /// LATIN SMALL LETTER O WITH MACRON
        /// </summary>
        public const char omacr = '\u014D';

        /// <summary>
        /// LATIN CAPITAL LETTER R WITH ACUTE
        /// </summary>
        public const char Racute = '\u0154';

        /// <summary>
        /// LATIN SMALL LETTER R WITH ACUTE
        /// </summary>
        public const char racute = '\u0155';

        /// <summary>
        /// LATIN CAPITAL LETTER R WITH CARON
        /// </summary>
        public const char Rcaron = '\u0158';

        /// <summary>
        /// LATIN SMALL LETTER R WITH CARON
        /// </summary>
        public const char rcaron = '\u0159';

        /// <summary>
        /// LATIN CAPITAL LETTER R WITH CEDILLA
        /// </summary>
        public const char Rcedil = '\u0156';

        /// <summary>
        /// LATIN SMALL LETTER R WITH CEDILLA
        /// </summary>
        public const char rcedil = '\u0157';

        /// <summary>
        /// LATIN CAPITAL LETTER S WITH ACUTE
        /// </summary>
        public const char Sacute = '\u015A';

        /// <summary>
        /// LATIN SMALL LETTER S WITH ACUTE
        /// </summary>
        public const char sacute = '\u015B';

        /// <summary>
        /// LATIN CAPITAL LETTER S WITH CARON
        /// </summary>
        public const char Scaron = '\u0160';

        /// <summary>
        /// LATIN SMALL LETTER S WITH CARON
        /// </summary>
        public const char scaron = '\u0161';

        /// <summary>
        /// LATIN CAPITAL LETTER S WITH CEDILLA
        /// </summary>
        public const char Scedil = '\u015E';

        /// <summary>
        /// LATIN SMALL LETTER S WITH CEDILLA
        /// </summary>
        public const char scedil = '\u015F';

        /// <summary>
        /// LATIN CAPITAL LETTER S WITH CIRCUMFLEX
        /// </summary>
        public const char Scirc = '\u015C';

        /// <summary>
        /// LATIN SMALL LETTER S WITH CIRCUMFLEX
        /// </summary>
        public const char scirc = '\u015D';

        /// <summary>
        /// LATIN CAPITAL LETTER T WITH CARON
        /// </summary>
        public const char Tcaron = '\u0164';

        /// <summary>
        /// LATIN SMALL LETTER T WITH CARON
        /// </summary>
        public const char tcaron = '\u0165';

        /// <summary>
        /// LATIN CAPITAL LETTER T WITH CEDILLA
        /// </summary>
        public const char Tcedil = '\u0162';

        /// <summary>
        /// LATIN SMALL LETTER T WITH CEDILLA
        /// </summary>
        public const char tcedil = '\u0163';

        /// <summary>
        /// LATIN CAPITAL LETTER T WITH STROKE
        /// </summary>
        public const char Tstrok = '\u0166';

        /// <summary>
        /// LATIN SMALL LETTER T WITH STROKE
        /// </summary>
        public const char tstrok = '\u0167';

        /// <summary>
        /// LATIN CAPITAL LETTER U WITH BREVE
        /// </summary>
        public const char Ubreve = '\u016C';

        /// <summary>
        /// LATIN SMALL LETTER U WITH BREVE
        /// </summary>
        public const char ubreve = '\u016D';

        /// <summary>
        /// LATIN CAPITAL LETTER U WITH DOUBLE ACUTE
        /// </summary>
        public const char Udblac = '\u0170';

        /// <summary>
        /// LATIN SMALL LETTER U WITH DOUBLE ACUTE
        /// </summary>
        public const char udblac = '\u0171';

        /// <summary>
        /// LATIN CAPITAL LETTER U WITH MACRON
        /// </summary>
        public const char Umacr = '\u016A';

        /// <summary>
        /// LATIN SMALL LETTER U WITH MACRON
        /// </summary>
        public const char umacr = '\u016B';

        /// <summary>
        /// LATIN CAPITAL LETTER U WITH OGONEK
        /// </summary>
        public const char Uogon = '\u0172';

        /// <summary>
        /// LATIN SMALL LETTER U WITH OGONEK
        /// </summary>
        public const char uogon = '\u0173';

        /// <summary>
        /// LATIN CAPITAL LETTER U WITH RING ABOVE
        /// </summary>
        public const char Uring = '\u016E';

        /// <summary>
        /// LATIN SMALL LETTER U WITH RING ABOVE
        /// </summary>
        public const char uring = '\u016F';

        /// <summary>
        /// LATIN CAPITAL LETTER U WITH TILDE
        /// </summary>
        public const char Utilde = '\u0168';

        /// <summary>
        /// LATIN SMALL LETTER U WITH TILDE
        /// </summary>
        public const char utilde = '\u0169';

        /// <summary>
        /// LATIN CAPITAL LETTER W WITH CIRCUMFLEX
        /// </summary>
        public const char Wcirc = '\u0174';

        /// <summary>
        /// LATIN SMALL LETTER W WITH CIRCUMFLEX
        /// </summary>
        public const char wcirc = '\u0175';

        /// <summary>
        /// LATIN CAPITAL LETTER Y WITH CIRCUMFLEX
        /// </summary>
        public const char Ycirc = '\u0176';

        /// <summary>
        /// LATIN SMALL LETTER Y WITH CIRCUMFLEX
        /// </summary>
        public const char ycirc = '\u0177';

        /// <summary>
        /// LATIN CAPITAL LETTER Y WITH DIAERESIS
        /// </summary>
        public const char Yuml = '\u0178';

        /// <summary>
        /// LATIN CAPITAL LETTER Z WITH ACUTE
        /// </summary>
        public const char Zacute = '\u0179';

        /// <summary>
        /// LATIN SMALL LETTER Z WITH ACUTE
        /// </summary>
        public const char zacute = '\u017A';

        /// <summary>
        /// LATIN CAPITAL LETTER Z WITH CARON
        /// </summary>
        public const char Zcaron = '\u017D';

        /// <summary>
        /// LATIN SMALL LETTER Z WITH CARON
        /// </summary>
        public const char zcaron = '\u017E';

        /// <summary>
        /// LATIN CAPITAL LETTER Z WITH DOT ABOVE
        /// </summary>
        public const char Zdot = '\u017B';

        /// <summary>
        /// LATIN SMALL LETTER Z WITH DOT ABOVE
        /// </summary>
        public const char zdot = '\u017C';

        #endregion

        #region Greek Letters Entities (ISOgrk1)

        /// <summary>
        /// GREEK CAPITAL LETTER ALPHA
        /// </summary>
        public const char Agr = '\u0391';

        /// <summary>
        /// GREEK SMALL LETTER ALPHA
        /// </summary>
        public const char agr = '\u03B1';

        /// <summary>
        /// GREEK CAPITAL LETTER BETA
        /// </summary>
        public const char Bgr = '\u0392';

        /// <summary>
        /// GREEK SMALL LETTER BETA
        /// </summary>
        public const char bgr = '\u03B2';

        /// <summary>
        /// GREEK CAPITAL LETTER DELTA
        /// </summary>
        public const char Dgr = '\u0394';

        /// <summary>
        /// GREEK SMALL LETTER DELTA
        /// </summary>
        public const char dgr = '\u03B4';

        /// <summary>
        /// GREEK CAPITAL LETTER ETA
        /// </summary>
        public const char EEgr = '\u0397';

        /// <summary>
        /// GREEK SMALL LETTER ETA
        /// </summary>
        public const char eegr = '\u03B7';

        /// <summary>
        /// GREEK CAPITAL LETTER EPSILON
        /// </summary>
        public const char Egr = '\u0395';

        /// <summary>
        /// GREEK SMALL LETTER EPSILON
        /// </summary>
        public const char egr = '\u03B5';

        /// <summary>
        /// GREEK CAPITAL LETTER GAMMA
        /// </summary>
        public const char Ggr = '\u0393';

        /// <summary>
        /// GREEK SMALL LETTER GAMMA
        /// </summary>
        public const char ggr = '\u03B3';

        /// <summary>
        /// GREEK CAPITAL LETTER IOTA
        /// </summary>
        public const char Igr = '\u0399';

        /// <summary>
        /// GREEK SMALL LETTER IOTA
        /// </summary>
        public const char igr = '\u03B9';

        /// <summary>
        /// GREEK CAPITAL LETTER KAPPA
        /// </summary>
        public const char Kgr = '\u039A';

        /// <summary>
        /// GREEK SMALL LETTER KAPPA
        /// </summary>
        public const char kgr = '\u03BA';

        /// <summary>
        /// GREEK CAPITAL LETTER CHI
        /// </summary>
        public const char KHgr = '\u03A7';

        /// <summary>
        /// GREEK SMALL LETTER CHI
        /// </summary>
        public const char khgr = '\u03C7';

        /// <summary>
        /// GREEK CAPITAL LETTER LAMDA
        /// </summary>
        public const char Lgr = '\u039B';

        /// <summary>
        /// GREEK SMALL LETTER LAMDA
        /// </summary>
        public const char lgr = '\u03BB';

        /// <summary>
        /// GREEK CAPITAL LETTER MU
        /// </summary>
        public const char Mgr = '\u039C';

        /// <summary>
        /// GREEK SMALL LETTER MU
        /// </summary>
        public const char mgr = '\u03BC';

        /// <summary>
        /// GREEK CAPITAL LETTER NU
        /// </summary>
        public const char Ngr = '\u039D';

        /// <summary>
        /// GREEK SMALL LETTER NU
        /// </summary>
        public const char ngr = '\u03BD';

        /// <summary>
        /// GREEK CAPITAL LETTER OMICRON
        /// </summary>
        public const char Ogr = '\u039F';

        /// <summary>
        /// GREEK SMALL LETTER OMICRON
        /// </summary>
        public const char ogr = '\u03BF';

        /// <summary>
        /// GREEK CAPITAL LETTER OMEGA
        /// </summary>
        public const char OHgr = '\u03A9';

        /// <summary>
        /// GREEK SMALL LETTER OMEGA
        /// </summary>
        public const char ohgr = '\u03C9';

        /// <summary>
        /// GREEK CAPITAL LETTER PI
        /// </summary>
        public const char Pgr = '\u03A0';

        /// <summary>
        /// GREEK SMALL LETTER PI
        /// </summary>
        public const char pgr = '\u03C0';

        /// <summary>
        /// GREEK CAPITAL LETTER PHI
        /// </summary>
        public const char PHgr = '\u03A6';

        /// <summary>
        /// GREEK SMALL LETTER PHI
        /// </summary>
        public const char phgr = '\u03C6';

        /// <summary>
        /// GREEK CAPITAL LETTER PSI
        /// </summary>
        public const char PSgr = '\u03A8';

        /// <summary>
        /// GREEK SMALL LETTER PSI
        /// </summary>
        public const char psgr = '\u03C8';

        /// <summary>
        /// GREEK CAPITAL LETTER RHO
        /// </summary>
        public const char Rgr = '\u03A1';

        /// <summary>
        /// GREEK SMALL LETTER RHO
        /// </summary>
        public const char rgr = '\u03C1';

        /// <summary>
        /// GREEK SMALL LETTER FINAL SIGMA
        /// </summary>
        public const char sfgr = '\u03C2';

        /// <summary>
        /// GREEK CAPITAL LETTER SIGMA
        /// </summary>
        public const char Sgr = '\u03A3';

        /// <summary>
        /// GREEK SMALL LETTER SIGMA
        /// </summary>
        public const char sgr = '\u03C3';

        /// <summary>
        /// GREEK CAPITAL LETTER TAU
        /// </summary>
        public const char Tgr = '\u03A4';

        /// <summary>
        /// GREEK SMALL LETTER TAU
        /// </summary>
        public const char tgr = '\u03C4';

        /// <summary>
        /// GREEK CAPITAL LETTER THETA
        /// </summary>
        public const char THgr = '\u0398';

        /// <summary>
        /// GREEK SMALL LETTER THETA
        /// </summary>
        public const char thgr = '\u03B8';

        /// <summary>
        /// GREEK CAPITAL LETTER UPSILON
        /// </summary>
        public const char Ugr = '\u03A5';

        /// <summary>
        /// GREEK SMALL LETTER UPSILON
        /// </summary>
        public const char ugr = '\u03C5';

        /// <summary>
        /// GREEK CAPITAL LETTER XI
        /// </summary>
        public const char Xgr = '\u039E';

        /// <summary>
        /// GREEK SMALL LETTER XI
        /// </summary>
        public const char xgr = '\u03BE';

        /// <summary>
        /// GREEK CAPITAL LETTER ZETA
        /// </summary>
        public const char Zgr = '\u0396';

        /// <summary>
        /// GREEK SMALL LETTER ZETA
        /// </summary>
        public const char zgr = '\u03B6';

        #endregion

        #region Monotoniko Greek Entities (ISOgrk2)

        /// <summary>
        /// GREEK CAPITAL LETTER ALPHA WITH TONOS
        /// </summary>
        public const char Aacgr = '\u0386';

        /// <summary>
        /// GREEK SMALL LETTER ALPHA WITH TONOS
        /// </summary>
        public const char aacgr = '\u03AC';

        /// <summary>
        /// GREEK CAPITAL LETTER EPSILON WITH TONOS
        /// </summary>
        public const char Eacgr = '\u0388';

        /// <summary>
        /// GREEK SMALL LETTER EPSILON WITH TONOS
        /// </summary>
        public const char eacgr = '\u03AD';

        /// <summary>
        /// GREEK CAPITAL LETTER ETA WITH TONOS
        /// </summary>
        public const char EEacgr = '\u0389';

        /// <summary>
        /// GREEK SMALL LETTER ETA WITH TONOS
        /// </summary>
        public const char eeacgr = '\u03AE';

        /// <summary>
        /// GREEK CAPITAL LETTER IOTA WITH TONOS
        /// </summary>
        public const char Iacgr = '\u038A';

        /// <summary>
        /// GREEK SMALL LETTER IOTA WITH TONOS
        /// </summary>
        public const char iacgr = '\u03AF';

        /// <summary>
        /// GREEK SMALL LETTER IOTA WITH DIALYTIKA AND TONOS
        /// </summary>
        public const char idiagr = '\u0390';

        /// <summary>
        /// GREEK CAPITAL LETTER IOTA WITH DIALYTIKA
        /// </summary>
        public const char Idigr = '\u03AA';

        /// <summary>
        /// GREEK SMALL LETTER IOTA WITH DIALYTIKA
        /// </summary>
        public const char idigr = '\u03CA';

        /// <summary>
        /// GREEK CAPITAL LETTER OMICRON WITH TONOS
        /// </summary>
        public const char Oacgr = '\u038C';

        /// <summary>
        /// GREEK SMALL LETTER OMICRON WITH TONOS
        /// </summary>
        public const char oacgr = '\u03CC';

        /// <summary>
        /// GREEK CAPITAL LETTER OMEGA WITH TONOS
        /// </summary>
        public const char OHacgr = '\u038F';

        /// <summary>
        /// GREEK SMALL LETTER OMEGA WITH TONOS
        /// </summary>
        public const char ohacgr = '\u03CE';

        /// <summary>
        /// GREEK CAPITAL LETTER UPSILON WITH TONOS
        /// </summary>
        public const char Uacgr = '\u038E';

        /// <summary>
        /// GREEK SMALL LETTER UPSILON WITH TONOS
        /// </summary>
        public const char uacgr = '\u03CD';

        /// <summary>
        /// GREEK SMALL LETTER UPSILON WITH DIALYTIKA AND TONOS
        /// </summary>
        public const char udiagr = '\u03B0';

        /// <summary>
        /// GREEK CAPITAL LETTER UPSILON WITH DIALYTIKA
        /// </summary>
        public const char Udigr = '\u03AB';

        /// <summary>
        /// GREEK SMALL LETTER UPSILON WITH DIALYTIKA
        /// </summary>
        public const char udigr = '\u03CB';

        #endregion

        #region Greek Symbols Entities (ISOgrk3)

        /// <summary>
        /// GREEK SMALL LETTER ALPHA
        /// </summary>
        public const char alpha = '\u03B1';

        /// <summary>
        /// GREEK SMALL LETTER BETA
        /// </summary>
        public const char beta = '\u03B2';

        /// <summary>
        /// GREEK SMALL LETTER CHI
        /// </summary>
        public const char chi = '\u03C7';

        /// <summary>
        /// GREEK CAPITAL LETTER DELTA
        /// </summary>
        public const char Delta = '\u0394';

        /// <summary>
        /// GREEK SMALL LETTER DELTA
        /// </summary>
        public const char delta = '\u03B4';

        /// <summary>
        /// GREEK SMALL LETTER EPSILON
        /// </summary>
        public const char epsi = '\u03B5';

        /// <summary>
        /// SMALL ELEMENT OF
        /// </summary>
        public const char epsis = '\u220A';

        ///// <summary>
        ///// variant epsilon
        ///// </summary>
        //public const char epsiv = '\u????';

        /// <summary>
        /// GREEK SMALL LETTER ETA
        /// </summary>
        public const char eta = '\u03B7';

        /// <summary>
        /// GREEK CAPITAL LETTER GAMMA
        /// </summary>
        public const char Gamma = '\u0393';

        /// <summary>
        /// GREEK SMALL LETTER GAMMA
        /// </summary>
        public const char gamma = '\u03B3';

        /// <summary>
        /// GREEK LETTER DIGAMMA
        /// </summary>
        public const char gammad = '\u03DC';

        /// <summary>
        /// GREEK SMALL LETTER IOTA
        /// </summary>
        public const char iota = '\u03B9';

        /// <summary>
        /// GREEK SMALL LETTER KAPPA
        /// </summary>
        public const char kappa = '\u03BA';

        /// <summary>
        /// GREEK KAPPA SYMBOL
        /// </summary>
        public const char kappav = '\u03F0';

        /// <summary>
        /// GREEK CAPITAL LETTER LAMDA
        /// </summary>
        public const char Lambda = '\u039B';

        /// <summary>
        /// GREEK SMALL LETTER LAMDA
        /// </summary>
        public const char lambda = '\u03BB';

        /// <summary>
        /// GREEK SMALL LETTER MU
        /// </summary>
        public const char mu = '\u03BC';

        /// <summary>
        /// GREEK SMALL LETTER NU
        /// </summary>
        public const char nu = '\u03BD';

        /// <summary>
        /// GREEK CAPITAL LETTER OMEGA
        /// </summary>
        public const char Omega = '\u03A9';

        /// <summary>
        /// GREEK SMALL LETTER OMEGA
        /// </summary>
        public const char omega = '\u03C9';

        /// <summary>
        /// GREEK CAPITAL LETTER PHI
        /// </summary>
        public const char Phi = '\u03A6';

        /// <summary>
        /// GREEK SMALL LETTER PHI
        /// </summary>
        public const char phis = '\u03C6';

        /// <summary>
        /// GREEK PHI SYMBOL
        /// </summary>
        public const char phiv = '\u03D5';

        /// <summary>
        /// GREEK CAPITAL LETTER PI
        /// </summary>
        public const char Pi = '\u03A0';

        /// <summary>
        /// GREEK SMALL LETTER PI
        /// </summary>
        public const char pi = '\u03C0';

        /// <summary>
        /// GREEK PI SYMBOL
        /// </summary>
        public const char piv = '\u03D6';

        /// <summary>
        /// GREEK CAPITAL LETTER PSI
        /// </summary>
        public const char Psi = '\u03A8';

        /// <summary>
        /// GREEK SMALL LETTER PSI
        /// </summary>
        public const char psi = '\u03C8';

        /// <summary>
        /// GREEK SMALL LETTER RHO
        /// </summary>
        public const char rho = '\u03C1';

        /// <summary>
        /// GREEK RHO SYMBOL
        /// </summary>
        public const char rhov = '\u03F1';

        /// <summary>
        /// GREEK CAPITAL LETTER SIGMA
        /// </summary>
        public const char Sigma = '\u03A3';

        /// <summary>
        /// GREEK SMALL LETTER SIGMA
        /// </summary>
        public const char sigma = '\u03C3';

        /// <summary>
        /// GREEK SMALL LETTER FINAL SIGMA
        /// </summary>
        public const char sigmav = '\u03C2';

        /// <summary>
        /// GREEK SMALL LETTER TAU
        /// </summary>
        public const char tau = '\u03C4';

        /// <summary>
        /// GREEK CAPITAL LETTER THETA
        /// </summary>
        public const char Theta = '\u0398';

        /// <summary>
        /// GREEK SMALL LETTER THETA
        /// </summary>
        public const char thetas = '\u03B8';

        /// <summary>
        /// GREEK THETA SYMBOL
        /// </summary>
        public const char thetav = '\u03D1';

        /// <summary>
        /// GREEK CAPITAL LETTER UPSILON
        /// </summary>
        public const char Upsi = '\u03A5';

        /// <summary>
        /// GREEK SMALL LETTER UPSILON
        /// </summary>
        public const char upsi = '\u03C5';

        /// <summary>
        /// GREEK CAPITAL LETTER XI
        /// </summary>
        public const char Xi = '\u039E';

        /// <summary>
        /// GREEK SMALL LETTER XI
        /// </summary>
        public const char xi = '\u03BE';

        /// <summary>
        /// GREEK SMALL LETTER ZETA
        /// </summary>
        public const char zeta = '\u03B6';

        #endregion

        #region Alternative Greek Symbols Entities (ISOgrk4)

        ///// <summary>
        ///// GREEK SMALL LETTER ALPHA
        ///// </summary>
        //public const char b.alpha = '\u03B1';

        ///// <summary>
        ///// GREEK SMALL LETTER BETA
        ///// </summary>
        //public const char b.beta = '\u03B2';

        ///// <summary>
        ///// GREEK SMALL LETTER CHI
        ///// </summary>
        //public const char b.chi = '\u03C7';

        ///// <summary>
        ///// GREEK CAPITAL LETTER DELTA
        ///// </summary>
        //public const char b.Delta = '\u0394';

        ///// <summary>
        ///// GREEK SMALL LETTER DELTA
        ///// </summary>
        //public const char b.delta = '\u03B4';

        ///// <summary>
        ///// GREEK SMALL LETTER EPSILON
        ///// </summary>
        //public const char b.epsi = '\u03B5';

        ///// <summary>
        ///// GREEK SMALL LETTER EPSILON
        ///// </summary>
        //public const char b.epsis = '\u03B5';

        ///// <summary>
        ///// GREEK SMALL LETTER EPSILON
        ///// </summary>
        //public const char b.epsiv = '\u03B5';

        ///// <summary>
        ///// GREEK SMALL LETTER ETA
        ///// </summary>
        //public const char b.eta = '\u03B7';

        ///// <summary>
        ///// GREEK CAPITAL LETTER GAMMA
        ///// </summary>
        //public const char b.Gamma = '\u0393';

        ///// <summary>
        ///// GREEK SMALL LETTER GAMMA
        ///// </summary>
        //public const char b.gamma = '\u03B3';

        ///// <summary>
        ///// GREEK LETTER DIGAMMA
        ///// </summary>
        //public const char b.gammad = '\u03DC';

        ///// <summary>
        ///// GREEK SMALL LETTER IOTA
        ///// </summary>
        //public const char b.iota = '\u03B9';

        ///// <summary>
        ///// GREEK SMALL LETTER KAPPA
        ///// </summary>
        //public const char b.kappa = '\u03BA';

        ///// <summary>
        ///// GREEK KAPPA SYMBOL
        ///// </summary>
        //public const char b.kappav = '\u03F0';

        ///// <summary>
        ///// GREEK CAPITAL LETTER LAMDA
        ///// </summary>
        //public const char b.Lambda = '\u039B';

        ///// <summary>
        ///// GREEK SMALL LETTER LAMDA
        ///// </summary>
        //public const char b.lambda = '\u03BB';

        ///// <summary>
        ///// GREEK SMALL LETTER MU
        ///// </summary>
        //public const char b.mu = '\u03BC';

        ///// <summary>
        ///// GREEK SMALL LETTER NU
        ///// </summary>
        //public const char b.nu = '\u03BD';

        ///// <summary>
        ///// GREEK CAPITAL LETTER OMEGA
        ///// </summary>
        //public const char b.Omega = '\u03A9';

        ///// <summary>
        ///// GREEK SMALL LETTER OMEGA WITH TONOS
        ///// </summary>
        //public const char b.omega = '\u03CE';

        ///// <summary>
        ///// GREEK CAPITAL LETTER PHI
        ///// </summary>
        //public const char b.Phi = '\u03A6';

        ///// <summary>
        ///// GREEK SMALL LETTER PHI
        ///// </summary>
        //public const char b.phis = '\u03C6';

        ///// <summary>
        ///// GREEK PHI SYMBOL
        ///// </summary>
        //public const char b.phiv = '\u03D5';

        ///// <summary>
        ///// GREEK CAPITAL LETTER PI
        ///// </summary>
        //public const char b.Pi = '\u03A0';

        ///// <summary>
        ///// GREEK SMALL LETTER PI
        ///// </summary>
        //public const char b.pi = '\u03C0';

        ///// <summary>
        ///// GREEK PI SYMBOL
        ///// </summary>
        //public const char b.piv = '\u03D6';

        ///// <summary>
        ///// GREEK CAPITAL LETTER PSI
        ///// </summary>
        //public const char b.Psi = '\u03A8';

        ///// <summary>
        ///// GREEK SMALL LETTER PSI
        ///// </summary>
        //public const char b.psi = '\u03C8';

        ///// <summary>
        ///// GREEK SMALL LETTER RHO
        ///// </summary>
        //public const char b.rho = '\u03C1';

        ///// <summary>
        ///// GREEK RHO SYMBOL
        ///// </summary>
        //public const char b.rhov = '\u03F1';

        ///// <summary>
        ///// GREEK CAPITAL LETTER SIGMA
        ///// </summary>
        //public const char b.Sigma = '\u03A3';

        ///// <summary>
        ///// GREEK SMALL LETTER SIGMA
        ///// </summary>
        //public const char b.sigma = '\u03C3';

        ///// <summary>
        ///// GREEK SMALL LETTER FINAL SIGMA
        ///// </summary>
        //public const char b.sigmav = '\u03C2';

        ///// <summary>
        ///// GREEK SMALL LETTER TAU
        ///// </summary>
        //public const char b.tau = '\u03C4';

        ///// <summary>
        ///// GREEK CAPITAL LETTER THETA
        ///// </summary>
        //public const char b.Theta = '\u0398';

        ///// <summary>
        ///// GREEK SMALL LETTER THETA
        ///// </summary>
        //public const char b.thetas = '\u03B8';

        ///// <summary>
        ///// GREEK THETA SYMBOL
        ///// </summary>
        //public const char b.thetav = '\u03D1';

        ///// <summary>
        ///// GREEK CAPITAL LETTER UPSILON
        ///// </summary>
        //public const char b.Upsi = '\u03A5';

        ///// <summary>
        ///// GREEK SMALL LETTER UPSILON
        ///// </summary>
        //public const char b.upsi = '\u03C5';

        ///// <summary>
        ///// GREEK CAPITAL LETTER XI
        ///// </summary>
        //public const char b.Xi = '\u039E';

        ///// <summary>
        ///// GREEK SMALL LETTER XI
        ///// </summary>
        //public const char b.xi = '\u03BE';

        ///// <summary>
        ///// GREEK SMALL LETTER ZETA
        ///// </summary>
        //public const char b.zeta = '\u03B6';

        #endregion

        #region Russian Cyrillic Entities (ISOcyr1)

        /// <summary>
        /// CYRILLIC CAPITAL LETTER A
        /// </summary>
        public const char Acy = '\u0410';

        /// <summary>
        /// CYRILLIC SMALL LETTER A
        /// </summary>
        public const char acy = '\u0430';

        /// <summary>
        /// CYRILLIC CAPITAL LETTER BE
        /// </summary>
        public const char Bcy = '\u0411';

        /// <summary>
        /// CYRILLIC SMALL LETTER BE
        /// </summary>
        public const char bcy = '\u0431';

        /// <summary>
        /// CYRILLIC CAPITAL LETTER CHE
        /// </summary>
        public const char CHcy = '\u0427';

        /// <summary>
        /// CYRILLIC SMALL LETTER CHE
        /// </summary>
        public const char chcy = '\u0447';

        /// <summary>
        /// CYRILLIC CAPITAL LETTER DE
        /// </summary>
        public const char Dcy = '\u0414';

        /// <summary>
        /// CYRILLIC SMALL LETTER DE
        /// </summary>
        public const char dcy = '\u0434';

        /// <summary>
        /// CYRILLIC CAPITAL LETTER E
        /// </summary>
        public const char Ecy = '\u042D';

        /// <summary>
        /// CYRILLIC SMALL LETTER E
        /// </summary>
        public const char ecy = '\u044D';

        /// <summary>
        /// CYRILLIC CAPITAL LETTER EF
        /// </summary>
        public const char Fcy = '\u0424';

        /// <summary>
        /// CYRILLIC SMALL LETTER EF
        /// </summary>
        public const char fcy = '\u0444';

        /// <summary>
        /// CYRILLIC CAPITAL LETTER GHE
        /// </summary>
        public const char Gcy = '\u0413';

        /// <summary>
        /// CYRILLIC SMALL LETTER GHE
        /// </summary>
        public const char gcy = '\u0433';

        /// <summary>
        /// CYRILLIC CAPITAL LETTER HARD SIGN
        /// </summary>
        public const char HARDcy = '\u042A';

        /// <summary>
        /// CYRILLIC SMALL LETTER HARD SIGN
        /// </summary>
        public const char hardcy = '\u044A';

        /// <summary>
        /// CYRILLIC CAPITAL LETTER I
        /// </summary>
        public const char Icy = '\u0418';

        /// <summary>
        /// CYRILLIC SMALL LETTER I
        /// </summary>
        public const char icy = '\u0438';

        /// <summary>
        /// CYRILLIC CAPITAL LETTER IE
        /// </summary>
        public const char IEcy = '\u0415';

        /// <summary>
        /// CYRILLIC SMALL LETTER IE
        /// </summary>
        public const char iecy = '\u0435';

        /// <summary>
        /// CYRILLIC CAPITAL LETTER IO
        /// </summary>
        public const char IOcy = '\u0401';

        /// <summary>
        /// CYRILLIC SMALL LETTER IO
        /// </summary>
        public const char iocy = '\u0451';

        /// <summary>
        /// CYRILLIC CAPITAL LETTER SHORT I
        /// </summary>
        public const char Jcy = '\u0419';

        /// <summary>
        /// CYRILLIC SMALL LETTER SHORT I
        /// </summary>
        public const char jcy = '\u0439';

        /// <summary>
        /// CYRILLIC CAPITAL LETTER KA
        /// </summary>
        public const char Kcy = '\u041A';

        /// <summary>
        /// CYRILLIC SMALL LETTER KA
        /// </summary>
        public const char kcy = '\u043A';

        /// <summary>
        /// CYRILLIC CAPITAL LETTER HA
        /// </summary>
        public const char KHcy = '\u0425';

        /// <summary>
        /// CYRILLIC SMALL LETTER HA
        /// </summary>
        public const char khcy = '\u0445';

        /// <summary>
        /// CYRILLIC CAPITAL LETTER EL
        /// </summary>
        public const char Lcy = '\u041B';

        /// <summary>
        /// CYRILLIC SMALL LETTER EL
        /// </summary>
        public const char lcy = '\u043B';

        /// <summary>
        /// CYRILLIC CAPITAL LETTER EM
        /// </summary>
        public const char Mcy = '\u041C';

        /// <summary>
        /// CYRILLIC SMALL LETTER EM
        /// </summary>
        public const char mcy = '\u043C';

        /// <summary>
        /// CYRILLIC CAPITAL LETTER EN
        /// </summary>
        public const char Ncy = '\u041D';

        /// <summary>
        /// CYRILLIC SMALL LETTER EN
        /// </summary>
        public const char ncy = '\u043D';

        /// <summary>
        /// NUMERO SIGN
        /// </summary>
        public const char numero = '\u2116';

        /// <summary>
        /// CYRILLIC CAPITAL LETTER O
        /// </summary>
        public const char Ocy = '\u041E';

        /// <summary>
        /// CYRILLIC SMALL LETTER O
        /// </summary>
        public const char ocy = '\u043E';

        /// <summary>
        /// CYRILLIC CAPITAL LETTER PE
        /// </summary>
        public const char Pcy = '\u041F';

        /// <summary>
        /// CYRILLIC SMALL LETTER PE
        /// </summary>
        public const char pcy = '\u043F';

        /// <summary>
        /// CYRILLIC CAPITAL LETTER ER
        /// </summary>
        public const char Rcy = '\u0420';

        /// <summary>
        /// CYRILLIC SMALL LETTER ER
        /// </summary>
        public const char rcy = '\u0440';

        /// <summary>
        /// CYRILLIC CAPITAL LETTER ES
        /// </summary>
        public const char Scy = '\u0421';

        /// <summary>
        /// CYRILLIC SMALL LETTER ES
        /// </summary>
        public const char scy = '\u0441';

        /// <summary>
        /// CYRILLIC CAPITAL LETTER SHCHA
        /// </summary>
        public const char SHCHcy = '\u0429';

        /// <summary>
        /// CYRILLIC SMALL LETTER SHCHA
        /// </summary>
        public const char shchcy = '\u0449';

        /// <summary>
        /// CYRILLIC CAPITAL LETTER SHA
        /// </summary>
        public const char SHcy = '\u0428';

        /// <summary>
        /// CYRILLIC SMALL LETTER SHA
        /// </summary>
        public const char shcy = '\u0448';

        /// <summary>
        /// CYRILLIC CAPITAL LETTER SOFT SIGN
        /// </summary>
        public const char SOFTcy = '\u042C';

        /// <summary>
        /// CYRILLIC SMALL LETTER SOFT SIGN
        /// </summary>
        public const char softcy = '\u044C';

        /// <summary>
        /// CYRILLIC CAPITAL LETTER TE
        /// </summary>
        public const char Tcy = '\u0422';

        /// <summary>
        /// CYRILLIC SMALL LETTER TE
        /// </summary>
        public const char tcy = '\u0442';

        /// <summary>
        /// CYRILLIC CAPITAL LETTER TSE
        /// </summary>
        public const char TScy = '\u0426';

        /// <summary>
        /// CYRILLIC SMALL LETTER TSE
        /// </summary>
        public const char tscy = '\u0446';

        /// <summary>
        /// CYRILLIC CAPITAL LETTER U
        /// </summary>
        public const char Ucy = '\u0423';

        /// <summary>
        /// CYRILLIC SMALL LETTER U
        /// </summary>
        public const char ucy = '\u0443';

        /// <summary>
        /// CYRILLIC CAPITAL LETTER VE
        /// </summary>
        public const char Vcy = '\u0412';

        /// <summary>
        /// CYRILLIC SMALL LETTER VE
        /// </summary>
        public const char vcy = '\u0432';

        /// <summary>
        /// CYRILLIC CAPITAL LETTER YA
        /// </summary>
        public const char YAcy = '\u042F';

        /// <summary>
        /// CYRILLIC SMALL LETTER YA
        /// </summary>
        public const char yacy = '\u044F';

        /// <summary>
        /// CYRILLIC CAPITAL LETTER YERU
        /// </summary>
        public const char Ycy = '\u042B';

        /// <summary>
        /// CYRILLIC SMALL LETTER YERU
        /// </summary>
        public const char ycy = '\u044B';

        /// <summary>
        /// CYRILLIC CAPITAL LETTER YU
        /// </summary>
        public const char YUcy = '\u042E';

        /// <summary>
        /// CYRILLIC SMALL LETTER YU
        /// </summary>
        public const char yucy = '\u044E';

        /// <summary>
        /// CYRILLIC CAPITAL LETTER ZE
        /// </summary>
        public const char Zcy = '\u0417';

        /// <summary>
        /// CYRILLIC SMALL LETTER ZE
        /// </summary>
        public const char zcy = '\u0437';

        /// <summary>
        /// CYRILLIC CAPITAL LETTER ZHE
        /// </summary>
        public const char ZHcy = '\u0416';

        /// <summary>
        /// CYRILLIC SMALL LETTER ZHE
        /// </summary>
        public const char zhcy = '\u0436';

        #endregion

        #region Non-Russian Cyrillic Entities (ISOcyr2)

        /// <summary>
        /// CYRILLIC CAPITAL LETTER DJE
        /// </summary>
        public const char DJcy = '\u0402';

        /// <summary>
        /// CYRILLIC SMALL LETTER DJE
        /// </summary>
        public const char djcy = '\u0452';

        /// <summary>
        /// CYRILLIC CAPITAL LETTER DZE
        /// </summary>
        public const char DScy = '\u0405';

        /// <summary>
        /// CYRILLIC SMALL LETTER DZE
        /// </summary>
        public const char dscy = '\u0455';

        /// <summary>
        /// CYRILLIC CAPITAL LETTER DZHE
        /// </summary>
        public const char DZcy = '\u040F';

        /// <summary>
        /// CYRILLIC SMALL LETTER DZHE
        /// </summary>
        public const char dzcy = '\u045F';

        /// <summary>
        /// CYRILLIC CAPITAL LETTER GJE
        /// </summary>
        public const char GJcy = '\u0403';

        /// <summary>
        /// CYRILLIC SMALL LETTER GJE
        /// </summary>
        public const char gjcy = '\u0453';

        /// <summary>
        /// CYRILLIC CAPITAL LETTER BYELORUSSIAN-UKRAINIAN I
        /// </summary>
        public const char Iukcy = '\u0406';

        /// <summary>
        /// CYRILLIC SMALL LETTER BYELORUSSIAN-UKRAINIAN I
        /// </summary>
        public const char iukcy = '\u0456';

        /// <summary>
        /// CYRILLIC CAPITAL LETTER JE
        /// </summary>
        public const char Jsercy = '\u0408';

        /// <summary>
        /// CYRILLIC SMALL LETTER JE
        /// </summary>
        public const char jsercy = '\u0458';

        /// <summary>
        /// CYRILLIC CAPITAL LETTER UKRAINIAN IE
        /// </summary>
        public const char Jukcy = '\u0404';

        /// <summary>
        /// CYRILLIC SMALL LETTER UKRAINIAN IE
        /// </summary>
        public const char jukcy = '\u0454';

        /// <summary>
        /// CYRILLIC CAPITAL LETTER KJE
        /// </summary>
        public const char KJcy = '\u040C';

        /// <summary>
        /// CYRILLIC SMALL LETTER KJE
        /// </summary>
        public const char kjcy = '\u045C';

        /// <summary>
        /// CYRILLIC CAPITAL LETTER LJE
        /// </summary>
        public const char LJcy = '\u0409';

        /// <summary>
        /// CYRILLIC SMALL LETTER LJE
        /// </summary>
        public const char ljcy = '\u0459';

        /// <summary>
        /// CYRILLIC CAPITAL LETTER NJE
        /// </summary>
        public const char NJcy = '\u040A';

        /// <summary>
        /// CYRILLIC SMALL LETTER NJE
        /// </summary>
        public const char njcy = '\u045A';

        /// <summary>
        /// CYRILLIC CAPITAL LETTER TSHE
        /// </summary>
        public const char TSHcy = '\u040B';

        /// <summary>
        /// CYRILLIC SMALL LETTER TSHE
        /// </summary>
        public const char tshcy = '\u045B';

        /// <summary>
        /// CYRILLIC CAPITAL LETTER SHORT U
        /// </summary>
        public const char Ubrcy = '\u040E';

        /// <summary>
        /// CYRILLIC SMALL LETTER SHORT U
        /// </summary>
        public const char ubrcy = '\u045E';

        /// <summary>
        /// CYRILLIC CAPITAL LETTER YI
        /// </summary>
        public const char YIcy = '\u0407';

        /// <summary>
        /// CYRILLIC SMALL LETTER YI
        /// </summary>
        public const char yicy = '\u0457';

        #endregion

        #region Diacritical Marks Entities (ISOdia)

        /// <summary>
        /// ACUTE ACCENT
        /// </summary>
        public const char acute = '\u00B4';

        /// <summary>
        /// BREVE
        /// </summary>
        public const char breve = '\u02D8';

        /// <summary>
        /// CARON
        /// </summary>
        public const char caron = '\u02C7';

        /// <summary>
        /// CEDILLA
        /// </summary>
        public const char cedil = '\u00B8';

        /// <summary>
        /// MODIFIER LETTER CIRCUMFLEX ACCENT
        /// </summary>
        public const char circ = '\u02C6';

        /// <summary>
        /// DOUBLE ACUTE ACCENT
        /// </summary>
        public const char dblac = '\u02DD';

        /// <summary>
        /// DIAERESIS
        /// </summary>
        public const char die = '\u00A8';

        /// <summary>
        /// DOT ABOVE
        /// </summary>
        public const char dot = '\u02D9';

        /// <summary>
        /// GRAVE ACCENT
        /// </summary>
        public const char grave = '\u0060';

        /// <summary>
        /// MACRON
        /// </summary>
        public const char macr = '\u00AF';

        /// <summary>
        /// OGONEK
        /// </summary>
        public const char ogon = '\u02DB';

        /// <summary>
        /// RING ABOVE
        /// </summary>
        public const char ring = '\u02DA';

        /// <summary>
        /// SMALL TILDE
        /// </summary>
        public const char tilde = '\u02DC';

        /// <summary>
        /// DIAERESIS
        /// </summary>
        public const char uml = '\u00A8';

        #endregion

        #region General Technical Entities (ISOtech)

        /// <summary>
        /// ALEF SYMBOL
        /// </summary>
        public const char aleph = '\u2135';

        /// <summary>
        /// LOGICAL AND
        /// </summary>
        public const char and = '\u2227';

        /// <summary>
        /// RIGHT ANGLE
        /// </summary>
        public const char ang90 = '\u221F';

        /// <summary>
        /// SPHERICAL ANGLE
        /// </summary>
        public const char angsph = '\u2222';

        /// <summary>
        /// ANGSTROM SIGN
        /// </summary>
        public const char angst = '\u212B';

        /// <summary>
        /// ALMOST EQUAL TO
        /// </summary>
        public const char ap = '\u2248';

        /// <summary>
        /// BECAUSE
        /// </summary>
        public const char becaus = '\u2235';

        /// <summary>
        /// SCRIPT CAPITAL B
        /// </summary>
        public const char bernou = '\u212C';

        /// <summary>
        /// UP TACK
        /// </summary>
        public const char bottom = '\u22A5';

        /// <summary>
        /// INTERSECTION
        /// </summary>
        public const char cap = '\u2229';

        /// <summary>
        /// RING OPERATOR
        /// </summary>
        public const char compfn = '\u2218';

        /// <summary>
        /// APPROXIMATELY EQUAL TO
        /// </summary>
        public const char cong = '\u2245';

        /// <summary>
        /// CONTOUR INTEGRAL
        /// </summary>
        public const char conint = '\u222E';

        /// <summary>
        /// UNION
        /// </summary>
        public const char cup = '\u222A';

        /// <summary>
        /// DIAERESIS
        /// </summary>
        public const char Dot = '\u00A8';

        /// <summary>
        /// COMBINING FOUR DOTS ABOVE
        /// </summary>
        public const char DotDot = '\u20DC';

        /// <summary>
        /// IDENTICAL TO
        /// </summary>
        public const char equiv = '\u2261';

        /// <summary>
        /// THERE EXISTS
        /// </summary>
        public const char exist = '\u2203';

        /// <summary>
        /// LATIN SMALL LETTER F WITH HOOK
        /// </summary>
        public const char fnof = '\u0192';

        /// <summary>
        /// FOR ALL
        /// </summary>
        public const char forall = '\u2200';

        /// <summary>
        /// GREATER-THAN OR EQUAL TO
        /// </summary>
        public const char ge = '\u2265';

        /// <summary>
        /// SCRIPT CAPITAL H
        /// </summary>
        public const char hamilt = '\u210B';

        /// <summary>
        /// LEFT RIGHT DOUBLE ARROW
        /// </summary>
        public const char iff = '\u21D4';

        /// <summary>
        /// INFINITY
        /// </summary>
        public const char infin = '\u221E';

        /// <summary>
        /// INTEGRAL
        /// </summary>
        public const char @int = '\u222B';

        /// <summary>
        /// ELEMENT OF
        /// </summary>
        public const char isin = '\u2208';

        /// <summary>
        /// SCRIPT CAPITAL L
        /// </summary>
        public const char lagran = '\u2112';

        /// <summary>
        /// LEFT-POINTING ANGLE BRACKET
        /// </summary>
        public const char lang = '\u2329';

        /// <summary>
        /// LEFTWARDS DOUBLE ARROW
        /// </summary>
        public const char lArr = '\u21D0';

        /// <summary>
        /// LESS-THAN OR EQUAL TO
        /// </summary>
        public const char le = '\u2264';

        /// <summary>
        /// ASTERISK OPERATOR
        /// </summary>
        public const char lowast = '\u2217';

        /// <summary>
        /// MINUS SIGN
        /// </summary>
        public const char minus = '\u2212';

        /// <summary>
        /// MINUS-OR-PLUS SIGN
        /// </summary>
        public const char mnplus = '\u2213';

        /// <summary>
        /// NABLA
        /// </summary>
        public const char nabla = '\u2207';

        /// <summary>
        /// NOT EQUAL TO
        /// </summary>
        public const char ne = '\u2260';

        /// <summary>
        /// CONTAINS AS MEMBER
        /// </summary>
        public const char ni = '\u220B';

        /// <summary>
        /// NOT AN ELEMENT OF
        /// </summary>
        public const char notin = '\u2209';

        /// <summary>
        /// LOGICAL OR
        /// </summary>
        public const char or = '\u2228';

        /// <summary>
        /// SCRIPT SMALL O
        /// </summary>
        public const char order = '\u2134';

        /// <summary>
        /// PARALLEL TO
        /// </summary>
        public const char par = '\u2225';

        /// <summary>
        /// PARTIAL DIFFERENTIAL
        /// </summary>
        public const char part = '\u2202';

        /// <summary>
        /// PER MILLE SIGN
        /// </summary>
        public const char permil = '\u2030';

        /// <summary>
        /// UP TACK
        /// </summary>
        public const char perp = '\u22A5';

        /// <summary>
        /// SCRIPT CAPITAL M
        /// </summary>
        public const char phmmat = '\u2133';

        /// <summary>
        /// PRIME
        /// </summary>
        public const char prime = '\u2032';

        /// <summary>
        /// DOUBLE PRIME
        /// </summary>
        public const char Prime = '\u2033';

        /// <summary>
        /// PROPORTIONAL TO
        /// </summary>
        public const char prop = '\u221D';

        /// <summary>
        /// SQUARE ROOT
        /// </summary>
        public const char radic = '\u221A';

        /// <summary>
        /// RIGHT-POINTING ANGLE BRACKET
        /// </summary>
        public const char rang = '\u232A';

        /// <summary>
        /// RIGHTWARDS DOUBLE ARROW
        /// </summary>
        public const char rArr = '\u21D2';

        /// <summary>
        /// TILDE OPERATOR
        /// </summary>
        public const char sim = '\u223C';

        /// <summary>
        /// ASYMPTOTICALLY EQUAL TO
        /// </summary>
        public const char sime = '\u2243';

        /// <summary>
        /// WHITE SQUARE
        /// </summary>
        public const char square = '\u25A1';

        /// <summary>
        /// SUBSET OF
        /// </summary>
        public const char sub = '\u2282';

        /// <summary>
        /// SUBSET OF OR EQUAL TO
        /// </summary>
        public const char sube = '\u2286';

        /// <summary>
        /// SUPERSET OF
        /// </summary>
        public const char sup = '\u2283';

        /// <summary>
        /// SUPERSET OF OR EQUAL TO
        /// </summary>
        public const char supe = '\u2287';

        /// <summary>
        /// COMBINING THREE DOTS ABOVE
        /// </summary>
        public const char tdot = '\u20DB';

        /// <summary>
        /// THEREFORE
        /// </summary>
        public const char there4 = '\u2234';

        /// <summary>
        /// TRIPLE PRIME
        /// </summary>
        public const char tprime = '\u2034';

        /// <summary>
        /// DOUBLE VERTICAL LINE
        /// </summary>
        public const char Verbar = '\u2016';

        /// <summary>
        /// ESTIMATES
        /// </summary>
        public const char wedgeq = '\u2259';

        #endregion

        #region Box and Line Drawing Entities (ISObox)

        /// <summary>
        /// BOX DRAWINGS LIGHT DOWN AND LEFT
        /// </summary>
        public const char boxdl = '\u2510';

        /// <summary>
        /// BOX DRAWINGS DOWN SINGLE AND LEFT DOUBLE
        /// </summary>
        public const char boxdL = '\u2555';

        /// <summary>
        /// BOX DRAWINGS DOWN DOUBLE AND LEFT SINGLE
        /// </summary>
        public const char boxDl = '\u2556';

        /// <summary>
        /// BOX DRAWINGS DOUBLE DOWN AND LEFT
        /// </summary>
        public const char boxDL = '\u2557';

        /// <summary>
        /// BOX DRAWINGS LIGHT DOWN AND RIGHT
        /// </summary>
        public const char boxdr = '\u250C';

        /// <summary>
        /// BOX DRAWINGS DOWN SINGLE AND RIGHT DOUBLE
        /// </summary>
        public const char boxdR = '\u2552';

        /// <summary>
        /// BOX DRAWINGS DOWN DOUBLE AND RIGHT SINGLE
        /// </summary>
        public const char boxDr = '\u2553';

        /// <summary>
        /// BOX DRAWINGS DOUBLE DOWN AND RIGHT
        /// </summary>
        public const char boxDR = '\u2554';

        /// <summary>
        /// BOX DRAWINGS LIGHT HORIZONTAL
        /// </summary>
        public const char boxh = '\u2500';

        /// <summary>
        /// BOX DRAWINGS DOUBLE HORIZONTAL
        /// </summary>
        public const char boxH = '\u2550';

        /// <summary>
        /// BOX DRAWINGS LIGHT DOWN AND HORIZONTAL
        /// </summary>
        public const char boxhd = '\u252C';

        /// <summary>
        /// BOX DRAWINGS DOWN SINGLE AND HORIZONTAL DOUBLE
        /// </summary>
        public const char boxHd = '\u2564';

        /// <summary>
        /// BOX DRAWINGS DOWN DOUBLE AND HORIZONTAL SINGLE
        /// </summary>
        public const char boxhD = '\u2565';

        /// <summary>
        /// BOX DRAWINGS DOUBLE DOWN AND HORIZONTAL
        /// </summary>
        public const char boxHD = '\u2566';

        /// <summary>
        /// BOX DRAWINGS LIGHT UP AND HORIZONTAL
        /// </summary>
        public const char boxhu = '\u2534';

        /// <summary>
        /// BOX DRAWINGS UP SINGLE AND HORIZONTAL DOUBLE
        /// </summary>
        public const char boxHu = '\u2567';

        /// <summary>
        /// BOX DRAWINGS UP DOUBLE AND HORIZONTAL SINGLE
        /// </summary>
        public const char boxhU = '\u2568';

        /// <summary>
        /// BOX DRAWINGS DOUBLE UP AND HORIZONTAL
        /// </summary>
        public const char boxHU = '\u2569';

        /// <summary>
        /// BOX DRAWINGS LIGHT UP AND LEFT
        /// </summary>
        public const char boxul = '\u2518';

        /// <summary>
        /// BOX DRAWINGS UP SINGLE AND LEFT DOUBLE
        /// </summary>
        public const char boxuL = '\u255B';

        /// <summary>
        /// BOX DRAWINGS UP DOUBLE AND LEFT SINGLE
        /// </summary>
        public const char boxUl = '\u255C';

        /// <summary>
        /// BOX DRAWINGS DOUBLE UP AND LEFT
        /// </summary>
        public const char boxUL = '\u255D';

        /// <summary>
        /// BOX DRAWINGS LIGHT UP AND RIGHT
        /// </summary>
        public const char boxur = '\u2514';

        /// <summary>
        /// BOX DRAWINGS UP SINGLE AND RIGHT DOUBLE
        /// </summary>
        public const char boxuR = '\u2558';

        /// <summary>
        /// BOX DRAWINGS UP DOUBLE AND RIGHT SINGLE
        /// </summary>
        public const char boxUr = '\u2559';

        /// <summary>
        /// BOX DRAWINGS DOUBLE UP AND RIGHT
        /// </summary>
        public const char boxUR = '\u255A';

        /// <summary>
        /// BOX DRAWINGS LIGHT VERTICAL
        /// </summary>
        public const char boxv = '\u2502';

        /// <summary>
        /// BOX DRAWINGS DOUBLE VERTICAL
        /// </summary>
        public const char boxV = '\u2551';

        /// <summary>
        /// BOX DRAWINGS LIGHT VERTICAL AND HORIZONTAL
        /// </summary>
        public const char boxvh = '\u253C';

        /// <summary>
        /// BOX DRAWINGS VERTICAL SINGLE AND HORIZONTAL DOUBLE
        /// </summary>
        public const char boxvH = '\u256A';

        /// <summary>
        /// BOX DRAWINGS VERTICAL DOUBLE AND HORIZONTAL SINGLE
        /// </summary>
        public const char boxVh = '\u256B';

        /// <summary>
        /// BOX DRAWINGS DOUBLE VERTICAL AND HORIZONTAL
        /// </summary>
        public const char boxVH = '\u256C';

        /// <summary>
        /// BOX DRAWINGS LIGHT VERTICAL AND LEFT
        /// </summary>
        public const char boxvl = '\u2524';

        /// <summary>
        /// BOX DRAWINGS VERTICAL SINGLE AND LEFT DOUBLE
        /// </summary>
        public const char boxvL = '\u2561';

        /// <summary>
        /// BOX DRAWINGS VERTICAL DOUBLE AND LEFT SINGLE
        /// </summary>
        public const char boxVl = '\u2562';

        /// <summary>
        /// BOX DRAWINGS DOUBLE VERTICAL AND LEFT
        /// </summary>
        public const char boxVL = '\u2563';

        /// <summary>
        /// BOX DRAWINGS LIGHT VERTICAL AND RIGHT
        /// </summary>
        public const char boxvr = '\u251C';

        /// <summary>
        /// BOX DRAWINGS VERTICAL SINGLE AND RIGHT DOUBLE
        /// </summary>
        public const char boxvR = '\u255E';

        /// <summary>
        /// BOX DRAWINGS VERTICAL DOUBLE AND RIGHT SINGLE
        /// </summary>
        public const char boxVr = '\u255F';

        /// <summary>
        /// BOX DRAWINGS DOUBLE VERTICAL AND RIGHT
        /// </summary>
        public const char boxVR = '\u2560';

        #endregion

        #region Numeric and Special Graphic Entities (ISOnum)

        /// <summary>
        /// AMPERSAND
        /// </summary>
        public const char amp = '\u0026';

        /// <summary>
        /// MODIFIER LETTER APOSTROPHE
        /// </summary>
        public const char apos = '\u02BC';

        /// <summary>
        /// ASTERISK
        /// </summary>
        public const char ast = '\u002A';

        /// <summary>
        /// BROKEN BAR
        /// </summary>
        public const char brvbar = '\u00A6';

        /// <summary>
        /// REVERSE SOLIDUS
        /// </summary>
        public const char bsol = '\u005C';

        /// <summary>
        /// CENT SIGN
        /// </summary>
        public const char cent = '\u00A2';

        /// <summary>
        /// COLON
        /// </summary>
        public const char colon = '\u003A';

        /// <summary>
        /// COMMA
        /// </summary>
        public const char comma = '\u002C';

        /// <summary>
        /// COMMERCIAL AT
        /// </summary>
        public const char commat = '\u0040';

        /// <summary>
        /// COPYRIGHT SIGN
        /// </summary>
        public const char copy = '\u00A9';

        /// <summary>
        /// CURRENCY SIGN
        /// </summary>
        public const char curren = '\u00A4';

        /// <summary>
        /// DOWNWARDS ARROW
        /// </summary>
        public const char darr = '\u2193';

        /// <summary>
        /// DEGREE SIGN
        /// </summary>
        public const char deg = '\u00B0';

        /// <summary>
        /// DIVISION SIGN
        /// </summary>
        public const char divide = '\u00F7';

        /// <summary>
        /// DOLLAR SIGN
        /// </summary>
        public const char dollar = '\u0024';

        /// <summary>
        /// EQUALS SIGN
        /// </summary>
        public const char equals = '\u003D';

        /// <summary>
        /// EXCLAMATION MARK
        /// </summary>
        public const char excl = '\u0021';

        /// <summary>
        /// VULGAR FRACTION ONE HALF
        /// </summary>
        public const char frac12 = '\u00BD';

        /// <summary>
        /// VULGAR FRACTION ONE QUARTER
        /// </summary>
        public const char frac14 = '\u00BC';

        /// <summary>
        /// VULGAR FRACTION ONE EIGHTH
        /// </summary>
        public const char frac18 = '\u215B';

        /// <summary>
        /// VULGAR FRACTION THREE QUARTERS
        /// </summary>
        public const char frac34 = '\u00BE';

        /// <summary>
        /// VULGAR FRACTION THREE EIGHTHS
        /// </summary>
        public const char frac38 = '\u215C';

        /// <summary>
        /// VULGAR FRACTION FIVE EIGHTHS
        /// </summary>
        public const char frac58 = '\u215D';

        /// <summary>
        /// VULGAR FRACTION SEVEN EIGHTHS
        /// </summary>
        public const char frac78 = '\u215E';

        /// <summary>
        /// GREATER-THAN SIGN
        /// </summary>
        public const char gt = '\u003E';

        /// <summary>
        /// VULGAR FRACTION ONE HALF
        /// </summary>
        public const char half = '\u00BD';

        /// <summary>
        /// HORIZONTAL BAR
        /// </summary>
        public const char horbar = '\u2015';

        /// <summary>
        /// HYPHEN-MINUS
        /// </summary>
        public const char hyphen = '\u002D';

        /// <summary>
        /// INVERTED EXCLAMATION MARK
        /// </summary>
        public const char iexcl = '\u00A1';

        /// <summary>
        /// INVERTED QUESTION MARK
        /// </summary>
        public const char iquest = '\u00BF';

        /// <summary>
        /// LEFT-POINTING DOUBLE ANGLE QUOTATION MARK
        /// </summary>
        public const char laquo = '\u00AB';

        /// <summary>
        /// LEFTWARDS ARROW
        /// </summary>
        public const char larr = '\u2190';

        /// <summary>
        /// LEFT CURLY BRACKET
        /// </summary>
        public const char lcub = '\u007B';

        /// <summary>
        /// LEFT DOUBLE QUOTATION MARK
        /// </summary>
        public const char ldquo = '\u201C';

        /// <summary>
        /// LOW LINE
        /// </summary>
        public const char lowbar = '\u005F';

        /// <summary>
        /// LEFT PARENTHESIS
        /// </summary>
        public const char lpar = '\u0028';

        /// <summary>
        /// LEFT SQUARE BRACKET
        /// </summary>
        public const char lsqb = '\u005B';

        /// <summary>
        /// LEFT SINGLE QUOTATION MARK
        /// </summary>
        public const char lsquo = '\u2018';

        /// <summary>
        /// LESS-THAN SIGN
        /// </summary>
        public const char lt = '\u003C';

        /// <summary>
        /// MICRO SIGN
        /// </summary>
        public const char micro = '\u00B5';

        /// <summary>
        /// MIDDLE DOT
        /// </summary>
        public const char middot = '\u00B7';

        /// <summary>
        /// NO-BREAK SPACE
        /// </summary>
        public const char nbsp = '\u00A0';

        /// <summary>
        /// NOT SIGN
        /// </summary>
        public const char not = '\u00AC';

        /// <summary>
        /// NUMBER SIGN
        /// </summary>
        public const char num = '\u0023';

        /// <summary>
        /// OHM SIGN
        /// </summary>
        public const char ohm = '\u2126';

        /// <summary>
        /// FEMININE ORDINAL INDICATOR
        /// </summary>
        public const char ordf = '\u00AA';

        /// <summary>
        /// MASCULINE ORDINAL INDICATOR
        /// </summary>
        public const char ordm = '\u00BA';

        /// <summary>
        /// PILCROW SIGN
        /// </summary>
        public const char para = '\u00B6';

        /// <summary>
        /// PERCENT SIGN
        /// </summary>
        public const char percnt = '\u0025';

        /// <summary>
        /// FULL STOP
        /// </summary>
        public const char period = '\u002E';

        /// <summary>
        /// PLUS SIGN
        /// </summary>
        public const char plus = '\u002B';

        /// <summary>
        /// PLUS-MINUS SIGN
        /// </summary>
        public const char plusmn = '\u00B1';

        /// <summary>
        /// POUND SIGN
        /// </summary>
        public const char pound = '\u00A3';

        /// <summary>
        /// QUESTION MARK
        /// </summary>
        public const char quest = '\u003F';

        /// <summary>
        /// QUOTATION MARK
        /// </summary>
        public const char quot = '\u0022';

        /// <summary>
        /// RIGHT-POINTING DOUBLE ANGLE QUOTATION MARK
        /// </summary>
        public const char raquo = '\u00BB';

        /// <summary>
        /// RIGHTWARDS ARROW
        /// </summary>
        public const char rarr = '\u2192';

        /// <summary>
        /// RIGHT CURLY BRACKET
        /// </summary>
        public const char rcub = '\u007D';

        /// <summary>
        /// RIGHT DOUBLE QUOTATION MARK
        /// </summary>
        public const char rdquo = '\u201D';

        /// <summary>
        /// REGISTERED SIGN
        /// </summary>
        public const char reg = '\u00AE';

        /// <summary>
        /// RIGHT PARENTHESIS
        /// </summary>
        public const char rpar = '\u0029';

        /// <summary>
        /// RIGHT SQUARE BRACKET
        /// </summary>
        public const char rsqb = '\u005D';

        /// <summary>
        /// RIGHT SINGLE QUOTATION MARK
        /// </summary>
        public const char rsquo = '\u2019';

        /// <summary>
        /// SECTION SIGN
        /// </summary>
        public const char sect = '\u00A7';

        /// <summary>
        /// SEMICOLON
        /// </summary>
        public const char semi = '\u003B';

        /// <summary>
        /// SOFT HYPHEN
        /// </summary>
        public const char shy = '\u00AD';

        /// <summary>
        /// SOLIDUS
        /// </summary>
        public const char sol = '\u002F';

        /// <summary>
        /// EIGHTH NOTE
        /// </summary>
        public const char sung = '\u266A';

        /// <summary>
        /// SUPERSCRIPT ONE
        /// </summary>
        public const char sup1 = '\u00B9';

        /// <summary>
        /// SUPERSCRIPT TWO
        /// </summary>
        public const char sup2 = '\u00B2';

        /// <summary>
        /// SUPERSCRIPT THREE
        /// </summary>
        public const char sup3 = '\u00B3';

        /// <summary>
        /// MULTIPLICATION SIGN
        /// </summary>
        public const char times = '\u00D7';

        /// <summary>
        /// TRADE MARK SIGN
        /// </summary>
        public const char trade = '\u2122';

        /// <summary>
        /// UPWARDS ARROW
        /// </summary>
        public const char uarr = '\u2191';

        /// <summary>
        /// VERTICAL LINE
        /// </summary>
        public const char verbar = '\u007C';

        /// <summary>
        /// YEN SIGN
        /// </summary>
        public const char yen = '\u00A5';

        #endregion

        #region Added Math Symbols: Arrow Relations Entities (ISOamsa)

        /// <summary>
        /// ANTICLOCKWISE TOP SEMICIRCLE ARROW
        /// </summary>
        public const char cularr = '\u21B6';

        /// <summary>
        /// CLOCKWISE TOP SEMICIRCLE ARROW
        /// </summary>
        public const char curarr = '\u21B7';

        /// <summary>
        /// DOWNWARDS DOUBLE ARROW
        /// </summary>
        public const char dArr = '\u21D3';

        /// <summary>
        /// DOWNWARDS PAIRED ARROWS
        /// </summary>
        public const char darr2 = '\u21CA';

        /// <summary>
        /// DOWNWARDS HARPOON WITH BARB LEFTWARDS
        /// </summary>
        public const char dharl = '\u21C3';

        /// <summary>
        /// DOWNWARDS HARPOON WITH BARB RIGHTWARDS
        /// </summary>
        public const char dharr = '\u21C2';

        /// <summary>
        /// SOUTH WEST ARROW
        /// </summary>
        public const char dlarr = '\u2199';

        /// <summary>
        /// SOUTH EAST ARROW
        /// </summary>
        public const char drarr = '\u2198';

        /// <summary>
        /// LEFT RIGHT ARROW
        /// </summary>
        public const char harr = '\u2194';

        /// <summary>
        /// LEFT RIGHT DOUBLE ARROW
        /// </summary>
        public const char hArr = '\u21D4';

        /// <summary>
        /// LEFT RIGHT WAVE ARROW
        /// </summary>
        public const char harrw = '\u21AD';

        /// <summary>
        /// LEFTWARDS TRIPLE ARROW
        /// </summary>
        public const char lAarr = '\u21DA';

        /// <summary>
        /// LEFTWARDS TWO HEADED ARROW
        /// </summary>
        public const char Larr = '\u219E';

        /// <summary>
        /// LEFTWARDS PAIRED ARROWS
        /// </summary>
        public const char larr2 = '\u21C7';

        /// <summary>
        /// LEFTWARDS ARROW WITH HOOK
        /// </summary>
        public const char larrhk = '\u21A9';

        /// <summary>
        /// LEFTWARDS ARROW WITH LOOP
        /// </summary>
        public const char larrlp = '\u21AB';

        /// <summary>
        /// LEFTWARDS ARROW WITH TAIL
        /// </summary>
        public const char larrtl = '\u21A2';

        /// <summary>
        /// LEFTWARDS HARPOON WITH BARB DOWNWARDS
        /// </summary>
        public const char lhard = '\u21BD';

        /// <summary>
        /// LEFTWARDS HARPOON WITH BARB UPWARDS
        /// </summary>
        public const char lharu = '\u21BC';

        /// <summary>
        /// LEFTWARDS ARROW OVER RIGHTWARDS ARROW
        /// </summary>
        public const char lrarr2 = '\u21C6';

        /// <summary>
        /// LEFTWARDS HARPOON OVER RIGHTWARDS HARPOON
        /// </summary>
        public const char lrhar2 = '\u21CB';

        /// <summary>
        /// UPWARDS ARROW WITH TIP LEFTWARDS
        /// </summary>
        public const char lsh = '\u21B0';

        /// <summary>
        /// RIGHTWARDS ARROW FROM BAR
        /// </summary>
        public const char map = '\u21A6';

        /// <summary>
        /// MULTIMAP
        /// </summary>
        public const char mumap = '\u22B8';

        /// <summary>
        /// NORTH EAST ARROW
        /// </summary>
        public const char nearr = '\u2197';

        /// <summary>
        /// LEFT RIGHT ARROW WITH STROKE
        /// </summary>
        public const char nharr = '\u21AE';

        /// <summary>
        /// LEFT RIGHT DOUBLE ARROW WITH STROKE
        /// </summary>
        public const char nhArr = '\u21CE';

        /// <summary>
        /// LEFTWARDS ARROW WITH STROKE
        /// </summary>
        public const char nlarr = '\u219A';

        /// <summary>
        /// LEFTWARDS DOUBLE ARROW WITH STROKE
        /// </summary>
        public const char nlArr = '\u21CD';

        /// <summary>
        /// RIGHTWARDS ARROW WITH STROKE
        /// </summary>
        public const char nrarr = '\u219B';

        /// <summary>
        /// RIGHTWARDS DOUBLE ARROW WITH STROKE
        /// </summary>
        public const char nrArr = '\u21CF';

        /// <summary>
        /// NORTH WEST ARROW
        /// </summary>
        public const char nwarr = '\u2196';

        /// <summary>
        /// ANTICLOCKWISE OPEN CIRCLE ARROW
        /// </summary>
        public const char olarr = '\u21BA';

        /// <summary>
        /// CLOCKWISE OPEN CIRCLE ARROW
        /// </summary>
        public const char orarr = '\u21BB';

        /// <summary>
        /// RIGHTWARDS TRIPLE ARROW
        /// </summary>
        public const char rAarr = '\u21DB';

        /// <summary>
        /// RIGHTWARDS TWO HEADED ARROW
        /// </summary>
        public const char Rarr = '\u21A0';

        /// <summary>
        /// RIGHTWARDS PAIRED ARROWS
        /// </summary>
        public const char rarr2 = '\u21C9';

        /// <summary>
        /// RIGHTWARDS ARROW WITH HOOK
        /// </summary>
        public const char rarrhk = '\u21AA';

        /// <summary>
        /// RIGHTWARDS ARROW WITH LOOP
        /// </summary>
        public const char rarrlp = '\u21AC';

        /// <summary>
        /// RIGHTWARDS ARROW WITH TAIL
        /// </summary>
        public const char rarrtl = '\u21A3';

        /// <summary>
        /// RIGHTWARDS WAVE ARROW
        /// </summary>
        public const char rarrw = '\u219D';

        /// <summary>
        /// RIGHTWARDS HARPOON WITH BARB DOWNWARDS
        /// </summary>
        public const char rhard = '\u21C1';

        /// <summary>
        /// RIGHTWARDS HARPOON WITH BARB UPWARDS
        /// </summary>
        public const char rharu = '\u21C0';

        /// <summary>
        /// RIGHTWARDS ARROW OVER LEFTWARDS ARROW
        /// </summary>
        public const char rlarr2 = '\u21C4';

        /// <summary>
        /// RIGHTWARDS HARPOON OVER LEFTWARDS HARPOON
        /// </summary>
        public const char rlhar2 = '\u21CC';

        /// <summary>
        /// UPWARDS ARROW WITH TIP RIGHTWARDS
        /// </summary>
        public const char rsh = '\u21B1';

        /// <summary>
        /// UPWARDS DOUBLE ARROW
        /// </summary>
        public const char uArr = '\u21D1';

        /// <summary>
        /// UPWARDS PAIRED ARROWS
        /// </summary>
        public const char uarr2 = '\u21C8';

        /// <summary>
        /// UPWARDS HARPOON WITH BARB LEFTWARDS
        /// </summary>
        public const char uharl = '\u21BF';

        /// <summary>
        /// UPWARDS HARPOON WITH BARB RIGHTWARDS
        /// </summary>
        public const char uharr = '\u21BE';

        /// <summary>
        /// UP DOWN ARROW
        /// </summary>
        public const char varr = '\u2195';

        /// <summary>
        /// UP DOWN DOUBLE ARROW
        /// </summary>
        public const char vArr = '\u21D5';

        /// <summary>
        /// LEFT RIGHT ARROW
        /// </summary>
        public const char xhArr = '\u2194';

        /// <summary>
        /// LEFT RIGHT ARROW
        /// </summary>
        public const char xharr = '\u2194';

        /// <summary>
        /// LEFTWARDS DOUBLE ARROW
        /// </summary>
        public const char xlArr = '\u21D0';

        /// <summary>
        /// RIGHTWARDS DOUBLE ARROW
        /// </summary>
        public const char xrArr = '\u21D2';

        #endregion

        #region Added Math Symbols: Binary Operators Entities (ISOamsb)

        /// <summary>
        /// N-ARY COPRODUCT
        /// </summary>
        public const char amalg = '\u2210';

        /// <summary>
        /// NAND
        /// </summary>
        public const char barwed = '\u22BC';

        /// <summary>
        /// PERSPECTIVE
        /// </summary>
        public const char Barwed = '\u2306';

        /// <summary>
        /// DOUBLE INTERSECTION
        /// </summary>
        public const char Cap = '\u22D2';

        /// <summary>
        /// N-ARY COPRODUCT
        /// </summary>
        public const char coprod = '\u2210';

        /// <summary>
        /// DOUBLE UNION
        /// </summary>
        public const char Cup = '\u22D3';

        /// <summary>
        /// CURLY LOGICAL OR
        /// </summary>
        public const char cuvee = '\u22CE';

        /// <summary>
        /// CURLY LOGICAL AND
        /// </summary>
        public const char cuwed = '\u22CF';

        /// <summary>
        /// DIAMOND OPERATOR
        /// </summary>
        public const char diam = '\u22C4';

        /// <summary>
        /// DIVISION TIMES
        /// </summary>
        public const char divonx = '\u22C7';

        /// <summary>
        /// INTERCALATE
        /// </summary>
        public const char intcal = '\u22BA';

        /// <summary>
        /// LEFT SEMIDIRECT PRODUCT
        /// </summary>
        public const char lthree = '\u22CB';

        /// <summary>
        /// LEFT NORMAL FACTOR SEMIDIRECT PRODUCT
        /// </summary>
        public const char ltimes = '\u22C9';

        /// <summary>
        /// SQUARED MINUS
        /// </summary>
        public const char minusb = '\u229F';

        /// <summary>
        /// CIRCLED ASTERISK OPERATOR
        /// </summary>
        public const char oast = '\u229B';

        /// <summary>
        /// CIRCLED RING OPERATOR
        /// </summary>
        public const char ocir = '\u229A';

        /// <summary>
        /// CIRCLED DASH
        /// </summary>
        public const char odash = '\u229D';

        /// <summary>
        /// CIRCLED DOT OPERATOR
        /// </summary>
        public const char odot = '\u2299';

        /// <summary>
        /// CIRCLED MINUS
        /// </summary>
        public const char ominus = '\u2296';

        /// <summary>
        /// CIRCLED PLUS
        /// </summary>
        public const char oplus = '\u2295';

        /// <summary>
        /// CIRCLED DIVISION SLASH
        /// </summary>
        public const char osol = '\u2298';

        /// <summary>
        /// CIRCLED TIMES
        /// </summary>
        public const char otimes = '\u2297';

        /// <summary>
        /// SQUARED PLUS
        /// </summary>
        public const char plusb = '\u229E';

        /// <summary>
        /// DOT PLUS
        /// </summary>
        public const char plusdo = '\u2214';

        /// <summary>
        /// N-ARY PRODUCT
        /// </summary>
        public const char prod = '\u220F';

        /// <summary>
        /// RIGHT SEMIDIRECT PRODUCT
        /// </summary>
        public const char rthree = '\u22CC';

        /// <summary>
        /// RIGHT NORMAL FACTOR SEMIDIRECT PRODUCT
        /// </summary>
        public const char rtimes = '\u22CA';

        /// <summary>
        /// DOT OPERATOR
        /// </summary>
        public const char sdot = '\u22C5';

        /// <summary>
        /// SQUARED DOT OPERATOR
        /// </summary>
        public const char sdotb = '\u22A1';

        /// <summary>
        /// SET MINUS
        /// </summary>
        public const char setmn = '\u2216';

        /// <summary>
        /// SQUARE CAP
        /// </summary>
        public const char sqcap = '\u2293';

        /// <summary>
        /// SQUARE CUP
        /// </summary>
        public const char sqcup = '\u2294';

        /// <summary>
        /// SET MINUS
        /// </summary>
        public const char ssetmn = '\u2216';

        /// <summary>
        /// STAR OPERATOR
        /// </summary>
        public const char sstarf = '\u22C6';

        /// <summary>
        /// N-ARY SUMMATION
        /// </summary>
        public const char sum = '\u2211';

        /// <summary>
        /// SQUARED TIMES
        /// </summary>
        public const char timesb = '\u22A0';

        /// <summary>
        /// DOWN TACK
        /// </summary>
        public const char top = '\u22A4';

        /// <summary>
        /// MULTISET UNION
        /// </summary>
        public const char uplus = '\u228E';

        /// <summary>
        /// WREATH PRODUCT
        /// </summary>
        public const char wreath = '\u2240';

        /// <summary>
        /// WHITE CIRCLE
        /// </summary>
        public const char xcirc = '\u25CB';

        /// <summary>
        /// WHITE DOWN-POINTING TRIANGLE
        /// </summary>
        public const char xdtri = '\u25BD';

        /// <summary>
        /// WHITE UP-POINTING TRIANGLE
        /// </summary>
        public const char xutri = '\u25B3';

        #endregion

        #region Added Math Symbols: Delimiters Entities (ISOamsc)

        /// <summary>
        /// BOTTOM LEFT CORNER
        /// </summary>
        public const char dlcorn = '\u231E';

        /// <summary>
        /// BOTTOM RIGHT CORNER
        /// </summary>
        public const char drcorn = '\u231F';

        /// <summary>
        /// LEFT CEILING
        /// </summary>
        public const char lceil = '\u2308';

        /// <summary>
        /// LEFT FLOOR
        /// </summary>
        public const char lfloor = '\u230A';

        ///// <summary>
        ///// left parenthesis, greater-than
        ///// </summary>
        //public const char lpargt = '\u????';

        /// <summary>
        /// RIGHT CEILING
        /// </summary>
        public const char rceil = '\u2309';

        /// <summary>
        /// RIGHT FLOOR
        /// </summary>
        public const char rfloor = '\u230B';

        ///// <summary>
        ///// right parenthesis, greater-than
        ///// </summary>
        //public const char rpargt = '\u????';

        /// <summary>
        /// TOP LEFT CORNER
        /// </summary>
        public const char ulcorn = '\u231C';

        /// <summary>
        /// TOP RIGHT CORNER
        /// </summary>
        public const char urcorn = '\u231D';

        #endregion

        #region ISO Added Math Symbols: Negated Relations Entities (ISOamsn)

        ///// <summary>
        ///// greater-than, not approximately equal to
        ///// </summary>
        //public const char gnap = '\u????';

        /// <summary>
        /// GREATER-THAN BUT NOT EQUAL TO
        /// </summary>
        public const char gne = '\u2269';

        /// <summary>
        /// GREATER-THAN BUT NOT EQUAL TO
        /// </summary>
        public const char gnE = '\u2269';

        /// <summary>
        /// GREATER-THAN BUT NOT EQUIVALENT TO
        /// </summary>
        public const char gnsim = '\u22E7';

        /// <summary>
        /// GREATER-THAN BUT NOT EQUAL TO
        /// </summary>
        public const char gvnE = '\u2269';

        ///// <summary>
        ///// less-than, not approximately equal to
        ///// </summary>
        //public const char lnap = '\u????';

        /// <summary>
        /// LESS-THAN BUT NOT EQUAL TO
        /// </summary>
        public const char lnE = '\u2268';

        /// <summary>
        /// LESS-THAN BUT NOT EQUAL TO
        /// </summary>
        public const char lne = '\u2268';

        /// <summary>
        /// LESS-THAN BUT NOT EQUIVALENT TO
        /// </summary>
        public const char lnsim = '\u22E6';

        /// <summary>
        /// LESS-THAN BUT NOT EQUAL TO
        /// </summary>
        public const char lvnE = '\u2268';

        /// <summary>
        /// NOT ALMOST EQUAL TO
        /// </summary>
        public const char nap = '\u2249';

        /// <summary>
        /// NEITHER APPROXIMATELY NOR ACTUALLY EQUAL TO
        /// </summary>
        public const char ncong = '\u2247';

        /// <summary>
        /// NOT IDENTICAL TO
        /// </summary>
        public const char nequiv = '\u2262';

        ///// <summary>
        ///// not greater-than, double equals
        ///// </summary>
        //public const char ngE = '\u????';

        /// <summary>
        /// NEITHER GREATER-THAN NOR EQUAL TO
        /// </summary>
        public const char nge = '\u2271';

        /// <summary>
        /// NEITHER GREATER-THAN NOR EQUAL TO
        /// </summary>
        public const char nges = '\u2271';

        /// <summary>
        /// NOT GREATER-THAN
        /// </summary>
        public const char ngt = '\u226F';

        ///// <summary>
        ///// not less-than, double equals
        ///// </summary>
        //public const char nlE = '\u????';

        /// <summary>
        /// NEITHER LESS-THAN NOR EQUAL TO
        /// </summary>
        public const char nle = '\u2270';

        /// <summary>
        /// NEITHER LESS-THAN NOR EQUAL TO
        /// </summary>
        public const char nles = '\u2270';

        /// <summary>
        /// NOT LESS-THAN
        /// </summary>
        public const char nlt = '\u226E';

        /// <summary>
        /// NOT NORMAL SUBGROUP OF
        /// </summary>
        public const char nltri = '\u22EA';

        /// <summary>
        /// NOT NORMAL SUBGROUP OF OR EQUAL TO
        /// </summary>
        public const char nltrie = '\u22EC';

        /// <summary>
        /// DOES NOT DIVIDE
        /// </summary>
        public const char nmid = '\u2224';

        /// <summary>
        /// NOT PARALLEL TO
        /// </summary>
        public const char npar = '\u2226';

        /// <summary>
        /// DOES NOT PRECEDE
        /// </summary>
        public const char npr = '\u2280';

        /// <summary>
        /// DOES NOT PRECEDE OR EQUAL
        /// </summary>
        public const char npre = '\u22E0';

        /// <summary>
        /// DOES NOT CONTAIN AS NORMAL SUBGROUP
        /// </summary>
        public const char nrtri = '\u22EB';

        /// <summary>
        /// DOES NOT CONTAIN AS NORMAL SUBGROUP OR EQUAL
        /// </summary>
        public const char nrtrie = '\u22ED';

        /// <summary>
        /// DOES NOT SUCCEED
        /// </summary>
        public const char nsc = '\u2281';

        /// <summary>
        /// DOES NOT SUCCEED OR EQUAL
        /// </summary>
        public const char nsce = '\u22E1';

        /// <summary>
        /// NOT TILDE
        /// </summary>
        public const char nsim = '\u2241';

        /// <summary>
        /// NOT ASYMPTOTICALLY EQUAL TO
        /// </summary>
        public const char nsime = '\u2244';

        ///// <summary>
        ///// nshortmid
        ///// </summary>
        //public const char nsmid = '\u????';

        /// <summary>
        /// NOT PARALLEL TO
        /// </summary>
        public const char nspar = '\u2226';

        /// <summary>
        /// NOT A SUBSET OF
        /// </summary>
        public const char nsub = '\u2284';

        /// <summary>
        /// NEITHER A SUBSET OF NOR EQUAL TO
        /// </summary>
        public const char nsubE = '\u2288';

        /// <summary>
        /// NEITHER A SUBSET OF NOR EQUAL TO
        /// </summary>
        public const char nsube = '\u2288';

        /// <summary>
        /// NOT A SUPERSET OF
        /// </summary>
        public const char nsup = '\u2285';

        /// <summary>
        /// NEITHER A SUPERSET OF NOR EQUAL TO
        /// </summary>
        public const char nsupE = '\u2289';

        /// <summary>
        /// NEITHER A SUPERSET OF NOR EQUAL TO
        /// </summary>
        public const char nsupe = '\u2289';

        /// <summary>
        /// DOES NOT PROVE
        /// </summary>
        public const char nvdash = '\u22AC';

        /// <summary>
        /// NOT TRUE
        /// </summary>
        public const char nvDash = '\u22AD';

        /// <summary>
        /// DOES NOT FORCE
        /// </summary>
        public const char nVdash = '\u22AE';

        /// <summary>
        /// NEGATED DOUBLE VERTICAL BAR DOUBLE RIGHT TURNSTILE
        /// </summary>
        public const char nVDash = '\u22AF';

        ///// <summary>
        ///// precedes, not approximately equal to
        ///// </summary>
        //public const char prnap = '\u????';

        ///// <summary>
        ///// precedes, not double equal
        ///// </summary>
        //public const char prnE = '\u????';

        /// <summary>
        /// PRECEDES BUT NOT EQUIVALENT TO
        /// </summary>
        public const char prnsim = '\u22E8';

        ///// <summary>
        ///// succeeds, not approximately equal to
        ///// </summary>
        //public const char scnap = '\u????';

        ///// <summary>
        ///// succeeds, not double equals
        ///// </summary>
        //public const char scnE = '\u????';

        /// <summary>
        /// SUCCEEDS BUT NOT EQUIVALENT TO
        /// </summary>
        public const char scnsim = '\u22E9';

        /// <summary>
        /// SUBSET OF WITH NOT EQUAL TO
        /// </summary>
        public const char subnE = '\u228A';

        /// <summary>
        /// SUBSET OF WITH NOT EQUAL TO
        /// </summary>
        public const char subne = '\u228A';

        /// <summary>
        /// SUPERSET OF WITH NOT EQUAL TO
        /// </summary>
        public const char supnE = '\u228B';

        /// <summary>
        /// SUPERSET OF WITH NOT EQUAL TO
        /// </summary>
        public const char supne = '\u228B';

        /// <summary>
        /// SUBSET OF WITH NOT EQUAL TO
        /// </summary>
        public const char vsubnE = '\u228A';

        /// <summary>
        /// SUBSET OF WITH NOT EQUAL TO
        /// </summary>
        public const char vsubne = '\u228A';

        /// <summary>
        /// SUPERSET OF WITH NOT EQUAL TO
        /// </summary>
        public const char vsupne = '\u228B';

        /// <summary>
        /// SUPERSET OF WITH NOT EQUAL TO
        /// </summary>
        public const char vsupnE = '\u228B';

        #endregion

        #region Added Math Symbols: Ordinary Entities (ISOamso)

        /// <summary>
        /// ANGLE
        /// </summary>
        public const char ang = '\u2220';

        /// <summary>
        /// MEASURED ANGLE
        /// </summary>
        public const char angmsd = '\u2221';

        /// <summary>
        /// BET SYMBOL
        /// </summary>
        public const char beth = '\u2136';

        /// <summary>
        /// REVERSED PRIME
        /// </summary>
        public const char bprime = '\u2035';

        /// <summary>
        /// COMPLEMENT
        /// </summary>
        public const char comp = '\u2201';

        /// <summary>
        /// DALET SYMBOL
        /// </summary>
        public const char daleth = '\u2138';

        /// <summary>
        /// SCRIPT SMALL L
        /// </summary>
        public const char ell = '\u2113';

        /// <summary>
        /// EMPTY SET
        /// </summary>
        public const char empty = '\u2205';

        /// <summary>
        /// GIMEL SYMBOL
        /// </summary>
        public const char gimel = '\u2137';

        /// <summary>
        /// BLACK-LETTER CAPITAL I
        /// </summary>
        public const char image = '\u2111';

        ///// <summary>
        ///// latin small letter dotless j
        ///// </summary>
        //public const char jnodot = '\u????';

        /// <summary>
        /// THERE DOES NOT EXIST
        /// </summary>
        public const char nexist = '\u2204';

        /// <summary>
        /// CIRCLED LATIN CAPITAL LETTER S
        /// </summary>
        public const char oS = '\u24C8';

        /// <summary>
        /// PLANCK CONSTANT OVER TWO PI
        /// </summary>
        public const char planck = '\u210F';

        /// <summary>
        /// BLACK-LETTER CAPITAL R
        /// </summary>
        public const char real = '\u211C';

        /// <summary>
        /// REVERSE SOLIDUS
        /// </summary>
        public const char sbsol = '\u005C';

        /// <summary>
        /// PRIME
        /// </summary>
        public const char vprime = '\u2032';

        /// <summary>
        /// SCRIPT CAPITAL P
        /// </summary>
        public const char weierp = '\u2118';

        #endregion

        #region Added Math Symbols: Relations Entities (ISOamsr)

        /// <summary>
        /// ALMOST EQUAL OR EQUAL TO
        /// </summary>
        public const char ape = '\u224A';

        /// <summary>
        /// ALMOST EQUAL TO
        /// </summary>
        public const char asymp = '\u2248';

        /// <summary>
        /// ALL EQUAL TO
        /// </summary>
        public const char bcong = '\u224C';

        /// <summary>
        /// SMALL CONTAINS AS MEMBER
        /// </summary>
        public const char bepsi = '\u220D';

        /// <summary>
        /// BOWTIE
        /// </summary>
        public const char bowtie = '\u22C8';

        /// <summary>
        /// REVERSED TILDE
        /// </summary>
        public const char bsim = '\u223D';

        /// <summary>
        /// REVERSED TILDE EQUALS
        /// </summary>
        public const char bsime = '\u22CD';

        /// <summary>
        /// GEOMETRICALLY EQUIVALENT TO
        /// </summary>
        public const char bump = '\u224E';

        /// <summary>
        /// DIFFERENCE BETWEEN
        /// </summary>
        public const char bumpe = '\u224F';

        /// <summary>
        /// RING EQUAL TO
        /// </summary>
        public const char cire = '\u2257';

        /// <summary>
        /// COLON EQUALS
        /// </summary>
        public const char colone = '\u2254';

        /// <summary>
        /// EQUAL TO OR PRECEDES
        /// </summary>
        public const char cuepr = '\u22DE';

        /// <summary>
        /// EQUAL TO OR SUCCEEDS
        /// </summary>
        public const char cuesc = '\u22DF';

        /// <summary>
        /// PRECEDES OR EQUAL TO
        /// </summary>
        public const char cupre = '\u227C';

        /// <summary>
        /// LEFT TACK
        /// </summary>
        public const char dashv = '\u22A3';

        /// <summary>
        /// RING IN EQUAL TO
        /// </summary>
        public const char ecir = '\u2256';

        /// <summary>
        /// EQUALS COLON
        /// </summary>
        public const char ecolon = '\u2255';

        /// <summary>
        /// GEOMETRICALLY EQUAL TO
        /// </summary>
        public const char eDot = '\u2251';

        /// <summary>
        /// APPROXIMATELY EQUAL TO OR THE IMAGE OF
        /// </summary>
        public const char efDot = '\u2252';

        /// <summary>
        /// EQUAL TO OR GREATER-THAN
        /// </summary>
        public const char egs = '\u22DD';

        /// <summary>
        /// EQUAL TO OR LESS-THAN
        /// </summary>
        public const char els = '\u22DC';

        /// <summary>
        /// IMAGE OF OR APPROXIMATELY EQUAL TO
        /// </summary>
        public const char erDot = '\u2253';

        /// <summary>
        /// APPROACHES THE LIMIT
        /// </summary>
        public const char esdot = '\u2250';

        /// <summary>
        /// PITCHFORK
        /// </summary>
        public const char fork = '\u22D4';

        /// <summary>
        /// FROWN
        /// </summary>
        public const char frown = '\u2322';

        ///// <summary>
        ///// greater-than, approximately equal to
        ///// </summary>
        //public const char gap = '\u????';

        /// <summary>
        /// GREATER-THAN OVER EQUAL TO
        /// </summary>
        public const char gE = '\u2267';

        ///// <summary>
        ///// greater-than, double equals, less-than
        ///// </summary>
        //public const char gEl = '\u????';

        /// <summary>
        /// GREATER-THAN EQUAL TO OR LESS-THAN
        /// </summary>
        public const char gel = '\u22DB';

        /// <summary>
        /// GREATER-THAN OR EQUAL TO
        /// </summary>
        public const char ges = '\u2265';

        /// <summary>
        /// VERY MUCH GREATER-THAN
        /// </summary>
        public const char Gg = '\u22D9';

        /// <summary>
        /// GREATER-THAN OR LESS-THAN
        /// </summary>
        public const char gl = '\u2277';

        /// <summary>
        /// GREATER-THAN WITH DOT
        /// </summary>
        public const char gsdot = '\u22D7';

        /// <summary>
        /// GREATER-THAN OR EQUIVALENT TO
        /// </summary>
        public const char gsim = '\u2273';

        /// <summary>
        /// MUCH GREATER-THAN
        /// </summary>
        public const char Gt = '\u226B';

        ///// <summary>
        ///// less-than, approximately equal to
        ///// </summary>
        //public const char lap = '\u????';

        /// <summary>
        /// LESS-THAN WITH DOT
        /// </summary>
        public const char ldot = '\u22D6';

        /// <summary>
        /// LESS-THAN OVER EQUAL TO
        /// </summary>
        public const char lE = '\u2266';

        ///// <summary>
        ///// less-than, double equals, greater-than
        ///// </summary>
        //public const char lEg = '\u????';

        /// <summary>
        /// LESS-THAN EQUAL TO OR GREATER-THAN
        /// </summary>
        public const char leg = '\u22DA';

        /// <summary>
        /// LESS-THAN OR EQUAL TO
        /// </summary>
        public const char les = '\u2264';

        /// <summary>
        /// LESS-THAN OR GREATER-THAN
        /// </summary>
        public const char lg = '\u2276';

        /// <summary>
        /// VERY MUCH LESS-THAN
        /// </summary>
        public const char Ll = '\u22D8';

        /// <summary>
        /// LESS-THAN OR EQUIVALENT TO
        /// </summary>
        public const char lsim = '\u2272';

        /// <summary>
        /// MUCH LESS-THAN
        /// </summary>
        public const char Lt = '\u226A';

        /// <summary>
        /// NORMAL SUBGROUP OF OR EQUAL TO
        /// </summary>
        public const char ltrie = '\u22B4';

        /// <summary>
        /// DIVIDES
        /// </summary>
        public const char mid = '\u2223';

        /// <summary>
        /// MODELS
        /// </summary>
        public const char models = '\u22A7';

        /// <summary>
        /// PRECEDES
        /// </summary>
        public const char pr = '\u227A';

        ///// <summary>
        ///// precedes, approximately equal to
        ///// </summary>
        //public const char prap = '\u????';

        /// <summary>
        /// PRECEDES OR EQUAL TO
        /// </summary>
        public const char pre = '\u227C';

        /// <summary>
        /// PRECEDES OR EQUIVALENT TO
        /// </summary>
        public const char prsim = '\u227E';

        /// <summary>
        /// CONTAINS AS NORMAL SUBGROUP OR EQUAL TO
        /// </summary>
        public const char rtrie = '\u22B5';

        /// <summary>
        /// N-ARY COPRODUCT
        /// </summary>
        public const char samalg = '\u2210';

        /// <summary>
        /// SUCCEEDS
        /// </summary>
        public const char sc = '\u227B';

        ///// <summary>
        ///// succeeds, approximately equal to
        ///// </summary>
        //public const char scap = '\u????';

        /// <summary>
        /// SUCCEEDS OR EQUAL TO
        /// </summary>
        public const char sccue = '\u227D';

        /// <summary>
        /// SUCCEEDS OR EQUAL TO
        /// </summary>
        public const char sce = '\u227D';

        /// <summary>
        /// SUCCEEDS OR EQUIVALENT TO
        /// </summary>
        public const char scsim = '\u227F';

        /// <summary>
        /// FROWN
        /// </summary>
        public const char sfrown = '\u2322';

        ///// <summary>
        ///// shortmid
        ///// </summary>
        //public const char smid = '\u????';

        /// <summary>
        /// SMILE
        /// </summary>
        public const char smile = '\u2323';

        /// <summary>
        /// PARALLEL TO
        /// </summary>
        public const char spar = '\u2225';

        /// <summary>
        /// SQUARE IMAGE OF
        /// </summary>
        public const char sqsub = '\u228F';

        /// <summary>
        /// SQUARE IMAGE OF OR EQUAL TO
        /// </summary>
        public const char sqsube = '\u2291';

        /// <summary>
        /// SQUARE ORIGINAL OF
        /// </summary>
        public const char sqsup = '\u2290';

        /// <summary>
        /// SQUARE ORIGINAL OF OR EQUAL TO
        /// </summary>
        public const char sqsupe = '\u2292';

        /// <summary>
        /// SMILE
        /// </summary>
        public const char ssmile = '\u2323';

        /// <summary>
        /// DOUBLE SUBSET
        /// </summary>
        public const char Sub = '\u22D0';

        /// <summary>
        /// SUBSET OF OR EQUAL TO
        /// </summary>
        public const char subE = '\u2286';

        /// <summary>
        /// DOUBLE SUPERSET
        /// </summary>
        public const char Sup = '\u22D1';

        /// <summary>
        /// SUPERSET OF OR EQUAL TO
        /// </summary>
        public const char supE = '\u2287';

        /// <summary>
        /// ALMOST EQUAL TO
        /// </summary>
        public const char thkap = '\u2248';

        /// <summary>
        /// TILDE OPERATOR
        /// </summary>
        public const char thksim = '\u223C';

        /// <summary>
        /// DELTA EQUAL TO
        /// </summary>
        public const char trie = '\u225C';

        /// <summary>
        /// BETWEEN
        /// </summary>
        public const char twixt = '\u226C';

        /// <summary>
        /// RIGHT TACK
        /// </summary>
        public const char vdash = '\u22A2';

        /// <summary>
        /// TRUE
        /// </summary>
        public const char vDash = '\u22A8';

        /// <summary>
        /// FORCES
        /// </summary>
        public const char Vdash = '\u22A9';

        /// <summary>
        /// XOR
        /// </summary>
        public const char veebar = '\u22BB';

        /// <summary>
        /// NORMAL SUBGROUP OF
        /// </summary>
        public const char vltri = '\u22B2';

        /// <summary>
        /// PROPORTIONAL TO
        /// </summary>
        public const char vprop = '\u221D';

        /// <summary>
        /// CONTAINS AS NORMAL SUBGROUP
        /// </summary>
        public const char vrtri = '\u22B3';

        /// <summary>
        /// TRIPLE VERTICAL BAR RIGHT TURNSTILE
        /// </summary>
        public const char Vvdash = '\u22AA';

        #endregion

        #region Publishing Entities (ISOpub)

        /// <summary>
        /// OPEN BOX
        /// </summary>
        public const char blank = '\u2423';

        /// <summary>
        /// MEDIUM SHADE
        /// </summary>
        public const char blk12 = '\u2592';

        /// <summary>
        /// LIGHT SHADE
        /// </summary>
        public const char blk14 = '\u2591';

        /// <summary>
        /// DARK SHADE
        /// </summary>
        public const char blk34 = '\u2593';

        /// <summary>
        /// FULL BLOCK
        /// </summary>
        public const char block = '\u2588';

        /// <summary>
        /// BULLET
        /// </summary>
        public const char bull = '\u2022';

        /// <summary>
        /// CARET INSERTION POINT
        /// </summary>
        public const char caret = '\u2041';

        /// <summary>
        /// CHECK MARK
        /// </summary>
        public const char check = '\u2713';

        /// <summary>
        /// WHITE CIRCLE
        /// </summary>
        public const char cir = '\u25CB';

        /// <summary>
        /// BLACK CLUB SUIT
        /// </summary>
        public const char clubs = '\u2663';

        /// <summary>
        /// SOUND RECORDING COPYRIGHT
        /// </summary>
        public const char copysr = '\u2117';

        /// <summary>
        /// BALLOT X
        /// </summary>
        public const char cross = '\u2717';

        /// <summary>
        /// DAGGER
        /// </summary>
        public const char dagger = '\u2020';

        /// <summary>
        /// DOUBLE DAGGER
        /// </summary>
        public const char Dagger = '\u2021';

        /// <summary>
        /// HYPHEN
        /// </summary>
        public const char dash = '\u2010';

        /// <summary>
        /// BLACK DIAMOND SUIT
        /// </summary>
        public const char diams = '\u2666';

        /// <summary>
        /// BOTTOM LEFT CROP
        /// </summary>
        public const char dlcrop = '\u230D';

        /// <summary>
        /// BOTTOM RIGHT CROP
        /// </summary>
        public const char drcrop = '\u230C';

        /// <summary>
        /// WHITE DOWN-POINTING SMALL TRIANGLE
        /// </summary>
        public const char dtri = '\u25BF';

        /// <summary>
        /// BLACK DOWN-POINTING SMALL TRIANGLE
        /// </summary>
        public const char dtrif = '\u25BE';

        /// <summary>
        /// EM SPACE
        /// </summary>
        public const char emsp = '\u2003';

        /// <summary>
        /// THREE-PER-EM SPACE
        /// </summary>
        public const char emsp13 = '\u2004';

        /// <summary>
        /// FOUR-PER-EM SPACE
        /// </summary>
        public const char emsp14 = '\u2005';

        /// <summary>
        /// EN SPACE
        /// </summary>
        public const char ensp = '\u2002';

        /// <summary>
        /// FEMALE SIGN
        /// </summary>
        public const char female = '\u2640';

        /// <summary>
        /// LATIN SMALL LIGATURE FFI
        /// </summary>
        public const char ffilig = '\uFB03';

        /// <summary>
        /// LATIN SMALL LIGATURE FF
        /// </summary>
        public const char fflig = '\uFB00';

        /// <summary>
        /// LATIN SMALL LIGATURE FFL
        /// </summary>
        public const char ffllig = '\uFB04';

        /// <summary>
        /// LATIN SMALL LIGATURE FI
        /// </summary>
        public const char filig = '\uFB01';

        ///// <summary>
        ///// fj ligature
        ///// </summary>
        //public const char fjlig = '\u????';

        /// <summary>
        /// MUSIC FLAT SIGN
        /// </summary>
        public const char flat = '\u266D';

        /// <summary>
        /// LATIN SMALL LIGATURE FL
        /// </summary>
        public const char fllig = '\uFB02';

        /// <summary>
        /// VULGAR FRACTION ONE THIRD
        /// </summary>
        public const char frac13 = '\u2153';

        /// <summary>
        /// VULGAR FRACTION ONE FIFTH
        /// </summary>
        public const char frac15 = '\u2155';

        /// <summary>
        /// VULGAR FRACTION ONE SIXTH
        /// </summary>
        public const char frac16 = '\u2159';

        /// <summary>
        /// VULGAR FRACTION TWO THIRDS
        /// </summary>
        public const char frac23 = '\u2154';

        /// <summary>
        /// VULGAR FRACTION TWO FIFTHS
        /// </summary>
        public const char frac25 = '\u2156';

        /// <summary>
        /// VULGAR FRACTION THREE FIFTHS
        /// </summary>
        public const char frac35 = '\u2157';

        /// <summary>
        /// VULGAR FRACTION FOUR FIFTHS
        /// </summary>
        public const char frac45 = '\u2158';

        /// <summary>
        /// VULGAR FRACTION FIVE SIXTHS
        /// </summary>
        public const char frac56 = '\u215A';

        /// <summary>
        /// HAIR SPACE
        /// </summary>
        public const char hairsp = '\u200A';

        /// <summary>
        /// BLACK HEART SUIT
        /// </summary>
        public const char hearts = '\u2665';

        /// <summary>
        /// HORIZONTAL ELLIPSIS
        /// </summary>
        public const char hellip = '\u2026';

        /// <summary>
        /// HYPHEN BULLET
        /// </summary>
        public const char hybull = '\u2043';

        /// <summary>
        /// CARE OF
        /// </summary>
        public const char incare = '\u2105';

        /// <summary>
        /// DOUBLE LOW-9 QUOTATION MARK
        /// </summary>
        public const char ldquor = '\u201E';

        /// <summary>
        /// LOWER HALF BLOCK
        /// </summary>
        public const char lhblk = '\u2584';

        /// <summary>
        /// LOZENGE
        /// </summary>
        public const char loz = '\u25CA';

        ///// <summary>
        ///// WHITE FOUR POINTED STAR
        ///// </summary>
        //public const char loz = '\u2727';

        /// <summary>
        /// BLACK FOUR POINTED STAR
        /// </summary>
        public const char lozf = '\u2726';

        /// <summary>
        /// SINGLE LOW-9 QUOTATION MARK
        /// </summary>
        public const char lsquor = '\u201A';

        /// <summary>
        /// WHITE LEFT-POINTING SMALL TRIANGLE
        /// </summary>
        public const char ltri = '\u25C3';

        /// <summary>
        /// BLACK LEFT-POINTING SMALL TRIANGLE
        /// </summary>
        public const char ltrif = '\u25C2';

        /// <summary>
        /// MALE SIGN
        /// </summary>
        public const char male = '\u2642';

        /// <summary>
        /// MALTESE CROSS
        /// </summary>
        public const char malt = '\u2720';

        /// <summary>
        /// BLACK VERTICAL RECTANGLE
        /// </summary>
        public const char marker = '\u25AE';

        /// <summary>
        /// EM DASH
        /// </summary>
        public const char mdash = '\u2014';

        /// <summary>
        /// HORIZONTAL ELLIPSIS
        /// </summary>
        public const char mldr = '\u2026';

        /// <summary>
        /// MUSIC NATURAL SIGN
        /// </summary>
        public const char natur = '\u266E';

        /// <summary>
        /// EN DASH
        /// </summary>
        public const char ndash = '\u2013';

        /// <summary>
        /// TWO DOT LEADER
        /// </summary>
        public const char nldr = '\u2025';

        /// <summary>
        /// FIGURE SPACE
        /// </summary>
        public const char numsp = '\u2007';

        /// <summary>
        /// BLACK TELEPHONE
        /// </summary>
        public const char phone = '\u260E';

        /// <summary>
        /// PUNCTUATION SPACE
        /// </summary>
        public const char puncsp = '\u2008';

        /// <summary>
        /// LEFT DOUBLE QUOTATION MARK
        /// </summary>
        public const char rdquor = '\u201C';

        /// <summary>
        /// WHITE RECTANGLE
        /// </summary>
        public const char rect = '\u25AD';

        /// <summary>
        /// LEFT SINGLE QUOTATION MARK
        /// </summary>
        public const char rsquor = '\u2018';

        /// <summary>
        /// WHITE RIGHT-POINTING SMALL TRIANGLE
        /// </summary>
        public const char rtri = '\u25B9';

        /// <summary>
        /// BLACK RIGHT-POINTING SMALL TRIANGLE
        /// </summary>
        public const char rtrif = '\u25B8';

        /// <summary>
        /// PRESCRIPTION TAKE
        /// </summary>
        public const char rx = '\u211E';

        /// <summary>
        /// SIX POINTED BLACK STAR
        /// </summary>
        public const char sext = '\u2736';

        /// <summary>
        /// MUSIC SHARP SIGN
        /// </summary>
        public const char sharp = '\u266F';

        /// <summary>
        /// BLACK SPADE SUIT
        /// </summary>
        public const char spades = '\u2660';

        /// <summary>
        /// WHITE SQUARE
        /// </summary>
        public const char squ = '\u25A1';

        /// <summary>
        /// BLACK SMALL SQUARE
        /// </summary>
        public const char squf = '\u25AA';

        /// <summary>
        /// WHITE STAR
        /// </summary>
        public const char star = '\u2606';

        /// <summary>
        /// BLACK STAR
        /// </summary>
        public const char starf = '\u2605';

        /// <summary>
        /// POSITION INDICATOR
        /// </summary>
        public const char target = '\u2316';

        /// <summary>
        /// TELEPHONE RECORDER
        /// </summary>
        public const char telrec = '\u2315';

        /// <summary>
        /// THIN SPACE
        /// </summary>
        public const char thinsp = '\u2009';

        /// <summary>
        /// UPPER HALF BLOCK
        /// </summary>
        public const char uhblk = '\u2580';

        /// <summary>
        /// TOP LEFT CROP
        /// </summary>
        public const char ulcrop = '\u230F';

        /// <summary>
        /// TOP RIGHT CROP
        /// </summary>
        public const char urcrop = '\u230E';

        /// <summary>
        /// WHITE UP-POINTING SMALL TRIANGLE
        /// </summary>
        public const char utri = '\u25B5';

        /// <summary>
        /// BLACK UP-POINTING SMALL TRIANGLE
        /// </summary>
        public const char utrif = '\u25B4';

        /// <summary>
        /// VERTICAL ELLIPSIS
        /// </summary>
        public const char vellip = '\u22EE';

        #endregion

        #region HTML-only symbols and special characters

        /// <summary>
        /// ALEF SYMBOL
        /// </summary>
        public const char alefsym = '\u2135';

        /// <summary>
        /// GREEK CAPITAL LETTER ALPHA
        /// </summary>
        public const char Alpha = '\u0391';

        /// <summary>
        /// DOUBLE LOW-9 QUOTATION MARK
        /// </summary>
        public const char bdquo = '\u201E';

        /// <summary>
        /// GREEK CAPITAL LETTER BETA
        /// </summary>
        public const char Beta = '\u0392';

        /// <summary>
        /// GREEK CAPITAL LETTER CHI
        /// </summary>
        public const char Chi = '\u03A7';

        /// <summary>
        /// DOWNWARDS ARROW WITH CORNER LEFTWARDS
        /// </summary>
        public const char crarr = '\u21B5';

        /// <summary>
        /// GREEK CAPITAL LETTER EPSILON
        /// </summary>
        public const char Epsilon = '\u0395';

        /// <summary>
        /// GREEK SMALL LETTER EPSILON
        /// </summary>
        public const char epsilon = '\u03B5';

        /// <summary>
        /// GREEK CAPITAL LETTER ETA
        /// </summary>
        public const char Eta = '\u0397';

        /// <summary>
        /// FRACTION SLASH
        /// </summary>
        public const char frasl = '\u2044';

        /// <summary>
        /// GREEK CAPITAL LETTER IOTA
        /// </summary>
        public const char Iota = '\u0399';

        /// <summary>
        /// GREEK CAPITAL LETTER KAPPA
        /// </summary>
        public const char Kappa = '\u039A';

        /// <summary>
        /// LEFT-TO-RIGHT MARK
        /// </summary>
        public const char lrm = '\u200E';

        /// <summary>
        /// SINGLE LEFT-POINTING ANGLE QUOTATION MARK
        /// </summary>
        public const char lsaquo = '\u2039';

        /// <summary>
        /// GREEK CAPITAL LETTER MU
        /// </summary>
        public const char Mu = '\u039C';

        /// <summary>
        /// GREEK CAPITAL LETTER NU
        /// </summary>
        public const char Nu = '\u039D';

        /// <summary>
        /// OVERLINE
        /// </summary>
        public const char oline = '\u203E';

        /// <summary>
        /// GREEK CAPITAL LETTER OMICRON
        /// </summary>
        public const char Omicron = '\u039F';

        /// <summary>
        /// GREEK SMALL LETTER OMICRON
        /// </summary>
        public const char omicron = '\u03BF';

        /// <summary>
        /// GREEK SMALL LETTER PHI
        /// </summary>
        public const char phi = '\u03C6';

        /// <summary>
        /// GREEK CAPITAL LETTER RHO
        /// </summary>
        public const char Rho = '\u03A1';

        /// <summary>
        /// RIGHT-TO-LEFT MARK
        /// </summary>
        public const char rlm = '\u200F';

        /// <summary>
        /// SINGLE RIGHT-POINTING ANGLE QUOTATION MARK
        /// </summary>
        public const char rsaquo = '\u203A';

        /// <summary>
        /// SINGLE LOW-9 QUOTATION MARK
        /// </summary>
        public const char sbquo = '\u201A';

        /// <summary>
        /// GREEK SMALL LETTER FINAL SIGMA
        /// </summary>
        public const char sigmaf = '\u03C2';

        /// <summary>
        /// GREEK CAPITAL LETTER TAU
        /// </summary>
        public const char Tau = '\u03A4';

        /// <summary>
        /// GREEK SMALL LETTER THETA
        /// </summary>
        public const char theta = '\u03B8';

        /// <summary>
        /// GREEK THETA SYMBOL
        /// </summary>
        public const char thetasym = '\u03D1';

        /// <summary>
        /// GREEK UPSILON WITH HOOK SYMBOL
        /// </summary>
        public const char upsih = '\u03D2';

        /// <summary>
        /// GREEK CAPITAL LETTER UPSILON
        /// </summary>
        public const char Upsilon = '\u03A5';

        /// <summary>
        /// GREEK SMALL LETTER UPSILON
        /// </summary>
        public const char upsilon = '\u03C5';

        /// <summary>
        /// GREEK CAPITAL LETTER ZETA
        /// </summary>
        public const char Zeta = '\u0396';

        /// <summary>
        /// ZERO WIDTH JOINER
        /// </summary>
        public const char zwj = '\u200D';

        /// <summary>
        /// ZERO WIDTH NON-JOINER
        /// </summary>
        public const char zwnj = '\u200C';

        #endregion

        // missing: block left-right, triangle up/down (or at least that are in the charmap are different)
    }
}
