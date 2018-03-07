using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WCFUtils {
    public class Log4netTraceListener : System.Diagnostics.TraceListener {
        private readonly log4net.ILog log = null;

        public Log4netTraceListener() {
            log = log4net.LogManager.GetLogger("System.Diagnostics.Redirection");
        }
        public Log4netTraceListener(string name) {
            this.log = log4net.LogManager.GetLogger("System.Diagnostics.Redirection." + name);
        }
        public Log4netTraceListener(log4net.ILog log) {
            this.log = log;
        }
        public override void Write(string message) {
            if(log != null) {
                log.Debug(message);
            }
        }

        public override void WriteLine(string message) {
            if(log != null) {
                log.Debug(message);
            }
        }
    }
}
