﻿namespace NineChronicles.Standalone.Requests
{
    public struct SetMiningRequest
    {
        public bool Mine { get; set; }
        public string PrivateKeyString { get; set; }
    }
}
