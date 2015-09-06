using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nimbus
{
    public class MediaFactory
    {
        private Func<Uri, bool> _predicate;
        private Func<Uri, Media> _constructor;
        public string Example { get; protected set; }

        public MediaFactory(Func<Uri, bool> predicate, Func<Uri, Media> constructor, string example)
        {
            _predicate = predicate;
            _constructor = constructor;
            Example = example;
        }

        public bool CanAccept(Uri uri)
        {
            return _predicate(uri);
        }

        public Media Construct(Uri uri)
        {
            return _constructor(uri);
        }
    }
}
