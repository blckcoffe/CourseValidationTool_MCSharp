using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Threading;

namespace CourseValidationTool_CSharp
{
    public enum ErrorCode
    {
        SUCCESS = 0,
        ERROR = 1,
        WRONG_FOLDER = 2
    }
    public partial class FirstPage : Form
    {

        public FirstPage()
        {
            InitializeComponent();
        }

        private void OpenFileFolderBtn_Click(object sender, EventArgs e)
        {
            folderBrowserDialog.ShowDialog();
            fileFolderText.Text = folderBrowserDialog.SelectedPath;
        }

        private int validateCourse(string FileFolder, string enCoding)
        {
            childDirectoryProcessor jsonFileProcessor;
            WaitHandle waitHandle;
            List < WaitHandle> waitHandles = new List<WaitHandle>();
            List< childDirectoryProcessor > childDirectoryProcessors = new List<childDirectoryProcessor>();
            DirectoryInfo parentDirectory = new DirectoryInfo( FileFolder );
            DirectoryInfo[] childDirectories = parentDirectory.GetDirectories();

            if ( childDirectories.Length == 0 )
            {
                return 2;
            }

            foreach (DirectoryInfo childDirectory in parentDirectory.GetDirectories())
            {
                jsonFileProcessor = new childDirectoryProcessor(childDirectory, enCoding);
                childDirectoryProcessors.Add(jsonFileProcessor);
                waitHandle = new AutoResetEvent(false);
                waitHandles.Add(waitHandle);
                ThreadPool.QueueUserWorkItem(new WaitCallback(jsonFileProcessor.executeCourseCheck), waitHandle);
                //Thread.Sleep(100);
            }

            foreach (var m in waitHandles)
                m.WaitOne();

            StringBuilder processLog = new StringBuilder();
            int result = 0;
            foreach (childDirectoryProcessor f in childDirectoryProcessors)
            {
                processLog.Append( f.getExecutionLog());
                processLog.Append("\r\n");
                result = ( result == 1 )? 1: f.getResult();
            }

            richTextBox.Text = processLog.ToString();
            return result;
        }

        private void validateCourseBtn_Click(object sender, EventArgs e)
        {
            string enCodeCode = enCodingList.GetItemText(enCodingList.SelectedItem);
            if (enCodeCode == "")
            {
                enCodeCode = "GB2312";
            }

            if ( fileFolderText.Text != "" )
            {
                int result = validateCourse( fileFolderText.Text, enCodeCode);

                switch (result)
                {
                    case 0:
                        MessageBox.Show("没有发现问题", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        break;
                    case 1:
                        MessageBox.Show("发现问题", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        break;
                    case 2:
                        MessageBox.Show("请选择正确的路径", "错误", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        break;
                    default:
                        return;

                }
            }
            else
            {
                MessageBox.Show("请选择课程所在目录", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void TestEncodingBtn_Click(object sender, EventArgs e)
        {
            richTextBox.Text = "";
            string enCodeCode = enCodingList.GetItemText(enCodingList.SelectedItem);
            //if((enCodeCode == "")||(fileFolderText.Text == "")){
           if (fileFolderText.Text == "")
           {
                MessageBox.Show("请选择课程所在目录", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            JsonFileProcessor jsonFileProcessor = new JsonFileProcessor(fileFolderText.Text, enCodeCode);
            string jsonFile = jsonFileProcessor.ReadJsonFile(fileFolderText.Text);
            if (jsonFile == "")
            {
                MessageBox.Show("当前目录下没有找到Json文件", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            richTextBox.Text = jsonFile;
        }
    }
}
