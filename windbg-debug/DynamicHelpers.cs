using System;

namespace WinDbgDebug
{
    public static class DynamicHelpers
    {
        #region Public Methods

        public static T To<T>(dynamic item, T defaultValue = default(T))
        {
            if (item == null)
                return defaultValue;

            try
            {
                return (T)Convert.ChangeType(item, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }

        #endregion
    }
}
