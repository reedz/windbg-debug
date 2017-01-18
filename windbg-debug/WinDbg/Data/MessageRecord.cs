using System;
using WinDbgDebug.WinDbg.Messages;
using WinDbgDebug.WinDbg.Results;

namespace WinDbgDebug.WinDbg.Data
{
    public class MessageRecord
    {
        #region Constructor

        public MessageRecord(Message message, Action<MessageResult> resultSetter)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (resultSetter == null)
                throw new ArgumentNullException(nameof(resultSetter));

            Message = message;
            ResultSetter = resultSetter;
        }

        #endregion

        #region Public Properties

        public Message Message { get; private set; }
        public Action<MessageResult> ResultSetter { get; private set; }

        #endregion
    }
}
