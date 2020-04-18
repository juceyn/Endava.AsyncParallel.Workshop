using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Endava.AsyncParallel.Workshop.SecondHomework
{
    public class FileProcessorWithTasks
    {
        private FileSystemWatcher _fileSystemWatcher;
        private static object _syncObject = new object();
        private List<string> _filePathList;
        private Task[] tasks = new Task[4];
        private CountdownEvent _countdown = new CountdownEvent(10);
        private readonly StringBuilder _sb;
        

        public FileProcessorWithTasks(string folder)
        {
            _fileSystemWatcher = new FileSystemWatcher();
            _filePathList = new List<string>();
            _sb = new StringBuilder();
            
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            _fileSystemWatcher.Path = folder;
            _fileSystemWatcher.Created += FileSystemWatcher_Created;
            _fileSystemWatcher.EnableRaisingEvents = true;  
        }

        public void Start()
        {
            Action consumer = () =>
            {
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
                            lock(_countdown)
                                _countdown.Signal();
                        }
                    }
                }
            };

            tasks[0] = Task.Factory.StartNew(consumer);
            tasks[1] = Task.Factory.StartNew(consumer);
            tasks[2] = Task.Factory.StartNew(consumer);
            tasks[3] = Task.Factory.StartNew(consumer);
            Task.WaitAll(tasks);

            Console.WriteLine("Finished file processing");
            Console.WriteLine(_sb.ToString());
        }
        
        private void FileSystemWatcher_Created(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine($"File: {e.FullPath}");

            Action producer = () =>
            {
                lock (_syncObject)
                {
                    _filePathList.Add((string)e.FullPath);
                }
            };
            Task.Factory.StartNew(producer);
        }

        static void Main(string[] args)
        {
            FileProcessorWithTasks fileProcessor = new FileProcessorWithTasks(Path.Combine(Environment.CurrentDirectory, "Files"));
            fileProcessor.Start();
            Console.WriteLine("Press any key to end");
            Console.ReadKey();
        }
    }
}
