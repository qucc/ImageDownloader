using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageDownloader
{
    public  class FileDownloader 
    {
        private string url;
        private string filePath;
        private long contentLength;
        private long totalBytesRead;
        private Stream tempDownloadStream;
        private const int readBlockSize = 2096;
        private byte[] readBuffer = null;
        private AsyncOperation currentAsycLoadOperation = null;
        private SendOrPostCallback loadCompletedDelegate = null;
        private SendOrPostCallback loadProgressDelegate = null;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <param name="filepath">输入文件路径</param>
        public FileDownloader(string url, string filepath)
        {
            this.url = url;
            this.filePath = filepath;
        }

        public void DownloadAsync()
        {
            
            currentAsycLoadOperation = AsyncOperationManager.CreateOperation(null);
            if (loadCompletedDelegate == null)
            {
                loadCompletedDelegate = new SendOrPostCallback(LoadCompletedDelegate);
                loadProgressDelegate = new SendOrPostCallback(LoadProgressDelegate);
                readBuffer = new byte[readBlockSize];
            }
            tempDownloadStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write);

            WebRequest req = WebRequest.Create(url);
            (new WaitCallback(BeginGetResponseDelegate)).BeginInvoke(req, null, null);
 
        }


        private void BeginGetResponseDelegate(object state)
        {
            WebRequest req = (WebRequest)state;
            req.BeginGetResponse(new AsyncCallback(GetResponseCallback), req);
        }

        private void PostCompleted(Exception error, bool cancelled)
        {
            AsyncOperation temp = currentAsycLoadOperation;
            currentAsycLoadOperation = null;
            if (temp != null)
            {
                temp.PostOperationCompleted(loadCompletedDelegate, new AsyncCompletedEventArgs(error,cancelled,null));
            }
        }

        private void LoadProgressDelegate(object state)
        {
            OnLoadProgressChanged((ProgressChangedEventArgs)state);
        }

        private void LoadCompletedDelegate(object state)
        {
            AsyncCompletedEventArgs e = (AsyncCompletedEventArgs)state;
            if (!e.Cancelled && e.Error == null)
            {
                //successful completion
                 e = new AsyncCompletedEventArgs(e.Error, e.Cancelled, filePath);
            }
            tempDownloadStream.Close();
            tempDownloadStream = null;
            OnLoadCompleted(e);
        }

        private void GetResponseCallback(IAsyncResult ar)
        {
            try
            {
                WebRequest req = (WebRequest)ar.AsyncState;
                WebResponse webResponse = req.EndGetResponse(ar);
                contentLength = webResponse.ContentLength;
                

                totalBytesRead = 0;
                Stream responseStream = webResponse.GetResponseStream();
                responseStream.BeginRead(
                    readBuffer,
                    0,
                    readBlockSize,
                    new AsyncCallback(ReadCallback),
                    responseStream);

            }
            catch (Exception error)
            {
                PostCompleted(error, false);
            }
        }



        private void ReadCallback(IAsyncResult ar)
        {
            Stream responseStream = (Stream)ar.AsyncState;
            try
            {
                int bytesRead = responseStream.EndRead(ar);
                if (bytesRead > 0)
                {
                    totalBytesRead += bytesRead;
                    tempDownloadStream.Write(readBuffer, 0, bytesRead);
                    responseStream.BeginRead(
                        readBuffer,
                        0,
                        readBlockSize,
                        new AsyncCallback(ReadCallback),
                        responseStream);
                    if (contentLength != -1)
                    {
                        int progress = (int)(100 * (((float)totalBytesRead) / ((float)contentLength)));
                        if (currentAsycLoadOperation != null)
                        {
                            currentAsycLoadOperation.Post(loadProgressDelegate, new ProgressChangedEventArgs(progress, null));
                        }
                    }
                }
                else
                {
                    tempDownloadStream.Seek(0, SeekOrigin.Begin);
                    if (currentAsycLoadOperation != null)
                    {
                        currentAsycLoadOperation.Post(loadProgressDelegate, new ProgressChangedEventArgs(100, null));
                    }
                    PostCompleted(null,false);

                    Stream rs = responseStream;
                    responseStream = null;
                    rs.Close();
                }
            }
            catch (Exception error)
            {
                PostCompleted(error, false);
                if (responseStream != null)
                {
                    responseStream.Close();
                }
            }
        }

        protected virtual void OnLoadProgressChanged(ProgressChangedEventArgs e)
        {
            ProgressChangedEventHandler handler = LoadProgressChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnLoadCompleted(AsyncCompletedEventArgs e)
        {
            if (LoadCompleted != null)
            {
                LoadCompleted(this, e);
            }
        }

        public event AsyncCompletedEventHandler LoadCompleted;

        public event ProgressChangedEventHandler LoadProgressChanged;
    }
}
