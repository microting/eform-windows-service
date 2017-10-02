using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileWatchingService
{
    public class FileWatcher
    {
        private FileSystemWatcher _fileWatcher;

        public FileWatcher()
        {
            _fileWatcher = new FileSystemWatcher(PathLocation());

            _fileWatcher.Created += _fileWatcher_Created;
            _fileWatcher.Deleted += _fileWatcher_Deleted;
            _fileWatcher.Changed += _fileWatcher_Changed;

            _fileWatcher.EnableRaisingEvents = true;
        }

        private string PathLocation()
        {
            string value = String.Empty;

            try
            {
                value = System.Configuration.ConfigurationManager.AppSettings["location_inbound"];
                if (value != String.Empty)
                {
                    return value;
                }
            }
            catch
            {

            }

            return value;

        }

        private void _fileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void _fileWatcher_Deleted(object sender, FileSystemEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void _fileWatcher_Created(object sender, FileSystemEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
