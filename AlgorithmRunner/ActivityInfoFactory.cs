﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlgorithmRunner
{
    class ActivityInfoFactory : ReviewInfoFactory
    {

        public string AttachmentPath { get; private set; }

        public ActivityInfoFactory(string pathToActivityLog, string pathToAttachments)
            : base(pathToActivityLog)
        {
            AttachmentPath = pathToAttachments;
        }

        public override IEnumerable<ReviewInfo> parseReviewInfos()
        {
            return GetActivityInfoFromFile(InputFilePath, AttachmentPath);
        }

        private static IEnumerable<ActivityInfo> GetActivityInfoFromFile(string pathToInputFile, string pathToAttachments)
        {
            Dictionary<int, List<string>> attachments = new Dictionary<int, List<string>>();
            var attachmentLines = File.ReadAllLines(pathToAttachments);
            foreach (var attachmentLine in attachmentLines)
            {
                var attachmentId = int.Parse(attachmentLine.Split(';')[1]);
                attachments.Add(attachmentId, attachmentLine.Split(';')[2].Split(',').Distinct().ToList());
            }


            var input = new StreamReader(pathToInputFile);
            var result = new List<ActivityInfo>();
            Debug.WriteLine("Starting ActivityInfo parsing at: " + DateTime.Now);
            try
            {
                string line;
                while ((line = input.ReadLine()) != null)
                {
                    ActivityInfo activityInfo = new ActivityInfo(line);
                    int? attachmentId = activityInfo.GetAttachmentId();

                    if (attachmentId != null)
                    {
                        activityInfo.Filenames = attachments[(int)attachmentId];
                    }

                    result.Add(activityInfo);
                }
            }
            finally
            {
                input.Close();
            }


            Debug.WriteLine("Finished ActivityInfo parsing at: " + DateTime.Now);

            return result;
        }

        protected override void PrefilterRawInput(string pathToRawInputFile)
        {
            var input = new StreamReader(pathToRawInputFile);
            string filteredInput;
            Debug.WriteLine("Starting parsing at: " + DateTime.Now);
            try
            {
                filteredInput = ActivityInfo.ParseAndFilterInput(input);
            }
            finally
            {
                input.Close();
            }

            Debug.WriteLine("Finished parsing at: " + DateTime.Now);

            File.WriteAllText(InputFilePath, filteredInput);

            Debug.WriteLine("Starting ordering at: " + DateTime.Now);
        }
    }
}
