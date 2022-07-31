using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using BepInEx.Logging;

namespace MelonLoader.MonoInternals.ResolveInternals
{
    internal static class SearchDirectoryManager
    {
        private static List<SearchDirectoryInfo> SearchDirectoryList = new List<SearchDirectoryInfo>();

        private static void Sort()
            => SearchDirectoryList =
                SearchDirectoryList.OrderBy(x => x.Priority).ToList();

        internal static void Add(string path, int priority = 0)
        {
            if (string.IsNullOrEmpty(path))
                return;

            path = Path.GetFullPath(path);
            if (path.ContainsExtension())
                return;

            SearchDirectoryInfo searchDirectory = SearchDirectoryList.FirstOrDefault(x => x.Path.Equals(path));
            if (searchDirectory != null)
                return;

            searchDirectory = new SearchDirectoryInfo();
            searchDirectory.Path = path;
            searchDirectory.Priority = priority;
            SearchDirectoryList.Add(searchDirectory);

            Sort();
        }

        internal static void Remove(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            path = Path.GetFullPath(path);
            if (path.ContainsExtension())
                return;

            SearchDirectoryInfo searchDirectory = SearchDirectoryList.FirstOrDefault(x => x.Path.Equals(path));
            if (searchDirectory == null)
                return;

            SearchDirectoryList.Remove(searchDirectory);

            Sort();
        }

        internal static Assembly Scan(string requested_name)
        {
            LemonEnumerator<SearchDirectoryInfo> enumerator = new LemonEnumerator<SearchDirectoryInfo>(SearchDirectoryList);
            while (enumerator.MoveNext())
            {
                string folderpath = enumerator.Current.Path;

                string filepath = Directory.GetFiles(folderpath
                ).FirstOrDefault(x => !string.IsNullOrEmpty(x)
                                      && Path.GetExtension(x).ToLowerInvariant().Equals(".dll")
                                      && Path.GetFileName(x).Equals($"{requested_name}.dll"));

                if (string.IsNullOrEmpty(filepath))
                    continue;

                try
                {
                    return Assembly.LoadFile(filepath);
                } catch (Exception e)
                {
                    MelonLogger.Msg(e.ToString());
                    return null;
                }
            }

            return null;
        }

        private class SearchDirectoryInfo
        {
            internal string Path = null;
            internal int Priority = 0;
        }
    }
}
