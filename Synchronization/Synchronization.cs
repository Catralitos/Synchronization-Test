using System.Text;
using System.Timers;

namespace Synchronization
{
    public static class Synchronization
    {
        private static string _sourcePath;
        private static string _replicaPath;
        private static System.Timers.Timer _period;
        private static string _logPath;

        private static List<string> _previousSourceFilesNames;
        private static List<byte[]> _previousSourceFiles;

        private static List<string> _previousSourceDirectories;
        private static List<string> _directoriesToDelete;

        private static void Main(string[] args)
        {
            Synchronize();
            
            //First we set the paths and the period according to the program args
            _sourcePath = args[0];
            _replicaPath = args[1];
            SetPeriod(int.Parse(args[2]));
            _logPath = args[3];

            //We initialize lists to save data for comparison between previous and current states
            _previousSourceFilesNames = new List<string>();
            _previousSourceFiles = new List<byte[]>();
            _previousSourceDirectories = new List<string>();
            _directoriesToDelete = new List<string>();
                
            //Period only runs while the program is running, so I have it waiting for an input with readline
            //When the user inputs any command, the period stops and the application is terminated
            Console.WriteLine("\nPress any key to exit the application...\n");
            Console.WriteLine("The application started at {0:HH:mm:ss}", DateTime.Now);
            Console.ReadLine();
            _period.Stop();
            _period.Dispose();
      
            Console.WriteLine("Terminating the application...");
        }
        
        private static void SetPeriod(int desiredPeriod)
        {
            _period = new System.Timers.Timer(desiredPeriod * 1000);
            _period.Elapsed += OnTimedEvent;
            _period.AutoReset = true;
            _period.Enabled = true;
        }

        private static void OnTimedEvent(object? source, ElapsedEventArgs e)
        {
            Synchronize();
        }

        private static void Synchronize()
        {
            SynchronizeDirectories();
            SynchronizeFiles();
        }

        private static void SynchronizeDirectories()
        {
            //Check directories in source
            List<string> sourceDirectories =
                            Directory.EnumerateDirectories(_sourcePath, "*", SearchOption.AllDirectories).ToList();
            
            //If it's there now but wasn't there before, then it was created
            List<string> createdDirectories = (from directoryName in sourceDirectories where !_previousSourceDirectories.Contains(directoryName) select directoryName.Remove(0, _sourcePath.Length + 1)).ToList();

            //If it was there previously, and now isn't, then it was deleted
            //We save them outside the method, because there might be files inside which we want to delete first
            _directoriesToDelete = (from directoryName in _previousSourceDirectories where !sourceDirectories.Contains(directoryName) select directoryName.Remove(0, _sourcePath.Length + 1)).ToList();
            
            //Now we apply the changes and log them

            StringBuilder sb = new StringBuilder();

            foreach (string directoryName in createdDirectories)
            {
                //We create each subfolder that was created
                Directory.CreateDirectory(_replicaPath + "\\" + directoryName);
                
                //And we add it to our logging stringbuilder
                sb.Append(DateTime.Now + ": Created directory " + directoryName + " in replica folder.\n");
            }

            //And we store the previous directories
            _previousSourceDirectories = sourceDirectories;
            
            //We'll delete the folders last, when we sync the files.
            
            //Write the logs to the console screen and the log file
            File.AppendAllText(_logPath, sb.ToString());
            Console.WriteLine(sb.ToString());
        }

        private static void SynchronizeFiles()
        {
            //Check files in source
            List<string> sourceFiles = Directory.EnumerateFiles(_sourcePath, "*.*", SearchOption.AllDirectories).ToList();
            
            //If it's there now but wasn't there before, then it was created
            //We store only the path inside the source folder, to make it easier to change the replica folder
            List<string> createdFiles = (from fileName in sourceFiles where !_previousSourceFilesNames.Contains(fileName) select fileName.Remove(0, _sourcePath.Length + 1)).ToList();

            //If it was there previously, and now isn't, then it was deleted
            //We store only the path inside the source folder, to make it easier to change the replica folder
            List<string> deletedFiles = (from fileName in _previousSourceFilesNames where !sourceFiles.Contains(fileName) select fileName.Remove(0, _sourcePath.Length + 1)).ToList();

            List<string> modifiedFiles = new List<string>();

            //To check if files are equal, we compare their bytes
            for (int i = 0; i < sourceFiles.Count; i++)
            {
                for (int j = 0; j < _previousSourceFilesNames.Count; j++)
                {
                    if (sourceFiles[i] == _previousSourceFilesNames[j])
                    {
                        if (!FileEquals(File.ReadAllBytes(sourceFiles[i]), _previousSourceFiles[j]))
                        {
                            //We store only the path inside the source folder, to make it easier to change the replica folder
                            modifiedFiles.Add(sourceFiles[i].Remove(0, _sourcePath.Length + 1));
                        }
                    }
                }
            }
            
            //Now we apply the changes and log them

            StringBuilder sb = new StringBuilder();
            
            //Get the bytes from the created file and create a new on in the replica folder
            foreach (string fileName in createdFiles)
            {
                //Get the bytes from the created file
                byte[] bytes = File.ReadAllBytes(sourceFiles[sourceFiles.IndexOf(_sourcePath + "\\" + fileName)]);
                
                //Write them to the right place. This could go wrong if the correct subfolders didn't exist, 
                //but we ensured they would in the SynchronizeDirectories method
                File.WriteAllBytes(_replicaPath + "\\" + fileName, bytes);
                
                //And we add it to our logging stringbuilder
                sb.Append(DateTime.Now + ": Created file " + fileName + " in replica folder.\n");
            }
            
            //Delete the files to be deleted in the replica folder
            foreach (string fileName in deletedFiles)
            {
                //First off we delete the file
                File.Delete(_replicaPath + "\\"+ fileName);
                
                //And we add it to our logging stringbuilder
                sb.Append(DateTime.Now + ": Deleted file " + fileName + " in replica folder.\n");            
            }
            
            //Now that we deleted all the files marked for deletion, there's no chance of trying to delete a directory that still has files
            foreach (string directoryName in _directoriesToDelete)
            {
                //Delete the directory
                Directory.Delete(_replicaPath + "\\" + directoryName);
                    
                //And we add it to our logging stringbuilder
                sb.Append(DateTime.Now + ": Deleted directory " + directoryName + " in replica folder.\n");
            }
            
            //Copy and overwrite the modified files to the replica folder
            foreach (string fileName in modifiedFiles)
            {
                //We copy the changes to the existing file and overwrite it
                File.Copy(_sourcePath + "\\" + fileName, _replicaPath + "\\" + fileName, true);
                
                //And we add it to our logging stringbuilder
                sb.Append(DateTime.Now + ": Updated file " + fileName + " in replica folder.\n");            
            }
            
            //Update the previous files
            _previousSourceFilesNames = sourceFiles;
            _previousSourceFiles.Clear();
            foreach (string fileName in _previousSourceFilesNames)
            {
                _previousSourceFiles.Add(File.ReadAllBytes(fileName));
            }
            _directoriesToDelete.Clear();
            
            //Write the logs to the console screen and the log file
            File.AppendAllText(_logPath, sb.ToString());
            Console.WriteLine(sb.ToString());
        }
        
        private static bool FileEquals(byte[] file1, byte[] file2)
        {
            if (file1.Length != file2.Length) return false;
            for (int i = 0; i < file1.Length; i++)
            {
                if (file1[i] != file2[i])
                {
                    return false;
                }
            }
            return true;
        }
    }
}