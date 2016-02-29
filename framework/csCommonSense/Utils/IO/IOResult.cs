using System;

namespace csCommon.Utils.IO
{
    public enum IOResultType
    {
        Error, Object
    }

    public class IOResult<TOut>
    {
        private readonly IOResultType _ioResultType;
        private readonly TOut _result;
        private readonly Exception _exception;

        public IOResult(TOut result)
        {
            _ioResultType = IOResultType.Object;
            _result = result;
        }

        public IOResult(Exception exception)
        {
            _ioResultType = IOResultType.Error;
            _exception = exception;
        }

        public IOResultType ResultType
        {
            get { return _ioResultType; }
        }

        public TOut Result
        {
            get { return _result; }
        }

        public Exception Exception
        {
            get { return _exception; }
        }

        public bool Successful
        {
            get { return ResultType != IOResultType.Error; }
        }
    }
}
