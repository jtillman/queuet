using System;

namespace AspNetCoreWebApp
{
    public class Response<T> where T : struct, Enum
    {
        public T? ErrorCode { get; }

        public bool HasError => ErrorCode.HasValue;

        public Response(T errorCode) { ErrorCode = errorCode; }

        public Response() { }
    }

    public class Response<T, V> : Response<V> where V : struct, Enum
    {
        public T Result { get; }

        public Response(T result) { Result = result; }

        public Response(V errorCode) : base(errorCode) { }
    }
}
