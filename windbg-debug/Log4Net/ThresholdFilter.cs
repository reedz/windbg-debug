using System;
using System.Linq;
using log4net.Core;
using log4net.Filter;
using log4net.Util;

namespace WinDbgDebug.Log4Net
{
    public class ThresholdFilter : LevelRangeFilter
    {
        public PatternString Threshold { get; set; }

        public override void ActivateOptions()
        {
            base.ActivateOptions();
            LevelMin = MapLevel(Threshold.Format());
            LevelMax = Level.Fatal;
        }

        private Level MapLevel(string level)
        {
            return Logging.PossibleLogLevels.FirstOrDefault(x => string.Equals(x.Name, level, StringComparison.OrdinalIgnoreCase)) ?? Logging.DefaultClientLogLevel;
        }
    }
}