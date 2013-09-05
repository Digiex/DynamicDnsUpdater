﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DynamicDnsUpdater
{
    public class DynDnsHost
    {
        public string Hostname { get; set; }
        public string UpdateUrl { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        public override string ToString()
        {
            return Hostname;
        }
    }
}
