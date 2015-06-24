namespace FastCGI
{
    /// <summary>
    /// FastCGI constants.
    /// Defined in the FastCGI specification section 8.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// Listening socket file number
        /// </summary>
        public const int FCGI_LISTENSOCK_FILENO = 0;

        /// <summary>
        /// Number of bytes in a FCGI_Header.
        /// Future versions of the protocol will not reduce this number.
        /// </summary>
        public const int FCGI_HEADER_LEN = 8;

        /// <summary>
        /// Value for version component of FCGI_Header
        /// </summary>
        public const int FCGI_VERSION_1 = 1;

        /// <summary>
        /// Values for type component of FCGI_Header
        /// </summary>
        public const int FCGI_BEGIN_REQUEST = 1;
        /// <summary>
        /// Values for type component of FCGI_Header
        /// </summary>
        public const int FCGI_ABORT_REQUEST = 2;
        /// <summary>
        /// Values for type component of FCGI_Header
        /// </summary>
        public const int FCGI_END_REQUEST = 3;
        /// <summary>
        /// Values for type component of FCGI_Header
        /// </summary>
        public const int FCGI_PARAMS = 4;
        /// <summary>
        /// Values for type component of FCGI_Header
        /// </summary>
        public const int FCGI_STDIN = 5;
        /// <summary>
        /// Values for type component of FCGI_Header
        /// </summary>
        public const int FCGI_STDOUT = 6;
        /// <summary>
        /// Values for type component of FCGI_Header
        /// </summary>
        public const int FCGI_STDERR = 7;
        /// <summary>
        /// Values for type component of FCGI_Header
        /// </summary>
        public const int FCGI_DATA = 8;
        /// <summary>
        /// Values for type component of FCGI_Header
        /// </summary>
        public const int FCGI_GET_VALUES = 9;
        /// <summary>
        /// Values for type component of FCGI_Header
        /// </summary>
        public const int FCGI_GET_VALUES_RESULT = 10;
        /// <summary>
        /// Values for type component of FCGI_Header
        /// </summary>
        public const int FCGI_UNKNOWN_TYPE = 11;
        /// <summary>
        /// Values for type component of FCGI_Header
        /// </summary>
        public const int FCGI_MAXTYPE = FCGI_UNKNOWN_TYPE;

        /// <summary>
        /// Value for requestId component of FCGI_Header
        /// </summary>
        public const int FCGI_NULL_REQUEST_ID = 0;

        /// <summary>
        /// Mask for flags component of FCGI_BeginRequestBody
        /// </summary>
        public const int FCGI_KEEP_CONN = 1;

        /// <summary>
        /// Values for role component of FCGI_BeginRequestBody
        /// </summary>
        public const int FCGI_RESPONDER = 1;
        /// <summary>
        /// Values for role component of FCGI_BeginRequestBody
        /// </summary>
        public const int FCGI_AUTHORIZER = 2;
        /// <summary>
        /// Values for role component of FCGI_BeginRequestBody
        /// </summary>
        public const int FCGI_FILTER = 3;

        /// <summary>
        /// Values for protocolStatus component of FCGI_EndRequestBody
        /// </summary>
        public const int FCGI_REQUEST_COMPLETE = 0;
        /// <summary>
        /// Values for protocolStatus component of FCGI_EndRequestBody
        /// </summary>
        public const int FCGI_CANT_MPX_CONN = 1;
        /// <summary>
        /// Values for protocolStatus component of FCGI_EndRequestBody
        /// </summary>
        public const int FCGI_OVERLOADED = 2;
        /// <summary>
        /// Values for protocolStatus component of FCGI_EndRequestBody
        /// </summary>
        public const int FCGI_UNKNOWN_ROLE = 3;

        /// <summary>
        /// Variable names for FCGI_GET_VALUES / FCGI_GET_VALUES_RESULT records
        /// </summary>
        public const string FCGI_MAX_CONNS = "FCGI_MAX_CONNS";
        /// <summary>
        /// Variable names for FCGI_GET_VALUES / FCGI_GET_VALUES_RESULT records
        /// </summary>
        public const string FCGI_MAX_REQS = "FCGI_MAX_REQS";
        /// <summary>
        /// Variable names for FCGI_GET_VALUES / FCGI_GET_VALUES_RESULT records
        /// </summary>
        public const string FCGI_MPXS_CONNS = "FCGI_MPXS_CONNS";
    }

}
