﻿/* Copyright (c) 2024 Rick (rick 'at' gibbed 'dot' us)
 *
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 *
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would
 *    be appreciated but is not required.
 *
 * 2. Altered source versions must be plainly marked as such, and must not
 *    be misrepresented as being the original software.
 *
 * 3. This notice may not be removed or altered from any source
 *    distribution.
 */

using System.IO;
using System.Linq;

namespace Gibbed.DragonsDogma2.FileFormats
{
    public static class ProjectHelpers
    {
        public static string GetExecutablePath()
        {
            using var process = System.Diagnostics.Process.GetCurrentProcess();
            var path = Path.GetFullPath(process.MainModule.FileName);
            return Path.GetFullPath(path);
        }

        public static string GetExecutableName()
        {
            return Path.GetFileName(GetExecutablePath());
        }

        public static string GetCurrentProjectNamePath()
        {
            var executablePath = GetExecutablePath();
            var binPath = Path.GetDirectoryName(executablePath);
            return Path.Combine(binPath, "..", "configs", "current.txt");
        }

        public static string LoadCurrentProjectName()
        {
            string projectName = null;
            var projectNamePath = GetCurrentProjectNamePath();
            if (File.Exists(projectNamePath) == true)
            {
                projectName = File.ReadLines(projectNamePath).FirstOrDefault();
            }
            if (string.IsNullOrEmpty(projectName) == true)
            {
                projectName = "Dragon's Dogma 2";
            }
            return projectName;
        }

        public static string GetProjectPath(string projectName)
        {
            var executablePath = GetExecutablePath();
            var binPath = Path.GetDirectoryName(executablePath);
            return Path.Combine(binPath, "..", "configs", projectName, "project.json");
        }

        public static ProjectData.HashList<ulong> LoadListsFileNames(this ProjectData.Project project)
        {
            return project.LoadLists("*.filelist", s => s.HashFileName(), s => s.Replace('\\', '/'));
        }
    }
}
