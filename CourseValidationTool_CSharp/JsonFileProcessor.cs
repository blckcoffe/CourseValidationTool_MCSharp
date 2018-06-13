using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using NPinyin;
using System.Text.RegularExpressions;


namespace CourseValidationTool_CSharp
{

    public struct JsonFileNameContent
    {
        string fileName;
        string fileContent;
    }

    class LogCache
    {
        private StringBuilder logBuffer;
        public LogCache()
        {
            logBuffer = new StringBuilder();
        }

        public void Write(string log)
        {
            logBuffer.Append(log);
        }

        public string GetLog()
        {
            return logBuffer.ToString();
        }
    }

    public class PinYinConverterHelp
    {
        public static string Convert2Pinyin( string src)
        {
            string sResult = Pinyin.GetPinyin(src);
            return Regex.Replace(sResult, @"\s", "");
        }
    }

    class JsonFileProcessor
    {
        private string CourseName;
        private List<string> CourseVideoInFolder;
        private List<string> CourseVideoInJsonOfSecton;
        private List<string> CourseVideoInJsonOfLinkedVideo;
        private LogCache swLog;
        string directoryFullName, enCoding;
        //private StreamWriter swLog;

        public JsonFileProcessor(String Directory, string enCoding)
        {
            CourseVideoInFolder = new List<string>();
            CourseVideoInJsonOfSecton = new List<string>();
            CourseVideoInJsonOfLinkedVideo = new List<string>();
            swLog = new LogCache();
            directoryFullName = Directory;
            this.enCoding = enCoding;
        }

        ~JsonFileProcessor()
        {
            //swLog.Flush();
            //swLog.Close();
            //fsLog.Close();
        }

        public string GetLog()
        {
            return swLog.GetLog();
            //swLog.Flush();
            //swLog.Close();
            //fsLog.Close();
        }
        public static System.Text.Encoding GetType(FileStream fs)
        {
            byte[] Unicode = new byte[] { 0xFF, 0xFE, 0x41 };
            byte[] UnicodeBIG = new byte[] { 0xFE, 0xFF, 0x00 };
            byte[] UTF8 = new byte[] { 0xEF, 0xBB, 0xBF }; //带BOM 
            Encoding reVal = Encoding.Default;

            BinaryReader r = new BinaryReader(fs, System.Text.Encoding.Default);
            int i;
            int.TryParse(fs.Length.ToString(), out i);
            byte[] ss = r.ReadBytes(i);
            if (IsUTF8Bytes(ss) || (ss[0] == 0xEF && ss[1] == 0xBB && ss[2] == 0xBF))
            {
                reVal = Encoding.UTF8;
            }
            else if (ss[0] == 0xFE && ss[1] == 0xFF && ss[2] == 0x00)
            {
                reVal = Encoding.BigEndianUnicode;
            }
            else if (ss[0] == 0xFF && ss[1] == 0xFE && ss[2] == 0x41)
            {
                reVal = Encoding.Unicode;
            }
            //r.Close();
            return reVal;

        }

        private static bool IsUTF8Bytes(byte[] data)
        {
            int charByteCounter = 1; //计算当前正分析的字符应还有的字节数 
            byte curByte; //当前分析的字节. 
            for (int i = 0; i < data.Length; i++)
            {
                curByte = data[i];
                if (charByteCounter == 1)
                {
                    if (curByte >= 0x80)
                    {
                        //判断当前 
                        while (((curByte <<= 1) & 0x80) != 0)
                        {
                            charByteCounter++;
                        }
                        //标记位首位若为非0 则至少以2个1开始 如:110XXXXX...........1111110X 
                        if (charByteCounter == 1 || charByteCounter > 6)
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    //若是UTF-8 此时第一位必须为1 
                    if ((curByte & 0xC0) != 0x80)
                    {
                        return false;
                    }
                    charByteCounter--;
                }
            }
            if (charByteCounter > 1)
            {
                throw new Exception("非预期的byte格式");
            }
            return true;
        }

        private string _ReadJsonFileContent(string jsonFile)
        {
            string filecontent;
            FileStream fs = new FileStream(jsonFile, FileMode.Open, FileAccess.Read);

            Encoding r = GetType(fs);
            string enCodingLocal = "UTF-8";
            if (r != Encoding.UTF8)
            {
                enCodingLocal = "GBK";
            }

            StreamReader sr = new StreamReader(fs, Encoding.GetEncoding(enCodingLocal));
            sr.BaseStream.Seek(0, SeekOrigin.Begin);
            filecontent = sr.ReadToEnd();
            sr.Close();
            fs.Close();

            return filecontent;
        }

        public string[] ReadJsonFile(string FilePath )
        {
            string jsonFile = "";
            List<string> jsonFileContentlist = new List<string>(2);
            var jsonFileContentArr = new string[2];
            string filecontent = "";
            DirectoryInfo folder = new DirectoryInfo(FilePath);
            string filenamePinyin = PinYinConverterHelp.Convert2Pinyin(folder.Name);
            _WriteLogWithEnter("文件夹拼音: " + filenamePinyin, 1);
            foreach (FileInfo file in folder.GetFiles("*.json"))
            {
                filecontent = _ReadJsonFileContent(file.FullName);
                if ( file.FullName.IndexOf("_Extend.json") < 0 )
                {
                    jsonFileContentArr[0] = filecontent;
                }
                else
                {
                    jsonFileContentArr[1] = filecontent;
                }
            }

            if ( jsonFile == "")
            {
                _WriteLogWithEnter(FilePath + "文件夹中不包含相关Json文件", 1);
            }

            return jsonFileContentArr;
        }

        public Boolean ValidateFolderFiles(string FilePath)
        {
            Boolean result = true;
            Boolean tmpResult = true;
            string localFileFolder = FilePath;
            string[] strArray = localFileFolder.Split('\\');
            this.CourseName = strArray[strArray.Length - 1];

            string regEpx = "^" + strArray[strArray.Length - 1] + "[_][0-9]*";
            DirectoryInfo folder = new DirectoryInfo(localFileFolder);
            FileInfo[] files = folder.GetFiles("*.mp4");
            if ( files.Length == 0)
            {
                _WriteLogWithEnter("文件夹中不包含任何《.mp4》文件", 1);
                result = false;
            }
            foreach ( FileInfo file in folder.GetFiles("*.mp4"))
            {
                this.CourseVideoInFolder.Add(file.Name);
                tmpResult = Regex.IsMatch(file.Name, regEpx);
                if (!tmpResult)
                {
                    _WriteLogWithEnter("文件夹中课程名称和视频不匹配\r\n   课程名称：《" + strArray[strArray.Length - 1] + "》  视频名称：" + file.Name, 1);
                    result = false;
                }
            }
            return result;
        }

        public Boolean ValidateJsonContent(JsonResult JsonContent )
        {
            Boolean result = true;
            string coursNameInJson = JsonContent.notetitle;
            if (coursNameInJson != this.CourseName)
            {
                _WriteLogWithEnter("NoteTile中的课程名《" + coursNameInJson +  "》与文件夹名称不一致", 1 );
                result = false;
            }

            int indexStart, indexEnd;
            string videoInJsonOfSection;
            CourseVideoInJsonOfLinkedVideo = JsonContent.linkedvideo;

            //匹配视频名称： 课程名_数字.mp4 或者 课程名.obb
            string regEpx = "^((" + coursNameInJson + "_)?\\d *\\.mp4)|(" + coursNameInJson + ".obb)";
            foreach (string videoName in CourseVideoInJsonOfLinkedVideo)
            {
                if ( !Regex.IsMatch(videoName, regEpx) )
                {
                    _WriteLogWithEnter("LinkedVideo中的视频名称与notetile不一致：" + videoName, 1);
                    result = false;
                }
            }

            foreach (CourseValidationTool_CSharp.Notecontent noteContent in JsonContent.notecontent )
            {
                if (noteContent.sectioncontent != null)
                {
                    foreach (CourseValidationTool_CSharp.Sectioncontent sectionContent in noteContent.sectioncontent )
                    {
                        foreach (string section in sectionContent.section)
                        {
                            indexStart = section.IndexOf("播放课件《");
                            if ( indexStart != -1)
                            {
                                indexEnd = section.IndexOf("》");
                                if (indexEnd != -1 )
                                {
                                    videoInJsonOfSection = section.Substring(indexStart + 5, indexEnd - indexStart-5) + ".mp4";
                                    if (CourseVideoInJsonOfLinkedVideo.IndexOf(videoInJsonOfSection) == -1)
                                    {
                                        _WriteLogWithEnter("Json sectioncontent中的视频在linkedvideo 中不存在\r\n  视频：" + videoInJsonOfSection, 1);
                                        result = false;
                                    }
                                    CourseVideoInJsonOfSecton.Add(videoInJsonOfSection);
                                }
                            }
                        }
                    }
                }
            }

            if ( CourseVideoInJsonOfSecton.Count != CourseVideoInFolder.Count )
            {
                _WriteLogWithEnter("Json sectioncontent 包含的视频个数和文件夹中不一致" + "  Json sectioncontent视频个数" + CourseVideoInJsonOfSecton.Count
                    + "   文件夹中视频个数" + CourseVideoInFolder.Count
                    , 1 );
                result = false;
            }

            if (CourseVideoInJsonOfSecton.Count != CourseVideoInJsonOfLinkedVideo.Count - 1)
            {
                _WriteLogWithEnter("Json sectioncontent 包含的视频个数和 linkedvideo 中不一致\r\n" + "  Json sectioncontent视频个数" + CourseVideoInJsonOfSecton.Count
                    + "   linkedvideo 中视频个数" + (CourseVideoInJsonOfLinkedVideo.Count - 1)
                    , 1);
                result = false;
            }

            if (CourseVideoInJsonOfLinkedVideo.IndexOf(CourseName + ".obb") == -1)
            {
                _WriteLogWithEnter("《" + CourseName + ".obb 》在linkedvideo 中不存在", 1);
                result = false;
            }

            foreach ( string courseName in CourseVideoInFolder)
            {
                if(CourseVideoInJsonOfLinkedVideo.IndexOf(courseName) == -1 )
                {
                    _WriteLogWithEnter("文件夹中的视频《" + courseName + "》在Json文件linkedvideo中未找到 ", 1 );
                    result = false;
                }else if (CourseVideoInJsonOfSecton.IndexOf(courseName) == -1)
                {
                    _WriteLogWithEnter("文件夹中的视频《" + courseName + "》在Json文件sectioncontent中未找到 ", 1);
                    result = false;
                }
            }

            return result;
        }

        private int _ProcessJsonFileFolder(string FileFolder )
        {
            int result = 0;

            var fileContentArr = ReadJsonFile(FileFolder);


            string fileContent = fileContentArr[0];
            if ((fileContent == "") || ( fileContent == null))
            {
                _WriteLogWithEnter("读取Json失败", 1);
                return 1;
            }
            _WriteLogWithEnter("开始检查Json文件： ", 0);
            JsonResult deserializedProduct = JsonConvert.DeserializeObject<JsonResult>(fileContent);
            if (deserializedProduct == null)
            {
                _WriteLogWithEnter("Json文件解析失败，请先检查json文件格式是否正确", 1);
                return 1;
            }
            Boolean jsonResult = ValidateJsonContent(deserializedProduct);
            if (true == jsonResult)
            {
                _WriteLogWithEnter("Json文件检查成功", 0);
            }
            else
            {
                _WriteLogWithEnter("Json文件检查失败", 1);
                result = 1;
            }


            fileContent = fileContentArr[1];
            if ((fileContent != "") && (fileContent != null))
            {
                JsonResult deserializedProduct_extend = JsonConvert.DeserializeObject<JsonResult>(fileContent);
                if (deserializedProduct_extend == null)
                {
                    _WriteLogWithEnter("Extend Json文件解析失败，请先检查Extend json文件格式是否正确", 1);
                    return 1;
                }

                var extendCourseFolder = new JsonFileProcessor( FileFolder + "_延伸", this.enCoding);
                var strArray = FileFolder.Split('\\');
                extendCourseFolder.CourseName = strArray[strArray.Length - 1] + "_延伸";
                Boolean jsonResult_extend = extendCourseFolder.ValidateJsonContent(deserializedProduct_extend);
                extendCourseFolder.ValidateFolderFiles(extendCourseFolder.CourseName);
                if (true == jsonResult_extend)
                {
                    _WriteLogWithEnter("Extend Json文件检查成功", 0);
                }
                else
                {
                    _WriteLogWithEnter("Extend Json文件检查失败", 1);
                    result = 1;
                }
            }

            return result;
        }


        public int ProcessJsonFileFolder( )
        {
            int result = 0;
            string FileFolder = directoryFullName;
            string enCoding = this.enCoding;


            System.DateTime currentTime = new System.DateTime();
            currentTime = System.DateTime.Now;
            string strY = currentTime.ToString("yyyy-MM-dd HH:mm:ss.fff");
            _WriteLogWithEnter(strY, 0);
            _WriteLogWithEnter("开始检查，文件路径：" + FileFolder, 0 );
            Boolean folderResult = ValidateFolderFiles(FileFolder);
            if ( true == folderResult)
            {
                _WriteLogWithEnter("文件夹检查成功", 0);
            }
            else
            {
                _WriteLogWithEnter("文件夹检查失败", 1);
                result = 1;
            }

            string fileContent = ReadJsonFile(FileFolder)[0];
            if (( fileContent == "")||( fileContent == null ))
            {
                _WriteLogWithEnter("读取Json失败", 1);
                return 1;

            }

            _WriteLogWithEnter("开始检查Json文件： ", 0);
            JsonResult deserializedProduct = JsonConvert.DeserializeObject<JsonResult>(fileContent);
            if( deserializedProduct == null)
            {
                _WriteLogWithEnter("Json文件解析失败，请先检查json文件格式是否正确", 1);
                return 1;
            }
            Boolean jsonResult = ValidateJsonContent(deserializedProduct);
            if (true == jsonResult)
            {
                _WriteLogWithEnter("Json文件检查成功", 0);
            }
            else
            {
                _WriteLogWithEnter("Json文件检查失败", 1);
                result = 1;
            }

            return result;
        }

        private void _WriteLogWithEnter(string log, int type ) // Type 0: Information, 1: Error
        {
            switch( type )
            {
                case 0:
                    swLog.Write(log + "\r\n"); 
                    break;
                case 1:
                    swLog.Write( "ERROR: " + log + "\r\n");
                    break;
                default:
                    swLog.Write(log + "\r\n");
                    break;
            }
        }
    }
}
