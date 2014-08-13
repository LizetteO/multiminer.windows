﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;

namespace MultiMiner.Utility.Serialization
{
    public class ObjectLogger
    {
        private const int maxFileSize = 1 * 1000 * 1000; //1mb
        private readonly bool rollOverFiles;
        private readonly int oldFileSets;

        public ObjectLogger(bool rollOverFiles, int oldFileSets)
        {
            this.rollOverFiles = rollOverFiles;
            this.oldFileSets = oldFileSets;
        }

        public static IEnumerable<T> LoadLogFile<T>(string logFilePath)
        {
            string[] logFile = File.ReadAllLines(logFilePath);
            List<T> result = new List<T>();
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            foreach (string line in logFile)
            {
                result.Add(serializer.Deserialize<T>(line));
            }
            return result;
        }

        public void LogObjectToFile(Object objectToLog, string logFilePath)
        {
            RollOverLogFile(logFilePath);

            JavaScriptSerializer serializer = new JavaScriptSerializer();
            string jsonData = serializer.Serialize(objectToLog);

            File.AppendAllText(logFilePath, jsonData + Environment.NewLine);
        }

        private void RollOverLogFile(string logFilePath)
        {
            if (rollOverFiles && File.Exists(logFilePath))
            {
                FileInfo fileInfo = new FileInfo(logFilePath);
                if (fileInfo.Length > maxFileSize)
                {
                    BackupLogFileToSets(logFilePath);
                    File.Delete(logFilePath);
                }
            }
        }

        private void BackupLogFileToSets(string logFilePath)
        {
            if (oldFileSets > 0)
            {
                string backupFilePath;

                for (int i = oldFileSets; i > 0; i--)
                {
                    backupFilePath = Path.ChangeExtension(logFilePath, i.ToString());
                    string previousFilePath = Path.ChangeExtension(logFilePath, (i - 1).ToString());
                    if (File.Exists(backupFilePath))
                    {
                        try
                        {
                            File.Delete(backupFilePath);
                        }
                        catch (UnauthorizedAccessException err)
                        {
                            //users report occasionally receiving access denied rolling log files
                            //rename/move the file instead
                            File.Move(backupFilePath, Path.ChangeExtension(backupFilePath, "deleteme"));
                        }
                    }
                    if (File.Exists(previousFilePath))
                        File.Move(previousFilePath, backupFilePath);
                }

                backupFilePath = Path.ChangeExtension(logFilePath, "1");
                if (File.Exists(backupFilePath))
                    File.Delete(backupFilePath);
                File.Move(logFilePath, backupFilePath);
            }
        }
    }
}
