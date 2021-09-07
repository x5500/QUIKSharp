using QUIKSharp;

namespace QuikSharp.Tests.Helpers
{
    struct Sec : ISecurity
    {
        public string ClassCode { get; set; }
        public string SecCode { get; set; }
    }
}
