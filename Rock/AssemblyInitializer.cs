﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Rock
{
    public static class AssemblyInitializer
    {
        public static void Initialize()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Step 1: Load registered HTTP Modules - http://blog.davidebbo.com/2011/02/register-your-http-modules-at-runtime.html
            var activeHttpModules = Rock.Web.HttpModules.HttpModuleContainer.GetActiveComponents(); // takes 8 seconds :(

            foreach( var httpModule in activeHttpModules )
            {
                HttpApplication.RegisterModule( httpModule.GetType() );
            }

            stopwatch.Stop();

            var elapsedTime = stopwatch.Elapsed;
        }
    }
}