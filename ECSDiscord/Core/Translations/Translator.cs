using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace ECSDiscord.Core.Translations
{
    public class Translator : ITranslator
    {
        public class TranslationNotFoundException : Exception
        {
            public readonly string TranslationKey;
            public TranslationNotFoundException(string key)
            {
                TranslationKey = key;
            }
        }

        public static Translator DefaultTranslations = new Translator(new Dictionary<string, string>
        {
            { "COMMAND_PROCESSING", "Processing..." },
            { "NO_COURSES", "There are no courses." },
            { "COURSE_LIST", "Here are the courses you can join: ```{0}```" },
            { "COURSE_LIST_ITEM", "\n{0}" },
            { "COURSE_UNLINKED", ":white_check_mark:  Successfuly unlinked course." },
            { "INVALID_COURSE", ":warning:  Course not found." },
            { "INVALID_COURSE_CREATE_NAME", ":warning:  Invalid course name." },
            { "DUPLICATE_COURSE", ":warning:  Course already exists." },
            { "COURSE_ADDED", ":white_check_mark:  Successfuly added course." },
            { "COURSE_ADDED_EXISTING_CHANNEL", ":white_check_mark:  Successfuly added existing channel as course." },
            { "COURSE_UPDATE_STARTED", "Course update started..." },
            { "COURSE_UPDATE_SUCCESS", "Course update succeeded." },
            { "COURSE_UPDATE_FAIL", "Course update failed. Please check the logs for more information." },
            { "COURSE_EMPTY", "There are no users in that course." },
            { "CHANNELS_ORGANISED", ":white_check_mark:  Organised {0} channels." },
            { "CHANNEL_ORGANISED", ":white_check_mark:  Organised {0}." },
            { "CHANNELS_PERMISSIONS_UPDATED", ":white_check_mark:  Updated permissions for {0} channels." },
            { "CHANNEL_PERMISSIONS_UPDATED", ":white_check_mark:  Updated permissions for {0}." },
            { "CHANNELS_IMPORTED", ":white_check_mark:  Imported {0} courses." },
            { "NO_CHANNELS_IMPORTED", ":warning:  No courses imported. Is your RegEx valid?" },
            { "INVALID_CHANNEL", ":warning:  Channel not found." },
            { "CATEGORY_ADDED", ":white_check_mark:  Successfully added category." },
            { "CATEGORY_ADDED_EXISTING", ":white_check_mark:  Successfully added existing category." },
            { "CATEGORY_ADD_HELP", "Creates/adds a category for course channels.\n" +
                "You can specify a RegEx auto import rule for a category to define which category new courses are added to.\n" +
                "The auto import priority specifies the order in which the auto import rule on categories are checked. A higher value is checked before a lower value." +
                "Use a value less than 0 disable auto import\n Examples:\n```{0}createcategory 100-Level [a-z]{{4}}-1\\d\\d 1```" +
                "```{0}createcategory 733285993481896008 [a-z]{{4}}-2\\d\\d 2``````{0}createcategory \"Text Channels\"```\n\n" +
                "To delete a category, just delete the Discord category." },
            { "INVALID_CATEGORY", ":warning:  Category not found." },
            { "INVALID_CATEGORY_CREATE_NAME", ":warning:  Invalid category name." },
            { "INVALID_CATEGORY_AUTO_IMPORT_REGEX", ":warning:  Invalid auto import RegEx. Try something like `ecen-1\\d\\d` to match all 100 level ECEN courses" },
            { "INVALID_REGEX", ":warning:  Invalid RegEx pattern." },
            { "ENROLLMENT_ALREADY_ENROLLED", ":warning:  **{0}** - You are already in `{0}`.\n" },
            { "ENROLLMENT_ALREADY_LEFT", ":warning:  **{0}** - You are not in `{0}`.\n" },
            { "ENROLLMENT_INVALID_COURSE",  ":warning:  **{0}** - Sorry `{0}` does not exist.\n" },
            { "ENROLLMENT_SERVER_ERROR",  ":fire:  **{0}** - A server error occured. Please ask an admin to check the logs.\n" },
            { "ENROLLMENT_JOIN_SUCCESS",  ":inbox_tray:  **{0}** - Added you to {0} successfully.\n" },
            { "ENROLLMENT_LEAVE_SUCCESS",  ":outbox_tray:  **{0}** - Removed you from {0} successfully.\n" },
            { "ENROLLMENT_VERIFICATION_REQUIRED",  ":warning:  **{0}** - Sorry you must be verified before you can join any courses.\n" +
                "Private message me the following command to verify: ```+verify username@myvuw.ac.nz```" },
            { "ENROLLMENT_NO_COURSES_JOINED",  ":warning:  You are not in any courses." },
        });

        private readonly IDictionary<string, string> _translationMap;

        private Translator(IDictionary<string, string> translationsMap)
        {
            _translationMap = translationsMap;
        }

        public string T(string key, params object[] values)
        {
            return Translate(key, values);
        }

        public string Translate(string key, params object[] values)
        {
            if (!_translationMap.ContainsKey(key))
                throw new TranslationNotFoundException(key);
            return string.Format(_translationMap[key], values);
        }
    }
}
