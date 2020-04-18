using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading;

namespace Endava.AsyncParallel.Workshop.FirstHomework
{
    public class FileProcessor
    {
        private FileSystemWatcher _fileSystemWatcher;
        private static object _syncObject = new object();
        private List<string> _filePathList;
        private BackgroundWorker[] _workers = new BackgroundWorker[4];
        private CountdownEvent _countdown = new CountdownEvent(10);
        private BackgroundWorker _feeder;
        private readonly StringBuilder _sb;   

        public FileProcessor(string folder)
        {
            _fileSystemWatcher = new FileSystemWatcher();
            _filePathList = new List<string>();
            _sb = new StringBuilder();
            InitializeWorkers();
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            _fileSystemWatcher.Path = folder;
            _fileSystemWatcher.Created += FileSystemWatcher_Created;
            _fileSystemWatcher.EnableRaisingEvents = true;
        }

        private void InitializeWorkers()
        {
            _feeder = new BackgroundWorker();
            _workers[0] = new BackgroundWorker();
            _workers[1] = new BackgroundWorker();
            _workers[2] = new BackgroundWorker();
            _workers[3] = new BackgroundWorker();
            _feeder.DoWork += _feeder_DoWork;
            _workers[0].DoWork += FileProcessor_DoWork;
            _workers[1].DoWork += FileProcessor_DoWork;
            _workers[2].DoWork += FileProcessor_DoWork;
            _workers[3].DoWork += FileProcessor_DoWork;

        }

        public void Start()
        {
            _workers[0].RunWorkerAsync();
            _workers[1].RunWorkerAsync();
            _workers[2].RunWorkerAsync();
            _workers[3].RunWorkerAsync();
            _countdown.Wait();
            Console.WriteLine("Finished file processing");
            Console.WriteLine(_sb.ToString());
        }

        private void _feeder_DoWork(object sender, DoWorkEventArgs e)
        {
            lock (_syncObject)
                _filePathList.Add((string)e.Argument);
        }

        private void FileProcessor_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            lock (_syncObject)
            {
                Console.WriteLine("finished");
                _countdown.Signal();
            }
        }

        private void FileProcessor_DoWork(object sender, DoWorkEventArgs e)
        {
            //Process file 
            while (_countdown.CurrentCount > 0)
            {
                lock (_syncObject)
                {

                    if (_filePathList.Count > 0)
                    {
                        string path = _filePathList[0];
                        _filePathList.RemoveAt(0);
                        Console.WriteLine($"Processing {path}");
                        //Write to shared space, for example StringBuilder
                        _sb.Append(path).Append(". Content: ").Append("\n").Append(File.ReadAllText(path));
                        _countdown.Signal();
                    }
                }
            }
        }

        private void FileSystemWatcher_Created(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine($"File: {e.FullPath}");
            while (_feeder.IsBusy)
                Thread.Sleep(10);
            _feeder.RunWorkerAsync(e.FullPath);
        }

        static void Main(string[] args)
        {
            FileProcessor fileProcessor = new FileProcessor(Path.Combine(Environment.CurrentDirectory, "Files"));
            fileProcessor.Start();
            Console.WriteLine("Press any key to end");
            Console.ReadKey();
        }
    }
}
