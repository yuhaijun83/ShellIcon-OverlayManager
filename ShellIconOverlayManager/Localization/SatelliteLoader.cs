using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace ShellIconOverlayManager.Localization
{
    /// <summary>
    /// 五种语言的资源文件统一放在 Languages\&lt;culture&gt;\ShellIcon-OverlayManager.resources.dll。
    /// 该目录不在 .NET 默认的卫星程序集探测路径内，因此在这里接管加载。
    /// </summary>
    public static class SatelliteLoader
    {
        public const string FolderName = "Languages";
        public const string FileName = "ShellIcon-OverlayManager.resources.dll";

        private static bool _installed;

        public static void Install()
        {
            if (_installed)
            {
                return;
            }

            _installed = true;
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
        }

        private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                var name = new AssemblyName(args.Name);
                var culture = name.CultureInfo;
                if (culture == null || string.IsNullOrEmpty(culture.Name))
                {
                    return null;
                }

                foreach (var directory in GetBaseDirectories())
                {
                    var path = Path.Combine(directory, FolderName, culture.Name, FileName);
                    if (File.Exists(path))
                    {
                        return Assembly.LoadFrom(path);
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }

            return null;
        }

        /// <summary>候选的探测根目录：程序 exe 所在目录及 AppDomain 基目录。</summary>
        private static IEnumerable<string> GetBaseDirectories()
        {
            var list = new List<string>();

            try
            {
                var self = typeof(SatelliteLoader).Assembly;
                if (!string.IsNullOrEmpty(self.Location))
                {
                    list.Add(Path.GetDirectoryName(self.Location));
                }
            }
            catch (Exception)
            {
            }

            try
            {
                list.Add(AppDomain.CurrentDomain.BaseDirectory);
            }
            catch (Exception)
            {
            }

            return list;
        }
    }
}
