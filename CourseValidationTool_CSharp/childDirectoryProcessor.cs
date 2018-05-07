using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;

namespace CourseValidationTool_CSharp
{
    class childDirectoryProcessor
    {
        private JsonFileProcessor jsonFileProcessor;
        string directoryFullName, enCoding;
        int result = 0;

        public childDirectoryProcessor( DirectoryInfo Directory, string enCoding)
        {
            this.directoryFullName = Directory.FullName;
            this.enCoding = enCoding;
            jsonFileProcessor = new JsonFileProcessor(directoryFullName, enCoding);

        }

        public void executeCourseCheck(Object state)
        {
            AutoResetEvent are = (AutoResetEvent)state;
            result = jsonFileProcessor.ProcessJsonFileFolder();
            are.Set();
        }

        public string getExecutionLog()
        {
            return jsonFileProcessor.GetLog();
        }

        public int getResult()
        {
            return result;
        }
    }
}
