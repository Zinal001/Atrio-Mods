using I2.Loc;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CModLib
{
    public static class Loc
    {
        private static List<Translation> _Translations = new List<Translation>();
        private static LanguageSourceData _SourceData;

        /// <summary>
        /// Adds or replaces a translation to the specified language.
        /// </summary>
        /// <param name="key">The key for the translation</param>
        /// <param name="value">The translated text</param>
        /// <param name="language">The name of the language</param>
        public static void SetTranslation(String key, String value, String language = "English")
        {
            if (_SourceData == null)
                _Translations.Add(new Translation() { Key = key, Value = value, Language = language });
            else
                _AddTranslation(key, value, language);
        }

        /// <summary>
        /// Get the translation of the specified key.
        /// <para>This is a simple wrapper around <see cref="Isto.Atrio.Loc.GetString(string)"/></para>
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static String GetTranslation(String key)
        {
            return Isto.Atrio.Loc.GetString(key);
        }

        /// <summary>
        /// This initializes the Loc class.
        /// Should be called after GameState.SendLoadCompleteEvent has been called! (In a Postfix, preferebly)
        /// </summary>
        public static void Init()
        {
            if (_SourceData == null)
                _SourceData = LocalizationManager.GetSourceContaining(Isto.Atrio.Constants.LOCALIZATION_KEY_NOT_FOUND);

            foreach(Translation translation in _Translations.ToArray())
            {
                _AddTranslation(translation.Key, translation.Value, translation.Language);
                _Translations.Remove(translation);
            }
        }

        private static void _AddTranslation(String key, String value, String language)
        {
            int languageIndex = _SourceData.GetLanguageIndex(language);
            if (languageIndex == -1)
            {
                _SourceData.AddLanguage(language);
                languageIndex = _SourceData.GetLanguageIndex(language);
            }

            TermData termData = _SourceData.GetTermData(key, true);
            if (termData == null)
                termData = _SourceData.AddTerm(key, eTermType.Text, false);

            termData.SetTranslation(languageIndex, value);
        }

        private class Translation
        {
            public String Key;
            public String Value;
            public String Language;
        }
    }
}
