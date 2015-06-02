﻿using AlgorithmRunner.AbstractIssueTracker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlgorithmRunner.Bugzilla
{
    class BugzillaAttachmentInfo : IssueTrackerEvent
    {
        public int BugId { get; set; }

        public override string ChangeId
        {
            get
            {
                return BugId.ToString();
            }
            set
            {
                BugId = int.Parse(value);
            }
        }

        public int AttachmentId { get; set; }

        public BugzillaAttachmentInfo(string csvAttachmentLine)
        {
            string line = csvAttachmentLine.ToLower();
            var fields = line.Split(';');

            BugId = int.Parse(fields[0]);
            AttachmentId = int.Parse(fields[1]);
            Filenames = fields[2].Split(',').Distinct().ToList();

            if (fields.Length > 3)    // is the upload date already found out?
                When = DateTime.Parse(fields[3]);
        }

        public override bool isValid()
        {
            // filter if not in examined window of time
            if (When < BugzillaReview.mercurialTransferDate || When > BugzillaReview.endOfHgDump)
                return false;

            // filter if there are no files
            if (!Filenames.Any())
                return false;

            // filter if there is only one file with no name 
            if (Filenames.Count == 1 && Filenames[0] == string.Empty)
                return false;

            return true;
        }
    }
}
