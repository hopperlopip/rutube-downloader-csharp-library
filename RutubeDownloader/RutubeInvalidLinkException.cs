using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RutubeDownloader
{
    [Serializable]
    public class RutubeInvalidLinkException : Exception
    {
        public RutubeInvalidLinkException() { }

        public RutubeInvalidLinkException(string message) : base(message) { }
    }
}
