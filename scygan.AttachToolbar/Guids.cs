// Guids.cs
// MUST match guids.h
using System;

namespace scygan.AttachToolbar {
    public static class GuidList {
        public const string guidAttachToPkgString = "8d6080f0-7276-44d7-8dc4-6378fb6ce225";

        public const string guidAttachToCmdSetString = "00680076-59CC-4238-A798-693D673602AC";

        public static readonly Guid guidAttachToCmdSet = new Guid(guidAttachToCmdSetString);
    }
}