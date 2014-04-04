using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageDownloader
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = null;

            ImageDownloader imageDownloader = new ImageDownloader(textBox1.Text);
            imageDownloader.LoadCompleted += imageDownloader_LoadCompleted;
            imageDownloader.LoadProgressChanged += imageDownloader_LoadProgressChanged;
            imageDownloader.DownloadAsync();
        }

        private void imageDownloader_LoadProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            label1.Text= e.ProgressPercentage.ToString();
        }

        private void imageDownloader_LoadCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                Image img = (Image)e.UserState;
                pictureBox1.Image = img;
            }
            else
            {
                Debug.Fail(e.Error.Message);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = null;
            string filePath = Path.GetTempPath() + Path.PathSeparator + Path.GetFileName(textBox1.Text);
            FileDownloader fileDownloader = new FileDownloader(textBox1.Text, filePath);
            fileDownloader.LoadCompleted += fileDownloader_LoadCompleted;
            fileDownloader.LoadProgressChanged +=fileDownloader_LoadProgressChanged;
            fileDownloader.DownloadAsync();
        }

        private void fileDownloader_LoadProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            label1.Text = e.ProgressPercentage.ToString();
        }

        private void fileDownloader_LoadCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                string filePath = (string)e.UserState;
                using(FileStream fs = new FileStream(filePath, FileMode.Open,FileAccess.Read))
                {
                    pictureBox1.Image = Image.FromStream(fs);
                }
            }
        }
    }
}
