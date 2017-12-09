using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AzureCloudDownloader
{
    public partial class AzureBlobContainer : Form
    {
        private string name;
        private string path;
        private string connectionString;
        private string fullPath { get { return path + "\\" + name; } }
        private bool containerExists = true;
        
        //full size of container in bytes
        private Int64 fullLength;
        //progressbar
        private int fileCount;
        private int currentFileNumber = 0;
        //fifo richtextbox
        private int maxLog = 10;
        private int n = 0;
        private List<string> lines = new List<string>();

        public AzureBlobContainer()
        {
            InitializeComponent();

        }

        private void button1_Click(object sender, EventArgs e)
        {
            name = NameBox.Text;
            if (String.IsNullOrEmpty(name))
            {
                logNoName();
                return;
            }

            connectionString = ConnectionString.Text;
            if (String.IsNullOrEmpty(connectionString))
            {
                logConnectionString();
                return;
            }

            folderBrowserDialog1.ShowDialog();
            if (String.IsNullOrEmpty(folderBrowserDialog1.SelectedPath))
            {
                updateLog("Der Dateipfad ist ungültig");
                return;
            } 
            path = folderBrowserDialog1.SelectedPath;

            
            Download();
        }


        private void Download()
        {
            
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(name);
            containerExists = container.Exists();
            logContainerExistence();
            if (!containerExists) return;

            System.IO.Directory.CreateDirectory(fullPath);

            IEnumerable<IListBlobItem> containerList = container.ListBlobs(null, false);
            fileCount = containerList.Count();

            

            foreach (IListBlobItem item in containerList)
            {
                if (item.GetType() == typeof(CloudBlockBlob))
                {
                    CloudBlockBlob blockBlob = (CloudBlockBlob)item;
                    fullLength += blockBlob.Properties.Length;
                    using (var fileStream = System.IO.File.OpenWrite(fullPath + "\\" + blockBlob.Name))
                    {
                        blockBlob.DownloadToStream(fileStream);
                        incrementProgressBar();
                    }
                } else if (item.GetType() == typeof(CloudPageBlob))
                {
                    CloudPageBlob pageBlob = (CloudPageBlob)item;
                    fullLength += pageBlob.Properties.Length;
                    using (var fileStream = System.IO.File.OpenWrite(fullPath + "\\" + pageBlob.Name))
                    {
                        pageBlob.DownloadToStream(fileStream);
                        incrementProgressBar();
                    }
                } else if (item.GetType() == typeof(CloudBlobDirectory))
                {
                    continue;
                }
            }

            if(fileCount == currentFileNumber)
            {
                updateLog(String.Format("Alle Dateien wurden heruntergeladen. Die Gesamtlänge betrug {0} Byte.",fullLength));
            }

        }

        private void incrementProgressBar()
        {
            int maxValue = toolStripProgressBar1.Maximum;
            currentFileNumber++;
            if (fileCount == 0) return;
            int ratio = (currentFileNumber / fileCount) * maxValue;
            int oldRatio = ((currentFileNumber - 1) / fileCount) * maxValue;
            int increment = ratio - oldRatio;
            if (increment == 0) return;
            toolStripProgressBar1.Increment(increment);
        }


        private void button2_Click(object sender, EventArgs e)
        {
            name = NameBox.Text;
            if (String.IsNullOrEmpty(name))
            {
                logNoName();
                return;
            }

            connectionString = ConnectionString.Text;
            if (String.IsNullOrEmpty(connectionString))
            {
                logConnectionString();
                return;
            }

            try
            {
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference(name);
                containerExists = container.Exists();
                logContainerExistence();
                updateLog("Die Verbindung konnte hergestellt werden");
            }
            catch (Exception)
            {
                updateLog("Es konnte keine Verbindung hergestellt werden");
            }
        }

        private void updateLog(string line)
        {
            lines.Insert(0, line);
            logTextBox.Text = string.Join("\n", lines.Take(maxLog).ToArray<string>());
            if (n < maxLog) n++;
        }

        private void logContainerExistence()
        {
            if (!containerExists)
            {
                updateLog("Der Container konnte nicht gefunden werden");
            }
        }

        private void logNoName()
        {
            updateLog("Es wurde kein Name für den Container eingetragen");
        }

        private void logConnectionString()
        {
            updateLog("Die Verbindungs-Zeichenkette ist leer");
            
        }

        private void label1_Click(object sender, EventArgs e)
        {
        }
    }
}
