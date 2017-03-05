﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Shell32;
using System.IO;

namespace RecycleBinHelper
{
    class FileDisplacedDateComparer : IComparer<FolderItem2>
    {
        public int Compare(FolderItem2 x, FolderItem2 y)
        {
            return DeletedFiles.GetDisplacedDate(x).CompareTo(DeletedFiles.GetDisplacedDate(y));
        }
    }

    class FileNameComparer : IComparer<FolderItem2>
    {
        public int Compare(FolderItem2 x, FolderItem2 y)
        {
            return String.Compare(x.Name, y.Name, StringComparison.Ordinal);
        }
    }

    class DeletedFiles
    {
        // FMTID/PID identifier of a column that will be displayed by the Windows Explorer Details view
        // SRC: https://msdn.microsoft.com/en-us/library/ms538308.aspx
        // INDRECTIED-SRC: http://bbs.csdn.net/topics/390549753

        private const string PID_DISPLACED_FROM = "{9B174B33-40FF-11D2-A27E-00C04FC30871} 2";
        private const string PID_DISPLACED_DATE = "{9B174B33-40FF-11D2-A27E-00C04FC30871} 3";

        private List<FolderItem2> _files = new List<FolderItem2>();

        public List<FolderItem2> Items => _files;

        public static DateTime GetDisplacedDate(FolderItem2 item)
        {
            return (DateTime)item.ExtendedProperty(PID_DISPLACED_DATE);
        }

        public DeletedFiles()
        {
            Refresh();
        }

        public void SelectDaysBefore(uint days)
        {
            var dateBefore = DateTime.Today.AddDays(1 - (int)days).Date;
            IEnumerable<FolderItem2> temp =
                from file in _files
                where GetDisplacedDate(file) <= dateBefore
                select file
            ;

            _files = temp.ToList();
            this.Sort();
        }

        public void Sort()
        {
            _files.Sort(new FileNameComparer());
            _files.Sort(new FileDisplacedDateComparer());
        }

        public void Refresh()
        {
            Shell shell = new Shell();
            Folder recycleBin = shell.NameSpace(10);

            _files.Clear();
            foreach (FolderItem2 item in recycleBin.Items())
            {
                _files.Add(item);
            }

            Marshal.FinalReleaseComObject(shell);
        }

        public string Empty()
        {
            string ret = "";
            foreach (FolderItem2 item in _files)
            {
                Console.Write("Deleting {0}...", item.Name);
                try
                {
                    if (item.IsFolder)
                        Directory.Delete(item.Path, true);
                    else
                        File.Delete(item.Path);
                    Console.WriteLine("Successed");
                }
                catch (System.UnauthorizedAccessException exception)
                {
                    ret += $"{item.Name}\tUnauthorized\n";
                }
                catch (Exception exception)
                {
                    ret += $"{item.Name}\t{exception.Message}\n";
                }
            }
            return ret;
        }

        public int TotalFileSize => (from item in _files select item.Size).Sum();
    }
}
