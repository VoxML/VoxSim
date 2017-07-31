using UnityEngine;
using UnityEditor;

namespace Crosstales.RTVoice.EditorExt
{
    /// <summary>Reminds the customer to create an UAS review.</summary>
    [InitializeOnLoad]
    public static class ReminderCheck
    {
        #region Constructor

        static ReminderCheck()
        {
            if (Util.Config.REMINDER_CHECK)
            {
                if (Util.Config.DEBUG)
                    Debug.Log("Reminder enabled!");

                string lastDate = EditorPrefs.GetString(Util.Constants.KEY_REMINDER_DATE);
                string date = System.DateTime.Now.ToString("yyyyMMdd"); // every day
                //string date = System.DateTime.Now.ToString("yyyyMMddmm"); // every minute (for tests)

                if (!date.Equals(lastDate))
                {
                    int count = EditorPrefs.GetInt(Util.Constants.KEY_REMINDER_COUNT) + 1;

                    if (Util.Constants.DEV_DEBUG)
                        Debug.Log("Current count: " + count);

                    //if (count % 2 == 0) // for testing only
                    if (count % 7 == 0)
                    {
                        if (Util.Config.DEBUG)
                            Debug.Log("Reminder active...");

                        int option = EditorUtility.DisplayDialogComplex(Util.Constants.ASSET_NAME + " - Reminder",
                                    "Please don't forget to rate " + Util.Constants.ASSET_NAME + " or even better write a little review – it would be very much appreciated!",
                                    "Yes, let's do it!",
                                    "Not right now",
                                    "Don't ask again!");

                        if (option == 0)
                        {
                            Application.OpenURL(Util.Constants.ASSET_URL);
                            Util.Config.REMINDER_CHECK = false;
                        }
                        else if (option == 1)
                        {
                            // do nothing!
                        }
                        else
                        {
                            Util.Config.REMINDER_CHECK = false;
                        }

                        Util.Config.Save();
                    }
                    else
                    {
                        if (Util.Config.DEBUG)
                            Debug.Log("No reminder needed.");
                    }

                    EditorPrefs.SetString(Util.Constants.KEY_REMINDER_DATE, date);
                    EditorPrefs.SetInt(Util.Constants.KEY_REMINDER_COUNT, count);
                }
            }
            else
            {
                if (Util.Config.DEBUG)
                    Debug.Log("Reminder disabled!");
            }
        }

        #endregion

    }
}
// © 2017 crosstales LLC (https://www.crosstales.com)